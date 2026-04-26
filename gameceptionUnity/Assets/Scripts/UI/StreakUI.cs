using TMPro;
using UnityEngine;
using Gameplay;

public class StreakUI : MonoBehaviour
{
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private TMP_Text streakText;
    [SerializeField] private int showFromStreak = 2;

    private void OnEnable()
    {
        if (scoreManager != null)
            scoreManager.OnStreakChanged += UpdateStreakText;
    }

    private void OnDisable()
    {
        if (scoreManager != null)
            scoreManager.OnStreakChanged -= UpdateStreakText;
    }

    private void Start()
    {
        UpdateStreakText(scoreManager != null ? scoreManager.CurrentStreak : 0);
    }

    private void UpdateStreakText(int streak)
    {
        if (streakText == null) return;

        if (streak >= showFromStreak)
        {
            streakText.gameObject.SetActive(true);
            streakText.text = $"{streak}";
        }
        else
        {
            streakText.gameObject.SetActive(false);
        }
    }
}