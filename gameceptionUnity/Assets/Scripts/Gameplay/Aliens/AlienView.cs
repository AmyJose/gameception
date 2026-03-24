using UnityEngine;

namespace Gameplay
{
    public class AlienView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Mood sprites")]
        [SerializeField] private Sprite happySprite;
        [SerializeField] private Sprite angrySprite;

        private bool _isAngry;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            ApplyMood();
        }
        public void SetMood(bool angry)
        {
            if (_isAngry == angry) return;

            _isAngry = angry;
            ApplyMood();
        }

        public bool IsAngry => _isAngry;

        private void ApplyMood()
        {
            if (spriteRenderer == null) return;

            spriteRenderer.sprite = _isAngry ? angrySprite : happySprite;
        }
    }
}
