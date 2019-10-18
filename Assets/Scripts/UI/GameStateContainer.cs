using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
Class dedicated to the container of the game state (running, pause, stopped) in the TherapistUi. Updates the text describing the state of the game
and shows the corresponding buttons.
*/

public class GameStateContainer : MonoBehaviour
{
    [SerializeField]
    private GameObject startContainer;

    [SerializeField]
    private GameObject runningContainer;

    [SerializeField]
    private Text gameStateText;

    // When the game starts, updates the text, shows the runningContainer (container containing the chronometer, pause button and stop button) and hides the startContainer.
    public void OnStartGame()
    {
        gameStateText.text = "Game is running.";
        startContainer.SetActive(false);
        runningContainer.SetActive(true);
    }

    // When the game stops, updates the text and hides the runningContainer and shows the start container (containing the start game button).
    public void OnStopGame()
    {
        gameStateText.text = "Game is ready.";
        runningContainer.SetActive(false);
        startContainer.SetActive(true);
    }

    // When the game is paused, updates the text.
    public void OnPauseGame()
    {
        gameStateText.text = "Game is paused.";
    }
}
