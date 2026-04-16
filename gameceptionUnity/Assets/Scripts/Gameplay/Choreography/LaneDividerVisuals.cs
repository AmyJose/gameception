using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Choreography
{
    [ExecuteAlways]
    public class LaneDividerVisuals : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PromptQueue promptQueue;

        [Header("Line Style")]
        [SerializeField] private Material lineMaterial;
        [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private float lineWidth = 0.06f;

        [Header("Render Order")]
        [SerializeField] private SpriteRenderer hitZoneRenderer;
        [SerializeField] private int sortingOrderOffset = 2;

        [Header("Line Span (Local Y)")]
        [SerializeField] private float minY = -8f;
        [SerializeField] private float maxY = 8f;
        [SerializeField] private float zOffset = 0f;
        [SerializeField] private float singleLaneHalfWidth = 1f;

        private readonly List<LineRenderer> _lineRenderers = new();

        private void OnEnable()
        {
            RebuildLines();
            RefreshLines();
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled)
                return;

            RebuildLines();
            RefreshLines();
        }

        private void Update()
        {
            if (promptQueue == null)
                return;

            RefreshLines();
        }

        [ContextMenu("Rebuild Lane Divider Lines")]
        public void RebuildLines()
        {
            if (promptQueue == null)
                return;

            EnsureTrackedChildLineRenderers();

            int laneCount = promptQueue.LaneCount;
            int boundaryCount = laneCount > 0 ? laneCount + 1 : 0;

            while (_lineRenderers.Count < boundaryCount)
            {
                int idx = _lineRenderers.Count;
                var child = new GameObject($"LaneDivider_{idx}");
                child.transform.SetParent(transform, false);

                var lr = child.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.useWorldSpace = true;
                lr.numCapVertices = 2;
                _lineRenderers.Add(lr);
            }

            while (_lineRenderers.Count > boundaryCount)
            {
                int last = _lineRenderers.Count - 1;
                var lr = _lineRenderers[last];
                _lineRenderers.RemoveAt(last);

                if (lr != null)
                {
                    if (Application.isPlaying)
                        Destroy(lr.gameObject);
                    else
                        DestroyImmediate(lr.gameObject);
                }
            }
        }

        private void RefreshLines()
        {
            if (promptQueue == null)
                return;

            for (int i = 0; i < _lineRenderers.Count; i++)
            {
                var lr = _lineRenderers[i];
                if (lr == null)
                    continue;

                if (!TryGetBoundaryLocalX(i, out var boundaryX))
                {
                    lr.enabled = false;
                    continue;
                }

                lr.enabled = true;
                lr.sharedMaterial = lineMaterial;
                lr.startColor = lineColor;
                lr.endColor = lineColor;
                lr.startWidth = lineWidth;
                lr.endWidth = lineWidth;
                ApplyRenderOrder(lr);

                Vector3 topLocal = new Vector3(boundaryX, maxY, zOffset);
                Vector3 bottomLocal = new Vector3(boundaryX, minY, zOffset);

                Vector3 topWorld = promptQueue.transform.TransformPoint(topLocal);
                Vector3 bottomWorld = promptQueue.transform.TransformPoint(bottomLocal);

                lr.SetPosition(0, topWorld);
                lr.SetPosition(1, bottomWorld);
            }
        }

        private bool TryGetBoundaryLocalX(int boundaryIndex, out float boundaryX)
        {
            boundaryX = 0f;

            int laneCount = promptQueue != null ? promptQueue.LaneCount : 0;
            if (laneCount <= 0)
                return false;

            if (boundaryIndex < 0 || boundaryIndex > laneCount)
                return false;

            if (laneCount == 1)
            {
                if (!promptQueue.TryGetLaneCenterLocalPosition(0, out var singleCenter))
                    return false;

                boundaryX = boundaryIndex == 0
                    ? singleCenter.x - singleLaneHalfWidth
                    : singleCenter.x + singleLaneHalfWidth;

                return true;
            }

            if (boundaryIndex == 0)
            {
                if (!promptQueue.TryGetLaneCenterLocalPosition(0, out var firstCenter))
                    return false;
                if (!promptQueue.TryGetLaneCenterLocalPosition(1, out var secondCenter))
                    return false;

                float firstSpacing = Mathf.Abs(secondCenter.x - firstCenter.x);
                boundaryX = firstCenter.x - firstSpacing * 0.5f;
                return true;
            }

            if (boundaryIndex == laneCount)
            {
                if (!promptQueue.TryGetLaneCenterLocalPosition(laneCount - 1, out var lastCenter))
                    return false;
                if (!promptQueue.TryGetLaneCenterLocalPosition(laneCount - 2, out var prevCenter))
                    return false;

                float lastSpacing = Mathf.Abs(lastCenter.x - prevCenter.x);
                boundaryX = lastCenter.x + lastSpacing * 0.5f;
                return true;
            }

            if (!promptQueue.TryGetLaneCenterLocalPosition(boundaryIndex - 1, out var leftCenter))
                return false;
            if (!promptQueue.TryGetLaneCenterLocalPosition(boundaryIndex, out var rightCenter))
                return false;

            boundaryX = (leftCenter.x + rightCenter.x) * 0.5f;
            return true;
        }

        private void EnsureTrackedChildLineRenderers()
        {
            _lineRenderers.RemoveAll(lr => lr == null);

            var childRenderers = GetComponentsInChildren<LineRenderer>(true);
            foreach (var lr in childRenderers)
            {
                if (_lineRenderers.Contains(lr))
                    continue;

                _lineRenderers.Add(lr);
            }
        }

        private void ApplyRenderOrder(LineRenderer lr)
        {
            if (lr == null)
                return;

            if (hitZoneRenderer != null)
            {
                lr.sortingLayerID = hitZoneRenderer.sortingLayerID;
                lr.sortingOrder = hitZoneRenderer.sortingOrder + sortingOrderOffset;
                return;
            }

            lr.sortingLayerID = SortingLayer.NameToID("Default");
            lr.sortingOrder = sortingOrderOffset;
        }
    }
}
