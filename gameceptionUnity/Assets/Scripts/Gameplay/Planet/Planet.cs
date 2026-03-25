using UnityEngine;
using InputLayer;
using System;
using System.Collections.Generic;
using UnityEditor.Build;

namespace Gameplay
{
    public class Planet : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PlanetDefinition definition;
        [SerializeField] private DifficultyProfile difficulty;

        [Header("Selection")]
        [SerializeField] private SelectionState selectionState;
        public int planetIndex;

        [Header("References")]
        [SerializeField] private PlanetResourceUI resourceUI;
        [SerializeField] private PlanetNeeds needs;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private AlienSwarmView alienSwarmView;

        [Header("Runtime Population")]
        [SerializeField] private float population = 0f;
        [SerializeField] private float populationGrowthPerSecond = 0.75f;
        [SerializeField] private float populationDeclinePerSecond = 0.5f;

        [Header("Stability Thresholds")]
        [SerializeField, Range(0f, 1f)] private float healthyThreshold = 0.75f;
        [SerializeField, Range(0f, 1f)] private float unstableThreshold = 0.4f;

        [Header("Visual State")]
        [SerializeField] private Color healthyColor = Color.white;
        [SerializeField] private Color dyingColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        [Header("Spawn Animation")]
        [SerializeField] private float growDuration = 0.5f;
        [SerializeField] private AnimationCurve growCurve = null;

        private Vector3 _targetScale;
        private float _growTimer;
        private bool _isGrowing;
        private bool _starterPop;
        private bool _aliensCurrentlyAngry = false;

        public PlanetDefinition Definition => definition;
        public float Population => population;
        public bool IsGrowing => _isGrowing;
        public PlanetNeeds Needs => needs;
        public bool StarterPop => _starterPop;

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

            if (needs == null)
            {
                needs = GetComponent<PlanetNeeds>();
            }

            if(alienSwarmView == null)
            {
                alienSwarmView = GetComponentInChildren<AlienSwarmView>();
            }
            
            Planet[] allPlanets = UnityEngine.Object.FindObjectsByType<Planet>(FindObjectsSortMode.InstanceID);
            // System.Array.Sort(allPlanets, (a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));
            for (int i = 0; i < allPlanets.Length; i++)
            {
                if (allPlanets[i] == this)
                {
                    planetIndex = i;
                    break;
                }
            }

            if (growCurve == null || growCurve.length == 0)
            {
                growCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }

            if (definition != null)
            {
                ApplyDefinition();
            }

            UpdateVisualState();
        }

        private void OnEnable()
        {
            if (selectionState == null){
                selectionState = UnityEngine.Object.FindFirstObjectByType<SelectionState>();
            }
            if (selectionState != null){
                selectionState.OnChanged += HandleSelectionChanged;
            }
        }

        private void OnDisable()
        {
            if (selectionState != null)
                selectionState.OnChanged -= HandleSelectionChanged;
        }

        // private void HandleSelectionChanged(IReadOnlyCollection<int> selectedIndices)
        // {
        //     // UpdateSelectionVisuals();
        //     bool isSelected = selectionState.IsSelected(planetIndex);
        //     if (spriteRenderer != null && definition != null)
        //     {
        //         spriteRenderer.sprite = isSelected ? definition.selectedPlanetSprite : definition.planetSprite;
        //     }
        // }

        // private void UpdateSelectionVisuals()
        private void HandleSelectionChanged(IReadOnlyCollection<int> selectedIndices)
        {
            if (spriteRenderer == null || definition == null || selectionState == null) return;

            bool isSelected = selectionState.IsSelected(planetIndex);
            spriteRenderer.sprite = isSelected ? definition.selectedPlanetSprite : definition.planetSprite;
            if (resourceUI != null)
            {
                resourceUI.SetVisible(isSelected);
            }
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

            float decayMult = difficulty != null ? difficulty.elementDecayMultiplier : 1f;

            if(needs != null)
            {
                needs.Tick(dt, decayMult);
            }

            UpdatePopulation(dt);
            UpdateVisualState();
        }

        //restore one matching need slot on this planet
        public bool RestoreNeed(ElementPose element)
        {
            if (needs == null)
            {
                Debug.LogWarning("[Planet] RestoreNeed called but PlanetNeeds is missing.");
                return false;
            }
            bool restored = needs.RestoreNeed(element);
            if (restored)
            {
                UpdateVisualState();
            }
            return restored;
        }
        public float GetStabilityRatio()
        {
            if (needs == null) return 0f;

            return needs.GetStabilityRatio();
        }
        public bool IsStable()
        {
            return GetStabilityRatio() >= healthyThreshold;
        }
        public bool IsUnstable()
        {
            return GetStabilityRatio() < unstableThreshold;
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

            if (needs != null)
            {
                needs.InitialiseFromDefinition(definition);
            }

            population = Mathf.Clamp(definition.startingPopulation, 0f, definition.populationCap);

            UpdateVisualState();
        }

        private void UpdatePopulation(float dt)
        {
            if (definition == null)
                return;

            float stability = GetStabilityRatio();

            if (stability >= healthyThreshold)
            {
                float growthMultiplier = 1f;
                if (difficulty != null)
                {
                    growthMultiplier = difficulty.populationGrowthMultiplier;
                }

                if (population <= 0f)
                {
                    population = definition.startingPopulation;
                }

                population += populationGrowthPerSecond * growthMultiplier * dt;
            }
            else if (stability < unstableThreshold)
            {
                population -= populationDeclinePerSecond * dt;
            }

            population = Mathf.Clamp(population, 0f, definition.populationCap);
        }
        private void UpdateVisualState()
        {
            if (spriteRenderer == null)
                return;

            float stability = GetStabilityRatio();
            spriteRenderer.color = Color.Lerp(dyingColor, healthyColor, stability);

            bool shouldBeAngry = stability < healthyThreshold;
            if(alienSwarmView != null && shouldBeAngry != _aliensCurrentlyAngry)
            {
                _aliensCurrentlyAngry = shouldBeAngry;
                alienSwarmView.SetAliensAngry(shouldBeAngry);
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
    }
}