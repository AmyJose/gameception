using System.Collections.Generic;
using UnityEngine;
using Rhythm;
using InputLayer;

namespace Gameplay.Rhythm
{
    public class ChoreographyQueue : MonoBehaviour
    {
        [SerializeField] private BeatClock beatClock;
        [SerializeField] private int beatsBetweenSequences = 60; 
        
        public List<ChoreographyAction> UpcomingActions = new();
        private int lastSequenceBeat = 0;

        private void OnEnable() => beatClock.OnBeat += HandleBeat;
        private void OnDisable() => beatClock.OnBeat -= HandleBeat;

        private void HandleBeat(BeatInfo beatInfo)
        {
            // Trigger a new sequence every x beats
            if (beatInfo.beatIndex >= lastSequenceBeat + beatsBetweenSequences)
            {
                GenerateSequence(beatInfo.beatIndex + 8, beatInfo.dspSongTime + (beatClock.BeatInterval * 8));
                lastSequenceBeat = beatInfo.beatIndex;
            }
        }

        private void GenerateSequence(int startBeat, double startDspTime)
        {
            double interval = beatClock.BeatInterval;
            
            UpcomingActions.Add(new ChoreographyAction { targetBeat = startBeat, targetDspTime = startDspTime, requiredPose = ElementPose.Water, requiredPad = 0 });
            UpcomingActions.Add(new ChoreographyAction { targetBeat = startBeat + 1, targetDspTime = startDspTime + interval, requiredPose = ElementPose.Earth, requiredPad = 0 });
            UpcomingActions.Add(new ChoreographyAction { targetBeat = startBeat + 2, targetDspTime = startDspTime + (interval * 2), requiredPose = ElementPose.Fire, requiredPad = 0 });
            
            Debug.Log($"[Choreography] Generated sequence starting at beat {startBeat}");
        }

        public ChoreographyAction GetNextUnresolvedAction()
        {
            return UpcomingActions.Find(a => !a.isResolved);
        }
    }
}