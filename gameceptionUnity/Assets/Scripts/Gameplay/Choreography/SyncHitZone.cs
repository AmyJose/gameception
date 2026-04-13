using UnityEngine;
using Gameplay.Choreography;
using System.Collections;

[ExecuteInEditMode]
public class SyncHitZone : MonoBehaviour
{
    [SerializeField] private PromptQueue queue;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float boxWidth = 74f;
    private Coroutine flashCoroutine;

    void Update()
    {
        if (queue == null || spriteRenderer == null) return;

        //Positions the box at the exact hitZoneY (logical zone)
        transform.localPosition = new Vector3(queue.spawnOffset.x, queue.hitZoneY, 0);

        //Scales the box to be exactly the size of the threshold math
        float boxHeight = queue.hitZoneThreshold * 2f;
        spriteRenderer.size = new Vector2(boxWidth, boxHeight); 
    }

    public void TriggerFeedback(bool isSuccess)
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashHitZone(isSuccess));
    }

    private IEnumerator FlashHitZone(bool isSuccess)
    {
        if (!Application.isPlaying) yield break; // Avoid running in edit mode
        Color originalColor = spriteRenderer.color;
        Color flashColor = isSuccess ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);
        float flashDuration = 0.2f;
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
