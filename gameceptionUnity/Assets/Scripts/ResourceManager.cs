using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
// Hanfles decay logic
// Key-presses
public class ResourceManager : MonoBehaviour
{
    private readonly List<Planet> planets = new();
    private int idx = 0;
    private Planet selected;
    [SerializeField] private Planet planetPrefab;
    // key press logic

    void Awake()
    {
        //Create initial planet
        Planet p = Instantiate(planetPrefab, Vector3.zero, Quaternion.identity);
        p.name = $"Planet_{planets.Count}";
        planets.Add(p);
        selected = p;

        ArrangePlanets();
        HighlightSelected();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        //switch planets with TAB
        if (kb.tabKey.wasPressedThisFrame)
        {
            if (planets.Count == 0) return;

            idx = (idx + 1) % planets.Count;
            selected = planets[idx];
            HighlightSelected();
        }

        //create new planet with T
        if (kb.tKey.wasPressedThisFrame)
        {
            Planet p = Instantiate(planetPrefab, Vector3.zero, Quaternion.identity);
            p.name = $"Planet_{planets.Count}";
            planets.Add(p);

            idx = planets.Count - 1;
            selected = p; // optionally auto-select new planet

            ArrangePlanets();
            HighlightSelected();
        }
        
        //element controls
        if (kb.wKey.wasPressedThisFrame) selected.AddWater(1f);
        if (kb.eKey.wasPressedThisFrame) selected.AddEarth(1f);
        if (kb.fKey.wasPressedThisFrame) selected.AddFire(1f);
        if (kb.aKey.wasPressedThisFrame) selected.AddAir(1f);

        //decay logic
        float dt = Time.deltaTime;
        foreach (var p in planets)
        {
            // optional decay (example)
            p.elements.water = Mathf.Max(0f, p.elements.water - 0.1f * dt);
            p.elements.earth = Mathf.Max(0f, p.elements.earth - 0.1f * dt);
            p.elements.fire = Mathf.Max(0f, p.elements.fire - 0.1f * dt);
            p.elements.air = Mathf.Max(0f, p.elements.air - 0.1f * dt);

            // compute habitability
            //p.habitability = HabitabilityLogic.Compute(p.elements);
            //p.population += (p.habitability - 0.5f) * 10f * dt;
            //p.population = Mathf.Max(0f, p.population);
        }
    }
    //arranging planets horizontally & centered
    private void ArrangePlanets()
    {
        float spacing = 3f; // distance between planets
        
        float totalWidth = (planets.Count - 1) * spacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < planets.Count; i++)
        {
           float xPos = startX + i * spacing;
           planets[i].transform.position = new Vector3(xPos, 0f, 0f);
        }
    }

    //highlight selected planet
    private void HighlightSelected()
    {
        foreach (var p in planets)
        {
            p.transform.localScale = Vector3.one;
        }
        selected.transform.localScale = Vector3.one * 1.2f;
    }
}
