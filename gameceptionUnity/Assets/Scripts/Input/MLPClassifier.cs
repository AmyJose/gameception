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
        private readonly float[] _processed = new float[70]; // 66 + 4 angles

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

            if (_scalerMean.Length != 70 || _scalerStd.Length != 70)
            {
                Debug.LogError($"[PoseClassifier] Scaler data wrong size: mean={_scalerMean.Length}, std={_scalerStd.Length}. Expected 70.");
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
            using var inputTensor = new Tensor<float>(new TensorShape(1, 70), _processed);

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
            if (bestScore <= 0.98f)
                return new PoseClassification { pose = ElementPose.None, confidence = bestScore };

            // Map index to Enum directly (skipping label JSON)
            ElementPose predictedPose = bestIndex switch
            {
                0 => ElementPose.Ice,
                1 => ElementPose.Earth,
                2 => ElementPose.Fire,
                3 => ElementPose.Water,
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

            for (int i = 0; i < 66; i++) { _processed[i] /= torsoLength; }

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
}