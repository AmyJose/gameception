using System.Collections.Generic;
using UnityEngine;
using Gameplay.Choreography;
using Rhythm;

public class AdaptiveDifficultyController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PromptJudge promptJudge;
    [SerializeField] private PromptQueue promptQueue;
    [SerializeField] private BeatClock beatClock;

    [Header("Difficulty")]
    [SerializeField] private float difficulty = 0f;              // 0 = easiest, 1 = hardest
    [SerializeField] private float baseIncreasePerSequence = 0.02f;
    [SerializeField] private float performanceInfluence = 0.30f;
    [SerializeField] private float targetAccuracy = 0.75f;

    [Header("Performance Tracking")]
    [SerializeField] private int movingAverageWindow = 3;
    private readonly List<float> recentAccuracies = new();

    [Header("BPM")]
    [SerializeField] private float bpmIncreaseRange = 20f;       // max BPM = start BPM + this
    [SerializeField] private float bpmSmoothing = 0.2f;
    private float startBpm;
    private float maxBpm;

    [Header("Structure")]
    [SerializeField] private int minPromptsPerSequence = 2;
    [SerializeField] private int maxPromptsPerSequence = 5;
    [SerializeField] private int maxExtraLaneRepeats = 2;

    private void OnEnable()
    {
        if (promptJudge != null)
            promptJudge.OnSequenceComplete += HandleSequenceComplete;
    }

    private void OnDisable()
    {
        if (promptJudge != null)
            promptJudge.OnSequenceComplete -= HandleSequenceComplete;
    }

    private void Start()
    {
        if (beatClock != null)
        {
            startBpm = (float)beatClock.BPM;
            maxBpm = startBpm + bpmIncreaseRange;
        }

        ApplyDifficulty();
    }

    private void HandleSequenceComplete(PromptJudge.SequenceResult result)
    {
        recentAccuracies.Add(result.accuracy);

        if (recentAccuracies.Count > movingAverageWindow)
            recentAccuracies.RemoveAt(0);

        float averageAccuracy = GetAverageAccuracy();

        // Main difficulty equation
        difficulty = Mathf.Clamp01(
            difficulty
            + baseIncreasePerSequence
            + performanceInfluence * (averageAccuracy - targetAccuracy)
        );

        ApplyDifficulty();

        Debug.Log(
            $"[AdaptiveDifficulty] Seq {result.sequenceId} | " +
            $"accuracy={result.accuracy:F2} | avg={averageAccuracy:F2} | difficulty={difficulty:F2}"
        );
    }

    private float GetAverageAccuracy()
    {
        if (recentAccuracies.Count == 0)
            return targetAccuracy;

        float sum = 0f;
        foreach (float accuracy in recentAccuracies)
            sum += accuracy;

        return sum / recentAccuracies.Count;
    }

    private void ApplyDifficulty()
    {
        ApplyBpm();
        ApplyStructure();
    }

    private void ApplyBpm()
    {
        if (beatClock == null) return;

        float targetBpm = Mathf.Lerp(startBpm, maxBpm, difficulty);
        float newBpm = Mathf.Lerp((float)beatClock.BPM, targetBpm, bpmSmoothing);

        beatClock.SetBpm(newBpm);

        Debug.Log($"[AdaptiveDifficulty] BPM={newBpm:F1}");
    }

    private void ApplyStructure()
    {
        if (promptQueue == null) return;

        int promptsPerSequence = Mathf.RoundToInt(
            Mathf.Lerp(minPromptsPerSequence, maxPromptsPerSequence, difficulty)
        );

        int extraRepeats = Mathf.FloorToInt(
            Mathf.Lerp(0, maxExtraLaneRepeats + 1, difficulty)
        );
        extraRepeats = Mathf.Clamp(extraRepeats, 0, maxExtraLaneRepeats);

        promptQueue.SetPromptsPerSequence(promptsPerSequence);
        promptQueue.SetExtraLaneRepeatsPerSequence(extraRepeats);

        Debug.Log(
            $"[AdaptiveDifficulty] Prompts/Seq={promptsPerSequence}, ExtraRepeats={extraRepeats}"
        );
    }
}