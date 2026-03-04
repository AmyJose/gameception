using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using InputLayer;

public class Planet : MonoBehaviour
{
    [SerializeField] private float life = 100f;
    [SerializeField] private float decayPerSecond = 2f;

    public void Tick(float dt)
    {
        life -= decayPerSecond * dt;
        life = Mathf.Clamp(life, 0f, 100f);
    }

    public void ApplyElement(ElementPose element)
    {
        switch (element) 
        {
            case ElementPose.Water: Debug.Log("Planet: Water recieved"); break;
            case ElementPose.Fire: Debug.Log("Planet: Fire recieved"); break;
            case ElementPose.Earth: Debug.Log("Planet: Earth recieved"); break;
            case ElementPose.Ice: Debug.Log("Planet: Ice recieved"); break;

        }
    }
}
