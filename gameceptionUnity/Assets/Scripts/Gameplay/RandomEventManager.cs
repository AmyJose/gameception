using UnityEngine;
using System.Collections.Generic;
using InputLayer;
using Gameplay;

namespace Gameplay
{
    public class RandomEventManager : MonoBehaviour
    {
        [SerializeField] private ResourceSystem resourceSystem;
        [SerializeField] private PlanetManager planetManager;

        [SerializeField] private float eventInterval = 5f;
        [SerializeField] private float minAmount = 5f;
        [SerializeField] private float maxAmount = 20f;


        [SerializeField] private AsteroidEffect asteroidEffect;

        private float timer = 0f;

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= eventInterval)
            {
                timer = 0f;
                TriggerRandomEvent();
            }
        }

        void TriggerRandomEvent()
        {
            float roll = Random.Range(0f, 1f);

            if (roll < 0.5f)
                ResourceBoost();
            else if (roll < 0.8f)
                ResourceDrain();
            else
                RareEvent();
        }

        // boosts a random element on all planets
        void ResourceBoost()
        {
            float amount = Random.Range(minAmount, maxAmount);
            ElementPose element = GetRandomElement();
            Debug.Log($"RandomEventManager: Boost {element} by {amount} on all planets");

            int count = planetManager.PlanetCount;
            for (int i = 0; i < count; i++)
            {
                Planet p = planetManager.GetPlanet(i);
                if (p == null) continue;
                //p.ApplyElement(element, amount);
            }
        }

        // drains a random element but targets planets randomly
        void ResourceDrain()
        {
            float amount = Random.Range(minAmount, maxAmount);
            Debug.Log($"RandomEventManager: Drain on random planets");

            int count = planetManager.PlanetCount;
            for (int i = 0; i < count; i++)
            {
                // each planet has a 50% chance of being targeted
                if (Random.value < 0.5f) continue;

                Planet p = planetManager.GetPlanet(i);
                if (p == null) continue;

                ElementPose element = GetRandomElement();
                //p.ApplyElement(element, -amount); // negative = drain
                Debug.Log($"RandomEventManager: Drained {element} on planet {i}");
            }
        }

        // wipes all elements on all planets to zero
        void RareEvent()
        {
            Debug.Log("RandomEventManager: ASTEROID IMPACT - wiping all elements");

            int count = planetManager.PlanetCount;
            for (int i = 0; i < count; i++)
            {
                if (Random.value < 0.1f) continue;
                Planet p = planetManager.GetPlanet(i);
                if (p == null) continue;
                asteroidEffect.StrikeAt(p.transform.position);
                //p.SetElements(0f, 0f, 0f, 0f);
            }
        }

        // helper to pick a random element
        ElementPose GetRandomElement()
        {
            return (ElementPose)Random.Range(0, 4);
        }
    }
}


//Ideas for future events:
// Ice Age - freeze all planets for few seconds, preventing any changes
// Solar Flare/ SuperNova - Insane spikes in fire element/ earth
// Asteroid Impact (DONE)
// Alien Invasion - randomly swap elements between planets (Potential Boss - DIFFICULT)
// Eclipse - temporarily turn screen black, also lose all fire and gain ice
// Comet Shower - Allows user to do one "wish" - "Wish" implementation could be done like roguelikes 
// Black Hole - drain elements from all planet, increased decay rate (Potential Boss - DIFFICULT)
// Golden Age - boost population, unlock some stuff maybe, lower to no decay rate
// Galactic Pandemic - population down, random element drained, increased decay rate (Potential Boss - EASY)
// Harvest Moon - Population up, earth boost
// Stardust - Element regen instead of decay


