using System.Collections;
using UnityEngine;

public class UFO : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpeechBubble speechBubble;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float pauseBeforeMessage = 0.4f;
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip enterSound;
    [SerializeField] private AudioClip flyOnPlanetSound;
    [SerializeField] private AudioClip exitSound;

    [Header("Idle Bob")]
    [SerializeField] private float bobAmplitude = 0.1f;
    [SerializeField] private float bobFrequency = 0.7f;

    private Vector3 visualBaseLocalPosition;
    private bool bobbing;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // if (audioSource != null && exitSound != null)
        //     audioSource.PlayOneShot(exitSound);

        visualBaseLocalPosition = visualRoot.localPosition;
    }

    public IEnumerator PlayEntranceSequence(Vector3 introWorldPosition, DirectionInstruction direction, float flyInDuration = 1.2f, float tiltAmount = 15f)
    {   
        if (audioSource != null && enterSound != null)
        {
            audioSource.PlayOneShot(enterSound);
        }
        yield return FlyTo(introWorldPosition, flyInDuration, tiltAmount);

        StartBobbing();

        yield return new WaitForSeconds(pauseBeforeMessage);

        if (speechBubble != null)
            speechBubble.ShowArrow(direction);
    }

    public IEnumerator PlayFlyToPlanetSequence(Vector3 planetPosition, float flyInDuration = 1f, float tiltAmount = 15f)
    {
        StopBobbing();

        if (audioSource != null && flyOnPlanetSound != null)
        {
            audioSource.PlayOneShot(flyOnPlanetSound);
        }

        yield return FlyTo(planetPosition, flyInDuration, tiltAmount);
    }

    public IEnumerator PlayExitSequence(Vector3 exitWorldPosition, float flyOutDuration = 1f, float tiltAmount = 15f)
    {
        Debug.Log("EXIT SOUND TRIGGERED!");
        StopBobbing();
        HideMessage();

        if (audioSource != null && exitSound != null)
        {
            audioSource.PlayOneShot(exitSound);
        }

        yield return FlyTo(exitWorldPosition, flyOutDuration, tiltAmount);
        Destroy(gameObject);
        
    }

    public IEnumerator FlyTo(Vector3 targetPosition, float duration = 1f, float tiltAmount = 15f)
    {
        StopBobbing();

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 direction = (targetPosition - startPos).normalized;
        float tiltZ = -direction.x * tiltAmount;
        Quaternion targetTilt = Quaternion.Euler(0f, 0f, tiltZ);

        float t = 0f;

        while (t < duration)
        {
            float dt = Time.deltaTime;
            t += dt;

            float normalized = Mathf.Clamp01(t / duration);
            float eased = Mathf.SmoothStep(0f, 1f, normalized);

            transform.position = Vector3.Lerp(startPos, targetPosition, eased);
            transform.rotation = Quaternion.Slerp(startRot, targetTilt, eased);

            yield return null;
        }

        // Finish movement
        transform.position = targetPosition;

        // Smoothly return to upright
        Quaternion finalTilt = transform.rotation;
        Quaternion upright = Quaternion.identity;

        float straightenTime = 0.2f;
        float t2 = 0f;

        while (t2 < straightenTime)
        {
            t2 += Time.deltaTime;

            float normalized = Mathf.Clamp01(t2 / straightenTime);
            float eased = Mathf.SmoothStep(0f, 1f, normalized);

            transform.rotation = Quaternion.Slerp(finalTilt, upright, eased);

            yield return null;
        }

        transform.rotation = upright;

    }

    public void StartBobbing()
    {
        if (!bobbing) StartCoroutine(BobRoutine());
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