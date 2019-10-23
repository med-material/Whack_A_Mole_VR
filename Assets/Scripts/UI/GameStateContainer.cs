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

    [SerializeField]
    private Text gameTimeText;

    [SerializeField]
    private Button pauseGameButton;


    // When the game starts, updates the text and shows the runningContainer (containing the chronometer, pause and stop buttons).
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

    // When the game pauses, darkens the pause button
    public void OnPauseGame(bool pause)
    {
        if (pause)
        {
            gameStateText.text = "Game is paused.";
            ColorBlock newColor = pauseGameButton.colors;
            newColor.colorMultiplier = 0.5f;
            pauseGameButton.colors = newColor;
        }
        else
        {
            gameStateText.text = "Game is running.";
            ColorBlock newColor = pauseGameButton.colors;
            newColor.colorMultiplier = 1f;
            pauseGameButton.colors = newColor;
        }
    }

    // Updates the time displayed by the chronometer. Sets it from seconds to a mm:ss.ddd format
    public void UpdateTime(float time)
    {
        string value = "";

        value += Mathf.FloorToInt(time / 60f);
        value += ":";
        value += (time % 60f).ToString("F3");
        gameTimeText.text = value;
    }
}
