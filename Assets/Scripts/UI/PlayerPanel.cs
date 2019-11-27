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
    private Text timeText;

    [SerializeField]
    private Text participantText;

    [SerializeField]
    private Text testText;

    private Canvas panelCanvas;


    void Start()
    {
        panelCanvas = gameObject.GetComponentInChildren<Canvas>();
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
}
