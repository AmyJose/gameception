using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class SpeechBubble : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Arrow Mode")]
    [SerializeField] private Sprite leftArrow;
    [SerializeField] private Sprite upArrow;
    [SerializeField] private Sprite rightArrow;
    [SerializeField] private Sprite downArrow;

    public bool IsTyping { get; private set; }

    private void Awake()
    {
        Hide();
    }

    public void ShowArrow(DirectionInstruction direction)
    {
        if (root != null)
            root.SetActive(true);

        if (spriteRenderer != null)
            spriteRenderer.sprite = GetSprite(direction);
    }

    public void Hide()
    {
        IsTyping = false;

        if (root != null)
            root.SetActive(false);
    }

    private Sprite GetSprite(DirectionInstruction direction)
    {
        return direction switch
        {
            DirectionInstruction.Left => leftArrow,
            DirectionInstruction.Up => upArrow,
            DirectionInstruction.Right => rightArrow,
            DirectionInstruction.Down => downArrow,
            _ => null
        };
    }
}