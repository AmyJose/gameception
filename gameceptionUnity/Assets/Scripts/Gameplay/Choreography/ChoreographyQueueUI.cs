using System.Collections.Generic;
using Gameplay.Choreography;
using UnityEngine;

namespace Gameplay.Choreography.UI
{
    // Manages the choreography queue visualization in world space.
    // Creates and animates prompt indicators as scrolling objects.
    public class ChoreographyQueueUI : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private PromptIndicator promptPrefab;

        [Header("Layout Settings (World Units)")]
        [SerializeField] private float promptSpacing = 2.5f;// World units between prompts
        [SerializeField] private float scrollSpeed = 5f;// World units per beat
        [SerializeField] private Vector3 spawnOffset = Vector3.zero; // offset from queue center

        [Header("Hit Zone")]
        [SerializeField] private float hitZoneYPosition = 0f; // y pos where prompts are "hit"
        [SerializeField] private float hitZoneThreshold = 0.5f; // World units tolerance for hit zone

        [Header("Hit Marker Visual (Optional)")]
        [SerializeField] private SpriteRenderer hitMarkerRenderer;
        [SerializeField] private Sprite hitMarkerSprite;
        [SerializeField] private float hitMarkerLineWidth = 0.2f;

        [Header("Dependencies")]
        [SerializeField] private ChoreographyQueueState queueState;
        [SerializeField] private Rhythm.BeatClock beatClock;

        private readonly List<PromptIndicator> _activePrompts = new();
        private float _scrollOffset = 0f;

        // Cache for initial pos (for resetting scroll)
        private readonly Dictionary<int, float> _promptInitialYPositions = new();

        private void OnEnable()
        {
            if (queueState != null)
            {
                queueState.OnSequenceStarted += HandleSequenceStarted;
                queueState.OnPromptExpired += HandlePromptExpired;
            }

            if (beatClock != null)
                beatClock.OnBeat += HandleBeat;

            InitializeHitMarker();
        }

        private void OnDisable()
        {
            if (queueState != null)
            {
                queueState.OnSequenceStarted -= HandleSequenceStarted;
                queueState.OnPromptExpired -= HandlePromptExpired;
            }

            if (beatClock != null)
                beatClock.OnBeat -= HandleBeat;
        }

        // Called when a new choreography sequence starts
        // Spawns visual indicators for each prompt in the sequence
        private void HandleSequenceStarted(ChoreographySequence sequence, int startBeat)
        {
            ClearPrompts();
            _scrollOffset = 0f;
            _promptInitialYPositions.Clear();

            if (promptPrefab == null)
            {
                Debug.LogError("[ChoreographyQueueUI] Prompt prefab not assigned!");
                return;
            }

            // Spawn a visual indicator for each prompt action
            int index = 0;
            foreach (var action in sequence.prompts)
            {
                // Instantiate as child of queue object
                PromptIndicator indicator = Instantiate(promptPrefab, transform);
                indicator.Initialize(action.requiredPose, action.promptId);

                // prompts are verticall, starting off screen at top
                float initialY = (sequence.prompts.Count - 1 - index) * promptSpacing + spawnOffset.y;
                indicator.SetYPosition(initialY);

                // Cache initial position for scroll math
                _promptInitialYPositions[action.promptId] = initialY;

                _activePrompts.Add(indicator);

                Debug.Log($"[ChoreographyQueueUI] Spawned prompt {action.promptId} " +
                         $"({action.requiredPose}) at Y={initialY:F2}");

                index++;
            }

            Debug.Log($"[ChoreographyQueueUI] Sequence started: {_activePrompts.Count} prompts");
        }

        // Called when a prompt expires ie is no longer judged, removes it from the visual queue.

        private void HandlePromptExpired(ChoreographyQueueState.PromptData data)
        {
            var prompt = _activePrompts.Find(p => p.GetPromptId() == data.promptId);
            if (prompt != null)
            {
                _activePrompts.Remove(prompt);
                _promptInitialYPositions.Remove(data.promptId);
                Destroy(prompt.gameObject);

                Debug.Log($"[ChoreographyQueueUI] Removed expired prompt {data.promptId}");
            }
        }

        // Called on each beat; advances all prompts down the sequence by one beat interval.
        private void HandleBeat(Rhythm.BeatInfo beatInfo)
        {
            // Accumulate scroll distance based on beat duration and scroll speed
            _scrollOffset += (float)beatInfo.beatInterval * scrollSpeed;
            UpdatePromptPositions();
        }

        // Updates all prompt positions and hit zone detection
        private void UpdatePromptPositions()
        {
            foreach (var prompt in _activePrompts)
            {
                int promptId = prompt.GetPromptId();

                //gets initial position and apply scroll offset
                if (_promptInitialYPositions.TryGetValue(promptId, out float initialY))
                {
                    float scrolledY = initialY - _scrollOffset;
                    prompt.SetYPosition(scrolledY);

                    //checks if prompt is within hit zone
                    float distanceToHitZone = Mathf.Abs(scrolledY - hitZoneYPosition);
                    bool inHitZone = distanceToHitZone <= hitZoneThreshold;
                    prompt.SetInHitZone(inHitZone);
                }
            }
        }

        // Initialize the hit marker visual
        private void InitializeHitMarker()
        {
            if (hitMarkerRenderer != null)
            {
                // Positions hit marker at the hit zone y position
                Transform markerTransform = hitMarkerRenderer.transform;
                Vector3 markerPos = markerTransform.localPosition;
                markerPos.y = hitZoneYPosition;
                markerTransform.localPosition = markerPos;

                if (hitMarkerSprite != null)
                    hitMarkerRenderer.sprite = hitMarkerSprite;

                // Scale to fit line width
                Vector3 markerScale = markerTransform.localScale;
                markerScale.y = hitMarkerLineWidth;
                markerTransform.localScale = markerScale;
            }
        }

        //Remove all active prompt indicators.
        private void ClearPrompts()
        {
            foreach (var prompt in _activePrompts)
            {
                if (prompt != null)
                    Destroy(prompt.gameObject);
            }

            _activePrompts.Clear();
            _promptInitialYPositions.Clear();
        }

        private void OnDrawGizmos()
        {
            // Draw hit zone in editor for visual debugging
            Gizmos.color = Color.green;
            Vector3 hitZoneCenter = transform.position + Vector3.up * hitZoneYPosition;
            float visualWidth = 3f; // Visual debugging width

            Gizmos.DrawLine(
                hitZoneCenter + Vector3.left * visualWidth * 0.5f,
                hitZoneCenter + Vector3.right * visualWidth * 0.5f
            );

            // Draw hit zone bounds
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Vector3 upper = hitZoneCenter + Vector3.up * hitZoneThreshold;
            Vector3 lower = hitZoneCenter - Vector3.up * hitZoneThreshold;
            Gizmos.DrawLine(upper + Vector3.left * visualWidth * 0.5f, upper + Vector3.right * visualWidth * 0.5f);
            Gizmos.DrawLine(lower + Vector3.left * visualWidth * 0.5f, lower + Vector3.right * visualWidth * 0.5f);
        }
    }
}