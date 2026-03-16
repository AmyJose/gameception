using Gameplay;
using InputLayer;
using System;
using TMPro;
using UnityEngine;

public class PlanetSpawnController : MonoBehaviour
{
    [SerializeField] private DanceMatInputProvider danceMatInputProvider;
    [SerializeField] private PlanetManager planetManager;

    [SerializeField] private GameObject ufoObject;
    [SerializeField] private TMP_Text promptText;

    public event Action OnSequenceCompleted;

    private PlanetSpawnSequence currentSequence;
    private bool waitingForPadInput = false;

    private void OnEnable()
    {
        if (danceMatInputProvider != null) danceMatInputProvider.OnPadPressed += HandlePadPressed;
    }
    private void OnDisable()
    {
        if (danceMatInputProvider != null) danceMatInputProvider.OnPadPressed -= HandlePadPressed;
    }

    public void PlaySequence(PlanetSpawnSequence sequence)
    {
        if (sequence == null)
        {
            Debug.LogError("PlanetSpawnController : sequence is null");
            return;
        }

        currentSequence = sequence;
        Debug.Log($"PlanetSpawnController: starting sequence{sequence.sequenceName}");

        //UFO appearence
        if (ufoObject != null) ufoObject.SetActive(sequence.showUFO);

        if(promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = sequence.promptText;
        }

        if (sequence.requirePadInput)
        {
            waitingForPadInput = true;
        }
        else SpawnPlanetFromCurrentSequence();
    }

    private void HandlePadPressed(int padIndex)
    {
        if (!waitingForPadInput) return;
        if (currentSequence == null) return;

        Debug.Log($"PlanetSpawnController: pad {padIndex} pressed, spawning planet");
        waitingForPadInput = false;
        SpawnPlanetFromCurrentSequence();
    }

    private void SpawnPlanetFromCurrentSequence()
    {
        if (currentSequence == null || planetManager == null) return;

        Planet spawnedPlanet = planetManager.SpawnPlanet(currentSequence.planetDefinitionToSpawn);

        if (spawnedPlanet == null) return;

        if (currentSequence.showAliensAfterSpawn)
        {
            Debug.Log("PlanetSpawnController: aliens should arrive now");
            //then here call the planetview animation thingy
        }

        if (ufoObject != null) ufoObject.SetActive(false);
        if (promptText != null) promptText.gameObject.SetActive(false);

        OnSequenceCompleted?.Invoke();
    }
}
