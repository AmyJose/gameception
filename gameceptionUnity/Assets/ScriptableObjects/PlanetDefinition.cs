using InputLayer;
using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public struct PoseWeight
{
    public ElementPose pose;
    [Min(0f)] public float weight;
}

[CreateAssetMenu(fileName = "PlanetDefinition", menuName = "Scriptable Objects/PlanetDefinition")]
public class PlanetDefinition : ScriptableObject
{
    public Sprite planetSprite;
    public Sprite selectedPlanetSprite;

    public float startingPopulation = 5f;
    public float populationCap = 100f;

    [Header("Prompt Generation")]
    public List<PoseWeight> promptPoseWeights = new();

    [Range(0f, 1f)] public float offThemeChance = 0.15f;
    [Min(1)] public int maxSamePoseStreak = 2;
    [Range(0f, 1f)] public float samePoseWeightPenalty = 0.6f;

    public AlienType alienType;
    public List<ElementPose> requiredNeeds = new();
}
