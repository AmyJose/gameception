using UnityEngine;
using System.Collections.Generic;
using Gameplay;
using InputLayer;

public class PromptSelector : MonoBehaviour
{
    [SerializeField] private PlanetManager planetManager;
    [SerializeField] private bool debugMode = false;
    private Dictionary<int, ElementPose> _lastPoseByLane = new();
    private Dictionary<int, int> _samePoseStreakByLane = new();

    public ElementPose SelectPromptForLane(int laneIndex)
    {
        PlanetDefinition definition = GetDefinitionForLane(laneIndex);

        bool hasDefinitionWeights = definition != null
            && definition.promptPoseWeights != null
            && definition.promptPoseWeights.Count > 0;

        bool useOffTheme = hasDefinitionWeights && UnityEngine.Random.value < definition.offThemeChance;

        ElementPose candidate;
        if (!hasDefinitionWeights || useOffTheme)
        {
            candidate = GetFallbackRandomPose();
        }
        else
        {
            candidate = SampleWeightedPose(definition, laneIndex, excludedPose: ElementPose.None);
            if (candidate == ElementPose.None)
                candidate = GetFallbackRandomPose();
        }

        candidate = ApplyStreakConstraint(candidate, definition, laneIndex);
        UpdateLaneHistory(laneIndex, candidate);

        if (debugMode)
        {
            Debug.Log($"[PromptSelector] lane {laneIndex} => {candidate} " +
                      $"(offTheme={useOffTheme}, hasWeights={hasDefinitionWeights})");
        }

        return candidate;
    }

    private PlanetDefinition GetDefinitionForLane(int laneIndex)
    {
        if (planetManager == null)
            return null;

        foreach (var planet in planetManager.Planets)
        {
            if (planet == null) continue;
            if (planet.PlanetIndex != laneIndex) continue;
            return planet.Definition;
        }

        return null;
    }

    private ElementPose ApplyStreakConstraint(ElementPose candidate, PlanetDefinition definition, int laneIndex)
    {
        if (!_lastPoseByLane.TryGetValue(laneIndex, out var lastPose))
            return candidate;

        if (!_samePoseStreakByLane.TryGetValue(laneIndex, out int streak))
            streak = 0;

        int maxStreak = definition != null ? Mathf.Max(1, definition.maxSamePoseStreak) : 2;
        if (candidate != lastPose || streak < maxStreak)
            return candidate;

        ElementPose rerolled = ElementPose.None;
        if (definition != null && definition.promptPoseWeights != null && definition.promptPoseWeights.Count > 0)
        {
            rerolled = SampleWeightedPose(definition, laneIndex, excludedPose: lastPose);
        }

        if (rerolled == ElementPose.None)
        {
            rerolled = GetFallbackRandomPose(excludedPose: lastPose);
        }

        return rerolled == ElementPose.None ? candidate : rerolled;
    }

    private ElementPose SampleWeightedPose(PlanetDefinition definition, int laneIndex, ElementPose excludedPose)
    {
        float totalWeight = 0f;
        var validEntries = new List<PoseWeight>(definition.promptPoseWeights.Count);

        _lastPoseByLane.TryGetValue(laneIndex, out var lastPose);
        _samePoseStreakByLane.TryGetValue(laneIndex, out int streak);

        float repeatPenalty = Mathf.Clamp01(definition.samePoseWeightPenalty);

        foreach (var entry in definition.promptPoseWeights)
        {
            if (entry.pose == ElementPose.None) continue;
            if (entry.pose == excludedPose) continue;

            float weight = Mathf.Max(0f, entry.weight);

            if (entry.pose == lastPose && streak > 0)
            {
                weight *= repeatPenalty;
            }

            if (weight <= 0f) continue;

            validEntries.Add(new PoseWeight { pose = entry.pose, weight = weight });
            totalWeight += weight;
        }

        if (totalWeight <= 0f || validEntries.Count == 0)
            return ElementPose.None;

        float pick = UnityEngine.Random.value * totalWeight;
        float running = 0f;

        for (int i = 0; i < validEntries.Count; i++)
        {
            running += validEntries[i].weight;
            if (pick <= running)
                return validEntries[i].pose;
        }

        return validEntries[validEntries.Count - 1].pose;
    }

    private ElementPose GetFallbackRandomPose(ElementPose excludedPose = ElementPose.None)
    {
        var candidates = new List<ElementPose>(4)
        {
            ElementPose.Earth,
            ElementPose.Water,
            ElementPose.Fire,
            ElementPose.Ice
        };

        if (excludedPose != ElementPose.None)
            candidates.Remove(excludedPose);

        if (candidates.Count == 0)
            return ElementPose.Earth;

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private void UpdateLaneHistory(int laneIndex, ElementPose selected)
    {
        if (_lastPoseByLane.TryGetValue(laneIndex, out var last) && last == selected)
        {
            _samePoseStreakByLane[laneIndex] = _samePoseStreakByLane.TryGetValue(laneIndex, out int streak)
                ? streak + 1
                : 1;
        }
        else
        {
            _lastPoseByLane[laneIndex] = selected;
            _samePoseStreakByLane[laneIndex] = 1;
        }
    }
}
