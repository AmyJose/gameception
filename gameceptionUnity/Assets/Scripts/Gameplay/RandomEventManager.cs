 using UnityEngine;
using System.Collections.Generic;
using Gameplay;

namespace Gameplay
{
    public class RandomEventManager : MonoBehaviour
    {
        [SerializeField] private ResourceSystem resourceSystem;
        [SerializeField] private List<int> targetPlanetIndices;
        [SerializeField] private ElementPose boostElement;
        [SerializeField] private ElementPose drainElement;

        [SerializeField] private float eventInterval = 5f;
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

        void ResourceBoost()
        {
            Debug.Log("RandomEventManager: Resource Boost triggered");
            resourceSystem.ApplyElementToPlanets(boostElement, targetPlanetIndices, 0);
        }

        void ResourceDrain()
        {
            Debug.Log("RandomEventManager: Resource Drain triggered");
            resourceSystem.ApplyElementToPlanets(drainElement, targetPlanetIndices, 0);
        }

        void RareEvent()
        {
            Debug.Log("RandomEventManager: Rare event triggered");
            // add your rare logic here
        }
    }
}

//template code for random event manager, can be expanded with more complex events and logic