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
            [Min(10)] public int generationIntervalBeats;
            [Min(4)] public int PromptsPerSequence;
            [Min(2)] public int minExtraLaneRepeatsPerSequence;
            [Min(4)] public int maxExtraLaneRepeatsPerSequence;
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
        [SerializeField] private DifficultyPromptBand easyPrompt = new DifficultyPromptBand { generationIntervalBeats = 10, PromptsPerSequence = 4, minExtraLaneRepeatsPerSequence = 2, maxExtraLaneRepeatsPerSequence = 4 };
        [SerializeField] private DifficultyPromptBand mediumPrompt = new DifficultyPromptBand { generationIntervalBeats = 12, PromptsPerSequence = 4, minExtraLaneRepeatsPerSequence = 4, maxExtraLaneRepeatsPerSequence = 6 };
        [SerializeField] private DifficultyPromptBand hardPrompt = new DifficultyPromptBand { generationIntervalBeats = 14, PromptsPerSequence = 4, minExtraLaneRepeatsPerSequence = 6, maxExtraLaneRepeatsPerSequence = 7 };

        [Header("BPM Transition")]
        [SerializeField, Range(0f, 1f)] private float bpmSmoothing = 0.2f;

        private float _remainingTime;
        private Difficulty _currentDifficulty = Difficulty.Easy;

        private float _performanceOffset = 0f; // future use for performance-based adjustments

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


                var band = GetBpmBand(_currentDifficulty);
                float targetBpm = band.median;

                beatClock.SetBpm(targetBpm);

                Debug.Log($"[Progression] Difficulty → {_currentDifficulty}, BPM set to {targetBpm}");


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
                // promptQueue.SetGenerationIntervalBeats(promptBand.generationIntervalBeats);
                // promptQueue.SetPromptsPerSequence(promptBand.PromptsPerSequence);
                // promptQueue.SetExtraLaneRepeatsPerSequence(promptBand.minExtraLaneRepeatsPerSequence);

                Debug.Log($"[Progression] Difficulty -> {_currentDifficulty}, BPM={targetBpm}, Prompts: interval={promptBand.generationIntervalBeats}, perSeq={promptBand.PromptsPerSequence}");
            }

        }

        //might need this later if we want to adjust difficulty based on performance 
        /*private void Update()
        {

        }*/

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