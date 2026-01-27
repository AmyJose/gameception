using UnityEngine;

public class PanelToggler : MonoBehaviour
{
    public GameObject panel;

    // This MUST have the word 'public' at the start!
    public void TogglePanel() 
    {
        panel.SetActive(!panel.activeSelf);
    }
}