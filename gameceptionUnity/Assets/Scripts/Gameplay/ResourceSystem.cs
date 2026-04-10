using UnityEngine;
using System.Collections.Generic;
using InputLayer;

namespace Gameplay
{
    public class ResourceSystem : MonoBehaviour
    {
        [SerializeField] private PlanetManager planetManager;
        //[SerializeField] private float resourceAmountPerBeat = 5f;

        public void ApplyElementToPlanets(ElementPose element, List<int> planetIndices, int beatIndex)
        {
            foreach (var idx in planetIndices)
            {
                var planet = planetManager.GetPlanet(idx);
                if (planet == null) continue;

                //planet.ApplyElement(element, resourceAmountPerBeat);
                //planet.RestoreNeed(element);
                Debug.Log($"ResourceSystem: applied {element} to planet {idx} on beat {beatIndex}");
            }
        }
    }
}