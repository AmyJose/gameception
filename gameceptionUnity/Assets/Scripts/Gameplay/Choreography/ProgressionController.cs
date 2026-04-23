using UnityEngine;
using Rhythm;
using System;
using System.Collections.Generic;

namespace Gameplay.Choreography
{
    public class ProgressionController : MonoBehaviour
    {
        [Serializable]
        private struct DifficultyBpmBand
        {
            [Min(1f)] public float min;
            [Min(1f)] public float max;
            public float median => Mathf.Round((min + max) * 0.5f);
        }

        [Serializable]
        private struct DifficultyPromptBand
        {
            [Min(1)] public int generationIntervalBeats;
            [Min(1)] public int promptSpawnBeatSpacing;
            [Min(0)] public int promptsPerLane0;
            [Min(0)] public int promptsPerLane1;
            [Min(0)] public int promptsPerLane2;
            [Min(0)] public int promptsPerLane3;
            public bool keepConsecutive;
        }

        [Header("References")]
        [SerializeField] private BeatClock beatClock;
        [SerializeField] private PromptQueue promptQueue;
        [SerializeField] private PromptJudge promptJudge;
        [SerializeField] private GameFlowController gameFlowController;

        public enum Difficulty
        {
            Easy,
            Medium,
            Hard
        }

        [Header("Difficulty Windows (seconds remaining)")]
        [SerializeField, Min(0f)] private float easyAboveSeconds = 200f;
        [SerializeField, Min(0f)] private float mediumAboveSeconds = 80f;

        [Header("BPM Bounds Per Difficulty")]
        [SerializeField] private DifficultyBpmBand easyBpm = new DifficultyBpmBand { min = 60f, max = 75f };
        [SerializeField] private DifficultyBpmBand mediumBpm = new DifficultyBpmBand { min = 75f, max = 95f };
        [SerializeField] private DifficultyBpmBand hardBpm = new DifficultyBpmBand { min = 95f, max = 120f };

        [Header("Prompt Bounds Per Difficulty")]
        [SerializeField] private DifficultyPromptBand easyPrompt = new DifficultyPromptBand
        {
            generationIntervalBeats = 10,
            promptSpawnBeatSpacing = 2,
            promptsPerLane0 = 3,
            promptsPerLane1 = 2,
            promptsPerLane2 = 2,
            promptsPerLane3 = 2,
            keepConsecutive = true
        };

        [SerializeField] private DifficultyPromptBand mediumPrompt = new DifficultyPromptBand
        {
            generationIntervalBeats = 12,
            promptSpawnBeatSpacing = 2,
            promptsPerLane0 = 1,
            promptsPerLane1 = 1,
            promptsPerLane2 = 1,
            promptsPerLane3 = 1,
            keepConsecutive = true
        };

        [SerializeField] private DifficultyPromptBand hardPrompt = new DifficultyPromptBand
        {
            generationIntervalBeats = 14,
            promptSpawnBeatSpacing = 2,
            promptsPerLane0 = 2,
            promptsPerLane1 = 1,
            promptsPerLane2 = 1,
            promptsPerLane3 = 2,
            keepConsecutive = true
        };

        [Header("Adaptive BPM - Performance Memory")]
        [SerializeField, Min(1)] private int recentSequenceCount = 3;
        [SerializeField] private Vector3 recentSequenceWeights = new Vector3(0.5f, 0.3f, 0.2f);

        [Header("Adaptive BPM - Decision Thresholds")]
        [SerializeField, Range(0f, 1f)] private float raiseThreshold = 0.80f;
        [SerializeField, Range(0f, 1f)] private float lowerThreshold = 0.45f;

        [Header("Adaptive BPM - BPM Change")]
        [SerializeField, Min(0.1f)] private float bpmIncreaseStep = 2f;
        [SerializeField, Min(0.1f)] private float bpmDecreaseStep = 3f;

        [Header("Adaptive BPM - Stability")]
        [SerializeField, Min(0)] private int sequenceCooldownAfterChange = 1;
        [SerializeField] private bool resetPerformanceMemoryOnDifficultyChange = true;

        [Header("Double Trouble Scheduling")]
        [SerializeField] private bool enableDoubleTroubleScheduling = true;
        [SerializeField, Min(0f)] private float doubleTroubleStartSeconds = 170f;  // Start at N sec remaining
        [SerializeField, Min(1f)] private float doubleTroubleDurationSeconds = 30f;  // Active for 30 seconds

