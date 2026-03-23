using UnityEngine;
using InputLayer;

namespace Gameplay.Rhythm
{
    [System.Serializable]
    public class ChoreographyAction
    {
        public int targetBeat;
        public double targetDspTime;
        public ElementPose requiredPose;
        public int requiredPad;
        public bool isResolved;
    }

    public enum HitQuality { Perfect, Good, Early, Late, WrongPose, Miss }
}
