using System;
using System.Collections.Generic;
using InputLayer;
using UnityEngine;
using Rhythm;
using Unity.VisualScripting;

namespace Gameplay.Choreography
{
    [Serializable]
    public class LanePromptConfig
    {
        public int laneIndex;
        public int promptsPerSequence = 1;
    }

    public class PromptQueue : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PromptJudge promptJudge;
        [SerializeField] private GameFlowController gameFlowController;

        [Header("Queue Layout")]
        [SerializeField] public float hitZoneY = 0f;
        [SerializeField] public float hitZoneThreshold = 0.8f;

        [Header("Spawn")]
        [SerializeField] private PromptIndicator promptPrefab;
        [SerializeField] public Vector3 spawnOffset = Vector3.zero;
        [SerializeField]
        private Vector3[] laneOffsets = new Vector3[4]
        {
            new Vector3(-8f, 0f, 0f), // Lane 0: Left
            new Vector3(-6f, 0f, 0f), // Lane 1
            new Vector3(-4f, 0f, 0f), // Lane 2
            new Vector3(-2f, 0f, 0f) // Lane 3: Right
        };
        [SerializeField] private bool autoStartGeneration = false;

        [Header("Scroll")]
        [SerializeField] private float unitsPerBeat = 1f;
        [SerializeField] private float beatSpeedMultiplier = 1f;
        [SerializeField] private BeatClock beatClock;

        [Header("Generation")]
        [SerializeField] private int generationIntervalBeats = 10;
        [SerializeField] private int promptSpawnBeatSpacing = 2; // within a sequence
        [SerializeField] private bool keepLanePromptsConsecutive = true;
        [SerializeField]
        private LanePromptConfig[] laneConfigs = new LanePromptConfig[4]
        {
            new LanePromptConfig { laneIndex = 0, promptsPerSequence = 1 },
            new LanePromptConfig { laneIndex = 1, promptsPerSequence = 1 },
            new LanePromptConfig { laneIndex = 2, promptsPerSequence = 1 },
            new LanePromptConfig { laneIndex = 3, promptsPerSequence = 1 }
        };

        [SerializeField] private PromptSelector promptSelector;

        [Header("Scripted Intro")]
        [SerializeField] private PromptSequenceAsset scriptedIntroSequence;
        [SerializeField] private float scriptedIntroDurationSeconds = 0f;
        [SerializeField] private bool loopScriptedIntroSequence = true;

        // Events
        public event Action<PromptData> OnPromptEnteredZone;
        public event Action<PromptData> OnPromptExitedZone;

        public struct PromptData
        {
            public int id;
            public int sequenceId;
            public int laneIndex;
            public ElementPose requiredPose;
            public float currentY;
        }

        private struct PromptInfo
        {
            public int id;
            public int sequenceId;
            public int laneIndex;
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
        public int LaneCount => laneOffsets != null ? laneOffsets.Length : 0;

        private int _introPoseCursor = 0;

        private int _lastPromptSpawnBeat = -999;  // Tracks when last prompt was spawned
        private int _promptsExpectedThisSequence = 0; // How many prompts should spawn this sequence

        private void OnEnable()
        {
            if (beatClock != null)
                beatClock.OnBeat += HandleBeat;



            if (autoStartGeneration)
                BeginGeneration();
            else
                _isGenerating = false;
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

            // Check if we should start a new sequence
            // Condition: either first sequence or enough beats after last prompt spawned
            bool shouldGenerateNewSequence = (_lastPromptSpawnBeat == -999) || (beat.beatIndex >= _lastPromptSpawnBeat + generationIntervalBeats);

            if (shouldGenerateNewSequence)
            {
                StartNewSequence(beat.beatIndex);
                _promptsSpawnedThisSequence = 0;
            }

            // Spawn prompts from current sequence
            int promptsThisSequence = _sequenceLaneOrder.Count;
            if (_promptsSpawnedThisSequence < promptsThisSequence)
            {
                int beatOffsetInSequence = beat.beatIndex - _currentSequenceStartBeat;

                if (beatOffsetInSequence % promptSpawnBeatSpacing == 0)
                {
                    if (TryGetScriptedIntroPose(out var scriptedPose))
                    {
                        SpawnOnePrompt(beat.beatIndex, scriptedPose);
                    }
                    else
                    {
                        Debug.Log("Random generation started");
                        SpawnOnePrompt(beat.beatIndex);
                    }
                    _promptsSpawnedThisSequence++;
                    _lastPromptSpawnBeat = beat.beatIndex;
                }
            }
        }

