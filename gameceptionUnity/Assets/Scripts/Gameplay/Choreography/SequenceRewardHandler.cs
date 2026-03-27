using UnityEngine;
using Gameplay.Choreography;

namespace Gameplay.Choreography
{
    // Listens to sequence completion and perfomrs actions (rewards)
    // i.e. resource gains, visual effects, etc.
    
    public class SequenceRewardHandler : MonoBehaviour
    {
        [SerializeField] private PromptJudge judge;
        [SerializeField] private PlanetManager planetManager;

        private void OnEnable()
        {
            if (judge != null)
                judge.OnSequenceComplete += HandleSequenceComplete;
        }

        private void OnDisable()
        {
            if (judge != null)
                judge.OnSequenceComplete -= HandleSequenceComplete;
        }

        private void HandleSequenceComplete(PromptJudge.SequenceResult result)
        {
            Debug.Log($"🎉 [SequenceRewardHandler] Sequence {result.sequenceId} completed!");
            Debug.Log($"   Accuracy: {result.accuracy:P0}");
            Debug.Log($"   Hits: {result.hitsCount}/{result.totalPrompts}");

            // eg. Award points based on accuracy
            int basePoints = 100;
            int bonusPoints = Mathf.RoundToInt(result.accuracy * 50);
            int totalPoints = basePoints + bonusPoints;

            Planet planet = planetManager?.GetPlanet(result.selectedPad);
            if (planet != null)
            {
                planet.ApplySequenceReward(totalPoints, result.detectedPose);
            }



            Debug.Log($"   🏆 Awarded {totalPoints} points!");

            // TODO: Possibly integrate with resource system, combo system, etc.
        }
    }
}
