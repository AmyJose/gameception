using System.Collections;
using UnityEngine;

namespace Gameplay
{
    public class BonusUFOFlyBy : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform visualRoot;

        [Header("Movement Points")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform targetPoint;

        [Header("Motion")]
        [SerializeField] private float moveDuration = 1.0f;
        [SerializeField] private float pauseBeforeHide = 0.15f;
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [SerializeField] private float spawnScale = 0.3f;
        [SerializeField] private float scaleUpDuration = 0.25f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Coroutine flyRoutine;

        private Vector3 visualStartLocalPosition;
        private Quaternion visualStartLocalRotation;
        private Vector3 visualStartLocalScale;

        private void Awake()
        {
            if (visualRoot != null)
            {
                visualStartLocalPosition = visualRoot.localPosition;
                visualStartLocalRotation = visualRoot.localRotation;
                visualStartLocalScale = visualRoot.localScale;
            }

            Hide();
        }

        public void Play()
        {
            if (spawnPoint == null || targetPoint == null)
            {
                Debug.LogWarning("[BonusUFOFlyBy] Missing spawnPoint or targetPoint.");
                return;
            }

            if (flyRoutine != null)
            {
                StopCoroutine(flyRoutine);
                flyRoutine = null;
            }

            ResetUFOToSpawn();
            Show();
            StartCoroutine(ScaleUpRoutine());
            flyRoutine = StartCoroutine(FlyRoutine());
        }

        private void ResetUFOToSpawn()
        {
            transform.position = spawnPoint.position;
            transform.rotation = Quaternion.identity;

            if (visualRoot != null)
            {
                visualRoot.localPosition = visualStartLocalPosition;
                visualRoot.localRotation = visualStartLocalRotation;

                visualRoot.localScale = visualStartLocalScale * spawnScale;
            }
        }

        private IEnumerator FlyRoutine()
        {
            Vector3 start = spawnPoint.position;
            Vector3 end = targetPoint.position;

            transform.position = start;

            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / moveDuration);
                float eased = movementCurve.Evaluate(t);

                transform.position = Vector3.Lerp(start, end, eased);

                yield return null;
            }

            transform.position = end;

            yield return new WaitForSeconds(pauseBeforeHide);

            Hide();
            ResetUFOToSpawn();

            flyRoutine = null;
        }
        private IEnumerator ScaleUpRoutine()
        {
            if (visualRoot == null) yield break;

            float t = 0f;

            while (t < scaleUpDuration)
            {
                t += Time.deltaTime;

                float normalized = Mathf.Clamp01(t / scaleUpDuration);
                float eased = scaleCurve.Evaluate(normalized);

                float scale = Mathf.Lerp(spawnScale, 1f, eased);
                visualRoot.localScale = visualStartLocalScale * scale;

                yield return null;
            }

            visualRoot.localScale = visualStartLocalScale;
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