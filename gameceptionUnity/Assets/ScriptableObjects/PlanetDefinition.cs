using UnityEngine;

[CreateAssetMenu(fileName = "PlanetDefinition", menuName = "Scriptable Objects/PlanetDefinition")]
public class PlanetDefinition : ScriptableObject
{
    public Sprite planetSprite;

    //target distribution
    public float targetFire = 70f;
    public float targetWater = 20f;
    public float targetEarth = 15f;
    public float targetIce = 0f;

    public float tolerance = 12f;

    //min pop once habitability is reached
    public float startingPopulation = 1f;

    //max population cap
    public float populationCap = 100f;

    public AlienType alienType;
}
