using System;
using InputLayer;
using UnityEngine;

namespace Gameplay.Choreography
{
    //Listens to PromptQueue.OnPromptEnteredZone and evaluate pose immediately, emitting a result
    
    public class PromptJudge : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PoseState poseState;
        [SerializeField] private SelectionState selectionState;
        [SerializeField] private PromptQueue promptQueue;

        [Header("Thresholds")]
        [SerializeField] private float minPoseConfidence = 0.7f;
        [SerializeField] private long stabilityMs = 200;

        public event Action<JudgementResult> OnJudged;

        public struct JudgementResult
        {
            public int promptId;
            public ElementPose detectedPose;
            public HitQuality quality;
            public int selectedPad;
        }

        public enum HitQuality { Perfect, Good, WrongPose, NoInput }

        private long _lastJudgeTime = 0;

        private void OnEnable()
        {
            if (promptQueue != null)
                promptQueue.OnPromptEnteredZone += HandlePromptInZone;
        }

        private void OnDisable()
        {
            if (promptQueue != null)
                promptQueue.OnPromptEnteredZone -= HandlePromptInZone;
        }

        private void HandlePromptInZone(PromptQueue.PromptData data)
        {
            long now = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            if (now - _lastJudgeTime < stabilityMs)
                return;
            _lastJudgeTime = now;

            var result = new JudgementResult
            {
                promptId = data.id,
                detectedPose = poseState?.CurrentPose ?? ElementPose.None,
                selectedPad = GetSelectedPad(),
                quality = Evaluate(data.requiredPose)
            };

            Debug.Log($"[PromptJudge] Prompt {data.id}: {result.quality} " +
                     $"(Pose: {result.detectedPose}, Pad: {result.selectedPad})");

            OnJudged?.Invoke(result);
        }

        private HitQuality Evaluate(ElementPose required)
        {
            // if (selectionState?.Selected.Count == 0)
            //     return HitQuality.NoInput;

            // if (poseState.Confidence < minPoseConfidence)
            //     return HitQuality.NoInput;

            if (poseState.CurrentPose != required)
                return HitQuality.WrongPose;

            return HitQuality.Perfect;
        }

        private int GetSelectedPad()
        {
            if (selectionState?.Selected.Count == 0) return -1;
            var list = new System.Collections.Generic.List<int>(selectionState.Selected);
            return list[list.Count - 1];
        }
    }
}