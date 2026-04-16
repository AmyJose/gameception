using InputLayer;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TutorialSceneController : MonoBehaviour
{
    [Serializable]
    public class PoseTutorialStep
    {
        public string poseId;
        [TextArea] public string instructionText;
        public float holdDuration = 1.0f;
    }

    [Header("Core References")]
    [SerializeField] private AlienSpeechBubble speechBubble;
    [SerializeField] private SelectionState selectionState;

    [Header("Alien Visuals")]
    [SerializeField] private SpriteRenderer alienSpriteRenderer;
    [SerializeField] private List<Sprite> poseSprites;

    [Header("Pose Detection")]
    [SerializeField] private PoseDetectionRunner poseDetectionRunner;
    [SerializeField] private PoseState poseState;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField, Range(0f, 1f)] private float minPoseConfidence = 0.75f;

    [Header("Ready Selection")]
    [SerializeField] private int readyPadIndex = 0;
    [SerializeField] private bool requireOnlyThisPadSelected = false;

    [Header("Pose Tutorial")]
    [SerializeField] private List<PoseTutorialStep> poseSteps = new();

    [SerializeField] private float countdownSeconds = 3f;

    [Header("Debug")]
    [SerializeField] private bool allowKeyboardDebug = true;
    [SerializeField] private bool verbosePoseDebug = true;

    [Header("Pose Hold Tolerance")]
    [SerializeField, Min(0f)] private float holdLossPerSecondOnMismatch = 0.5f;

    [Header("Next Tutorial Section")]
    [SerializeField] private GameObject choreographyTutorialRoot;

    private bool currentPoseMatched;
    private string currentExpectedPoseId;
    private float currentRequiredHoldTime;
    private float currentHoldTimer;
    private float _nextHoldDebugTime;

    public event Action<string> OnPoseStepStarted;
    public event Action<string> OnPoseStepCompleted;
    public event Action OnTutorialFinished;

    private void OnEnable()
    {
        if (selectionState != null)
        {
            selectionState.OnChanged += HandleSelectionChanged;
        }
    }
    private void OnDisable()
    {
        if (selectionState != null)
        {
            selectionState.OnChanged -= HandleSelectionChanged;
        }
    }
    private void HandleSelectionChanged(IReadOnlyCollection<int> selected)
    {
        string selectedText = selected == null ? "null" : string.Join(", ", selected);
        Debug.Log($"[TutorialSceneController] Selection changed: [{selectedText}]");
    }

    private void Start()
    {
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        if (choreographyTutorialRoot != null)
            choreographyTutorialRoot.SetActive(false);

        StartCoroutine(RunTutorialRoutine());
    }

    private void Update()
    {
        if (allowKeyboardDebug && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ForceCompleteCurrentPose();
        }

        if (!string.IsNullOrEmpty(currentExpectedPoseId))
        {
            bool poseMatched = EvaluateCurrentPose(currentExpectedPoseId);

            if (poseMatched)
            {
                currentHoldTimer += Time.deltaTime;

                if (verbosePoseDebug && Time.time >= _nextHoldDebugTime)
                {
                    _nextHoldDebugTime = Time.time + 0.25f;
                    Debug.Log($"[Tutorial] HOLD progress for {currentExpectedPoseId}: {currentHoldTimer:F2}/{currentRequiredHoldTime:F2}");
                }

                if (currentHoldTimer >= currentRequiredHoldTime)
                {
                    if (verbosePoseDebug)
                    {
                        Debug.Log($"[Tutorial] HOLD COMPLETE for {currentExpectedPoseId}");
                    }

                    currentPoseMatched = true;
                }
            }
            else
            {
                if (currentHoldTimer > 0f)
                {
                    float previous = currentHoldTimer;
                    currentHoldTimer = Mathf.Max(0f, currentHoldTimer - Time.deltaTime * holdLossPerSecondOnMismatch);

                    if (verbosePoseDebug)
                    {
                        Debug.Log($"[Tutorial] HOLD decayed for {currentExpectedPoseId}: {previous:F2} -> {currentHoldTimer:F2}. " +
                                  $"CurrentPose={poseState?.CurrentPose}, Confidence={poseState?.Confidence:F2}");
                    }
                }
            }
        }
    }

    private IEnumerator RunTutorialRoutine()
    {
        yield return SpeakAndPause("Hey you! We need your help!", 1.0f);
        yield return SpeakAndPause("First, we need to calibrate our camera system.", 1.0f);

        ActivatePoseDetection();

        SetObjective("Stand so your whole body is visible in the camera.");
        yield return SpeakAndPause("Position yourself in the middle of the camera so we can see your entire body.", 1.2f);

        yield return SpeakAndPause($"Jump on pad {readyPadIndex + 1} once you're ready.", 0.5f);
        SetObjective($"Select pad {readyPadIndex + 1} when you're ready.");

        yield return new WaitUntil(IsReadyPadSelected);
        selectionState.Clear();

        yield return SpeakAndPause("Perfect. Let's try a few poses.", 0.8f);

        for (int i = 0; i < poseSteps.Count; i++)
        {
            yield return RunPoseStep(poseSteps[i], i + 1, poseSteps.Count);
        }

        yield return SpeakAndPause("Amazing! You are ready to help us.", 1.0f);
        yield return SpeakAndPause("Next, prompts will move down the lane. Match the pose at the right moment!", 1.2f);

        StartChoreographyTutorial();
        SetObjective("Choreography tutorial coming next...");

        OnTutorialFinished?.Invoke();

        SceneManager.LoadScene("Level1DanceSequence");
    }

    private IEnumerator RunPoseStep(PoseTutorialStep step, int index, int total)
    {
        if (alienSpriteRenderer != null && poseSprites != null && poseSprites.Count >= index)
        {
            alienSpriteRenderer.sprite = poseSprites[index - 1];
        }

        currentExpectedPoseId = null;
        currentPoseMatched = false;
        currentHoldTimer = 0f;

        OnPoseStepStarted?.Invoke(step.poseId);

        SetObjective($"Pose {index}/{total}: {step.instructionText}");
        yield return SpeakAndPause(step.instructionText, 0.4f);

        yield return RunCountdown(countdownSeconds);

        currentExpectedPoseId = step.poseId;
        currentRequiredHoldTime = step.holdDuration;
        currentHoldTimer = 0f;
        _nextHoldDebugTime = Time.time;
        currentPoseMatched = false;

        if (verbosePoseDebug)
        {
            Debug.Log($"[Tutorial] Step start expected={currentExpectedPoseId}, hold={currentRequiredHoldTime}");
            Debug.Log($"[Tutorial] Waiting for pose match: {step.poseId}");
        }

        SetObjective($"Hold the pose: {step.poseId}");



        yield return new WaitUntil(() => currentPoseMatched);

        if (verbosePoseDebug)
        {
            Debug.Log($"[Tutorial] Wait finished for pose: {step.poseId}");
        }

        currentExpectedPoseId = null;
        currentHoldTimer = 0f;

        SetObjective($"Great! {step.poseId} complete.");
        yield return SpeakAndPause("Nice!", 0.5f);

        OnPoseStepCompleted?.Invoke(step.poseId);


    }

    private IEnumerator RunCountdown(float seconds)
    {
        if (countdownText == null)
            yield break;

        countdownText.gameObject.SetActive(true);

        int startNumber = Mathf.CeilToInt(seconds);
        for (int i = startNumber; i >= 1; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        countdownText.text = "GO!";
        yield return new WaitForSeconds(0.5f);

        countdownText.gameObject.SetActive(false);
    }

    private IEnumerator SpeakAndPause(string message, float extraWait)
    {
        if (speechBubble != null)
        {
            Coroutine typing = speechBubble.ShowTyped(message);
            if (typing != null)
                yield return typing;
        }

        yield return new WaitForSeconds(extraWait);
    }

    private void ActivatePoseDetection()
    {
        poseDetectionRunner.SetVisualsVisible(true);
        poseDetectionRunner.ResumeDetection();
    }

    private void StartChoreographyTutorial()
    {
        if (choreographyTutorialRoot != null)
            choreographyTutorialRoot.SetActive(true);
    }

    private void SetObjective(string text)
    {
        if (objectiveText != null)
            objectiveText.text = text;
    }

    private bool IsReadyPadSelected()
    {
        if (selectionState == null)
            return false;

        if (requireOnlyThisPadSelected)
            return selectionState.GetSingleSelected() == readyPadIndex;

        return selectionState.IsSelected(readyPadIndex);
    }

    private bool EvaluateCurrentPose(string poseId)
    {
        if (poseState == null || string.IsNullOrWhiteSpace(poseId))
        {
            if (verbosePoseDebug && poseState == null)
            {
                Debug.LogWarning("[Tutorial] PoseState reference is null. Cannot evaluate pose.");
            }
            return false;
        }

        ElementPose expectedPose = ParsePoseId(poseId);
        if (expectedPose == ElementPose.None)
        {
            if (verbosePoseDebug)
            {
                Debug.LogWarning($"[Tutorial] Could not parse expected pose id '{poseId}'.");
            }
            return false;
        }

        return poseState.CurrentPose == expectedPose && poseState.Confidence >= minPoseConfidence;
    }

    private ElementPose ParsePoseId(string poseId)
    {
        string normalized = poseId.Trim();
        if (normalized.Length == 0)
            return ElementPose.None;

        if (Enum.TryParse(normalized, true, out ElementPose parsed))
            return parsed;

        switch (normalized.ToLowerInvariant())
        {
            case "air": return ElementPose.Ice;
            case "earth": return ElementPose.Earth;
            case "fire": return ElementPose.Fire;
            case "water": return ElementPose.Water;
            default:
                return ElementPose.None;
        }
    }

    public void ForceCompleteCurrentPose()
    {
        currentPoseMatched = true;
    }
}