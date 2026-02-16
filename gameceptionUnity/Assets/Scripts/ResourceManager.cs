using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
// Hanfles decay logic
// Key-presses
public class ResourceManager : MonoBehaviour
{
    private readonly List<Planet> planets = new();
    private int idx = 0;
    private Planet selected;
    [SerializeField] private Planet planetPrefab;
    // key press logic
    [SerializeField] private PoseDetectionRunner poseRunner;

    [SerializeField] private float poseHoldTime = 3f;

    private PoseLandmarkHUD.ElementPose lastPose = PoseLandmarkHUD.ElementPose.None;
    private float poseTimer = 0f;

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
        //if (kb.wKey.wasPressedThisFrame) selected.AddWater(1f);
        /**if (kb.wKey.wasPressedThisFrame) selected.AddWater(1f);
        if (kb.eKey.wasPressedThisFrame) selected.AddEarth(1f);
        if (kb.fKey.wasPressedThisFrame) selected.AddFire(1f);
        if (kb.aKey.wasPressedThisFrame) selected.AddAir(1f);**/

        HandlePoseInput();

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
            p.transform.localScale = Vector3.one * 1.5f; // default scale
        }
        selected.transform.localScale = Vector3.one * 1.7f; // enlarge selected
    }

    private void HandlePoseInput()
    {
        if (poseRunner.HUD == null) return;

        var currentPose = poseRunner.HUD.CurrentPose;

        // 1. If we are seeing a pose we weren't seeing last frame
        if (currentPose != PoseLandmarkHUD.ElementPose.None && currentPose != lastPose)
        {
            TriggerElementEffect(currentPose);
        }

        // 2. Remember what we saw this frame for next frame's comparison
        lastPose = currentPose;
    }
    private void TriggerElementEffect(PoseLandmarkHUD.ElementPose pose)
    {
        if (selected == null) return;

        switch (pose)
        {
            case PoseLandmarkHUD.ElementPose.Water:
                selected.AddWater(10f);
                selected.waterEffect.Activate();
                break;

            case PoseLandmarkHUD.ElementPose.Fire:
                selected.AddFire(10f);
                selected.fireEffect.Activate();
                break;

            case PoseLandmarkHUD.ElementPose.Air:
                selected.AddAir(10f);
                selected.airEffect.Activate();
                break;

            case PoseLandmarkHUD.ElementPose.Earth:
                selected.AddEarth(10f);
                selected.earthEffect.Activate();
                break;
        }


    }
}
