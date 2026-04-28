using UnityEngine;
using UnityEngine.UI;
using Gameplay.Choreography;

namespace UI
{
    public class DoubleTroubleIndicator : MonoBehaviour
    {
        [SerializeField] private PromptQueue promptQueue;
        [SerializeField] private SpriteRenderer modeIndicatorRenderer; // Sprite renderer for world-space display

        [Header("Visual Assets")]
        [SerializeField] private Sprite activeModeSprite; // Asset when Double Trouble ON
        [SerializeField] private Sprite inactiveModeSprite; // Asset when Double Trouble OFF

        [Header("Visual Polish")]
        [SerializeField, Range(0f, 5f)] private float fadeOutDuration = 2f;
        [SerializeField] private bool scaleOnModeChange = true;
        [SerializeField, Range(0.8f, 1.5f)] private float activeModeScale = 1.2f;
        [SerializeField, Range(0.8f, 1.5f)] private float inactiveModeScale = 0.9f;

        [Header("Audio Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip activateModeSound; // Plays when Double Trouble ON
        [SerializeField] private AudioClip deactivateModeSound; // Plays when Double Trouble OFF

        private float _fadeOutTimer = 0f;
        private bool _isFadingOut = false;
        private Color _originalColor;

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
            if (modeIndicatorRenderer == null)
                modeIndicatorRenderer = GetComponent<SpriteRenderer>();

            if (modeIndicatorRenderer != null)
                _originalColor = modeIndicatorRenderer.color;

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
            if (!_isFadingOut || modeIndicatorRenderer == null) return;

            _fadeOutTimer += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - (_fadeOutTimer / fadeOutDuration));
            Color newColor = _originalColor;
            newColor.a = alpha;
            modeIndicatorRenderer.color = newColor;
        }

        private void UpdateDisplay(bool isDoubleTroubleEnabled)
        {
            if (modeIndicatorRenderer != null)
            {
                modeIndicatorRenderer.sprite = isDoubleTroubleEnabled ? activeModeSprite : inactiveModeSprite;
            }

            if (scaleOnModeChange)
            {
                float targetScale = isDoubleTroubleEnabled ? activeModeScale : inactiveModeScale;
                transform.localScale = Vector3.one * targetScale;
            }

            // Reset alpha for fade in
            if (modeIndicatorRenderer != null)
            {
                Color color = modeIndicatorRenderer.color;
                color.a = 1f;
                modeIndicatorRenderer.color = color;
            }

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