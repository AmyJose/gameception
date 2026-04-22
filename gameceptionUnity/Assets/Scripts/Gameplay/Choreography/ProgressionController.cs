using UnityEngine;
using Rhythm;
using System;

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

        [Header("BPM Transition")]
        [SerializeField, Range(0f, 1f)] private float bpmSmoothing = 0.2f;

        private float _remainingTime;
        private Difficulty _currentDifficulty = Difficulty.Easy;

        private float _performanceOffset = 0f; // for future difficulty

        private void OnEnable()
        {
            if (gameFlowController != null)
                gameFlowController.OnTimerUpdated += HandleTimerUpdated;
        }

        private void OnDisable()
        {
            if (gameFlowController != null)
                gameFlowController.OnTimerUpdated -= HandleTimerUpdated;
        }

        private void HandleTimerUpdated(float timeLeft)
        {
            _remainingTime = timeLeft;

            Difficulty newDifficulty = GetDifficultyForTime(_remainingTime);

            if (newDifficulty != _currentDifficulty)
            {
                _currentDifficulty = newDifficulty;
                _performanceOffset = 0f;

                var bpmBand = GetBpmBand(_currentDifficulty);
                float targetBpm = bpmBand.median;
                beatClock.SetBpm(targetBpm);

                var promptBand = GetPromptBand(_currentDifficulty);
                ApplyPromptBandSettings(promptBand);

                Debug.Log($"[Progression] Difficulty -> {_currentDifficulty}, BPM={targetBpm}");
            }
        }

        private void Start()
        {
            if (gameFlowController != null)
            {
                _remainingTime = gameFlowController.RemainingTime;
                _currentDifficulty = GetDifficultyForTime(_remainingTime);

                var bpmBand = GetBpmBand(_currentDifficulty);
                float targetBpm = bpmBand.median;
                beatClock.SetBpm(targetBpm);

                var promptBand = GetPromptBand(_currentDifficulty);
                ApplyPromptBandSettings(promptBand);

                Debug.Log($"[Progression] Start -> {_currentDifficulty}, BPM={targetBpm}");
            }
        }

        private void ApplyPromptBandSettings(DifficultyPromptBand promptBand)
        {
            // Apply generation timing
            promptQueue.SetGenerationIntervalBeats(promptBand.generationIntervalBeats);
            
            // Apply spawn spacing
            promptQueue.SetPromptSpawnBeatSpacing(promptBand.promptSpawnBeatSpacing);
            
            // Apply per-lane prompt counts
            promptQueue.SetLanePromptsPerSequence(0, promptBand.promptsPerLane0);
            promptQueue.SetLanePromptsPerSequence(1, promptBand.promptsPerLane1);
            promptQueue.SetLanePromptsPerSequence(2, promptBand.promptsPerLane2);
            promptQueue.SetLanePromptsPerSequence(3, promptBand.promptsPerLane3);
            
            // Apply consecutive lane grouping
            promptQueue.SetKeepLanePromptsConsecutive(promptBand.keepConsecutive);

            Debug.Log($"[Progression] Prompt settings -> Interval={promptBand.generationIntervalBeats}, Spacing={promptBand.promptSpawnBeatSpacing}, PerLane=[{promptBand.promptsPerLane0},{promptBand.promptsPerLane1},{promptBand.promptsPerLane2},{promptBand.promptsPerLane3}], Consecutive={promptBand.keepConsecutive}");
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