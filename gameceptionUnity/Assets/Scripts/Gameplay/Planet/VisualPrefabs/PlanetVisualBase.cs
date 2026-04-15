using UnityEngine;

namespace Gameplay
{
    public abstract class PlanetVisualBase : MonoBehaviour
    {
        [SerializeField] private float alienOrbitRadiusWorld = 1.25f;
        public abstract void Initialize(PlanetDefinition definition);
        public abstract void SetSelected(bool isSelected);
        public abstract void SetVitality(float normalizedVitality);
        public virtual float GetBodyRadiusWorld()
        {
            return alienOrbitRadiusWorld;
        }
        //public abstract void SetGrowingScale(float scaleMultiplier);
        //next: setChoreoActive
        //playspawnstart
        //playspawncomplete
    }
}