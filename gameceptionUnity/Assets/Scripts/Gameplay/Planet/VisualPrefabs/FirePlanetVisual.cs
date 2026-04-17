using UnityEngine;

namespace Gameplay
{
    public class FirePlanetVisual : PlanetVisualBase
    {
        [Header("Core Renderers")]
        [SerializeField] private SpriteRenderer basePlanetRenderer;

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
            //something here!
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