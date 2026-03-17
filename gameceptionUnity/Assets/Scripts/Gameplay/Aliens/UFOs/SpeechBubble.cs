using System.Collections;
using TMPro;
using UnityEngine;

public class SpeechBubble : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float charactersPerSecond = 30f;

    public bool IsTyping { get; private set; }

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void ShowInstant(string message)
    {
        IsTyping = false;

        if (root != null)
            root.SetActive(true);

        if (messageText != null)
            messageText.text = message;
    }

    public IEnumerator ShowTyped(string message)
    {
        if (root != null)
            root.SetActive(true);

        IsTyping = true;
        messageText.text = "";

        float delay = 1f / charactersPerSecond;

        foreach (char c in message)
        {
            messageText.text += c;
            yield return new WaitForSeconds(delay);
        }

        Debug.Log("[SpeechBubble] Showing message");

        IsTyping = false;
    }

    public void Hide()
    {
        IsTyping = false;

        if (root != null)
            root.SetActive(false);
    }
}