using InputLayer;
using UnityEngine;

namespace Gameplay.Choreography.UI
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
        [SerializeField] private Color hitZoneColor = new Color(0f, 1f, 0f, 0.9f); //becomes green when in hit zone

        [Header("Visual Polish")]
        [SerializeField] private float scaleInHitZone = 1.15f; // grows when in hit zone
        [SerializeField] private float normalScale = 1f;

        private int _promptId;
        private ElementPose _pose;
        private float _initialYPosition;
        private bool _isInHitZone = false;

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

            // Update colors
            if (backgroundRenderer != null)
            {
                backgroundRenderer.color = inZone ? hitZoneColor : normalColor;
            }

            // Update scale for emphasis
            UpdateScale();
        }

        public float GetInitialYPosition() => _initialYPosition;

        public int GetPromptId() => _promptId;

        public ElementPose GetRequiredPose() => _pose;

        private void UpdateScale()
        {
            float targetScale = _isInHitZone ? scaleInHitZone : normalScale;
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