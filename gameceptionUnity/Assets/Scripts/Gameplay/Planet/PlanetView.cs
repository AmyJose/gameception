using UnityEngine;

public class PlanetView : MonoBehaviour
{
    //[SerializeField] private GameObject selectionRing;
    [SerializeField] private PlanetResourceUI resourceUI;

    public void SetSelected(bool selected)
    { 
        //if (selectionRing != null) selectionRing.SetActive(selected);

        if (resourceUI != null)
        {
            resourceUI.SetVisible(selected);
        }

    }

}
