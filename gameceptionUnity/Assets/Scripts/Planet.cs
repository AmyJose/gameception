using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class Planet : MonoBehaviour
{
    public ElementState elements = new ElementState();
    public ResourceBarTracker waterBar;
    public ResourceBarTracker earthBar;
    public ResourceBarTracker airBar;
    public ResourceBarTracker fireBar;
    public float Habitability => HabitabilityLogic.Compute(elements);
    //public float population;
    public AlienManager inhabitants;

    [SerializeField, Tooltip("Computed habitability (read-only)")]
    private float habitabilityDebug;

    [SerializeField, Tooltip("Current population (read-only)")]
    private int populationDebug;

    public ElementEffect fireEffect;
    public ElementEffect waterEffect;
    public ElementEffect airEffect;
    public ElementEffect earthEffect;

    public void AddAir(float amount)
    {
        elements.air = Math.Min(elements.air + amount, 100);
        airBar.SetValue(elements.air);
    }
    public void AddWater(float amount)
    {
        elements.water = Math.Min(elements.water + amount, 100);
        waterBar.SetValue(elements.water);
    }
    public void AddFire(float amount)
    {
        elements.fire = Math.Min(elements.fire + amount, 100);
        fireBar.SetValue(elements.fire);
    }
    public void AddEarth(float amount)
    {
        elements.earth = Math.Min(elements.earth + amount, 100);
        earthBar.SetValue(elements.earth);
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
        /*var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.wKey.wasPressedThisFrame) waterEffect.Activate();
        if (kb.eKey.wasPressedThisFrame) earthEffect.Activate();
        if (kb.fKey.wasPressedThisFrame) fireEffect.Activate();
        if (kb.aKey.wasPressedThisFrame) airEffect.Activate();*/

    }
    public void GeneralDecay(float dt)
    {
        // optional decay (example)
        elements.water = Mathf.Max(0f, elements.water - 0.1f * dt);
        elements.earth = Mathf.Max(0f, elements.earth - 0.1f * dt);
        elements.fire = Mathf.Max(0f, elements.fire - 0.1f * dt);
        elements.air = Mathf.Max(0f, elements.air - 0.1f * dt);

        waterBar.SetValue(elements.water);
        earthBar.SetValue(elements.earth);
        airBar.SetValue(elements.air);
        fireBar.SetValue(elements.fire);

    }



}
