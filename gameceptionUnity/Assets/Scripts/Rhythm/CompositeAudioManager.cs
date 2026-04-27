using System.Collections.Generic;
using UnityEngine;
using InputLayer;

namespace Audio
{
    public class CompositeAudioManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource coreLVL;
        [SerializeField] private AudioSource coreAdds;
        [SerializeField] private List<AudioSource> planetTracks = new List<AudioSource>(4);

        [Header("Selection")]
        [SerializeField] private SelectionState selectionState;

        [Header("Sync Settings")]
        [SerializeField] private float bpm = 120f;
        [SerializeField] private int beatsPerSync = 4; // 4 = next bar in 4/4

        private HashSet<int> _pendingSelectedIndices = new HashSet<int>();
        private bool _hasPendingChange;

        private double _startDspTime;
        private double _secondsPerBeat;
        private double _secondsPerSync;

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

        private void Update()
        {
            if (!_hasPendingChange) return;

            double songTime = AudioSettings.dspTime - _startDspTime;
            double currentSyncIndex = songTime / _secondsPerSync;

            bool isAtSyncPoint = currentSyncIndex >= System.Math.Ceiling(currentSyncIndex) - 0.02;

            if (isAtSyncPoint)
            {
                ApplyPendingSelection();
            }
        }

        private void InitializeAudioLayers()
        {
            _secondsPerBeat = 60.0 / bpm;
            _secondsPerSync = _secondsPerBeat * beatsPerSync;

            _startDspTime = AudioSettings.dspTime + 0.1;

            coreLVL.PlayScheduled(_startDspTime);
            coreAdds.PlayScheduled(_startDspTime);

            foreach (var planet in planetTracks)
            {
                if (planet == null) continue;

                planet.mute = true;
                planet.PlayScheduled(_startDspTime);
            }
        }

        private void HandleSelectionChanged(IReadOnlyCollection<int> selectedIndices)
        {
            _pendingSelectedIndices.Clear();

            foreach (int idx in selectedIndices)
            {
                _pendingSelectedIndices.Add(idx);
            }

            _hasPendingChange = true;
        }

        private void ApplyPendingSelection()
        {
            foreach (var planet in planetTracks)
            {
                if (planet != null)
                    planet.mute = true;
            }

            foreach (int idx in _pendingSelectedIndices)
            {
                if (idx >= 0 && idx < planetTracks.Count && planetTracks[idx] != null)
                {
                    planetTracks[idx].mute = false;
                }
            }

            _hasPendingChange = false;
        }
    }
}