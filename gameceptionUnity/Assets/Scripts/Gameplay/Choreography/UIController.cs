using System.Collections.Generic;
using Gameplay.Choreography;
using UnityEngine;

namespace Gameplay.Choreography.UI
{
    // Listens to judge results, colours icons green/red, removes on success
    public class UIController : MonoBehaviour
    {
        [SerializeField] private PromptQueue queue;
        [SerializeField] private PromptJudge judge;



        private Dictionary<int, PromptIndicator> _promptMap = new();

        private void OnEnable()
        {
            if (queue != null)
                queue.OnPromptEnteredZone += (data) =>
                {
                    // Caches the indicator when it enters zone (will be used when judge result comes)
                };

            if (judge != null)
                judge.OnJudged += HandleJudged;
        }

        private void OnDisable()
        {
            if (judge != null)
                judge.OnJudged -= HandleJudged;
        }

        private void HandleJudged(PromptJudge.JudgementResult result)
        {
            var prompts = queue.GetComponentsInChildren<PromptIndicator>();
            PromptIndicator found = null;

            foreach (var p in prompts)
            {
                if (p.GetPromptId() == result.promptId)
                {
                    found = p;
                    break;
                }
            }

            if (found == null) return;

            if (result.quality == PromptJudge.HitQuality.WrongPose ||
                result.quality == PromptJudge.HitQuality.NoInput ||
                result.quality == PromptJudge.HitQuality.WrongPlanet)
            {
                found.SetFail();
                //Debug.Log($"❌ Prompt {result.promptId} FAIL: {result.quality}");
            }
            else if (result.quality == PromptJudge.HitQuality.Perfect &&
                     result.timing == PromptJudge.PoseTiming.Perfect)
            {
                found.SetSuccess();
                //Debug.Log($"💎 Prompt {result.promptId} PERFECT HIT");
            }
            else
            {
                found.SetMidHit();
                //Debug.Log($"✅ Prompt {result.promptId} GOOD HIT ({result.timing})");
            }
        }
    }
}