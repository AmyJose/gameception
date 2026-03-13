using UnityEngine;

public class BeatVisualiser : MonoBehaviour
{
    [SerializeField] private Rhythm.BeatClock beatClock;
    [SerializeField] private float scaleMultiplier = 1.2f;
    [SerializeField] private float lerpSpeed = 10f;

    private Vector3 _originalScale;
    private Vector3 _targetScale;

    private void Awake() => _originalScale = transform.localScale;
    private void OnEnable() => beatClock.OnBeat += Pulse;
    private void OnDisable() => beatClock.OnBeat -= Pulse;

    private void Pulse(Rhythm.BeatInfo info)
    {
        transform.localScale = _originalScale * scaleMultiplier;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, _originalScale, Time.deltaTime * lerpSpeed);
    }
}
