using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Mediapipe;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using Unity.Loading;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// NOTE: this script currently holds a lot of switch statements depending on the type of imagesource and processing selected
// possibly can strip this back for performance if needed

public class PoseDetectionRunner : VisionTaskApiRunner<PoseLandmarker>
{
    [SerializeField] private PoseLandmarkerResultAnnotationController _poseLandmarkerResultAnnotationController;
    private Mediapipe.Unity.Experimental.TextureFramePool _textureFramePool;

    // heads up display
    [SerializeField] private PoseLandmarkHUD _hud;

    // instance of the config class
    public readonly PoseDetectionConfig config = new PoseDetectionConfig();

    public override void Stop()
    {
        base.Stop();
        _textureFramePool?.Dispose();
        _textureFramePool = null;
    }

    protected override IEnumerator Run()
    {
        //wait on loading the model
        yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

        //load the options from the config and create a new task API instance
        var options = config.GetPoseLandmarkerOptions(config.RunningMode == Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnPoseLandmarkDetectionOutput : null);
        taskApi = PoseLandmarker.CreateFromOptions(options, GpuManager.GpuResources);

        //TODO move this from the samples folder
        //e.g., do our own implementation of accessing the image source and waiting for a response
        var imageSource = ImageSourceProvider.ImageSource;
        yield return imageSource.Play();
        if (!imageSource.isPrepared)
        {
            Mediapipe.Logger.LogError(TAG, "Failed to start ImageSource, exiting...");
            yield break;
        }

        _textureFramePool = new Mediapipe.Unity.Experimental.TextureFramePool(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32, 10);

        //this is held in the visiontaskapirunner
        screen.Initialize(imageSource);

        SetupAnnotationController(_poseLandmarkerResultAnnotationController, imageSource);
        
        _poseLandmarkerResultAnnotationController.InitScreen(imageSource.textureWidth, imageSource.textureHeight);

        var transformationOptions = imageSource.GetTransformationOptions();
        var flipHorizontally = transformationOptions.flipHorizontally;
        var flipVertically = transformationOptions.flipVertically;
        var imageProcessingOptions = new Mediapipe.Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: 0);

        AsyncGPUReadbackRequest req = default;
        var waitUntilReqDone = new WaitUntil(() => req.done);
        var waitForEndOfFrame = new WaitForEndOfFrame();
        var result = PoseLandmarkerResult.Alloc(options.numPoses, options.outputSegmentationMasks);

        // checking if we can use the GPU
        var canUseGpuImage = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && GpuManager.GpuResources != null;
        using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;

        while (true)
        {
            if (isPaused)
            {
                yield return new WaitWhile(() => isPaused);
            }

            if(!_textureFramePool.TryGetTextureFrame(out var textureFrame))
            {
                yield return new WaitForEndOfFrame();
                continue;
            }

            //Building the output image
            Mediapipe.Image image;
            switch (config.ImageReadMode)
            {
                case ImageReadMode.GPU:
                    if (!canUseGpuImage)
                    {
                        throw new System.Exception("ImageReadMode.GPU is not supported");
                    }
                    textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                    image = textureFrame.BuildGPUImage(glContext);
                    yield return waitForEndOfFrame;
                    break;
                case ImageReadMode.CPU:
                    yield return waitForEndOfFrame;
                    textureFrame.ReadTextureOnCPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                    image = textureFrame.BuildCPUImage();
                    textureFrame.Release();
                    break;
                case ImageReadMode.CPUAsync:
                default:
                    req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                    yield return waitUntilReqDone;

                    if (req.hasError)
                    {
                        Debug.LogWarning($"Failed to read texture from the image source");
                        continue;
                    }
                    image = textureFrame.BuildCPUImage();
                    textureFrame.Release();
                    break;
            }

            switch (taskApi.runningMode)
            {
                case Mediapipe.Tasks.Vision.Core.RunningMode.IMAGE:
                    if(taskApi.TryDetect(image, imageProcessingOptions, ref result))
                    {
                        _poseLandmarkerResultAnnotationController.DrawNow(result);
                    }
                    else
                    {
                        _poseLandmarkerResultAnnotationController.DrawNow(default);
                    }
                    DisposeAllMasks(result);
                    break;
                case Mediapipe.Tasks.Vision.Core.RunningMode.VIDEO:
                    if(taskApi.TryDetectForVideo(image, GetCurrentTimestampMillisec(), imageProcessingOptions, ref result))
                    {
                        _poseLandmarkerResultAnnotationController.DrawNow(result);
                    }
                    else
                    {
                        _poseLandmarkerResultAnnotationController.DrawNow(result);
                    }
                    DisposeAllMasks(result);
                    break;
                case Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM:
                    taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
                    break;
            }
        }
    }

    private void OnPoseLandmarkDetectionOutput(PoseLandmarkerResult result, Mediapipe.Image image, long timestamp)
    {
        _hud?.EnqueueResult(result);
        _poseLandmarkerResultAnnotationController.DrawLater(result);
        DisposeAllMasks(result);
    }
    private void DisposeAllMasks(PoseLandmarkerResult result)
    {
        if(result.segmentationMasks != null)
        {
            foreach (var mask in result.segmentationMasks)
            {
                mask.Dispose();
            }
        }
    }

    // logger function for accessing underlying data structure
    private void LogPoseLandmarks(PoseLandmarkerResult result)
    {
        if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
        {
            Debug.Log("No pose landmarks detected");
            return;
        }

        for(int poseIndex = 0; poseIndex < result.poseLandmarks.Count; poseIndex++)
        {
            var pose = result.poseLandmarks[poseIndex];

            var landmarks = pose.landmarks;

            Debug.Log($"Pose {poseIndex} - Landmark count: {landmarks.Count}");

            for (int i=0; i<landmarks.Count; i++)
            {
                var lm = landmarks[i];

                Debug.Log(
                    $"Pose {poseIndex}, Landmark {i}: " +
                    $"x={lm.x:F4}, y={lm.y:F4}, z={lm.z:F4}, " +
                    $"visibility={lm.visibility:F4}, presence={lm.presence:F4}"
                );
            }
        }
    }
}
