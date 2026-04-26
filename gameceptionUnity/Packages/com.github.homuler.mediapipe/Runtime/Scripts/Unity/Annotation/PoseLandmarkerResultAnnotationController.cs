// Copyright (c) 2023 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections.Generic;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine;

namespace Mediapipe.Unity
{
  public class PoseLandmarkerResultAnnotationController : AnnotationController<MultiPoseLandmarkListWithMaskAnnotation>
  {
    [SerializeField] private bool _visualizeZ = false;

    private readonly object _currentTargetLock = new object();

        private PoseLandmarkerResult _currentTarget;

        [Header("Skeleton Smoothing")]
        [SerializeField] private bool _smoothSkeleton = true;
        [SerializeField] private float _smoothSpeed = 22f;
        [SerializeField] private float _maxJump = 0.35f;
        [SerializeField] private int _maxBadFrames = 12;

        private Vector3[] _smoothedPositions;
        private int[] _badFrames;
        private bool[] _hasSmoothedPosition;

        public void InitScreen(int maskWidth, int maskHeight) => annotation.InitMask(maskWidth, maskHeight);

    public void DrawNow(PoseLandmarkerResult target)
    {
      target.CloneTo(ref _currentTarget);
      if (_currentTarget.segmentationMasks != null)
      {
        ReadMask(_currentTarget.segmentationMasks);
        // NOTE: segmentationMasks can still be accessed from newTarget.
        _currentTarget.segmentationMasks.Clear();
      }
      SyncNow();
    }

    public void DrawLater(PoseLandmarkerResult target) => UpdateCurrentTarget(target);

    private void ReadMask(IReadOnlyList<Image> segmentationMasks) => annotation.ReadMask(segmentationMasks, isMirrored);

    protected void UpdateCurrentTarget(PoseLandmarkerResult newTarget)
    {
      lock (_currentTargetLock)
      {
        newTarget.CloneTo(ref _currentTarget);
        if (_currentTarget.segmentationMasks != null)
        {
          ReadMask(_currentTarget.segmentationMasks);
          // NOTE: segmentationMasks can still be accessed from newTarget.
          _currentTarget.segmentationMasks.Clear();
        }
        isStale = true;
      }
    }

    public void SetSkeletonColor(UnityEngine.Color color)
    {
        annotation.SetLeftLandmarkColor(color);
        annotation.SetRightLandmarkColor(color);
        annotation.SetConnectionColor(color);
            annotation.SetTorsoColor(color);
    }

        protected override void SyncNow()
        {
            lock (_currentTargetLock)
            {
                isStale = false;

                annotation.Draw(_currentTarget.poseLandmarks, _visualizeZ);

                if (_smoothSkeleton)
                {
                    SmoothPointAnnotations();
                }

                HideFaceLandmarks();
            }
        }

        private void HideFaceLandmarks()
    {
      var pointAnnotations = annotation.GetComponentsInChildren<PointAnnotation>();
      // face landmarks are always the first 11 per pose (33 total per pose)
      for (int i = 0; i < pointAnnotations.Length; i++)
      {
          int landmarkIndex = i % 33;
          if (landmarkIndex <= 10)
              pointAnnotations[i].gameObject.SetActive(false);
      }
    }
        private void SmoothPointAnnotations()
        {
            var points = annotation.GetComponentsInChildren<PointAnnotation>(true);

            if (points == null || points.Length == 0)
                return;

            if (_smoothedPositions == null || _smoothedPositions.Length != points.Length)
            {
                _smoothedPositions = new Vector3[points.Length];
                _badFrames = new int[points.Length];
                _hasSmoothedPosition = new bool[points.Length];
            }

            float t = 1f - Mathf.Exp(-_smoothSpeed * Time.deltaTime);

            for (int i = 0; i < points.Length; i++)
            {
                var point = points[i];
                if (point == null) continue;

                var tr = point.transform;
                Vector3 rawPos = tr.position;

                if (!_hasSmoothedPosition[i])
                {
                    _smoothedPositions[i] = rawPos;
                    _hasSmoothedPosition[i] = true;
                    _badFrames[i] = 0;
                    continue;
                }

                Vector3 delta = rawPos - _smoothedPositions[i];

                if (delta.magnitude > _maxJump)
                {
                    _badFrames[i]++;

                    if (_badFrames[i] <= _maxBadFrames)
                    {
                        tr.position = _smoothedPositions[i];
                        continue;
                    }

                    point.gameObject.SetActive(false);
                    continue;
                }

                _badFrames[i] = 0;
                _smoothedPositions[i] = Vector3.Lerp(_smoothedPositions[i], rawPos, t);
                tr.position = _smoothedPositions[i];
            }
        }
    }
}