        private float _doubleTroubleEndTime = -1f;
        private bool _doubleTroubleActive = false;

        private float _remainingTime;
        private Difficulty _currentDifficulty = Difficulty.Easy;
        private float _currentBpm;

        private readonly Queue<float> _recentAccuracies = new Queue<float>();
        private int _sequencesUntilNextBpmChange = 0;

        private void OnEnable()
        {
            if (gameFlowController != null)
                gameFlowController.OnTimerUpdated += HandleTimerUpdated;

            if (promptJudge != null)
                promptJudge.OnSequenceComplete += HandleSequenceComplete;
        }

        private void OnDisable()
        {
            if (gameFlowController != null)
                gameFlowController.OnTimerUpdated -= HandleTimerUpdated;

            if (promptJudge != null)
                promptJudge.OnSequenceComplete -= HandleSequenceComplete;
        }

        private void Start()
        {
            if (gameFlowController == null || beatClock == null || promptQueue == null)
                return;

            _remainingTime = gameFlowController.RemainingTime;
            _currentDifficulty = GetDifficultyForTime(_remainingTime);

            var bpmBand = GetBpmBand(_currentDifficulty);
            _currentBpm = bpmBand.median;
            beatClock.SetBpm(_currentBpm);

            var promptBand = GetPromptBand(_currentDifficulty);
            ApplyPromptBandSettings(promptBand);

            Debug.Log($"[Progression] Start -> {_currentDifficulty}, BPM={_currentBpm}");
        }

        private void HandleTimerUpdated(float timeLeft)
        {
            _remainingTime = timeLeft;

            Difficulty newDifficulty = GetDifficultyForTime(_remainingTime);

            if (newDifficulty != _currentDifficulty)
            {
                _currentDifficulty = newDifficulty;

                var bpmBand = GetBpmBand(_currentDifficulty);
                _currentBpm = bpmBand.median;
                beatClock.SetBpm(_currentBpm);

                var promptBand = GetPromptBand(_currentDifficulty);
                ApplyPromptBandSettings(promptBand);

                if (resetPerformanceMemoryOnDifficultyChange)
                    ClearPerformanceMemory();

                Debug.Log($"[Progression] Difficulty -> {_currentDifficulty}, BPM reset to {_currentBpm}");
            }

            // Handle Double Trouble scheduling
            if (enableDoubleTroubleScheduling && promptQueue != null)
            {
                HandleDoubleTroubleScheduling(timeLeft);
            }
        }

        private void HandleDoubleTroubleScheduling(float timeLeft)
        {
            // Calculate when Double Trouble window is active
            float doubleTroubleStart = doubleTroubleStartSeconds;
            float doubleTroubleEnd = doubleTroubleStart - doubleTroubleDurationSeconds;

            bool shouldBeActive = (timeLeft <= doubleTroubleStart && timeLeft > doubleTroubleEnd);

            // Toggle on
            if (shouldBeActive && !_doubleTroubleActive)
            {
                promptQueue.SetDoubleTroubleMode(true);
                _doubleTroubleActive = true;
                Debug.Log($"[Progression] Double Trouble ENABLED (active until {doubleTroubleEnd:F1}s remaining)");
            }

            // Toggle off
            if (!shouldBeActive && _doubleTroubleActive)
            {
                promptQueue.SetDoubleTroubleMode(false);
                _doubleTroubleActive = false;
                Debug.Log($"[Progression] Double Trouble DISABLED");
            }
        }

