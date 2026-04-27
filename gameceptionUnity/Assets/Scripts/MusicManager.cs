using UnityEngine;

namespace Audio
{
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [SerializeField] private AudioSource melodySource;

        public AudioSource MelodySource => melodySource;

        private double _dspStartTime;

        public double DspStartTime => _dspStartTime;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                StartMelody();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void StartMelody()
        {
            if (melodySource == null) return;

            melodySource.playOnAwake = false;
            melodySource.loop = true;

            _dspStartTime = AudioSettings.dspTime + 0.1;
            melodySource.PlayScheduled(_dspStartTime);
        }

        public double GetSongTime()
        {
            if (melodySource == null || melodySource.clip == null)
                return 0;

            double rawTime = AudioSettings.dspTime - _dspStartTime;
            double loopLength = melodySource.clip.length;

            return rawTime % loopLength;
        }
    }
}