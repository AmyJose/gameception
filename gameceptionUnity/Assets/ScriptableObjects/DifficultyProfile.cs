using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "DifficultyProfile", menuName = "Scriptable Objects/DifficultyProfile")]
public class DifficultyProfile : ScriptableObject
{
    //multiplies base element decay on planets
    public float elementDecayMultiplier = 1f;
    //multiplies base consumption per alien on planets
    public float consumptionMultiplier = 1f;
    //multiplies the base population growth per second when habitable
    public float populationGrowthMultiplier = 1f;
}
