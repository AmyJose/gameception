using Mediapipe.Tasks.Vision.PoseLandmarker;

//interface for pose classification
namespace InputLayer
{
    public struct PoseClassification
    {
        public ElementPose pose;
        public float confidence;
    }

    public interface IPoseClassifier
    {
        PoseClassification Classify(PoseLandmarkerResult result);
    }
}