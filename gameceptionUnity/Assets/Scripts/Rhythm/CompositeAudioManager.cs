using System.Collections.Generic;
using UnityEngine;
using InputLayer;
using Audio;

public class GameplayMusicController : MonoBehaviour
{
    [SerializeField] private SelectionState selectionState;

    private void OnEnable()
    {
        if (selectionState != null)
            selectionState.OnChanged += HandleSelectionChanged;
    }

    private void OnDisable()
    {
        if (selectionState != null)
            selectionState.OnChanged -= HandleSelectionChanged;
    }

    private void Start()
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetGameplayMode();
    }

    private void HandleSelectionChanged(IReadOnlyCollection<int> selectedIndices)
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetGameplaySelectedPlanets(selectedIndices);
    }
}