using Gameplay;
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
    [SerializeField] private DanceMatInputProvider danceMatInputProvider;
    [SerializeField] private SelectionState selectionState;

    [Serializable]
    public class PoseSpritePair
    {
        public Sprite alien;
        public Sprite element;
    }

    [Header("Alien Visuals")]
    [SerializeField] private SpriteRenderer alienSpriteRenderer1;
    [SerializeField] private SpriteRenderer alienSpriteRenderer2;
    [SerializeField] private List<PoseSpritePair> poseSpritePairs;
    [SerializeField] private Sprite defaultAlienSprite;
    [SerializeField] private Sprite defaultElementSprite;

    [Header("Pose Detection")]
    [SerializeField] private PoseDetectionRunner poseDetectionRunner;
    [SerializeField] private PoseState poseState;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private UnityEngine.UI.Image holdFillImage;
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

    [Header("Queue and Arrow Visibility")]
    [SerializeField] private List<GameObject> hideUntilLanePadStep = new();

    [Serializable]
    public class TutorialDialogueLine
    {
        [TextArea] public string text;
        public float extraWait = 1.0f;
    }

    [Header("Post Pose Dialogue")]
    [SerializeField] private List<TutorialDialogueLine> postPoseDialogue = new();

    private bool currentPoseMatched;
    private string currentExpectedPoseId;
    private float currentRequiredHoldTime;
    private float currentHoldTimer;
    private float _nextHoldDebugTime;
    private Sprite _initialAlienSprite;
    private Sprite _initialElementSprite;
    private bool waitingForSpecificPad;
    private bool requiredPadPressed;
    private int requiredPadIndex = -1;

    public event Action<string> OnPoseStepStarted;
    public event Action<string> OnPoseStepCompleted;
    public event Action OnTutorialFinished;

    private void OnEnable()
    {
        if (danceMatInputProvider != null)
        {
            danceMatInputProvider.OnPadPressed += HandlePadPressed;
            Debug.Log("[Tutorial] Subscribed to DanceMatInputProvider.OnPadPressed");
        }
        else
        {
            Debug.LogWarning("[Tutorial] DanceMatInputProvider is NOT assigned! Pad presses won't work.");
        }

        if (selectionState != null)
        {
            selectionState.OnChanged += HandleSelectionChanged;
        }
    }
    private void OnDisable()
    {
        if (danceMatInputProvider != null)
        {
            danceMatInputProvider.OnPadPressed -= HandlePadPressed;
        }

        if (selectionState != null)
        {
            selectionState.OnChanged -= HandleSelectionChanged;
        }
    }

    private void HandlePadPressed(int idx)
    {
        Debug.Log($"[Tutorial] Pad pressed: idx={idx}, waiting={waitingForSpecificPad}, required={requiredPadIndex}");

        if (!waitingForSpecificPad) return;
        if (idx != requiredPadIndex) return;

        Debug.Log($"[Tutorial] Pad matched! Setting requiredPadPressed=true");
        requiredPadPressed = true;
    }

    private void HandleSelectionChanged(IReadOnlyCollection<int> selected)
    {
        string selectedText = selected == null ? "null" : string.Join(", ", selected);
        Debug.Log($"[TutorialSceneController] Selection changed: [{selectedText}]");
    }

    private void Start()
    {
        _initialAlienSprite = alienSpriteRenderer1 != null ? alienSpriteRenderer1.sprite : null;
        _initialElementSprite = alienSpriteRenderer2 != null ? alienSpriteRenderer2.sprite : null;

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        if (choreographyTutorialRoot != null)
            choreographyTutorialRoot.SetActive(false);

        SetLanePadObjectsVisible(false);

        if (holdFillImage != null)
        {
            holdFillImage.fillAmount = 1f;
            holdFillImage.transform.parent.gameObject.SetActive(false);
        }

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
            //progress bar updating
            if (holdFillImage != null && currentRequiredHoldTime > 0f)
            {
                float progress = currentHoldTimer / currentRequiredHoldTime;
                holdFillImage.fillAmount = Mathf.Lerp(
                    holdFillImage.fillAmount,
                    progress,
                    Time.deltaTime * 10f
                );
            }
        }
    }

    private IEnumerator RunTutorialRoutine()
    {
        string intro = "Hey " + PlayerSession.PlayerName + "! We need your help!";
        yield return SpeakAndPause(intro, 1.0f);
        yield return SpeakAndPause("First, we need to calibrate our camera system.", 1.0f);

        ActivatePoseDetection();

        SetObjective("Stand so your whole body is visible in the camera.");
        yield return SpeakAndPause("Position yourself in the middle of the camera so we can see your entire body.", 1.2f);

        // yield return SpeakAndPause($"Jump on pad {readyPadIndex + 1} once you're ready.", 0.5f);
        yield return SpeakAndPause($"Jump on pad <sprite name=\"arrowup\"> once you're ready.", 0.5f);
        SetObjective($"Select pad <sprite name=\"arrowup\"> when you're ready.");

        yield return WaitForSpecificPadPress(readyPadIndex);
        //selectionState?.Clear();

        yield return SpeakAndPause("Perfect. Let's try a few poses.", 0.8f);

        for (int i = 0; i < poseSteps.Count; i++)
        {
            yield return RunPoseStep(poseSteps[i], i + 1, poseSteps.Count);
        }

        RestoreDefaultAlienVisuals();

        yield return SpeakAndPause("Let's learn the lanes and pads one by one.", 1.0f);
        SetLanePadObjectsVisible(true);

        // UP
        yield return SpeakAndPause("Step on the  <sprite name=\"arrowup\"> pad", 0.5f);
        yield return WaitForSpecificPadPress(0);

        yield return SpeakAndPause("Good!", 0.5f);

        // LEFT
        yield return SpeakAndPause("Now step on the  <sprite name=\"arrowleft\"> pad", 0.5f);
        yield return WaitForSpecificPadPress(1);

        yield return SpeakAndPause("Nice!", 0.5f);

        // DOWN
        yield return SpeakAndPause("Now step on the  <sprite name=\"arrowdown\"> pad", 0.5f);
        yield return WaitForSpecificPadPress(2);

        yield return SpeakAndPause("Great!", 0.5f);

        // RIGHT
        yield return SpeakAndPause("Finally step on the  <sprite name=\"arrowright\"> pad", 0.5f);
        yield return WaitForSpecificPadPress(3);

        yield return SpeakAndPause("Perfect! You’ve mastered the pads.", 1.0f);

        yield return SpeakAndPause("Amazing! You are ready to help us.", 1.0f);
        yield return SpeakAndPause("Next, prompts will move down the lane. Match the pose at the right moment!", 1.2f);


        StartChoreographyTutorial();
        SetObjective("Choreography tutorial coming next...");

        OnTutorialFinished?.Invoke();

        SceneManager.LoadScene("Level1DanceSequence");
    }

    private IEnumerator RunPoseStep(PoseTutorialStep step, int index, int total)
    {
        // if (alienSpriteRenderer != null && poseSprites != null && poseSprites.Count >= index)
        // {
        //     alienSpriteRenderer.sprite = poseSprites[index - 1];
        // }

        if (poseSpritePairs != null && poseSpritePairs.Count >= index)
        {
            PoseSpritePair pair = poseSpritePairs[index - 1];

            if (alienSpriteRenderer1 != null) alienSpriteRenderer1.sprite = pair.alien;
            if (alienSpriteRenderer2 != null) alienSpriteRenderer2.sprite = pair.element;
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

        //show progress bar
        if (holdFillImage != null)
        {
            holdFillImage.fillAmount = 0f;
            holdFillImage.transform.parent.gameObject.SetActive(true);
        }

        _nextHoldDebugTime = Time.time;
        currentPoseMatched = false;

        if (verbosePoseDebug)
        {
            Debug.Log($"[Tutorial] Step start expected={currentExpectedPoseId}, hold={currentRequiredHoldTime}");
            Debug.Log($"[Tutorial] Waiting for pose match: {step.poseId}");
        }

        SetObjective($"Hold the pose: {step.poseId}");



        yield return new WaitUntil(() => currentPoseMatched);
        //hide progress bar
        if (holdFillImage != null)
        {
            holdFillImage.fillAmount = 1f;
            holdFillImage.transform.parent.gameObject.SetActive(false);
        }

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


    private void RestoreDefaultAlienVisuals()
    {
        if (alienSpriteRenderer1 != null)
            alienSpriteRenderer1.sprite = defaultAlienSprite != null ? defaultAlienSprite : _initialAlienSprite;

        if (alienSpriteRenderer2 != null)
            alienSpriteRenderer2.sprite = defaultElementSprite != null ? defaultElementSprite : _initialElementSprite;
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

        SceneManager.LoadScene("Level1DanceSequence");
    }

    private void SetObjective(string text)
    {
        if (objectiveText != null)
            objectiveText.text = text;
    }

    private void SetLanePadObjectsVisible(bool isVisible)
    {
        if (hideUntilLanePadStep == null)
            return;

        for (int i = 0; i < hideUntilLanePadStep.Count; i++)
        {
            GameObject target = hideUntilLanePadStep[i];
            if (target != null)
                target.SetActive(isVisible);
        }
    }

    private IEnumerator WaitForSpecificPadPress(int padIndex)
    {
        waitingForSpecificPad = true;
        requiredPadIndex = padIndex;
        requiredPadPressed = false;

        yield return new WaitUntil(() => requiredPadPressed);

        waitingForSpecificPad = false;
        requiredPadIndex = -1;
    }

    private IEnumerator WaitForSinglePad(int padIndex)
    {
        yield return new WaitUntil(() => IsSinglePadSelected(padIndex));
        selectionState?.Clear();
    }

    /*private bool IsReadyPadSelected()
    {
        if (selectionState == null)
            return false;

        if (requireOnlyThisPadSelected)
            return selectionState.GetSingleSelected() == readyPadIndex;

        return selectionState.IsSelected(readyPadIndex);
    }*/

    private bool IsSinglePadSelected(int padIndex)
    {
        if (selectionState == null)
            return false;

        return selectionState.GetSingleSelected() == padIndex;
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