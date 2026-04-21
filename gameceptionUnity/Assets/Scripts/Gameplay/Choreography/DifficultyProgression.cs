using Rhythm;
using UnityEngine;

namespace Gameplay.Choreography
{
    public class DifficultyProgression : MonoBehaviour
    {
        public enum ProgressionCurve
        {
            Linear,
            ExponentialBounded
        }

        [Header("References")]
        [SerializeField] private BeatClock beatClock;
        [SerializeField] private PromptQueue promptQueue;

        [Header("Session")]
        [SerializeField, Min(1f)] private float sessionDurationSeconds = 300f; // 5 minutes
        [SerializeField, Min(0.05f)] private float updateIntervalSeconds = 10f;
        [SerializeField] private bool autoStart = true;

        [Header("BPM Range")]
        [SerializeField] private bool useBeatClockBpmAsStart = true;
        [SerializeField, Min(1f)] private float explicitStartBpm = 60f;
        [SerializeField, Min(0f)] private float maxBpmDelta = 30f;

        [Header("Function 1: Progression y(t)")]
        [SerializeField] private ProgressionCurve curve = ProgressionCurve.ExponentialBounded;
        [SerializeField, Range(0.1f, 8f)] private float exponentialSharpness = 2.2f;

        private bool _running;
        private float _startTime;
        private float _nextTickTime;
        private float _startBpmResolved;

        private void Start()
        {
            if (autoStart)
                StartProgression();
        }

        private void Update()
        {
            if (!_running || beatClock == null) return;

            if (Time.time >= _nextTickTime)
            {
                float elapsed = Time.time - _startTime;
                ApplyBpmAtTime(elapsed);
                _nextTickTime = Time.time + updateIntervalSeconds;

                if (elapsed >= sessionDurationSeconds)
                    _running = false;
            }
        }

        public void StartProgression()
        {
            if (beatClock == null)
            {
                Debug.LogWarning("[DifficultyProgression] BeatClock is missing.");
                return;
            }

            _startBpmResolved = useBeatClockBpmAsStart ? (float)beatClock.BPM : explicitStartBpm;
            _startTime = Time.time;
            _nextTickTime = Time.time; // apply immediately
            _running = true;

            ApplyBpmAtTime(0f);
        }

        public void StopProgression()
        {
            _running = false;
        }

        public void ResetProgression()
        {
            if (beatClock == null) return;

            _running = false;
            beatClock.SetBpm(_startBpmResolved);
        }

        // interpret y(t) as BPM in [startBpm, startBpm + delta]
        private void ApplyBpmAtTime(float elapsedSeconds)
        {
            float y = EvaluateProgress01(elapsedSeconds);
            float targetBpm = _startBpmResolved + y * maxBpmDelta;
            beatClock.SetBpm(targetBpm);

            Debug.Log($"[DifficultyProgression] t={elapsedSeconds:F1}s, y={y:F3}, bpm={targetBpm:F2}");
        }

        // y(t) in [0,1], t in [0, sessionDurationSeconds]
        public float EvaluateProgress01(float elapsedSeconds)
        {
            float x = Mathf.Clamp01(elapsedSeconds / sessionDurationSeconds);

            switch (curve)
            {
                case ProgressionCurve.Linear:
                    // y = x
                    return x;

                case ProgressionCurve.ExponentialBounded:
                default:
                    // bounded exponential: y = (1 - e^(-k*x)) / (1 - e^(-k))
                    // avoids infinity, smooth early progression, accelerates later
                    float k = Mathf.Max(0.0001f, exponentialSharpness);
                    float numerator = 1f - Mathf.Exp(-k * x);
                    float denominator = 1f - Mathf.Exp(-k);
                    return denominator > 0f ? numerator / denominator : x;
            }
        }

        public float EvaluateTargetBpm(float elapsedSeconds)
        {
            return _startBpmResolved + EvaluateProgress01(elapsedSeconds) * maxBpmDelta;
        }
    }
}