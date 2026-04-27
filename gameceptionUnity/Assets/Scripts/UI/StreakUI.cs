using TMPro;
using UnityEngine;
using Gameplay;
using System.Collections;

public class StreakUI : MonoBehaviour
{
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private TMP_Text streakText;
    [SerializeField] private int showFromStreak = 3;

    [SerializeField] private float popScale = 1.35f;
    [SerializeField] private float popDuration = 0.12f;
    [SerializeField] private float settleDuration = 0.18f;

    private int lastStreak;
    private Vector3 baseScale;
    private Vector2 baseAnchoredPosition;
    private RectTransform rectTransform;
    private Coroutine animRoutine;

    private void Awake()
    {
        if (streakText != null)
        {
            rectTransform = streakText.rectTransform;
            baseScale = rectTransform.localScale;
            baseAnchoredPosition = rectTransform.anchoredPosition;
        }
    }

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

        bool shouldShow = streak >= showFromStreak;
        streakText.gameObject.SetActive(shouldShow);

        if (!shouldShow)
        {
            lastStreak = streak;
            return;
        }

        streakText.text = $"{streak}";

        if (streak > lastStreak)
        {
            PlayPunch();
        }

        lastStreak = streak;
    }
    private void PlayPunch()
    {
        if (animRoutine != null)
            StopCoroutine(animRoutine);

        animRoutine = StartCoroutine(PunchRoutine());
    }

    private IEnumerator PunchRoutine()
    {
        float timer = 0f;

        while (timer < popDuration)
        {
            timer += Time.deltaTime;
            float t = timer / popDuration;

            rectTransform.localScale = Vector3.Lerp(baseScale, baseScale * popScale, t);

            yield return null;
        }

        timer = 0f;

        while (timer < settleDuration)
        {
            timer += Time.deltaTime;
            float t = timer / settleDuration;
            t = 1f - Mathf.Pow(1f - t, 3f);

            rectTransform.localScale = Vector3.Lerp(baseScale * popScale, baseScale, t);
            rectTransform.anchoredPosition = Vector3.Lerp(rectTransform.anchoredPosition, baseAnchoredPosition, t);

            yield return null;
        }

        rectTransform.localScale = baseScale;
        rectTransform.anchoredPosition = baseAnchoredPosition;
        animRoutine = null;
    }
}