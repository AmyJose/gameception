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

    //call this from PlanetManager each frame
    public void Tick(float dt)
    {
        //natural element decay
        float decayMult = difficulty != null ? difficulty.elementDecayMultiplier : 1f;
        ApplyDecay(dt, elementDecayPerSecond * decayMult);

        //alien consumption
        float conMult = difficulty != null ? difficulty.consumptionMultiplier : 1f;
        ApplyConsumption(dt, consumptionPerAlienPerSecond * conMult);

        //check habitability and grow pop
        UpdatePopulation(dt);
        ClampAll();
    }

    public void ApplyElement(ElementPose element, float amount = 10f)
    {
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
}
