using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class ModifierUpdateEvent : UnityEvent<string, string> { }


/*
Manages different VR modifiers, which are setting the main hand, dual-task mode, eye-patch, mirror mode and prism offset.

WARNING: Due to the Vive overlay, it is necessary to disable the chaperone bounds in the Vive settings, otherwise the eye patch would still render the chaperone if near a boundary.
To do so, go the the VR settings WHILE INSIDE THE HEADSET -> Chaperone -> select DEVELOPER MODE and set a neutral color with the lowest opacity possible.
It is also possible to fully hide the chaperone by editing the steamvr.vrsettings file and setting "CollisionBoundsColorGammaA" to 0.
*/


public class ModifiersManager : MonoBehaviour
{

    public Dictionary<string, object> defaultModifiers;

    public enum ControllerSetup { Left, Both, Right, Off };
    public enum Embodiment { Hands, Cursor, Off };
    public enum MotorspaceSize { Small, Medium, Large };
    public enum EyePatch { Left, None, Right };
    public enum HideWall { Left, None, Right };
    public enum PointerType { BasicPointer, EMGPointer };

    [SerializeField]
    private GameObject hideWallLeft;

    [SerializeField]
    private GameObject hideWallRight;

    [SerializeField]
    private Material hideWallRightMat;

    [SerializeField]
    private Material hideWallLeftMat;

    [SerializeField]
    private UnityEngine.UI.Slider hideWallSlider;

    [SerializeField]
    private UnityEngine.UI.Slider prismEffectSlider;

    [SerializeField]
    private GameObject rightController;

    [SerializeField]
    private GameObject[] rightControllerVisuals;

    [SerializeField]
    private GameObject mirrorControllerR;

    [SerializeField]
    private GameObject mirrorControllerL;

    [SerializeField]
    private WallManager wallManager;

    [SerializeField]
    private MotorSpaceManager motorSpaceManager;

    [SerializeField]
    private GameObject leftController;

    [SerializeField]
    private GameObject[] leftControllerVisuals;

    [SerializeField]
    private Transform rightControllerContainer;

    [SerializeField]
    private Transform leftControllerContainer;

    [SerializeField]
    private UnityEngine.UI.Slider controllerOffsetSlider;

    [SerializeField]
    private GameObject prismOffsetObject;
    private float prismOffsetAmount = -1f;

    [SerializeField]
    private Camera viveCamera;

    [SerializeField]
    private Transform wallReference;

    [SerializeField]
    private GameObject physicalMirror;

    [SerializeField]
    private float hideWallHighestStart = 1.3f;
    [SerializeField]
    private float hideWallHighestEnd = 0.6f;
    [SerializeField]
    private float hideWallLowestStart = -0.2f;
    [SerializeField]
    private float hideWallLowestEnd = -1.05f;
    private float hideWallAmount = -1f;
    ModifiersManager.MotorspaceSize motorspaceSize = ModifiersManager.MotorspaceSize.Large;
    private Embodiment embodiment = Embodiment.Hands;
    private EyePatch eyePatch = EyePatch.None;
    private HideWall hideWall = HideWall.None;
    private ControllerSetup controllerSetup = ControllerSetup.Right;
    private bool performanceFeedback = true;
    private bool mirrorEffect;
    private bool physicalMirrorEffect;
    private bool geometricMirrorEffect;
    private bool dualTask;
    private bool rightControllerMain;
    private float controllerOffset;
    private float prismOffset;
    private bool motorRestriction;
    private float motorRestrictionUpper = 1f;
    private float motorRestrictionLower = 0.5f;
    private Dictionary<string, GameObject> controllersList;
    private Pointer[] rightControllerPointers;
    private Pointer[] leftControllerPointers;
    public const PointerType defaultPointerType = PointerType.BasicPointer;

    private LoggerNotifier loggerNotifier;
    private ModifierUpdateEvent modifierUpdateEvent = new ModifierUpdateEvent();

