using System.Collections;
using UnityEngine;

public class UFO : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpeechBubble speechBubble;
    [SerializeField] private Transform visualRoot;

    [Header("Entrance")]
    [SerializeField] private float flyInDuration = 1.2f;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 3f, 0f);
    [SerializeField] private float pauseBeforeMessage = 0.4f;

    [Header("Idle Bob")]
    [SerializeField] private float bobAmplitude = 0.12f;
    [SerializeField] private float bobFrequency = 0.8f;

    private Vector3 baseWorldPosition;
    private Vector3 visualBaseLocalPosition;
    private bool bobbing;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        baseWorldPosition = transform.position;
        visualBaseLocalPosition = visualRoot.localPosition;
    }

    public IEnumerator PlayEntranceSequence(string message)
    {
        yield return FlyIntoPosition();
        StartBobbing();

        yield return new WaitForSeconds(pauseBeforeMessage);

        if (speechBubble != null)
            yield return speechBubble.ShowTyped(message);
    }

    public IEnumerator FlyIntoPosition()
    {
        Vector3 target = transform.position;
        Vector3 start = target + spawnOffset;

        transform.position = start;

        float elapsed = 0f;

        while (elapsed < flyInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flyInDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            transform.position = Vector3.Lerp(start, target, eased);
            yield return null;
        }

        transform.position = target;
        baseWorldPosition = target;
    }

    public void StartBobbing()
    {
        if (!bobbing)
            StartCoroutine(BobRoutine());
    }

    public void StopBobbing()
    {
        bobbing = false;
    }

    public void HideMessage()
    {
        if (speechBubble != null)
            speechBubble.Hide();
    }

    private IEnumerator BobRoutine()
    {
        bobbing = true;
        float time = 0f;

        while (bobbing)
        {
            time += Time.deltaTime;
            float yOffset = Mathf.Sin(time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;

            if (visualRoot != null)
                visualRoot.localPosition = visualBaseLocalPosition + new Vector3(0f, yOffset, 0f);

            yield return null;
        }

        if (visualRoot != null)
            visualRoot.localPosition = visualBaseLocalPosition;
    }
}