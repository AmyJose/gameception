using System;
using System.Collections.Generic;
using InputLayer;
using UnityEngine;
using Rhythm;

namespace Gameplay.Choreography
{
    // Spawns prompts at top and moves them down at each beat at fixed speed
    // Judge checks at hit zone: specified y position

    public class PromptQueue : MonoBehaviour
    {
        [Header("Queue Layout")]
        [SerializeField] private float hitZoneY = 0f;          // y pos where prompts are judged
        [SerializeField] private float hitZoneThreshold = 0.5f; // Tolerance

        [Header("Spawn")]
        [SerializeField] private PromptIndicator promptPrefab;
        [SerializeField] private float promptSpacing = 2.5f;
        [SerializeField] private Vector3 spawnOffset = Vector3.zero;

        [Header("Scroll")]
        [SerializeField] private float unitsPerBeat = 2.5f;     // impacts scroll smoothness.
        [SerializeField] private BeatClock beatClock;

        [Header("Generation")]
        [SerializeField] private int generationIntervalBeats = 60;
        [SerializeField] private int promptsPerSequence = 4;

        // Events
        public event Action<PromptData> OnPromptEnteredZone;
        public event Action<PromptData> OnPromptExitedZone;

        public struct PromptData
        {
            public int id;
            public ElementPose requiredPose;
        }

        private List<PromptIndicator> _activePrompts = new();
        private Dictionary<int, float> _promptInitialY = new();
        private float _totalScrollDistance = 0f;
        private int _nextPromptId = 0;
        private int _lastGeneratedBeat = -999;
        private HashSet<int> _promptsInZone = new(); // Track which prompts fired OnEntered

        private void OnEnable()
        {
            if (beatClock != null)
                beatClock.OnBeat += HandleBeat;
        }

        private void OnDisable()
        {
            if (beatClock != null)
                beatClock.OnBeat -= HandleBeat;
        }

        private void HandleBeat(BeatInfo beat)
        {
            // Generates new sequence if interval reached
            if (beat.beatIndex >= _lastGeneratedBeat + generationIntervalBeats)
            {
                GenerateSequence();
                _lastGeneratedBeat = beat.beatIndex;
            }

            // Scrolls prompts down
            _totalScrollDistance += unitsPerBeat;

            // Updates all prompts and check zone crossing
            UpdatePrompts();
        }

        private void GenerateSequence()
        {
            for (int i = 0; i < promptsPerSequence; i++)
            {
                SpawnPrompt(GetRandomPose());
            }

            Debug.Log($"[PromptQueue] Generated {promptsPerSequence} prompts");
        }

        private void SpawnPrompt(ElementPose pose)
        {
            if (promptPrefab == null) return;

            var indicator = Instantiate(promptPrefab, transform);
            indicator.Initialize(pose, _nextPromptId);

            // Position at top
            float initialY = (_activePrompts.Count) * promptSpacing + spawnOffset.y;
            indicator.SetYPosition(initialY);

            _promptInitialY[_nextPromptId] = initialY;
            _activePrompts.Add(indicator);

            _nextPromptId++;
        }

        private void UpdatePrompts()
        {
            for (int i = _activePrompts.Count - 1; i >= 0; i--)
            {
                var prompt = _activePrompts[i];
                if (prompt == null) 
                {
                    _activePrompts.RemoveAt(i);
                    continue;
                }
                int id = prompt.GetPromptId();

                // Calculates scrolled y
                float scrolledY = _promptInitialY[id] - _totalScrollDistance;
                prompt.SetYPosition(scrolledY);

                // Check if entered zone
                if (!_promptsInZone.Contains(id))
                {
                    float distance = Mathf.Abs(scrolledY - hitZoneY);
                    if (distance <= hitZoneThreshold)
                    {
                        _promptsInZone.Add(id);
                        prompt.SetInHitZone(true);
                        OnPromptEnteredZone?.Invoke(new PromptData { id = id, requiredPose = prompt.GetRequiredPose() });
                    }
                }
                else
                {
                    // In zone, checks if still in zone
                    float distance = Mathf.Abs(scrolledY - hitZoneY);
                    if (distance > hitZoneThreshold)
                    {
                        _promptsInZone.Remove(id);
                        prompt.SetInHitZone(false);
                        OnPromptExitedZone?.Invoke(new PromptData { id = id, requiredPose = prompt.GetRequiredPose() });
                    }
                }

                // Removes if scrolled past bottom
                if (scrolledY < hitZoneY - 30f)
                {
                    _activePrompts.RemoveAt(i);
                    _promptInitialY.Remove(id);
                    Destroy(prompt.gameObject);
                }
            }
        }

        private ElementPose GetRandomPose()
        {
            int random = UnityEngine.Random.Range(1, 5);
            return (ElementPose)random;
        }

        public void ClearAll()
        {
            foreach (var p in _activePrompts)
                if (p != null) Destroy(p.gameObject);
            _activePrompts.Clear();
            _promptInitialY.Clear();
            _promptsInZone.Clear();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Vector3 center = transform.position + Vector3.up * hitZoneY;
            Gizmos.DrawLine(center - Vector3.right * 3, center + Vector3.right * 3);
        }
    }
}
