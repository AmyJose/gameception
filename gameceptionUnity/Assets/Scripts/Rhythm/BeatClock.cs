using System;
using UnityEngine;

namespace Rhythm
{
    [Serializable]
    public struct BeatInfo
    {
        public int beatIndex;          // 0,1,2,...
        public double dspSongTime;     // seconds since song start (DSP time)
        public double beatInterval;    // seconds per beat
        public double phase;           // 0..1 how far through the current beat we are at callback time (usually ~0)
    }
    public class BeatClock : MonoBehaviour
    {
        public event Action<BeatInfo> OnBeat;

        [SerializeField] private AudioSource musicSource;
        [SerializeField] private double bpm = 5.0;
        [SerializeField] private double startDelaySeconds = 0.1;

        [SerializeField] private double beatOffsetSeconds = 0.0; // for calibration later

        private double _beatInterval;
        private double _dspStart;
        private int _lastBeat = -1;
        private bool _running;

        public double BPM => bpm;
        public double BeatInterval => _beatInterval;

        public void StartClock()
        {
            _beatInterval = 60.0 / bpm;
            _dspStart = AudioSettings.dspTime + startDelaySeconds;

            if (musicSource != null)
                musicSource.PlayScheduled(_dspStart);

            _running = true;
            _lastBeat = -1;
        }

        public void StopClock()
        {
            _running = false;
            musicSource.Stop();
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

                double beatTime = beat * _beatInterval;
                double phase = (songTime - beatTime) / _beatInterval;

                OnBeat?.Invoke(new BeatInfo
                {
                    beatIndex = beat,
                    dspSongTime = songTime,
                    beatInterval = _beatInterval,
                    phase = phase
                });
            }
        }
    }
}