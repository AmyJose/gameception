using System;
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
        [SerializeField] private int beatsPerSync = 4;

        [Header("Fade Settings")]
        [SerializeField] private float fadeInDuration = 0.25f;
        [SerializeField] private float fadeOutDuration = 0.35f;
        [SerializeField] private float activePlanetVolume = 1f;

        private readonly HashSet<int> _pendingSelectedIndices = new();
        private readonly Dictionary<AudioSource, float> _targetVolumes = new();

        private bool _hasPendingChange;

        private double _startDspTime;
        private double _secondsPerBeat;
        private double _secondsPerSync;
        private int _lastAppliedSyncIndex = -1;

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
            FadePlanetTracks();

            if (!_hasPendingChange) return;

            double songTime = AudioSettings.dspTime - _startDspTime;
            if (songTime < 0) return;

            int currentSyncIndex = Mathf.FloorToInt((float)(songTime / _secondsPerSync));
            double nextSyncTime = (currentSyncIndex + 1) * _secondsPerSync;

            bool reachedNextSyncPoint = songTime >= nextSyncTime - 0.02;

            if (reachedNextSyncPoint && currentSyncIndex != _lastAppliedSyncIndex)
            {
                ApplyPendingSelection();
                _lastAppliedSyncIndex = currentSyncIndex;
            }
        }

        private void InitializeAudioLayers()
        {
            _secondsPerBeat = 60.0 / bpm;
            _secondsPerSync = _secondsPerBeat * beatsPerSync;

            _startDspTime = AudioSettings.dspTime + 0.1;

            if (coreLVL != null)
                coreLVL.PlayScheduled(_startDspTime);

            if (coreAdds != null)
                coreAdds.PlayScheduled(_startDspTime);

            foreach (var planet in planetTracks)
            {
                if (planet == null) continue;

                planet.playOnAwake = false;
                planet.loop = true;
                planet.volume = 0f;

                _targetVolumes[planet] = 0f;

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
                if (planet == null) continue;

                _targetVolumes[planet] = 0f;
            }

            foreach (int idx in _pendingSelectedIndices)
            {
                if (idx >= 0 && idx < planetTracks.Count && planetTracks[idx] != null)
                {
                    _targetVolumes[planetTracks[idx]] = activePlanetVolume;
                }
            }

            _hasPendingChange = false;
        }

        private void FadePlanetTracks()
        {
            foreach (var kvp in _targetVolumes)
            {
                AudioSource source = kvp.Key;
                float targetVolume = kvp.Value;

                if (source == null) continue;

                float duration = source.volume < targetVolume
                    ? fadeInDuration
                    : fadeOutDuration;

                if (duration <= 0f)
                {
                    source.volume = targetVolume;
                    continue;
                }

                source.volume = Mathf.MoveTowards(
                    source.volume,
                    targetVolume,
                    Time.deltaTime / duration
                );
            }
        }
    }
}