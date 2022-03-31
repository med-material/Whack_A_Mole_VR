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
    private GameObject countdownContainer;

    [SerializeField]
    private GameObject startContainer;

    [SerializeField]
    private GameObject runningContainer;

    [SerializeField]
    private GameObject dataSaveContainer;

    [SerializeField]
    private GameObject uploadingContainer;

    [SerializeField]
    private Text gameStateText;

    [SerializeField]
    private Text gameTimeText;

    [SerializeField]
    private Text countDownText;
    private string countDownTextTemplate;

    [SerializeField]
    private Button pauseGameButton;

    [SerializeField]
    private LoggingManager loggingManager;

    void Awake() {
        countDownTextTemplate = countDownText.text;
    }

    public void OnCountDownGame()
    {
        gameStateText.text = "Preparing the game..";
        startContainer.SetActive(false);
        runningContainer.SetActive(false);
        countdownContainer.SetActive(true);
        dataSaveContainer.SetActive(false);
    }

    // When the game starts, updates the text and shows the runningContainer (containing the chronometer, pause and stop buttons).
    public void OnStartGame()
    {
        gameStateText.text = "Game is running.";
        startContainer.SetActive(false);
        countdownContainer.SetActive(false);
        runningContainer.SetActive(true);
        dataSaveContainer.SetActive(false);
    }

    // When the game stops, updates the text and hides the runningContainer and shows the start container (containing the start game button).
    public void OnStopGame()
    {
        gameStateText.text = "Keep this session?";
        runningContainer.SetActive(false);
        countdownContainer.SetActive(false);
        startContainer.SetActive(false);
        dataSaveContainer.SetActive(true);
    }

    public void OnUploadData()
    {
        startContainer.SetActive(false);
        countdownContainer.SetActive(false);
        runningContainer.SetActive(false);
        dataSaveContainer.SetActive(false);
        uploadingContainer.SetActive(true);
        gameStateText.text = "Uploading Data..";
        loggingManager.SaveAllLogs(clear:true);
        loggingManager.NewFilestamp();
    }

    public void OnDiscardData()
    {
        startContainer.SetActive(false);
        countdownContainer.SetActive(false);
        runningContainer.SetActive(false);
        dataSaveContainer.SetActive(false);
        uploadingContainer.SetActive(true);
        gameStateText.text = "Hold on..";
        loggingManager.SaveAllLogs(targetType:TargetType.CSV, clear:true);
        loggingManager.NewFilestamp();
        Reset();
    }

    public void Reset() {
        gameStateText.text = "Game is ready.";
        startContainer.SetActive(true);
        countdownContainer.SetActive(false);
        runningContainer.SetActive(false);
        dataSaveContainer.SetActive(false);
        uploadingContainer.SetActive(false);
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

    public void UpdateCountDown(int count) {
        countDownText.text = string.Format(countDownTextTemplate, count.ToString());
    }
}
