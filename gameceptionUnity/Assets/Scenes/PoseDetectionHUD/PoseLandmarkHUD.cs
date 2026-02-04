using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class PoseLandmarkHUD : MonoBehaviour
{
    /// <summary>
    /// Define the joint mapping used for angles
    /// </summary>
    public static readonly Dictionary<string, int[]> jointMap = new Dictionary<string, int[]>()
    {
        {"LArm", new[]{12, 14, 16}}, //left shoulder, left elbow, left wrist
        {"RArm", new[]{11, 13, 15}}, //right shoulder, right elbow, right wrist
        {"LLeg", new[]{24, 26, 28}}, //left hip, left knee, left ankle
        {"RLeg", new[]{23, 25, 27}}  //right hip, right knee, right ankle
    };

    [SerializeField] private TMP_Text text;

    [SerializeField] private double angleThreshold = 10;
    [SerializeField] private float yThreshold = 0.07f;

    //Picking the landmarks to show
    //Landmarks to display (from landmark diagram in mediapipe)
    [SerializeField] private readonly int[] landmarkIndices = {
        0, 3, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 23, 24, 25, 26, 27, 28};

    private readonly object _lock = new object();
    private string _pendingText;
    private bool _hasPendingText;

    private readonly StringBuilder sb = new StringBuilder(512);

    /// <summary>
    /// Called from Mediapipe callback thread. Do NOT touch Unity UI here
    /// </summary>
    public void EnqueueResult(PoseLandmarkerResult result)
    {
        string built = BuildText(result);

        lock (_lock)
        {
            _pendingText = built;
            _hasPendingText = true;
        }
    }

    private void Update()
    {
        if (text == null) return;
        string toApply = null;

        lock (_lock)
        {
            if (_hasPendingText)
            {
                toApply = _pendingText;
                _hasPendingText = false;
            }
        }
        
        if(toApply != null)
        {
            text.text = toApply;
        }
    }

    private string BuildText(PoseLandmarkerResult result)
    {
        if (result.poseLandmarks == null || result.poseLandmarks.Count <= 0)
        {
            return "No pose";
        }

        var pose = result.poseLandmarks[0];
        var lms = pose.landmarks;

        sb.Clear();
        sb.AppendLine($"Landmarks Detected: {lms.Count}");

        double lArmAngle = getJointAngle(lms[jointMap["LArm"][0]], lms[jointMap["LArm"][1]], lms[jointMap["LArm"][2]]);
        double rArmAngle = getJointAngle(lms[jointMap["RArm"][0]], lms[jointMap["RArm"][1]], lms[jointMap["RArm"][2]]);
        double lLegAngle = getJointAngle(lms[jointMap["LLeg"][0]], lms[jointMap["LLeg"][1]], lms[jointMap["LLeg"][2]]);
        double rLegAngle = getJointAngle(lms[jointMap["RLeg"][0]], lms[jointMap["RLeg"][1]], lms[jointMap["RLeg"][2]]);

        sb.AppendLine($"LArm Angle: {lArmAngle}");
        sb.AppendLine($"RArm Angle: {rArmAngle}");
        sb.AppendLine($"LLeg Angle: {lLegAngle}");
        sb.AppendLine($"RLeg Angle: {rLegAngle}");

        var lWrist = lms[jointMap["LArm"][2]].y;
        var lShoulder = lms[jointMap["LArm"][0]].y;
        var lArmYDiff = Math.Abs(lWrist - lShoulder);

        var rWrist = lms[jointMap["RArm"][2]].y;
        var rShoulder = lms[jointMap["RArm"][0]].y; 
        var rArmYDiff = Math.Abs(rWrist - rShoulder);

        if (poseDectectionTPose(lArmAngle, rArmAngle, lArmYDiff, rArmYDiff))
        {
             sb.AppendLine("T Pose detected!");
        }

        return sb.ToString();
    }

    //getting angle at a joint
    //code from https://developers.google.com/ml-kit/vision/pose-detection/classifying-poses#java
    private double getJointAngle(NormalizedLandmark firstPoint, NormalizedLandmark midPoint, NormalizedLandmark lastPoint)
    {
        // computing the angle at midPoint formed by two segments
        double result = 
            (Math.Atan2(lastPoint.y - midPoint.y, lastPoint.x - midPoint.x) // last to mid vector
            - Math.Atan2(firstPoint.y - midPoint.y, firstPoint.x - midPoint.x)) // first to mid vector
            * (180.0 / Math.PI); //convert to degrees

        //angle should never be negative
        result = Math.Abs(result);

        if (result > 180)
        {
            //get acute representation
            result = (360.0 - result);
        }
        return result;
    }

    //right now this is just angles. probs need orientation too
    private bool poseDectectionTPose(double lArmAngle, double rArmAngle, float lArmY, float rArmY)
    {
        if (lArmY <= 0 + yThreshold && rArmY <= 0 + yThreshold)
        {
            if (lArmAngle >= 180.0 - angleThreshold && lArmAngle <= 180.0 + angleThreshold
            && rArmAngle >= 180.0 - angleThreshold && rArmAngle <= 180.0 + angleThreshold)
            {
                return true;
            }
            else return false;
        }
        else return false;
    }
}
