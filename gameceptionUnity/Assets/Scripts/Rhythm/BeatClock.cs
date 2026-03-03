using System;
using UnityEngine;

namespace Rhythm
{
    public class BeatClock : MonoBehaviour
    {
        public event Action<int> OnBeat;

        [SerializeField] private AudioSource musicSource;
        [SerializeField] private double bpm = 75.0;
        [SerializeField] private double startDelaySeconds = 0.1;
        [SerializeField] private double beatOffsetSeconds = 0.0; // for calibration later

        private double _beatInterval;
        private double _dspStart;
        private int _lastBeat = -1;
        private bool _running;

        public void StartClock()
        {
            _beatInterval = 60.0 / bpm;
            _dspStart = AudioSettings.dspTime + startDelaySeconds;

            if (musicSource != null)
                musicSource.PlayScheduled(_dspStart);

            _running = true;
            _lastBeat = -1;
        }

        private void Start()
        {
            StartClock();
        }

        private void Update()
        {
            if (!_running) return;

            double songTime = AudioSettings.dspTime - _dspStart + beatOffsetSeconds;
            if (songTime < 0) return;

            int beat = (int)Math.Floor(songTime / _beatInterval);
            if (beat > _lastBeat)
            {
                _lastBeat = beat;
                OnBeat?.Invoke(beat);
            }
        }
    }
}