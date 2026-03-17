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

    [Header("Planet Setup")]
    [SerializeField] private float starterElementAmount = 50f;

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
        if (ufoPrefab != null && ufoSpawnPoint != null && ufoIntroPoint != null)
        {
            currentUFO = Instantiate(ufoPrefab, ufoSpawnPoint.position, Quaternion.identity);
            yield return currentUFO.PlayEntranceSequence(ufoIntroPoint.position, "Please make us a planet. Jump on a pad to spawn it!");
        }

        // Wait for any pad press
        waitingForSpawnPad = true;
        spawnPadPressed = false;
        spawnPadIndex = -1;

        yield return new WaitUntil(() => spawnPadPressed);

        waitingForSpawnPad = false;
        currentUFO.HideMessage();

        // Spawn planet
        Planet newPlanet = planetManager.SpawnPlanet();

        if (newPlanet != null)
        {
            InitialiseStarterPlanet(newPlanet);

            var view = newPlanet.GetComponent<PlanetView>();
            if (view != null)
                view.HideResourceUI();

            int newPlanetIndex = planetManager.PlanetCount - 1;

            if (selectionState != null)
                selectionState.SoloSelect(newPlanetIndex);

            yield return new WaitUntil(() => !newPlanet.IsGrowing);

            if (currentUFO != null)
            {
                Vector3 ufoTarget = newPlanet.transform.position + new Vector3(0f, 3f, 0f);
                yield return currentUFO.FlyTo(ufoTarget, 1.2f, 20f);
            }

            yield return new WaitForSeconds(0.4f);
            AddStarterPopulation(newPlanet);

            yield return new WaitForSeconds(0.3f);

            if (currentUFO != null && ufoExitPoint != null)
            {
                currentUFO.HideMessage();
                yield return currentUFO.FlyTo(ufoExitPoint.position, 1f, 25f);
                Destroy(currentUFO.gameObject);
                currentUFO = null;
            }

            yield return new WaitForSeconds(0.4f);

            var newPlanetView = newPlanet.GetComponent<PlanetView>();
            if (newPlanetView != null)
                newPlanetView.ShowResourceUI();
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

    private void AddStarterPopulation(Planet planet)
    {
        if (planet == null) return;

        planet.AddPopulation(10);

        Debug.Log("[LevelOneController] Added starter population to planet.");
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