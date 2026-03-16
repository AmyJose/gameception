using UnityEngine;

public class AlienWalker : MonoBehaviour
{
    [SerializeField] private float angleDeg;
    [SerializeField] private float radius = 1f;
    [SerializeField] private float angularSpeedDegPerSec = 20f;

    [SerializeField] private float spriteRotationOffset = -90f;
    private Transform orbitCenter;

    public void Initialise(Transform center, float startAngleDeg, float orbitRadius, float speedDegPerSec, float rotationOffsetDeg = -90f)
    {
        orbitCenter = center;
        angleDeg = startAngleDeg;
        radius = orbitRadius;
        angularSpeedDegPerSec = speedDegPerSec;
        spriteRotationOffset = rotationOffsetDeg;

        UpdatePositionAndRotation();
    }

    private void Update()
    {
        if (orbitCenter == null) return;

        angleDeg += angularSpeedDegPerSec * Time.deltaTime;
        UpdatePositionAndRotation();
    }

    private void UpdatePositionAndRotation()
    {
        float angleRad = angleDeg * Mathf.Deg2Rad;

        Vector3 localOffset = new Vector3(
            Mathf.Cos(angleRad) * radius,
            Mathf.Sin(angleRad) * radius,
            0f
        );

        // If this alien is parented under the container, use localPosition
        transform.localPosition = localOffset;

        Vector2 dir = localOffset.normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        transform.localRotation = Quaternion.Euler(0f, 0f, angle + spriteRotationOffset);
    }
}
