using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay
{
    public class ResultsSceneController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text accuracyText;
        [SerializeField] private TMP_Text hitsText;
        [SerializeField] private TMP_Text missesText;
        [SerializeField] private TMP_Text streakText;
        [SerializeField] private TMP_Text sequencesText;

        [Header("Scene Names")]
        [SerializeField] private string mainMenuSceneName = "Start";
        [SerializeField] private string playSceneName = "Level1DanceSequence";

        private void Start()
        {
            DisplayResults();
        }

        private void DisplayResults()
        {
            RunResults results = RunResultsStore.LastResults;

            if (results == null)
            {
                Debug.LogWarning("[ResultsSceneController] No results found.");
                return;
            }

            if (scoreText != null) scoreText.text = $"Score: {results.finalScore}";
            if (accuracyText != null) accuracyText.text = $"Accuracy: {results.accuracy:P1}";
            if (hitsText != null) hitsText.text = $"Hits: {results.promptsHit}";
            if (missesText != null) missesText.text = $"Misses: {results.promptsMissed}";
            if (streakText != null) streakText.text = $"Longest Streak: {results.longestStreak}";
            if (sequencesText != null) sequencesText.text = $"Sequences: {results.sequencesCompleted}";
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