using System.Collections.Generic;
using UnityEngine;

namespace Audio
{
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [SerializeField] private AudioSource melodySource;
        [SerializeField] private AudioSource beatSource;
        [SerializeField] private List<AudioSource> planetSources;

        [Header("Volumes")]
        [SerializeField] private float melodyVolume = 1f;
        [SerializeField] private float beatVolume = 0.8f;
        [SerializeField] private float gameplayPlanetVolume = 1f;
        [SerializeField] private float resultsPlanetVolume = 0.5f;

        [Header("Fade Settings")]
        [SerializeField] private float fadeInDuration = 0.35f;
        [SerializeField] private float fadeOutDuration = 0.6f;

        private readonly Dictionary<AudioSource, float> _targetVolumes = new();

        private double _dspStartTime;

        public double DspStartTime => _dspStartTime;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitialiseMusic();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
        private void Update()
        {
            FadeAllSources();
        }

        private void InitialiseMusic()
        {
            _dspStartTime = AudioSettings.dspTime + 0.1;

            SetupAndScheduleSource(melodySource, melodyVolume);
            SetupAndScheduleSource(beatSource, 0f);

            foreach (AudioSource planet in planetSources)
            {
                SetupAndScheduleSource(planet, 0f);
            }
        }
        private void SetupAndScheduleSource(AudioSource source, float startingVolume)
        {
            if (source == null) return;

            source.playOnAwake = false;
            source.loop = true;
            source.volume = startingVolume;

            _targetVolumes[source] = startingVolume;

            source.PlayScheduled(_dspStartTime);
        }
        public void SetMenuMode()
        {
            SetTargetVolume(melodySource, melodyVolume);
            SetTargetVolume(beatSource, 0f);

            foreach (var planet in planetSources)
                SetTargetVolume(planet, 0f);
        }
        public void SetTutorialMode()
        {
            SetMenuMode();
        }
        public void SetGameplayMode()
        {
            SetTargetVolume(melodySource, melodyVolume);
            SetTargetVolume(beatSource, beatVolume);

            foreach (AudioSource planet in planetSources)
            {
                SetTargetVolume(planet, 0f);
            }
        }
        public void SetGameplaySelectedPlanets(IReadOnlyCollection<int> selectedIndices)
        {
            SetGameplayMode();

            if (selectedIndices == null) return;

            foreach (int index in selectedIndices)
            {
                if (index >= 0 && index < planetSources.Count)
                {
                    SetTargetVolume(planetSources[index], gameplayPlanetVolume);
                }
            }
        }
        public void SetResultsMode()
        {
            SetTargetVolume(melodySource, melodyVolume);
            SetTargetVolume(beatSource, beatVolume);

            foreach (AudioSource planet in planetSources)
            {
                SetTargetVolume(planet, resultsPlanetVolume);
            }
        }
        private void SetTargetVolume(AudioSource source, float targetVolume)
        {
            if (source == null) return;

            _targetVolumes[source] = Mathf.Clamp01(targetVolume);
        }
        private void FadeAllSources()
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