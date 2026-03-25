using System;
using System.Collections.Generic;
using InputLayer;
using UnityEngine;

namespace Gameplay.Choreography
{
    // Validates player performance for choreography prompts

    public class ChoreographyJudge : MonoBehaviour
    {
        [Header("Judgement Windows")]
        [SerializeField] private double perfectWindow = 0.15; //sec
        [SerializeField] private double goodWindow = 0.3; //sec
        [SerializeField] private double missWindow = 0.45; //sec

        [Header("Pose Requirements")]
        [SerializeField] private float minPoseConfidence = 0.75f;
        [SerializeField] private long poseStabilityDebounceMs = 200; //how long a pose must be held

        [Header("Dependencies")]
        [SerializeField] private PoseState poseState;
        [SerializeField] private SelectionState selectionState;
        [SerializeField] private ChoreographyQueueState choreographyQueue;

        //Emitted when a prompt is judged
        public event Action<JudgementResult> OnPromptJudged;

        public struct JudgementResult
        {
            public int promptId;
            public HitQuality quality;
            public ElementPose detectedPose;
            public int selectedPad;
            public double timingOffset; //seconds from perfect beat
        }

        public enum HitQuality
        {
            Perfect,
            Good,
            Early,
            Late,
            WrongPose,
            WrongPad,
            NoInput,
            Miss
        }

        private readonly Dictionary<int, long> _lastJudgedPrompt = new(); // promptId: judgement timestamp

        private void OnEnable()
        {
            if (choreographyQueue != null)
                choreographyQueue.OnPromptActive += JudgePrompt;
        }

        private void OnDisable()
        {
            if (choreographyQueue != null)
                choreographyQueue.OnPromptActive -= JudgePrompt;
        }

        private void JudgePrompt(ChoreographyQueueState.PromptData promptData)
        {
            // Debounce: prevent judging same prompt twice
            long now = GetCurrentTimestampMs();
            if (_lastJudgedPrompt.TryGetValue(promptData.promptId, out long lastTime))
            {
                if (now - lastTime < poseStabilityDebounceMs)
                    return;
            }

            _lastJudgedPrompt[promptData.promptId] = now;

            var result = new JudgementResult
            {
                promptId = promptData.promptId,
                detectedPose = poseState?.CurrentPose ?? ElementPose.None,
                selectedPad = GetSelectedPad(),
                quality = EvaluateHit(promptData)
            };

            Debug.Log($"[ChoreographyJudge] Prompt {result.promptId}: {result.quality} " +
                     $"(Detected: {result.detectedPose}, Selected Pad: {result.selectedPad})");

            OnPromptJudged?.Invoke(result);
        }

        private HitQuality EvaluateHit(ChoreographyQueueState.PromptData prompt)
        {
            // // Check if a pad is selected
            // if (selectionState?.Selected.Count == 0)
            //     return HitQuality.NoInput;

            // // Check pose confidence
            // if (poseState.Confidence < minPoseConfidence)
            //     return HitQuality.NoInput;

            // Check if pose is stable ie (held for minimum duration)
            long timeSincePoseChange = GetCurrentTimestampMs() - poseState.LastTimestampMs;
            if (timeSincePoseChange < poseStabilityDebounceMs)
                return HitQuality.Early; // Pose changed too recently, likely transitioning

            // Check if correct pose was performed
            if (poseState.CurrentPose != prompt.requiredPose)
                return HitQuality.WrongPose;

            // correct pose, correct pad, sufficient confidence
            // future maybe (real timing logic AudioSettings.dspTime tracking)
            return HitQuality.Perfect;
        }

        private int GetSelectedPad()
        {
            if (selectionState?.Selected.Count == 0)
                return -1;

            var list = new List<int>(selectionState.Selected);
            return list[list.Count - 1];
        }

        private long GetCurrentTimestampMs() 
            => System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
    }
}