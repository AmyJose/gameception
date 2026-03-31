using System;
using UnityEngine;
using Gameplay.Choreography;

namespace Gameplay
{
    public enum TimingJudgement
    {
        Miss,
        Okay,
        Good,
        Perfect
    }

    public enum PoseConfidence
    {
        Low,
        Medium,
        High
    }

    // NOTE: currently doesnt care about lanes and that.
    // also doesnt care about population etc. just only the prompt Judge
    public class ScoreManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PromptJudge promptJudge;

        [Header("Base Score Values")]
        [SerializeField] private int perfectScore = 100;
        [SerializeField] private int goodScore = 70;
        [SerializeField] private int okayScore = 40;
        [SerializeField] private int missPenalty = 0;

        [Header("Bonuses")]
        [SerializeField] private int sequenceCompleteBonus = 250;
        [SerializeField] private bool useStreakBonuses = true;
        [SerializeField] private int streakBonusEvery = 5;
        [SerializeField] private int streakBonusAmount = 50;

        public int CurrentScore { get; private set; }
        public int PromptsHit { get; private set; }
        public int PromptsMissed { get; private set; }
        public int CurrentStreak { get; private set; }
        public int LongestStreak { get; private set; }
        public int SequencesCompleted { get; private set; }
        public bool RunActive { get; private set; }

        public int TotalJudged => PromptsHit + PromptsMissed;

        public event Action<int> OnScoreChanged;
        public event Action<int> OnStreakChanged;
        public event Action OnStatsChanged;

        private void OnEnable()
        {
            if (promptJudge != null)
            {
                promptJudge.OnJudged += HandleJudged;
                promptJudge.OnSequenceComplete += HandleSequenceComplete;
            }
        }

        private void OnDisable()
        {
            if (promptJudge != null)
            {
                promptJudge.OnJudged -= HandleJudged;
                promptJudge.OnSequenceComplete -= HandleSequenceComplete;
            }
        }

        public void BeginRun()
        {
            ResetStats();
            RunActive = true;
        }

        public void EndRun()
        {
            RunActive = false;
        }

        public void ResetStats()
        {
            CurrentScore = 0;
            PromptsHit = 0;
            PromptsMissed = 0;
            CurrentStreak = 0;
            LongestStreak = 0;
            SequencesCompleted = 0;

            NotifyAll();
        }

        private void HandleJudged(PromptJudge.JudgementResult result)
        {
            if (!RunActive) return;

            switch (result.quality)
            {
                case PromptJudge.HitQuality.Perfect:
                    RegisterHit(TimingJudgement.Perfect);
                    break;

                case PromptJudge.HitQuality.Good:
                    RegisterHit(TimingJudgement.Good);
                    break;

                case PromptJudge.HitQuality.WrongPose:
                case PromptJudge.HitQuality.NoInput:
                    RegisterMiss();
                    break;

                default:
                    RegisterMiss();
                    break;
            }
        }

        private void HandleSequenceComplete(PromptJudge.SequenceResult result)
        {
            if (!RunActive) return;

            RegisterSequenceCompleted();
        }

        public void RegisterHit(TimingJudgement timing)
        {
            if (!RunActive) return;

            int awardedScore = GetBaseScoreForTiming(timing);

            if (timing == TimingJudgement.Miss)
            {
                RegisterMiss();
                return;
            }

            CurrentScore += awardedScore;
            PromptsHit++;
            CurrentStreak++;

            if (CurrentStreak > LongestStreak)
            {
                LongestStreak = CurrentStreak;
            }

            if (useStreakBonuses && streakBonusEvery > 0 && CurrentStreak % streakBonusEvery == 0)
            {
                CurrentScore += streakBonusAmount;
            }

            NotifyAll();
        }

        public void RegisterMiss()
        {
            if (!RunActive) return;

            PromptsMissed++;
            CurrentStreak = 0;

            if (missPenalty != 0)
            {
                CurrentScore += missPenalty;
                CurrentScore = Mathf.Max(0, CurrentScore);
            }

            NotifyAll();
        }

        public void RegisterSequenceCompleted()
        {
            if (!RunActive) return;

            SequencesCompleted++;
            CurrentScore += sequenceCompleteBonus;

            NotifyAll();
        }

        public float GetAccuracy()
        {
            int total = TotalJudged;
            if (total <= 0) return 0f;

            return (float)PromptsHit / total;
        }

        private int GetBaseScoreForTiming(TimingJudgement timing)
        {
            switch (timing)
            {
                case TimingJudgement.Perfect:
                    return perfectScore;

                case TimingJudgement.Good:
                    return goodScore;

                case TimingJudgement.Okay:
                    return okayScore;

                case TimingJudgement.Miss:
                default:
                    return 0;
            }
        }

        private void NotifyAll()
        {
            OnScoreChanged?.Invoke(CurrentScore);
            OnStreakChanged?.Invoke(CurrentStreak);
            OnStatsChanged?.Invoke();
        }
    }
}