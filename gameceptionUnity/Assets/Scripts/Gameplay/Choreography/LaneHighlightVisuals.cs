using System.Collections.Generic;
using UnityEngine;
using InputLayer;

namespace Gameplay.Choreography
{
    [System.Serializable]
    public class LaneHighlightConfig
    {
        public Color highlightColor = new Color(1f, 1f, 1f, 0.3f);
        public GameObject prefab;
        public float xOffsetPixels = -0.08f;
        public float widthOffsetPixels = 0f;
    }

    public class LaneHighlightVisuals : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PromptQueue promptQueue;
        [SerializeField] private SelectionState selectionState;

        [Header("Visual Style")]
        [SerializeField] private Color defaultHighlightColor = new Color(1f, 1f, 1f, 0.3f);
        [SerializeField] private float minY = -10f;
        [SerializeField] private float maxY = 10f;
        
        [Header("Per-Lane Configuration")]
        [SerializeField] private LaneHighlightConfig[] laneConfigs = new LaneHighlightConfig[4];

        private Dictionary<int, GameObject> _highlights = new();
        private Dictionary<int, SpriteRenderer> _spriteRenderers = new();

        private void OnEnable()
        {
            if (selectionState != null)
                selectionState.OnChanged += OnSelectionChanged;
            
            CreateHighlights();
        }

        private void OnDisable()
        {
            if (selectionState != null)
                selectionState.OnChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged(IReadOnlyCollection<int> selected)
        {
            UpdateHighlights();
        }

        private void CreateHighlights()
        {
            if (promptQueue == null) return;

            int laneCount = promptQueue.LaneCount;

            for (int i = 0; i < laneCount; i++)
            {
                if (_highlights.ContainsKey(i)) continue;

                LaneHighlightConfig config = i < laneConfigs.Length ? laneConfigs[i] : new LaneHighlightConfig();

                GameObject highlightObj;

                // Use prefab if provided, otherwise create sprite-based highlight
                if (config.prefab != null)
                {
                    highlightObj = Instantiate(config.prefab, transform);
                    highlightObj.name = $"LaneHighlight_{i}";
                    highlightObj.transform.localPosition = Vector3.zero;
                }
                else
                {
                    highlightObj = new GameObject($"LaneHighlight_{i}");
                    highlightObj.transform.SetParent(transform, false);

                    SpriteRenderer sr = highlightObj.AddComponent<SpriteRenderer>();
                    sr.sprite = CreateWhiteSprite();
                    sr.drawMode = SpriteDrawMode.Sliced;
                    sr.color = config.highlightColor;
                    sr.sortingOrder = -10;

                    _spriteRenderers[i] = sr;
                }

                _highlights[i] = highlightObj;
            }
        }

        private void UpdateHighlights()
        {
            if (promptQueue == null || selectionState == null) return;

            for (int laneIdx = 0; laneIdx < promptQueue.LaneCount; laneIdx++)
            {
                if (!_highlights.TryGetValue(laneIdx, out var highlightObj)) continue;

                bool isSelected = selectionState.IsSelected(laneIdx);
                LaneHighlightConfig config = laneIdx < laneConfigs.Length ? laneConfigs[laneIdx] : new LaneHighlightConfig();

                if (!isSelected)
                {
                    highlightObj.SetActive(false);
                    continue;
                }

                highlightObj.SetActive(true);

                if (!TryGetLaneBoundariesLocalX(laneIdx, out float leftX, out float rightX))
                {
                    highlightObj.SetActive(false);
                    continue;
                }

                // Apply per-lane width offset
                float centerX = (leftX + rightX) * 0.5f;
                float halfWidth = (rightX - leftX) * 0.5f;
                
                leftX = centerX - halfWidth + config.widthOffsetPixels;
                rightX = centerX + halfWidth - config.widthOffsetPixels;

                // Position and size rectangle
                float finalCenterX = (leftX + rightX) * 0.5f + config.xOffsetPixels;
                float centerY = (minY + maxY) * 0.5f;
                float width = rightX - leftX;
                float height = maxY - minY;

                highlightObj.transform.localPosition = new Vector3(finalCenterX, centerY, 0.1f);

                // Update SpriteRenderer,if using sprite-based highlight
                if (_spriteRenderers.TryGetValue(laneIdx, out var sr))
                {
                    sr.color = config.highlightColor;
                    sr.size = new Vector2(width, height);
                }
                else
                {
                    // scale prefab
                    highlightObj.transform.localScale = new Vector3(width, height, 1f);
                }
            }
        }

        private bool TryGetLaneBoundariesLocalX(int laneIdx, out float leftX, out float rightX)
        {
            leftX = 0f;
            rightX = 0f;

            int laneCount = promptQueue != null ? promptQueue.LaneCount : 0;
            if (laneCount <= 0)
                return false;

            if (laneIdx < 0 || laneIdx >= laneCount)
                return false;

            // Get current lane center
            if (!promptQueue.TryGetLaneCenterLocalPosition(laneIdx, out var currentCenter))
                return false;

            // Left boundary
            if (laneIdx == 0)
            {
                if (!promptQueue.TryGetLaneCenterLocalPosition(1, out var nextCenter))
                    return false;

                float spacing = Mathf.Abs(nextCenter.x - currentCenter.x);
                leftX = currentCenter.x - spacing * 0.5f;
            }
            else
            {
                if (!promptQueue.TryGetLaneCenterLocalPosition(laneIdx - 1, out var prevCenter))
                    return false;

                leftX = (prevCenter.x + currentCenter.x) * 0.5f;
            }

            // Right boundary
            if (laneIdx == laneCount - 1)
            {
                if (!promptQueue.TryGetLaneCenterLocalPosition(laneIdx - 1, out var prevCenter))
                    return false;

                float spacing = Mathf.Abs(currentCenter.x - prevCenter.x);
                rightX = currentCenter.x + spacing * 0.5f;
            }
            else
            {
                if (!promptQueue.TryGetLaneCenterLocalPosition(laneIdx + 1, out var nextCenter))
                    return false;

                rightX = (currentCenter.x + nextCenter.x) * 0.5f;
            }

            return true;
        }

        private Sprite CreateWhiteSprite()
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        }
    }
}