using System;
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


    public enum ControllerSetup { Left, Both, Right, Off };
    public enum Embodiment { Full, LeftArm, RightArm, Arms, LeftHand, RightHand, Hands, Cursor, Off };
    public enum MotorspaceSize { Small, Medium, Large };
    public enum PerformanceFeedback { None, Operation, Action, Task, All };
    public enum EyePatch { Left, None, Right };
    public enum HideWall { Left, None, Right };
    public enum PointerType { BasicPointer, EMGPointer };

    private class Modifiers
    {
        public ControllerSetup? controllerSetup;
        public MotorspaceSize? motorspaceSize;
        public EyePatch? eyePatch;
        public HideWall? hideWall;
        public float? hideWallAmount;
        public bool? mirrorEffect;
        public bool? physicalMirrorEffect;
        public bool? geometricMirrorEffect;
        public bool? rightControllerMain;
        public float? controllerOffset;
        public float? prismOffset;
        public bool? motorRestriction;
        public float? motorRestrictionUpper;
        public float? motorRestrictionLower;
        public Embodiment? embodiment;
        public PerformanceFeedback? performanceFeedback;
        public JudgementType? judgementType;
    }

    private Modifiers currentModifiers = new();

    private Modifiers defaultModifiers = new Modifiers()
        {
            controllerSetup = ControllerSetup.Right,
            motorspaceSize = MotorspaceSize.Large,
            eyePatch = EyePatch.None,
            hideWall = HideWall.None,
            hideWallAmount = 0f, // hideWallAmount is a calculated value.
            mirrorEffect = false,
            physicalMirrorEffect = false,
            geometricMirrorEffect = false,
            rightControllerMain = false,
            controllerOffset = 0f,
            prismOffset = 0f,
            motorRestriction = false,
            motorRestrictionUpper = 1f,
            motorRestrictionLower = 0.5f,
            embodiment = Embodiment.RightHand,
            performanceFeedback = PerformanceFeedback.None,
            judgementType = JudgementType.MaxSpeed
        };

    [SerializeField]
    private bool performanceFeedbackText = false;

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
    public GameObject rightController;

    [SerializeField]
    private GameObject[] rightControllerVisuals;

    [SerializeField]
    public GameObject mirrorControllerR;

    [SerializeField]
    public GameObject mirrorControllerL;

    [SerializeField]
    private WallManager wallManager;

    [SerializeField]
    private MotorSpaceManager motorSpaceManager;

    [SerializeField]
    public GameObject leftController;

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
    private PerformanceManager performanceManager;

    [SerializeField]
    private GameObject VRBody;

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
    private Embodiment embodiment = Embodiment.RightHand;
    private EyePatch eyePatch = EyePatch.None;
    private HideWall hideWall = HideWall.None;
    private ControllerSetup controllerSetup = ControllerSetup.Right;
    private bool mirrorEffect;
    private bool physicalMirrorEffect;
    private bool geometricMirrorEffect;
    private bool dualTask;
    private Dictionary<string, GameObject> controllersList;
    private Pointer[] rightControllerPointers;
    private Pointer[] leftControllerPointers;
    private Embodiment geometricEmbodiment;
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

        SetControllerEnabled(defaultModifiers.controllerSetup.Value);

        // Initialization of the LoggerNotifier. Here we will only pass parameters to PersistentEvent, even if we will also raise Events.
        loggerNotifier = new LoggerNotifier(persistentEventsHeadersDefaults: new Dictionary<string, string>(){
            {"RightControllerMain", "Undefined"},
            {"MirrorEffect", "No Mirror Effect Defined"},
            {"EyePatch", "No Eye Patch Defined"},
            {"ControllerOffset", "No Controller Offset Defined"},
            {"PrismOffset", "No Prism Offset Defined"},
            {"DualTask", "No Dual Task Defined"},
            {"HideWall", "No Hide Wall Defined"},
            {"HideWallAmount", "No Hide Wall Amount Defined"},
            {"GeometricMirror", "No GeometricMirror Defined"},
            {"Embodiment", "Undefined"},
        });
        // Initialization of the starting values of the parameters.
        loggerNotifier.InitPersistentEventParameters(new Dictionary<string, object>(){
            {"RightControllerMain", defaultModifiers.rightControllerMain},
            {"MirrorEffect", defaultModifiers.mirrorEffect},
            {"EyePatch", System.Enum.GetName(typeof(EyePatch), defaultModifiers.eyePatch)},
            {"ControllerOffset", defaultModifiers.controllerOffset},
            {"PrismOffset", defaultModifiers.prismOffset},
            {"DualTask", dualTask},
            {"HideWall", System.Enum.GetName(typeof(HideWall), defaultModifiers.hideWall)},
            {"HideWallAmount", defaultModifiers.hideWallAmount},
            {"GeometricMirror", defaultModifiers.geometricMirrorEffect},
            {"Embodiment", System.Enum.GetName(typeof(Embodiment), defaultModifiers.embodiment)},
        });
    }

    void Start()
    {
        SetDefaultModifiers();
        SetPerformanceFeedback(defaultModifiers.performanceFeedback.Value);
        SetJudgementType(defaultModifiers.judgementType.Value);
    }

    private Pointer getActivePointer(GameObject controller)
    {
        // Get the PointerTypeSelector component from the controller and return the active pointer.
        return controller.GetComponent<PointerTypeSelector>().GetActivePointer();
    }

    public void UpdateDefaultModifier(string modifier, object val)
    {
        if (defaultModifiers == null) defaultModifiers = new Modifiers();
        switch (modifier)
        {
            case "ControllerSetup": defaultModifiers.controllerSetup = (ControllerSetup)val; break;
            case "MotorspaceSize": defaultModifiers.motorspaceSize = (MotorspaceSize)val; break;
            case "EyePatch": defaultModifiers.eyePatch = (EyePatch)val; break;
            case "HideWall": defaultModifiers.hideWall = (HideWall)val; break;
            case "HideWallAmount": defaultModifiers.hideWallAmount = (float)val; break;
            case "MirrorEffect": defaultModifiers.mirrorEffect = (bool)val; break;
            case "PhysicalMirrorEffect": defaultModifiers.physicalMirrorEffect = (bool)val; break;
            case "GeometricMirrorEffect": defaultModifiers.geometricMirrorEffect = (bool)val; break;
            case "RightControllerMain": defaultModifiers.rightControllerMain = (bool)val; break;
            case "ControllerOffset": defaultModifiers.controllerOffset = (float)val; break;
            case "PrismOffset": defaultModifiers.prismOffset = (float)val; break;
            case "MotorRestriction": defaultModifiers.motorRestriction = (bool)val; break;
            case "MotorRestrictionUpper": defaultModifiers.motorRestrictionUpper = (float)val; break;
            case "MotorRestrictionLower": defaultModifiers.motorRestrictionLower = (float)val; break;
            case "Embodiment": defaultModifiers.embodiment = (Embodiment)val; break;
            default: break;
        }
    }

    public void SetDefaultModifiers()
    {
        SetModifiers(defaultModifiers);
    }

    private void SetModifiers(Modifiers modifiers)
    {
        SetEyePatch(modifiers.eyePatch.Value);
        SetHideWall(modifiers.hideWall.Value);
        SetMotorRestrictionUpper(modifiers.motorRestrictionUpper.Value);
        SetMotorRestrictionLower(modifiers.motorRestrictionLower.Value);
        SetMotorRestriction(modifiers.motorRestriction.Value);
        SetMotorspace(modifiers.motorspaceSize.Value);
        SetMirrorEffect(modifiers.mirrorEffect.Value);
        SetPhysicalMirror(modifiers.physicalMirrorEffect.Value);
        SetGeometricMirror(modifiers.geometricMirrorEffect.Value);
        SetControllerOffset(modifiers.controllerOffset.Value);
        SetPrismOffset(modifiers.prismOffset.Value);
        SetMainController(modifiers.controllerSetup.Value);
        SetControllerEnabled(modifiers.controllerSetup.Value, true);
        SetEmbodiment(modifiers.embodiment.Value);
        // remove the thing to force when it's = null and check if it still execute everything
    }

    // Sets an eye patch. Calls WaitForCameraAndUpdate coroutine to set eye patch.
    public void SetEyePatch(EyePatch value)
    {
        if (currentModifiers.eyePatch == value) return;
        currentModifiers.eyePatch = value;

        StartCoroutine(WaitForCameraAndUpdate(currentModifiers.eyePatch.Value));
    }

    public void SetPointerType(PointerType pointerType)
    {
        leftController.GetComponent<PointerTypeSelector>().ActivatePointer(pointerType);
        rightController.GetComponent<PointerTypeSelector>().ActivatePointer(pointerType);
    }

    public void SetHideWall(HideWall value)
    {
        if (currentModifiers.hideWall == value) return;
        currentModifiers.hideWall = value;

        loggerNotifier.NotifyLogger("Hide Wall Effect Set " + value, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"HideWall", value}
        });

        if (currentModifiers.hideWall == HideWall.Left)
        {
            hideWallLeft.SetActive(true);
            hideWallRight.SetActive(false);
            onHideWallSliderChanged();
        }
        else if (currentModifiers.hideWall == HideWall.Right)
        {
            hideWallLeft.SetActive(false);
            hideWallRight.SetActive(true);
            onHideWallSliderChanged();
        }
        else if (currentModifiers.hideWall == HideWall.None)
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
        currentModifiers.hideWallAmount = ((sliderValue - lowVal) / highVal);
        float multiplier = 1 - currentModifiers.hideWallAmount.Value;
        float startRange = hideWallHighestStart - hideWallLowestStart;
        float endRange = hideWallHighestEnd - hideWallLowestEnd;

        float newStart = (startRange * multiplier) + hideWallLowestStart;
        float newEnd = (endRange * multiplier) + hideWallLowestEnd;

        hideWallLeftMat.SetFloat("_FogMaxHeight", -newStart);
        hideWallLeftMat.SetFloat("_FogMinHeight", -newEnd);
        hideWallRightMat.SetFloat("_FogMaxHeight", newStart);
        hideWallRightMat.SetFloat("_FogMinHeight", newEnd);

        loggerNotifier.NotifyLogger("Hide Wall Amount: " + currentModifiers.hideWallAmount, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"HideWallAmount", currentModifiers.hideWallAmount}
        });
    }

    public void SetPerformanceFeedback(PerformanceFeedback value)
    {
        SetPerformanceFeedback(value, performanceFeedbackText);
    }

    public void SetPerformanceFeedback(PerformanceFeedback value, bool withText)
    {
        bool actionFeedback = false;
        bool operationFeedback = false;
        bool taskFeedback = false;

        switch (value)
        {
            case PerformanceFeedback.Operation:
                operationFeedback = true;
                break;
            case PerformanceFeedback.Action:
                actionFeedback = true;
                break;
            case PerformanceFeedback.Task:
                taskFeedback = true;
                break;
            case PerformanceFeedback.All:
                actionFeedback = operationFeedback = taskFeedback = true;
                break;
        }

        performanceFeedbackText = withText;
        // Apply values to all modifiers
        wallManager.SetActionPerformanceFeedback(actionFeedback, withText);
        foreach (Pointer c in rightControllerPointers)
        {
            c.SetActionPerformanceFeedback(actionFeedback, withText);
        }
        foreach (Pointer c in leftControllerPointers)
        {
            c.SetActionPerformanceFeedback(actionFeedback, withText);
        }

        // Task changes
        wallManager.SetTaskPerformanceFeedback(taskFeedback);
        motorSpaceManager.SetTaskPerformanceFeedback(taskFeedback);
        motorSpaceManager.SetOperationPerformanceFeedback(operationFeedback, withText);

        // Raises an Event and updates a PersistentEvent's parameter (in consequence, a PersistentEvent will also be raised)
        loggerNotifier.NotifyLogger($"Performance Feedback Set {Enum.GetName(typeof(PerformanceFeedback), value)}", EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"PerformanceFeedback", Enum.GetName(typeof(PerformanceFeedback), value)}
        });

        modifierUpdateEvent.Invoke($"PerformanceFeedback", Enum.GetName(typeof(PerformanceFeedback), value));

        currentModifiers.performanceFeedback = value;
    }

    public void SetMotorRestriction(bool value)
    {
        // motor restriction may need to be "refreshed" when controllers change.
        // therefore, allow calling motorRestriction = True to update.
        //if (motorRestriction == value) return;

        currentModifiers.motorRestriction = value;

        MotorRestriction restriction = MotorRestriction.none;
        if (value)
        {
            restriction = MotorRestriction.restrict;
        }

        motorSpaceManager.SetMotorRestriction(restriction, currentModifiers.motorRestrictionLower.Value, currentModifiers.motorRestrictionUpper.Value);

        // Raises an Event and updates a PersistentEvent's parameter (in consequence, a PersistentEvent will also be raised)
        loggerNotifier.NotifyLogger("Motor Restriciton Set " + value, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"MotorRestrictionLower", currentModifiers.motorRestrictionLower},
            {"MotorRestrictionUpper", currentModifiers.motorRestrictionUpper}
        });

        modifierUpdateEvent.Invoke("MotorRestriction", value.ToString());
    }

    public void SetMotorRestrictionUpper(float value)
    {
        if (currentModifiers.motorRestrictionUpper == value) return;

        currentModifiers.motorRestrictionUpper = value;

        // Raises an Event and updates a PersistentEvent's parameter (in consequence, a PersistentEvent will also be raised)
        loggerNotifier.NotifyLogger("Motor Restriciton Upper Set to " + value, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"MotorRestrictionLower", currentModifiers.motorRestrictionLower.GetValueOrDefault(-1f)},
            {"MotorRestrictionUpper", currentModifiers.motorRestrictionUpper}
        });

        modifierUpdateEvent.Invoke("MotorRestrictionUpper", value.ToString());
    }

    public void SetMotorRestrictionLower(float value)
    {
        if (currentModifiers.motorRestrictionLower == value) return;

        currentModifiers.motorRestrictionLower = value;

        // Raises an Event and updates a PersistentEvent's parameter (in consequence, a PersistentEvent will also be raised)
        loggerNotifier.NotifyLogger("Motor Restriciton Lower Set to " + value, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"MotorRestrictionLower", currentModifiers.motorRestrictionLower},
            {"MotorRestrictionUpper", currentModifiers.motorRestrictionUpper.GetValueOrDefault(-1f)}
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
        if (currentModifiers.mirrorEffect == value) return;
        if (!getActivePointer(controllersList["main"]).isActiveAndEnabled) return;

        currentModifiers.mirrorEffect = value;

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
        if (currentModifiers.physicalMirrorEffect == value) return;

        currentModifiers.physicalMirrorEffect = value;

        physicalMirror.SetActive(value);
    }

    public void SetGeometricMirror(bool value)
    {
        if (currentModifiers.geometricMirrorEffect == value) return;

        currentModifiers.geometricMirrorEffect = value;

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
            if (currentModifiers.controllerSetup == ModifiersManager.ControllerSetup.Right)
            {
                mirrorControllerL.SetActive(false);
                mirrorControllerR.SetActive(true);
                VRBody.gameObject.GetComponent<VRBodyEmbodimentManager>().SetMirrorRightEmbodiment(true);
            }
            else
            {
                mirrorControllerR.SetActive(false);
                mirrorControllerL.SetActive(true);
                VRBody.gameObject.GetComponent<VRBodyEmbodimentManager>().SetMirrorLeftEmbodiment(true);
            }

            if (currentModifiers.embodiment != Embodiment.Hands && currentModifiers.embodiment != Embodiment.Arms &&
            currentModifiers.embodiment != Embodiment.Full) {
                geometricEmbodiment = currentModifiers.embodiment.Value;
            }

            if (currentModifiers.embodiment == Embodiment.LeftHand || currentModifiers.embodiment == Embodiment.RightHand) {
                SetEmbodiment(Embodiment.Hands);
            } else if (currentModifiers.embodiment == Embodiment.LeftArm || currentModifiers.embodiment == Embodiment.RightArm) {
                SetEmbodiment(Embodiment.Arms);
            }
        }
        else
        {
            mirrorControllerL.SetActive(false);
            mirrorControllerR.SetActive(false);
            VRBody.gameObject.GetComponent<VRBodyEmbodimentManager>().SetMirrorLeftEmbodiment(false);
            VRBody.gameObject.GetComponent<VRBodyEmbodimentManager>().SetMirrorRightEmbodiment(false);
            SetEmbodiment(geometricEmbodiment);
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
        currentModifiers.controllerOffset = value;

        // Before calibration was implemented, controllers
        // were offset by setting their world position.
        // However, with calibration, this results in
        // undefined behavior. A temporary fix was to
        // use localPosition, but the localPosition is
        // rarely aligned to world axes after calibration.
        // This implements a helper function which
        // reads the parents' rotation and compensates for
        // it, when sertting the controller's local position.
        Vector3 xOffset = new Vector3(currentModifiers.controllerOffset.Value * 0.1f, 0f, 0f);
        Transform controllerParent = rightControllerContainer.parent;
        Vector3 rotatedVector = RotateTowardsUp(xOffset, controllerParent.eulerAngles.y);

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
        currentModifiers.prismOffset = value;

        prismOffsetObject.transform.localEulerAngles = new Vector3(0, currentModifiers.prismOffset.Value, 0);

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
        currentModifiers.controllerSetup = controller;

        SetControllerEnabled(controller);
        if (currentModifiers.controllerSetup == ModifiersManager.ControllerSetup.Left)
        {
            controllersList["main"] = leftController;
            controllersList["second"] = rightController;
        }
        else // Right and Both
        {
            controllersList["main"] = rightController;
            controllersList["second"] = leftController;
        }

        if (currentModifiers.mirrorEffect.Value)
        {
            UpdateMirrorEffect();
        }

        if (currentModifiers.geometricMirrorEffect.Value)
        {
            UpdateGeometricMirror(currentModifiers.geometricMirrorEffect.Value);
        }

        string controllerName = currentModifiers.controllerSetup.HasValue ? System.Enum.GetName(typeof(ControllerSetup), currentModifiers.controllerSetup.Value) : "Unknown";
        loggerNotifier.NotifyLogger("Controller Main Set " + controllerName, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"ControllerMain", controllerName}
        });
    }

    public UnityEvent<string, string> GetModifierUpdateEvent()
    {
        return modifierUpdateEvent;
    }

    // Updates the mirroring effect. Is called when enabling/disabling the mirror effect or when controllers are activated/deactivated (dual task, main controller change).
    private void UpdateMirrorEffect()
    {
        if (currentModifiers.mirrorEffect.Value)
        {
            leftControllerContainer.localScale = new Vector3(-1, 1, 1);
            rightControllerContainer.localScale = new Vector3(-1, 1, 1);
            leftControllerContainer.localPosition = new Vector3(-0.44f, 0, 0);
            rightControllerContainer.localPosition = new Vector3(0.44f, 0, 0);
            //controllersList["main"].gameObject.GetComponent<ControllerModifierManager>().EnableMirror(viveCamera.transform, wallReference);

            //if (!dualTask)
            //{
            //    controllersList["second"].gameObject.GetComponent<ControllerModifierManager>().DisableMirror();
            //}
            //else
            //{
            //    controllersList["second"].gameObject.GetComponent<ControllerModifierManager>().EnableMirror(viveCamera.transform, wallReference);
            //}
        }
        else
        {
            leftControllerContainer.localScale = new Vector3(1, 1, 1);
            rightControllerContainer.localScale = new Vector3(1, 1, 1);
            leftControllerContainer.localPosition = new Vector3(0, 0, 0);
            rightControllerContainer.localPosition = new Vector3(0, 0, 0);
            //controllersList["main"].gameObject.GetComponent<ControllerModifierManager>().DisableMirror();

            //if (dualTask)
            //{
            //    controllersList["second"].gameObject.GetComponent<ControllerModifierManager>().DisableMirror();
            //}
        }
    }
    public void SetJudgementType(JudgementType value)
    {
        if (currentModifiers.judgementType == value) return;
        performanceManager.SetJudgementType(value);

        // Raises an Event and updates a PersistentEvent's parameter (in consequence, a PersistentEvent will also be raised)
        loggerNotifier.NotifyLogger($"Judgement Type Set {Enum.GetName(typeof(JudgementType), value)}", EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"JudgementType", Enum.GetName(typeof(JudgementType), value)}
        });

        modifierUpdateEvent.Invoke($"JudgementType", Enum.GetName(typeof(JudgementType), value));

        currentModifiers.judgementType = value;
    }

    // Sets the level of embodiment used by the game. (Show hands (including controller) or just cursor).
    public void SetEmbodiment(Embodiment value)
    {
        if (currentModifiers.embodiment == value) return;

        currentModifiers.embodiment = value;

        // Pass embodiment on to the ControllerModifierManager.
        controllersList["main"].gameObject.GetComponent<ControllerModifierManager>().SetEmbodiment(currentModifiers.embodiment.Value);
        controllersList["second"].gameObject.GetComponent<ControllerModifierManager>().SetEmbodiment(currentModifiers.embodiment.Value);

        VRBody.gameObject.GetComponent<VRBodyEmbodimentManager>().SetEmbodiment(currentModifiers.embodiment.Value);

        loggerNotifier.NotifyLogger("Embodiment Set " + System.Enum.GetName(typeof(Embodiment), currentModifiers.embodiment), EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"Embodiment", System.Enum.GetName(typeof(Embodiment), currentModifiers.embodiment)}
        });

    }

    // Enables/disables a given controller
    private void SetControllerEnabled(ControllerSetup? controllerType, bool enabled = true)
    {
        currentModifiers.controllerSetup = controllerType;

        bool enableRight = (controllerType == ControllerSetup.Right || controllerType == ControllerSetup.Both);
        bool enableLeft = (controllerType == ControllerSetup.Left || controllerType == ControllerSetup.Both);

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
        string controllerName = currentModifiers.controllerSetup.HasValue ? System.Enum.GetName(typeof(ControllerSetup), currentModifiers.controllerSetup.Value) : "Unknown";
        loggerNotifier.NotifyLogger("Controller Main Set " + controllerName, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"ControllerMain", controllerName}
        });

        loggerNotifier.NotifyLogger("Prism Offset Set " + currentModifiers.prismOffset, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"PrismOffset", currentModifiers.prismOffset}
        });
        loggerNotifier.NotifyLogger("Controller Offset Set " + currentModifiers.controllerOffset, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"ControllerOffset", currentModifiers.controllerOffset}
        });
        loggerNotifier.NotifyLogger("Geometric Mirror Set " + currentModifiers.geometricMirrorEffect.ToString(), EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"GeometricMirror", currentModifiers.geometricMirrorEffect.ToString()}
        });
        loggerNotifier.NotifyLogger("Motor Restriciton Lower Set to " + currentModifiers.motorRestrictionLower, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"MotorRestrictionLower", currentModifiers.motorRestrictionLower},
            {"MotorRestrictionUpper", currentModifiers.motorRestrictionUpper}
        });
        loggerNotifier.NotifyLogger("Motor Restriciton Upper Set to " + currentModifiers.motorRestrictionUpper, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"MotorRestrictionLower", currentModifiers.motorRestrictionLower},
            {"MotorRestrictionUpper", currentModifiers.motorRestrictionUpper}
        });
        loggerNotifier.NotifyLogger("Motor Restriciton Set " + currentModifiers.motorRestriction, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"MotorRestrictionLower", currentModifiers.motorRestrictionLower},
            {"MotorRestrictionUpper", currentModifiers.motorRestrictionUpper}
        });
        loggerNotifier.NotifyLogger("Hide Wall Amount: " + currentModifiers.hideWallAmount.GetValueOrDefault(-1f), EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"HideWallAmount", currentModifiers.hideWallAmount.GetValueOrDefault(-1f)}
        });
        loggerNotifier.NotifyLogger("Hide Wall Effect Set " + currentModifiers.hideWall, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"HideWall", currentModifiers.hideWall}
        });
        loggerNotifier.NotifyLogger("Embodiment Set " + System.Enum.GetName(typeof(Embodiment), currentModifiers.embodiment), EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"Embodiment", currentModifiers.embodiment}
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
