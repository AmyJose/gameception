using InputLayer;
using NUnit.Framework;
using Rhythm;
using UnityEngine;
using System.Collections.Generic;

namespace Gameplay
{
    public class BeatActionResolver : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private BeatClock beatClock;
        [SerializeField] private PoseState poseState;
        [SerializeField] private SelectionState selectionState;
        [SerializeField] private ResourceSystem resourceSystem;
        [SerializeField] private ComboSystem comboSystem;

        [Header("Judging")]
        [SerializeField] private float minPoseConfidence = 0.6f;

        private void OnEnable()
        {
            //when the object becomes active, subscribe Handle Beat
            beatClock.OnBeat += HandleBeat;
        }
        private void OnDisable()
        {
            //when disabled, unsubscribe it
            beatClock.OnBeat -= HandleBeat;
        }

        private void HandleBeat(BeatInfo beat)
        {
            if (poseState.Confidence < minPoseConfidence || poseState.CurrentPose == ElementPose.None)
            {
                // miss
                //no valid pose at the beat
                comboSystem.RegisterMiss(beat.beatIndex);
                return;
            }

            //selected planets
            var targets = new List<int>(selectionState.Selected);

            if (targets.Count == 0)
            {
                //miss
                //pose was correct but no planet selected
                comboSystem.RegisterMiss(beat.beatIndex);
                return;
            }

            //apply the element to the selected planets
            resourceSystem.ApplyElementToPlanets(poseState.CurrentPose, targets, beat.beatIndex);

            comboSystem.RegisterHit(poseState.CurrentPose, targets, beat.beatIndex);
        }
    }
}
