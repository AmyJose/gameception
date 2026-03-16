using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using InputLayer;

public class Planet : MonoBehaviour
{
    [SerializeField] private PlanetDefinition definition;
    [SerializeField] private DifficultyProfile difficulty;

    //runtime element amounts
    [SerializeField] private float fire;
    [SerializeField] private float water;
    [SerializeField] private float earth;
    [SerializeField] private float ice;

    //runtime population
    [SerializeField] private float population = 0f;

    //base rates (before difficulty profile multiplication)
    [SerializeField] private float elementDecayPerSecond = 0.6f;
    [SerializeField] private float consumptionPerAlienPerSecond = 0.01f;
    [SerializeField] private float populationGrowthPerSecond = 1.2f;
    [SerializeField] private float maxElement = 100f;

    public PlanetDefinition Definition => definition;
    public float Population => population;
    public AlienType PlanetAlienType => definition.alienType;

    public void SetDifficulty(DifficultyProfile profile) => difficulty = profile;

    private bool _frozen = false;
    private float _freezeTimer = 0f;

    private string _activeEffect = null;
    private float _effectTimer = 0f;
    private float _effectTickTimer = 0f;
    [SerializeField] private float effectTickInterval = 2f;

    //call this from PlanetManager each frame
    public void Tick(float dt)
    {
        if (_frozen)
        {
            _freezeTimer -= dt;
            if (_freezeTimer <= 0f)
                _frozen = false;
            return; 
        }
        //natural element decay
        float decayMult = difficulty != null ? difficulty.elementDecayMultiplier : 1f;
        ApplyDecay(dt, elementDecayPerSecond * decayMult);

        //alien consumption
        float conMult = difficulty != null ? difficulty.consumptionMultiplier : 1f;
        ApplyConsumption(dt, consumptionPerAlienPerSecond * conMult);

        //check habitability and grow pop
        UpdatePopulation(dt);
        UpdateEffects(dt);
        ClampAll();
    }

    public void ApplyElement(ElementPose element, float amount = 10f)
    {
        if (_frozen) return; //can't apply elements while frozen
        switch (element) 
        {
            case ElementPose.Water: water += amount; break;
            case ElementPose.Fire: fire += amount; break;
            case ElementPose.Earth: earth += amount; break;
            case ElementPose.Ice: ice += amount; break;
            default: break;
        }
        ClampAll();
    }

    private void ApplyDecay(float dt, float decayRate)
    {
        fire -= decayRate * dt;
        water -= decayRate * dt;
        earth -= decayRate * dt;
        ice -= decayRate * dt;
    }

    private void ApplyConsumption(float dt, float perAlienRate)
    {
        if (population <= 0f) return;

        //consumption scales with population
        float total = population * perAlienRate * dt;

        //aliens consume all resources evenly
        fire -= total;
        water -= total;
        earth -= total;
        ice -= total;
    }

    private void UpdatePopulation(float dt)
    {
        if (definition == null) return;

        bool habitable = IsHabitable();

        if (habitable)
        {
            if (population <= 0f)
            {
                population = definition.startingPopulation;
            }

            float growthMult = difficulty != null ? difficulty.populationGrowthMultiplier : 1f;
            population += populationGrowthPerSecond * growthMult * dt;
        }
        else
        {
            population -= (populationGrowthPerSecond*0.5f) * dt;
        }
        population = Mathf.Clamp(population, 0f, definition.populationCap);
    }

    private bool IsHabitable()
    {
        float tolerance = definition.tolerance;

        bool okFire = Mathf.Abs(fire - definition.targetFire) <= tolerance;
        bool okWater = Mathf.Abs(water - definition.targetWater) <= tolerance;
        bool okEarth = Mathf.Abs(earth - definition.targetEarth) <= tolerance;
        bool okIce = Mathf.Abs(ice - definition.targetIce) <= tolerance;

        return okFire && okWater && okEarth && okIce;
    }
    private void ClampAll()
    {
        fire = Mathf.Clamp(fire, 0f, maxElement);
        water = Mathf.Clamp(water, 0f, maxElement);
        earth = Mathf.Clamp(earth, 0f, maxElement);
        ice = Mathf.Clamp(ice, 0f, maxElement);
    }

