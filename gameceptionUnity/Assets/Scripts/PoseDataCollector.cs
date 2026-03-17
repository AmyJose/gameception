using System.IO;
using System.Text;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using Mediapipe.Tasks.Vision.PoseLandmarker;

public class PoseDataCollector : MonoBehaviour
{
    [Header("UI Feedback (Optional)")]
    [SerializeField] private TMPro.TextMeshProUGUI statusText;

    private string _filePath;
    private StringBuilder _csvBuffer = new StringBuilder();

    private string _currentLabel = "idle";
    private bool _isRecording = false;
    private bool _sessionActive = false;

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Space to toggle the file session
        if (kb.spaceKey.wasPressedThisFrame)
        {
            if (!_sessionActive) StartNewSession();
            else SaveAndCloseSession();
        }

        // 1-4 for Pose Labels, 9 for Idle, 0 to Pause Recording
        if (kb.digit1Key.wasPressedThisFrame) SetRecordingState("earth");
        if (kb.digit2Key.wasPressedThisFrame) SetRecordingState("water");
        if (kb.digit3Key.wasPressedThisFrame) SetRecordingState("fire");
        if (kb.digit4Key.wasPressedThisFrame) SetRecordingState("ice");
        if (kb.digit9Key.wasPressedThisFrame) SetRecordingState("idle");
        if (kb.digit0Key.wasPressedThisFrame) SetRecordingState("paused");

        UpdateStatusUI();
    }

    private void StartNewSession()
    {
        string folder = "TestData";
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _filePath = Path.Combine(folder, $"posedata_{timestamp}.csv");

        _csvBuffer.Clear();
        // Write Header
        for (int i = 0; i < 33; i++) _csvBuffer.Append($"x{i},y{i},");
        _csvBuffer.AppendLine("label");

        File.WriteAllText(_filePath, _csvBuffer.ToString());
        _csvBuffer.Clear();

        _sessionActive = true;
        Debug.Log($"[Collector] Session Started: {_filePath}");
    }

    public void AddSample(PoseLandmarkerResult result)
    {
        // Logic match: Only write if active, recording, and not paused
        if (!_sessionActive || !_isRecording || _currentLabel == "paused") return;
        if (result.poseLandmarks == null || result.poseLandmarks.Count == 0) return;

        var landmarks = result.poseLandmarks[0].landmarks;
        if (landmarks.Count < 33) return;

        // Build the row
        for (int i = 0; i < 33; i++)
        {
            _csvBuffer.Append(landmarks[i].x.ToString("F6", CultureInfo.InvariantCulture)).Append(",");
            _csvBuffer.Append(landmarks[i].y.ToString("F6", CultureInfo.InvariantCulture)).Append(",");
        }
        _csvBuffer.AppendLine(_currentLabel);

        // Periodically flush to disk to keep memory low
        if (_csvBuffer.Length > 5000)
        {
            File.AppendAllText(_filePath, _csvBuffer.ToString());
            _csvBuffer.Clear();
        }
    }

    private void SetRecordingState(string label)
    {
        if (label == "paused")
        {
            _isRecording = false;
            Debug.Log("[Collector] Recording PAUSED");
        }
        else
        {
            _currentLabel = label;
            _isRecording = true;
            Debug.Log($"[Collector] Recording label: {label}");
        }
    }

    private void SaveAndCloseSession()
    {
        if (_sessionActive)
        {
            File.AppendAllText(_filePath, _csvBuffer.ToString());
            _csvBuffer.Clear();
            _sessionActive = false;
            _isRecording = false;
            Debug.Log("[Collector] Session Saved and Closed.");
        }
    }

    private void UpdateStatusUI()
    {
        if (statusText == null) return;
        if (!_sessionActive) statusText.text = "Press SPACE to start session";
        else if (!_isRecording) statusText.text = "PAUSED - Select Pose (1-4)";
        else statusText.text = $"RECORDING: {_currentLabel.ToUpper()}";
    }

    private void OnApplicationQuit() => SaveAndCloseSession();
}