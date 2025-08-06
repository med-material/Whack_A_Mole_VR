using System.Collections.Generic;
using UnityEngine;

/*
Class managing the profiles scroll view (containing the profile buttons ProfileButtonContainer).
*/

public class ProfileScrollViewManager : MonoBehaviour
{
    [SerializeField]
    private float buttonHeight = 35f;

    [SerializeField]
    private ProfileButtonContainer buttonPrefab;

    [SerializeField]
    private GameObject buttonsContainer;

    private ProfilePanelManager panelManager;

    void Awake()
    {
        panelManager = FindObjectOfType<ProfilePanelManager>();

    }
    // Adds a profile button with the given name and ID.
    public void AddProfile(string name, string id)
    {
        ProfileButtonContainer newButton = Instantiate(buttonPrefab, buttonsContainer.transform);
        newButton.SetButton(id, name, buttonHeight, panelManager);
        buttonsContainer.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, buttonHeight * buttonsContainer.transform.childCount);
    }

    // Regenerates the displayed profile buttons.
    public void RefreshDisplayedProfiles(List<Dictionary<string, string>> profilesNameIdList)
    {
        List<GameObject> children = new List<GameObject>();

        for (int i = 0; i < buttonsContainer.transform.childCount; i++)
        {
            children.Add(buttonsContainer.transform.GetChild(i).gameObject);
        }

        foreach (GameObject child in children)
        {
            Destroy(child);
        }

        foreach (Dictionary<string, string> profileNameId in profilesNameIdList)
        {
            ProfileButtonContainer newButton = Instantiate(buttonPrefab, buttonsContainer.transform);
            newButton.SetButton(profileNameId["Id"], profileNameId["Name"], buttonHeight, panelManager);
        }

        buttonsContainer.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, buttonHeight * profilesNameIdList.Count);
    }

    // Switches the active profile buttons back and forth between the Select mode and the Delete mode.
    public void SwitchButtonsToDeleteMode(bool deleteMode)
    {
        foreach (ProfileButtonContainer button in gameObject.GetComponentsInChildren<ProfileButtonContainer>())
        {
            button.SwitchToDeleteMode(deleteMode);
        }
    }
}
