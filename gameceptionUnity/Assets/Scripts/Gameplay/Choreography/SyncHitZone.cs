using UnityEngine;
using Gameplay.Choreography;
using System.Collections;

[ExecuteInEditMode]
public class SyncHitZone : MonoBehaviour
{
    [SerializeField] private PromptQueue queue;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer centerLineRenderer;
    [SerializeField] private float boxWidth = 74f;
    private Coroutine flashCoroutine;
    private Color originalColor;

    void Start()
    {
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    void Update()
    {
        if (queue == null || spriteRenderer != null) return;

        transform.localPosition = new Vector3(queue.spawnOffset.x, queue.hitZoneY, 0);
        float boxHeight = queue.hitZoneThreshold * 2f;
        spriteRenderer.size = new Vector2(boxWidth, boxHeight); 

        if (centerLineRenderer != null)
        {
            centerLineRenderer.size = new Vector2(boxWidth, 0.1f);
            centerLineRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        }
    }

    public void TriggerFeedback(bool isSuccess)
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            spriteRenderer.color = originalColor;
        }
        flashCoroutine = StartCoroutine(FlashHitZone(isSuccess));
    }

    private IEnumerator FlashHitZone(bool isSuccess)
    {
        if (!Application.isPlaying) yield break;
        
        Color flashColor = isSuccess ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);
        float flashDuration = 0.35f;
        float elapsedTime = 0f;

        while (elapsedTime < flashDuration)
        {
            spriteRenderer.color = Color.Lerp(originalColor, flashColor, elapsedTime / flashDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        spriteRenderer.color = originalColor;
    }
}
