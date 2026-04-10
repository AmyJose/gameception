using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "DifficultyProfile", menuName = "Scriptable Objects/DifficultyProfile")]
public class DifficultyProfile : ScriptableObject
{
    [Header("Choreography Difficulty")]
    public float vitalityRewardMultiplier = 1f;
    public float vitalityPenaltyMultiplier = 1f;
    public float populationRewardMultiplier = 1f;
    public float populationPenaltyMultiplier = 1f;
}
