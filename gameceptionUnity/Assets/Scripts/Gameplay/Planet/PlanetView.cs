using Unity.VisualScripting;
using UnityEngine;

public class PlanetView : MonoBehaviour
{
    //[SerializeField] private GameObject selectionRing;
    [SerializeField] private PlanetResourceUI resourceUI;

    //glowing
    [SerializeField] private SpriteRenderer planetRenderer;
    [SerializeField] private Sprite planet;
    [SerializeField] private Sprite planetglowing;

    public void SetSelected(bool selected)
    { 
        //if (selectionRing != null) selectionRing.SetActive(selected);
        if (resourceUI != null)
        {
            resourceUI.SetVisible(selected);
        }

        //glowing, sprite swap
        if (planetRenderer != null && planet != null && planetglowing != null)
        {
            planetRenderer.sprite = selected ? planetglowing : planet;
        }

    }

    public void ShowResourceUI()
    {
        if (resourceUI != null) resourceUI.SetVisible(true);
    }
    public void HideResourceUI()
    {
        if (resourceUI != null) resourceUI.SetVisible(false);
    }
}