    void Awake()
    {
        controllersList = new Dictionary<string, GameObject>(){
            {"main", rightController},
            {"second", leftController}
        };

        leftControllerPointers = leftController.GetComponents<Pointer>();
        rightControllerPointers = rightController.GetComponents<Pointer>();

        SetControllerEnabled(controllerSetup);

        // Initialization of the LoggerNotifier. Here we will only pass parameters to PersistentEvent, even if we will also raise Events.
        loggerNotifier = new LoggerNotifier(persistentEventsHeadersDefaults: new Dictionary<string, string>(){
            {"RightControllerMain", "Undefined"},
            {"MirrorEffect", "No Mirror Effect Defined"},
            {"EyePatch", "No Eye Patch Defined"},
            {"ControllerOffset", "No Controller Offset Defined"},
            {"PrismOffset", "No Controller Offset Defined"},
            {"DualTask", "No Dual Task Defined"},
            {"HideWall", "No Hide Wall Defined"},
            {"HideWallAmount", "No Hide Wall Amount Defined"},
            {"GeometricMirror", "No GeometricMirror Defined"},
            {"PerformanceFeedback", "No PerformanceFeedback Defined"},
            {"Embodiment", "Undefined"},
        });
        // Initialization of the starting values of the parameters.
        loggerNotifier.InitPersistentEventParameters(new Dictionary<string, object>(){
            {"RightControllerMain", rightControllerMain},
            {"MirrorEffect", mirrorEffect},
            {"EyePatch", System.Enum.GetName(typeof(ModifiersManager.EyePatch), eyePatch)},
            {"ControllerOffset", controllerOffset},
            {"PrismOffset", controllerOffset},
            {"DualTask", dualTask},
            {"HideWall", System.Enum.GetName(typeof(ModifiersManager.HideWall), hideWall)},
            {"HideWallAmount", hideWallAmount},
            {"GeometricMirror", geometricMirrorEffect},
            {"PerformanceFeedback", performanceFeedback},
            {"Embodiment", System.Enum.GetName(typeof(ModifiersManager.Embodiment), embodiment)},
        });

        defaultModifiers = new Dictionary<string, object>() {
        {"ControllerSetup", this.controllerSetup},
        {"MotorspaceSize", this.motorspaceSize},
        {"EyePatch", this.eyePatch},
        {"HideWall", this.hideWall},
        {"HideWallAmount", 0f}, // hideWallAmount is a calculated value.
        {"MirrorEffect", this.mirrorEffect},
        {"PhysicalMirrorEffect", this.physicalMirrorEffect},
        {"GeometricMirrorEffect", this.geometricMirrorEffect},
        {"RightControllerMain", this.rightControllerMain},
        {"ControllerOffset", this.controllerOffset},
        {"PrismOffset", this.prismOffset},
        {"MotorRestriction", this.motorRestriction},
        {"MotorRestrictionUpper", this.motorRestrictionUpper},
        {"MotorRestrictionLower", this.motorRestrictionLower},
        {"PerformanceFeedback", this.performanceFeedback},
        {"Embodiment", this.embodiment},
        };
    }

    void Start()
    {
        SetDefaultModifiers();
    }

    private Pointer getActivePointer(GameObject controller)
    {
        // Get the PointerTypeSelector component from the controller and return the active pointer.
        return controller.GetComponent<PointerTypeSelector>().GetActivePointer();
    }

    public void UpdateDefaultModifier(string modifier, object val)
    {
        defaultModifiers[modifier] = val;
    }

    public void SetDefaultModifiers()
    {
        SetModifiers(defaultModifiers);
    }

