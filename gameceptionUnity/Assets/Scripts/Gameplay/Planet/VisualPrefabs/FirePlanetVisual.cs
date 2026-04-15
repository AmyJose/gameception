using UnityEngine;

namespace Gameplay
{
    public class FirePlanetVisual : PlanetVisualBase
    {
        [Header("Core Renderers")]
        [SerializeField] private SpriteRenderer basePlanetRenderer;

        [Header("Optional Extra Renderers To Tint")]
        [SerializeField] private SpriteRenderer[] tintedRenderers;

        [Header("Vitality Colours")]
        [SerializeField] private Color healthyColor = Color.white;
        [SerializeField] private Color strugglingColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        [SerializeField] private Color dyingColor = new Color(0.45f, 0.45f, 0.45f, 1f);

        private PlanetDefinition _definition;
        private bool _isSelected;

        public override void Initialize(PlanetDefinition definition)
        {
            _definition = definition;

            if (_definition == null)
            {
                Debug.LogWarning("[FirePlanetVisual] Initialize called with null definition.");
                return;
            }

            ApplySprite();
            SetVitality(1f);
        }

        public override void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            ApplySprite();
        }

        public override void SetVitality(float normalizedVitality)
        {
            float t = Mathf.Clamp01(normalizedVitality);

            Color targetColor;
            if (t >= 0.5f)
            {
                float lerp = Mathf.InverseLerp(0.5f, 1f, t);
                targetColor = Color.Lerp(strugglingColor, healthyColor, lerp);
            }
            else
            {
                float lerp = Mathf.InverseLerp(0f, 0.5f, t);
                targetColor = Color.Lerp(dyingColor, strugglingColor, lerp);
            }

            if (basePlanetRenderer != null)
            {
                basePlanetRenderer.color = targetColor;
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
    }
}