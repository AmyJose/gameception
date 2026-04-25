using InputLayer;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TutorialLaneGlowController : MonoBehaviour
{
    [Serializable]
    public class LaneGlowBinding
    {
        [Tooltip("Pad index from DanceMatInputProvider. 0=Key1, 1=Key2, 2=Key3, 3=Key4")]
        public int padIndex;
        public SpriteRenderer laneRenderer;
        public Sprite normalSprite;
        public Sprite glowSprite;
        [Min(0.1f)] public float glowScaleMultiplier = 1f;
    }

    [Header("References")]
    [SerializeField] private DanceMatInputProvider danceMatInputProvider;

    [Header("Lane Asset Bindings")]
    [SerializeField] private List<LaneGlowBinding> laneBindings = new();

    private readonly Dictionary<int, LaneGlowBinding> _bindingsByPad = new();
    private readonly Dictionary<SpriteRenderer, Vector3> _initialScalesByRenderer = new();
    private int _activePadIndex = -1;
    private Vector3 _activeOriginalScale = Vector3.one;

    private void Awake()
    {
        BuildLookup();
        CacheInitialScales();
        ResetAllToNormal();
    }

    private void OnEnable()
    {
        if (danceMatInputProvider != null)
            danceMatInputProvider.OnPadPressed += HandlePadPressed;
    }

    private void OnDisable()
    {
        if (danceMatInputProvider != null)
            danceMatInputProvider.OnPadPressed -= HandlePadPressed;
    }

    public void ActivateGlowForPad(int padIndex)
    {
        DeactivateGlow();

        if (!_bindingsByPad.TryGetValue(padIndex, out LaneGlowBinding binding))
            return;

        if (binding.laneRenderer == null)
            return;

        _activePadIndex = padIndex;
        _activeOriginalScale = GetInitialScale(binding.laneRenderer);

        if (binding.glowSprite != null)
            binding.laneRenderer.sprite = binding.glowSprite;

        if (!Mathf.Approximately(binding.glowScaleMultiplier, 1f))
            binding.laneRenderer.transform.localScale = _activeOriginalScale * binding.glowScaleMultiplier;
    }

    public void DeactivateGlow()
    {
        if (_activePadIndex < 0)
            return;

        if (_bindingsByPad.TryGetValue(_activePadIndex, out LaneGlowBinding binding) && binding.laneRenderer != null)
        {
            if (binding.normalSprite != null)
                binding.laneRenderer.sprite = binding.normalSprite;

            binding.laneRenderer.transform.localScale = _activeOriginalScale;
        }

        _activePadIndex = -1;
    }

    public void ResetAllToNormal()
    {
        foreach (LaneGlowBinding binding in laneBindings)
        {
            if (binding == null || binding.laneRenderer == null)
                continue;

            if (binding.normalSprite != null)
                binding.laneRenderer.sprite = binding.normalSprite;

            binding.laneRenderer.transform.localScale = GetInitialScale(binding.laneRenderer);
        }

        _activePadIndex = -1;
    }

    private void HandlePadPressed(int padIndex)
    {
        if (padIndex == _activePadIndex)
            DeactivateGlow();
    }

    private void BuildLookup()
    {
        _bindingsByPad.Clear();

        foreach (LaneGlowBinding binding in laneBindings)
        {
            if (binding == null)
                continue;

            if (_bindingsByPad.ContainsKey(binding.padIndex))
            {
                Debug.LogWarning($"[TutorialLaneGlowController] Duplicate padIndex {binding.padIndex}. Last binding wins.");
            }

            _bindingsByPad[binding.padIndex] = binding;
        }
    }

    private void CacheInitialScales()
    {
        _initialScalesByRenderer.Clear();

        foreach (LaneGlowBinding binding in laneBindings)
        {
            if (binding == null || binding.laneRenderer == null)
                continue;

            if (_initialScalesByRenderer.ContainsKey(binding.laneRenderer))
                continue;

            _initialScalesByRenderer[binding.laneRenderer] = binding.laneRenderer.transform.localScale;
        }
    }

    private Vector3 GetInitialScale(SpriteRenderer renderer)
    {
        if (renderer == null)
            return Vector3.one;

        if (_initialScalesByRenderer.TryGetValue(renderer, out Vector3 scale))
            return scale;

        Vector3 current = renderer.transform.localScale;
        _initialScalesByRenderer[renderer] = current;
        return current;
    }
}