using System.Collections;
using TMPro;
using UnityEngine;

public class AlienSpeechBubble : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private GameObject bubbleRoot;

    [Header("Positioning")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.8f, 0f);

    [Header("Typing")]
    [SerializeField] private float charactersPerSecond = 40f;

    private Coroutine typingRoutine;

    private void Awake()
    {
        if (bubbleRoot != null)
            bubbleRoot.SetActive(false);
    }

    private void LateUpdate()
    {
        if (followTarget != null)
        {
            transform.position = followTarget.position + worldOffset;
        }
    }

    public void ShowInstant(string text)
    {
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        if (bubbleRoot != null)
            bubbleRoot.SetActive(true);

        if (messageText != null)
            messageText.text = text;
    }

    public Coroutine ShowTyped(string text)
    {
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        if (bubbleRoot != null)
            bubbleRoot.SetActive(true);

        typingRoutine = StartCoroutine(TypeTextRoutine(text));
        return typingRoutine;
    }

    public void Hide()
    {
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        if (bubbleRoot != null)
            bubbleRoot.SetActive(false);
    }

    private IEnumerator TypeTextRoutine(string fullText)
    {
        if (bubbleRoot != null)
            bubbleRoot.SetActive(true);

        if (messageText == null)
            yield break;

        messageText.text = "";

        if (string.IsNullOrEmpty(fullText))
            yield break;

        float delay = 1f / Mathf.Max(1f, charactersPerSecond);

        int i = 0;

        while (i < fullText.Length)
        {
            if (fullText[i] == '<')
            {
                int closingIndex = fullText.IndexOf('>', i);

                if (closingIndex != -1)
                {
                    // Reveal the whole rich-text tag instantly.
                    i = closingIndex + 1;
                    messageText.text = fullText.Substring(0, i);
                    continue;
                }
            }

            i++;
            messageText.text = fullText.Substring(0, i);

            yield return new WaitForSeconds(delay);
        }

        typingRoutine = null;
    }
}