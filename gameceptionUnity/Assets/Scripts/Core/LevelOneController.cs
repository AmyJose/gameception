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
        Debug.Log($"[LevelOne] Start. Time.timeScale = {Time.timeScale}");
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
        Debug.Log("[LevelOneController] S - after begin gameplay phase");

        // Repeating spawns
        Debug.Log("[LevelOneController] T -before recurring planet spawn loop");
        StartCoroutine(RecurringPlanetSpawnLoop());
        Debug.Log("[LevelOneController] T2 - recurring loop disabled for test");
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

        Debug.Log("[LevelOneController] A - starting arrival sequence");

        Transform spawnPoint = GetNextPlanetSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogWarning("[LevelOneController] No free planet spawn point available.");
            yield break;
        }

        Debug.Log("[LevelOneController] B - got spawn point");

        // Spawn UFO and do intro fly-in
        if (ufoPrefab != null && ufoSpawnPoint != null && ufoIntroPoint != null)
        {
            currentUFO = Instantiate(ufoPrefab, ufoSpawnPoint.position, Quaternion.identity);
            Debug.Log("[LevelOneController] C - spawned UFO");
            yield return currentUFO.PlayEntranceSequence(ufoIntroPoint.position, ufoMessage);
            Debug.Log("[LevelOneController] D - UFO entreance sequence done");
        }

        // Wait for pad press
        yield return WaitForSpawnPadPress();
        Debug.Log("[LevelOneController] E - got spawn pad press");

        if (currentUFO != null)
            currentUFO.HideMessage();

        // Spawn planet at chosen location
        //FOR BETA: Circle through all definitions
        Planet newPlanet = planetManager.SpawnPlanetAt(spawnPoint.position, planetManager.availableDefinitions[planetManager.PlanetCount]);
        Debug.Log("[LevelOneController] F - SpawnPlanetAt returned");

        if (newPlanet == null)
        {
            Debug.Log("[LevelOneController] newPlanet is null");
            yield break;
        }

        Debug.Log("[LevelOneController] H - initialising starter planet");
        InitialiseStarterPlanet(newPlanet);

        // Wait for growth
        Debug.Log($"[LevelOneController] I -before growth wait, IsGrowing = {newPlanet.IsGrowing}");
        yield return new WaitUntil(() => !newPlanet.IsGrowing);

        Debug.Log("[LevelOneController] J - growth finished");
        // UFO moves to planet
        if (currentUFO != null)
        {
            Vector3 ufoTarget = newPlanet.transform.position + new Vector3(0f, 3f, 0f);
            Debug.Log("[LevelOneController] K - moving UFO to planet");
            yield return currentUFO.FlyTo(ufoTarget, 1.2f, 20f);
            Debug.Log("[LevelOneController] L - UFO reached planet");
            currentUFO.StartBobbing();
        }

        yield return new WaitForSeconds(0.4f);
        Debug.Log("[LevelOneController] M - about to add starter population");

        AddStarterPopulation(newPlanet);

        Debug.Log("[LevelOneController] N - added starter population");

        yield return new WaitForSeconds(0.3f);
        Debug.Log("[LevelOneController] N2 - after 0.3 second wait");

        // UFO exits
        if (currentUFO != null && ufoExitPoint != null)
        {
            Debug.Log("[LevelOneController] O - before HideMessage");
            currentUFO.HideMessage();
            Debug.Log("[LevelOneController] O2 - after HideMessage");
            Debug.Log("[LevelOneController] O3 - before FlyTo exit");
            yield return currentUFO.FlyTo(ufoExitPoint.position, 1f, 25f);
            Debug.Log("[LevelOneController] P - after FlyTo exit");

            Destroy(currentUFO.gameObject);
            Debug.Log("[LevelOneController] P2 - after Destroy");
        }

        yield return new WaitForSeconds(0.4f);
        Debug.Log("[LevelOneController] Q - arrival sequence complete");
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
        Debug.Log("[LevelOneController] Before null check");
        if (planet == null) return;
        Debug.Log("[LevelOneController] About to add starter population");
        planet.AddPopulation(starterPopulationAmount);
        Debug.Log("[LevelOneController] Added starter population to planet.");
    }

    private void BeginGameplayPhase()
    {
        Debug.Log($"[LevelOneController] Intro finished. Normal gameplay begins. timeScale={Time.timeScale}");
    }
}