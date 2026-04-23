using System;
using System.Collections.Generic;
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

    public class ScoreManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PromptJudge promptJudge;

        [Header("Base Score Values")]
        [SerializeField] private int perfectScore = 100;
        [SerializeField] private int goodScore = 70;
        [SerializeField] private int okayScore = 40;
        [SerializeField] private int missPenalty = 0;

        [Header("Sequence Bonuses")]
        [SerializeField] private int cleanSequenceBonus = 250;
        [SerializeField] private int allPerfectSequenceBonus = 400;

        [Header("Streak Bonuses")]
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
        public event Action<string, int> OnSequenceBonusAwarded;

        private struct SequenceScoreStatus
        {
            public int perfectCount;
            public int goodCount;
            public int okayCount;
            public int missCount;
            public HashSet<int> judgedPromptIds;
        }

        private readonly Dictionary<int, SequenceScoreStatus> _sequenceScores = new();

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

            _sequenceScores.Clear();

            NotifyAll();
        }

        private void HandleJudged(PromptJudge.JudgementResult result)
        {
            if (!RunActive) return;

            TimingJudgement finalJudgement = ConvertToFinalJudgement(result);

            ApplyPromptScore(finalJudgement);
            UpdateSequenceScoreStatus(result.sequenceId, result.promptId, finalJudgement);
        }

        private TimingJudgement ConvertToFinalJudgement(PromptJudge.JudgementResult result)
        {
            switch (result.quality)
            {
                case PromptJudge.HitQuality.Perfect:
                    return result.timing == PromptJudge.PoseTiming.Perfect
                        ? TimingJudgement.Perfect
                        : TimingJudgement.Good;

                case PromptJudge.HitQuality.Good:
                    return result.timing == PromptJudge.PoseTiming.Perfect
                        ? TimingJudgement.Good
                        : TimingJudgement.Okay;

                case PromptJudge.HitQuality.WrongPlanet:
                case PromptJudge.HitQuality.WrongPose:
                case PromptJudge.HitQuality.NoInput:
                default:
                    return TimingJudgement.Miss;
            }
        }

        private void ApplyPromptScore(TimingJudgement judgement)
        {
            if (!RunActive) return;

            if (judgement == TimingJudgement.Miss)
            {
                RegisterMiss();
                return;
            }

            int awardedScore = GetBaseScoreForTiming(judgement);

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

        private void UpdateSequenceScoreStatus(int sequenceId, int promptId, TimingJudgement judgement)
        {
            if (!_sequenceScores.TryGetValue(sequenceId, out var status))
            {
                status = new SequenceScoreStatus
                {
                    perfectCount = 0,
                    goodCount = 0,
                    okayCount = 0,
                    missCount = 0,
                    judgedPromptIds = new HashSet<int>()
                };
            }

            if (status.judgedPromptIds == null)
            {
                status.judgedPromptIds = new HashSet<int>();
            }

            if (status.judgedPromptIds.Contains(promptId))
            {
                return;
            }

            status.judgedPromptIds.Add(promptId);

            switch (judgement)
            {
                case TimingJudgement.Perfect:
                    status.perfectCount++;
                    break;

                case TimingJudgement.Good:
                    status.goodCount++;
                    break;

                case TimingJudgement.Okay:
                    status.okayCount++;
                    break;

                case TimingJudgement.Miss:
                default:
                    status.missCount++;
                    break;
            }

            _sequenceScores[sequenceId] = status;
        }

        private void HandleSequenceComplete(PromptJudge.SequenceResult result)
        {
            if (!RunActive) return;

            SequencesCompleted++;

            if (!_sequenceScores.TryGetValue(result.sequenceId, out var status))
            {
                Debug.LogWarning($"[ScoreManager] No tracked score data for sequence {result.sequenceId}");
                NotifyAll();
                return;
            }

            int totalSuccessfulPrompts = status.perfectCount + status.goodCount + status.okayCount;
            int totalEvaluatedPrompts = totalSuccessfulPrompts + status.missCount;

            bool cleanSequence =
                result.totalPrompts > 0 &&
                status.missCount == 0 &&
                totalSuccessfulPrompts == result.totalPrompts;

            bool allPerfect =
                cleanSequence &&
                status.perfectCount == result.totalPrompts;

            Debug.Log(
                $"[ScoreManager] SequenceComplete | seq={result.sequenceId}, total={result.totalPrompts}, " +
                $"perfect={status.perfectCount}, good={status.goodCount}, okay={status.okayCount}, misses={status.missCount}, " +
                $"tracked={totalEvaluatedPrompts}"
            );

            if (allPerfect)
            {
                CurrentScore += allPerfectSequenceBonus;
                OnSequenceBonusAwarded?.Invoke("ALL PERFECT!", allPerfectSequenceBonus);
            }
            else if (cleanSequence)
            {
                CurrentScore += cleanSequenceBonus;
                OnSequenceBonusAwarded?.Invoke("CLEAN SEQUENCE!", cleanSequenceBonus);
            }

            _sequenceScores.Remove(result.sequenceId);

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