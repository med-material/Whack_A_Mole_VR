using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;

/*
Class managing the profiles. Loads, saves, creates and check the profiles locally on the computer.
Profiles are stored as files in a *.ini style format in the AppData/LocalLow/[project]/UserProfiles folder.
*/

public class ProfilesReadWriter
{
    private Dictionary<string, Dictionary<string, string>> profiles;
    private string saveDirectory;
    private string extensionName = "uprofile";


    public ProfilesReadWriter()
    {
        saveDirectory = Application.persistentDataPath + "/UserProfiles/";
        if (!Directory.Exists(saveDirectory)) Directory.CreateDirectory(saveDirectory);
        profiles = LoadProfiles();
    }

    // Checks if the given profile exists.
    public bool HasProfile(string id)
    {
        return (profiles.ContainsKey(id));
    }

    // Returns all profiles with their properties.
    public Dictionary<string, Dictionary<string, string>> GetAllProfiles()
    {
        return profiles;
    }

    // Returns the properties of the profile corresponding to the given ID.
    public Dictionary<string, string> GetProfile(string id)
    {
        if (!profiles.ContainsKey(id)) return null;
        return profiles[id];
    }

    // Creates a profile on the disk from a given name, mail and set of properties.
    public string CreateProfile(string name, string mail, Dictionary<string, string> properties = null)
    {
        string newProfileID = Md5Sum(System.DateTime.Now.ToString("yyyyMMddHHmmssffff") + name + mail);

        if (profiles.ContainsKey(newProfileID)) return null;

        Dictionary<string, string> tempProperties = new Dictionary<string, string>()
        {
            {"Name", name},
            {"Mail", mail}
        };

        if (properties != null)
        {
            foreach (KeyValuePair<string, string> property in properties)
            {
                tempProperties.Add(property.Key, property.Value);
            }
        }

        StreamWriter writer;

        try
        {
            writer = new StreamWriter(saveDirectory + newProfileID + "." + extensionName);
        }
        catch
        {
            return null;
        }

        foreach (KeyValuePair<string, string> property in tempProperties)
        {
            writer.WriteLine(property.Key + ":" + property.Value);
        }

        writer.Close();
        profiles.Add(newProfileID, tempProperties);
        return newProfileID;
    }

    // Deletes from the disk the profile corresponding to the given ID.
    public bool DeleteProfile(string id)
    {
        if (!profiles.ContainsKey(id)) return false;
        if (EraseProfile(id)) profiles.Remove(id);

        return true;
    }

    // Erases a profile from the disk.
    private bool EraseProfile(string id)
    {
        try
        {
            File.Delete(saveDirectory + id + "." + extensionName);
            return true;
        }
        catch
        {
            return false;
        }
        
    }

    private Dictionary<string, Dictionary<string, string>> LoadProfiles()
    {
        Dictionary<string, Dictionary<string, string>> profiles = new Dictionary<string, Dictionary<string, string>>();
        DirectoryInfo info = new DirectoryInfo(saveDirectory);

        foreach (FileInfo file in info.GetFiles("*." + extensionName))
        {

            StreamReader reader = new StreamReader(file.FullName);
            string[] lines = reader.ReadToEnd().Split("\n"[0]);
            Dictionary<string, string> properties = new Dictionary<string, string>();

            for (int i = 0; i < lines.Length; i++)
                lines[i] = lines[i].Replace("\r", string.Empty);

            foreach (string line in lines)
            {
                if (line == "") break;

                string[] keyValue = line.Split(":"[0]);

                if (keyValue.Length != 2)
                {
                    Debug.LogError("Profile file " + file.Name + ": line '" + line + "' have an incorrect format. Line ignored.");
                    continue;
                }

                if (properties.ContainsKey(keyValue[0]))
                {
                    Debug.LogError("Profile file " + file.Name + ": line '" + line + "' seems to be a duplicate. Line ignored.");
                    continue;
                }

                properties.Add(keyValue[0], keyValue[1]);
            }

            profiles.Add(file.Name.Replace("." + extensionName, ""), properties);
        }
        return profiles;
    }

    private string Md5Sum(string strToEncrypt)
    {
        System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
        byte[] bytes = ue.GetBytes(strToEncrypt);

        // encrypt bytes
        System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
        byte[] hashBytes = md5.ComputeHash(bytes);

        // Convert the encrypted bytes back to a string (base 16)
        string hashString = "";

        for (int i = 0; i < hashBytes.Length; i++)
        {
            hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
        }

        return hashString.PadLeft(32, '0');
    }
}
