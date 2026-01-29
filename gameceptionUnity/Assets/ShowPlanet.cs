using UnityEngine;
using UnityEngine.InputSystem;

public class ShowPlanet : MonoBehaviour
{
    public GameObject planet;
    public GameObject panelToggle; //old(first) conversation panel to hide
    public GameObject newConversationPanel; //new(second) conversation panel to show

    void Start()
    {
        planet.SetActive(false);
        panelToggle.SetActive(true); //old panel is visible at start
        newConversationPanel.SetActive(false); //new panel is hidden at the start
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {   
            planet.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            planet.SetActive(true);
            panelToggle.SetActive(false); //old panel is hidden when new panel is shown
            newConversationPanel.SetActive(true); //show new panel
        }
    }
}
