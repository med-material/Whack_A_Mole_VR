using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
Class dedicated to the maximized therapist panel. Updates the different UI components when needed.
*/

public class TherapistPanelController : MonoBehaviour
{
    [SerializeField]
    private GameObject maximizedContainer;

    [SerializeField]
    private GameObject minimizedContainer;

    [SerializeField]
    private GameObject buttonTextContainer;

    [SerializeField]
    private GameStateContainer gameStateContainer;

    [SerializeField]
    private Text profileNameText;

    private Animation animationPlayer;
    private LayoutElement layoutElement;
    private TherapistUi therapistUi;
    private List<ButtonTextController> buttonTextControllers = new List<ButtonTextController>();

    // On start, references the game data ButtonTextController (participant Id...)
    void Start()
    {
        animationPlayer = gameObject.GetComponent<Animation>();
        layoutElement = gameObject.GetComponent<LayoutElement>();
        therapistUi = FindObjectOfType<TherapistUi>();

        foreach(ButtonTextController controller in buttonTextContainer.GetComponentsInChildren<ButtonTextController>())
        {
            buttonTextControllers.Add(controller);
        }
    }

    // On start, disables the ButtonTextController and updates the GameStateContainer
    public void GameStart()
    {
        foreach(ButtonTextController controller in buttonTextControllers)
        {
            controller.Disable();
        }
        gameStateContainer.OnStartGame();
    }

    // On game stop, enables the ButtonTextController and updates the GameStateContainer
    public void GameStop()
    {
        foreach(ButtonTextController controller in buttonTextControllers)
        {
            controller.Enable();
        }
        gameStateContainer.OnPauseGame(false);
        gameStateContainer.OnStopGame();
    }

    // When the game pauses, updates the gameState container.
    public void GamePause(bool pause)
    {
        gameStateContainer.OnPauseGame(pause);
    }

    // When game time updated, updates the gameState container
    public void GameTimeUpdate(float time)
    {
        gameStateContainer.UpdateTime(time);
    }

    // Plays the collapsing animation of the maximized therapist Ui.
    public void Collapse()
    {
        animationPlayer.Play("PanelSlideDown");
    }

    // At the end of the collapsing animation, plays the extending animation of the minimized UI
    public void OnCollapseFinished()
    {
        maximizedContainer.SetActive(false);
        minimizedContainer.SetActive(true);
        animationPlayer.Play("MinimizedSlideUp");
    }

    // Plays the collapsing animation of the minimized therapist Ui.
    public void MinimizedCollapse()
    {
        animationPlayer.Play("MinimizedSlideDown");
    }

    // At the end of the collapsing animation, plays the extending animation of the maximized UI
    public void OnMinimizedCollapseFinished()
    {
        minimizedContainer.SetActive(false);
        maximizedContainer.SetActive(true);
        animationPlayer.Play("PanelSlideUp");
    }

    // Tells the TherapistUi to stop the game and switch to the ProfilePanel.
    public void SwitchToProfilePanel()
    {
        therapistUi.StopGame();
        therapistUi.SwitchPanel(false);
    }

    // Updates the displayed profile name. Calles by the TherapistUi.
    public void UpdateProfileName(string name)
    {
        profileNameText.text = name;
    }
}
