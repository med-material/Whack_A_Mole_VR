using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/*
Class doing the interface between the PatternPlayer and the different classes of the game it needs to interact with.
Reads and parses a dictionary of actions to do and calls the different game classes in consequence.
The dictionary has a mandatory "FUNCTION" key containing the action to do, then other keys corresponding to the arguments of the given action.
*/
public class PatternInterface : MonoBehaviour
{
    private WallManager wallManager;
    private ModifiersManager modifiersManager;
    private GameDirector gameDirector;
    private MotorSpaceManager motorspaceManager;
    private LoggerNotifier loggerNotifier;
    private PlayerPanel playerPanel;
    private ThemeManager themeManager;
    private TactorConnector tactorConnector;
    private float randVar = 0f;

    private Dictionary<int, Mole> targetsList = null;
    private Dictionary<int, Mole> molesList = new Dictionary<int, Mole>();

    public Dictionary<int, Mole> GetMolesList()
    {
        return molesList;
    }

    public void ClearMolesList()
    {
        molesList.Clear();
    }

    public Dictionary<int, Mole> GetTargetsList()
    {
        return targetsList;
    }

    public void RemoveFromTargetsList(int moleId)
    {
        //Debug.Log(targetsList.Count);
        targetsList.Remove(moleId);
    }

    public void ResetTargetsList()
    {
        targetsList = null;
    }

    public void AddToTargetsList(Mole mole)
    {
        if (targetsList == null)
        {
            targetsList = new Dictionary<int, Mole>();
        }
        targetsList[mole.GetId()] = mole;
        //Debug.Log(targetsList.Count);
    }

    public void ClearTargetsList()
    {
        targetsList.Clear();
        //Debug.Log(targetsList.Count);
    }

    void Awake()
    {
        wallManager = FindObjectOfType<WallManager>();
        modifiersManager = FindObjectOfType<ModifiersManager>();
        gameDirector = FindObjectOfType<GameDirector>();
        motorspaceManager = FindObjectOfType<MotorSpaceManager>();
        playerPanel = FindObjectOfType<PlayerPanel>();
        themeManager = FindObjectOfType<ThemeManager>();
        tactorConnector = FindObjectOfType<TactorConnector>();
    }

    void Start()
    {
        // Initialization of the LoggerNotifier. Here we will only pass parameters to PersistentEvent, even if we will also raise Events.
        loggerNotifier = new LoggerNotifier(persistentEventsHeadersDefaults: new Dictionary<string, string>(){
            {"PatternSegmentID", "NULL"},
            {"PatternSegmentLabel", "NULL"},
        });
    }

    // Plays an action
    public void PlayAction(Dictionary<string, string> action, int lineNumber = 0)
    {
        string[] keys = new string[action.Keys.Count];
        action.Keys.CopyTo(keys, 0);

        // If one of the argument is "RAND", replaces it with the random variable.
        foreach (string key in keys)
        {
            if (action[key] == "RAND") action[key] = randVar.ToString();
        }

        try
        {
            // Matches the "FUNCTION" key, corresponding to the action to do.
            switch (action["FUNCTION"])
            {
                case "START":
                    CallPlay();
                    break;

                case "STOP":
                    CallStop();
                    break;

                case "WALL":
                    SetWall(action);
                    break;

                case "OUTLINE":
                    SetOutline(action);
                    break;

                case "MOLE":
                    Mole.MoleType moleType =
                        (action.ContainsKey("TYPE") && System.Enum.TryParse(action["TYPE"], out Mole.MoleType parsedType)) ?
                        parsedType : Mole.MoleType.SimpleTarget;

                    SetMole(
                        action["X"], action["Y"],
                        action["LIFETIME"], moleType,
                        Mole.MoleOutcome.Valid,
                        action.ContainsKey("VALIDATION") ? action["VALIDATION"] : ""
                    );

                    break;

                case "DISTRACTOR":
                    Mole.MoleType distractorType =
                        (action.ContainsKey("TYPE") && System.Enum.TryParse(action["TYPE"], out Mole.MoleType parsedMoleType)) ?
                        parsedMoleType : Mole.MoleType.DistractorLeft;

                    SetMole(action["X"], action["Y"], action["LIFETIME"], distractorType, Mole.MoleOutcome.Distractor);
                    break;

                case "DIFFICULTY":
                    SetDifficulty(action["SPEED"]);
                    break;

                case "MODIFIER":
                    SetModifier(action);
                    break;

                case "SEGMENT":
                    SetSegment(action["ID"], action["LABEL"]);
                    break;

                case "CALIBRATION":
                    TriggerCalibration(action["TYPE"]);
                    break;

                case "MESSAGE":
                    if (!action.ContainsKey("IMAGE")) action["IMAGE"] = null;
                    SetMessage(action["LABEL"], action["TIME"], action["IMAGE"]);
                    break;

                case "RANDGEN":
                    RegenRand(ParseFloat(action["STARTVALUE"]), ParseFloat(action["ENDVALUE"]), bool.Parse(action["ISINT"]));
                    break;

                default:
                    Debug.LogWarning($"Unknown test pattern statement {action["FUNCTION"]}");
                    break;
            }
        }
        catch (System.Exception e)
        {
            // Log the error message along with the exact stack trace.
            Debug.LogError($"Error in {action["FUNCTION"]} line {lineNumber} : {e.Message}\nStackTrace: {e.StackTrace}");
        }
    }