        private void Update()
        {
            if (beatClock == null) return;

            _totalScrollDistance = beatClock.CurrentBeat * unitsPerBeat * beatSpeedMultiplier;
            UpdatePrompts();
        }

        private void StartNewSequence(int startBeat)
        {
            if (_activeLanes.Count == 0) return;

            int sequenceId = _nextSequenceId++;
            _currentSequenceStartBeat = startBeat;

            BuildSequenceLaneOrder();

            int promptsThisSequence = _sequenceLaneOrder.Count;
            _promptsExpectedThisSequence = promptsThisSequence;  // Store expected count

            Debug.Log($"[PromptQueue] Beat {startBeat}: START sequence {sequenceId} with {promptsThisSequence} prompts | Next sequence eligible at beat {_lastPromptSpawnBeat + generationIntervalBeats}");

            if (promptJudge != null)
            {
                promptJudge.RegisterSequence(sequenceId, promptsThisSequence);
            }
        }

        private void BuildSequenceLaneOrder()
        {
            _sequenceLaneOrder.Clear();

            if (keepLanePromptsConsecutive)
            {
                // Build groups (keep lane prompts together)
                var laneGroups = new List<List<int>>();

                foreach (int laneIndex in _activeLanes)
                {
                    int count = laneConfigs[laneIndex].promptsPerSequence;
                    var group = new List<int>();
                    for (int i = 0; i < count; i++)
                    {
                        group.Add(laneIndex);
                    }
                    laneGroups.Add(group);
                }

                // Shuffle groups
                for (int i = 0; i < laneGroups.Count; i++)
                {
                    int j = UnityEngine.Random.Range(i, laneGroups.Count);
                    (laneGroups[i], laneGroups[j]) = (laneGroups[j], laneGroups[i]);
                }

                // Flatten
                foreach (var group in laneGroups)
                {
                    _sequenceLaneOrder.AddRange(group);
                }

                Debug.Log($"[PromptQueue] Lane order (grouped): [{string.Join(", ", _sequenceLaneOrder)}]");
            }
            else
            {
                // Original behavior: shuffle individual prompts
                foreach (int laneIndex in _activeLanes)
                {
                    int count = laneConfigs[laneIndex].promptsPerSequence;
                    for (int i = 0; i < count; i++)
                    {
                        _sequenceLaneOrder.Add(laneIndex);
                    }
                }

                // Shuffle individual prompts
                for (int i = 0; i < _sequenceLaneOrder.Count; i++)
                {
                    int j = UnityEngine.Random.Range(i, _sequenceLaneOrder.Count);
                    (_sequenceLaneOrder[i], _sequenceLaneOrder[j]) = (_sequenceLaneOrder[j], _sequenceLaneOrder[i]);
                }

                Debug.Log($"[PromptQueue] Lane order (shuffled): [{string.Join(", ", _sequenceLaneOrder)}]");
            }
        }

