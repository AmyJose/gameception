using System;
using System.Collections.Generic;
using InputLayer;
using UnityEngine;

namespace Gameplay.Choreography
{
    [Serializable]
    [CreateAssetMenu(menuName = "Rhythm/Prompt Sequence")]
    public class PromptSequenceAsset : ScriptableObject
    {
        public List<PromptStep> steps;
    }

    [Serializable]
    public class PromptStep
    {
        public ElementPose pose;
    }
}