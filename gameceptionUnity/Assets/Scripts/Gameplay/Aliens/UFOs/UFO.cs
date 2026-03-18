using System.Collections;
using UnityEngine;

public class UFO : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpeechBubble speechBubble;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float pauseBeforeMessage = 0.4f;

    [Header("Idle Bob")]
    [SerializeField] private float bobAmplitude = 0.1f;
    [SerializeField] private float bobFrequency = 0.7f;

    private Vector3 visualBaseLocalPosition;
    private bool bobbing;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        visualBaseLocalPosition = visualRoot.localPosition;
    }

    public IEnumerator PlayEntranceSequence(Vector3 introWorldPosition, string message, float flyInDuration = 1.2f, float tiltAmount = 15f)
    {
        yield return FlyTo(introWorldPosition, flyInDuration, tiltAmount);

        StartBobbing();

        yield return new WaitForSeconds(pauseBeforeMessage);

        if (speechBubble != null)
            yield return speechBubble.ShowTyped(message);
    }

    public IEnumerator FlyTo(Vector3 targetPosition, float duration = 1f, float tiltAmount = 15f)
    {
        Debug.Log($"[UFO] FlyTo start. From {transform.position} to {targetPosition}, duration={duration}");

        StopBobbing();

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 direction = (targetPosition - startPos).normalized;
        float tiltZ = -direction.x * tiltAmount;
        Quaternion targetTilt = Quaternion.Euler(0f, 0f, tiltZ);

        float t = 0f;
        float watchdog = 0f;

        while (t < duration)
        {
            float dt = Time.deltaTime;
            t += dt;
            watchdog += Time.unscaledDeltaTime;

            if (Mathf.FloorToInt(watchdog * 10f) % 10 == 0)
            {
                Debug.Log($"[UFO] FlyTo looping. dt={dt}, t={t}, duration={duration}, pos={transform.position}");
            }

            // safety escape so it can't hang forever
            if (watchdog > 5f)
            {
                Debug.LogError("[UFO] FlyTo watchdog triggered. Breaking out.");
                break;
            }

            float normalized = Mathf.Clamp01(t / duration);
            float eased = Mathf.SmoothStep(0f, 1f, normalized);

            transform.position = Vector3.Lerp(startPos, targetPosition, eased);
            transform.rotation = Quaternion.Slerp(startRot, targetTilt, eased);

            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = Quaternion.identity;

        Debug.Log("[UFO] FlyTo complete");
    }

    public void StartBobbing()
    {
        Debug.Log("[UFO] in bobbing");
        if (!bobbing)
            StartCoroutine(BobRoutine());
        Debug.Log("[UFO] after bob");
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