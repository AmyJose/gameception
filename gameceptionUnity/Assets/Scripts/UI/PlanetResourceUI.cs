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

        float fireCurrent = planet.Fire / maxVal;
        float waterCurrent = planet.Water / maxVal;
        float earthCurrent = planet.Earth / maxVal;
        float iceCurrent = planet.Ice / maxVal;

        float fireTarget = planet.Definition.targetFire / maxVal;
        float waterTarget = planet.Definition.targetWater / maxVal;
        float earthTarget = planet.Definition.targetEarth / maxVal;
        float iceTarget = planet.Definition.targetIce / maxVal;

        if (fireBar != null) fireBar.SetNormalised(fireCurrent, fireTarget);
        if (waterBar != null) waterBar.SetNormalised(waterCurrent, waterTarget);
        if (earthBar != null) earthBar.SetNormalised(earthCurrent, earthTarget);
        if (iceBar != null) iceBar.SetNormalised(iceCurrent, iceTarget);
    }

    public void SetVisible(bool visible)
    {
        if (root != null) root.SetActive(visible);
    }
}