        private void SpawnOnePrompt(int beatIndex, ElementPose? forcedPose = null)
        {
            int sequenceId = _nextSequenceId - 1;
            int indexInSequence = _promptsSpawnedThisSequence;

            if (indexInSequence < 0 || indexInSequence >= _sequenceLaneOrder.Count) return;

            int laneIndex = _sequenceLaneOrder[indexInSequence];

            ElementPose pose = forcedPose ?? promptSelector.SelectPromptForLane(laneIndex);

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

                float scrollAmount = _totalScrollDistance - info.spawnTime;
                float scrolledY = info.initialY - scrollAmount;

                prompt.SetYPosition(scrolledY);
                float distance = Mathf.Abs(scrolledY - hitZoneY);

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

        public void BeginGeneration()
        {
            _isGenerating = true;
            _lastGeneratedSequenceBeat = -999;
            _introPoseCursor = 0;
            Debug.Log("[PromptQueue] Generation started");
        }

        public void StopGeneration()
        {
            _isGenerating = false;
            Debug.Log("[PromptQueue] Generation stopped");
        }

        private bool TryGetScriptedIntroPose(out ElementPose pose)
        {
            pose = default;

            if (scriptedIntroSequence == null) return false;
            if (scriptedIntroSequence.steps == null || scriptedIntroSequence.steps.Count == 0) return false;
            if (scriptedIntroDurationSeconds <= 0f) return false;
            if (gameFlowController == null) return false;

            float remainingTime = gameFlowController.RemainingTime;
            float introWindowStartRemaining = Mathf.Max(0f, gameFlowController.RunDurationSeconds - scriptedIntroDurationSeconds);

            if (remainingTime < introWindowStartRemaining)
            {
                _introPoseCursor = 0;
                return false;
            }

            int count = scriptedIntroSequence.steps.Count;
            if (_introPoseCursor >= count)
            {
                if (!loopScriptedIntroSequence) return false;
                _introPoseCursor = 0;
            }

            pose = scriptedIntroSequence.steps[_introPoseCursor].pose;
            _introPoseCursor++;
            return true;
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

        public int GetActiveLaneCount()
        {
            return _activeLanes.Count;
        }

        public void SetGenerationIntervalBeats(int beats)
        {
            generationIntervalBeats = Mathf.Max(1, beats);
            Debug.Log($"[PromptQueue] Generation interval -> {generationIntervalBeats} beats");
        }

        public void SetPromptSpawnBeatSpacing(int spacing)
        {
            promptSpawnBeatSpacing = Mathf.Max(1, spacing);
            Debug.Log($"[PromptQueue] Prompt spawn beat spacing -> {promptSpawnBeatSpacing}");
        }

        public void SetKeepLanePromptsConsecutive(bool keep)
        {
            keepLanePromptsConsecutive = keep;
            Debug.Log($"[PromptQueue] Keep lane prompts consecutive -> {keep}");
        }

        public void SetLanePromptsPerSequence(int laneIndex, int count)
        {
            if (laneIndex < 0 || laneIndex >= laneConfigs.Length) return;
            laneConfigs[laneIndex].promptsPerSequence = Mathf.Max(0, count);
            Debug.Log($"[PromptQueue] Lane {laneIndex} -> {count} prompts per sequence");
        }

        public void SetAllLanesPromptsPerSequence(int count)
        {
            count = Mathf.Max(0, count);
            foreach (var config in laneConfigs)
            {
                config.promptsPerSequence = count;
            }
            Debug.Log($"[PromptQueue] All lanes -> {count} prompts per sequence");
        }

        public bool TryGetLaneCenterLocalPosition(int laneIndex, out Vector3 centerLocal)
        {
            centerLocal = Vector3.zero;

            if (laneOffsets == null) return false;
            if (laneIndex < 0 || laneIndex >= laneOffsets.Length) return false;

            centerLocal = spawnOffset + laneOffsets[laneIndex];
            return true;
        }

        public bool TryGetLaneBoundaryLocalX(int boundaryIndex, out float boundaryX)
        {
            boundaryX = 0f;

            if (!TryGetLaneCenterLocalPosition(boundaryIndex, out var leftCenter)) return false;
            if (!TryGetLaneCenterLocalPosition(boundaryIndex + 1, out var rightCenter)) return false;

            boundaryX = (leftCenter.x + rightCenter.x) * 0.5f;
            return true;
        }
    }
}