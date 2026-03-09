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
        [SerializeField] private DanceMatInputProvider matInput;
        [SerializeField] private ResourceSystem resourceSystem;
        [SerializeField] private ComboSystem comboSystem;

        [Header("Judging")]
        [SerializeField] private float minPoseConfidence = 0.6f;

        private void OnEnable()
        {
            beatClock.OnBeat += HandleBeat;
        }
        private void OnDisable()
        {
            beatClock.OnBeat -= HandleBeat;
        }

        private void HandleBeat(BeatInfo beat)
        {
            Debug.Log("BeatActionResolver: HandleBeat");
            if (poseState.Confidence < minPoseConfidence || poseState.CurrentPose == ElementPose.None)
            {
                // miss
                //no valid pose at the beat
                comboSystem.RegisterMiss(beat.beatIndex);
                Debug.Log("BeatActionResolver: no valid pose at the beat");
                return;
            }

            //selected planets
            var targets = new List<int>(matInput.Selected);

            if (targets.Count == 0)
            {
                //miss
                //pose was correct but no planet selected
                comboSystem.RegisterMiss(beat.beatIndex);
                Debug.Log("BeatActionResolver: valid pose, no planet selected");
                return;
            }

            //apply the element to the selected planets
            Debug.Log("BeatActionResolver: about to apply element to planet");
            resourceSystem.ApplyElementToPlanets(poseState.CurrentPose, targets, beat.beatIndex);

            Debug.Log("BeatActionResolver: about to register hit with combo systems");
            comboSystem.RegisterHit(poseState.CurrentPose, targets, beat.beatIndex);
        }
    }
}
