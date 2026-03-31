using UnityEngine;
using Gameplay.Choreography;

[ExecuteInEditMode]
public class SyncHitZone : MonoBehaviour
{
    [SerializeField] private PromptQueue queue;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float boxWidth = 74f;

    void Update()
    {
        if (queue == null || spriteRenderer == null) return;

        //Positions the box at the exact hitZoneY (logical zone)
        transform.localPosition = new Vector3(queue.spawnOffset.x, queue.hitZoneY, 0);

        //Scales the box to be exactly the size of the threshold math
        float boxHeight = queue.hitZoneThreshold * 2f;
        spriteRenderer.size = new Vector2(boxWidth, boxHeight); 
    }
}
