using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using System.Diagnostics;
using Mediapipe.Unity.CoordinateSystem;
using Stopwatch  = System.Diagnostics.Stopwatch;
using NUnit.Framework.Interfaces;
using System.Threading.Tasks;

public class PoseDetectionRunner : MonoBehaviour
{
    [SerializeField] private RawImage screen;
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private int fps;
    [SerializeField] private TextAsset modelAsset;

    private WebCamTexture webCamTexture;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private IEnumerator Start()
    {
        if(WebCamTexture.devices.Length == 0)
        {
            throw new System.Exception("Web camera devices are not found");
        }
        var webCamDevice = WebCamTexture.devices[0];
        webCamTexture = new WebCamTexture(webCamDevice.name, width, height, fps);
        webCamTexture.Play();
        // NOTE: On macOS, the contents of webCamTexture may not be readable immediately, so wait until it is readable
        yield return new WaitUntil(() => webCamTexture.width >16);

        screen.rectTransform.sizeDelta = new Vector2(width, height);
        screen.texture = webCamTexture;

        //create the task
        var options = new PoseLandmarkerOptions(
            baseOptions: new Mediapipe.Tasks.Core.BaseOptions(
                Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
                modelAssetBuffer: modelAsset.bytes
            ),
            runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.VIDEO
        );

        using var poseLandmarker = PoseLandmarker.CreateFromOptions(options);

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        var textureFrame = new Mediapipe.Unity.Experimental.TextureFrame(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32);
        var waitForEndOfFrame = new WaitForEndOfFrame();

        var screenRect = screen.rectTransform.rect;

        // try to get sphere on nose
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(screen.transform);
        sphere.transform.localPosition = new Vector3(0,0,0);
        sphere.transform.localScale = new Vector3(10f, 10f, 10f);
        sphere.SetActive(false);

        while (true)
        {
            textureFrame.ReadTextureOnCPU(webCamTexture, flipHorizontally: false, flipVertically: true);
            using var image = textureFrame.BuildCPUImage();

            var result = poseLandmarker.DetectForVideo(image, stopwatch.ElapsedMilliseconds);
            if(result.poseLandmarks?.Count > 0)
            {
                var landmarks = result.poseLandmarks[0].landmarks;
                var nose = landmarks[0];
                var position = screenRect.GetPoint(in nose);
                position.z = 0;
                sphere.transform.localPosition = position;
                sphere.SetActive(true);
            }
            else
            {
                sphere.SetActive(false);
            }

            yield return waitForEndOfFrame;
        }
    }

    private void OnDestroy()
    {
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
        }
    }
}
