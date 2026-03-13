using UnityEngine;
using Rhythm;

public class BeatMetronome : MonoBehaviour
{
    [SerializeField] private BeatClock beatClock;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip tickSound;

    private void OnEnable()
    {
        if (beatClock != null)
        {
            beatClock.OnBeat += PlayTick;
        }
    }
    private void OnDisable()
    {
        if (beatClock != null)
        {
            beatClock.OnBeat -= PlayTick;
        }
    }

    private void PlayTick(BeatInfo beat)
    {
        if (audioSource != null && tickSound != null)
        {
            audioSource.PlayOneShot(tickSound);
        }
    }
}
