using UnityEngine;

[CreateAssetMenu(fileName = "PlanetSpawnSequence", menuName = "Scriptable Objects/PlanetSpawnSequence")]
public class PlanetSpawnSequence : ScriptableObject
{
    public string sequenceName;
    [TextArea] public string promptText;

    public bool showUFO = true;
    public bool showAliensAfterSpawn = true;

    public bool requirePadInput = true;

    public PlanetDefinition planetDefinitionToSpawn;
}