    public void SetModifiers(Dictionary<string, object> state)
    {
        SetEyePatch((ModifiersManager.EyePatch)state["EyePatch"]);
        SetHideWall((ModifiersManager.HideWall)state["HideWall"]);
        SetMotorRestriction((bool)state["MotorRestriction"]);
        SetMotorRestrictionUpper((float)state["MotorRestrictionUpper"]);
        SetMotorRestrictionLower((float)state["MotorRestrictionLower"]);
        SetMotorspace((ModifiersManager.MotorspaceSize)state["MotorspaceSize"]);
        SetMirrorEffect((bool)state["MirrorEffect"]);
        SetPhysicalMirror((bool)state["PhysicalMirrorEffect"]);
        SetGeometricMirror((bool)state["GeometricMirrorEffect"]);
        SetControllerOffset((float)state["ControllerOffset"]);
        SetPrismOffset((float)state["PrismOffset"]);
        SetMainController((ModifiersManager.ControllerSetup)state["ControllerSetup"]);
        SetControllerEnabled((ModifiersManager.ControllerSetup)state["ControllerSetup"], true);
        SetPerformanceFeedback((bool)state["PerformanceFeedback"]);
        SetEmbodiment((ModifiersManager.Embodiment)state["Embodiment"]);
    }

    // Sets an eye patch. Calls WaitForCameraAndUpdate coroutine to set eye patch.
    public void SetEyePatch(EyePatch value)
    {
        if (eyePatch == value) return;
        eyePatch = value;
        StartCoroutine(WaitForCameraAndUpdate(eyePatch));
    }

    public void SetPointerType(PointerType pointerType)
    {
        leftController.GetComponent<PointerTypeSelector>().ActivatePointer(pointerType);
        rightController.GetComponent<PointerTypeSelector>().ActivatePointer(pointerType);
    }

