using UnityEngine;

namespace Gameplay
{
    public class PlanetManager : MonoBehaviour
    {
        [SerializeField] private Planet[] planets;

        public Planet GetPlanet(int index)
        {
            if (index < 0 || index >= planets.Length) return null;
            return planets[index];
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (var p in planets) p.Tick(dt);
        }
    }
}