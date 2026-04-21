using Gameplay.Choreography;
using InputLayer;
using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;

namespace Gameplay
{
    public class Planet : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PlanetDefinition definition;
        [SerializeField] private DifficultyProfile difficulty;

        [Header("Selection / Lane Mapping")]
        [SerializeField] private SelectionState selectionState;
        [SerializeField] private int planetIndex; // for now same as lane id.

        [Header("References")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private AlienSwarmView alienSwarmView;

        [Header("Population")]
        [SerializeField] private float population = 0f;

        [Header("Choreography State")]
        [SerializeField] private bool choreographyActive = false;
        [SerializeField] private float vitality = 60f;
        [SerializeField] private float maxVitality = 100f;

        [Header("Base Vitality Values")]
        [SerializeField] private float perfectVitalityBase = 10f;
        [SerializeField] private float goodVitalityBase = 6f;
        [SerializeField] private float wrongPoseVitalityBase = -8f;
        [SerializeField] private float noInputVitalityBase = -12f;
        [Header("Base Population Values")]
        [SerializeField] private float perfectPopulationBase = 4f;
        [SerializeField] private float goodPopulationBase = 2f;
        [SerializeField] private float wrongPosePopulationBase = -2f;
        [SerializeField] private float noInputPopulationBase = -4f;
        [Header("Timing Multipliers")]
        [SerializeField] private float perfectTimingMultiplier = 1.2f;
        [SerializeField] private float earlyTimingMultiplier = 1.0f;
        [SerializeField] private float lateTimingMultiplier = 1.0f;

        [Header("Visual Deterioration")]
        [SerializeField] private float healthyScaleMultiplier = 1f;
        [SerializeField] private float dyingScaleMultiplier = 0.9f;

        [Header("Alien Mood Thresholds")]
        [SerializeField, Range(0f, 1f)] private float angryVitalityThreshold = 0.4f;

        [Header("Spawn Animation")]
        [SerializeField] private float growDuration = 1.5f;
        [SerializeField] private AnimationCurve growCurve = null;

        [Header("Feedback")]
        [SerializeField] private ElementBurst elementBurstPrefab;
        [SerializeField] private Transform burstSpawnPoint;
        [SerializeField] private GameObject goodFeedbackPrefab;
        [SerializeField] private GameObject missFeedbackPrefab;
        [SerializeField] private GameObject perfectFeedbackPrefab;
        [SerializeField] private GameObject wrongPlanetFeedbackPrefab;
        [SerializeField] private Transform judgementFeedbackSpawnPoint;
        [SerializeField] private float judgementFeedbackLifetime = 1f;

        [Header("Element Icons")]
        [SerializeField] private Sprite fireIcon;
        [SerializeField] private Sprite waterIcon;
        [SerializeField] private Sprite earthIcon;
        [SerializeField] private Sprite iceIcon;

        [SerializeField] private Color fireColor = new Color32(250,172,140,255);
        [SerializeField] private Color waterColor = new Color32(110,254,247,255);
        [SerializeField] private Color earthColor = new Color32(211, 243, 132, 255);
        [SerializeField] private Color iceColor = new Color32(224,204,253,255);

        private Vector3 _targetScale;
        private float _growTimer;
        private bool _isGrowing;
        private bool _starterPop;
        private bool _aliensCurrentlyAngry = false;

        private PlanetVisualBase _currentVisual;
        private GameObject _currentVisualInstance;

        public PlanetDefinition Definition => definition;
        public float Population => population;
        public bool IsGrowing => _isGrowing;
        public bool StarterPop => _starterPop;
        public int PlanetIndex => planetIndex;
        public bool ChoreographyActive => choreographyActive;
        public float Vitality => vitality;
        public float VitalityNormalized => maxVitality > 0f ? vitality / maxVitality : 0f;

        public AlienType PlanetAlienType => definition != null ? definition.alienType : AlienType.Earth;

        public float GetBodyRadiusWorld()
        {
            if (_currentVisual == null)
            {
                Debug.LogWarning($"[Planet] {name} has no active visual. Using fallback radius.");
                return 0.5f;
            }

            return _currentVisual.GetBodyRadiusWorld();
        }

        private void Awake()
        {
            if (selectionState == null)
            {
                selectionState = FindFirstObjectByType<SelectionState>();
            }

            if (visualRoot == null)
            {
                visualRoot = transform;
            }
            _targetScale = visualRoot.localScale;

            if(alienSwarmView == null)
            {
                alienSwarmView = GetComponentInChildren<AlienSwarmView>();
            }

            if (growCurve == null || growCurve.length == 0)
            {
                growCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }

            if (definition != null)
            {
                ApplyDefinition();
            }

            RefreshAlienMood();
        }

        private void OnEnable()
        {
            if (selectionState == null)
            {
                selectionState = FindFirstObjectByType<SelectionState>();
            }
            if (selectionState != null)
            {
                selectionState.OnChanged += HandleSelectionChanged;
                UpdateSelectionVisuals();
            }
        }

        private void OnDisable()
        {
            if (selectionState != null)
                selectionState.OnChanged -= HandleSelectionChanged;
        }

        private void HandleSelectionChanged(IReadOnlyCollection<int> selectedIndices)
        {
            UpdateSelectionVisuals();
        }
        public void SetPlanetIndex(int index)
        {
            planetIndex = index;
            UpdateSelectionVisuals();
        }

        public void SetDefinition(PlanetDefinition newDefinition)
        {
            definition = newDefinition;
            ApplyDefinition();
        }

        public void SetDifficulty(DifficultyProfile profile)
        {
            difficulty = profile;
        }

        public void BeginSpawnAnimation()
        {
            if (visualRoot == null)
            {
                Debug.LogWarning("[Planet] Cannot begin spawn animation: visualRoot is missing.");
                return;
            }

            _targetScale = visualRoot.localScale;
            visualRoot.localScale = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;

            _growTimer = 0f;
            _isGrowing = true;
        }

        public void Tick(float dt)
        {
            HandleGrowth(dt);

            if (_isGrowing) return;
        }
        public void ActivateChoreography()
        {
            choreographyActive = true;
            RefreshAlienMood();
            RefreshDeteriorationVisuals();
            Debug.Log($"[Planet] Planet {planetIndex} choreography activated");
        }
        public void DeactivateChoreography()
        {
            choreographyActive = false;
            RefreshAlienMood();
            Debug.Log($"[Planet] Planet {planetIndex} choreography deactivated");
        }

        public void SetVitality(float amount)
        {
            vitality = Mathf.Clamp(amount, 0f, maxVitality);
            RefreshAlienMood();
            RefreshDeteriorationVisuals();
        }

        public void AddStarterPopulation(float amount)
        {
            if (definition == null)
                return;

            population += amount;
            population = Mathf.Clamp(population, 0f, definition.populationCap);
            _starterPop = true;
        }
        public void SetPopulation(float amount)
        {
            if (definition == null)
            {
                population = Mathf.Max(0f, amount);
                return;
            }

            population = Mathf.Clamp(amount, 0f, definition.populationCap);
        }
        public void AddPopulation(float amount)
        {
            if (definition == null)
                return;

            population += amount;
            population = Mathf.Clamp(population, 0f, definition.populationCap);
        }

        public void ApplyJudgement(PromptJudge.JudgementResult result)
        {
            if (!choreographyActive)
                return;

            float vitalityDelta = GetVitalityDelta(result);
            float populationDelta = GetPopulationDelta(result);

            vitality += vitalityDelta;
            vitality = Mathf.Clamp(vitality, 0f, maxVitality);

            AddPopulation(populationDelta);

            PlayElementFeedback(result);
            SpawnJudgementFeedback(result);

            RefreshAlienMood();
            RefreshDeteriorationVisuals();

            Debug.Log(
                $"[Planet] Planet {planetIndex} got {result.quality}/{result.timing}. " +
                $"Vitality delta={vitalityDelta:+0.0;-0.0;0.0}, " +
                $"Population delta={populationDelta:+0.0;-0.0;0.0}, " +
                $"Vitality={vitality:F1}/{maxVitality}, Population={population:F1}"
            );
        }
        private void SpawnJudgementFeedback(PromptJudge.JudgementResult result)
        {
            GameObject prefabToSpawn = null;

            if (result.quality == PromptJudge.HitQuality.WrongPlanet)
            {
                prefabToSpawn = wrongPlanetFeedbackPrefab;
                Debug.Log("[Planet] WrongPlanet prefab");
            }
            else if (result.quality == PromptJudge.HitQuality.WrongPose ||
                     result.quality == PromptJudge.HitQuality.NoInput)
            {
                prefabToSpawn = missFeedbackPrefab;
                Debug.Log("[Planet] Miss prefab");
            }
            else if (result.quality == PromptJudge.HitQuality.Perfect &&
                     result.timing == PromptJudge.PoseTiming.Perfect)
            {
                prefabToSpawn = perfectFeedbackPrefab;
                Debug.Log("[Planet] Perfect prefab");
            }
            else
            {
                prefabToSpawn = goodFeedbackPrefab;
                Debug.Log("[Planet] good prefab");
            }

            if (prefabToSpawn == null)
                return;

            Vector3 spawnPos = judgementFeedbackSpawnPoint != null
                ? judgementFeedbackSpawnPoint.position
                : transform.position;

            GameObject instance = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            Debug.Log("[Planet]Feedback instantiated");
            Destroy(instance, judgementFeedbackLifetime);
        }
        private void PlayElementFeedback(PromptJudge.JudgementResult result)
        {
            // Only show for successful hits
            if (result.quality != PromptJudge.HitQuality.Perfect &&
                result.quality != PromptJudge.HitQuality.Good)
                return;

            if (elementBurstPrefab == null || burstSpawnPoint == null)
                return;

            Sprite icon = GetIconForPose(result.detectedPose);
            Color colour = GetColorForPose(result.detectedPose);

            if (icon == null)
                return;

            ElementBurst burst = Instantiate(
                elementBurstPrefab,
                burstSpawnPoint.position,
                Quaternion.identity
            );

            burst.Play(icon, colour);
        }
        private Color GetColorForPose(ElementPose pose)
        {
            switch (pose)
            {
                case ElementPose.Fire: return fireColor;
                case ElementPose.Water: return waterColor;
                case ElementPose.Earth: return earthColor;
                case ElementPose.Ice: return iceColor;
                default: return Color.white;
            }
        }
        private Sprite GetIconForPose(ElementPose pose)
        {
            switch (pose)
            {
                case ElementPose.Fire: return fireIcon;
                case ElementPose.Water: return waterIcon;
                case ElementPose.Earth: return earthIcon;
                case ElementPose.Ice: return iceIcon;
                default: return null;
            }
        }
        public bool IsDead()
        {
            return vitality <= 0f;
        }
        public bool IsStruggling()
        {
            return VitalityNormalized < angryVitalityThreshold;
        }
        private void ApplyDefinition()
        {
            if (definition == null)
            {
                Debug.LogWarning($"[Planet] {name} has no PlanetDefinition assigned.");
                return;
            }

            population = Mathf.Clamp(definition.startingPopulation, 0f, definition.populationCap);

            RebuildVisual();
            UpdateSelectionVisuals();
            RefreshAlienMood();
            RefreshDeteriorationVisuals();
        }
        private void RebuildVisual()
        {
            if(visualRoot == null)
            {
                Debug.LogWarning("[Planet] Cannot rebuild visual: visualRoot is missing.");
                return;
            }
            for(int i = visualRoot.childCount -1; i>=0; i--)
            {
                Destroy(visualRoot.GetChild(i).gameObject);
            }
            _currentVisual = null;
            _currentVisualInstance = null;

            if (definition == null || definition.visualPrefab == null)
            {
                Debug.LogWarning($"[Planet] {name} has no visual prefab in its definition.");
                return;
            }

            _currentVisualInstance = Instantiate(definition.visualPrefab, visualRoot);
            _currentVisualInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _currentVisualInstance.transform.localScale = Vector3.one;

            _currentVisual = _currentVisualInstance.GetComponent<PlanetVisualBase>();

            if (_currentVisual == null)
            {
                Debug.LogError($"[Planet] Visual prefab '{definition.visualPrefab.name}' is missing a PlanetVisualBase component.");
                return;
            }

            _currentVisual.Initialize(definition);
        }
        private void UpdateSelectionVisuals()
        {
            if (definition == null || selectionState == null) return;

            bool isSelected = selectionState.IsSelected(planetIndex);

            if (_currentVisual != null)
            {
                _currentVisual.SetSelected(isSelected);
            }
        }

        private void RefreshAlienMood()
        {
            if (alienSwarmView == null)
                return;

            bool shouldBeAngry = choreographyActive && VitalityNormalized < angryVitalityThreshold;

            if (shouldBeAngry != _aliensCurrentlyAngry)
            {
                _aliensCurrentlyAngry = shouldBeAngry;
                alienSwarmView.SetAliensAngry(shouldBeAngry);
            }
        }
        private void RefreshDeteriorationVisuals()
        {
            float t = VitalityNormalized;

            if (_currentVisual != null)
            {
                _currentVisual.SetVitality(t);
            }

            if (visualRoot != null && !_isGrowing)
            {
                float scaleMult = Mathf.Lerp(dyingScaleMultiplier, healthyScaleMultiplier, t);
                visualRoot.localScale = _targetScale * scaleMult;
            }
        }

        private void HandleGrowth(float dt)
        {
            if (!_isGrowing) return;

            _growTimer += dt;
            float t = Mathf.Clamp01(_growTimer / growDuration);

            float curvedT = growCurve.Evaluate(t);
            visualRoot.localScale = Vector3.Lerp(Vector3.zero, _targetScale, curvedT);

            if (t >= 1f)
            {
                visualRoot.localScale = _targetScale;
                visualRoot.localRotation = Quaternion.identity;
                _isGrowing = false;

                RefreshDeteriorationVisuals();

                Debug.Log("[Planet] Growth complete");
            }
        }

        private float GetVitalityDelta(PromptJudge.JudgementResult result)
        {
            float baseValue = result.quality switch
            {
                PromptJudge.HitQuality.Perfect => perfectVitalityBase,
                PromptJudge.HitQuality.Good => goodVitalityBase,
                PromptJudge.HitQuality.WrongPose => wrongPoseVitalityBase,
                PromptJudge.HitQuality.NoInput => noInputVitalityBase,
                _ => 0f
            };

            float timingMultiplier = GetTimingMultiplier(result.timing);

            bool isReward = baseValue > 0f;
            float difficultyMultiplier = 1f;

            if (difficulty != null)
            {
                difficultyMultiplier = isReward
                    ? difficulty.vitalityRewardMultiplier
                    : difficulty.vitalityPenaltyMultiplier;
            }

            return baseValue * timingMultiplier * difficultyMultiplier;
        }

        private float GetPopulationDelta(PromptJudge.JudgementResult result)
        {
            float baseValue = result.quality switch
            {
                PromptJudge.HitQuality.Perfect => perfectPopulationBase,
                PromptJudge.HitQuality.Good => goodPopulationBase,
                PromptJudge.HitQuality.WrongPose => wrongPosePopulationBase,
                PromptJudge.HitQuality.NoInput => noInputPopulationBase,
                _ => 0f
            };

            float timingMultiplier = GetTimingMultiplier(result.timing);

            bool isReward = baseValue > 0f;
            float difficultyMultiplier = 1f;

            if (difficulty != null)
            {
                difficultyMultiplier = isReward
                    ? difficulty.populationRewardMultiplier
                    : difficulty.populationPenaltyMultiplier;
            }

            return baseValue * timingMultiplier * difficultyMultiplier;
        }
        private float GetTimingMultiplier(PromptJudge.PoseTiming timing)
        {
            switch (timing)
            {
                case PromptJudge.PoseTiming.Perfect:
                    return perfectTimingMultiplier;

                case PromptJudge.PoseTiming.Early:
                    return earlyTimingMultiplier;

                case PromptJudge.PoseTiming.Late:
                    return lateTimingMultiplier;

                default:
                    return 1f;
            }
        }

    }
}