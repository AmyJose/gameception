using System.Collections.Generic;
using UnityEngine;
using InputLayer;

namespace Audio
{
    public class CompositeAudioManager : MonoBehaviour
    {
        [SerializeField] private AudioSource coreLVL;
        [SerializeField] private AudioSource coreAdds;
        [SerializeField] private List<AudioSource> planetTracks = new List<AudioSource>(4); // indices 0-3
        
        [SerializeField] private SelectionState selectionState;

        private int _previousActivePlanetIndex = -1;

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
            InitializeAudioLayers();
        }

        private void InitializeAudioLayers()
        {
            // Play core tracks
            coreLVL.Play();
            coreAdds.Play();

            // Start all planet tracks but mute them
            foreach (var planet in planetTracks)
            {
                if (planet != null)
                {
                    planet.Play();
                    planet.mute = true;
                }
            }
        }

        private void HandleSelectionChanged(IReadOnlyCollection<int> selectedIndices)
        {
            // Mute all planet tracks
            foreach (var planet in planetTracks)
            {
                if (planet != null)
                    planet.mute = true;
            }

            // Unmute only the selected planets and sync them
            foreach (int idx in selectedIndices)
            {
                if (idx >= 0 && idx < planetTracks.Count && planetTracks[idx] != null)
                {
                    planetTracks[idx].mute = false;
                    SyncTrackTiming(planetTracks[idx]);
                }
            }
        }

        private void SyncTrackTiming(AudioSource trackToSync)
        {
            trackToSync.time = coreLVL.time;
        }
    }
}