    public void ApplyComboEffect(string recipe_name){
        switch(recipe_name){
            case "Permafrost":
                ice += maxElement * 0.3f;
                _frozen = true;
                _freezeTimer = 5f; //freezes decay or input for 5 seconds
                ClampAll();
                break;
            case "Lava":
                fire += maxElement * 0.3f;
                _activeEffect = "Lava";
                _effectTimer = 10f;
                _effectTickTimer = effectTickInterval;
                ClampAll();
                break;
            case "Ecosystem":
                earth += maxElement * 0.3f;
                ClampAll();
                //TODO: implement animals, population boost, faster element decay, chance of random element spawn
                //TODO : animals spawned are all common
                break;

            case "Air":
                // TODO : implement air that boosts population growth and slightly increases decay for a duration, chance to spawn rare birds that generates air at a rate
                _activeEffect = "Air";
                _effectTimer = 8f;
                _effectTickTimer = effectTickInterval;
                break;

            case "Flood":
                water += maxElement * 0.3f;
                _activeEffect = "Flood";
                _effectTimer = 10f;
                _effectTickTimer = effectTickInterval;
                ClampAll();
                break;

            case "Snow":
                ice += maxElement * 0.3f;
                _activeEffect = "Snow";
                _effectTimer = 12f;
                elementDecayPerSecond *= 0.5f;
                ClampAll();
                break;
                // TODO : slow down elementdecay and growth for a duration, chance to spawn rare snowmen
            default:
                Debug.LogWarning($"Unknown combo recipe: {recipe_name}");
                break;

        }
    }

    private void UpdateEffects(float dt)
    {
        if (_activeEffect == null) return;

        _effectTimer -= dt;
        _effectTickTimer -= dt;

        if (_effectTickTimer <= 0f)
        {
            _effectTickTimer = effectTickInterval;
            OnEffectTick(_activeEffect);
        }

        if (_effectTimer <= 0f)
        {
            if (_activeEffect == "Snow")
                elementDecayPerSecond /= 0.5f;
            Debug.Log($"Effect {_activeEffect} ended");
            _activeEffect = null;
        }
    }
    private void OnEffectTick(string effect)
    {
        switch (effect)
        {
            case "Lava":
                // randomly damages one non-fire element
                float lavaDamage = UnityEngine.Random.Range(5f, 15f);
                int target = UnityEngine.Random.Range(0, 3);
                if (target == 0) water -= lavaDamage;
                else if (target == 1) earth -= lavaDamage;
                else ice -= lavaDamage;
                population -= populationGrowthPerSecond * 0.5f; //population hit from volcanic activity

                // rare animal chance
                if (UnityEngine.Random.value < 0.1f)
                {
                    Debug.Log("Rare volcanic creature spawned! Population boost");
                    population += 50f;
                }
                ClampAll();
                break;

            case "Flood":
                float floodDamage = UnityEngine.Random.Range(3f, 10f);
                fire -= floodDamage;
                earth -= floodDamage;
                if (UnityEngine.Random.value < 0.1f)
                {
                    Debug.Log("Rare construct spawned! Decay halted");
                    elementDecayPerSecond = 0f;
                }
                population -= populationGrowthPerSecond * 0.5f;
                ClampAll();
                break;

            case "Air":
                population += populationGrowthPerSecond * 0.5f;
                if (UnityEngine.Random.value < 0.1f)
                {
                    Debug.Log("Rare bird spawned! Cleaning elements");
                    fire = Mathf.Max(0f, fire - 10f);
                    ice = Mathf.Max(0f, ice - 10f);
                }
                break;

            case "Snow":
                // slow tick — handled via decay multiplier, nothing random here
                break;
        }
    }
    

    
}
