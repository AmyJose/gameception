using System;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine;

namespace InputLayer
{
    //uses the pose classification interface
    public class HeuristicPoseClassifier : MonoBehaviour, IPoseClassifier
    {
        private static class Joints
        {
            public const int LeftShoulder = 12, LeftElbow = 14, LeftWrist = 16;
            public const int RightShoulder = 11, RightElbow = 13, RightWrist = 15;
            public const int LeftHip = 24, LeftKnee = 26, LeftAnkle = 28;
            public const int RightHip = 23, RightKnee = 25, RightAnkle = 27;
        }

        [Header("Thresholds")]
        [SerializeField] private double angleToleranceDeg = 12.0;
        [SerializeField] private float yTolerance = 0.06f;
        [SerializeField] private float xTolerance = 0.06f;

        public PoseClassification Classify(PoseLandmarkerResult result)
        {
            if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
                return new PoseClassification { pose = ElementPose.None, confidence = 0f };

            var landmarks = result.poseLandmarks[0].landmarks;
            if (landmarks == null || landmarks.Count == 0)
                return new PoseClassification { pose = ElementPose.None, confidence = 0f };

            double lArmAngle = JointAngleDeg(landmarks[Joints.LeftShoulder], landmarks[Joints.LeftElbow], landmarks[Joints.LeftWrist]);
            double rArmAngle = JointAngleDeg(landmarks[Joints.RightShoulder], landmarks[Joints.RightElbow], landmarks[Joints.RightWrist]);

            var lArmYDiff = Mathf.Abs(landmarks[Joints.LeftWrist].y - landmarks[Joints.LeftShoulder].y);
            var rArmYDiff = Mathf.Abs(landmarks[Joints.RightWrist].y - landmarks[Joints.RightShoulder].y);
            var lElbowInline = Mathf.Abs(landmarks[Joints.LeftElbow].y - landmarks[Joints.LeftShoulder].y);
            var rElbowInline = Mathf.Abs(landmarks[Joints.RightElbow].y - landmarks[Joints.RightShoulder].y);
            var lArmXDiff = Mathf.Abs(landmarks[Joints.LeftWrist].x - landmarks[Joints.LeftShoulder].x);
            var rArmXDiff = Mathf.Abs(landmarks[Joints.RightWrist].x - landmarks[Joints.RightShoulder].x);

            // Decide pose
            if (IsEarthPose(lArmAngle, rArmAngle, lArmYDiff, rArmYDiff))
                return new PoseClassification { pose = ElementPose.Earth, confidence = 1f };

            if (IsWaterPose(lArmAngle, rArmAngle,
                    landmarks[Joints.LeftWrist].y, landmarks[Joints.LeftShoulder].y,
                    landmarks[Joints.RightWrist].y, landmarks[Joints.RightShoulder].y))
                return new PoseClassification { pose = ElementPose.Water, confidence = 1f };

            if (IsFirePose(lArmAngle, rArmAngle,
                    landmarks[Joints.LeftWrist].y, landmarks[Joints.LeftShoulder].y,
                    landmarks[Joints.RightWrist].y, landmarks[Joints.RightShoulder].y,
                    lElbowInline, rElbowInline))
                return new PoseClassification { pose = ElementPose.Fire, confidence = 1f };

            if (IsIcePose(lArmAngle, rArmAngle,
                    landmarks[Joints.LeftWrist].y, landmarks[Joints.LeftShoulder].y,
                    landmarks[Joints.RightWrist].y, landmarks[Joints.RightShoulder].y,
                    lArmXDiff, rArmXDiff))
                return new PoseClassification { pose = ElementPose.Ice, confidence = 1f };

            return new PoseClassification { pose = ElementPose.None, confidence = 0f };
        }

        private static double JointAngleDeg(NormalizedLandmark firstPoint, NormalizedLandmark midPoint, NormalizedLandmark lastPoint)
        {
            double angle =
                (Math.Atan2(lastPoint.y - midPoint.y, lastPoint.x - midPoint.x)
                - Math.Atan2(firstPoint.y - midPoint.y, firstPoint.x - midPoint.x))
                * (180.0 / Math.PI);

            angle = Math.Abs(angle);
            if (angle > 180.0) angle = 360.0 - angle;
            return angle;
        }

        private bool IsEarthPose(double leftArmAngle, double rightArmAngle, float leftArmYDiff, float rightArmYDiff)
        {
            bool armsLevel = leftArmYDiff <= yTolerance && rightArmYDiff <= yTolerance;
            bool armsStraight =
                IsWithin(leftArmAngle, 180.0, angleToleranceDeg) &&
                IsWithin(rightArmAngle, 180.0, angleToleranceDeg);
            return armsLevel && armsStraight;
        }

        private bool IsWaterPose(double leftArmAngle, double rightArmAngle, float leftWristY, float leftShoulderY, float rightWristY, float rightShoulderY)
        {
            bool armsAboveHead = leftWristY < leftShoulderY && rightWristY < rightShoulderY;
            bool armsAngledInwards =
                IsWithin(leftArmAngle, 130.0, angleToleranceDeg) &&
                IsWithin(rightArmAngle, 130.0, angleToleranceDeg);
            return armsAboveHead && armsAngledInwards;
        }

        private bool IsFirePose(double leftArmAngle, double rightArmAngle, float leftWristY, float leftShoulderY, float rightWristY, float rightShoulderY, float lElbowInline, float rElbowInline)
        {
            bool armsBelowShoulder = leftWristY > leftShoulderY && rightWristY > rightShoulderY;
            bool elbowsLevel = lElbowInline <= yTolerance && rElbowInline <= yTolerance;
            bool armsAngled =
                IsWithin(leftArmAngle, 90.0, angleToleranceDeg) &&
                IsWithin(rightArmAngle, 90.0, angleToleranceDeg);
            return armsAngled && armsBelowShoulder && elbowsLevel;
        }

        private bool IsIcePose(double leftArmAngle, double rightArmAngle, float leftWristY, float leftShoulderY, float rightWristY, float rightShoulderY, float leftArmXDiff, float rightArmXDiff)
        {
            bool armsAboveShoulder = leftWristY < leftShoulderY && rightWristY < rightShoulderY;
            bool armsAligned = leftArmXDiff <= xTolerance && rightArmXDiff <= xTolerance;
            bool armsStraight =
                IsWithin(leftArmAngle, 180.0, angleToleranceDeg) &&
                IsWithin(rightArmAngle, 180.0, angleToleranceDeg);
            return armsStraight && armsAboveShoulder && armsAligned;
        }

        private static bool IsWithin(double value, double target, double tolerance) =>
            value >= target - tolerance && value <= target + tolerance;
    }
}