using UnityEngine;

namespace Gameplay
{
    public class WaterPlanetVisual : PlanetVisualBase
    {
        [Header("Core Renderers")]
        [SerializeField] private SpriteRenderer basePlanetRenderer;
        [SerializeField] private SpriteRenderer seafoamRenderer;

        [Header("Optional Extra Renderers To Tint")]
        [SerializeField] private SpriteRenderer[] tintedRenderers;

        [Header("Optional Scrollers / Effects")]
        [SerializeField] private MonoBehaviour[] effectsToToggleWhenDead;

        [Header("Vitality Colours")]
        [SerializeField] private Color healthyColor = Color.white;
        [SerializeField] private Color strugglingColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        [SerializeField] private Color dyingColor = new Color(0.45f, 0.45f, 0.45f, 1f);

        [Header("Seafoam Settings")]
        [SerializeField] private bool tintSeafoamWithVitality = true;
        [SerializeField] private bool dimEffectsWhenLowVitality = false;
        [SerializeField, Range(0f, 1f)] private float lowVitalityEffectThreshold = 0.15f;

        private PlanetDefinition _definition;
        private bool _isSelected;

        public override void Initialize(PlanetDefinition definition)
        {
            _definition = definition;

            if (_definition == null)
            {
                Debug.LogWarning("[WaterPlanetVisual] Initialize called with null definition.");
                return;
            }

            ApplySprite();
            SetVitality(1f);
            UpdateEffectState(1f);
        }

        public override void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            ApplySprite();
        }

        public override void SetVitality(float normalizedVitality)
        {
            float t = Mathf.Clamp01(normalizedVitality);
            Color targetColor = EvaluateVitalityColor(t);

            if (basePlanetRenderer != null)
            {
                basePlanetRenderer.color = targetColor;
            }

            if (tintSeafoamWithVitality && seafoamRenderer != null)
            {
                seafoamRenderer.color = targetColor;
            }

            if (tintedRenderers != null)
            {
                for (int i = 0; i < tintedRenderers.Length; i++)
                {
                    if (tintedRenderers[i] != null)
                    {
                        tintedRenderers[i].color = targetColor;
                    }
                }
            }

            UpdateEffectState(t);
        }

        private void ApplySprite()
        {
            if (basePlanetRenderer == null || _definition == null)
                return;

            Sprite targetSprite = _isSelected && _definition.selectedPlanetSprite != null
                ? _definition.selectedPlanetSprite
                : _definition.planetSprite;

            basePlanetRenderer.sprite = targetSprite;
        }

        private Color EvaluateVitalityColor(float t)
        {
            if (t >= 0.5f)
            {
                float lerp = Mathf.InverseLerp(0.5f, 1f, t);
                return Color.Lerp(strugglingColor, healthyColor, lerp);
            }
            else
            {
                float lerp = Mathf.InverseLerp(0f, 0.5f, t);
                return Color.Lerp(dyingColor, strugglingColor, lerp);
            }
        }

        private void UpdateEffectState(float normalizedVitality)
        {
            if (!dimEffectsWhenLowVitality || effectsToToggleWhenDead == null)
                return;

            bool shouldEnable = normalizedVitality > lowVitalityEffectThreshold;

            for (int i = 0; i < effectsToToggleWhenDead.Length; i++)
            {
                if (effectsToToggleWhenDead[i] != null)
                {
                    effectsToToggleWhenDead[i].enabled = shouldEnable;
                }
            }
        }
    }
}