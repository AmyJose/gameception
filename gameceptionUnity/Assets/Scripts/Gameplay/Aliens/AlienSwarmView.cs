using Gameplay;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class AlienSwarmView : MonoBehaviour
{
    [SerializeField] private Planet planet;
    [SerializeField] private Transform container;
    [SerializeField] private AlienLibrary alienLibrary;
    [SerializeField] private SpriteRenderer planetSpriteRenderer;

    [SerializeField] private int populationPerSprite = 5;
    [SerializeField] private int maxSprites = 30;

    [SerializeField] private float edgeOffset = 0f;
    [SerializeField] private float jitter = 0.05f;
    [SerializeField] private float radiusMultiplier = 0.1f;

    private readonly List<GameObject> _spawned = new();
    private AlienType _currentType;
    private int _currentCount = -1;

    private void Awake()
    {
        if (container == null)
        {
            var go = new GameObject("Aliens");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            container = go.transform;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    void Update()
    {
        if (planet == null || alienLibrary == null) return;

        AlienType type = planet.PlanetAlienType;
        int desired = Mathf.Clamp(
            Mathf.FloorToInt(planet.Population / Mathf.Max(1, populationPerSprite)),
            0, maxSprites
        );

        if(type != _currentType)
        {
            _currentType = type;
            Rebuild(desired);
            return;
        }

        if (desired != _currentCount)
        {
            Resize(desired);
        }
    }
    private void Rebuild(int desired)
    {
        ClearAll();
        Resize(desired);
    }
    private void Resize(int desired)
    {
        _currentCount = desired;

        while (_spawned.Count < desired) SpawnOne();
        while (_spawned.Count > desired) DespawnOne();
    }
    private void SpawnOne()
    {
        var prefab = alienLibrary.GetPrefab(_currentType);
        if (prefab == null) return;

        var go = Instantiate(prefab, container);

        float startAngle = (_spawned.Count / (float)Mathf.Max(1, maxSprites)) * 360f;
        float orbitRadius = GetSpawnRadius();
        float walkSpeed = Random.Range(12f, 28f);

        //randomly reverse the direction for some aliens
        if (Random.value < 0.5f) walkSpeed *= -1f;

        var walker = go.GetComponent<AlienWalker>();
        if (walker == null) walker = go.AddComponent<AlienWalker>();

        walker.Initialise(container, startAngle, orbitRadius, walkSpeed, -90f);

        _spawned.Add(go);
    }
    private void DespawnOne()
    {
        int last = _spawned.Count - 1;
        if (last < 0) return;

        var go = _spawned[last];
        _spawned.RemoveAt(last);
        if (go != null) Destroy(go);
    }
    private void ClearAll()
    {
        foreach (var go in _spawned)
            if (go != null) Destroy(go);
        _spawned.Clear();
    }
    private Vector3 RandomPointOnPlanetEdge()
    {
        float localRadius = 1f;

        if (planetSpriteRenderer != null)
        {
            float worldRadius = planetSpriteRenderer.bounds.extents.x;

            float parentScaleX = transform.lossyScale.x;

            localRadius = (worldRadius / Mathf.Max(parentScaleX, 0.0001f)) * radiusMultiplier;

        }
        float angle = Random.value * Mathf.PI * 2f;
        float r = localRadius + edgeOffset + Random.Range(-jitter, jitter);

        return new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f);
    }

    private void RotateAlienToPlanetEdge(Transform alienTransform)
    {
        Vector2 dir = alienTransform.localPosition.normalized;

        //angle of the aliens position around the planet
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        float spriteRotationOffset = -90f;

        alienTransform.localRotation = Quaternion.Euler(0f, 0f, angle + spriteRotationOffset);
    }

    private float GetSpawnRadius()
    {
        float localRadius = 1f;

        if (planetSpriteRenderer != null)
        {
            float worldRadius = planetSpriteRenderer.bounds.extents.x;
            float parentScaleX = transform.lossyScale.x;
            localRadius = (worldRadius / Mathf.Max(parentScaleX, 0.0001f)) * radiusMultiplier;
        }

        return localRadius + edgeOffset + Random.Range(-jitter, jitter);
    }
}
