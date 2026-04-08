using InputLayer;
using Mediapipe;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Loading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Color = UnityEngine.Color;

// NOTE: this script currently holds a lot of switch statements depending on the type of imagesource and processing selected
// possibly can strip this back for performance if needed

public class PoseDetectionRunner : VisionTaskApiRunner<PoseLandmarker>
{
    [SerializeField] private PoseLandmarkerResultAnnotationController _poseLandmarkerResultAnnotationController;
    [SerializeField] private PoseDataCollector dataCollector;
    private Mediapipe.Unity.Experimental.TextureFramePool _textureFramePool;

    // instance of the config class
    public readonly PoseDetectionConfig config = new PoseDetectionConfig();

    [SerializeField] private PoseState poseState;
    [SerializeField] private MonoBehaviour poseClassifierComponent; // assign HeuristicPoseClassifier
    private IPoseClassifier poseClassifier;

    [Header("Tutorial Mode")]
    [SerializeField] private bool tutorialStartHidden = false;
    [SerializeField] private GameObject cameraFeedVisualRoot;
    [SerializeField] private GameObject annotationVisualRoot;

    [Header("Thread Handoff")]
    private PoseLandmarkerResult _latestResult;
    private long _latestTimestamp;
    private bool _isNewResultAvailable = false;
    private readonly object _resultLock = new object();


    public override void Stop()
    {
        base.Stop();
        _textureFramePool?.Dispose();
        _textureFramePool = null;
    }
    protected override IEnumerator Start()
    {
        if (tutorialStartHidden)
        {
            SetVisualsVisible(false);
        }

        yield return base.Start();

        if (tutorialStartHidden)
        {
            PauseDetection();
        }
    }

    private void Update()
    {
        // 1. Check for a new result from the background thread
        PoseLandmarkerResult resultToProcess = default;
        long timestampToProcess = 0;
        bool shouldProcess = false;

        lock (_resultLock)
        {
            if (_isNewResultAvailable)
            {
                resultToProcess = _latestResult;
                timestampToProcess = _latestTimestamp;
                _isNewResultAvailable = false;
                shouldProcess = true;
            }
        }
        if (shouldProcess && poseClassifier != null && poseState != null)
        {
            var classification = poseClassifier.Classify(resultToProcess);
            poseState.SetPose(classification.pose, classification.confidence, timestampToProcess);


            UpdateSkeletonColor(classification.pose);

        }
    }

    private void UpdateSkeletonColor(ElementPose pose)
    {
        Color color = pose switch
        {
            ElementPose.Earth => Color.green,
            ElementPose.Fire => Color.red,
            ElementPose.Ice => Color.cyan,
            ElementPose.Water => Color.blue,
            _ => Color.yellow
        };

        if (_poseLandmarkerResultAnnotationController == null)
        {
            Debug.LogError("[SkeletonColor] _poseLandmarkerResultAnnotationController is NULL");
            return;
        }

        _poseLandmarkerResultAnnotationController.SetSkeletonColor(color);
    }

    protected override IEnumerator Run()
    {
        //wait on asset being prepared or copied into StreamingAssets if needed
        yield return AssetLoader.PrepareAssetAsync(config.ModelResourcePath);
        Debug.Log($"[PoseDetectionRunner] Expecting model at: {config.StreamingAssetsModelPath}");

        //cache interface
        poseClassifier = poseClassifierComponent as IPoseClassifier;
        if (poseClassifier == null)
        {
            Debug.LogError("[PoseDetectionRunner] poseClassifierComponent does not implement IPoseClassifier");
        }

        //load the options from the config
        var options = config.GetPoseLandmarkerOptions(
            config.RunningMode == Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnPoseLandmarkDetectionOutput : null);

        //only pass GPU resources if actually using GPU delegate
        var gpuResources = (config.Delegate == Mediapipe.Tasks.Core.BaseOptions.Delegate.GPU)
            ? GpuManager.GpuResources
            : null;

        //new task api instance
        Debug.Log("[PoseDetectionRunner] Options created, creating PoseLandmarker...");
        taskApi = PoseLandmarker.CreateFromOptions(options, gpuResources);

        var imageSource = ImageSourceProvider.ImageSource;

        if (imageSource is WebCamSource webCamSource)
        {
            var names = webCamSource.sourceCandidateNames;
            var usbIndex = Array.FindIndex(names, n => n == "USB Camera");
            if (usbIndex >= 0)
            {
                webCamSource.SelectSource(usbIndex);
            }
        }

        yield return imageSource.Play();
        if (!imageSource.isPrepared)
        {
            Mediapipe.Logger.LogError(TAG, "Failed to start ImageSource, exiting...");
            yield break;
        }

        _textureFramePool = new Mediapipe.Unity.Experimental.TextureFramePool(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32, 10);

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

            if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
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
                    if (taskApi.TryDetect(image, imageProcessingOptions, ref result))
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
                    if (taskApi.TryDetectForVideo(image, GetCurrentTimestampMillisec(), imageProcessingOptions, ref result))
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
        // 1) classify pose (thread-safe: pure math)
        lock (_resultLock)
        {
            _latestResult = result;
            _latestTimestamp = timestamp;
            _isNewResultAvailable = true;
        }

        // Added Null Check to prevent background thread crashing
        if (dataCollector != null)
        {
            dataCollector.AddSample(result);
        }

        // 2) UI / annotation
        _poseLandmarkerResultAnnotationController.DrawLater(result);
        DisposeAllMasks(result);
    }

    private void DisposeAllMasks(PoseLandmarkerResult result)
    {
        if (result.segmentationMasks != null)
        {
            foreach (var mask in result.segmentationMasks)
            {
                mask.Dispose();
            }
        }
    }
    public void PauseDetection()
    {
        isPaused = true;
    }
    public void ResumeDetection()
    {
        isPaused = false;
    }
    public void SetVisualsVisible(bool visible)
    {
        if (cameraFeedVisualRoot != null) cameraFeedVisualRoot.SetActive(visible);

        if (annotationVisualRoot != null) annotationVisualRoot.SetActive(visible);
    }
}