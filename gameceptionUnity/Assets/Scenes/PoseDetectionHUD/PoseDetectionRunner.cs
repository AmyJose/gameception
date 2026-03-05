using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Mediapipe;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using Unity.InferenceEngine;
using Unity.Loading;
using UnityEngine;
using UnityEngine.InputSystem;
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
    public PoseLandmarkHUD HUD => _hud;

    // instance of the config class
    public readonly PoseDetectionConfig config = new PoseDetectionConfig();

    // Pose classifier (Sentis/InferenceEngine)
    [Header("Pose Classifier")]
    [SerializeField] private ModelAsset _classifierModelAsset;
    [SerializeField] private TextAsset _scalerJson;
    [SerializeField] private TextAsset _labelsJson;

    private PoseClassifier _poseClassifier;
    // private readonly float[] _landmarkBuffer = new float[66];
    private volatile string _lastPrediction = "";
    public string LastPrediction => _lastPrediction;

    public float[] LastClassScores => _poseClassifier?.LastScores;
    public string[] ClassLabels => _poseClassifier?.LabelNames;

    // Buffer for deferring classification to the main thread
    private readonly float[] _pendingLandmarks = new float[66];
    private volatile bool _hasLandmarksPending = false;

    // CSV data collection fields
    private StreamWriter _csvWriter;
    private volatile int _currentLabel = 0; // 0 = paused (don't write), 1-4 = pose labels, 9 = idle
    private readonly object _csvLock = new object();

    // Maps key number to label string. Empty string = not writing.
    private static readonly string[] LabelNames = new string[]
    {
        "",       // 0 — not writing
        "earth",  // 1
        "water",  // 2
        "fire",   // 3
        "air",    // 4
        "",       // 5 
        "",       // 6 
        "",       // 7 
        "",       // 8 
        "idle"    // 9
    };

    public override void Stop()
    {
        base.Stop();
        _textureFramePool?.Dispose();
        _textureFramePool = null;
        CloseCsv();
        _poseClassifier?.Dispose();
        _poseClassifier = null;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame)
        {
            _currentLabel = 0;
            Debug.Log("[CSV] Label set to 0 — PAUSED (not writing)");
        }
        else if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
        {
            _currentLabel = 1;
            Debug.Log("[CSV] Label set to 1 — earth");
        }
        else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
        {
            _currentLabel = 2;
            Debug.Log("[CSV] Label set to 2 — water");
        }
        else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
        {
            _currentLabel = 3;
            Debug.Log("[CSV] Label set to 3 — fire");
        }
        else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame)
        {
            _currentLabel = 4;
            Debug.Log("[CSV] Label set to 4 — air");
        }
        else if (kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame)
        {
            _currentLabel = 9;
            Debug.Log("[CSV] Label set to 9 — idle");
        }

        // Run classification on the main thread
        if (_hasLandmarksPending && _poseClassifier != null)
        {
            _hasLandmarksPending = false;
            _lastPrediction = _poseClassifier.Classify(_pendingLandmarks);
        }
    }

    protected override IEnumerator Run()
    {
        //wait on asset being prepared or copied into StreamingAssets if needed
        yield return AssetLoader.PrepareAssetAsync(config.ModelResourcePath);
        Debug.Log($"[PoseDetectionRunner] Expecting model at: {config.StreamingAssetsModelPath}");

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

        //TODO move this from the samples folder
        //e.g., do our own implementation of accessing the image source and waiting for a response
        var imageSource = ImageSourceProvider.ImageSource;

        if (imageSource is WebCamSource webCamSource)
        {
            var names = webCamSource.sourceCandidateNames;
            var usbIndex = Array.FindIndex(names, n => n== "USB Camera");
            if(usbIndex >= 0){
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

        // Initialise pose classifier
        if (_classifierModelAsset != null && _scalerJson != null && _labelsJson != null)
        {
            _poseClassifier = new PoseClassifier();
            _poseClassifier.Load(_classifierModelAsset, _scalerJson, _labelsJson);
        }
        else
        {
            Debug.LogWarning("[PoseDetectionRunner] Classifier assets not assigned — classification disabled.");
        }


        // Create a new CSV file for this session
        OpenCsv();
        _currentLabel = 0; // start paused
        Debug.Log("[CSV] Ready. Keys: 1=earth 2=water 3=fire 4=air 9=idle 0=pause");

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

    // CSV helpers

    private void OpenCsv()
    {
        // Write CSVs to a PoseData/ folder 
        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var folder = Path.Combine(projectRoot, "PoseData");
        Directory.CreateDirectory(folder);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var filePath = Path.Combine(folder, $"posedata_{timestamp}.csv");

        _csvWriter = new StreamWriter(filePath, append: false, Encoding.UTF8);

        // Header: x0,y0,x1,y1,...,x32,y32,label
        var header = new StringBuilder();
        for (int i = 0; i < 33; i++)
        {
            header.Append($"x{i},y{i}");
            header.Append(',');
        }
        header.Append("label");
        _csvWriter.WriteLine(header.ToString());
        _csvWriter.Flush();

        Debug.Log($"[CSV] File opened: {filePath}");
    }

    private void CloseCsv()
    {
        lock (_csvLock)
        {
            if (_csvWriter != null)
            {
                _csvWriter.Flush();
                _csvWriter.Close();
                _csvWriter.Dispose();
                _csvWriter = null;
                Debug.Log("[CSV] File closed.");
            }
        }
    }

    private void WriteLandmarksToCsv(PoseLandmarkerResult result)
    {
        int label = _currentLabel;

        // 0 is paused, don't write anything
        if (label == 0) return;

        // Validate label index
        if (label < 0 || label >= LabelNames.Length || string.IsNullOrEmpty(LabelNames[label])) return;

        // Need at least one detected pose
        if (result.poseLandmarks == null || result.poseLandmarks.Count == 0) return;

        var landmarks = result.poseLandmarks[0].landmarks;
        if (landmarks == null || landmarks.Count < 33) return;

        var row = new StringBuilder(512);
        for (int i = 0; i < 33; i++)
        {
            row.Append(landmarks[i].x.ToString("F6", CultureInfo.InvariantCulture));
            row.Append(',');
            row.Append(landmarks[i].y.ToString("F6", CultureInfo.InvariantCulture));
            row.Append(',');
        }
        row.Append(LabelNames[label]);

        lock (_csvLock)
        {
            if (_csvWriter != null)
            {
                _csvWriter.WriteLine(row.ToString());
                _csvWriter.Flush();
            }
        }
    }

    private void OnPoseLandmarkDetectionOutput(PoseLandmarkerResult result, Mediapipe.Image image, long timestamp)
    {
        _hud?.EnqueueResult(result);
        WriteLandmarksToCsv(result);

        // Stages landmarks for classification on the main thread (Classify should not be called from)
        if (_poseClassifier != null
            && result.poseLandmarks != null
            && result.poseLandmarks.Count > 0)
        {
            var landmarks = result.poseLandmarks[0].landmarks;
            if (landmarks != null && landmarks.Count >= 33)
            {
                // Extract x,y pairs into flat buffer
                for (int i = 0; i < 33; i++)
                {
                    _pendingLandmarks[i * 2]     = landmarks[i].x;
                    _pendingLandmarks[i * 2 + 1] = landmarks[i].y;
                }
                _hasLandmarksPending = true;

            }
        }



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
}
