using Gameplay;
using InputLayer;
using System.Collections;
using UnityEngine;

public class LevelOneController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlanetManager planetManager;
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

    [Header("Planet Setup")]
    [SerializeField] private float starterElementAmount = 50f;
    [SerializeField] private int starterPopulationAmount = 10;

    private bool waitingForSpawnPad;
    private bool spawnPadPressed;
    private int spawnPadIndex = -1;
    private UFO currentUFO;

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
        StartCoroutine(RunLevelFlow());
    }

    private void HandlePadPressed(int idx)
    {
        if (!waitingForSpawnPad) return;

        spawnPadPressed = true;
        spawnPadIndex = idx;
    }

    private IEnumerator RunLevelFlow()
    {
        if (danceMatSelectionController != null)
            danceMatSelectionController.SetSelectionEnabled(false);

        if (selectionState != null)
            selectionState.Clear();

        yield return new WaitForSeconds(2f);

        // First guided tutorial spawn
        yield return RunPlanetArrivalSequence(
            "Please make us a planet. Jump on a pad to spawn it!"
        );

        if (danceMatSelectionController != null)
            danceMatSelectionController.SetSelectionEnabled(true);

        BeginGameplayPhase();

        // Repeating spawns
        StartCoroutine(RecurringPlanetSpawnLoop());
    }

    private IEnumerator RecurringPlanetSpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(repeatSpawnDelay);

            if (planetManager == null) continue;
            if (planetManager.PlanetCount >= maxPlanets) continue;

            yield return RunPlanetArrivalSequence(
                "We want a planet too! Jump on a pad to create one!"
            );
        }
    }

    private IEnumerator RunPlanetArrivalSequence(string ufoMessage)
    {
        if (planetManager == null || planetManager.PlanetCount >= maxPlanets)
            yield break;


        Transform spawnPoint = GetNextPlanetSpawnPoint();
        if (spawnPoint == null)
        {
            yield break;
        }

        // Spawn UFO and do intro fly-in
        if (ufoPrefab != null && ufoSpawnPoint != null && ufoIntroPoint != null)
        {
            currentUFO = Instantiate(ufoPrefab, ufoSpawnPoint.position, Quaternion.identity);
            yield return currentUFO.PlayEntranceSequence(ufoIntroPoint.position, ufoMessage);
        }

        // Wait for pad press
        yield return WaitForSpawnPadPress();

        if (currentUFO != null)
            currentUFO.HideMessage();

        // Spawn planet at chosen location
        //FOR BETA: Circle through all definitions
        Planet newPlanet = planetManager.SpawnPlanetAt(spawnPoint.position, planetManager.availableDefinitions[planetManager.PlanetCount]);

        if (newPlanet == null)
        {
            Debug.Log("[LevelOneController] newPlanet is null");
            yield break;
        }

        InitialiseStarterPlanet(newPlanet);

        // Wait for growth
        yield return new WaitUntil(() => !newPlanet.IsGrowing);

        // UFO moves to planet
        if (currentUFO != null)
        {
            Vector3 ufoTarget = newPlanet.transform.position + new Vector3(0f, 3f, 0f);
            yield return currentUFO.FlyTo(ufoTarget, 1.2f, 20f);
            currentUFO.StartBobbing();
        }

        yield return new WaitForSeconds(0.4f);

        AddStarterPopulation(newPlanet);

        yield return new WaitForSeconds(0.3f);

        // UFO exits
        if (currentUFO != null && ufoExitPoint != null)
        {
            currentUFO.HideMessage();
            yield return currentUFO.FlyTo(ufoExitPoint.position, 1f, 25f);

            Destroy(currentUFO.gameObject);
        }

        yield return new WaitForSeconds(0.4f);
    }

    private IEnumerator WaitForSpawnPadPress()
    {
        waitingForSpawnPad = true;
        spawnPadPressed = false;
        spawnPadIndex = -1;

        yield return new WaitUntil(() => spawnPadPressed);

        waitingForSpawnPad = false;
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

    private void InitialiseStarterPlanet(Planet planet)
    {
        if (planet == null) return;

        /*planet.SetElements(
            starterElementAmount,
            starterElementAmount,
            starterElementAmount,
            starterElementAmount
        );*/
    }

    private void AddStarterPopulation(Planet planet)
    {
        if (planet == null) return;
        planet.AddPopulation(starterPopulationAmount);
    }

    private void BeginGameplayPhase()
    {
        Debug.Log($"[LevelOneController] Intro finished. Normal gameplay begins. timeScale={Time.timeScale}");
    }
}