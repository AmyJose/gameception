using UnityEngine;

namespace InputLayer
{
    public enum ElementPose { None, Earth, Water, Fire, Ice }

    public class PoseState : MonoBehaviour
    {
        public ElementPose CurrentPose { get; private set; } = ElementPose.None;
        public float Confidence { get; private set; } = 0f;
        public long LastTimestampMs { get; private set; } = 0;

        public void SetPose(ElementPose pose, float confidence, long timestampMs)
        {
            CurrentPose = pose;
            Confidence = confidence;
            LastTimestampMs = timestampMs;
        }
    }
}