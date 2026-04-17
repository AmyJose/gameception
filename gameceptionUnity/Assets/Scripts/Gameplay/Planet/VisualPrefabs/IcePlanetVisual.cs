using UnityEngine;

namespace Gameplay
{
    public class IcePlanetVisual : PlanetVisualBase
    {
        [Header("Core Renderers")]
        [SerializeField] private SpriteRenderer basePlanetRenderer;
        [SerializeField] private SpriteRenderer shineRenderer;

        private PlanetDefinition _definition;
        private bool _isSelected;

        public override void Initialize(PlanetDefinition definition)
        {
            _definition = definition;

            if (_definition == null)
            {
                Debug.LogWarning("[IcePlanetVisual] Initialize called with null definition.");
                return;
            }

            ApplySprite();
        }

        public override void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            ApplySprite();
        }

        public override void SetVitality(float normalizedVitality)
        {
            
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