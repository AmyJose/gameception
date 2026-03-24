using System;
using System.Collections.Generic;
using InputLayer;
using UnityEngine;

namespace Gameplay.Choreography
{
    // Immutable data structure representing a single choreography sequence.
    // Defines the poses to perform and their timing relative to beats.

    [System.Serializable]
    public class ChoreographySequence
    {
        [System.Serializable]
        public struct PromptAction
        {
            //Beat offset from sequence start (0, 1, 2, etc.)
            public int beatOffset;
            
            //Pose to be performed by the player
            public ElementPose requiredPose;
            
            //Unique ID for tracking this specific prompt
            public int promptId;
        }

        //All prompts in a sequence, in order
        public List<PromptAction> prompts = new();
        
        //Total duration in beats
        public int totalBeats;

        public ChoreographySequence(int totalBeats = 0)
        {
            this.totalBeats = totalBeats;
        }

        //Add a prompt at the given beat offset
        public void AddPrompt(int beatOffset, ElementPose pose, int promptId)
        {
            if (beatOffset < 0 || beatOffset >= totalBeats)
            {
                Debug.LogWarning($"[ChoreographySequence] Prompt beat offset {beatOffset} out of range [0, {totalBeats})");
                return;
            }

            prompts.Add(new PromptAction 
            { 
                beatOffset = beatOffset, 
                requiredPose = pose,
                promptId = promptId
            });

            prompts.Sort((a, b) => a.beatOffset.CompareTo(b.beatOffset));
        }
    }
}