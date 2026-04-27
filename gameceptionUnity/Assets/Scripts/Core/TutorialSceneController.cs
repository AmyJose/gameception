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
        if (!waitingForSpecificPad) return;
        if (idx != requiredPadIndex) return;
        requiredPadPressed = true;
    }

    private void HandleSelectionChanged(IReadOnlyCollection<int> selected)
    {
        string selectedText = selected == null ? "null" : string.Join(", ", selected);
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
                }

                if (currentHoldTimer >= currentRequiredHoldTime)
                {
                    currentPoseMatched = true;
                }
            }
            else
            {
                if (currentHoldTimer > 0f)
                {
                    float previous = currentHoldTimer;
                    currentHoldTimer = Mathf.Max(0f, currentHoldTimer - Time.deltaTime * holdLossPerSecondOnMismatch);
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
        string intro = "Hey you! " + PlayerSession.PlayerName + ", yes you!";
        yield return SpeakAndPause(intro, 0.8f);
        yield return SpeakAndPause("Oh thank goodness, I found someone. My home has been destroyed.", 1.0f);
        yield return SpeakAndPause("There's not many of us left.", 1.0f);
        yield return SpeakAndPause("Most of our worlds are... gone.", 1.2f);
        yield return SpeakAndPause("Please help me fix it!", 1.0f);

        SetAlienVisual(grumpyAlienSprite, defaultElementSprite);
        yield return SpeakAndPause("Long story. Bad timing. Not my fault.", 0.9f);

        RestoreDefaultAlienVisuals();

        yield return SpeakAndPause("Now they’re all here, and they all need something different to survive.", 1.2f);
        yield return SpeakAndPause("The problem is… they don’t understand me.", 1.0f);
        yield return SpeakAndPause("But they might understand you.", 0.9f);

        yield return SpeakAndPause("Every species responds to movement.", 1.0f);
        yield return SpeakAndPause("Different shapes. Different energy.", 1.0f);
        yield return SpeakAndPause("When you hold the right pose… they recognise it.", 1.2f);
        yield return SpeakAndPause("It gives them what they need.", 1.0f);

        yield return SpeakAndPause("First, I need to see you properly.", 0.9f);

        ActivatePoseDetection();

        SetObjective("Stand so your whole body is visible in the camera.");

        yield return SpeakAndPause("Stand in the middle so I can read your movement.", 1.0f);
        yield return SpeakAndPause("If I can’t see you, I can’t translate anything.", 1.0f);

        yield return SpeakAndPause("Step on the <sprite name=\"arrowup\"> pad when you're ready.", 0.7f);
        SetObjective("Step on <sprite name=\"arrowup\"> when you're ready.");

        yield return WaitForSpecificPadPress(readyPadIndex);
        //selectionState?.Clear();

        yield return SpeakAndPause("I’ll show you what each one needs.", 0.8f);
        yield return SpeakAndPause("Copy the pose… and hold it.", 0.9f);
        yield return SpeakAndPause("If you lose it, they lose it too.", 1.2f);

        for (int i = 0; i < poseSteps.Count; i++)
        {
            yield return RunPoseStep(poseSteps[i], i + 1, poseSteps.Count);
        }

        RestoreDefaultAlienVisuals();

        yield return SpeakAndPause("Now… the dancemat.", 0.8f);
        SetLanePadObjectsVisible(true);
        yield return SpeakAndPause("They’re spread across different lanes.", 0.9f);
        yield return SpeakAndPause("Your pose gives the energy.", 0.9f);
        yield return SpeakAndPause("Your feet decide who receives it.", 1.1f);
        yield return SpeakAndPause("So step on the pad I call out.", 0.8f);
        SetAlienVisual(grumpyAlienSprite, defaultElementSprite);
        yield return SpeakAndPause("Try not to mix them up.", 0.8f);
        yield return SpeakAndPause("They get… tricky.", 0.9f);

        RestoreDefaultAlienVisuals();

        // UP
        yield return SpeakAndWaitForPadWithGlow("Step on the  <sprite name=\"arrowup\"> pad", 0, 0.5f);

        yield return SpeakAndPause("Good!", 0.5f);

        // LEFT
        yield return SpeakAndWaitForPadWithGlow("Now step on the  <sprite name=\"arrowleft\"> pad", 1, 0.5f);

        yield return SpeakAndPause("Nice!", 0.5f);

        // DOWN
        yield return SpeakAndWaitForPadWithGlow("Now step on the  <sprite name=\"arrowdown\"> pad", 2, 0.5f);

        yield return SpeakAndPause("Great!", 0.5f);

        // RIGHT
        yield return SpeakAndWaitForPadWithGlow("Finally step on the  <sprite name=\"arrowright\"> pad", 3, 0.5f);

        yield return SpeakAndPause("Yes! You're getting it", 0.7f);

        yield return SpeakAndPause("Okay. Final thing, promise...", 1.0f);
        yield return SpeakAndPause("They won’t wait forever.", 0.9f);
        yield return SpeakAndPause("When a prompt reaches the target… you have to respond in time.", 1.2f);

        SetAlienVisual(grumpyAlienSprite, defaultElementSprite);
        yield return SpeakAndPause("Miss too many, and… let’s not miss too many.", 1.0f);

        RestoreDefaultAlienVisuals();

        yield return SpeakAndPause("Ready?", 0.6f);
        yield return SpeakAndPause("Let’s keep them alive.", 1.2f);

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
            SetAlienVisual(pair.alien, pair.element);
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

        SetObjective($"Hold the pose: {step.poseId}");



        yield return new WaitUntil(() => currentPoseMatched);
        //hide progress bar
        if (holdFillImage != null)
        {
            holdFillImage.fillAmount = 1f;
            holdFillImage.transform.parent.gameObject.SetActive(false);
        }

        currentExpectedPoseId = null;
        currentHoldTimer = 0f;

        string[] praiseLines =
        {
            "They're responding to you.",
            "That's... actually working",
            "I knew this would work",
            "You’re actually good at this… weird."
        };

        SetObjective($"Great! {step.poseId} complete.");
        yield return SpeakAndPause(praiseLines[index-1], 0.5f);

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
    }
    private void SetAlienVisual(Sprite alienSprite, Sprite elementSprite)
    {
        if (alienSpriteRenderer1 != null && alienSprite != null)
        {
            alienSpriteRenderer1.sprite = alienSprite;
        }

        if (alienSpriteRenderer2 != null && elementSprite != null)
        {
            alienSpriteRenderer2.sprite = elementSprite;
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