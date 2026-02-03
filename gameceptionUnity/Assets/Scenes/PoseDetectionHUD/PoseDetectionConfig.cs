using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity;
using Unity.VisualScripting;

// This file does the configuration for the mediapipe model used to detection pose landmarks


public enum ModelType : int
    {
        BlazePoseLite = 0,
        BlazePoseFull = 1,
        BlazePoseHeavy = 2,
    }

public class PoseDetectionConfig
{
    // delegation
    public Mediapipe.Tasks.Core.BaseOptions.Delegate Delegate{get; set;} =
    #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
      Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU;
    #else
        Mediapipe.Tasks.Core.BaseOptions.Delegate.GPU;
    #endif

    public ImageReadMode ImageReadMode {get;set;} = ImageReadMode.CPUAsync;
    public ModelType Model{get;set;} = ModelType.BlazePoseLite;
    public Mediapipe.Tasks.Vision.Core.RunningMode RunningMode{get; set;} = Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM;

    public int NumPoses{get;set;} = 1;
    public float MinPoseDetectionConfidence{get;set;} = 0.5f;
    public float MinPosePresenceConfidence{get;set;} = 0.5f;
    public float MinTrackingConfidence{get;set;} = 0.5f;
    public bool OutputSegmentationMasks{get;set;} = false;
    
    public string ModelPath
    {
        get
        {
            switch(Model){
                case ModelType.BlazePoseLite: return "pose_landmarker_lite.bytes";
                case ModelType.BlazePoseFull: return "pose_landmarker_full.bytes";
                case ModelType.BlazePoseHeavy: return "pose_landmarker_heavy.bytes";
                default : return null;
            }
        }
    }

    // configuring the task
    // use the varibles assigned aboce to define the options we will be using
    public PoseLandmarkerOptions GetPoseLandmarkerOptions(PoseLandmarkerOptions.ResultCallback resultCallback = null)
    {
        return new PoseLandmarkerOptions(
            new Mediapipe.Tasks.Core.BaseOptions(Delegate, modelAssetPath: ModelPath),
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
