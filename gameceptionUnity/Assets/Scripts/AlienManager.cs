using UnityEngine;
using System.Collections.Generic;


public class AlienManager : MonoBehaviour
{
    [SerializeField] private SpriteRenderer planetRenderer;

    [Header("Logic")]
    public List<Alien> aliens = new();

    [Header("Visuals")]
    [SerializeField] private Transform alienVisualRoot;
    [SerializeField] private GameObject alienVisualPrefab;

    private readonly List<GameObject> visuals = new();


    public int Population => aliens.Count;

    [Header("Population Rules")]
    public float reproductionThreshold = 0.7f;
    public float reproductionRate = 0.2f; // aliens per second
    public int maxPopulation = 100;
    public float generalMood { get; private set; }
    private float reproductionAccumulator = 0f;

    void Awake()
    {
        SpawnAlien();
    }

    public void UpdateAliens(float dt, float habitability)
    {
        float moodSum = 0f;
        // Update each alien's state and remove dead ones
        for (int i = aliens.Count - 1; i >= 0; i--)
        {
            aliens[i].UpdateState(habitability, dt);

            if (!aliens[i].IsAlive)
            {
                Destroy(visuals[i]);
                visuals.RemoveAt(i);
                aliens.RemoveAt(i);
                continue;
            }


            moodSum += aliens[i].mood;
        }
        generalMood = Population > 0 ? moodSum / Population : 0f;
        // Reproduction logic 
        if (habitability >= reproductionThreshold && Population < maxPopulation)
        {
            reproductionAccumulator += reproductionRate * dt;

            while (reproductionAccumulator >= 1f && Population < maxPopulation)
            {
                SpawnAlien();
                reproductionAccumulator -= 1f;
            }
        }
    }

    // Spawns a new alien with default parameters if habitability conditions are met
    void SpawnAlien()
    {
        Alien newAlien = new Alien
        {
            mood = 1f,
            lifespan = 30f
        };

        aliens.Add(newAlien);
        SpawnAlienVisual();

        Debug.Log($"<color=green>Alien Spawned!</color> Current Population: {Population}");


    }

    void SpawnAlienVisual()
    {
        GameObject vis = Instantiate(alienVisualPrefab, alienVisualRoot);
        visuals.Add(vis);
        UpdateVisualLayout();
    }

    void UpdateVisualLayout()
    {
        int count = visuals.Count;

        float radius = 0.6f;

        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector3 pos = new Vector3(
                Mathf.Cos(angle),
                Mathf.Sin(angle),
                0f
            ) * radius;

            visuals[i].transform.localPosition = pos;
        }
    }


}
