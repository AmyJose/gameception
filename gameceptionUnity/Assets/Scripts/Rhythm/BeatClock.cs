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
        [SerializeField] private double bpm = 60.0;
        [SerializeField] private double startDelaySeconds = 0.1;

        [SerializeField] private double beatOffsetSeconds = 0.0; // for calibration later

        private double _beatInterval;
        private double _dspStart;
        private int _lastBeat = -1;
        private bool _running;

        public double BPM => bpm;
        public double BeatInterval => _beatInterval;

        public float CurrentBeat
        {
            get
            {
                if (!_running || _beatInterval <= 0) return 0f;
                
                double songTime = AudioSettings.dspTime - _dspStart + beatOffsetSeconds;
                if (songTime < 0) return 0f; // Song hasn't started yet
                
                return (float)(songTime / _beatInterval);
            }
        }

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

        public void SetBpm(double newBpm)
        {
            if (!_running)
            {
                bpm = newBpm;
                _beatInterval = 60.0 / bpm;
                return;
            }

            // Current song time under old BPM
            double currentSongTime = AudioSettings.dspTime - _dspStart + beatOffsetSeconds;

            // Compute current beat position before changing BPM
            double currentBeat = currentSongTime / _beatInterval;

            // Apply new BPM
            bpm = newBpm;
            _beatInterval = 60.0 / bpm;

            // Recalculate dspStart so beat position stays continuous
            _dspStart = AudioSettings.dspTime - (currentBeat * _beatInterval) + beatOffsetSeconds;
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