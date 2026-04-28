using System.Collections;
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
        [SerializeField] private TMP_Text bonusText;

        [Header("Formatting")]
        [SerializeField] private string timerPrefix = "Time: ";
        [SerializeField] private string scorePrefix = "Score: ";

        [Header("Bonus Popup")]
        [SerializeField] private float bonusShowDuration = 1.5f;

        private Coroutine bonusRoutine;

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
                scoreManager.OnSequenceBonusAwarded += HandleSequenceBonusAwarded;
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
                scoreManager.OnSequenceBonusAwarded -= HandleSequenceBonusAwarded;
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
            if (bonusText != null)
            {
                bonusText.gameObject.SetActive(false);
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

        private void HandleSequenceBonusAwarded(string label, int amount)
        {
            if (bonusText != null)
            {
                if (bonusRoutine != null)
                {
                    StopCoroutine(bonusRoutine);
                }

                bonusRoutine = StartCoroutine(ShowBonusRoutine(label, amount));
            }
        }

        private IEnumerator ShowBonusRoutine(string label, int amount)
        {
            bonusText.text = $"{label} +{amount}";
            bonusText.gameObject.SetActive(true);

            yield return new WaitForSeconds(bonusShowDuration);

            bonusText.gameObject.SetActive(false);
            bonusRoutine = null;
        }
    }
}
