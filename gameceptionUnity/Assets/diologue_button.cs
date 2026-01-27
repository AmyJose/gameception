using UnityEngine;

public class PanelToggle : MonoBehaviour
{
    public GameObject dialoguePanel; // assign your dialogue panel here in Inspector

    // This function will be called when the button is clicked
    public void TogglePanel()
    {
        if (dialoguePanel != null)
        {
            // Switch the panel on/off
            dialoguePanel.SetActive(!dialoguePanel.activeSelf);
        }
    }
}
