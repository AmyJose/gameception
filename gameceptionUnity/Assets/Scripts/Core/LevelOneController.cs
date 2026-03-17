using Gameplay;
using InputLayer;
using Mono.Cecil;
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

    [Header("Planet Setup")]
    [SerializeField] private PlanetDefinition starterDefinition;
    [SerializeField] private float starterElementAmount = 10f;

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
        StartCoroutine(RunLevelOneSequence());
    }

    private void HandlePadPressed(int idx)
    {
        if (!waitingForSpawnPad) return;

        spawnPadPressed = true;
        spawnPadIndex = idx;
    }

    private IEnumerator RunLevelOneSequence()
    {
        // Disable normal selection while we are in the intro sequence.
        if (danceMatSelectionController != null)
            danceMatSelectionController.SetSelectionEnabled(false);

        if (selectionState != null)
            selectionState.Clear();

        // Empty galaxy at start
        yield return new WaitForSeconds(2f);

        // UFO appears
        if (ufoPrefab != null)
        {
            currentUFO = Instantiate(ufoPrefab, ufoSpawnPoint.position, Quaternion.identity);
            yield return currentUFO.PlayEntranceSequence("Please make us a planet. Jump on a pad to spawn it!");
        }

        // Wait for any pad press
        waitingForSpawnPad = true;
        spawnPadPressed = false;
        spawnPadIndex = -1;

        yield return new WaitUntil(() => spawnPadPressed);

        waitingForSpawnPad = false;
        currentUFO.HideMessage();

        // Spawn planet
        Planet newPlanet = planetManager.SpawnPlanet(starterDefinition);

        if (newPlanet != null)
        {
            InitialiseStarterPlanet(newPlanet);

            int newPlanetIndex = planetManager.PlanetCount - 1;

            if (selectionState != null)
                selectionState.SoloSelect(newPlanetIndex);
        }

        // Re-enable normal selection/gameplay
        if (danceMatSelectionController != null)
            danceMatSelectionController.SetSelectionEnabled(true);

        BeginGameplayPhase();
    }

    private void InitialiseStarterPlanet(Planet planet)
    {
        if (planet == null) return;

        planet.SetElements(starterElementAmount, starterElementAmount, starterElementAmount, starterElementAmount);
    }

    private void BeginGameplayPhase()
    {
        Debug.Log("[LevelOneController] Intro finished. Normal gameplay begins.");

        // Later this will trigger:
        // - beat pose gameplay
        // - objective tracking
        // - timed second planet arrival
    }
}