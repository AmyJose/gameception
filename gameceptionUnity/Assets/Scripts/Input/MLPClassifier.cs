using System;
using UnityEngine;
using Unity.InferenceEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;

namespace InputLayer
{
    public class MLPPoseClassifier : MonoBehaviour, IDisposable, IPoseClassifier
    {
        [Header("Model & Preprocessing Data")]
        [SerializeField] private ModelAsset modelAsset;
        [SerializeField] private TextAsset scalerJson;

        private Worker _worker;
        private float[] _scalerMean;
        private float[] _scalerStd;
        private string[] _labelNames;
        public string[] LabelNames => _labelNames;
        //private float[] _lastScores = new float[0];
        //public float[] LastScores => _lastScores;

        private readonly float[] _rawBuffer = new float[66]; // 33 landmarks * 2 (x,y)
        private readonly float[] _processed = new float[8]; // updated 8 features 

        private void Start()
        {
            if (modelAsset != null && scalerJson != null)
            {
                Load(modelAsset, scalerJson);
            }
            else
            {
                Debug.LogWarning("[PoseClassifier] Missing model or scaler assets in inspector.");
            }
        }

        public void Load(ModelAsset modelAsset, TextAsset scalerJson)
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
            //_labelNames = ParseLabelJson(labelsJson.text);
        }

        // Classify a pose from 70 raw landmark floats (x0,y0,x1,y1,...,x34,y34).
        // Returns the predicted label string (e.g. "earth").
        public PoseClassification Classify(PoseLandmarkerResult result)
        {
            if (_worker == null || result.poseLandmarks == null || result.poseLandmarks.Count == 0)
            {
                //Debug.LogError("[PoseClassifier] Not loaded.");
                return new PoseClassification { pose = ElementPose.None, confidence = 0f };
            }

            var landmarks = result.poseLandmarks[0].landmarks;
            for (int i = 0; i < 33; i++)
            {
                _rawBuffer[i * 2] = landmarks[i].x;
                _rawBuffer[i * 2 + 1] = landmarks[i].y;
            }

            // Preprocess: must match Python pipeline exactly
            Preprocess(_rawBuffer);

            // Creates input tensor: shape (1, 70)
            using var inputTensor = new Tensor<float>(new TensorShape(1, 8), _processed);

            // Runs the model
            _worker.Schedule(inputTensor);

            // Read output: shape (1, 4) — one score per class
            using var outputTensor = _worker.PeekOutput() as Tensor<float>;
            using var cpuTensor = outputTensor.ReadbackAndClone() as Tensor<float>;

            int numClasses = cpuTensor.shape[1];

            // Argmax: finds which class has the highest score
            int bestIndex = 0;
            float bestScore = cpuTensor[0, 0];
            for (int i = 1; i < numClasses; i++)
            {
                if (cpuTensor[0, i] > bestScore)
                {
                    bestScore = cpuTensor[0, i];
                    bestIndex = i;
                }
            }

            // 5. Confidence Threshold & Result Construction
            if (bestScore <= 0.90f)
                return new PoseClassification { pose = ElementPose.None, confidence = bestScore };

            // Map index to Enum directly (skipping label JSON)
            ElementPose predictedPose = bestIndex switch
            {
                0 => ElementPose.Ice,
                1 => ElementPose.Earth,
                2 => ElementPose.Fire,
                3 => ElementPose.None,
                4 => ElementPose.Water,
                _ => ElementPose.None
            };

            return new PoseClassification
            {
                pose = predictedPose,
                confidence = bestScore
            };

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

        private void OnDestroy() => Dispose();

        // class with a public fields for JsonUtility to deserialize
        [Serializable]
        private class ScalerData
        {
            public float[] mean;
            public float[] std;
        }


        private float GetAngle(Vector2 a, Vector2 b, Vector2 c)
        {
            float ang = Mathf.Atan2(c.y - b.y, c.x - b.x) - Mathf.Atan2(a.y - b.y, a.x - b.x);
            ang = Mathf.Abs(ang * Mathf.Rad2Deg);
            if (ang > 180f) ang = 360f - ang;
            return ang;
        }
    }
}