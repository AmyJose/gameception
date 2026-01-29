using UnityEngine;

public class PanelToggle : MonoBehaviour
{
    public GameObject firstPanel; 
    public GameObject secondPanel; 

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
}