using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Main class of the therapist Ui. Controls the overall behavior of the UI and is used as an interface betweel the UI and the rest of the game.
*/

public enum PanelChoice {
    TherapistPanel,
    ModifiersPanel,
    ProfilePanel
}

public class TherapistUi : MonoBehaviour
{
    [SerializeField]
    private ModifiersManager modifiersManager;

    [SerializeField]
    private GameDirector gameDirector;

    [SerializeField]
    private TherapistPanelController therapistPanelController;

    [SerializeField]
    private MinimizedPanelController minimizedPanelController;

    [SerializeField]
    private WarningPanel warningPanel;

    [SerializeField]
    private PlayerPanel playerPanel;

    [SerializeField]
    private PatternManager patternManager;

    [SerializeField]
    private ApplicationManager applicationManager;

    [SerializeField]
    private WallManager wallManager;

    private GameDirector.GameState currentGameState = GameDirector.GameState.Stopped;
    private LoggerNotifier loggerNotifier;
    private Animation animationPlayer;
    

    // Temporary implementation
    private string profileName;

    void Start()
    {
        animationPlayer = gameObject.GetComponent<Animation>();
        UpdatePatternDropDown();

        // Connect to the modifier updated event from the ModifiersManager
        //modifiersManager.GetModifierUpdateEvent().AddListener(ModifierUpdated);
        // Connect to the speed updated event from the GameDirector
        gameDirector.GetDifficultyUpdateEvent().AddListener(ModifierUpdated);

        // Initialization of the LoggerNotifier. Here we will only raise PersistentEvent.
        loggerNotifier = new LoggerNotifier(persistentEventsHeadersDefaults: new Dictionary<string, string>(){
            {"ParticipantId", "NULL"},
            {"TestId", "NULL"}
        });
        // Initialization of the starting values of the parameters.
        loggerNotifier.InitPersistentEventParameters(new Dictionary<string, object>(){
            {"ParticipantId", 0},
            {"TestId", 0}
        });
    }

    public void UpdateDeviceWarningDisplay(bool display)
    {
        warningPanel.UpdateWarningDisplay(display);
    }

    // When the exit button is pressed, close the application
    public void ExitApplication()
    {
        applicationManager.CloseGame();
    }

    // When start game event is raised, nofifies the gameDirector.
    public void StartGame()
    {
        gameDirector.CountDownGame();
    }

    // When stop game event is raised, nofifies the gameDirector.
    public void StopGame()
    {
        gameDirector.StopGame();
    }

    // When pause game event is raised, nofifies the gameDirector.
    public void PauseGame()
    {
        gameDirector.PauseUnpauseGame();
    }

    // If game state updated event is raised (by the gameDirector), updates the UI accordingly.
    public void OnGameDirectorStateUpdate(GameDirector.GameState newState)
    {
        switch(newState)
        {
            case GameDirector.GameState.CountDown:
                GameCountDown();
                break;
            case GameDirector.GameState.Stopped:
                GameStopped();
                break;
            case GameDirector.GameState.Playing:
                if(currentGameState == GameDirector.GameState.Paused)
                {
                    GamePausedUnpaused(false);
                }
                else
                {
                    GameStarted();
                }
                break;
            case GameDirector.GameState.Paused:
                GamePausedUnpaused();
                break;
        }
        currentGameState = newState;
    }

    // When game data updated event is raised, notifies the gameDirector and updates the UI.
    public void UpdateGameDatas(Dictionary<string, object> data)
    {
        foreach(KeyValuePair<string, object> entry in data)
        {
            switch(entry.Key)
            {
                case "GameDuration":
                    gameDirector.SetGameDuration(float.Parse((string)entry.Value) * 60f);
                    break;
                default:
                    // Raise an event.
                    loggerNotifier.NotifyLogger(overrideEventParameters: new Dictionary<string, object>()
                    {
                        {entry.Key, entry.Value}
                    });
                    break;
            }
        }
        minimizedPanelController.UpdateDisplayedInfos(data);
        playerPanel.UpdateDisplayedInfos(data);
    }

