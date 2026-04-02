using System;
using UnityEngine;
using Unity.InferenceEngine;

public class PoseClassifier : IDisposable
{
    private Worker _worker;
    private float[] _scalerMean;
    private float[] _scalerStd;
    private string[] _labelNames;
    public string[] LabelNames => _labelNames;
    private float[] _lastScores = new float[0];
    public float[] LastScores => _lastScores;

    // Pre-allocated buffer for preprocessed data
    private readonly float[] _processed = new float[8];

    public void Load(ModelAsset modelAsset, TextAsset scalerJson, TextAsset labelsJson)
    {
        // load model into a Worker
        var model = ModelLoader.Load(modelAsset);
        _worker = new Worker(model, BackendType.CPU);
        Debug.Log("[PoseClassifier] Model loaded.");

        // Parse scaler JSON: {"mean": [66 floats], "std": [66 floats]}
        var scaler = JsonUtility.FromJson<ScalerData>(scalerJson.text);
        _scalerMean = scaler.mean;
        _scalerStd = scaler.std;

        if (_scalerMean.Length != 8 || _scalerStd.Length != 8)
        {
            Debug.LogError($"[PoseClassifier] Scaler data wrong size: mean={_scalerMean.Length}, std={_scalerStd.Length}. Expected 8.");
        }

        // Parse labels JSON
        _labelNames = ParseLabelJson(labelsJson.text);

        Debug.Log($"[PoseClassifier] Loaded. Labels: {string.Join(", ", _labelNames)}");
    }

    // Classify a pose from 8 preprocessed floats.
    // Returns the predicted label string (e.g. "earth").
    public string Classify(float[] input8)
    {
        if (_worker == null)
        {
            Debug.LogError("[PoseClassifier] Not loaded.");
            return "unknown";
        }

        // Preprocess: must match Python pipeline exactly
        Preprocess(input8);

        // Creates input tensor: shape (1, 8)
        using var inputTensor = new Tensor<float>(new TensorShape(1, 8), _processed);

        // Runs the model
        _worker.Schedule(inputTensor);

        // Read output: shape (1, 4) — one score per class
        var outputTensor = _worker.PeekOutput() as Tensor<float>;
        using var cpuTensor = outputTensor.ReadbackAndClone() as Tensor<float>;

        int numClasses = cpuTensor.shape[1];

        // Store raw logits for display
        if (_lastScores.Length != numClasses)
            _lastScores = new float[numClasses];

        // Reads from cpuTensor (guaranteed CPU data)
        for (int i = 0; i < numClasses; i++)
        {
            _lastScores[i] = cpuTensor[0, i];
        }

        // Argmax: finds which class has the highest score
        int bestIndex = 0;
        float bestScore = cpuTensor[0, 0];
        for (int i = 0; i < numClasses; i++)
        {
            float val = cpuTensor[0, i];
            if (val > bestScore)
            {
                bestScore = val;
                bestIndex = i;
            }
        }

        // Confidence threshold - reject low-confidence predictions
        if (bestScore <= 0.99f)
            return "none";


        if (bestIndex >= 0 && bestIndex < _labelNames.Length)
            return _labelNames[bestIndex];

        return "unknown";

    }

    private void Preprocess(float[] raw)
    {
        // --- Torso length (same as Python) ---
        Vector2 shoulderMid = (GetRawPos(raw, 11) + GetRawPos(raw, 12)) / 2f;
        Vector2 hipMid = (GetRawPos(raw, 23) + GetRawPos(raw, 24)) / 2f;
        float torsoLen = Vector2.Distance(shoulderMid, hipMid);
        torsoLen = Mathf.Max(torsoLen, 1e-6f);

        // --- Group A: Angles ---
        _processed[0] = GetAngle(GetRawPos(raw, 11), GetRawPos(raw, 13), GetRawPos(raw, 15)); // L Elbow
        _processed[1] = GetAngle(GetRawPos(raw, 12), GetRawPos(raw, 14), GetRawPos(raw, 16)); // R Elbow
        _processed[2] = GetAngle(GetRawPos(raw, 23), GetRawPos(raw, 11), GetRawPos(raw, 13)); // L Shoulder
        _processed[3] = GetAngle(GetRawPos(raw, 24), GetRawPos(raw, 12), GetRawPos(raw, 14)); // R Shoulder

        // --- Group B: Distances ---
        _processed[4] = (raw[15 * 2 + 1] - raw[11 * 2 + 1]) / torsoLen; // L Wrist Y
        _processed[5] = (raw[16 * 2 + 1] - raw[12 * 2 + 1]) / torsoLen; // R Wrist Y
        _processed[6] = (raw[15 * 2] - raw[11 * 2]) / torsoLen;         // L Wrist X
        _processed[7] = (raw[16 * 2] - raw[12 * 2]) / torsoLen;         // R Wrist X

        // --- StandardScaler ---
        for (int i = 0; i < 8; i++)
        {
            _processed[i] = (_processed[i] - _scalerMean[i]) / _scalerStd[i];
        }
    }

