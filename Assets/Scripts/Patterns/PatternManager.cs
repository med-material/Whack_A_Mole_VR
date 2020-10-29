using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Events;


[System.Serializable]
public class PatternUpdateEvent : UnityEvent<string> {}

/*
Class managing the selected pattern. Stores it, and does the interface between the rest of the game and the
PatternReadWriter, PatternParser
*/

public class PatternManager : MonoBehaviour
{
    private string loadedPatternName = "";
    private PatternReadWriter patternReadWriter;
    private PatternParser patternParser;
    private PatternPlayer patternPlayer;
    private LoggerNotifier loggerNotifier;
    private PatternUpdateEvent patternUpdateEvent = new PatternUpdateEvent();

    [System.Serializable]
    public struct InternalPattern {
        public string name;
        public TextAsset asset;
    }

    [SerializeField]
    private InternalPattern[] internalPatterns;

    private Dictionary<string, string[]> internalPatternsDict = new Dictionary<string, string[]>();
    
    void Awake()
    {
        patternReadWriter = new PatternReadWriter();
        patternParser = new PatternParser();
        patternPlayer = FindObjectOfType<PatternPlayer>();
    }

    void Start()
    {
        loggerNotifier = new LoggerNotifier(persistentEventsHeadersDefaults: new Dictionary<string, string>(){
            {"PlayedPattern", "None"}
        });

        foreach(InternalPattern pattern in internalPatterns) {
            internalPatternsDict.Add(pattern.name, pattern.asset.text.Split('\n'));
        }

    }

    // Plays the pattern if one is loaded.
    public bool PlayPattern()
    {
        if (!PatternLoaded())
        {
            Debug.LogError("No pattern loaded!");
            loggerNotifier.NotifyLogger(overrideEventParameters: new Dictionary<string, object>(){
                {"PlayedPattern", "None"}
            });
            return false;
        }
        patternPlayer.PlayPattern();
        loggerNotifier.NotifyLogger(overrideEventParameters: new Dictionary<string, object>(){
                {"PlayedPattern", loadedPatternName}
            });
        return true;
    }

    // Stops the pattern playing.
    public void StopPattern()
    {
        if (patternPlayer.IsPlaying()) patternPlayer.StopPatternPlay();
    }

    // Pauses the pattern playing.
    public void PauseUnpausePattern(bool pause)
    {
        patternPlayer.PauseUnpausePattern(pause);
    }

    // Returns if a pattern is currently playing or not.
    public bool IsPlayingPattern()
    {
        return patternPlayer.IsPlaying();
    }

    public UnityEvent<string> GetPatternUpdateEvent()
    {
        return patternUpdateEvent;
    }
    
    // Returns if a pattern is loaded or not.
    public bool PatternLoaded()
    {
        if (loadedPatternName == "") return false;
        return true;
    }

    // Returns the play duration of the pattern.
    public float GetPatternDuration()
    {
        return patternPlayer.GetPatternDuration();
    }

    // Loads a pattern from a given name.
    public bool LoadPattern(string patternName)
    {
        string[] patternProperties = null;

        if (internalPatternsDict.ContainsKey(patternName)) {
            patternProperties = internalPatternsDict[patternName];
        } else {
            patternProperties = patternReadWriter.LoadPattern(patternName);
        }

        if (patternProperties == null) return false;

        if (patternProperties.Length == 0)
        {
            Debug.LogError("Pattern " + patternName + " is empty. Pattern is ignored.");
            return false;
        }

        loadedPatternName = patternName;

        patternPlayer.SetPattern(patternParser.ParsePattern(patternProperties));
        patternUpdateEvent.Invoke(patternName);
        return true;
    }

    // Unload the loaded pattern to get back to a state where no pattern is loaded.
    public void ClearPattern()
    {
        if (!PatternLoaded()) return;
        loadedPatternName = "";
        patternPlayer.ClearPattern();
    }

    // Returns the name of the available patterns
    public List<string> GetPatternsName()
    {
        List<string> patternList = new List<string>();

        foreach(KeyValuePair<string, string[]> pair in internalPatternsDict) {
            patternList.Add(pair.Key);
        }

        patternList.AddRange(patternReadWriter.GetPatternsName());

        return patternList;
    }

    // Returns the name of the currently loaded pattern
    public string GetLoadedPatternName()
    {
        return loadedPatternName;
    }
}