        private void HandleSequenceComplete(PromptJudge.SequenceResult result)
        {
            AddRecentAccuracy(result.accuracy);

            if (_sequencesUntilNextBpmChange > 0)
            {
                _sequencesUntilNextBpmChange--;

                Debug.Log(
                    $"[Progression] Sequence {result.sequenceId} complete | " +
                    $"Accuracy={result.accuracy:P0} | " +
                    $"Cooldown active ({_sequencesUntilNextBpmChange} left) | BPM stays {_currentBpm}"
                );

                return;
            }

            float recentPerformanceScore = CalculateRecentPerformanceScore();
            var bpmBand = GetBpmBand(_currentDifficulty);

            float bpmBefore = _currentBpm;
            string decision = "Hold";

            if (recentPerformanceScore >= raiseThreshold)
            {
                _currentBpm += bpmIncreaseStep;
                decision = "Raise";
            }
            else if (recentPerformanceScore <= lowerThreshold)
            {
                _currentBpm -= bpmDecreaseStep;
                decision = "Lower";
            }

            _currentBpm = Mathf.Clamp(_currentBpm, bpmBand.min, bpmBand.max);

            if (!Mathf.Approximately(_currentBpm, bpmBefore))
            {
                beatClock.SetBpm(_currentBpm);
                _sequencesUntilNextBpmChange = sequenceCooldownAfterChange;
            }

            Debug.Log(
                $"[Progression] Sequence {result.sequenceId} complete | " +
                $"Hits={result.hitsCount}/{result.totalPrompts} ({result.accuracy:P0}) | " +
                $"RecentScore={recentPerformanceScore:F2} | " +
                $"Decision={decision} | BPM {bpmBefore} -> {_currentBpm} | " +
                $"Difficulty={_currentDifficulty}"
            );
        }

        private void AddRecentAccuracy(float accuracy)
        {
            _recentAccuracies.Enqueue(Mathf.Clamp01(accuracy));

            while (_recentAccuracies.Count > recentSequenceCount)
                _recentAccuracies.Dequeue();
        }

        private float CalculateRecentPerformanceScore()
        {
            if (_recentAccuracies.Count == 0)
                return 0.5f;

            float[] values = _recentAccuracies.ToArray();

            float newestWeight = recentSequenceWeights.x;
            float middleWeight = recentSequenceWeights.y;
            float oldestWeight = recentSequenceWeights.z;

            float weightedSum = 0f;
            float totalWeight = 0f;

            int newestIndex = values.Length - 1;
            weightedSum += values[newestIndex] * newestWeight;
            totalWeight += newestWeight;

            if (values.Length >= 2)
            {
                int previousIndex = values.Length - 2;
                weightedSum += values[previousIndex] * middleWeight;
                totalWeight += middleWeight;
            }

            if (values.Length >= 3)
            {
                int oldestIndex = values.Length - 3;
                weightedSum += values[oldestIndex] * oldestWeight;
                totalWeight += oldestWeight;
            }

            if (totalWeight <= 0f)
                return values[newestIndex];

            return weightedSum / totalWeight;
        }

        private void ClearPerformanceMemory()
        {
            _recentAccuracies.Clear();
            _sequencesUntilNextBpmChange = 0;
        }

        private void ApplyPromptBandSettings(DifficultyPromptBand promptBand)
        {
            promptQueue.SetGenerationIntervalBeats(promptBand.generationIntervalBeats);
            promptQueue.SetPromptSpawnBeatSpacing(promptBand.promptSpawnBeatSpacing);

            promptQueue.SetLanePromptsPerSequence(0, promptBand.promptsPerLane0);
            promptQueue.SetLanePromptsPerSequence(1, promptBand.promptsPerLane1);
            promptQueue.SetLanePromptsPerSequence(2, promptBand.promptsPerLane2);
            promptQueue.SetLanePromptsPerSequence(3, promptBand.promptsPerLane3);

            promptQueue.SetKeepLanePromptsConsecutive(promptBand.keepConsecutive);

            Debug.Log(
                $"[Progression] Prompt settings -> Interval={promptBand.generationIntervalBeats}, " +
                $"Spacing={promptBand.promptSpawnBeatSpacing}, " +
                $"PerLane=[{promptBand.promptsPerLane0},{promptBand.promptsPerLane1}," +
                $"{promptBand.promptsPerLane2},{promptBand.promptsPerLane3}], " +
                $"Consecutive={promptBand.keepConsecutive}"
            );
        }

        private Difficulty GetDifficultyForTime(float timeLeft)
        {
            if (timeLeft > easyAboveSeconds) return Difficulty.Easy;
            if (timeLeft > mediumAboveSeconds) return Difficulty.Medium;
            return Difficulty.Hard;
        }

        private DifficultyBpmBand GetBpmBand(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
                    return easyBpm;
                case Difficulty.Medium:
                    return mediumBpm;
                case Difficulty.Hard:
                default:
                    return hardBpm;
            }
        }

        private DifficultyPromptBand GetPromptBand(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
                    return easyPrompt;
                case Difficulty.Medium:
                    return mediumPrompt;
                case Difficulty.Hard:
                default:
                    return hardPrompt;
            }
        }
    }
}