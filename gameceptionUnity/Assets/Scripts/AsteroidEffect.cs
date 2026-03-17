using UnityEngine;
using System.Collections;

namespace Gameplay
{
    public class AsteroidEffect : MonoBehaviour
    {
        [SerializeField] private GameObject asteroidPrefab;
        [SerializeField] private GameObject impactPrefab; 
        [SerializeField] private float speed = 3f;
        [SerializeField] private float spawnDistance = 10f;

        public void StrikeAt(Vector3 planetPosition)
        {
            StartCoroutine(AsteroidFly(planetPosition));
        }

        private IEnumerator AsteroidFly(Vector3 target)
        {

            Debug.Log("AsteroidFly started, target: " + target);
            // spawn from a random direction offscreen
            Vector3 randomDir = Random.insideUnitCircle.normalized;
            Vector3 spawnPos = target + randomDir * spawnDistance;

            spawnPos.z = 0f;

            GameObject asteroid = Instantiate(asteroidPrefab, spawnPos, Quaternion.identity);

            // rotate asteroid to face direction of travel
            Vector3 dir = (target - spawnPos).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            asteroid.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

            // fly toward planet
            float t = 0f;
            while (t < 1f)
            {
                if (asteroid == null) yield break;
                t += Time.deltaTime * speed;
                asteroid.transform.position = Vector3.Lerp(spawnPos, target, t);
                yield return null;
            }

            // impact
            Destroy(asteroid);
            if (impactPrefab != null)
            {
                GameObject impact = Instantiate(impactPrefab, target, Quaternion.identity);
                Destroy(impact, 1f); // remove after 1 second
            }

            Debug.Log("AsteroidEffect: impact at " + target);
        }
    }
}