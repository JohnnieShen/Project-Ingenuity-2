using UnityEngine;

public class UIToggler : MonoBehaviour
{
    // This creates a slot in the Inspector to assign your Panel
    public GameObject panelToToggle;

    // This is the function the button will call
    public void TogglePanelVisibility()
    {
        if (panelToToggle != null)
        {
            // Check if the panel is currently active
            bool isCurrentlyActive = panelToToggle.activeSelf;
            
            // Set it to the opposite of its current state
            panelToToggle.SetActive(!isCurrentlyActive);
        }
        else
        {
            Debug.LogWarning("You forgot to assign the Panel to the UIToggler script!");
        }
    }
}