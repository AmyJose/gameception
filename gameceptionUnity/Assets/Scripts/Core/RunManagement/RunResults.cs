using System;

namespace Gameplay
{
    [Serializable]
    public class RunResults
    {
        public int finalScore;
        public int promptsHit;
        public int promptsMissed;
        public int longestStreak;
        public int sequencesCompleted;
        public float accuracy;
        public float runDuration;
    }
}
