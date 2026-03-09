using UnityEngine;

public enum AlienType { Fire, Water, Earth, Ice}

[CreateAssetMenu(fileName = "AlienDefinition", menuName = "Scriptable Objects/AlienDefinition")]
public class AlienDefinition : ScriptableObject
{
    public AlienType type;
    public GameObject alienPrefab;
}
