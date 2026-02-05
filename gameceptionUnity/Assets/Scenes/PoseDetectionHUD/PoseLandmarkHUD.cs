using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class PoseLandmarkHUD : MonoBehaviour
{
    //indices from Mediapipe poselandmarker landmark diagram
    private static class Joints
    {
        // Left
        public const int LeftShoulder = 12;
        public const int LeftElbow = 14;
        public const int LeftWrist = 16;

        public const int LeftHip = 24;
        public const int LeftKnee = 26;
        public const int LeftAnkle = 28;

        // Right
        public const int RightShoulder = 11;
        public const int RightElbow = 13;
        public const int RightWrist = 15;

        public const int RightHip = 23;
        public const int RightKnee = 25;
        public const int RightAnkle = 27;
    }


    [Header("UI")]
    [SerializeField] private TMP_Text landmarksText;
    [SerializeField] private TMP_Text poseText;

    [Header("Detection thresholds")]
    [SerializeField] private double angleToleranceDeg = 12.0;
    [SerializeField] private float yTolerance = 0.06f;

    private readonly object _lock = new object();
    private string _pendingLandmarkText;
    private string _pendingPoseText;
    private bool _hasPending;

    private readonly StringBuilder sb = new StringBuilder(512);

    /// <summary>
    /// Called from Mediapipe callback thread. Do NOT touch Unity UI here
    /// </summary>
    public void EnqueueResult(PoseLandmarkerResult result)
    {
        (string landmarksBuilt, string poseBuilt) = BuildTexts(result);

        lock (_lock)
        {
            _pendingLandmarkText = landmarksBuilt;
            _pendingPoseText = poseBuilt;
            _hasPending = true;
        }
    }

    private void Update()
    {
        if (landmarksText == null && poseText == null) return;
        string lm = null;
        string pose = null;

        lock (_lock)
        {
            if (_hasPending)
            {
                lm = _pendingLandmarkText;
                pose = _pendingPoseText;
                _hasPending = false;
            }
        }

        if (lm != null && landmarksText != null) landmarksText.text = lm;
        if (pose!= null && poseText != null) poseText.text = pose;
    }

    private (string landmarksTextOut, string poseTextOut) BuildTexts(PoseLandmarkerResult result)
    {
        if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
        {
            return ("No pose", "None");
        }

        var landmarks = result.poseLandmarks[0].landmarks;
        if (landmarks == null || landmarks.Count== 0)
        {
            return ("No pose", "None");
        }

        // Landmarks debug text
        sb.Clear();
        sb.AppendLine($"Landmarks Detected: {landmarks.Count}");

        double lArmAngle = JointAngleDeg(landmarks[Joints.LeftShoulder], landmarks[Joints.LeftElbow], landmarks[Joints.LeftWrist]);
        double rArmAngle = JointAngleDeg(landmarks[Joints.RightShoulder], landmarks[Joints.RightElbow], landmarks[Joints.RightWrist]);
        double lLegAngle = JointAngleDeg(landmarks[Joints.LeftHip], landmarks[Joints.LeftKnee], landmarks[Joints.LeftAnkle]);
        double rLegAngle = JointAngleDeg(landmarks[Joints.RightHip], landmarks[Joints.RightKnee], landmarks[Joints.RightAnkle]);

        sb.AppendLine($"LArm Angle: {lArmAngle:F1}");
        sb.AppendLine($"RArm Angle: {rArmAngle:F1}");
        sb.AppendLine($"LLeg Angle: {lLegAngle:F1}");
        sb.AppendLine($"RLeg Angle: {rLegAngle:F1}");

        var lArmYDiff = Math.Abs(landmarks[Joints.LeftWrist].y - landmarks[Joints.LeftShoulder].y);
        var rArmYDiff = Math.Abs(landmarks[Joints.RightWrist].y - landmarks[Joints.RightShoulder].y);

        sb.AppendLine($"LArm Y Difference: {lArmYDiff:F2}");
        sb.AppendLine($"RArm Y Difference: {rArmYDiff:F2}");

        string landmarksOut = sb.ToString();

        //Decide pose text
        string poseOut = "None";

        // get this into a switch statement?
        if (isEarthPose(lArmAngle, rArmAngle, lArmYDiff, rArmYDiff))
        {
            poseOut = "Earth Pose";
        }
        else if (isWaterPose(lArmAngle, rArmAngle, landmarks[Joints.LeftWrist].y, landmarks[Joints.LeftShoulder].y, landmarks[Joints.RightWrist].y, landmarks[Joints.RightShoulder].y))
        {
            poseOut = "Water Pose";
        }
        else poseOut = "No Pose";

            return (landmarksOut, poseOut);
    }

    /// <summary>
    /// Computes the angle at midPoint formed by firstPoint-midPoint-lastPoint, in degrees [0, 180]
    /// </summary>
    /// <param name="firstPoint"></param>
    /// <param name="midPoint"></param>
    /// <param name="lastPoint"></param>
    /// <returns></returns>
    private static double JointAngleDeg(NormalizedLandmark firstPoint, NormalizedLandmark midPoint, NormalizedLandmark lastPoint)
    {
        double angle =
            (Math.Atan2(lastPoint.y - midPoint.y, lastPoint.x - midPoint.x)
            - Math.Atan2(firstPoint.y - midPoint.y, firstPoint.x - midPoint.x))
            * (180.0 / Math.PI); //convert to degrees

        angle = Math.Abs(angle);
        if (angle > 180.00) angle = 360.0 - angle;

        return angle;
    }

    /// <summary>
    /// Earth Pose = T Pose.
    /// Tests both elbows are at 180 degree angles, wrists are in the same y as shoulders
    /// </summary>
    /// <param name="leftArmAngle"></param>
    /// <param name="rightArmAngle"></param>
    /// <param name="leftArmYDiff"></param>
    /// <param name="rightArmYDiff"></param>
    /// <returns>True if T pose conditions are met</returns>
    private bool isEarthPose(double leftArmAngle, double rightArmAngle, float leftArmYDiff, float rightArmYDiff)
    {
        bool armsLevel = leftArmYDiff <= yTolerance && rightArmYDiff <= yTolerance;

        bool armsStraight =
            isWithin(leftArmAngle, 180.0, angleToleranceDeg) &&
            isWithin(rightArmAngle, 180.0, angleToleranceDeg);

        return armsLevel && armsStraight;
    }

    /// <summary>
    /// Water Pose = touching hands above your head.
    /// Tests elbows are at 130 degrees and wrists are above shoulders
    /// </summary>
    /// <param name="leftArmAngle"></param>
    /// <param name="rightArmAngle"></param>
    /// <param name="leftWristY"></param>
    /// <param name="leftShoulderY"></param>
    /// <param name="rightWristY"></param>
    /// <param name="rightShoulderY"></param>
    /// <returns>True if hands above head and arms at 130 angle</returns>
    private bool isWaterPose(double leftArmAngle, double rightArmAngle, float leftWristY, float leftShoulderY, float rightWristY, float rightShoulderY)
    {
        bool armsAboveHead = leftWristY < leftShoulderY && rightWristY < rightShoulderY;

        bool armsAngledInwards = 
            isWithin(leftArmAngle, 130.0, angleToleranceDeg) && 
            isWithin(rightArmAngle, 130.0, angleToleranceDeg);

        return armsAngledInwards && armsAboveHead;
    }

    private static bool isWithin(double value, double target, double tolerance) => 
        value >= target-tolerance && value <= target+tolerance; 
}
