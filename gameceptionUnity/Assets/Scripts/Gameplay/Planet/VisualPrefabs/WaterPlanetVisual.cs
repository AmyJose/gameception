using UnityEngine;

namespace Gameplay
{
    public class WaterPlanetVisual : PlanetVisualBase
    {
        [Header("Core Renderers")]
        [SerializeField] private SpriteRenderer basePlanetRenderer;
        [SerializeField] private SpriteRenderer seafoamRenderer;
        [SerializeField] private GameObject selectionGlowObject;

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

            if (basePlanetRenderer != null && _definition.planetSprite != null)
            {
                basePlanetRenderer.sprite = _definition.planetSprite;
            }

            if (selectionGlowObject != null)
            {
                selectionGlowObject.SetActive(false);
            }
        }

        public override void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            if (selectionGlowObject != null)
            {
                selectionGlowObject.SetActive(isSelected);
            }
        }

        public override void SetVitality(float normalizedVitality)
        {
            
        }
    }
}