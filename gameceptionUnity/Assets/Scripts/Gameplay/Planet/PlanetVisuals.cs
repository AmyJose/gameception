using UnityEngine;
using System.Collections;

public class PlanetVisuals : MonoBehaviour
{
    private Vector3 _baseScale;
    private Coroutine _currentEffect;

    void Start() 
    {
        _baseScale = transform.localScale;
    }


    
    public void TriggerSuccess(float duration)
    {
        if (_currentEffect != null) StopCoroutine(_currentEffect);
        _currentEffect = StartCoroutine(HitPulse(duration, Color.green));
    }

    public void TriggerMiss(float duration)
    {
        if (_currentEffect != null) StopCoroutine(_currentEffect);
        _currentEffect = StartCoroutine(HitPulse(duration, Color.red));
    }

    private IEnumerator HitPulse(float duration, Color feedbackColor)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scaleCurve = Mathf.Sin(t* Mathf.PI);
            transform.localScale = _baseScale + (Vector3.one * scaleCurve * 0.5f); // pulse up to 150% size
            yield return null;
        }
        transform.localScale = _baseScale;
    }
}
