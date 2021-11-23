using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

/*
Class dedicated to parse the loaded pattern file into a readable format for the PatternPlayer.
Translates the text file into a dictionary organized by the time of execution, containing a list of dictionaries,
each containing an action to do and its arguments.
Example: Dictionary[3.75, List[Dictionary["FUNCTION": "MOLE", "X":"1", "Y":"5", "LIFETIME":"1"], Dictionary["FUNCTION":"MODIFIER", "EYEPATCH":"LEFT"]]
--> this line means after 3.75 seconds, spawns a Mole at the coordinates x = 1 and y = 5 with a lifetime of 1s, and activates the eye patch on the left eye.
*/

public class PatternParser
{
    public enum Paradigm { Time }

    private static Paradigm paradigm;

    public Paradigm GetParadigm()
    {
        return paradigm;
    }

    private static int moleCount;

    public int GetMoleCount()
    {
        return moleCount;
    }

    private void SetMoleCount(int value)
    {
        moleCount = value;
    }

    // Parses the pattern file into a readable file for the PatternInterface.
    public Dictionary<float, List<Dictionary<string, string>>> ParsePattern(string[] patternStrings)
    {
        Dictionary<float, List<Dictionary<string, string>>> parsedPattern = new Dictionary<float, List<Dictionary<string, string>>>();
        float playTime = 0f;
        float moleDelay = 0f;
        SetMoleCount(0);

        paradigm = Paradigm.Time;

        // For each line in the file.
        foreach (string line in patternStrings)
        {
            string uncommentedLine = line;

            // Removes the comments. If the line is empty, ignores the line.
            if (line == "") continue;
            uncommentedLine = RemoveComments(line);
            if (uncommentedLine == "") continue;

            float tempPlayTime = playTime;

            // Splits the line to separate the property and its patameters.
            string[] keyValue = uncommentedLine.Replace(" ", "").Split(":"[0]);
            if (keyValue.Length != 2) continue;

            Dictionary<string, string> extractedProperties = ExtractProperty(keyValue[1]);

            // If property = "WAIT", adds duration to the play time and ignores the rest.
            if (keyValue[0] == "WAIT")
            {
                float waitTime = float.Parse(extractedProperties["TIME"], System.Globalization.CultureInfo.InvariantCulture);
                playTime += waitTime;
                continue;
            }
            else if (keyValue[0] == "MOLE")
            {
                moleCount++;
                SetMoleCount(moleCount);

                // If property = "MOLE", checks if it has the property STARTDELAY and if it does, takes it into account.
                if (extractedProperties.ContainsKey("STARTDELAY"))
                {
                    tempPlayTime += float.Parse(extractedProperties["STARTDELAY"], System.Globalization.CultureInfo.InvariantCulture);
                    extractedProperties.Remove("STARTDELAY");
                }

                moleDelay = tempPlayTime + float.Parse(extractedProperties["LIFETIME"], System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (keyValue[0] == "DISTRACTOR")
            {
                moleCount++;
                SetMoleCount(moleCount);
            }

            // Add the extracted property to the dictionary

            Dictionary<string, string> returnProperties = new Dictionary<string, string>(){{"FUNCTION",keyValue[0]}};

            foreach (KeyValuePair<string, string> property in extractedProperties)
            {
                returnProperties.Add(property.Key, property.Value);
            }
            
            if(parsedPattern.ContainsKey(tempPlayTime))
            {
                parsedPattern[tempPlayTime].Add(returnProperties);
            }
            else
            {
                parsedPattern.Add(tempPlayTime, new List<Dictionary<string, string>>(){returnProperties});
            }
        }

        // Adds extra time to the play time if last property = "WAIT" or the moleDelay (playtime + mole "LIFETIME") > playTime

        if (moleDelay > playTime)
        {
            playTime = moleDelay;
        }
        
        if (!parsedPattern.ContainsKey(playTime))
        {
            parsedPattern.Add(playTime, new List<Dictionary<string, string>>());
        }

        return parsedPattern;
    }

    // Extracts the properties from a string.
    private Dictionary<string, string> ExtractProperty(string propertiesString)
    {
        Dictionary<string, string> properties = new Dictionary<string, string>();

        MatchCollection matches = Regex.Matches(propertiesString, @"(?<=[\(),])([^),].*?)(?=[\),])");
        foreach (Match match in matches)
        {
            string[] parameterValue = match.ToString().Split('=');
            if (parameterValue.Length == 2)
            {
                properties.Add(parameterValue[0], parameterValue[1]);
            }
            else if (parameterValue.Length == 1)
            {
                properties.Add(parameterValue[0], "null");
            }
        }
        return properties;
    }

    // Removes the comments from the file.
    private string RemoveComments(string line)
    {
        return Regex.Replace(line, @"(\/\/.+)", "");
    }
}
