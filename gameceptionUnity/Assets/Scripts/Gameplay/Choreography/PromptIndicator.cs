using InputLayer;
using UnityEngine;
using TMPro;

namespace Gameplay.Choreography
{
    // Visual representation of a single choreography prompt in world space
    public class PromptIndicator : MonoBehaviour
    {
        [Header("Sprite References")]
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private Sprite earthSprite;
        [SerializeField] private Sprite waterSprite;
        [SerializeField] private Sprite fireSprite;
        [SerializeField] private Sprite iceSprite;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.7f);
        [SerializeField] private Color hitZoneColor = new Color(1f, 1f, 1f, 0.7f); //becomes green when in hit zone
        [SerializeField] private Color missColor = new Color(1f, 0f, 0f, 0.9f);
        [SerializeField] private Color successColor = new Color(1f, 1f, 1f, 0.7f);
        [SerializeField] private Color midHitColor = new Color(1f, 1f, 0f, 0.9f); // Yellow for "Good" hits

        [Header("Visual Polish")]
        [SerializeField] private float scaleInHitZone = 1.15f; // grows when in hit zone
        [SerializeField] private float normalScale = 1f;

        [Header("Text Feedback")]
        [SerializeField] private TMPro.TextMeshProUGUI feedbackText;

        private int _promptId;
        private ElementPose _pose;
        private float _initialYPosition;
        private bool _isInHitZone = false;
        private bool _missed = false;
        private bool _succeeded = false;
        private bool _midHit = false;

        private void Awake()
        {
            // Cache initial position for scroll calculations
            _initialYPosition = transform.localPosition.y;
        }

        // Initialises this prompt with pose type and unique ID, called once when prompt is created
        public void Initialize(ElementPose pose, int promptId)
        {
            _pose = pose;
            _promptId = promptId;

            // Set pose sprite
            if (iconRenderer != null)
            {
                iconRenderer.sprite = GetSpriteForPose(pose);
                iconRenderer.color = Color.white;
            }

            // Initialize background
            if (backgroundRenderer != null)
            {
                backgroundRenderer.color = normalColor;
            }

            _isInHitZone = false;
            UpdateScale();
        }

        //Called every frame as the queue scrolls
        public void SetYPosition(float yPos)
        {
            Vector3 pos = transform.localPosition;
            pos.y = yPos;
            transform.localPosition = pos;
        }


        // Update visual feedback when prompt enters/exits hit zone.

        public void SetInHitZone(bool inZone)
        {
            if (_isInHitZone == inZone)
                return; // No change, skip update

            _isInHitZone = inZone;

            // // Update colors
            // if (backgroundRenderer != null)
            // {
            //     backgroundRenderer.color = inZone ? hitZoneColor : normalColor;
            // }

            // Update scale for emphasis
            UpdateScale();
        }


        public void SetSuccess()
        {
            if (backgroundRenderer != null)
                backgroundRenderer.color = new Color(0f, 1f, 0f, 1f); // green

            ShowFeedbackText("PERFECT!", new Color(0f, 1f, 0f, 1f));
        }

        public void SetFail()
        {
            if (backgroundRenderer != null)
                backgroundRenderer.color = new Color(1f, 0f, 0f, 1f); // Red

            ShowFeedbackText("MISS!", new Color(1f, 0f, 0f, 1f));
        }

        public void SetMidHit()
        {
            if (backgroundRenderer != null)
                backgroundRenderer.color = midHitColor; // Yellow

            ShowFeedbackText("GOOD!", new Color(1f, 1f, 0f, 1f));
        }
        
        private void ShowFeedbackText(string text, Color color)
        {
            if (feedbackText == null) return;
            
            feedbackText.text = text;
            feedbackText.color = color;
            feedbackText.gameObject.SetActive(true);
        }


        public float GetInitialYPosition() => _initialYPosition;

        public int GetPromptId() => _promptId;

        public ElementPose GetRequiredPose() => _pose;

        private void UpdateScale()
        {
            float shrinkFactor = 0.5f;
            float targetScale = (_isInHitZone ? scaleInHitZone : normalScale)*shrinkFactor;
            transform.localScale = Vector3.one * targetScale;
        }

        private Sprite GetSpriteForPose(ElementPose pose)
        {
            return pose switch
            {
                ElementPose.Earth => earthSprite,
                ElementPose.Water => waterSprite,
                ElementPose.Fire => fireSprite,
                ElementPose.Ice => iceSprite,
                _ => null
            };
        }
    }
}