    private Vector2 GetRawPos(float[] raw, int index)
    {
        return new Vector2(raw[index * 2], raw[index * 2 + 1]);
    }

    /*private void Preprocess(float[] raw)
    {
        // 1. Centre on hip midpoint (23 & 24)
        float hipCenterX = (raw[23 * 2] + raw[24 * 2]) / 2f;
        float hipCenterY = (raw[23 * 2 + 1] + raw[24 * 2 + 1]) / 2f;

        for (int i = 0; i < 33; i++)
        {
            _processed[i * 2] = raw[i * 2] - hipCenterX;
            _processed[i * 2 + 1] = raw[i * 2 + 1] - hipCenterY;
        }

        // 2. Normalise by torso length
        float shoulderCenterX = (_processed[11 * 2] + _processed[12 * 2]) / 2f;
        float shoulderCenterY = (_processed[11 * 2 + 1] + _processed[12 * 2 + 1]) / 2f;
        float torsoLength = Mathf.Sqrt(shoulderCenterX * shoulderCenterX + shoulderCenterY * shoulderCenterY);
        torsoLength = Mathf.Max(torsoLength, 1e-6f);

        for (int i = 0; i < 70; i++) { _processed[i] /= torsoLength; }

        // 3. Feature Engineering: Angles (Indices 66-69)
        // Left Elbow: Shoulder(11), Elbow(13), Wrist(15)
        _processed[66] = GetAngle(GetPos(11), GetPos(13), GetPos(15));
        // Right Elbow: Shoulder(12), Elbow(14), Wrist(16)
        _processed[67] = GetAngle(GetPos(12), GetPos(14), GetPos(16));
        // Left Shoulder: Hip(23), Shoulder(11), Elbow(13)
        _processed[68] = GetAngle(GetPos(23), GetPos(11), GetPos(13));
        // Right Shoulder: Hip(24), Shoulder(12), Elbow(14)
        _processed[69] = GetAngle(GetPos(24), GetPos(12), GetPos(14));


        // 5. StandardScaler (Now 70 features)
        for (int i = 0; i < 70; i++)
        {
            _processed[i] = (_processed[i] - _scalerMean[i]) / _scalerStd[i];
        }
    }*/

    private static string[] ParseLabelJson(string json)
    {
        json = json.Trim().TrimStart('{').TrimEnd('}');
        var entries = json.Split(',');
        var labels = new string[entries.Length];

        foreach (var entry in entries)
        {
            var parts = entry.Split(':');
            var key = parts[0].Trim().Trim('"');
            var val = parts[1].Trim().Trim('"');
            int index = int.Parse(key);
            if (index < labels.Length)
                labels[index] = val;
        }

        return labels;
    }

    public void Dispose()
    {
        _worker?.Dispose();
        _worker = null;
    }

    // class with a public fields for JsonUtility to deserialize
    [Serializable]
    private class ScalerData
    {
        public float[] mean;
        public float[] std;
    }

    private Vector2 GetPos(int index)
    {
        return new Vector2(_processed[index * 2], _processed[index * 2 + 1]);
    }

    private float GetAngle(Vector2 a, Vector2 b, Vector2 c)
    {
        float ang = Mathf.Atan2(c.y - b.y, c.x - b.x) - Mathf.Atan2(a.y - b.y, a.x - b.x);
        ang = Mathf.Abs(ang * Mathf.Rad2Deg);
        if (ang > 180f) ang = 360f - ang;
        return ang;
    }

}
