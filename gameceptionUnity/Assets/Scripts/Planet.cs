using Unity.VisualScripting;
using UnityEngine;

public class Planet : MonoBehaviour
{
    public ElementState elements = new ElementState();
    public float Habitability => HabitabilityLogic.Compute(elements);
    //public float population;
    public AlienManager inhabitants;

    [SerializeField, Tooltip("Computed habitability (read-only)")]
    private float habitabilityDebug;

    [SerializeField, Tooltip("Current population (read-only)")]
    private int populationDebug;


    public void AddAir(float amount)
    {
        elements.air += amount;
    }
    public void AddWater(float amount)
    {
        elements.water += amount;
    }
    public void AddFire(float amount)
    {
        elements.fire += amount;
    }
    public void AddEarth(float amount)
    {
        elements.earth += amount;
    }

    void Awake()
    {
        inhabitants = GetComponent<AlienManager>();
        elements.air = 5f;
        elements.water = 5f;
        elements.fire = 5f;
        elements.earth = 5f;
    }


    void Update()
    {
        GeneralDecay(Time.deltaTime);
        habitabilityDebug = Habitability;
        populationDebug = inhabitants != null ? inhabitants.Population : 0;
        inhabitants.UpdateAliens(Time.deltaTime, Habitability);
    }
    public void GeneralDecay(float dt)
    {
        // optional decay (example)
        elements.water = Mathf.Max(0f, elements.water - 0.1f * dt);
        elements.earth = Mathf.Max(0f, elements.earth - 0.1f * dt);
        elements.fire = Mathf.Max(0f, elements.fire - 0.1f * dt);
        elements.air = Mathf.Max(0f, elements.air - 0.1f * dt);

    }



}
