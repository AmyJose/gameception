using System.Collections;
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

        [Header("Scene Names")]
        [SerializeField] private string mainMenuSceneName = "Start";
        [SerializeField] private string playSceneName = "Level1DanceSequence";
        [SerializeField] private string leaderboardSceneName = "Leaderboard";

        private RunResults _results;
        private bool _scoreSubmitted = false;

        private void Start()
        {
            DisplayResults();
            StartCoroutine(SubmitWhenReady());
        }

        private IEnumerator SubmitWhenReady()
        {
            while (!FirebaseInitializer.IsFirebaseReady)
            {
                yield return null;
            }
            while (FirebaseAuthController.Instance == null || !FirebaseAuthController.Instance.IsSignedIn)
            {
                yield return null;
            }
            while(LeaderboardSubmissionService.Instance == null)
            {
                yield return null;
            }
            if(_scoreSubmitted) yield break;

            RunResults results = RunResultsStore.LastResults;

            if(results == null)
            {
                Debug.LogWarning("[ResultsSceneController] No results to submit.");
                yield break;
            }

            string playerName = PlayerSession.HasName ? PlayerSession.PlayerName : "Player";

            Debug.Log("[ResultsSceneController] Auto-submitting score via Cloud Function...");

            LeaderboardSubmissionService.Instance.SubmitScore(playerName, results, success =>
            {
                if (success)
                {
                    _scoreSubmitted = true;
                    Debug.Log("[ResultsSceneController] Score auto-submitted.");
                }
                else
                {
                    Debug.LogWarning("[ResultsSceneController] Auto-submit failed.");
                }
            });
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

        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void Replay()
        {
            SceneManager.LoadScene(playSceneName);
        }
        public void GoToLeaderboard()
        {
            SceneManager.LoadScene(leaderboardSceneName);
        }
    }
}