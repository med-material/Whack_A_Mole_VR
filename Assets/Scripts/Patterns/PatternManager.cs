using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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

    // TO-DO: ADD PATTERN DISPLAY AND SELECTION IN EDITOR
    // TEMPORARY: the pattern can be selected by entering its name in the editor

    [SerializeField]
    private string desiredPatternName = "";

    private bool initDone = false;
    
    void Awake()
    {
        patternReadWriter = new PatternReadWriter();
        patternParser = new PatternParser();
        patternPlayer = FindObjectOfType<PatternPlayer>();

        // TEMPORARY
        if (desiredPatternName != "")
        {
            LoadPattern(desiredPatternName);
        }

        initDone = true;
    }

    // TEMPORARY
    void OnValidate()
    {
        if (!EditorApplication.isPlaying) return;
        if (!initDone) return;
        if (desiredPatternName == "")
        {
            ClearPattern();
            return;
        }
        if (!LoadPattern(desiredPatternName)) loadedPatternName = "";
    }

    // Plays the pattern if one is loaded.
    public bool PlayPattern()
    {
        if (!PatternLoaded())
        {
            Debug.LogError("No pattern loaded!");
            return false;
        }
        patternPlayer.PlayPattern();
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
        string[] patternProperties = patternReadWriter.LoadPattern(patternName);

        if (patternProperties == null) return false;

        if (patternProperties.Length == 0)
        {
            Debug.LogError("Pattern " + patternName + " is empty. Pattern is ignored.");
            return false;
        }

        loadedPatternName = patternName;

        patternPlayer.SetPattern(patternParser.ParsePattern(patternProperties));
        return true;
    }

    // Unload the loaded pattern to get back to a state where no pattern is loaded.
    public void ClearPattern()
    {
        if (loadedPatternName == "") return;
        loadedPatternName = "";
        patternPlayer.ClearPattern();
    }
}
