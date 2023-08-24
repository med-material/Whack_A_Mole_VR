using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

/*
Class dedicated to play the pattern at runtime from the dictionary given by the pattern parser.
Calls the PatternInterface to call the different elements and translate the dictionary into concrete actions;
*/

public class PatternPlayer: MonoBehaviour
{
    private Dictionary<float, List<Dictionary<string, string>>> pattern = new Dictionary<float, List<Dictionary<string, string>>>();
    private List<float> sortedKeys = new List<float>();
    private List<int> moleIdList = new List<int>();
    private float waitTimeLeft = 0f;
    private float waitForDuration = -1f;
    private int playIndex = 0;
    private bool isRunning = false;
    private bool isPaused = false;
    private PatternInterface patternInterface;
    private WallManager wallManager;
    private PatternParser patternParser;

    void Awake()
    {
        patternInterface = FindObjectOfType<PatternInterface>();
        wallManager = FindObjectOfType<WallManager>();
        patternParser = new PatternParser();
    }

    void Update()
    {
        if (patternParser.GetParadigm() == PatternParser.Paradigm.Progression)
        {
            ProgressionParadigm();
        }

        // Whether we are in a Progression or Time paradigm we always call the TimeParadigm,
        // because the pattern still needs to act normally if the moles are not hit and play the next step when a mole expires
        TimeParadigm();

    }

    // Plays the loaded pattern if one is actually loaded.
    public void PlayPattern()
    {
        if (isRunning) return;
        if (sortedKeys.Count == 0) return;
        isRunning = true;
        isPaused = false;

        if (sortedKeys[0] == 0f) PlayStep();
        else waitForDuration = sortedKeys[0];
    }

    // Stops the pattern play if it is currently playing.
    public void StopPatternPlay()
    {
        if (!isRunning) return;
        isRunning = false;
        ResetPlay();
    }

    // Pauses/unpauses the pattern play if it is currently playing.
    public void PauseUnpausePattern(bool pause)
    {
        if (isPaused == pause) return;
        isPaused = pause;
    }

    // Returns if it is currently playing a pattern or not.
    public bool IsPlaying()
    {
        return isRunning;
    }

    // Returns the duration of the currently loaded pattern.
    public float GetPatternDuration()
    {
        return sortedKeys.Max();
    }

    // Loads a pattern
    public void SetPattern(Dictionary<float, List<Dictionary<string, string>>> newPattern)
    {
        if (newPattern.Count == 0) return;
        pattern = newPattern;
        sortedKeys = pattern.Keys.ToList();
        sortedKeys.Sort();
    }

    // Unloads the loaded pattern.
    public void ClearPattern()
    {
        if (isRunning) StopPatternPlay();
        pattern.Clear();
        sortedKeys.Clear();
    }

    // Resets the state of pattern play (so it can be played again).
    private void ResetPlay()
    {
        waitForDuration = -1f;
        playIndex = 0;
        waitTimeLeft = 0f;
        // Clear any moles remaining in the MolesIdList.
        patternInterface.ClearMolesList();
    }

    // Returns the time to wait before playing the next action when the loaded pattern is playing.
    private float GetWaitTime(int index)
    {
        if (index >= sortedKeys.Count - 1) return 0f;
        else return sortedKeys[index + 1] - sortedKeys[index];
    }

    // Plays a step from the pattern and triggers the wait to play the next step (if the current step isn't the last).
    private void PlayStep()
    {
        // Increase the Spawn Order of the moles at each step, logged for analysis purposes.
        wallManager.SetSpawnOrder(playIndex);

        // Clear any moles remaining in the MolesIdList.
        patternInterface.ClearMolesList();

        int currentLineNumber = 1;
        foreach (Dictionary<string, string> action in pattern[sortedKeys[playIndex]])
        {
            currentLineNumber++;
            patternInterface.PlayAction(new Dictionary<string, string>(action), currentLineNumber);
        }


        if (playIndex < sortedKeys.Count - 1)
        {
            playIndex++;
            waitForDuration = GetWaitTime(playIndex - 1);
        }
    }

    //Time Paradigm : Create each moles with their respective Lifetime
    public void TimeParadigm()
    {
        // Check whether we need to wait.
        if (waitForDuration == -1f) return;

        // if our wait time is 0, we initialize it.
        if (waitTimeLeft == 0f) waitTimeLeft = waitForDuration;

        // if wait time is above 0, we wait.
        // if wait time is below 0, wait time is over.
        if (waitTimeLeft > 0)
        {
            if (!isPaused) waitTimeLeft -= Time.deltaTime;
        }
        else
        {
            waitForDuration = -1f;
            waitTimeLeft = 0f;
            PlayStep();
        }
    }

    //Progression Paradigm : make the next moles appear immediately after all the current moles have been hit
    public void ProgressionParadigm()
    {
        // Check whether we need to wait.
        if (waitForDuration == -1f) return;

        // If the targetsList hasnt been initialized yet, return.
        if (patternInterface.GetTargetsList() == null) return;

        // If there are still moles to be shot, return.
        // If there are no more moles left, continue.
        if (patternInterface.GetTargetsList().Count > 0) {
            // Clear disabled and popped moles from targetsList
            //foreach (var mole in patternInterface.GetTargetsList().Where(mole => mole.Value.GetState() != Mole.States.Enabled))
            //{
            //    patternInterface.RemoveFromTargetsList(mole.Value.GetId());
            //}

            var molesToRemove = new List<int>(); // assuming the id is of type int

            foreach (var mole in patternInterface.GetTargetsList())
            {
                if (mole.Value.GetState() != Mole.States.Enabled)
                {
                    molesToRemove.Add(mole.Value.GetId());
                }
            }

            foreach (var moleId in molesToRemove)
            {
                patternInterface.RemoveFromTargetsList(moleId);
            }

            return;
        }

        var molesList = patternInterface.GetMolesList();

        // Clear all distractors.
        foreach (var fake in molesList.Where(fake => fake.Value.IsFake() && fake.Value.GetState() == Mole.States.Enabled)) {
            fake.Value.Disable();
        }

        foreach (Dictionary<string, string> action in pattern[sortedKeys[playIndex]])
        {
            // If the next action contains a message, wait until it
            // dissapears.
            // Otherwise, if the next action contains a mole, play the next step.
            // Otherwise, wait.
            //Debug.Log(action["FUNCTION"]);
            if (action["FUNCTION"] == "MESSAGE") {
                waitForDuration = GetWaitTime(playIndex);
                patternInterface.ResetTargetsList();
                return;
            } else if (action["FUNCTION"] == "MOLE") {
                waitForDuration = -1f;
                waitTimeLeft = 0f;
                continue;
            }
        }
        PlayStep();
    
        // Check if we are at the last Index of the Pattern and if all the moles have been hit or has expired
        if ((playIndex == sortedKeys.Count) || (playIndex == sortedKeys.Count - 1))
        {
            // Stop the Game
            patternInterface.CallStop();
        }
    }
}
