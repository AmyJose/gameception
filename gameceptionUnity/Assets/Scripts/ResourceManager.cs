using UnityEngine;
using System.Collections.Generic;
// Hanfles decay logic
// Key-presses
public class ResourceManager: MonoBehaviour
{
    private readonly List<Planet> planets = new();
    private int idx=0;
    private Planet selected;
    // key press logic

    void Awake()
    {   
        var gameObj = new GameObject($"Planet_{0}");
        Planet p = gameObj.AddComponent<Planet>();
        planets.Add(p);
        selected = planets[0];
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            idx = (idx + 1) % planets.Count;
            selected = planets[idx];
        }

        if (Input.GetKeyDown(KeyCode.W)) selected.AddWater(1f);
        if (Input.GetKeyDown(KeyCode.E)) selected.AddEarth(1f);
        if (Input.GetKeyDown(KeyCode.F)) selected.AddFire(1f);
        if (Input.GetKeyDown(KeyCode.A)) selected.AddAir(1f);

        float dt = Time.deltaTime;
        foreach (var p in planets)
        {
            // optional decay (example)
            p.elements.water = Mathf.Max(0f, p.elements.water - 0.1f * dt);
            p.elements.earth = Mathf.Max(0f, p.elements.earth - 0.1f * dt);
            p.elements.fire  = Mathf.Max(0f, p.elements.fire  - 0.1f * dt);
            p.elements.air   = Mathf.Max(0f, p.elements.air   - 0.1f * dt);

            // compute habitability
            p.habitability = HabitabilityLogic.Compute(p.elements);
            p.population += (p.habitability - 0.5f) * 10f * dt;
            p.population = Mathf.Max(0f, p.population);
        }

    }
}
