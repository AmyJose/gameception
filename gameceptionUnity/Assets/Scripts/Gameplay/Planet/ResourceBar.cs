using UnityEngine;

public class ResourceBar : MonoBehaviour
{
    [SerializeField] private Transform fillTransform;
    [SerializeField] private float fullWidth = 2.2f;
    [SerializeField] private Transform targetMarker;

    public void SetNormalised(float current, float target)
    {
        target = Mathf.Clamp01(target);
        current = Mathf.Clamp01(current);

        // fill the bar
        if (fillTransform != null)
        {
            Vector3 scale = fillTransform.localScale;
            scale.x = fullWidth * current;
            fillTransform.localScale = scale;

            Vector3 pos = fillTransform.localPosition;
            pos.x = (fullWidth * current) * 0.5f;
            fillTransform.localPosition = pos;
        }

        //target marker
        if (targetMarker != null) 
        {
            Vector3 markerPos = targetMarker.localPosition;
            markerPos.x = fullWidth * target;
            targetMarker.localPosition = markerPos;
        }
    }
}
