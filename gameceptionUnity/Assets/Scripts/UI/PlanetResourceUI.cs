using Gameplay;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PlanetResourceUI : MonoBehaviour
{
    [SerializeField] private Planet planet;

    [SerializeField] private ResourceBar fireBar;
    [SerializeField] private ResourceBar waterBar;
    [SerializeField] private ResourceBar earthBar;
    [SerializeField] private ResourceBar iceBar;

    [SerializeField] private GameObject root;

    private void Awake()
    {
        if (planet ==null) planet = GetComponentInParent<Planet>();
        if (root == null) root = gameObject;
    }

    private void Update()
    {
        if (planet == null) return;

        float maxVal = Mathf.Max(planet.MaxElement, 0.0001f);

        if (fireBar != null) fireBar.SetNormalised(planet.Fire / maxVal);
        if (waterBar != null) waterBar.SetNormalised(planet.Water / maxVal);
        if (earthBar != null) earthBar.SetNormalised(planet.Earth / maxVal);
        if (iceBar != null) iceBar.SetNormalised(planet.Ice / maxVal);
    }

    public void SetVisible(bool visible)
    {
        if (root != null) root.SetActive(visible);
    }
}
