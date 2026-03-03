using Mediapipe.Tasks.Vision.PoseLandmarker;

namespace InputLayer
{
    public struct PoseClassification
    {
        public ElementPose pose;
        public float confidence; // 0..1
    }

    public interface IPoseClassifier
    {
        PoseClassification Classify(PoseLandmarkerResult result);
    }
}