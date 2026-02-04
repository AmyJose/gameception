using UnityEngine;
using System.Collections;

public class ElementEffect : MonoBehaviour
{
    public float duration = 3f;

    private Coroutine activeRoutine;

    public void Activate()
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        gameObject.SetActive(true);
        activeRoutine = StartCoroutine(EffectTimer());
    }

    IEnumerator EffectTimer()
    {
        yield return new WaitForSeconds(duration);
        gameObject.SetActive(false);
        activeRoutine = null;
    }
}
