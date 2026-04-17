using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LeaderboardSceneController : MonoBehaviour
{
    [SerializeField] private TMP_Text leaderboardText;

    private void Start()
    {
        StartCoroutine(LoadLeaderboardWhenReady());
    }

    private IEnumerator LoadLeaderboardWhenReady()
    {
        while (LeaderboardManager.Instance == null || !LeaderboardManager.Instance.IsReady)
        {
            yield return null;
        }

        LeaderboardManager.Instance.LoadTopScores(DisplayLeaderboard);
    }

    private void DisplayLeaderboard(List<LeaderboardManager.ScoreEntry> scores)
    {
        if (leaderboardText == null) return;

        if (scores == null || scores.Count == 0)
        {
            leaderboardText.text = "No scores yet.";
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Leaderboard ===\n");

        for (int i = 0; i < scores.Count; i++)
        {
            var s = scores[i];

            sb.AppendLine(
                $"{i + 1}. {s.name} - {s.score} | " +
                $"Acc: {s.accuracy:P0} | " +
                $"Streak: {s.longestStreak}"
            );
        }

        leaderboardText.text = sb.ToString();
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("Start");
    }
}