    // Calls the GameDirector to start the game
    public void CallPlay()
    {
        gameDirector.StartGame();
    }

    // Calls the GameDirector to stop the game
    public void CallStop()
    {
        gameDirector.StopGame();
    }

    // Updates one or multiple Wall's properties
    private void SetWall(Dictionary<string, string> action)
    {
        string tempValue;

        wallManager.Clear();

        if (action.TryGetValue("ROW", out tempValue))
        {
            wallManager.UpdateMoleCount(newRowCount: int.Parse(tempValue));
        }
        if (action.TryGetValue("COL", out tempValue))
        {
            wallManager.UpdateMoleCount(newColumnCount: int.Parse(tempValue));
        }
        if (action.TryGetValue("SIZEX", out tempValue))
        {
            wallManager.UpdateWallSize(newWallSizeX: ParseFloat(tempValue));
        }
        if (action.TryGetValue("SIZEY", out tempValue))
        {
            wallManager.UpdateWallSize(newWallSizeY: ParseFloat(tempValue));
        }
        if (action.TryGetValue("SIZEZ", out tempValue))
        {
            wallManager.UpdateWallSize(newWallSizeZ: ParseFloat(tempValue));
        }
        if (action.TryGetValue("POSZ", out tempValue))
        {
            wallManager.UpdateWallPosition(posZ: ParseFloat(tempValue));
        }
        if (action.TryGetValue("POSY", out tempValue))
        {
            wallManager.UpdateWallPosition(posY: ParseFloat(tempValue));
        }
        if (action.TryGetValue("CURVEX", out tempValue))
        {
            wallManager.UpdateWallCurveRatio(newCurveRatioX: ParseFloat(tempValue));
        }
        if (action.TryGetValue("CURVEY", out tempValue))
        {
            wallManager.UpdateWallCurveRatio(newCurveRatioY: ParseFloat(tempValue));
        }
        if (action.TryGetValue("MAXANGLE", out tempValue))
        {
            wallManager.UpdateWallMaxAngle(ParseFloat(tempValue));
        }
        if (action.TryGetValue("MOLESCALEX", out tempValue))
        {
            wallManager.UpdateMoleScale(newMoleScaleX: ParseFloat(tempValue));
        }
        if (action.TryGetValue("MOLESCALEY", out tempValue))
        {
            wallManager.UpdateMoleScale(newMoleScaleY: ParseFloat(tempValue));
        }
        if (action.TryGetValue("MOLESCALEZ", out tempValue))
        {
            wallManager.UpdateMoleScale(newMoleScaleZ: ParseFloat(tempValue));
        }
        if (action.TryGetValue("VISIBLE", out tempValue))
        {
            wallManager.SetWallVisible(bool.Parse(tempValue));
        }
        wallManager.Enable();
    }

    // Spawns a Mole
    private void SetMole(string xIndex, string yIndex, string lifeTime, Mole.MoleType moleType, Mole.MoleOutcome outcome, string validationArg = "")
    {
        int targetSpawnId = ((int.Parse(xIndex)) * 100) + (int.Parse(yIndex));
        Mole mole = wallManager.CreateMole(targetSpawnId, ParseFloat(lifeTime), gameDirector.GetMoleExpiringDuration(), moleType, outcome, validationArg);
        molesList[targetSpawnId] = mole;
        if (mole.IsValid()) AddToTargetsList(mole);
    }

    // Updates the game difficulty
    private void SetDifficulty(string difficulty)
    {
        gameDirector.SetDifficulty(difficulty);
    }

    // Updates the game difficulty
    private void SetMessage(string message, string time, string ImgPath)
    {
        message = message.Replace("_", " ").Replace("|", "\n");
        playerPanel.SetMessage(message, ParseFloat(time));
        playerPanel.SetImage(ImgPath);
    }

    // Triggers a calibration
    private void TriggerCalibration(string type) => gameDirector.InvokeCalibrationEvent(type);

