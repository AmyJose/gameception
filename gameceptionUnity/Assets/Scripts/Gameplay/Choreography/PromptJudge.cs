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
        [SerializeField] private float perfectWindow = 0.5f;


        public event Action<JudgementResult> OnJudged;
        public event Action<SequenceResult> OnSequenceComplete;
        public event Action<PromptQueue.PromptData> OnPromptUpdated;
        private Dictionary<int, ActivePrompt> _activePrompts = new();
        private readonly Dictionary<int, SequenceStatus> _sequenceProgress = new();

        private class ActivePrompt
        {
            public int promptId;
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

        public enum HitQuality { Perfect, Good, WrongPose, WrongPlanet, NoInput }
        public enum PoseTiming { Early, Perfect, Late }

        private struct SequenceStatus
        {
            public int sequenceId;
            public int totalPrompts;
            public int hitsCount;
            public int missesCount;
            public HashSet<int> evaluatedPrompts;
        }

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
            if (promptQueue == null || poseState == null)
                return;

            foreach (var kvp in _activePrompts)
            {
                int id = kvp.Key;
                var prompt = kvp.Value;

                float currentY = promptQueue.GetPromptCurrentY(id);
                float offset = currentY - promptQueue.hitZoneY;
                float absOffset = Mathf.Abs(offset);

                bool correctPlanetSelected = 
                    selectionState != null 
                    && selectionState.IsSelected(prompt.laneIndex);

                bool poseMatches = poseState.CurrentPose == prompt.requiredPose;
                bool hasEnoughConfidence = poseState.Confidence >= minPoseConfidence;
                bool insideHitWindow = absOffset < perfectWindow;

                if (!prompt.success &&
                    correctPlanetSelected &&
                    poseMatches &&
                    hasEnoughConfidence &&
                    insideHitWindow)
                {
                    prompt.success = true;
                    EmitSuccess(id, prompt, offset);
                }

            }
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
            OnPromptUpdated?.Invoke(data);
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
                missesCount = 0,
                evaluatedPrompts = new HashSet<int>()
            };

            //Debug.Log($"[PromptJudge] Registered sequence {sequenceId} with {totalPrompts} prompts");
        }

        private void EmitSuccess(int promptId, ActivePrompt prompt, float offset)
        {
            var result = new JudgementResult
            {
                promptId = promptId,
                sequenceId = prompt.sequenceId,
                laneIndex = prompt.laneIndex,
                detectedPose = poseState != null ? poseState.CurrentPose : ElementPose.None,
                selectedPad = GetSelectedPad(),
                quality = EvaluateConfidence(
                    prompt.requiredPose,
                    poseState != null ? poseState.CurrentPose : ElementPose.None,
                    minPoseConfidence,
                    maxPoseConfidence,
                    poseState != null ? poseState.Confidence : 0f
                ),
                timing = EvaluateTimingFromOffset(offset)
            };

            //Debug.Log($"Prompt {promptId} {result.quality} HIT");

            OnJudged?.Invoke(result);
            UpdateSequenceProgress(promptId, prompt.sequenceId, result.quality);

            if (hitZone != null)
            {
                hitZone.TriggerFeedback(true);
            }
        }

        private void EmitFailure(int promptId, ActivePrompt prompt)
        {
            HitQuality failureQuality = EvaluateFailureReason(prompt);

            var result = new JudgementResult
            {
                promptId = promptId,
                sequenceId = prompt.sequenceId,
                laneIndex = prompt.laneIndex,
                detectedPose = poseState != null ? poseState.CurrentPose : ElementPose.None,
                selectedPad = GetSelectedPad(),
                quality = failureQuality,
                timing = PoseTiming.Late
            };

            Debug.Log(
                $"[PromptJudge - MISS] Seq {result.sequenceId} | Prompt {promptId}: {result.quality} | " +
                $"Pose: {result.detectedPose} | SelectedPad: {result.selectedPad} | ExpectedLane: {result.laneIndex}"
            );

            OnJudged?.Invoke(result);
            UpdateSequenceProgress(promptId, prompt.sequenceId, result.quality);

            if (hitZone != null)
            {
                hitZone.TriggerFeedback(false);
            }
        }

        private PoseTiming EvaluateTimingFromOffset(float offset)
        {
            if (Mathf.Abs(offset) <= perfectWindow * 0.5f)
                return PoseTiming.Perfect;

            return offset > 0 ? PoseTiming.Early : PoseTiming.Late;
        }
        private HitQuality EvaluateFailureReason(ActivePrompt prompt)
        {
            bool hasAnySelection =
                selectionState != null &&
                selectionState.Selected != null &&
                selectionState.Selected.Count > 0;

            bool correctPlanetSelected =
                selectionState != null &&
                selectionState.IsSelected(prompt.laneIndex);

            bool poseMatches =
                poseState != null &&
                poseState.CurrentPose == prompt.requiredPose;

            bool hasEnoughConfidence =
                poseState != null &&
                poseState.Confidence >= minPoseConfidence;

            // No selected planet, or wrong one selected
            if (!hasAnySelection || !correctPlanetSelected)
                return HitQuality.WrongPlanet;

            // Right planet, wrong pose
            if (!poseMatches)
                return HitQuality.WrongPose;

            // Right planet and right pose, but confidence too low
            if (!hasEnoughConfidence)
                return HitQuality.NoInput;

            return HitQuality.NoInput;
        }

        //Updates progress when a prompt is judged
        private void UpdateSequenceProgress(int promptId, int sequenceId, HitQuality quality)
        {
            if (!_sequenceProgress.TryGetValue(sequenceId, out var status))
            {
                Debug.LogWarning($"[PromptJudge] Sequence {sequenceId} not registered!");
                return;
            }

            if (status.evaluatedPrompts == null)
            {
                status.evaluatedPrompts = new HashSet<int>();
            }

            if (status.evaluatedPrompts.Contains(promptId))
            {
                return;
            }

            status.evaluatedPrompts.Add(promptId);

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

            Debug.Log(
                $"[PromptJudge] Sequence {sequenceId} progress: {totalEvaluated}/{status.totalPrompts} " +
                $"(Hits: {status.hitsCount}, Misses: {status.missesCount})"
            );

            if (totalEvaluated >= status.totalPrompts)
            {
                float accuracy = status.totalPrompts > 0
                    ? (float)status.hitsCount / status.totalPrompts
                    : 0f;

                var seqResult = new SequenceResult
                {
                    sequenceId = sequenceId,
                    totalPrompts = status.totalPrompts,
                    hitsCount = status.hitsCount,
                    missesCount = status.missesCount,
                    accuracy = accuracy
                };

                Debug.Log(
                    $"[PromptJudge] Sequence {sequenceId} COMPLETE! " +
                    $"Hits: {status.hitsCount}, Misses: {status.missesCount}, Accuracy: {accuracy:P2}"
                );

                OnSequenceComplete?.Invoke(seqResult);
                _sequenceProgress.Remove(sequenceId);
            }
        }

        private HitQuality EvaluateConfidence(ElementPose required, ElementPose current, float minConfidence, float maxConfidence, float confidence)
        {
            if (current != required)
                return HitQuality.WrongPose;

            if (confidence >= maxConfidence)
                return HitQuality.Perfect;

            if (confidence >= minConfidence)
                return HitQuality.Good;

            return HitQuality.NoInput;

        }
        private int GetSelectedPad()
        {
            return selectionState != null ? selectionState.GetSingleSelected() : -1;
        }
    }
}