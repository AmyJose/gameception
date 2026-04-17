using UnityEngine;
using System.Collections;

public class AlienWalker : MonoBehaviour
{
    [SerializeField] private float angleDeg;
    [SerializeField] private float radius = 1f;
    [SerializeField] private float angularSpeedDegPerSec = 20f;
    [SerializeField] private float bobHeight = 0.6f;
    [SerializeField] private float bobDuration = 0.5f;

    [SerializeField] private float spriteRotationOffset = -90f;
    [SerializeField] private Transform orbitCenter;
    private float currentBobOffset;

    private void Start()
    {
        InvokeRepeating(nameof(TestBob), 0.1f, 0.5f);
    }
    private void TestBob()
    {
        TriggerBeatBob(bobHeight, bobDuration);
    }

    public void Initialise(Transform center, float startAngleDeg, float orbitRadius, float speedDegPerSec, float rotationOffsetDeg = -90f)
    {
        orbitCenter = center;
        angleDeg = startAngleDeg;
        radius = orbitRadius;
        angularSpeedDegPerSec = speedDegPerSec;
        spriteRotationOffset = rotationOffsetDeg;

        UpdatePositionAndRotation();
    }

    public void SetBobOffset(float offset)
    {
        currentBobOffset = offset;
    }

    public void TriggerBeatBob(float height, float duration)
    {
        StartCoroutine(BobRoutine(height, duration));
    }

    private System.Collections.IEnumerator BobRoutine(float height, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;

            currentBobOffset = Mathf.Sin(percent * Mathf.PI) * height; // simple sine wave bob
            yield return null;
        }
        currentBobOffset = 0f;
    }

    private void Update()
    {
        if (orbitCenter == null) return;

        angleDeg += angularSpeedDegPerSec * Time.deltaTime;
        UpdatePositionAndRotation();
    }

    private void UpdatePositionAndRotation()
    {
        if (orbitCenter == null) return;
        float angleRad = angleDeg * Mathf.Deg2Rad;

        Vector3 outDirection = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f);
        Vector3 basePosition = outDirection * radius;

        float totalDistanceFromCenter = radius + currentBobOffset;
        transform.localPosition = basePosition + (outDirection * currentBobOffset);
        
        float lookAngle = Mathf.Atan2(outDirection.y, outDirection.x) * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0f, 0f, lookAngle + spriteRotationOffset);
    }
}
