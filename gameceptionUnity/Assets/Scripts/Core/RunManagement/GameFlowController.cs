using Gameplay.Choreography;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
/*
 * holds states of gameplay
 * should...
 *  - start the run
 *  - start/end timer (length of game)
 *  - listen for end condition
 *  - freeze/stop gameplay when run ends
 *  - pass results to results scene/UI
 */

namespace Gameplay
{
    //current state of the play thru
    public enum GameState
    {
        WaitingToStart,
        Playing,
        Ending,
        Results
    }
    public class GameFlowController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PromptQueue promptQueue;
        [SerializeField] private ScoreManager scoreManager;

        [Header("Run Settings")]
        [SerializeField] private bool autoStartRun = true;
        [SerializeField] private float runDurationSeconds = 300f;
        [SerializeField] private float startDelaySeconds = 1.5f;

        [Header("Scene Flow")]
        [SerializeField] private string resultsSceneName = "ResultsScene";

        public GameState CurrentState { get; private set; } = GameState.WaitingToStart;
        public float RemainingTime { get; private set; }
        public float RunDurationSeconds => runDurationSeconds;
        public bool IsRunActive => CurrentState == GameState.Playing;

        public event Action<GameState> OnStateChanged;
        public event Action<float> OnTimerUpdated;
        public event Action<RunResults> OnRunEnded;

        private bool _runStarted = false;
        private bool _runEnded = false;

        private void Awake()
        {
            RemainingTime = runDurationSeconds;
            ChangeState(GameState.WaitingToStart);
        }
        private void Start()
        {
            if (autoStartRun) StartCoroutine(BeginRunAfterDelay());
        }
        private void Update()
        {
            if (!IsRunActive) return;

            RemainingTime -= Time.deltaTime;
            if (RemainingTime < 0f) RemainingTime = 0f;

            OnTimerUpdated?.Invoke(RemainingTime);

            if (RemainingTime <= 0f)
            {
                EndRun();
            }
        }
        private IEnumerator BeginRunAfterDelay()
        {
            if (startDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(startDelaySeconds);
            }
            StartRun();
        }

        public void StartRun()
        {
            if (_runStarted || _runEnded) return;

            _runStarted = true;
            RemainingTime = runDurationSeconds;

            if (scoreManager != null) scoreManager.BeginRun();
            if (promptQueue != null)
            {
                promptQueue.BeginGeneration();
            }

            ChangeState(GameState.Playing);
        }
        public void EndRun()
        {
            if (_runEnded) return;
            _runEnded = true;
            ChangeState(GameState.Ending);

            if (promptQueue != null)
            {
                promptQueue.StopGeneration();
                promptQueue.ClearAll();
            }

            if (scoreManager != null) scoreManager.EndRun();

            RunResults results = BuildResults();
            RunResultsStore.LastResults = results;

            OnRunEnded?.Invoke(results);

            ChangeState(GameState.Results);
            SceneManager.LoadScene(resultsSceneName);
        }
        private RunResults BuildResults()
        {
            if (scoreManager == null)
            {
                return new RunResults
                {
                    finalScore = 0,
                    promptsHit = 0,
                    promptsMissed = 0,
                    longestStreak = 0,
                    sequencesCompleted = 0,
                    accuracy = 0f,
                    runDuration = runDurationSeconds
                };
            }
            return new RunResults
            {
                finalScore = scoreManager.CurrentScore,
                promptsHit = scoreManager.PromptsHit,
                promptsMissed = scoreManager.PromptsMissed,
                longestStreak = scoreManager.LongestStreak,
                sequencesCompleted = scoreManager.SequencesCompleted,
                accuracy = scoreManager.GetAccuracy(),
                runDuration = runDurationSeconds
            };
        }
        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            OnStateChanged?.Invoke(CurrentState);
        }
    }
}