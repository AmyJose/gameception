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
        [SerializeField] public float hitZoneThreshold = 0.8f;

        [Header("Spawn")]
        [SerializeField] private PromptIndicator promptPrefab;
        [SerializeField] public Vector3 spawnOffset = Vector3.zero;
        [SerializeField]
        private Vector3[] laneOffsets = new Vector3[4]
        {
            new Vector3(-8f, 0f, 0f),  // Lane 0: Left
            new Vector3(-6f, 0f, 0f),  // Lane 1
            new Vector3(-4f, 0f, 0f),   // Lane 2
            new Vector3(-2f, 0f, 0f)    // Lane 3: Right
        };
        [SerializeField] private bool autoStartGeneration = false;

        [Header("Scroll")]
        [SerializeField] private float unitsPerBeat = 1f;
        [SerializeField] private float beatSpeedMultiplier = 1f;
        [SerializeField] private BeatClock beatClock;

        [Header("Generation")]
        [SerializeField] private int generationIntervalBeats = 10;
        [SerializeField] private int promptsPerSequence = 4;

        [SerializeField] private PromptSelector promptSelector;

        // Events
        public event Action<PromptData> OnPromptEnteredZone;
        public event Action<PromptData> OnPromptExitedZone;

        public struct PromptData
        {
            public int id;
            public int sequenceId;
            public int laneIndex; // (0-3)for future: link with planet id
            public ElementPose requiredPose;
            public float currentY;
        }

        private struct PromptInfo
        {
            public int id;
            public int sequenceId;
            public int laneIndex; // for future: link with planet id
            public float initialY;
            public float spawnTime;
        }

        private List<PromptIndicator> _activePrompts = new();
        private Dictionary<int, PromptInfo> _promptInfo = new();
        private HashSet<int> _promptsInZone = new();

        private readonly List<int> _activeLanes = new();
        private readonly List<int> _sequenceLaneOrder = new();

        private float _totalScrollDistance = 0f;
        private int _nextPromptId = 0;
        private int _nextSequenceId = 0;
        private int _lastGeneratedSequenceBeat = -999;
        private int _currentSequenceStartBeat = 0;
        private int _promptsSpawnedThisSequence = 0;

        private bool _isGenerating = false;
        public bool IsGenerating => _isGenerating;
        public IReadOnlyList<int> ActiveLanes => _activeLanes;

        private void OnEnable()
        {
            if (beatClock != null)
                beatClock.OnBeat += HandleBeat;
            _isGenerating = autoStartGeneration;
        }

        private void OnDisable()
        {
            if (beatClock != null)
                beatClock.OnBeat -= HandleBeat;
        }

        private void HandleBeat(BeatInfo beat)
        {
            if (!_isGenerating) return;
            if (_activeLanes.Count == 0) return;

            // Check if time to start a new sequence
            if (beat.beatIndex >= _lastGeneratedSequenceBeat + generationIntervalBeats)

            {
                StartNewSequence(beat.beatIndex);
                _lastGeneratedSequenceBeat = beat.beatIndex;
                _promptsSpawnedThisSequence = 0;
            }

            // Spawn one prompt per beat within the current seq based on spacing pattern
            int promptsThisSequence = Mathf.Min(promptsPerSequence, _sequenceLaneOrder.Count);
            if (_promptsSpawnedThisSequence < promptsThisSequence)
            {
                int beatOffsetInSequence = beat.beatIndex - _currentSequenceStartBeat;

                // Check if this beat should spawn based on spacing
                int promptBeatSpacing = GetCurrentSpacing();
                if (beatOffsetInSequence % promptBeatSpacing == 0)
                {
                    SpawnOnePrompt(beat.beatIndex);
                    _promptsSpawnedThisSequence++;
                }
            }

        }
        private void Update()
        {
            if (beatClock == null) return;

            _totalScrollDistance = beatClock.CurrentBeat * unitsPerBeat * beatSpeedMultiplier;

            // Move the prompts every single frame
            UpdatePrompts();
        }

        private void StartNewSequence(int startBeat)
        {
            if (_activeLanes.Count == 0) return;

            int sequenceId = _nextSequenceId++;
            _currentSequenceStartBeat = startBeat;

            BuildSequenceLaneOrder();

            int promptsThisSequence = Mathf.Min(promptsPerSequence, _sequenceLaneOrder.Count);

            Debug.Log($"[PromptQueue] Beat {startBeat}: START sequence {sequenceId} with {promptsThisSequence} prompts across {_activeLanes.Count} active lanes");

            if (promptJudge != null)
            {
                promptJudge.RegisterSequence(sequenceId, promptsThisSequence);
            }
        }
        private void BuildSequenceLaneOrder()
        {
            _sequenceLaneOrder.Clear();
            _sequenceLaneOrder.AddRange(_activeLanes);

            for (int i = 0; i < _sequenceLaneOrder.Count; i++)
            {
                int j = UnityEngine.Random.Range(i, _sequenceLaneOrder.Count);
                (_sequenceLaneOrder[i], _sequenceLaneOrder[j]) = (_sequenceLaneOrder[j], _sequenceLaneOrder[i]);
            }
        }

        private void SpawnOnePrompt(int beatIndex)
        {
            int sequenceId = _nextSequenceId - 1;  // Current sequence
            int indexInSequence = _promptsSpawnedThisSequence;  // Position in sequence (0, 1, 2, 3)

            if (indexInSequence < 0 || indexInSequence >= _sequenceLaneOrder.Count) return;

            int laneIndex = _sequenceLaneOrder[indexInSequence];

            ElementPose pose = promptSelector.SelectPromptForLane(laneIndex);

            var indicator = Instantiate(promptPrefab, transform);
            indicator.Initialize(pose, _nextPromptId);

            float initialY = spawnOffset.y;
            Vector3 laneSpawnPos = spawnOffset + laneOffsets[laneIndex];
            indicator.transform.localPosition = laneSpawnPos;

            _promptInfo[_nextPromptId] = new PromptInfo
            {
                id = _nextPromptId,
                sequenceId = sequenceId,
                laneIndex = laneIndex,
                initialY = initialY,
                spawnTime = _totalScrollDistance
            };

            _activePrompts.Add(indicator);

            Debug.Log($"[PromptQueue] Beat {beatIndex}: Spawned prompt {_nextPromptId} (seq {sequenceId}, index {indexInSequence}, lane {laneIndex})");

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
                float distance = Mathf.Abs(scrolledY - hitZoneY);

                // Zone detection
                if (!_promptsInZone.Contains(id))
                {
                    if (distance <= hitZoneThreshold)
                    {
                        _promptsInZone.Add(id);
                        prompt.SetInHitZone(true);
                        OnPromptEnteredZone?.Invoke(new PromptData
                        {
                            id = id,
                            sequenceId = info.sequenceId,
                            laneIndex = info.laneIndex,
                            requiredPose = prompt.GetRequiredPose(),
                            currentY = scrolledY
                        });
                    }
                }
                else
                {
                    if (distance > hitZoneThreshold)
                    {
                        _promptsInZone.Remove(id);
                        prompt.SetInHitZone(false);
                        OnPromptExitedZone?.Invoke(new PromptData
                        {
                            id = id,
                            sequenceId = info.sequenceId,
                            laneIndex = info.laneIndex,
                            requiredPose = prompt.GetRequiredPose(),
                            currentY = scrolledY
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

        private int GetCurrentSpacing()
        {
            if (beatClock.CurrentBeat < 60) return 2;
            else return 1;
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
            _sequenceLaneOrder.Clear();
        }

        public float GetPromptCurrentY(int id)
        {
            if (_promptInfo.TryGetValue(id, out var info))
            {
                float scrollAmount = _totalScrollDistance - info.spawnTime;
                return info.initialY - scrollAmount;
            }

            return -999f;
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

        //helper functions to controll when generation begins
        public void BeginGeneration()
        {
            _isGenerating = true;
            _lastGeneratedSequenceBeat = -999;
            Debug.Log("[PromptQueue] Generation started");
        }
        public void StopGeneration()
        {
            _isGenerating = false;
            Debug.Log("[PromptQueue] Generation stopped");
        }

        public void SetActiveLanes(IEnumerable<int> lanes)
        {
            _activeLanes.Clear();

            foreach (var lane in lanes)
            {
                if (lane < 0 || lane >= laneOffsets.Length) continue;
                if (_activeLanes.Contains(lane)) continue;
                _activeLanes.Add(lane);
            }

            Debug.Log($"[PromptQueue] Active lanes set: [{string.Join(", ", _activeLanes)}]");
        }

        public void AddActiveLane(int lane)
        {
            if (lane < 0 || lane >= laneOffsets.Length) return;
            if (_activeLanes.Contains(lane)) return;

            _activeLanes.Add(lane);
            Debug.Log($"[PromptQueue] Added active lane {lane}");
        }
        public void RemoveActiveLane(int lane)
        {
            if (_activeLanes.Remove(lane))
            {
                Debug.Log($"[PromptQueue] Removed active lane {lane}");
            }
        }

        public bool IsActiveLane(int lane)
        {
            return _activeLanes.Contains(lane);
        }
    }
}