using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
Class dedicated to the minimized container (panel shown when the therapist ui is minimized). Updates the displayed information when asked to.
*/

public class MinimizedPanelController : MonoBehaviour
{

    [SerializeField]
    private GameDirector gameDirector;

    [SerializeField]
    private Image gameStateSprite;

    [SerializeField]
    private Text gameStateText;

    [SerializeField]
    private Text participantText;

    [SerializeField]
    private Text testIdText;

    [SerializeField]
    private Sprite playSprite;

    [SerializeField]
    private Sprite pauseSprite;

    [SerializeField]
    private Sprite stopSprite;

    private bool gamePlaying = false;

    void OnEnable()
    {
        gameDirector.timeUpdate.AddListener(onTimeChanged);
        gameDirector.stateUpdate.AddListener(onStateChanged);
        gameDirector.participantChanged.AddListener(onParticipantChanged);
        gameDirector.testChanged.AddListener(onTestChanged);
        onTestChanged(gameDirector.GetTest());
        onParticipantChanged(gameDirector.GetParticipant());
    }

    public void onTimeChanged(float newTime) {
        GameTimeUpdate(newTime);
    }

    public void onStateChanged(GameDirector.GameState newState) {
        if (newState == GameDirector.GameState.Stopped) {
            GameStop();
        } else if (newState == GameDirector.GameState.Playing) {
            GameStart();
        } else if (newState == GameDirector.GameState.Paused) {
            GamePause();
        }
    }

    public void onParticipantChanged(int newParticipant) {
        participantText.text = "Participant " + newParticipant.ToString();
    }

    public void onTestChanged(int newTest) {
        testIdText.text = "Test " + newTest.ToString();
    }

    // When the game starts, updates the "state sprite" to the "playing" icon.
    public void GameStart()
    {
        gameStateSprite.sprite = playSprite;
        gamePlaying = true;
    }

    // When the game stops, updates the "state sprite" to the "stopped" icon.
    public void GameStop()
    {
        gameStateSprite.sprite = stopSprite;
        gameStateText.text = "Ready";
        gamePlaying = false;
    }

    // When the game pauses, updates the "state sprite" to the "paused" icon.
    public void GamePause()
    {
        gameStateSprite.sprite = pauseSprite;
        gameStateText.text = "Paused";
        gamePlaying = false;
    }

    // Updates the displayed time, sets it to a mm:ss format.
    public void GameTimeUpdate(float time)
    {
        if (!gamePlaying) return;
        string value = "";

        value += Mathf.FloorToInt(time / 60f).ToString("00");
        value += ":";
        value += Mathf.Floor(time % 60f).ToString("00");
        gameStateText.text = value;
    }
}
