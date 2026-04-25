using UnityEngine;

public class PoseHoldProgressBar : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Transform fill;

    [Header("Optional")]
    [SerializeField] private Vector3 emptyScale = new Vector3(0f, 1f, 1f);
    [SerializeField] private Vector3 fullScale = new Vector3(1f, 1f, 1f);

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        if (root != null)
            root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void SetProgress(float progress01)
    {
        progress01 = Mathf.Clamp01(progress01);

        if (fill != null)
            fill.localScale = Vector3.Lerp(emptyScale, fullScale, progress01);
    }
}