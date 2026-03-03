using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity;
using Unity.VisualScripting;
using System.IO;
using UnityEngine;

// This file does the configuration for the mediapipe model used to detection pose landmarks

public class PoseDetectionConfig
{
    private byte[] _cachedModelBytes;
    // delegation
    public Mediapipe.Tasks.Core.BaseOptions.Delegate Delegate{get; set;} =
    #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
      Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU;
    #else
        Mediapipe.Tasks.Core.BaseOptions.Delegate.GPU;
    #endif

    public ImageReadMode ImageReadMode {get;set;} = ImageReadMode.CPUAsync;
    public Mediapipe.Tasks.Vision.Core.RunningMode RunningMode{get; set;} = Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM;

    public int NumPoses{get;set;} = 1;
    public float MinPoseDetectionConfidence{get;set;} = 0.5f;
    public float MinPosePresenceConfidence{get;set;} = 0.5f;
    public float MinTrackingConfidence{get;set;} = 0.5f;
    public bool OutputSegmentationMasks{get;set;} = false;

    public string ModelResourcePath => "MediaPipe/pose_landmarker_lite.bytes";
    public string StreamingAssetsModelPath =>
        Path.Combine(Application.streamingAssetsPath, "MediaPipe", "pose_landmarker_lite.bytes")
            .Replace("\\", "/");

    //loading the model bytes directly with some messing around for weiiiirdddd paths
    public byte[] LoadModelBytes()
    {
        if(_cachedModelBytes != null)
        {
            return _cachedModelBytes;
        }

        var fullPath = Path.Combine(Application.streamingAssetsPath, "MediaPipe", "pose_landmarker_lite.bytes");
        fullPath = fullPath.Replace("\\", "/");
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[PoseDetectionConfig] Model file not found at: {fullPath}");
            return null;
        }
        var bytes = File.ReadAllBytes(fullPath);
        Debug.Log($"[PoseDetectionConfig] Loaded model bytes: {bytes?.Length ?? 0} from {fullPath}");
        return bytes;
    }

    // configuring the task
    // use the varibles assigned aboce to define the options we will be using
    public PoseLandmarkerOptions GetPoseLandmarkerOptions(PoseLandmarkerOptions.ResultCallback resultCallback = null)
    {
        var modelBytes = LoadModelBytes();
        if (modelBytes == null || modelBytes.Length == 0)
        {
            Debug.LogError("[PoseDetectionConfig] Model bytes were null/empty.");
        }
        return new PoseLandmarkerOptions(
            new Mediapipe.Tasks.Core.BaseOptions(Delegate, modelAssetBuffer: modelBytes),
            runningMode: RunningMode,
            numPoses: NumPoses,
            minPoseDetectionConfidence : MinPoseDetectionConfidence,
            minPosePresenceConfidence : MinPosePresenceConfidence,
            minTrackingConfidence : MinTrackingConfidence,
            outputSegmentationMasks : OutputSegmentationMasks,
            resultCallback : resultCallback
        );
    }
}
