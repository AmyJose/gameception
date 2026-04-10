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
        [SerializeField] private PlanetResourceUI resourceUI;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private AlienSwarmView alienSwarmView;

        [Header("Population")]
        [SerializeField] private float population = 0f;

        [Header("Choreography State")]
        [SerializeField] private bool choreographyActive = false;
        [SerializeField] private float vitality = 60f;
        [SerializeField] private float maxVitality = 100f;

        [Header("Judgement Effects")]
        [SerializeField] private float perfectVitalityGain = 12f;
        [SerializeField] private float goodVitalityGain = 7f;
        [SerializeField] private float wrongPoseVitalityLoss = 8f;
        [SerializeField] private float noInputVitalityLoss = 12f;

        [Header("Visual Deterioration")]
        [SerializeField] private Color healthyColor = Color.white;
        [SerializeField] private Color strugglingColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        [SerializeField] private Color dyingColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        [SerializeField] private float healthyScaleMultiplier = 1f;
        [SerializeField] private float dyingScaleMultiplier = 0.9f;

        [Header("Population Rewards")]
        [SerializeField] private float perfectPopulationGain = 2f;
        [SerializeField] private float goodPopulationGain = 1f;
        [SerializeField] private float wrongPosePopulationLoss = 0.5f;
        [SerializeField] private float noInputPopulationLoss = 1f;

        [Header("Alien Mood Thresholds")]
        [SerializeField, Range(0f, 1f)] private float angryVitalityThreshold = 0.4f;

        [Header("Spawn Animation")]
        [SerializeField] private float growDuration = 1.5f;
        [SerializeField] private AnimationCurve growCurve = null;

        private Vector3 _targetScale;
        private float _growTimer;
        private bool _isGrowing;
        private bool _starterPop;
        private bool _aliensCurrentlyAngry = false;

        public PlanetDefinition Definition => definition;
        public float Population => population;
        public bool IsGrowing => _isGrowing;
        public bool StarterPop => _starterPop;
        public int PlanetIndex => planetIndex;
        public bool ChoreographyActive => choreographyActive;
        public float Vitality => vitality;
        public float VitalityNormalized => maxVitality > 0f ? vitality / maxVitality : 0f;

        public AlienType PlanetAlienType => definition != null ? definition.alienType : AlienType.Earth;

        private void Awake()
        {   
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (selectionState == null)
            {
                selectionState = UnityEngine.Object.FindFirstObjectByType<SelectionState>();
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

            if (choreographyActive) TickChoreography(dt);
        }

        private void TickChoreography(float dt)
        {
           //no passive drain
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

            RefreshAlienMood();
            RefreshDeteriorationVisuals();

            Debug.Log(
                $"[Planet] Planet {planetIndex} got {result.quality}/{result.timing}. " +
                $"Vitality delta={vitalityDelta:+0.0;-0.0;0.0}, " +
                $"Population delta={populationDelta:+0.0;-0.0;0.0}, " +
                $"Vitality={vitality:F1}/{maxVitality}, Population={population:F1}"
            );
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

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = definition.planetSprite;
            }

            population = Mathf.Clamp(definition.startingPopulation, 0f, definition.populationCap);

            UpdateSelectionVisuals();
            RefreshAlienMood();
            RefreshDeteriorationVisuals();
        }
        private void UpdateSelectionVisuals()
        {
            if (spriteRenderer == null || definition == null || selectionState == null) return;

            bool isSelected = selectionState.IsSelected(planetIndex);

            Sprite targetSprite = isSelected && definition.selectedPlanetSprite != null
                ? definition.selectedPlanetSprite
                : definition.planetSprite;

            spriteRenderer.sprite = targetSprite;

            if (resourceUI != null)
            {
                resourceUI.SetVisible(isSelected);
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
            if (spriteRenderer == null)
                return;

            float t = VitalityNormalized;

            Color targetColor;
            if (t >= 0.5f)
            {
                float lerp = Mathf.InverseLerp(0.5f, 1f, t);
                targetColor = Color.Lerp(strugglingColor, healthyColor, lerp);
            }
            else
            {
                float lerp = Mathf.InverseLerp(0f, 0.5f, t);
                targetColor = Color.Lerp(dyingColor, strugglingColor, lerp);
            }

            spriteRenderer.color = targetColor;

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

            visualRoot.localScale = Vector3.Lerp(Vector3.zero, _targetScale, t);

            if (t >= 1f)
            {
                visualRoot.localScale = _targetScale;
                visualRoot.localRotation = Quaternion.identity;
                _isGrowing = false;
                Debug.Log("[Planet] Growth complete");
            }
        }

        private float GetVitalityDelta(PromptJudge.JudgementResult result)
        {
            switch (result.quality)
            {
                case PromptJudge.HitQuality.Perfect:
                    return result.timing == PromptJudge.PoseTiming.Perfect ? 12f : 9f;

                case PromptJudge.HitQuality.Good:
                    return result.timing == PromptJudge.PoseTiming.Perfect ? 7f : 5f;

                case PromptJudge.HitQuality.WrongPose:
                    return result.timing == PromptJudge.PoseTiming.Perfect ? -6f : -8f;

                case PromptJudge.HitQuality.NoInput:
                    return -12f;

                default:
                    return 0f;
            }
        }

        private float GetPopulationDelta(PromptJudge.JudgementResult result)
        {
            switch (result.quality)
            {
                case PromptJudge.HitQuality.Perfect:
                    return result.timing == PromptJudge.PoseTiming.Perfect ? 4f : 3f;

                case PromptJudge.HitQuality.Good:
                    return result.timing == PromptJudge.PoseTiming.Perfect ? 2f : 1f;

                case PromptJudge.HitQuality.WrongPose:
                    return result.timing == PromptJudge.PoseTiming.Perfect ? -2f : -3f;

                case PromptJudge.HitQuality.NoInput:
                    return -4f;

                default:
                    return 0f;
            }
        }
    }
}