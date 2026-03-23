using UnityEngine;
using InputLayer;
using Rhythm;
using UnityEngine;

namespace Gameplay.Rhythm
{
    public class ChoreographyJudge : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ChoreographyQueue queue;
        [SerializeField] private PoseState poseState;
        [SerializeField] private SelectionState selectionState;
        [SerializeField] private ResourceSystem resourceSystem;
        [SerializeField] private ComboSystem comboSystem;
        [SerializeField] private BeatClock beatClock;

        [Header("Judgement Windows (in seconds)")]
        [SerializeField] private double perfectWindow = 0.15;
        [SerializeField] private double goodWindow = 0.3;
        [SerializeField] private double missWindow = 0.45;
        
        [Header("Pose Criteria")]
        [SerializeField] private float minPoseConfidence = 0.6f;
        [SerializeField] private long poseStabilityDebounceMs = 200;

        private void Update()
        {
            var nextAction = queue.GetNextUnresolvedAction();
            if (nextAction == null) return;

            double currentDspTime = AudioSettings.dspTime;
            double timeDifference = currentDspTime - nextAction.targetDspTime;

    
            if (timeDifference > missWindow)
            {
                ResolveAction(nextAction, HitQuality.Miss);
                return;
            }

            if (Mathf.Abs((float)timeDifference) <= goodWindow)
            {
                EvaluatePlayerInput(nextAction, timeDifference);
            }
        }

        private void EvaluatePlayerInput(ChoreographyAction action, double timeDifference)
        {
            // Verify Pad
            if (!selectionState.IsSelected(action.requiredPad)) return;

            // Verify Pose
            if (poseState.CurrentPose == ElementPose.None || poseState.Confidence < minPoseConfidence) return;

            // Ensure the pose was held for a brief moment
            long timeSincePoseChange = GetCurrentTimestampMillisec() - poseState.LastTimestampMs;
            if (timeSincePoseChange < poseStabilityDebounceMs) return;

            if (poseState.CurrentPose != action.requiredPose)
            {
                // wrong pose on input
                ResolveAction(action, HitQuality.WrongPose);
                return;
            }

            // Calculate accuracy
            HitQuality quality;
            if (Mathf.Abs((float)timeDifference) <= perfectWindow)
                quality = HitQuality.Perfect;
            else if (timeDifference < 0)
                quality = HitQuality.Early;
            else
                quality = HitQuality.Late;

            ResolveAction(action, quality);
        }

        private void ResolveAction(ChoreographyAction action, HitQuality quality)
        {
            action.isResolved = true;

            Debug.Log($"[Judge] Action Resolved: {quality} on Beat {action.targetBeat}");

            if (quality == HitQuality.Perfect || quality == HitQuality.Early || quality == HitQuality.Late)
            {
                // Successful: Apply elements
                var targetList = new System.Collections.Generic.List<int> { action.requiredPad };
                resourceSystem.ApplyElementToPlanets(action.requiredPose, targetList, action.targetBeat);
                comboSystem.RegisterHit(action.requiredPose, targetList, action.targetBeat);
            }
            else
            {
                // Fail
                comboSystem.RegisterMiss(action.targetBeat);
            }
        }

        private long GetCurrentTimestampMillisec() => System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
    }
}