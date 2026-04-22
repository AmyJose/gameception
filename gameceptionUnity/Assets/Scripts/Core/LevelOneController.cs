using Gameplay;
using Gameplay.Choreography;
using InputLayer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable] public class LaneUnlockData
{
    public int laneIndex;
    public int requiredPadIndex;
}

public class LevelOneController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlanetManager planetManager;
    [SerializeField] private PromptQueue promptQueue;
    [SerializeField] private DanceMatInputProvider danceMatInputProvider;
    [SerializeField] private DanceMatSelectionController danceMatSelectionController;
    [SerializeField] private SelectionState selectionState;

    [Header("Scene Objects")]
    [SerializeField] private UFO ufoPrefab;
    [SerializeField] private Transform ufoSpawnPoint;
    [SerializeField] private Transform ufoIntroPoint;
    [SerializeField] private Transform ufoExitPoint;

    [Header("Planet Spawn Points")]
    [SerializeField] private Transform[] planetSpawnPoints;
    [SerializeField] private int maxPlanets = 4;
    [SerializeField] private float repeatSpawnDelay = 15f;

    [Header("Lane Unlock Order")]
    [SerializeField] private LaneUnlockData[] laneUnlockOrder;

    [Header("Planet Setup")]
    [SerializeField] private int starterPopulationAmount = 10;

    private bool waitingForSpecificPad;
    private bool requiredPadPressed;
    private int requiredPadIndex = -1;
    private UFO currentUFO;
    private int nextUnlockStep = 0;

    private readonly List<int> unlockedLanes = new();

    private void OnEnable()
    {
        if (danceMatInputProvider != null)
            danceMatInputProvider.OnPadPressed += HandlePadPressed;
    }

    private void OnDisable()
    {
        if (danceMatInputProvider != null)
            danceMatInputProvider.OnPadPressed -= HandlePadPressed;
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (promptQueue != null)
        {
            promptQueue.StopGeneration();
            promptQueue.SetActiveLanes(unlockedLanes);
            promptQueue.ClearAll();
        }

        StartCoroutine(RunLevelFlow());
    }

    private void HandlePadPressed(int idx)
    {
        if (!waitingForSpecificPad) return;
        if (idx != requiredPadIndex) return;

        requiredPadPressed = true;
    }

    private IEnumerator RunLevelFlow()
    {
        if (danceMatSelectionController != null)
            danceMatSelectionController.SetSelectionEnabled(false);

        if (selectionState != null)
            selectionState.Clear();

        yield return new WaitForSeconds(2f);

        yield return IntroduceNextPlanet();

        if (promptQueue != null && unlockedLanes.Count > 0)
        {
            promptQueue.SetActiveLanes(unlockedLanes);
            promptQueue.BeginGeneration();
        }

        StartCoroutine(RecurringPlanetSpawnLoop());
    }

    private IEnumerator RecurringPlanetSpawnLoop()
    {
        while (nextUnlockStep < maxPlanets && nextUnlockStep < laneUnlockOrder.Length)
        {
            yield return new WaitForSeconds(repeatSpawnDelay);

            yield return IntroduceNextPlanet();
        }
    }

    private IEnumerator IntroduceNextPlanet()
    {
        if (planetManager == null || planetManager.PlanetCount >= maxPlanets) yield break;

        if(nextUnlockStep >=laneUnlockOrder.Length) yield break;

        if (promptQueue != null)
        {
            promptQueue.StopGeneration();
        }

        Transform spawnPoint = GetNextPlanetSpawnPoint();
        if (spawnPoint == null) yield break;

        LaneUnlockData unlock = laneUnlockOrder[nextUnlockStep];

        string message = $"Jump on pad {unlock.requiredPadIndex + 1} to create this planet!";

        if(ufoPrefab != null && ufoSpawnPoint != null && ufoIntroPoint!= null)
        {
            currentUFO = Instantiate(ufoPrefab, ufoSpawnPoint.position, Quaternion.identity);
            yield return currentUFO.PlayEntranceSequence(ufoIntroPoint.position, message);
        }
        yield return WaitForSpecificPadPress(unlock.requiredPadIndex);

        if (currentUFO != null)
            currentUFO.HideMessage();

        Planet newPlanet = planetManager.SpawnPlanetAt(
            spawnPoint.position,
            planetManager.availableDefinitions[planetManager.PlanetCount]
        );
        if (newPlanet == null) yield break;

        newPlanet.SetPlanetIndex(unlock.laneIndex);

        yield return new WaitUntil(() => !newPlanet.IsGrowing);
        newPlanet.ActivateChoreography();

        if (danceMatSelectionController != null)
            danceMatSelectionController.SetSelectionEnabled(true);

        if (selectionState != null)
            selectionState.SoloSelect(unlock.laneIndex);

        if (currentUFO != null)
        {
            Vector3 ufoTarget = newPlanet.transform.position + new Vector3(0f, 3f, 0f);
            // yield return currentUFO.FlyTo(ufoTarget, 1.2f, 20f);
            yield return currentUFO.PlayFlyToPlanetSequence(ufoTarget, 1.2f, 20f);
            currentUFO.StartBobbing();
        }

        yield return new WaitForSeconds(0.4f);

        AddStarterPopulation(newPlanet);

        unlockedLanes.Add(unlock.laneIndex);

        if (promptQueue != null)
        {
            promptQueue.SetActiveLanes(unlockedLanes);

            if (!promptQueue.IsGenerating)
                promptQueue.BeginGeneration();
        }

        yield return new WaitForSeconds(0.3f);

        if (currentUFO != null && ufoExitPoint != null)
        {
            // currentUFO.HideMessage();
            // yield return currentUFO.FlyTo(ufoExitPoint.position, 1f, 25f);
            yield return currentUFO.PlayExitSequence(ufoExitPoint.position, 1f, 25f);
            // Destroy(currentUFO.gameObject);
        }

        if (promptQueue != null)
        {
            promptQueue.SetActiveLanes(unlockedLanes);

            if (!promptQueue.IsGenerating)
                promptQueue.BeginGeneration();
        }

        currentUFO = null;
        nextUnlockStep++;
    }
    private IEnumerator WaitForSpecificPadPress(int padIndex)
    {
        waitingForSpecificPad = true;
        requiredPadIndex = padIndex;
        requiredPadPressed = false;

        yield return new WaitUntil(() => requiredPadPressed);

        waitingForSpecificPad = false;
        requiredPadIndex = -1;
    }

    private Transform GetNextPlanetSpawnPoint()
    {
        if (planetSpawnPoints == null || planetSpawnPoints.Length == 0)
            return null;

        int index = planetManager.PlanetCount;

        if (index < 0 || index >= planetSpawnPoints.Length)
            return null;

        return planetSpawnPoints[index];
    }

    private void AddStarterPopulation(Planet planet)
    {
        if (planet == null) return;
        planet.AddStarterPopulation(starterPopulationAmount);
    }
}