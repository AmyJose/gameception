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
        [SerializeField] private SyncHitZone hitZone; 

        [Header("Thresholds")]
        [SerializeField] private float minPoseConfidence = 0.85f;
        [SerializeField] private float maxPoseConfidence = 0.86f;
        [SerializeField] private long stabilityMs = 200;
        [SerializeField] private float perfectWindow = 0.45f;


        public event Action<JudgementResult> OnJudged;
        public event Action<SequenceResult> OnSequenceComplete;
        public event Action<PromptQueue.PromptData> OnPromptUpdated;
        private Dictionary<int, ActivePrompt> _activePrompts = new();

        private class ActivePrompt
        {
            public int sequenceId;
            public int laneIndex;
            public ElementPose requiredPose;
            public bool success;
            public float bestOffset;
        }

        public struct JudgementResult
        {
            public int promptId;
            public int sequenceId;
            public int laneIndex;
            public ElementPose detectedPose;
            public HitQuality quality;
            public PoseTiming timing;
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
        public enum PoseTiming { Early, Perfect, Late }

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
            {
                promptQueue.OnPromptEnteredZone += HandlePromptEnter;
                promptQueue.OnPromptExitedZone += HandlePromptExit;
            }
        }

        private void OnDisable()
        {
            if (promptQueue != null)
            {
                promptQueue.OnPromptEnteredZone -= HandlePromptEnter;
                promptQueue.OnPromptExitedZone -= HandlePromptExit;
            }
        }
        private void Update()
        {
            foreach (var kvp in _activePrompts)
            {
                int id = kvp.Key;
                var prompt = kvp.Value;

                float currentY = promptQueue != null ? promptQueue.GetPromptCurrentY(id) : -999f;
                float offset = currentY - promptQueue.hitZoneY;
                float abs = Mathf.Abs(offset);

                // Track best timing
                // if (abs < Mathf.Abs(prompt.bestOffset))
                //     prompt.bestOffset = offset;

                // Check pose
                if (!prompt.success &&
                    poseState.CurrentPose == prompt.requiredPose &&
                    poseState.Confidence >= minPoseConfidence &&
                    abs < perfectWindow)
                {
                    prompt.success = true;
                    EmitSuccess(id, prompt, offset);
                }
                else if (!prompt.success && offset > perfectWindow) // Missed the window
                {
                    EmitFailure(id, prompt);
                    prompt.success = true; // Mark as evaluated to prevent multiple failure emissions
                }
            }
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
                laneIndex = data.laneIndex,
                detectedPose = poseState?.CurrentPose ?? ElementPose.None,
                selectedPad = GetSelectedPad(),
                quality = EvaluateConfidence(data.requiredPose, poseState.CurrentPose, minPoseConfidence, maxPoseConfidence, poseState.Confidence),
                timing = EvaluateTiming(data.currentY, promptQueue.hitZoneY)
            };

            Debug.Log($"[PromptJudge- seq {data.sequenceId}:] Prompt {data.id}: {result.quality} " +
                     $"(Pose: {result.detectedPose}, Quality: {result.quality}, Timing: {result.timing}, Pad: {result.selectedPad})");

            OnJudged?.Invoke(result);

            UpdateSequenceProgress(data.sequenceId, result.quality);
        }

        //helper functions for continuous window based monitoring
        private void HandlePromptEnter(PromptQueue.PromptData data)
        {
            _activePrompts[data.id] = new ActivePrompt
            {
                sequenceId = data.sequenceId,
                laneIndex = data.laneIndex,
                requiredPose = data.requiredPose,
                success = false,
                bestOffset = float.MaxValue
            };
        }

        private void HandlePromptExit(PromptQueue.PromptData data)
        {
            if (!_activePrompts.TryGetValue(data.id, out var prompt))
                return;

            if (!prompt.success)
            {
                EmitFailure(data.id, prompt);
            }

            _activePrompts.Remove(data.id);
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

        private void EmitSuccess(int id, ActivePrompt prompt, float offset)
        {
            var result = new JudgementResult
            {
                promptId = id,
                sequenceId = prompt.sequenceId,
                laneIndex = prompt.laneIndex,
                detectedPose = poseState.CurrentPose,
                selectedPad = GetSelectedPad(),
                quality = EvaluateConfidence(
                    prompt.requiredPose,
                    poseState.CurrentPose,
                    minPoseConfidence,
                    maxPoseConfidence,
                    poseState.Confidence
                ),
                timing = EvaluateTimingFromOffset(offset)
            };

            Debug.Log($"[PromptJudge - HIT] Seq {result.sequenceId} | Prompt {id}: {result.quality} | " +
              $"Timing: {result.timing} (Offset: {offset:F2}) | Pose: {result.detectedPose}| Confidence: {poseState.Confidence:F2}");
            OnJudged?.Invoke(result);
            UpdateSequenceProgress(prompt.sequenceId, result.quality);

            float distanceToCenter = Mathf.Abs(offset);
            if (hitZone != null){
            // {
            //     bool isPerfect = Mathf.Abs(offset) <= perfectWindow * 0.5f;
            //     hitZone.TriggerFeedback(true); //green flash
            // }
            if (distanceToCenter <= 0.1f)
                {
                    hitZone.TriggerFeedback(true);
                }
            }
        }

        private void EmitFailure(int id, ActivePrompt prompt)
        {
            var result = new JudgementResult
            {
                promptId = id,
                sequenceId = prompt.sequenceId,
                laneIndex = prompt.laneIndex,
                detectedPose = poseState.CurrentPose,
                selectedPad = GetSelectedPad(),
                quality = HitQuality.NoInput,
                timing = PoseTiming.Late
            };
            Debug.Log($"[PromptJudge - MISS] Seq {result.sequenceId} | Prompt {id}: {result.quality} | " +
              $"Pose: {result.detectedPose}");
            OnJudged?.Invoke(result);
            UpdateSequenceProgress(prompt.sequenceId, result.quality);

            if (hitZone != null)
            {
                hitZone.TriggerFeedback(false); //red flash
            }
        }

        private PoseTiming EvaluateTimingFromOffset(float offset)
        {
            if (Mathf.Abs(offset) <= perfectWindow * 0.5f)
                return PoseTiming.Perfect;

            return offset > 0 ? PoseTiming.Early : PoseTiming.Late;
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

        private HitQuality EvaluateConfidence(ElementPose required, ElementPose current, float minConfidence, float maxConfidence, float confidence)
        {
            if (current != required)
                return HitQuality.WrongPose;
            //evaluate teh current confidence against thresholds to determine hit quality
            bool isHighConfidence = confidence >= maxConfidence;
            bool isMediumConfidence = confidence >= minConfidence;
            if (isHighConfidence)
                return HitQuality.Perfect;
            else if (isMediumConfidence)
                return HitQuality.Good;
            else
                return HitQuality.NoInput;

        }

        private PoseTiming EvaluateTiming(float promptY, float hitZoneY)
        {
            float offset = promptY - hitZoneY;
            if (Mathf.Abs(offset) <= perfectWindow * 0.5f)
                return PoseTiming.Perfect;

            return offset > 0 ? PoseTiming.Early : PoseTiming.Late;
        }
        private int GetSelectedPad()
        {
            if (selectionState?.Selected.Count == 0) return -1;
            var list = new System.Collections.Generic.List<int>(selectionState.Selected);
            return list[list.Count - 1];
        }
    }
}