using Gameplay;
using Gameplay.Choreography;
using UnityEngine;

public class PlanetJudgementRouter : MonoBehaviour
{
    [SerializeField] private PromptJudge promptJudge;
    [SerializeField] private PlanetManager planetManager;
    private void OnEnable()
    {
        if (promptJudge != null)
            promptJudge.OnJudged += HandleJudged;
    }

    private void OnDisable()
    {
        if (promptJudge != null)
            promptJudge.OnJudged -= HandleJudged;
    }

    private void HandleJudged(PromptJudge.JudgementResult result)
    {
        if (planetManager == null) return;

        foreach (var planet in planetManager.Planets)
        {
            if (planet == null) continue;
            if (planet.PlanetIndex != result.laneIndex) continue;

            planet.ApplyJudgement(result);
            return;
        }
    }
}
