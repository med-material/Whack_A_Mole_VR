using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Class managing the profile selection and edition. Contains the selected profile, adds profiles for deletion...
Does the transition between the ProfilesReadWriter and the ProfilePanelManager.
*/

public class ProfileManager : MonoBehaviour
{
    private string selectedProfileId = "";
    private Dictionary<string, string> selectedProfileProperties;
    private ProfilesReadWriter readWriter;
    private List<string> deletionBuffer = new List<string>();

    void Awake()
    {
        readWriter = new ProfilesReadWriter();
        selectedProfileProperties = new Dictionary<string, string>();
    }

    // Returns a list containing the name and ID of every existing profile. Used to generate the profile button on the UI.
    public List<Dictionary<string, string>> GetAllProfilesNameId()
    {
        List<Dictionary<string, string>> returnList = new List<Dictionary<string, string>>();
        foreach(KeyValuePair<string, Dictionary<string, string>> pair in readWriter.GetAllProfiles())
        {
            returnList.Add(new Dictionary<string, string>(){
                {"Id", pair.Key},
                {"Name", pair.Value["Name"]}
            });
        }
        return returnList;
    }

    // Selects a profile from its ID.
    public void SelectProfile(string profileId)
    {
        if (profileId == selectedProfileId) return;
        selectedProfileId = profileId;
        selectedProfileProperties = readWriter.GetProfile(profileId);
    }

    // Creates a new profile from its name, mail and properties.
    public string CreateProfile(string name, string mail, Dictionary<string, string> properties)
    {
        return(readWriter.CreateProfile(name, mail, properties));
    }

    // Adds a profile to the deletion queue.
    public bool AddProfileForDeletion(string id)
    {
        if (!readWriter.HasProfile(id)) return false;
        deletionBuffer.Add(id);
        return true;
    }

    // Removes a profile from the deletion queue.
    public bool RemoveProfileFromDeletion(string id)
    {
        if (!deletionBuffer.Contains(id)) return false;
        deletionBuffer.Remove(id);
        return true;
    }

    // Tells the ProfileReadWriter to delete all profiles that are in the deletion queue.
    public void ConfirmProfileDeletion()
    {
        DeleteProfilesFromDeletionBuffer();
    }

    // Returns the properties of the currently selected profile.
    public Dictionary<string, string> GetSelectedProfileProperties()
    {
        if (selectedProfileId == "") return null;
        return selectedProfileProperties;
    }

    // Deletes from the disk all profiles present in the deletion buffer.
    private void DeleteProfilesFromDeletionBuffer()
    {
        foreach(string profile in deletionBuffer)
        {
            readWriter.DeleteProfile(profile);
        }
        ClearDeletionBuffer();
    }

    // Clears the deletion buffer.
    private void ClearDeletionBuffer()
    {
        deletionBuffer.Clear();
    }
}
