using UnityEngine;
using UnityEngine.InputSystem;

public class ShowPlanet : MonoBehaviour
{
    public GameObject planet;
    public Transform parent;
    public float spacing = 2.0f;
    private int count;

    void Start()
    {
        planet.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame && count < 8)
        {   
            var p = parent != null ? parent : planet.transform.parent;
            GameObject clone = Instantiate(planet, p);
            clone.name = $"{planet.name}_Clone_{count++}";
            clone.transform.position = planet.transform.position + Vector3.right * spacing * count;
            clone.transform.rotation = planet.transform.rotation;
            clone.transform.localScale = planet.transform.localScale;
            planet.SetActive(true);
            count++;
        }
    }
}
