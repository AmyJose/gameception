using System.Collections.Generic;
using Gameplay.Choreography;
using InputLayer;
using UnityEngine;

namespace Gameplay
{
    // Handles rewards for successful choreography hits
    // Listens to ChoreographyJudge events and applies resources/population accordingly
    public class ChoreographyRewardSystem : MonoBehaviour
    {
        [Header("Rewards")]
        [SerializeField] private float perfectReward = 10f;
        [SerializeField] private float goodReward = 5f;
        [SerializeField] private float earlyLateReward = 2f;

        [Header("Dependencies")]
        [SerializeField] private ChoreographyJudge judge;
        [SerializeField] private PlanetManager planetManager;
        [SerializeField] private ComboSystem comboSystem;

        private void OnEnable()
        {
            if (judge != null)
                judge.OnPromptJudged += HandlePromptJudged;
        }

        private void OnDisable()
        {
            if (judge != null)
                judge.OnPromptJudged -= HandlePromptJudged;
        }

        private void HandlePromptJudged(ChoreographyJudge.JudgementResult result)
        {
            // Ignore misses
            if (result.quality == ChoreographyJudge.HitQuality.Miss || 
                result.quality == ChoreographyJudge.HitQuality.NoInput ||
                result.quality == ChoreographyJudge.HitQuality.WrongPose)
            {
                return;
            }

            // Determine reward amount
            float reward = result.quality switch
            {
                ChoreographyJudge.HitQuality.Perfect => perfectReward,
                ChoreographyJudge.HitQuality.Good => goodReward,
                ChoreographyJudge.HitQuality.Early or ChoreographyJudge.HitQuality.Late => earlyLateReward,
                _ => 0f
            };

            // Apply to selected planet
            if (result.selectedPad >= 0 && planetManager != null)
            {
                Planet planet = planetManager.GetPlanet(result.selectedPad);
                if (planet != null)
                {
                    planet.RestoreNeed(result.detectedPose);
                    
                    Debug.Log($"[ChoreographyReward] Planet {result.selectedPad} gained {reward} " +
                             $"population for {result.quality} {result.detectedPose}");
                }
            }

            //Register with combo system for potential combos
            if (comboSystem != null)
            {
                var targets = new List<int> { result.selectedPad };
                comboSystem.RegisterHit(result.detectedPose, targets, 0);
            }
        }
    }
}