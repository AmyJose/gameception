using UnityEngine;
using UnityEngine.UI;
using Gameplay.Choreography;

namespace UI
{
    public class DoubleTroubleIndicator : MonoBehaviour
    {
        [SerializeField] private PromptQueue promptQueue;
        [SerializeField] private Image modeIndicatorImage; // Visual asset/sprite for mode display
        [SerializeField] private Text modeIndicatorText; // Optional text overlay

        [Header("Visual Assets")]
        [SerializeField] private Sprite activeModeSprite; // Asset when Double Trouble ON
        [SerializeField] private Sprite inactiveModeSprite; // Asset when Double Trouble OFF
        [SerializeField] private Color activeModeColor = Color.green;
        [SerializeField] private Color inactiveModeColor = Color.gray;

        [Header("Text Overlay (Optional)")]
        [SerializeField] private string activeModeLabel = "🟢 DOUBLE TROUBLE";
        [SerializeField] private string inactiveModeLabel = "🔴 NORMAL MODE";

        [Header("Visual Polish")]
        [SerializeField, Range(0f, 5f)] private float fadeOutDuration = 2f;
        [SerializeField] private bool scaleOnModeChange = true;
        [SerializeField, Range(0.8f, 1.5f)] private float activeModeScale = 1.2f;
        [SerializeField, Range(0.8f, 1.5f)] private float inactiveModeScale = 0.9f;

        [Header("Audio Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip activateModeSound; // Plays when Double Trouble ON
        [SerializeField] private AudioClip deactivateModeSound; // Plays when Double Trouble OFF

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private float _fadeOutTimer = 0f;
        private bool _isFadingOut = false;

        private void OnEnable()
        {
            if (promptQueue != null)
                promptQueue.OnDoubleTroubleModeChanged += HandleModeChanged;
        }

        private void OnDisable()
        {
            if (promptQueue != null)
                promptQueue.OnDoubleTroubleModeChanged -= HandleModeChanged;
        }

        private void Start()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
                _rectTransform = gameObject.AddComponent<RectTransform>();

            // Initialize display
            if (promptQueue != null)
                UpdateDisplay(promptQueue.IsDoubleTroubleModeEnabled);
        }

        private void HandleModeChanged(bool isDoubleTroubleEnabled)
        {
            UpdateDisplay(isDoubleTroubleEnabled);
            PlayModeChangeAudio(isDoubleTroubleEnabled);
            
            _isFadingOut = false;
            _fadeOutTimer = 0f;

            // Start fade out after duration
            if (fadeOutDuration > 0)
                _isFadingOut = true;
        }

        private void Update()
        {
            if (!_isFadingOut) return;

            _fadeOutTimer += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - (_fadeOutTimer / fadeOutDuration));
            _canvasGroup.alpha = alpha;
        }

        private void UpdateDisplay(bool isDoubleTroubleEnabled)
        {
            if (modeIndicatorImage != null)
            {
                modeIndicatorImage.sprite = isDoubleTroubleEnabled ? activeModeSprite : inactiveModeSprite;
                modeIndicatorImage.color = isDoubleTroubleEnabled ? activeModeColor : inactiveModeColor;
            }

            if (modeIndicatorText != null)
            {
                modeIndicatorText.text = isDoubleTroubleEnabled ? activeModeLabel : inactiveModeLabel;
                modeIndicatorText.color = isDoubleTroubleEnabled ? activeModeColor : inactiveModeColor;
            }

            if (scaleOnModeChange && _rectTransform != null)
            {
                float targetScale = isDoubleTroubleEnabled ? activeModeScale : inactiveModeScale;
                _rectTransform.localScale = Vector3.one * targetScale;
            }

            // Reset alpha for fade in
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;

            Debug.Log($"[DoubleTroubleIndicator] Mode display updated -> {(isDoubleTroubleEnabled ? "ACTIVE" : "INACTIVE")}");
        }

        private void PlayModeChangeAudio(bool isDoubleTroubleEnabled)
        {
            if (audioSource == null) return;

            AudioClip clipToPlay = isDoubleTroubleEnabled ? activateModeSound : deactivateModeSound;

            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);
                Debug.Log($"[DoubleTroubleIndicator] Playing mode change audio: {clipToPlay.name}");
            }
        }
    }
}