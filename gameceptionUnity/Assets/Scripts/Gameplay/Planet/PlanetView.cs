using Unity.VisualScripting;
using System.Linq;
using UnityEngine;
using InputLayer;
public class PlanetView : MonoBehaviour
{
    [SerializeField] private PlanetResourceUI resourceUI;

    //glowing
    [SerializeField] private SpriteRenderer planetRenderer;
    [SerializeField] private Sprite planet;
    [SerializeField] private Sprite planetGlowing;

    public void SetSelected(bool selected)
    { 
        //if (selectionRing != null) selectionRing.SetActive(selected);
        if (resourceUI != null)
        {
            resourceUI.SetVisible(selected);
        }

        //glowing, sprite swap
        if (planetRenderer != null && planet != null && planetGlowing != null)
        {
            planetRenderer.sprite = selected ? planetGlowing : planet;
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
