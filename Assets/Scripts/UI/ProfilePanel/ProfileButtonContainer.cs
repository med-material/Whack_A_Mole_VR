using UnityEngine;
using UnityEngine.UI;

/*
Class managing the profile button (which is a prefab). Sets its properties, bridges the button to the ProfilePanelManager
and changes the displayed button depending on the mode (select, delete, restore).
*/

public class ProfileButtonContainer : MonoBehaviour
{
    private enum ButtonStates { Select, Delete, Restore }

    [SerializeField]
    private GameObject SelectButton;

    [SerializeField]
    private GameObject DeleteButton;

    [SerializeField]
    private GameObject RestoreButton;

    [SerializeField]
    private Text SelectButtonText;

    [SerializeField]
    private Text DeleteButtonText;

    [SerializeField]
    private Text RestoreButtonText;

    private string profileId;
    private ProfilePanelManager profilePanelManager;
    private ButtonStates buttonState = ButtonStates.Select;
    private ButtonStates previousButtonState = ButtonStates.Select;

    // Initializes the button values
    public void SetButton(string id, string name, float buttonHeight, ProfilePanelManager panelManager)
    {
        profileId = id;
        SelectButtonText.text = name;
        DeleteButtonText.text = name;
        RestoreButtonText.text = name + " deleted";
        profilePanelManager = panelManager;
        gameObject.GetComponent<LayoutElement>().preferredHeight = buttonHeight;
    }

    // Restores the button's profile. Called by the restore button on press.
    public void RestoreProfile()
    {
        if (profilePanelManager.ProfileRestored(profileId)) ChangeButtonState(previousButtonState);
    }

    // Select the button's profile. Called by the select button on press.
    public void SelectProfile()
    {
        profilePanelManager.ProfileSelected(profileId);
    }

    // Switches back and forth between the delete button and the select button.
    public void SwitchToDeleteMode(bool deleteMode)
    {
        if (buttonState == ButtonStates.Restore)
        {
            if (deleteMode) previousButtonState = ButtonStates.Delete;
            else previousButtonState = ButtonStates.Select;
            return;
        }
        if (deleteMode) ChangeButtonState(ButtonStates.Delete);
        else ChangeButtonState(ButtonStates.Select);
    }

    // Delete the button's profile. Called by the delete button on press.
    public void RemoveProfile()
    {
        if (profilePanelManager.ProfileRemoved(profileId)) ChangeButtonState(ButtonStates.Restore);
    }

    // Changes the state of the button and updates its display accordingly (Select, Delete, Restore).
    private void ChangeButtonState(ButtonStates newState)
    {
        if (buttonState == newState) return;

        previousButtonState = buttonState;
        buttonState = newState;

        switch (buttonState)
        {
            case ButtonStates.Select:
                SelectButton.SetActive(true);
                DeleteButton.SetActive(false);
                RestoreButton.SetActive(false);
                break;
            case ButtonStates.Delete:
                SelectButton.SetActive(false);
                DeleteButton.SetActive(true);
                RestoreButton.SetActive(false);
                break;
            case ButtonStates.Restore:
                SelectButton.SetActive(false);
                DeleteButton.SetActive(false);
                RestoreButton.SetActive(true);
                break;
        }
    }
}
