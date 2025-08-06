using UnityEngine;

/*
Class managing the profile deletion button. Simply switches the button between the "delete profile" and "go back" modes.
The functions are called by the button itself.
*/

public class DeleteProfileButton : MonoBehaviour
{
    [SerializeField]
    private GameObject deleteButton;

    [SerializeField]
    private GameObject goBackButton;

    private ProfilePanelManager profilePanelManager;

    void Awake()
    {
        profilePanelManager = FindObjectOfType<ProfilePanelManager>();
    }

    public void SwitchToDeleteMode()
    {
        deleteButton.SetActive(false);
        goBackButton.SetActive(true);
        profilePanelManager.SwitchToDeleteMode(true);
    }

    public void SwitchToSelectMode()
    {
        deleteButton.SetActive(true);
        goBackButton.SetActive(false);
        profilePanelManager.SwitchToDeleteMode(false);
    }
}
