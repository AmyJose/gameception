using InputLayer;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    //holds the list of all the planets
    //updates them all every frame
    public class PlanetManager : MonoBehaviour
    {
        [SerializeField] private Planet planetPrefab;
        //FOR BETA public
        [SerializeField] public List<PlanetDefinition> availableDefinitions = new();

        [SerializeField] private int initialPlanetCount = 0;
        [SerializeField] private bool spawnOnStart = false;
        [SerializeField] private bool randomiseDefinitions = true;

        [SerializeField] private float spacing =12f;
        [SerializeField] private Vector3 centerPosition = Vector3.zero;

        [SerializeField] private DifficultyProfile difficultyProfile;
        [SerializeField] private SelectionState selectionState;

        [SerializeField] private DanceMatSelectionController danceMatSelectionController;

        private readonly List<Planet> planets = new();
        public int PlanetCount => planets.Count;
        public IReadOnlyList<Planet> Planets => planets;

        private void Start()
        {
            if (spawnOnStart) 
            {
                SpawnInitialPlanets();
            }
        }

        public Planet GetPlanet(int index)
        {
            if (index < 0 || index >= planets.Count) return null;
            return planets[index];
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (var p in planets)
            {
                if (p == null) continue;
                p.Tick(dt);
            }
        }

        public void SpawnInitialPlanets()
        {
            ClearAllPlanets();

            for (int i = 0; i < initialPlanetCount; i++)
            {
                SpawnPlanet();
            }

            RefreshPlanetIndices();
            ArrangePlanets();
        }

        public Planet SpawnPlanetAt(Vector3 worldPosition, PlanetDefinition definitionOverride = null)
        {
            if (planetPrefab == null)
            {
                Debug.LogError("[PlanetManager] Planet prefab is not assigned");
                return null;
            }

            Planet newPlanet = Instantiate(planetPrefab, worldPosition, Quaternion.identity, transform);
            newPlanet.name = $"Planet_{planets.Count}";

            PlanetDefinition chosenDefinition = definitionOverride != null
                ? definitionOverride
                : ChooseDefinitionForSpawn();

            if (chosenDefinition != null)
            {
                newPlanet.SetDefinition(chosenDefinition);
            }
            if (difficultyProfile != null)
            {
                newPlanet.SetDifficulty(difficultyProfile);
            }

            planets.Add(newPlanet);
            RefreshPlanetIndices();
            newPlanet.BeginSpawnAnimation();

            if (danceMatSelectionController != null) danceMatSelectionController.SetPlanetCount(planets.Count);

            Debug.Log("Spawned planet at " + newPlanet.transform.position);

            return newPlanet;
        }
        private void RefreshPlanetIndices()
        {
            for (int i=0; i<planets.Count; i++)
            {
                if (planets[i] != null)
                {
                    planets[i].SetPlanetIndex(i);
                }
            }
        }

        public Planet SpawnPlanet(PlanetDefinition definitionOverride = null)
        {
            return SpawnPlanetAt(centerPosition, definitionOverride);
        }
        
        public void RemovePlanet(Planet planet)
        {
            if (planet == null) return;
            if (!planets.Remove(planet)) return;

            Destroy(planet.gameObject);
            ArrangePlanets();
            RefreshPlanetIndices();

            if (danceMatSelectionController != null) danceMatSelectionController.SetPlanetCount(planets.Count);
        }
        public void SetDifficultyForAll(DifficultyProfile profile)
        {
            difficultyProfile = profile;

            foreach(var p in planets)
            {
                if (p == null) continue;
                p.SetDifficulty(profile);
            }
        }
        public void ArrangePlanets()
        {
            int count = planets.Count;
            if (count == 0) return;

            float totalWidth = (count - 1) * spacing;
            float startX = centerPosition.x - totalWidth / 2f;

            for (int i = 0; i < count; i++)
            {
                if (planets[i]  == null) continue;

                float x = startX + i * spacing;
                planets[i].transform.position = new Vector3(x, centerPosition.y, centerPosition.z);
            }
        }

        private PlanetDefinition ChooseDefinitionForSpawn()
        {
            if (availableDefinitions == null || availableDefinitions.Count == 0) return null;

            if (!randomiseDefinitions)
            {
                int index = planets.Count % availableDefinitions.Count;
                return availableDefinitions[index];
            }

            int randomIndex = Random.Range(0, availableDefinitions.Count);
            return availableDefinitions[randomIndex];
        }
        private void ClearAllPlanets()
        {
            foreach (var p in planets)
            {
                if (p != null)
                    Destroy(p.gameObject);
            }

            planets.Clear();

            if (danceMatSelectionController != null) danceMatSelectionController.SetPlanetCount(0);
        }
    }
}