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

        [SerializeField] private List<Planet> planetObjects;

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
            if (selectionState == null) return;
            //selected targets
            var targets = new List<int>(selectionState.Selected);
            if (targets.Count > 0)
            {
               Debug.Log("planets currently selected: " + targets[0]);
            }
            else
            {
                Debug.Log("no planets currently selected");
            }

            int activeIndex = -1;
            if (targets.Count > 0)            {
                activeIndex = targets[targets.Count - 1];
            }
            if (activeIndex != -1 && poseState.Confidence >= minPoseConfidence && poseState.CurrentPose != ElementPose.None)
            {
                //Apply the element and register the hit with the combo system. 
                var singleTarget = new List<int> {activeIndex};
                resourceSystem.ApplyElementToPlanets(poseState.CurrentPose, singleTarget, beat.beatIndex);
                comboSystem.RegisterHit(poseState.CurrentPose, singleTarget, beat.beatIndex);
            }
        }
    }
}
