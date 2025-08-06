using System.Collections.Generic;
using System.IO;
using UnityEngine;

/*
Class dedicated to load the pattern files. Caould be used to also save custom patterns created in-game
in the future.
*/

public class PatternReadWriter
{
    private string saveDirectory;
    private string extensionName = "wampat";


    public PatternReadWriter()
    {
        saveDirectory = Application.persistentDataPath + "/TestPatterns/";
        if (!Directory.Exists(saveDirectory)) Directory.CreateDirectory(saveDirectory);
    }

    // Tries to load a pattern from a given file name (without extension)
    public string[] LoadPattern(string patternName)
    {
        if (!File.Exists(saveDirectory + patternName + "." + extensionName))
        {
            Debug.LogError("The selected pattern file: " + patternName + "." + extensionName + " can't be found!");
            return null;
        }

        StreamReader reader = new StreamReader(saveDirectory + patternName + "." + extensionName);
        string[] lines = reader.ReadToEnd().Split("\n"[0]);

        return lines;
    }

    // Returns the name of all pattern files found
    public List<string> GetPatternsName()
    {
        List<string> foundPatternsName = new List<string>();
        DirectoryInfo info = new DirectoryInfo(saveDirectory);

        foreach (FileInfo file in info.GetFiles("*." + extensionName))
        {
            foundPatternsName.Add(file.Name.Replace(file.Extension, ""));
        }
        return foundPatternsName;
    }
}
