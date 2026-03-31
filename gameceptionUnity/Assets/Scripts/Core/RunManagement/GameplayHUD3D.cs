using TMPro;
using UnityEngine;

namespace Gameplay
{
    public class GameplayHUD3D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameFlowController gameFlowController;
        [SerializeField] private ScoreManager scoreManager;

        [Header("Text References")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text scoreText;

        [Header("Formatting")]
        [SerializeField] private string timerPrefix = "Time: ";
        [SerializeField] private string scorePrefix = "Score: ";

        private void Awake()
        {
            if (gameFlowController == null)
            {
                gameFlowController = FindFirstObjectByType<GameFlowController>();
            }

            if (scoreManager == null)
            {
                scoreManager = FindFirstObjectByType<ScoreManager>();
            }
        }

        private void OnEnable()
        {
            if (gameFlowController != null)
            {
                gameFlowController.OnTimerUpdated += HandleTimerUpdated;
            }

            if (scoreManager != null)
            {
                scoreManager.OnScoreChanged += HandleScoreChanged;
            }
        }

        private void OnDisable()
        {
            if (gameFlowController != null)
            {
                gameFlowController.OnTimerUpdated -= HandleTimerUpdated;
            }

            if (scoreManager != null)
            {
                scoreManager.OnScoreChanged -= HandleScoreChanged;
            }
        }

        private void Start()
        {
            if (gameFlowController != null)
            {
                HandleTimerUpdated(gameFlowController.RemainingTime);
            }

            if (scoreManager != null)
            {
                HandleScoreChanged(scoreManager.CurrentScore);
            }
        }

        private void HandleTimerUpdated(float remainingTime)
        {
            if (timerText == null) return;

            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, remainingTime));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            timerText.text = $"{timerPrefix}{minutes:00}:{seconds:00}";
        }

        private void HandleScoreChanged(int score)
        {
            if (scoreText == null) return;

            scoreText.text = $"{scorePrefix}{score}";
        }
    }
}
