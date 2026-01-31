using UnityEngine;

public class PanelToggle : MonoBehaviour
{
    public GameObject firstPanel; 
    public GameObject secondPanel; 
    public GameObject thirdPanel;
    public GameObject fourthPanel;

    // Moves forward (Planet -> Rain)
    public void SwitchToRainPanel()
    {
        firstPanel.SetActive(false);
        secondPanel.SetActive(true);
    }

    // Moves backward (Rain -> Planet)
    public void GoBackToPlanet()
    {
        secondPanel.SetActive(false);
        firstPanel.SetActive(true);
    }

    // Moves foward (Rain -> Food)
    public void SwitchToFoodPanel()
    {
        secondPanel.SetActive(false);
        thirdPanel.SetActive(true);
    }

    // Moves backward (Food -> Rain)
    public void GoBackToRain()
    {
        thirdPanel.SetActive(false);
        secondPanel.SetActive(true);
    }

    //Moves forward (Food -> Wildlife)
    public void SwitchToWildlifePanel()
    {
        thirdPanel.SetActive(false);
        fourthPanel.SetActive(true);
    }

    //Moves backward (Wildlife -> Food)
    public void GoBackToFood()
    {
        fourthPanel.SetActive(false);
        thirdPanel.SetActive(true);
    }
}