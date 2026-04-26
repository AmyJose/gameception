using System.Collections;
using UnityEngine;

namespace Gameplay
{
    public class BonusUFOFlyBy : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform visualRoot;

        [Header("Movement Points")]
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform endPoint;

        [Header("Motion")]
        [SerializeField] private float flyDuration = 1.5f;
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Coroutine flyRoutine;

        private void Awake()
        {
            Hide();
        }

        public void Play()
        {
            if (startPoint == null || endPoint == null)
            {
                Debug.LogWarning("[BonusUFOFlyBy] Missing start or end point.");
                return;
            }

            Debug.Log($"[BonusUFOFlyBy] Start={startPoint.position}, End={endPoint.position}");

            if (flyRoutine != null)
            {
                StopCoroutine(flyRoutine);
            }

            transform.position = startPoint.position;
            Show();

            flyRoutine = StartCoroutine(FlyRoutine());
        }

        private IEnumerator FlyRoutine()
        {
            float elapsed = 0f;

            while (elapsed < flyDuration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / flyDuration);
                float curvedT = movementCurve.Evaluate(t);

                transform.position = Vector3.Lerp(startPoint.position, endPoint.position, curvedT);

                yield return null;
            }

            transform.position = endPoint.position;
            Hide();
            flyRoutine = null;
        }

        private void Show()
        {
            if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(true);
            }
        }

        private void Hide()
        {
            if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(false);
            }
        }
    }
}