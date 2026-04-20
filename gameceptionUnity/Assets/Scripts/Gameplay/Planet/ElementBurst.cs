using UnityEngine;

public class ElementBurst : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private ParticleSystem burstParticles;

    [Header("Timing")]
    [SerializeField] private float lifetime = 0.9f;
    [SerializeField] private float fadeStartNormalized = 0.65f;

    [Header("Icon Motion")]
    [SerializeField] private float iconRiseDistance = 0f; //no drift
    [SerializeField] private float popDurationNormalized = 0.18f;

    [Header("Icon Scale")]
    [SerializeField] private float startScaleMultiplier = 0.9f;
    [SerializeField] private float peakScaleMultiplier = 1.05f;

    private Vector3 _iconStartLocalPos;
    private Vector3 _iconTargetLocalPos;
    private Vector3 _iconBaseScale;
    private Color _iconBaseColor;
    private float _timer;

    public void Play(Sprite iconSprite, Color particleColor)
    {
        if (iconRenderer != null)
        {
            iconRenderer.sprite = iconSprite;

            _iconStartLocalPos = iconRenderer.transform.localPosition;
            _iconTargetLocalPos = _iconStartLocalPos + Vector3.up * iconRiseDistance;

            _iconBaseScale = iconRenderer.transform.localScale;
            _iconBaseColor = iconRenderer.color;

            // start slightly smaller, then do a tiny pop
            iconRenderer.transform.localScale = _iconBaseScale * startScaleMultiplier;

            Color c = _iconBaseColor;
            c.a = 1f;
            iconRenderer.color = c;
        }

        if (burstParticles != null)
        {
            var main = burstParticles.main;
            main.startColor = particleColor;
            burstParticles.Play();
        }

        _timer = 0f;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        float t = Mathf.Clamp01(_timer / lifetime);

        if (iconRenderer != null)
        {
            // very small or zero movement
            iconRenderer.transform.localPosition = Vector3.Lerp(_iconStartLocalPos, _iconTargetLocalPos, t);

            // quick pop at the start, then settle
            float popT = Mathf.Clamp01(t / Mathf.Max(0.0001f, popDurationNormalized));
            float scaleMultiplier = Mathf.Lerp(startScaleMultiplier, peakScaleMultiplier, popT);
            iconRenderer.transform.localScale = _iconBaseScale * scaleMultiplier;

            // stay visible, then fade near the end
            float alpha = 1f;
            if (t >= fadeStartNormalized)
            {
                float fadeT = (t - fadeStartNormalized) / Mathf.Max(0.0001f, 1f - fadeStartNormalized);
                alpha = Mathf.Lerp(1f, 0f, fadeT);
            }

            Color c = _iconBaseColor;
            c.a = alpha;
            iconRenderer.color = c;
        }

        if (_timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}