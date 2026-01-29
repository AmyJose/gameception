using Mediapipe.Tasks.Vision.PoseLandmarker;
using System.Text;
using TMPro;
using UnityEngine;

public class PoseLandmarkHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private int poseIndex = 0;
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
        if (result.poseLandmarks == null || result.poseLandmarks.Count <= poseIndex)
        {
            return "No pose";
        }

        var pose = result.poseLandmarks[poseIndex];
        var lms = pose.landmarks;

        sb.Clear();
        sb.AppendLine($"Landmarks Detected: {lms.Count}");

        foreach(var idx in landmarkIndices)
        {
            if (idx < 0 || idx >= lms.Count) continue;
            var lm = lms[idx];
            sb.AppendLine($"{idx:00}: x={lm.x:F3} y={lm.y:F3}");
        }

        return sb.ToString();
    }
}
