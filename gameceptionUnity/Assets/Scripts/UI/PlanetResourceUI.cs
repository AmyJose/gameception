using Gameplay;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PlanetResourceUI : MonoBehaviour
{
    [SerializeField] private Planet planet;

    [SerializeField] private GameObject root;

    private void Awake()
    {
        if (planet == null) planet = GetComponentInParent<Planet>();
        if (root == null) root = gameObject;
    }
    public void SetVisible(bool visible)
    {
        if (root != null) root.SetActive(visible);
    }
}
