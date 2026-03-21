using UnityEngine;
using InputLayer;

namespace Gameplay
{
    public class NeedSlotSprite : MonoBehaviour
    {
        [Header("Renderers")]
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private SpriteRenderer backgroundRenderer;

        [Header("Element Sprites")]
        [SerializeField] private Sprite fireSprite;
        [SerializeField] private Sprite waterSprite;
        [SerializeField] private Sprite earthSprite;
        [SerializeField] private Sprite iceSprite;

        [Header("Colours")]
        [SerializeField] private Color filledColor = Color.white;
        [SerializeField] private Color fadingColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);

        public void SetSlot(ElementPose element, NeedState state)
        {
            if (iconRenderer != null)
            {
                iconRenderer.sprite = GetSprite(element);
                iconRenderer.color = GetColor(state);
            }

            if (backgroundRenderer != null)
            {
                backgroundRenderer.color = GetBackgroundColor(state);
            }
        }

        private Sprite GetSprite(ElementPose element)
        {
            switch (element)
            {
                case ElementPose.Fire: return fireSprite;
                case ElementPose.Water: return waterSprite;
                case ElementPose.Earth: return earthSprite;
                case ElementPose.Ice: return iceSprite;
                default: return null;
            }
        }

        private Color GetColor(NeedState state)
        {
            switch (state)
            {
                case NeedState.Filled: return filledColor;
                case NeedState.Fading: return fadingColor;
                case NeedState.Empty: return emptyColor;
                default: return Color.white;
            }
        }

        private Color GetBackgroundColor(NeedState state)
        {
            switch (state)
            {
                case NeedState.Filled: return new Color(1f, 1f, 1f, 0.2f);
                case NeedState.Fading: return new Color(1f, 1f, 1f, 0.1f);
                case NeedState.Empty: return new Color(0f, 0f, 0f, 0.15f);
                default: return Color.white;
            }
        }
    }
}