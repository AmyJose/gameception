using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AlienLibrary", menuName = "Scriptable Objects/AlienLibrary")]
public class AlienLibrary : ScriptableObject
{
    [SerializeField] private List<AlienDefinition> aliens = new();
    private Dictionary<AlienType, GameObject> _map;

    private void OnEnable()
    {
        _map = new Dictionary<AlienType, GameObject>();
        foreach(var a in aliens)
        {
            if (a == null || a.alienPrefab == null) continue;
            _map[a.type] = a.alienPrefab;
        }
    }
    public GameObject GetPrefab(AlienType type)
    {
        if (_map == null) OnEnable();
        return _map.TryGetValue(type, out var prefab) ? prefab : null;
    }
}
