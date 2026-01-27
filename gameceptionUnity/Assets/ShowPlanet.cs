using UnityEngine;
using UnityEngine.InputSystem;

public class ShowPlanet : MonoBehaviour
{
    public GameObject planet;

    void Start()
    {
        planet.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {   
            planet.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            planet.SetActive(true);
        }
    }
}