    // When the game modifier updated event is raised, notifies the modifiersManager.
    public void SetGameModifier(Dictionary<string, object> data)
    {
        foreach(KeyValuePair<string, object> entry in data)
        {
            switch(entry.Key)
            {
                case "MirrorEffect":
                    modifiersManager.SetMirrorEffect(bool.Parse((string)entry.Value));
                    break;
                case "PhysicalMirror":
                    modifiersManager.SetPhysicalMirror(bool.Parse((string)entry.Value));
                    break;
                case "GeometricMirror":
                    modifiersManager.SetGeometricMirror(bool.Parse((string)entry.Value));
                    break;
                case "ShowHand":
                    break;
                case "DualTask":
                    modifiersManager.SetDualTask(bool.Parse((string)entry.Value));
                    break;
                case "EyePatch":
                    ModifiersManager.EyePatch value = ModifiersManager.EyePatch.None;
                    switch(entry.Value)
                    {
                        case "Left":
                            value = ModifiersManager.EyePatch.Left;
                            break;
                        case "None":
                            value = ModifiersManager.EyePatch.None;
                            break;
                        case "Right":
                            value = ModifiersManager.EyePatch.Right;
                            break;
                    }
                    modifiersManager.SetEyePatch(value);
                    break;
                case "HideWall":
                    ModifiersManager.HideWall wallValue = ModifiersManager.HideWall.None;
                    switch(entry.Value)
                    {
                        case "Left":
                            wallValue = ModifiersManager.HideWall.Left;
                            break;
                        case "None":
                            wallValue = ModifiersManager.HideWall.None;
                            break;
                        case "Right":
                            wallValue = ModifiersManager.HideWall.Right;
                            break;
                    }
                    modifiersManager.SetHideWall(wallValue);
                    break;
                case "ControllerOffset":
                    modifiersManager.SetControllerOffset(float.Parse((string)entry.Value));
                    break;
                case "PrismOffset":
                    modifiersManager.SetPrismOffset(float.Parse((string)entry.Value));
                    break;
                case "GameSpeed":
                    gameDirector.SetDifficulty((string)entry.Value);
                    break;
            }
        }
    }

    // Updates the values of the pattern dropdown
    public void UpdatePatternDropDown()
    {
        therapistPanelController.UpdatePatternDropDown(patternManager.GetPatternsName());
    }

    // When a pattern is selected on the dropdown
    public void DropDownPatternSelected(string patternName)
    {
        if (patternName == "Random Moles")
        {
            patternManager.ClearPattern();
            wallManager.Clear();
            wallManager.SetDefaultWall();
            return;
        }
        patternManager.LoadPattern(patternName);
    }

    // When the game time updated event is raised (by the game director), updates the UI.
    public void GameTimeUpdate(float time)
    {
        therapistPanelController.GameTimeUpdate(time);
        minimizedPanelController.GameTimeUpdate(time);
    }

    // Switches between the therapist panel and the profile panel.
    public void SwitchPanel(PanelChoice choice, string name = null)
    {
        if (name != null) profileName = name;
        if (choice == PanelChoice.TherapistPanel) {
         animationPlayer.Play("ToTherapist");
        } else if (choice == PanelChoice.ProfilePanel) {
         animationPlayer.Play("ToProfile");
        } else if (choice == PanelChoice.ModifiersPanel) {
            animationPlayer.Play("ToModifier");
        }
    }

    // Updates the TherapistPanel profile name. Called by the transition animation.
    public void UpdateTherapistProfileName()
    {
        therapistPanelController.UpdateProfileName(profileName);
    }

    // When the game CountDown event is raised (by the game director), updates the UI.
    private void GameCountDown()
    {
        therapistPanelController.GameCountDown();
        minimizedPanelController.GameStart();
        playerPanel.SetPausedContainer(false);
        playerPanel.SetCountDownContainer(true);
        playerPanel.SetEnablePanel(true);
    }

    // When the game started event is raised (by the game director), updates the UI.
    private void GameStarted()
    {
        therapistPanelController.GameStart();
        minimizedPanelController.GameStart();
        playerPanel.SetEnablePanel(false);
    }

    // When the game stopped event is raised (by the game director), updates the UI.
    private void GameStopped()
    {
        therapistPanelController.GameStop();
        minimizedPanelController.GameStop();
        playerPanel.SetPausedContainer(false);
        playerPanel.SetCountDownContainer(false);
        playerPanel.SetEnablePanel();
    }

    // When the game paused event is raised (by the game director), updates the UI.
    private void GamePausedUnpaused(bool pause = true)
    {
        therapistPanelController.GamePause(pause);
        minimizedPanelController.GamePause(pause);
        playerPanel.SetCountDownContainer(false);
        playerPanel.SetPausedContainer(pause);
        playerPanel.SetEnablePanel(pause);
    }

    private void ModifierUpdated(string identifier, string value)
    {
        therapistPanelController.UpdateSelectedModifier(identifier, value);
    }
}
