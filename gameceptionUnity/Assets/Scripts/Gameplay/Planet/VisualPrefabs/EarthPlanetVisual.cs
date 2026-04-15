using UnityEngine;

namespace Gameplay
{
    public class EarthPlanetVisual : PlanetVisualBase
    {
        [Header("Core Renderers")]
        [SerializeField] private SpriteRenderer basePlanetRenderer;

        [Header("Decorative Renderers")]
        [SerializeField] private SpriteRenderer[] decorativeRenderers;

        [Header("Optional Sway / Effect Scripts")]
        [SerializeField] private MonoBehaviour[] effectsToToggleWhenLowVitality;

        [Header("Vitality Colours")]
        [SerializeField] private Color healthyColor = Color.white;
        [SerializeField] private Color strugglingColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        [SerializeField] private Color dyingColor = new Color(0.48f, 0.48f, 0.48f, 1f);

        [Header("Decoration Settings")]
        [SerializeField] private bool tintDecorationsWithVitality = true;
        [SerializeField] private bool disableSwayWhenVeryLow = false;
        [SerializeField, Range(0f, 1f)] private float swayDisableThreshold = 0.15f;

        private PlanetDefinition _definition;
        private bool _isSelected;

        public override void Initialize(PlanetDefinition definition)
        {
            _definition = definition;

            if (_definition == null)
            {
                Debug.LogWarning("[EarthPlanetVisual] Initialize called with null definition.");
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

            if (tintDecorationsWithVitality && decorativeRenderers != null)
            {
                for (int i = 0; i < decorativeRenderers.Length; i++)
                {
                    if (decorativeRenderers[i] != null)
                    {
                        decorativeRenderers[i].color = targetColor;
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
            if (!disableSwayWhenVeryLow)
                return;

            bool shouldEnable = normalizedVitality > swayDisableThreshold;

            if (effectsToToggleWhenLowVitality != null)
            {
                for (int i = 0; i < effectsToToggleWhenLowVitality.Length; i++)
                {
                    if (effectsToToggleWhenLowVitality[i] != null)
                    {
                        effectsToToggleWhenLowVitality[i].enabled = shouldEnable;
                    }
                }
            }
        }
    }
}