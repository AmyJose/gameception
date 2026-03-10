using UnityEngine;

public class ResourceBar : MonoBehaviour
{
    [SerializeField] private Transform fillTransform;
    [SerializeField] private float fullWidth = 2.2f;

    public void SetNormalised(float t)
    {
        if (fillTransform == null) return;

        t = Mathf.Clamp01(t);

        Vector3 scale = fillTransform.localScale;
        scale.x = fullWidth * t;
        fillTransform.localScale = scale;

        Vector3 pos = fillTransform.localPosition;
        pos.x = (fullWidth * t) * 0.5f;
        fillTransform.localPosition = pos;
    }
}
