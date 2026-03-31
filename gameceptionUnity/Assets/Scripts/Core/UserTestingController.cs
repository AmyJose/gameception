using Gameplay;
using Gameplay.Choreography;
using System.Collections;
using UnityEngine;

public class UserTestingController : MonoBehaviour
{
    //what do we want to happen?
    /*
        - load 4 planets on start, in proper positions.
        - wait until they have loaded to begin the choregraphy
        - get faster as we go along?
        - maybe we have 3 different difficulty profiles?
     */

    [Header("References")]
    [SerializeField] private PlanetManager planetManager;
    [SerializeField] private PromptQueue promptQueue;

    [Header("Planet Spawn Points")]
    [SerializeField] private Transform[] planetSpawnPoints;
    void Start()
    {
        StartCoroutine(SpawnTestingPlanets());
    }

    private IEnumerator SpawnTestingPlanets()
    {
        if (planetManager == null) yield break;

        for (int i = 0; i < planetSpawnPoints.Length; i++) 
        {
            Transform spawnPoint = GetNextPlanetSpawnPoint();
            if (spawnPoint == null) yield break;

            Planet newPlanet = planetManager.SpawnPlanetAt(spawnPoint.position, planetManager.availableDefinitions[planetManager.PlanetCount]);

            yield return new WaitUntil(() => !newPlanet.IsGrowing);
            yield return new WaitForSeconds(1f);
        }

    }
    private Transform GetNextPlanetSpawnPoint()
    {
        if (planetSpawnPoints == null || planetSpawnPoints.Length == 0) return null;
        int index = planetManager.PlanetCount;
        if (index < 0 || index >= planetSpawnPoints.Length) return null;
        return planetSpawnPoints[index];
    }
}