    // Sets the EMG pointer behavior mode (Training or LivePrediction)
    public void SetEMGPointerMode(EMGPointerBehavior mode)
    {
        // Get EMGPointer components from both controllers
        EMGPointer leftEMGPointer = leftController.GetComponent<EMGPointer>();
        EMGPointer rightEMGPointer = rightController.GetComponent<EMGPointer>();

        // Change behavior on both controllers if they have EMGPointer component
        if (leftEMGPointer != null && leftEMGPointer.enabled)
        {
            leftEMGPointer.ChangeBehavior(mode);
        }

        if (rightEMGPointer != null && rightEMGPointer.enabled)
        {
            rightEMGPointer.ChangeBehavior(mode);
        }

        loggerNotifier.NotifyLogger("EMG Pointer Mode Set " + System.Enum.GetName(typeof(EMGPointerBehavior), mode), EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"EMGPointerMode", System.Enum.GetName(typeof(EMGPointerBehavior), mode)}
        });

        modifierUpdateEvent.Invoke("EMGPointerMode", System.Enum.GetName(typeof(EMGPointerBehavior), mode));
    }

    public void SetHideWall(HideWall value)
    {
        if (hideWall == value) return;
        hideWall = value;

        loggerNotifier.NotifyLogger("Hide Wall Effect Set " + value, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"HideWall", value}
        });

        if (hideWall == HideWall.Left)
        {
            hideWallLeft.SetActive(true);
            hideWallRight.SetActive(false);
            onHideWallSliderChanged();
        }
        else if (hideWall == HideWall.Right)
        {
            hideWallLeft.SetActive(false);
            hideWallRight.SetActive(true);
            onHideWallSliderChanged();
        }
        else if (hideWall == HideWall.None)
        {
            hideWallLeft.SetActive(false);
            hideWallRight.SetActive(false);
        }
    }

    public void SetHideWallAmount(float value)
    {
        hideWallSlider.value = value;
    }

    public void onHideWallSliderChanged()
    {
        float sliderValue = (float)hideWallSlider.value;
        float highVal = (float)hideWallSlider.maxValue;
        float lowVal = (float)hideWallSlider.minValue;
        hideWallAmount = ((sliderValue - lowVal) / highVal);
        float multiplier = 1 - hideWallAmount;
        float startRange = hideWallHighestStart - hideWallLowestStart;
        float endRange = hideWallHighestEnd - hideWallLowestEnd;

        float newStart = (startRange * multiplier) + hideWallLowestStart;
        float newEnd = (endRange * multiplier) + hideWallLowestEnd;

        hideWallLeftMat.SetFloat("_FogMaxHeight", -newStart);
        hideWallLeftMat.SetFloat("_FogMinHeight", -newEnd);
        hideWallRightMat.SetFloat("_FogMaxHeight", newStart);
        hideWallRightMat.SetFloat("_FogMinHeight", newEnd);

        loggerNotifier.NotifyLogger("Hide Wall Amount: " + hideWallAmount, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"HideWallAmount", hideWallAmount}
        });
    }

    public void SetPerformanceFeedback(bool value)
    {
        if (performanceFeedback == value) return;

        performanceFeedback = value;

        wallManager.SetPerformanceFeedback(performanceFeedback);
        // Apply performance feedback to all pointers in rightControllerPointers using LINQ
        rightControllerPointers.ToList().ForEach(pointer => pointer.SetPerformanceFeedback(performanceFeedback));
        leftControllerPointers.ToList().ForEach(pointer => pointer.SetPerformanceFeedback(performanceFeedback));

        // Raises an Event and updates a PersistentEvent's parameter (in consequence, a PersistentEvent will also be raised)
        loggerNotifier.NotifyLogger("Performance Feedback Set " + value, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"PerformanceFeedback", value}
        });

        modifierUpdateEvent.Invoke("PerformanceFeedback", value.ToString());
    }

    public void SetMotorRestriction(bool value)
    {
        // motor restriction may need to be "refreshed" when controllers change.
        // therefore, allow calling motorRestriction = True to update.
        //if (motorRestriction == value) return;

        motorRestriction = value;

        MotorRestriction restriction = MotorRestriction.none;
        if (value)
        {
            restriction = MotorRestriction.restrict;
        }

        motorSpaceManager.SetMotorRestriction(restriction, motorRestrictionLower, motorRestrictionUpper);

        // Raises an Event and updates a PersistentEvent's parameter (in consequence, a PersistentEvent will also be raised)
        loggerNotifier.NotifyLogger("Motor Restriciton Set " + value, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"MotorRestrictionLower", motorRestrictionLower},
            {"MotorRestrictionUpper", motorRestrictionUpper}
        });

        modifierUpdateEvent.Invoke("MotorRestriction", value.ToString());
    }

    public void SetMotorRestrictionUpper(float value)
    {
        if (motorRestrictionUpper == value) return;

        motorRestrictionUpper = value;

        // Raises an Event and updates a PersistentEvent's parameter (in consequence, a PersistentEvent will also be raised)
        loggerNotifier.NotifyLogger("Motor Restriciton Upper Set to " + value, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"MotorRestrictionLower", motorRestrictionLower},
            {"MotorRestrictionUpper", motorRestrictionUpper}
        });

        modifierUpdateEvent.Invoke("MotorRestrictionUpper", value.ToString());
    }

    public void SetMotorRestrictionLower(float value)
    {
        if (motorRestrictionLower == value) return;

        motorRestrictionLower = value;

        // Raises an Event and updates a PersistentEvent's parameter (in consequence, a PersistentEvent will also be raised)
        loggerNotifier.NotifyLogger("Motor Restriciton Lower Set to " + value, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"MotorRestrictionLower", motorRestrictionLower},
            {"MotorRestrictionUpper", motorRestrictionUpper}
        });

        modifierUpdateEvent.Invoke("MotorRestrictionLower", value.ToString());
    }

    public void SetMotorspace(ModifiersManager.MotorspaceSize size)
    {
        if (size == ModifiersManager.MotorspaceSize.Small)
        {
            motorSpaceManager.SetMotorSpaceSmall();
        }
        else if (size == ModifiersManager.MotorspaceSize.Medium)
        {
            motorSpaceManager.SetMotorSpaceMedium();
        }
        else if (size == ModifiersManager.MotorspaceSize.Large)
        {
            motorSpaceManager.SetMotorSpaceLarge();
        }
    }

    // Sets a controller position and rotation's mirroring effect. Calls UpdateMirrorEffect to set the mirror.
    public void SetMirrorEffect(bool value)
    {
        if (mirrorEffect == value) return;
        if (!getActivePointer(controllersList["main"]).isActiveAndEnabled) return;

        mirrorEffect = value;
        UpdateMirrorEffect();

        // Raises an Event and updates a PersistentEvent's parameter (in consequence, a PersistentEvent will also be raised)
        loggerNotifier.NotifyLogger("Mirror Effect Set " + value, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"MirrorEffect", value}
        });

        modifierUpdateEvent.Invoke("MirrorEffect", value.ToString());
    }

    public void SetPhysicalMirror(bool value)
    {
        if (physicalMirrorEffect == value) return;
        physicalMirrorEffect = value;
        physicalMirror.SetActive(value);
    }

    public void SetGeometricMirror(bool value)
    {
        if (geometricMirrorEffect == value) return;
        geometricMirrorEffect = value;
        motorSpaceManager.SetMirror(value);
        UpdateGeometricMirror(value);
        loggerNotifier.NotifyLogger("Geometric Mirror Set " + value.ToString(), EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"GeometricMirror", value.ToString()}
        });
    }

    public void UpdateGeometricMirror(bool enable)
    {
        if (enable)
        {
            if (controllerSetup == ModifiersManager.ControllerSetup.Right)
            {
                mirrorControllerR.SetActive(true);
                mirrorControllerL.SetActive(false);
            }
            else
            {
                mirrorControllerL.SetActive(true);
                mirrorControllerR.SetActive(false);
            }
        }
        else
        {
            mirrorControllerL.SetActive(false);
            mirrorControllerR.SetActive(false);
        }
    }

    // Helper function to calculate how to modify
    // controller's local position, to make it
    // offset in the right direction.
    // Normally this would be handled by setting
    // its world position, but this causes glitches.
    // Adapted from:
    // https://stackoverflow.com/questions/71710139/how-do-i-rotate-a-direction-vector3-upwards-by-an-angle-in-unity
    Vector3 RotateTowardsUp(Vector3 start, float angle)
    {
        // Positive X offsets needs a Vector3.forward.
        // Negative X offsets needs a Vector3.back.
        Vector3 direction = Vector3.forward;
        if (start.x < 0)
        {
            direction = Vector3.back;
        }

        Vector3 axis = Vector3.Cross(start, direction);

        return Quaternion.AngleAxis(angle, axis) * start;
    }


    public void SetControllerOffset(float value)
    {

        controllerOffset = value;

        // Before calibration was implemented, controllers
        // were offset by setting their world position.
        // However, with calibration, this results in
        // undefined behavior. A temporary fix was to
        // use localPosition, but the localPosition is
        // rarely aligned to world axes after calibration.
        // This implements a helper function which
        // reads the parents' rotation and compensates for
        // it, when sertting the controller's local position.
        Vector3 xOffset = new Vector3(controllerOffset * 0.1f, 0f, 0f);
        Transform controllerParent = rightControllerContainer.parent;
        Vector3 rotatedVector = RotateTowardsUp(xOffset, controllerParent.eulerAngles.y);
        rightControllerContainer.localPosition = rotatedVector;

        rightControllerContainer.localPosition = rotatedVector;
        leftControllerContainer.localPosition = rotatedVector;

        loggerNotifier.NotifyLogger("Controller Offset Set " + value, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"ControllerOffset", value}
        });

        //modifierUpdateEvent.Invoke("ControllerOffset", value.ToString());
    }

    public void OnControllerOffsetSliderChanged()
    {
        float sliderValue = (float)controllerOffsetSlider.value;

        SetControllerOffset(sliderValue);
    }

    // Sets the prism effect. Shifts the view (around y axis) by a given angle to create a shifting between seen view and real positions.
    public void SetPrismOffset(float value)
    {
        prismOffset = value;
        prismOffsetObject.transform.localEulerAngles = new Vector3(0, prismOffset, 0);

        loggerNotifier.NotifyLogger("Prism Offset Set " + value, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"PrismOffset", value}
        });

        //modifierUpdateEvent.Invoke("PrismOffset", value.ToString());
    }

    public void OnPrismOffsetSliderChanged()
    {
        float sliderValue = (float)prismEffectSlider.value;

        SetPrismOffset(sliderValue);
    }

    public void SetMainControllerFromString(string controller)
    {
        Debug.Log("Called");
        SetMainController((ModifiersManager.ControllerSetup)System.Enum.Parse(typeof(ModifiersManager.ControllerSetup), controller));
    }

    // Sets the main controller. By default it is the right handed one.
    public void SetMainController(ModifiersManager.ControllerSetup controller)
    {
        controllerSetup = controller;
        SetControllerEnabled(controller);
        if (controllerSetup == ModifiersManager.ControllerSetup.Left)
        {
            controllersList["main"] = leftController;
            controllersList["second"] = rightController;
        }
        else // Right and Both
        {
            controllersList["main"] = rightController;
            controllersList["second"] = leftController;
        }

        if (mirrorEffect)
        {
            UpdateMirrorEffect();
        }

        if (geometricMirrorEffect)
        {
            UpdateGeometricMirror(geometricMirrorEffect);
        }

        loggerNotifier.NotifyLogger("Controller Main Set " + System.Enum.GetName(typeof(ControllerSetup), controllerSetup), EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"ControllerMain", System.Enum.GetName(typeof(ControllerSetup), controllerSetup)}
        });
    }

    public UnityEvent<string, string> GetModifierUpdateEvent()
    {
        return modifierUpdateEvent;
    }

    // Updates the mirroring effect. Is called when enabling/disabling the mirror effect or when controllers are activated/deactivated (dual task, main controller change).
    private void UpdateMirrorEffect()
    {
        if (mirrorEffect)
        {
            controllersList["main"].gameObject.GetComponent<ControllerModifierManager>().EnableMirror(viveCamera.transform, wallReference);

            if (!dualTask)
            {
                controllersList["second"].gameObject.GetComponent<ControllerModifierManager>().DisableMirror();
            }
            else
            {
                controllersList["second"].gameObject.GetComponent<ControllerModifierManager>().EnableMirror(viveCamera.transform, wallReference);
            }
        }
        else
        {
            controllersList["main"].gameObject.GetComponent<ControllerModifierManager>().DisableMirror();

            if (dualTask)
            {
                controllersList["second"].gameObject.GetComponent<ControllerModifierManager>().DisableMirror();
            }
        }
    }

    // Sets the level of embodiment used by the game. (Show hands (including controller) or just cursor).
    public void SetEmbodiment(Embodiment value)
    {
        if (embodiment == value) return;

        embodiment = value;
        // Pass embodiment on to the ControllerModifierManager.
        controllersList["main"].gameObject.GetComponent<ControllerModifierManager>().SetEmbodiment(embodiment);
        controllersList["second"].gameObject.GetComponent<ControllerModifierManager>().SetEmbodiment(embodiment);

        loggerNotifier.NotifyLogger("Embodiment Set " + System.Enum.GetName(typeof(Embodiment), embodiment), EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"Embodiment", System.Enum.GetName(typeof(Embodiment), embodiment)}
        });

    }

    // Enables/disables a given controller
    private void SetControllerEnabled(ControllerSetup controllerType, bool enabled = true)
    {
        controllerSetup = controllerType;

        bool enableRight = (controllerType == ControllerSetup.Right || controllerType == ControllerSetup.Both);
        bool enableLeft = (controllerType == ControllerSetup.Left || controllerType == ControllerSetup.Both);
        Debug.Log("Right controller: " + enableRight + " | Left controller: " + enableLeft);

        if (enableRight)
        {
            foreach (GameObject obj in rightControllerVisuals)
            {
                obj.SetActive(true);
            }
        }
        else
        {
            foreach (GameObject obj in rightControllerVisuals)
            {
                obj.SetActive(false);
            }
        }

        if (enableLeft)
        {
            foreach (GameObject obj in leftControllerVisuals)
            {
                obj.SetActive(true);
            }
        }
        else
        {
            foreach (GameObject obj in leftControllerVisuals)
            {
                obj.SetActive(false);
            }
        }
    }


    public void LogState()
    {
        loggerNotifier.NotifyLogger("Controller Main Set " + System.Enum.GetName(typeof(ControllerSetup), controllerSetup), EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"ControllerMain", System.Enum.GetName(typeof(ControllerSetup), controllerSetup)}
        });

        loggerNotifier.NotifyLogger("Prism Offset Set " + prismOffset, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"PrismOffset", prismOffset}
        });
        loggerNotifier.NotifyLogger("Controller Offset Set " + controllerOffset, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"ControllerOffset", controllerOffset}
        });
        loggerNotifier.NotifyLogger("Geometric Mirror Set " + geometricMirrorEffect.ToString(), EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"GeometricMirror", geometricMirrorEffect.ToString()}
        });
        loggerNotifier.NotifyLogger("Motor Restriciton Lower Set to " + motorRestrictionLower, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"MotorRestrictionLower", motorRestrictionLower},
            {"MotorRestrictionUpper", motorRestrictionUpper}
        });
        loggerNotifier.NotifyLogger("Motor Restriciton Upper Set to " + motorRestrictionUpper, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"MotorRestrictionLower", motorRestrictionLower},
            {"MotorRestrictionUpper", motorRestrictionUpper}
        });
        loggerNotifier.NotifyLogger("Motor Restriciton Set " + motorRestriction, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"MotorRestrictionLower", motorRestrictionLower},
            {"MotorRestrictionUpper", motorRestrictionUpper}
        });
        loggerNotifier.NotifyLogger("Hide Wall Amount: " + hideWallAmount, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"HideWallAmount", hideWallAmount}
        });
        loggerNotifier.NotifyLogger("Hide Wall Effect Set " + hideWall, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"HideWall", hideWall}
        });
        loggerNotifier.NotifyLogger("Embodiment Set " + System.Enum.GetName(typeof(Embodiment), embodiment), EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"Embodiment", embodiment}
        });
    }

    // Sets the eye patch. Forces the camera to render a black screen for a short duration and disables an eye while the screen is black.
    // If the image wasn't forced black we would have a frozen image of the game in the disabled eye.

    /*
    WARNING: Due to the Vive overlay, it is necessary to disable the chaperone bounds in the Vive settings, otherwise the eye patch would still render the chaperone if near a boundary.
    To do so, go the the VR settings WHILE INSIDE THE HEADSET -> Chaperone -> select DEVELOPER MODE and set a neutral color with the lowest opacity possible.
    It is also possible to fully hide the chaperone by editing the steamvr.vrsettings file and setting "CollisionBoundsColorGammaA" to 0.
    */
    private IEnumerator WaitForCameraAndUpdate(EyePatch value)
    {
        viveCamera.farClipPlane = 0.02f;
        viveCamera.clearFlags = CameraClearFlags.SolidColor;
        viveCamera.backgroundColor = Color.black;

        yield return new WaitForSeconds(0.05f);

        viveCamera.farClipPlane = 1000f;
        viveCamera.clearFlags = CameraClearFlags.Skybox;

        if (value == EyePatch.Right)
        {
            viveCamera.stereoTargetEye = StereoTargetEyeMask.Left;
        }
        else if (value == EyePatch.None)
        {
            viveCamera.stereoTargetEye = StereoTargetEyeMask.Both;
        }
        else if (value == EyePatch.Left)
        {
            viveCamera.stereoTargetEye = StereoTargetEyeMask.Right;
        }

        loggerNotifier.NotifyLogger("Eye Patch Set " + System.Enum.GetName(typeof(ModifiersManager.EyePatch), value), EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"EyePatch", System.Enum.GetName(typeof(ModifiersManager.EyePatch), value)}
        });

        modifierUpdateEvent.Invoke("EyePatch", System.Enum.GetName(typeof(ModifiersManager.EyePatch), value));
    }
}
