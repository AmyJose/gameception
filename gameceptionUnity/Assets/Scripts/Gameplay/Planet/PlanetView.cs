using Unity.VisualScripting;
using System.Linq;
using UnityEngine;
using InputLayer;
public class PlanetView : MonoBehaviour
{
    //glowing
    [SerializeField] private SpriteRenderer planetRenderer;
    [SerializeField] private Sprite planet;
    [SerializeField] private Sprite planetGlowing;

    /*public void SetSelected(bool selected)
    {

        //glowing, sprite swap
        if (planetRenderer != null && planet != null && planetGlowing != null)
        {
            planetRenderer.sprite = selected ? planetGlowing : planet;
        }
    }*/
}
