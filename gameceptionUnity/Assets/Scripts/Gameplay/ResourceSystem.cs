using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using InputLayer;

namespace Gameplay
{
    public class ResourceSystem : MonoBehaviour
    {
        [SerializeField] private PlanetManager planetManager;

        public void ApplyElementToPlanets(ElementPose element, List<int> planetIndices, int beatIndex)
        {
            foreach(var idx in planetIndices)
            {
                var planet = planetManager.GetPlanet(idx);
                if (planet == null) continue;

                planet.ApplyElement(element);
                Debug.Log("ResourceSystem: applied element to a planet");
            }
        }
    }
}