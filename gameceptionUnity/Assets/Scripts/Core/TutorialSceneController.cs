using InputLayer;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Pose Detection")]
    [SerializeField] private PoseDetectionRunner poseDetectionRunner;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text objectiveText;

    [Header("Ready Selection")]
    [SerializeField] private int readyPadIndex = 0;
    [SerializeField] private bool requireOnlyThisPadSelected = false;

    [Header("Pose Tutorial")]
    [SerializeField] private List<PoseTutorialStep> poseSteps = new();

    [SerializeField] private float countdownSeconds = 3f;

    [Header("Debug")]
    [SerializeField] private bool allowKeyboardDebug = true;

    [Header("Next Tutorial Section")]
    [SerializeField] private GameObject choreographyTutorialRoot;

    private bool currentPoseMatched;
    private string currentExpectedPoseId;
    private float currentRequiredHoldTime;
    private float currentHoldTimer;

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
        if(selectionState != null)
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
                if (currentHoldTimer >= currentRequiredHoldTime)
                {
                    currentPoseMatched = true;
                }
            }
            else
            {
                currentHoldTimer = 0f;
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
    }

    private IEnumerator RunPoseStep(PoseTutorialStep step, int index, int total)
    {
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
        currentPoseMatched = false;

        SetObjective($"Hold the pose: {step.poseId}");

        yield return new WaitUntil(() => currentPoseMatched);

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
        // Replace with actual pose-check logic later.
        return false;
    }

    public void ForceCompleteCurrentPose()
    {
        currentPoseMatched = true;
    }
}