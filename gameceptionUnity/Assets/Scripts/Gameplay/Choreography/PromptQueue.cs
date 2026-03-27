using System;
using System.Collections.Generic;
using InputLayer;
using UnityEngine;
using Rhythm;

namespace Gameplay.Choreography
{
    // Spawns prompts at cascading positions from spawnOffset

    public class PromptQueue : MonoBehaviour
    {   
        [Header("Dependencies")]
        [SerializeField] private PromptJudge promptJudge;
        [Header("Queue Layout")]
        [SerializeField] public float hitZoneY = 0f;
        [SerializeField] public float hitZoneThreshold = 0.5f;

        [Header("Spawn")]
        [SerializeField] private PromptIndicator promptPrefab;
        [SerializeField] private float promptSpacing = 2f;
        [SerializeField] public Vector3 spawnOffset = Vector3.zero;

        [Header("Scroll")]
        [SerializeField] private float unitsPerBeat = 1f;
        [SerializeField] private float beatSpeedMultiplier = 1f;
        [SerializeField] private BeatClock beatClock;

        [Header("Generation")]
        [SerializeField] private int generationIntervalBeats = 10;
        [SerializeField] private int promptsPerSequence = 4;

        // Events
        public event Action<PromptData> OnPromptEnteredZone;
        public event Action<PromptData> OnPromptExitedZone;

        public struct PromptData
        {
            public int id;
            public int sequenceId;
            public ElementPose requiredPose;
        }

        private struct PromptInfo
        {
            public int id;
            public int sequenceId;
            public float initialY;
            public float spawnTime;
        }

        private List<PromptIndicator> _activePrompts = new();
        private Dictionary<int, PromptInfo> _promptInfo = new();
        private HashSet<int> _promptsInZone = new();

        private float _totalScrollDistance = 0f;
        private int _nextPromptId = 0;
        private int _nextSequenceId = 0;
        private int _lastGeneratedBeat = -999;

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
            // Generate new sequence if interval reached
            if (beat.beatIndex >= _lastGeneratedBeat + generationIntervalBeats)
            {
                GenerateSequence();
                _lastGeneratedBeat = beat.beatIndex;
                Debug.Log($"[PromptQueue] Beat {beat.beatIndex}: Generated sequence {_nextSequenceId - 1}");
            }

            // Scroll all prompts down
            // _totalScrollDistance += unitsPerBeat;
            // UpdatePrompts();
        }
        private void Update()
        {
            if (beatClock == null) return;

            // Calculate the exact distance based on the smooth floating-point beat.
            // If multiplier is 2, it multiplies the final distance, keeping it perfectly smooth.
            _totalScrollDistance = beatClock.CurrentBeat * unitsPerBeat * beatSpeedMultiplier;

            // Move the prompts every single frame
            UpdatePrompts();
        }

        private void GenerateSequence()
        {
            int sequenceId = _nextSequenceId++;

            for (int i = 0; i < promptsPerSequence; i++)
            {
                SpawnPrompt(GetRandomPose(), sequenceId, i);
            }

            Debug.Log($"[PromptQueue] Sequence {sequenceId}: spawned {promptsPerSequence} prompts");
            if (promptJudge != null)
            {
                promptJudge.RegisterSequence(sequenceId, promptsPerSequence);
            }
        }

        private void SpawnPrompt(ElementPose pose, int sequenceId, int indexInSequence)
        {
            if (promptPrefab == null) return;

            var indicator = Instantiate(promptPrefab, transform);
            indicator.Initialize(pose, _nextPromptId);

            // Calculate spawn position
            float initialY = spawnOffset.y + (indexInSequence * promptSpacing);
            indicator.transform.localPosition = new Vector3(spawnOffset.x, initialY, 0);

            _promptInfo[_nextPromptId] = new PromptInfo
            {
                id = _nextPromptId,
                sequenceId = sequenceId,
                initialY = initialY,
                spawnTime = _totalScrollDistance
            };

            _activePrompts.Add(indicator);

            Debug.Log($"[PromptQueue] Spawned prompt {_nextPromptId} (seq {sequenceId}, idx {indexInSequence}) at Y={initialY:F2}");

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
                if (!_promptInfo.TryGetValue(id, out var info))
                    continue;

                //Uses stored initialY and scroll amount
                float scrollAmount = _totalScrollDistance - info.spawnTime;
                float scrolledY = info.initialY - scrollAmount;
                
                prompt.SetYPosition(scrolledY);

                // Zone detection
                if (!_promptsInZone.Contains(id))
                {
                    float distance = Mathf.Abs(scrolledY - hitZoneY);
                    if (distance <= hitZoneThreshold)
                    {
                        _promptsInZone.Add(id);
                        prompt.SetInHitZone(true);
                        OnPromptEnteredZone?.Invoke(new PromptData
                        {
                            id = id,
                            sequenceId = info.sequenceId,
                            requiredPose = prompt.GetRequiredPose()
                        });
                    }
                }
                else
                {
                    float distance = Mathf.Abs(scrolledY - hitZoneY);
                    if (distance > hitZoneThreshold)
                    {
                        _promptsInZone.Remove(id);
                        prompt.SetInHitZone(false);
                        OnPromptExitedZone?.Invoke(new PromptData
                        {
                            id = id,
                            sequenceId = info.sequenceId,
                            requiredPose = prompt.GetRequiredPose()
                        });
                    }
                }

                // Remove if scrolledpast bottom
                if (scrolledY < hitZoneY - 10f)
                {
                    _activePrompts.RemoveAt(i);
                    _promptInfo.Remove(id);
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
            _promptInfo.Clear();
            _promptsInZone.Clear();
        }

        private void OnDrawGizmos()
        {
            Vector3 center = transform.TransformPoint(new Vector3(0, hitZoneY, 0));
            Vector3 top = transform.TransformPoint(new Vector3(0, hitZoneY + hitZoneThreshold, 0));
            Vector3 bottom = transform.TransformPoint(new Vector3(0, hitZoneY - hitZoneThreshold, 0));

            float width = 5f;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(top - transform.right * width, top + transform.right * width);
            Gizmos.DrawLine(bottom - transform.right * width, bottom + transform.right * width);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(center - transform.right * width, center + transform.right * width);

            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            float scaledHeight = hitZoneThreshold * 2f * transform.lossyScale.y;
            Gizmos.DrawCube(center, new Vector3(width * 2, scaledHeight, 0.1f));
        }
    }
}