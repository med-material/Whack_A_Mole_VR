using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
Class dedicated to the player panel. Updates the displayed information, hides/shows the panel and switches between the 
information panel and the "game paused" panel.
*/

public class PlayerPanel : MonoBehaviour
{
    [SerializeField]
    private GameObject infoContainer;

    [SerializeField]
    private GameObject gamePausedContainer;

    [SerializeField]
    private GameObject gameCountDownContainer;

    private SoundManager soundManager;
    private LoggerNotifier loggerNotifier;

    [SerializeField]
    private Text timeText;

    [SerializeField]
    private Text participantText;

    [SerializeField]
    private Text testText;

    [SerializeField]
    private Text countDownText;
    private string countDownTextTemplate;

    [SerializeField]
    private Text instructionText;
    private string instructionTextDefault;

    [SerializeField]
    private HighlightPerformance highlightPerformance;

    private Canvas panelCanvas;


    void Awake()
    {
        panelCanvas = gameObject.transform.GetChild(0).GetComponent<Canvas>();
        countDownTextTemplate = countDownText.text;
        instructionTextDefault = instructionText.text;
    }

    void Start()
    {
        loggerNotifier = new LoggerNotifier(persistentEventsHeadersDefaults: new Dictionary<string, string>(){
            {"GameMessage", "NULL"}
        });
        soundManager = FindObjectOfType<SoundManager>();
    }

    // Hides/shows the panel
    public void SetEnablePanel(bool enable = true)
    {
        panelCanvas.enabled = enable;
    }

    // Switches between the "game paused" panel and the information panel.
    public void SetPausedContainer(bool pausedContainer = true)
    {
        if (pausedContainer)
        {
            infoContainer.SetActive(false);
            gamePausedContainer.SetActive(true);
        }
        else
        {
            infoContainer.SetActive(true);
            gamePausedContainer.SetActive(false);
        }
    }

    // Switches between the "countdown" panel and the information panel.
    public void SetCountDownContainer(bool countDownContainer = true) {
        if (countDownContainer)
        {
            infoContainer.SetActive(false);
            gameCountDownContainer.SetActive(true);
        }
        else
        {
            infoContainer.SetActive(true);
            gameCountDownContainer.SetActive(false);
            SetInstructionText(instructionTextDefault);
        }
    }

    // Updates the participant Id, game duration and test Id displayed on the information panel.
    public void UpdateDisplayedInfos(Dictionary<string, object> data)
    {
        foreach(KeyValuePair<string, object> entry in data)
        {
            switch(entry.Key)
            {
                case "ParticipantId":
                    participantText.text = "Participant " + entry.Value.ToString();
                    break;

                case "TestId":
                    testText.text = "Test " + entry.Value.ToString();
                    break;
                
                case "GameDuration":
                    timeText.text = entry.Value.ToString() + " minutes";
                    break;
            }
        }
    }

    // Updates the countdown in the countdown panel.
    public void UpdateCountDownInfo(int count) {
        countDownText.text = string.Format(countDownTextTemplate, count.ToString());
    }

    public void SetInstructionText(string text) {
        instructionText.text = text;
    }    

    public void SetMessage(string text, float time) {
        SetInstructionText(text);
        highlightPerformance.ShowResults();
        StartCoroutine(WaitShowMessage(time, text));
    }

    private IEnumerator WaitShowMessage(float duration, string text) {
        float currentCountDownLeft = duration;
        int currentCountDownLeftRounded = -1;
        int prevCount = -1;
        SetEnablePanel(true);
        SetCountDownContainer(true);

        while (currentCountDownLeft > -0.9) {
            prevCount = currentCountDownLeftRounded;

            currentCountDownLeft -= Time.deltaTime;
            currentCountDownLeftRounded = (int) Mathf.Ceil(currentCountDownLeft);
            
            if (currentCountDownLeftRounded != prevCount) {
                var data = new Dictionary<string, object>()
                {
                    {"CountDown", currentCountDownLeftRounded}
                };
                UpdateCountDownInfo(currentCountDownLeftRounded);
                soundManager.PlaySound(gameObject, SoundManager.Sound.countdown);
                loggerNotifier.NotifyLogger("CountDown " + currentCountDownLeftRounded.ToString(), EventLogger.EventType.GameEvent, new Dictionary<string, object>()
                {
                    {"GameMessage", text}
                });

            }
            yield return null;
        }
        SetEnablePanel(false);
        SetCountDownContainer(false);
    }

}
