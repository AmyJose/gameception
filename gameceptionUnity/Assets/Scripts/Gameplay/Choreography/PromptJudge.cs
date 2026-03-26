using System;
using System.Collections.Generic;
using InputLayer;
using UnityEngine;

namespace Gameplay.Choreography
{
    // Listens to PromptQueue.OnPromptEnteredZone and evaluate pose immediately, emitting a result, and tracks sequence completion

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
        public event Action<SequenceResult> OnSequenceComplete;

        public struct JudgementResult
        {
            public int promptId;
            public int sequenceId;
            public ElementPose detectedPose;
            public HitQuality quality;
            public int selectedPad;
        }

        public struct SequenceResult
        {
            public int sequenceId;
            public int totalPrompts;
            public int hitsCount;
            public int missesCount;
            public float accuracy;
        }

        public enum HitQuality { Perfect, Good, WrongPose, NoInput }

        private struct SequenceStatus
        {
            public int sequenceId;
            public int totalPrompts;
            public int hitsCount;
            public int missesCount;
            public HashSet<int> evaluatedPrompts;
        }

        private Dictionary<int, SequenceStatus> _sequenceProgress = new();

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
                sequenceId = data.sequenceId,
                detectedPose = poseState?.CurrentPose ?? ElementPose.None,
                selectedPad = GetSelectedPad(),
                quality = Evaluate(data.requiredPose)
            };

            Debug.Log($"[PromptJudge- seq {data.sequenceId}:] Prompt {data.id}: {result.quality} " +
                     $"(Pose: {result.detectedPose}, Pad: {result.selectedPad})");

            OnJudged?.Invoke(result);

            UpdateSequenceProgress(data.sequenceId, result.quality);
        }

        // Called by PromptQueue when a sequence is generated
        public void RegisterSequence(int sequenceId, int totalPrompts)
        {
            _sequenceProgress[sequenceId] = new SequenceStatus
            {
                sequenceId = sequenceId,
                totalPrompts = totalPrompts,
                hitsCount = 0,
                missesCount = 0,
                evaluatedPrompts = new HashSet<int>()
            };

            Debug.Log($"[PromptJudge] Registered sequence {sequenceId} with {totalPrompts} prompts");
        }

        //Updates progress when a prompt is judged
        private void UpdateSequenceProgress(int sequenceId, HitQuality quality)
        {
            if (!_sequenceProgress.TryGetValue(sequenceId, out var status))
            {
                Debug.LogWarning($"[PromptJudge] Sequence {sequenceId} not registered!");
                return;
            }

            // Count hit/miss
            if (quality == HitQuality.Perfect || quality == HitQuality.Good)
            {
                status.hitsCount++;
            }
            else
            {
                status.missesCount++;
            }

            _sequenceProgress[sequenceId] = status;

            int totalEvaluated = status.hitsCount + status.missesCount;
            Debug.Log($"[PromptJudge] Sequence {sequenceId} progress: {totalEvaluated}/{status.totalPrompts} " +
                     $"(Hits: {status.hitsCount}, Misses: {status.missesCount})");

            // Check if sequence complete
            if (totalEvaluated >= status.totalPrompts)
            {
                float accuracy = (float)status.hitsCount / status.totalPrompts;

                var seqResult = new SequenceResult
                {
                    sequenceId = sequenceId,
                    totalPrompts = status.totalPrompts,
                    hitsCount = status.hitsCount,
                    missesCount = status.missesCount,
                    accuracy = accuracy
                };

                Debug.Log($"✅ [PromptJudge] Sequence {sequenceId} COMPLETE! " +
                         $"Hits: {status.hitsCount}/{status.totalPrompts}, Accuracy: {accuracy:P}");

                OnSequenceComplete?.Invoke(seqResult);
            }
        }

        private HitQuality Evaluate(ElementPose required)
        {
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