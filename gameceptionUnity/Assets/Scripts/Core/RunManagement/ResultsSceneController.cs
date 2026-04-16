using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay
{
    public class ResultsSceneController : MonoBehaviour
    {
        [Header("Run Stats UI")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text accuracyText;
        [SerializeField] private TMP_Text hitsText;
        [SerializeField] private TMP_Text missesText;
        [SerializeField] private TMP_Text streakText;
        [SerializeField] private TMP_Text sequencesText;

        [Header("Leaderboard UI")]
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private TMP_Text leaderboardText;
        [SerializeField] private TMP_Text submitStatusText;

        [Header("Scene Names")]
        [SerializeField] private string mainMenuSceneName = "Start";
        [SerializeField] private string playSceneName = "Level1DanceSequence";

        private RunResults _results;
        private bool _scoreSubmitted = false;

        private void Start()
        {
            DisplayResults();
        }

        private void DisplayResults()
        {
            _results = RunResultsStore.LastResults;

            if (_results == null)
            {
                Debug.LogWarning("[ResultsSceneController] No results found.");
                return;
            }

            if (scoreText != null) scoreText.text = $"Score: {_results.finalScore}";
            if (accuracyText != null) accuracyText.text = $"Accuracy: {_results.accuracy:P1}";
            if (hitsText != null) hitsText.text = $"Hits: {_results.promptsHit}";
            if (missesText != null) missesText.text = $"Misses: {_results.promptsMissed}";
            if (streakText != null) streakText.text = $"Longest Streak: {_results.longestStreak}";
            if (sequencesText != null) sequencesText.text = $"Sequences: {_results.sequencesCompleted}";
        }

        public void SubmitCurrentScore()
        {
            if (_scoreSubmitted)
            {
                SetSubmitStatus("Score already submitted.");
                return;
            }

            if (_results == null)
            {
                SetSubmitStatus("No run results to submit.");
                return;
            }

            if (LeaderboardManager.Instance == null)
            {
                SetSubmitStatus("Leaderboard manager not found.");
                return;
            }

            string playerName = "Player";
            if (nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
            {
                playerName = nameInputField.text.Trim();
            }

            SetSubmitStatus("Submitting score...");

            LeaderboardManager.Instance.SubmitScore(playerName, _results.finalScore, success =>
            {
                if (success)
                {
                    _scoreSubmitted = true;
                    SetSubmitStatus("Score submitted!");
                }
                else
                {
                    SetSubmitStatus("Failed to submit score.");
                }
            });
        }

        private void LoadLeaderboard()
        {
            if (leaderboardText != null)
            {
                leaderboardText.text = "Loading leaderboard...";
            }

            if (LeaderboardManager.Instance == null)
            {
                if (leaderboardText != null)
                {
                    leaderboardText.text = "Leaderboard unavailable.";
                }
                return;
            }
        }

        private void SetSubmitStatus(string message)
        {
            if (submitStatusText != null)
            {
                submitStatusText.text = message;
            }
        }

        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void Replay()
        {
            SceneManager.LoadScene(playSceneName);
        }
    }
}