using Gameplay;
using Rhythm;
using System.Collections.Generic;
using UnityEngine;

public class AlienSwarmView : MonoBehaviour
{
    [SerializeField] private Planet planet;
    [SerializeField] private Transform container;
    [SerializeField] private AlienLibrary alienLibrary;
    [SerializeField] private BeatClock beatClock;
    [SerializeField] private int populationPerSprite = 2;
    [SerializeField] private int maxSprites = 30;

    [SerializeField] private float edgeOffset = 0.5f;
    [SerializeField] private float jitter = 0.05f;
    [SerializeField] private float bobHeight = 1;
    [SerializeField] private float bobDurationBeats = 0.5f;

    private readonly List<GameObject> _spawned = new();
    private AlienType _currentType;
    private int _currentCount = -1;
    private bool isBobbingEnabled = false;
    private bool _aliensAreAngry = false;

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
    private void OnEnable()
    {
        if (beatClock == null)
        {
            beatClock = FindFirstObjectByType<Rhythm.BeatClock>();
        }
        if (beatClock != null)
        {
            beatClock.OnBeat += HandleBeat;
        }
        // else {
        //     Debug.LogWarning("No BeatClock found in scene for AlienSwarmView to subscribe to.");
        // }
    }

    void Update()
    {
        if (planet == null || alienLibrary == null) return;
        if (planet.IsGrowing) return;
        if (!planet.StarterPop) return;

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

    public void SetBobbingEnabled(bool enabled){
        isBobbingEnabled = enabled;
    }

    public void SetAliensAngry(bool angry)
    {
        _aliensAreAngry = angry;
        foreach(var go in _spawned)
        {
            if (go == null) continue;
            var mood = go.GetComponent<AlienView>();
            if (mood != null) mood.SetMood(angry);
        }
    }
    private void HandleBeat(BeatInfo beatInfo)
    {
        if (!isBobbingEnabled) return;

        float bobDuration = (float)beatInfo.beatInterval * bobDurationBeats;  

        foreach (var go in _spawned)
        {
            if (go == null) continue;

            var walker = go.GetComponent<AlienWalker>();
            if (walker == null) continue;

            float variedHeight = bobHeight * Random.Range(0.9f, 1.1f);
            float variedDuration = bobDuration * Random.Range(0.95f, 1.05f);

            walker.TriggerBeatBob(variedHeight, variedDuration);
        }
    }
    public void SetSwarmVisible(bool visible)
    {
        if (container != null){
            container.gameObject.SetActive(visible);
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

        var mood = go.GetComponent<AlienView>();
        if (mood != null) mood.SetMood(_aliensAreAngry);

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

        if (planet != null)
        {
            float worldRadius = planet.GetBodyRadiusWorld();
            float parentScaleX = container != null ? container.lossyScale.x : transform.lossyScale.x;

            localRadius = worldRadius / Mathf.Max(parentScaleX, 0.0001f);
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

        if (planet != null)
        {
            float worldRadius = planet.GetBodyRadiusWorld();
            float parentScaleX = container != null ? container.lossyScale.x : transform.lossyScale.x;

            localRadius = worldRadius / Mathf.Max(parentScaleX, 0.0001f);
        }

        return localRadius + edgeOffset + Random.Range(-jitter, jitter);
    }
    private void OnDrawGizmosSelected()
    {
        if (container == null) return;

        float localRadius = GetSpawnRadius();
        Gizmos.color = Color.yellow;

        const int steps = 64;
        Vector3 prev = container.TransformPoint(new Vector3(localRadius, 0f, 0f));

        for (int i = 1; i <= steps; i++)
        {
            float a = (i / (float)steps) * Mathf.PI * 2f;
            Vector3 localPoint = new Vector3(Mathf.Cos(a) * localRadius, Mathf.Sin(a) * localRadius, 0f);
            Vector3 worldPoint = container.TransformPoint(localPoint);

            Gizmos.DrawLine(prev, worldPoint);
            prev = worldPoint;
        }
    }
}