    // Updates one or multiple modifiers
    private void SetModifier(Dictionary<string, string> action)
    {
        string tempValue;

        if (action.TryGetValue("EYEPATCH", out tempValue))
        {
            modifiersManager.SetEyePatch((ModifiersManager.EyePatch)System.Enum.Parse(typeof(ModifiersManager.EyePatch), tempValue));
        }
        if (action.TryGetValue("MIRROR", out tempValue))
        {
            modifiersManager.SetMirrorEffect(bool.Parse(tempValue));
        }
        if (action.TryGetValue("CONTROLLEROFFSET", out tempValue))
        {
            modifiersManager.SetControllerOffset(ParseFloat(tempValue));
        }
        if (action.TryGetValue("MAINCONTROLLER", out tempValue))
        {
            modifiersManager.SetMainController((ModifiersManager.ControllerSetup)System.Enum.Parse(typeof(ModifiersManager.ControllerSetup), tempValue));
            motorspaceManager.SetActiveMotorSpace(tempValue);
        }
        if (action.TryGetValue("POINTERTYPE", out tempValue))
        {
            modifiersManager.SetPointerType((ModifiersManager.PointerType)System.Enum.Parse(typeof(ModifiersManager.PointerType), tempValue));
        }
        if (action.TryGetValue("EMGPOINTERMODE", out tempValue))
        {
            modifiersManager.SetEMGPointerMode((EMGPointerBehavior)System.Enum.Parse(typeof(EMGPointerBehavior), tempValue));
        }
        if (action.TryGetValue("PRISM", out tempValue))
        {
            modifiersManager.SetPrismOffset(ParseFloat(tempValue));
        }
        if (action.TryGetValue("HIDEWALL", out tempValue))
        {
            modifiersManager.SetHideWall((ModifiersManager.HideWall)System.Enum.Parse(typeof(ModifiersManager.HideWall), tempValue));
        }
        if (action.TryGetValue("THEME", out tempValue))
        {
            themeManager.LoadTheme(tempValue);
        }
        if (action.TryGetValue("HIDEWALLAMOUNT", out tempValue))
        {
            modifiersManager.SetHideWallAmount(ParseFloat(tempValue));
        }
        if (action.TryGetValue("GEOMETRICMIRROR", out tempValue))
        {
            modifiersManager.SetGeometricMirror(bool.Parse(tempValue));
        }
        if (action.TryGetValue("RESTRICTMOTOR", out tempValue))
        {
            modifiersManager.SetMotorRestriction(bool.Parse(tempValue));
        }
        if (action.TryGetValue("RESTRICTMOTORUPPER", out tempValue))
        {
            modifiersManager.SetMotorRestrictionUpper(ParseFloat(tempValue));
        }
        if (action.TryGetValue("RESTRICTMOTORLOWER", out tempValue))
        {
            modifiersManager.SetMotorRestrictionLower(ParseFloat(tempValue));
        }
        if (action.TryGetValue("MOTORSPACE", out tempValue))
        {
            modifiersManager.SetMotorspace((ModifiersManager.MotorspaceSize)System.Enum.Parse(typeof(ModifiersManager.MotorspaceSize), tempValue));
        }
        if (action.TryGetValue("PERFORMANCEFEEDBACK", out tempValue))
        {
            modifiersManager.SetPerformanceFeedback(bool.Parse(tempValue));
        }
        if (action.TryGetValue("HAPTICFEEDBACK", out tempValue))
        {
            tactorConnector.feedbackEnabled = bool.Parse(tempValue);
        }
        if (action.TryGetValue("EMBODIMENT", out tempValue))
        {
            modifiersManager.SetEmbodiment((ModifiersManager.Embodiment)System.Enum.Parse(typeof(ModifiersManager.Embodiment), tempValue));
        }
        if (action.TryGetValue("CURSORVISIBILITY", out tempValue))
        {
            GameObject cursorR = GameObject.Find("SphereR");
            if (cursorR != null) cursorR.SetActive(bool.Parse(tempValue));
            GameObject cursorL = GameObject.Find("SphereL");
            if (cursorL != null) cursorL.SetActive(bool.Parse(tempValue));
        }

    }

    private void SetSegment(string Idval, string label)
    {
        loggerNotifier.NotifyLogger(overrideEventParameters: new Dictionary<string, object>(){
            {"PatternSegmentID", Idval},
            {"PatternSegmentLabel", label}
            });
    }

    // Regenerates the random variable
    private void RegenRand(float startValue, float endValue, bool isInt)
    {
        randVar = Random.Range(startValue, endValue);
        if (isInt) randVar = Mathf.Round(randVar);
    }

    // Parses a string into a float
    private float ParseFloat(string value)
    {
        return float.Parse(value, CultureInfo.InvariantCulture);
    }

    // Sets the outline visibility
    private void SetOutline(Dictionary<string, string> action)
    {
        string tempValue;

        if (action.TryGetValue("VISIBLE", out tempValue))
        {
            wallManager.SetOutlineVisible(bool.Parse(tempValue));
        }
    }
}
