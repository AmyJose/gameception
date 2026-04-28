using Audio;
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
    [SerializeField] private TutorialLaneGlowController laneGlowController;

    [Serializable]
    public class PoseSpritePair
    {
        public Sprite alien;
        public Sprite element;
        public Color barColor = Color.white;
    }

    [Header("Alien Visuals")]
    [SerializeField] private SpriteRenderer alienSpriteRenderer1;
    [SerializeField] private SpriteRenderer alienSpriteRenderer2;
    [SerializeField] private List<PoseSpritePair> poseSpritePairs;
    [SerializeField] private Sprite defaultAlienSprite;
    [SerializeField] private Sprite defaultElementSprite;
    [SerializeField] private Sprite grumpyAlienSprite;

    [Header("Pose Detection")]
    [SerializeField] private PoseDetectionRunner poseDetectionRunner;
    [SerializeField] private PoseState poseState;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private UnityEngine.UI.Image holdFillImage;
    [SerializeField] private GameObject progressPanel;
    [SerializeField, Range(0f, 1f)] private float minPoseConfidence = 0.75f;

    [Header("Ready Selection")]
    [SerializeField] private int readyPadIndex = 0;

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
        MusicManager.Instance.SetTutorialMode();
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
        if(progressPanel != null) progressPanel.gameObject.SetActive(false);

        laneGlowController?.ResetAllToNormal();

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
        string intro = "Zorp, you! " + PlayerSession.PlayerName + ", yes you!";
        yield return SpeakAndPause(intro, 0.8f);
        yield return SpeakAndPause("Oh finally, I found someone.", 1.0f);
        yield return SpeakAndPause("Our homes have been destroyed. My friends and I need your help!", 1.0f);

        SetAlienVisual(grumpyAlienSprite, defaultElementSprite);
        yield return SpeakAndPause("Long story. Evil space rock. Not my fault.", 0.9f);

        RestoreDefaultAlienVisuals();

        yield return SpeakAndPause("Everyone here needs something different to survive.", 1.2f);
        yield return SpeakAndPause("The problem is… the universe doesn’t understand us.", 1.0f);
        yield return SpeakAndPause("But it will understand you.", 0.9f);

        yield return SpeakAndPause("Everything responds to movement. Different shapes, different energy.", 1.0f);
        yield return SpeakAndPause("When you hold the right pose at the right time, the atoms align.", 1.2f);

        yield return SpeakAndPause("First, we need to see you properly.", 0.9f);

        ActivatePoseDetection();

        SetObjective("Stand so your whole body is visible in the camera.");
        yield return SpeakAndPause("Position yourself in the middle of the camera so we can see your entire body.", 1.2f);

        yield return SpeakAndPause("Please stand in the middle so I can translate your movement.", 1.0f);

        yield return SpeakAndPause("Step on the <sprite name=\"arrowup\"> pad when you're ready.", 0.7f);
        SetObjective("Step on <sprite name=\"arrowup\"> when you're ready.");

        yield return WaitForSpecificPadPress(readyPadIndex);
        //selectionState?.Clear();

        yield return SpeakAndPause("I’ll show you what we need, copy our poses and hold it!", 0.8f);

        for (int i = 0; i < poseSteps.Count; i++)
        {
            yield return RunPoseStep(poseSteps[i], i + 1, poseSteps.Count);
        }

        RestoreDefaultAlienVisuals();

        yield return SpeakAndPause("Now onto the dancemat!", 0.8f);
        SetLanePadObjectsVisible(true);
        yield return SpeakAndPause("Our planets are spread across different lanes.", 0.9f);
        yield return SpeakAndPause("Your pose gives them energy and your steps decides where it goes!", 0.9f);
        SetAlienVisual(grumpyAlienSprite, defaultElementSprite);
        yield return SpeakAndPause("Don't mix them up.", 0.8f);

        RestoreDefaultAlienVisuals();

        // yield return SpeakAndWaitForPadWithGlow("Step on the <sprite name=\"arrowup\"> <sprite name=\"arrowleft\"> <sprite name=\"arrowdown\"> <sprite name=\"arrowright\"> pads!", 0, 0.5f);



        // // UP
        // yield return SpeakAndWaitForPadWithGlow("Step on the  <sprite name=\"arrowup\"> pad", 0, 0.5f);

        // yield return SpeakAndPause("Good!", 0.5f);

        // // LEFT
        // yield return SpeakAndWaitForPadWithGlow("Now step on the  <sprite name=\"arrowleft\"> pad", 1, 0.5f);

        // yield return SpeakAndPause("Nice!", 0.5f);

        // // DOWN
        // yield return SpeakAndWaitForPadWithGlow("Now step on the  <sprite name=\"arrowdown\"> pad", 2, 0.5f);

        // yield return SpeakAndPause("Great!", 0.5f);

        // // RIGHT
        // yield return SpeakAndWaitForPadWithGlow("Finally step on the  <sprite name=\"arrowright\"> pad", 3, 0.5f);

        List<int> allPads = new List<int> { 0, 1, 2, 3 };
        yield return WaitForAllPadsWithGlow("Try stepping on all the pads!", allPads, 0.5f);

        yield return SpeakAndPause("Perfect! You’ve mastered the pads.", 1.0f);

        yield return SpeakAndPause("Okay. One last thing, they won’t wait forever.", 1.0f);
        yield return SpeakAndPause("When a prompt reaches the target, you have to respond in time.", 1.2f);

        yield return SpeakAndPause("Miss too many, and…", 1.0f);
        SetAlienVisual(grumpyAlienSprite, defaultElementSprite);
        yield return SpeakAndPause("You don't wanna know.", 1.0f);

        RestoreDefaultAlienVisuals();

        yield return SpeakAndPause("Ready?", 0.6f);
        yield return SpeakAndPause("Let’s groove!", 1.2f);

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

            if (holdFillImage != null)
            {
                holdFillImage.color = pair.barColor;
            }
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

        if (progressPanel != null) progressPanel.gameObject.SetActive(true);
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
        if (progressPanel != null) progressPanel.gameObject.SetActive(false);

        if (verbosePoseDebug)
        {
            Debug.Log($"[Tutorial] Wait finished for pose: {step.poseId}");
        }

        currentExpectedPoseId = null;
        currentHoldTimer = 0f;

        string[] praiseLines =
        {
            "They're responding to you!",
            "It's really working!",
            "I knew this would work.",
            "You’re actually good at this!"
        };

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
    private void SetAlienVisual(Sprite alienSprite, Sprite elementSprite)
    {
        if (alienSpriteRenderer1 != null)
        {
            alienSpriteRenderer1.sprite = alienSprite != null
                ? alienSprite
                : defaultAlienSprite;
        }

        if (alienSpriteRenderer2 != null)
        {
            alienSpriteRenderer2.sprite = elementSprite != null
                ? elementSprite
                : defaultElementSprite;
        }
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

    private IEnumerator SpeakAndWaitForPadWithGlow(string message, int padIndex, float extraWait)
    {
        yield return SpeakAndPause(message, extraWait);

        laneGlowController?.ActivateGlowForPad(padIndex);
        yield return WaitForSpecificPadPress(padIndex);
        laneGlowController?.DeactivateGlow();
    }


    private void RestoreDefaultAlienVisuals()
    {
        if (alienSpriteRenderer1 != null)
            alienSpriteRenderer1.sprite = defaultAlienSprite != null ? defaultAlienSprite : _initialAlienSprite;

        if (alienSpriteRenderer2 != null)
            alienSpriteRenderer2.sprite = defaultElementSprite != null ? defaultElementSprite : _initialElementSprite;

            if (holdFillImage != null)
            {
                holdFillImage.color = Color.white;
            }
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

    private IEnumerator WaitForAllPadsWithGlow(string message, List<int> padIndices, float extraWait)
    {
    yield return SpeakAndPause(message, extraWait);

    laneGlowController?.ResetAllToNormal();

    HashSet<int> unpressedPads = new HashSet<int>(padIndices);

    Action<int> onPadPressedHandler = null;
    onPadPressedHandler = (idx) =>
    {
        if (unpressedPads.Contains(idx))
        {
            laneGlowController?.ActivateGlowForPad(idx);
            unpressedPads.Remove(idx);
        }
    };

    danceMatInputProvider.OnPadPressed += onPadPressedHandler;

    yield return new WaitUntil(() => unpressedPads.Count == 0);
    
    danceMatInputProvider.OnPadPressed -= onPadPressedHandler;
    
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