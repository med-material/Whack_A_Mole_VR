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


    // When the game starts, updates the "state sprite" to the "playing" icon.
    public void GameStart()
    {
        gameStateSprite.sprite = playSprite;
    }

    // When the game stops, updates the "state sprite" to the "stopped" icon.
    public void GameStop()
    {
        gameStateSprite.sprite = stopSprite;
    }

    // When the game is paused, updates the "state sprite" to the "paused" icon.
    public void GamePause()
    {
        gameStateSprite.sprite = pauseSprite;
    }

    // Updates displayed information such as the participant Id and the test Id
    public void UpdateDisplayedInfos(Dictionary<string, object> data)
    {
        foreach(KeyValuePair<string, object> entry in data)
        {
            switch(entry.Key)
            {
                case "participant":
                    participantText.text = "Participant " + entry.Value.ToString();
                    break;

                case "test":
                    testIdText.text = "Test " + entry.Value.ToString();
                    break;
            }
        }
    }
}
