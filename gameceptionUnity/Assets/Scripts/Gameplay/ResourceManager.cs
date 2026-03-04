using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using InputLayer;
using Rhythm;


public class ResourceManager : MonoBehaviour
{
    [Header("Prefabs / References")]
    [SerializeField] private Planet planetPrefab;

    [Tooltip("BeatClock that fires OnBeat events (music/metronome).")]
    [SerializeField] private BeatClock beatClock;

    [Tooltip("PoseState that contains latest detected pose + confidence.")]
    [SerializeField] private PoseState poseState;

    [Header("Startup")]
    [SerializeField] private int initialPlanets = 2;

    [Header("Selection (Dance Mat as Keyboard Digits)")]
    [Tooltip("If true: holding Shift while pressing a digit will SOLO-select that planet (clears others).")]
    [SerializeField] private bool shiftToSoloSelect = true;

    [Tooltip("Digit keys map to planet indices starting at 0 (1->0, 2->1, 3->2...). Increase if you want more hotkeys.")]
    [SerializeField] private int maxDigitSelect = 9;

    [Header("Pose Filtering")]
    [SerializeField, Range(0f, 1f)] private float minConfidence = 0.6f;

    [Header("Planet Layout")]
    [SerializeField] private float spacing = 9f;
    [SerializeField] private Vector3 planetBasePosition = new Vector3(0f, 0f, 5f);

    [Header("Decay")]
    [SerializeField] private bool enableDecay = true;
    [SerializeField] private float decayPerSecond = 0.1f; // per element

    // Runtime
    private readonly List<Planet> planets = new();
    private readonly HashSet<int> selectedIndices = new();

    private void Awake()
    {
        // Spawn initial planets
        for (int i = 0; i < initialPlanets; i++)
        {
            SpawnPlanet();
        }

        // Select first by default if available
        if (planets.Count > 0)
        {
            selectedIndices.Add(0);
            HighlightSelected();
        }
    }

    private void Update()
    {
        HandleKeyboardSelectionAndSpawning();
    }

    private void HandleKeyboardSelectionAndSpawning()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Spawn planet (T)
        if (kb.tKey.wasPressedThisFrame)
        {
            SpawnPlanet();
        }

        // Clear selection (0)
        if (kb.digit0Key.wasPressedThisFrame)
        {
            selectedIndices.Clear();
            HighlightSelected();
        }

        // Multi-select toggles (1..9)
        // 1 -> planet index 0, 2 -> 1, etc.
        // Note: Unity InputSystem exposes digit keys individually; we handle 1..9 explicitly.
        bool shiftHeld = (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);

        if (kb.digit1Key.wasPressedThisFrame) SelectByDigit(1, shiftHeld);
        if (kb.digit2Key.wasPressedThisFrame) SelectByDigit(2, shiftHeld);
        if (kb.digit3Key.wasPressedThisFrame) SelectByDigit(3, shiftHeld);
        if (kb.digit4Key.wasPressedThisFrame) SelectByDigit(4, shiftHeld);
        if (kb.digit5Key.wasPressedThisFrame) SelectByDigit(5, shiftHeld);
        if (kb.digit6Key.wasPressedThisFrame) SelectByDigit(6, shiftHeld);
        if (kb.digit7Key.wasPressedThisFrame) SelectByDigit(7, shiftHeld);
        if (kb.digit8Key.wasPressedThisFrame) SelectByDigit(8, shiftHeld);
        if (kb.digit9Key.wasPressedThisFrame) SelectByDigit(9, shiftHeld);
    }

    private void SelectByDigit(int digit, bool shiftHeld)
    {
        if (digit < 1 || digit > maxDigitSelect) return;

        int idx = digit - 1;
        if (idx < 0 || idx >= planets.Count) return;

        bool doSolo = shiftToSoloSelect && shiftHeld;

        if (doSolo)
        {
            selectedIndices.Clear();
            selectedIndices.Add(idx);
        }
        else
        {
            // toggle selection
            if (!selectedIndices.Add(idx))
                selectedIndices.Remove(idx);
        }

        HighlightSelected();
    }

    private void SpawnPlanet()
    {
        if (planetPrefab == null)
        {
            Debug.LogError("[ResourceManager] Planet prefab not assigned.");
            return;
        }

        Planet p = Instantiate(planetPrefab, Vector3.zero, Quaternion.identity);
        p.name = $"Planet_{planets.Count}";
        planets.Add(p);

        ArrangePlanets();

        selectedIndices.Add(planets.Count - 1);
        HighlightSelected();
    }

    private void ArrangePlanets()
    {
        float totalWidth = (planets.Count - 1) * spacing;
        float startX = planetBasePosition.x - totalWidth / 2f;

        for (int i = 0; i < planets.Count; i++)
        {
            float xPos = startX + i * spacing;
            planets[i].transform.position = new Vector3(xPos, planetBasePosition.y, planetBasePosition.z);
        }
    }

    private void HighlightSelected()
    {
        for (int i = 0; i < planets.Count; i++)
        {
            bool isSelected = selectedIndices.Contains(i);
            planets[i].transform.localScale = isSelected ? Vector3.one * 0.9f : Vector3.one * 0.8f;
        }
    }
}