// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using UnityEngine;
using UnityEngine.UI;

using mptcc = Mediapipe.Tasks.Components.Containers;

namespace Mediapipe.Unity
{
#pragma warning disable IDE0065
  using Color = UnityEngine.Color;
#pragma warning restore IDE0065

  public sealed class PoseLandmarkListWithMaskAnnotation : HierarchicalAnnotation
  {
    [SerializeField] private PoseLandmarkListAnnotation _poseLandmarkListAnnotation;
    [SerializeField] private MaskOverlayAnnotation _maskOverlayAnnotation;

        [Header("Torso Fill")]
        private Mesh _torsoMesh;
        private MeshFilter _torsoMeshFilter;
        private MeshRenderer _torsoMeshRenderer;

        [SerializeField] private Color _torsoColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private bool _fillTorso = true;

        public override bool isMirrored
    {
      set
      {
        _poseLandmarkListAnnotation.isMirrored = value;
        _maskOverlayAnnotation.isMirrored = value;
        base.isMirrored = value;
      }
    }

    public override RotationAngle rotationAngle
    {
      set
      {
        _poseLandmarkListAnnotation.rotationAngle = value;
        _maskOverlayAnnotation.rotationAngle = value;
        base.rotationAngle = value;
      }
    }

    public void InitMask(RawImage screen, int width, int height) => _maskOverlayAnnotation.Init(screen, width, height);

    public void SetLeftLandmarkColor(Color leftLandmarkColor) => _poseLandmarkListAnnotation.SetLeftLandmarkColor(leftLandmarkColor);

    public void SetRightLandmarkColor(Color rightLandmarkColor) => _poseLandmarkListAnnotation.SetRightLandmarkColor(rightLandmarkColor);

    public void SetLandmarkRadius(float landmarkRadius) => _poseLandmarkListAnnotation.SetLandmarkRadius(landmarkRadius);

    public void SetConnectionColor(Color connectionColor) => _poseLandmarkListAnnotation.SetConnectionColor(connectionColor);

    public void SetConnectionWidth(float connectionWidth) => _poseLandmarkListAnnotation.SetConnectionWidth(connectionWidth);

    public void SetMaskTexture(Texture2D maskTexture, Color color) => _maskOverlayAnnotation.SetMaskTexture(maskTexture, color);

    public void SetMaskThreshold(float threshold) => _maskOverlayAnnotation.SetThreshold(threshold);

    public void ReadMask(Image segmentationMask, bool isMirrored = false) => _maskOverlayAnnotation.Read(segmentationMask, isMirrored);

        public void Draw(mptcc.NormalizedLandmarks poseLandmarks, bool visualizeZ = false)
        {
            if (ActivateFor(poseLandmarks.landmarks))
            {
                _poseLandmarkListAnnotation.Draw(poseLandmarks, visualizeZ);

                if (_fillTorso)
                {
                    DrawTorsoFill();
                }

                _maskOverlayAnnotation.Draw();
            }
            else
            {
                if (_torsoMeshRenderer != null)
                {
                    _torsoMeshRenderer.enabled = false;
                }
            }
        }
        private void DrawTorsoFill()
        {
            var points = _poseLandmarkListAnnotation.GetComponentsInChildren<PointAnnotation>(true);

            if (points == null || points.Length < 33)
                return;

            EnsureTorsoMesh();
            _torsoMeshRenderer.enabled = true;

            Vector3 ls = points[11].transform.position;
            Vector3 rs = points[12].transform.position;
            Vector3 rh = points[24].transform.position;
            Vector3 lh = points[23].transform.position;

            _torsoMesh.Clear();

            _torsoMesh.vertices = new Vector3[]
            {
        ls, // 0
        rs, // 1
        rh, // 2
        lh  // 3
            };

            _torsoMesh.triangles = new int[]
            {
        0, 1, 2,
        0, 2, 3
            };

            _torsoMesh.RecalculateBounds();
        }
        private void EnsureTorsoMesh()
        {
            if (_torsoMeshFilter != null)
                return;

            var go = new GameObject("TorsoMesh");
            var pointRoot = _poseLandmarkListAnnotation.transform.Find("PointList Annotation");
            go.transform.SetParent(pointRoot, false);

            _torsoMeshFilter = go.AddComponent<MeshFilter>();
            _torsoMeshRenderer = go.AddComponent<MeshRenderer>();

            _torsoMesh = new Mesh();
            _torsoMesh.name = "TorsoMesh";

            _torsoMeshFilter.mesh = _torsoMesh;

            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = _torsoColor;

            _torsoMeshRenderer.material = mat;
            _torsoMeshRenderer.sortingOrder = 50;
        }
        public void SetTorsoColor(Color color)
        {
            if (_torsoMeshRenderer != null)
            {
                var mat = _torsoMeshRenderer.material;
                mat.color = new Color(color.r, color.g, color.b, mat.color.a); // keep alpha
            }
        }
    }
}
