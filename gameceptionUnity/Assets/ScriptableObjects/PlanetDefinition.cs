using InputLayer;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PlanetDefinition", menuName = "Scriptable Objects/PlanetDefinition")]
public class PlanetDefinition : ScriptableObject
{
    public Sprite planetSprite;

    public float startingPopulation = 5f;
    public float populationCap = 100f;

    public AlienType alienType;
    public List<ElementPose> requiredNeeds = new();
}
