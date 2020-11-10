 using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using UnityEngine.Events;

[System.Serializable]
public class ModifierUpdateEvent : UnityEvent<string, string> {}


/*
Manages different VR modifiers, which are setting the main hand, dual-task mode, eye-patch, mirror mode and prism offset.

WARNING: Due to the Vive overlay, it is necessary to disable the chaperone bounds in the Vive settings, otherwise the eye patch would still render the chaperone if near a boundary.
To do so, go the the VR settings WHILE INSIDE THE HEADSET -> Chaperone -> select DEVELOPER MODE and set a neutral color with the lowest opacity possible.
It is also possible to fully hide the chaperone by editing the steamvr.vrsettings file and setting "CollisionBoundsColorGammaA" to 0.
*/


public class ModifiersManager : MonoBehaviour
{
    public enum EyePatch {Left, None, Right};
    public enum HideWall {Left, None, Right};

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
    private Pointer rightController;

    [SerializeField]
    private GameObject rightControllerVisual;

    [SerializeField]
    private GameObject rightControllerBubbleDisplay;

    [SerializeField]
    private GameObject mirrorController;

    [SerializeField]
    private GameObject mirrorMotorSpace;

    [SerializeField]
    private Pointer leftController;

    [SerializeField]
    private GameObject leftControllerVisual;

    [SerializeField]
    private GameObject leftControllerBubbleDisplay;

    [SerializeField]
    private Transform rightControllerContainer;

    [SerializeField]
    private Transform leftControllerContainer;

    [SerializeField]
    private GameObject prismOffsetObject;

    [SerializeField]
    private Camera viveCamera;

    [SerializeField]
    private Transform wallReference;

    [SerializeField]
    private GameObject physicalMirror;

    [SerializeField]
    private float hideWallhighestStart = 1.3f;
    [SerializeField]
    private float hideWallhighestEnd = 0.6f;
    [SerializeField]
    private float hideWalllowestStart = -0.2f;
    [SerializeField]
    private float hideWalllowestEnd = -1.05f;

    private EyePatch eyePatch = EyePatch.None;
    private HideWall hideWall = HideWall.None;
    private bool mirrorEffect;
    private bool physicalMirrorEffect;
    private bool geometricMirrorEffect;
    private bool dualTask;
    private bool rightControllerMain;
    private float controllerOffset;
    private float prismOffset;
    private Dictionary<string, Pointer> controllersList;
    private LoggerNotifier loggerNotifier;
    private ModifierUpdateEvent modifierUpdateEvent = new ModifierUpdateEvent();

    void Awake()
    {
        controllersList = new Dictionary<string, Pointer>(){
            {"main", rightController},
            {"second", leftController}
        };
        SetControllerEnabled("main");
        SetControllerEnabled("second", false);

        // Initialization of the LoggerNotifier. Here we will only pass parameters to PersistentEvent, even if we will also raise Events.
        loggerNotifier = new LoggerNotifier(persistentEventsHeadersDefaults: new Dictionary<string, string>(){
            {"RightControllerMain", "Undefined"},
            {"MirrorEffect", "No Mirror Effect Defined"},
            {"EyePatch", "No Eye Patch Defined"},
            {"ControllerOffset", "No Controller Offset Defined"},
            {"PrismOffset", "No Controller Offset Defined"},
            {"DualTask", "No Dual Task Defined"}
        });
        // Initialization of the starting values of the parameters.
        loggerNotifier.InitPersistentEventParameters(new Dictionary<string, object>(){
            {"RightControllerMain", rightControllerMain},
            {"MirrorEffect", mirrorEffect},
            {"EyePatch", System.Enum.GetName(typeof(ModifiersManager.EyePatch), eyePatch)},
            {"ControllerOffset", controllerOffset},
            {"PrismOffset", controllerOffset},
            {"DualTask", dualTask}
        });
    }

    // Sets an eye patch. Calls WaitForCameraAndUpdate coroutine to set eye patch.
    public void SetEyePatch(EyePatch value)
    {
        if (eyePatch == value) return;
        eyePatch = value;
        StartCoroutine(WaitForCameraAndUpdate(eyePatch));
    }

    public void SetHideWall(HideWall value) {
        if (hideWall == value) return;
        hideWall = value;

        if (hideWall == HideWall.Left) {
            hideWallLeft.SetActive(true);
            hideWallRight.SetActive(false);
            onHideWallSliderChanged();
        } else if (hideWall == HideWall.Right) {
            hideWallLeft.SetActive(false);
            hideWallRight.SetActive(true);
            onHideWallSliderChanged();
        } else if (hideWall == HideWall.None) {
            hideWallLeft.SetActive(false);
            hideWallRight.SetActive(false);
        }
    }

    public void onHideWallSliderChanged() {
        var sliderValue = (float) hideWallSlider.value;
        var highVal = (float) hideWallSlider.maxValue;
        var lowVal = (float) hideWallSlider.minValue;
        var multiplier = 1 - ((sliderValue - lowVal) / highVal);
        var startRange = hideWallhighestStart - hideWalllowestStart;
        var endRange = hideWallhighestEnd - hideWalllowestEnd;

        var newStart = (startRange * multiplier) + hideWalllowestStart;
        var newEnd = (endRange * multiplier) + hideWalllowestEnd;

        hideWallLeftMat.SetFloat("_FogMaxHeight", -newStart);
        hideWallLeftMat.SetFloat("_FogMinHeight", -newEnd);
        hideWallRightMat.SetFloat("_FogMaxHeight", newStart);
        hideWallRightMat.SetFloat("_FogMinHeight", newEnd);

        // 1.3:-0.2
        // 0.6:-1.05

        // 1 = 1.3, 0.6
        // 2 = 1, 0.0
        // 3 = 0.6, -0.3
        // 4 = 0.35, -0.55
        // 5 = -0.2, -1.05
    }

    // Sets a controller position and rotation's mirroring effect. Calls UpdateMirrorEffect to set the mirror.
    public void SetMirrorEffect(bool value)
    {
        if (mirrorEffect == value) return;
        if (!controllersList["main"].isActiveAndEnabled) return;

        mirrorEffect = value;
        UpdateMirrorEffect();

        // Raises an Event and updates a PersistentEvent's parameter (in consequence, a PersistentEvent will also be raised)
        loggerNotifier.NotifyLogger("Mirror Effect Set "+value, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
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
        mirrorMotorSpace.SetActive(value);
        mirrorController.SetActive(value);
    }

    // Sets the dual task mode (if dualtask is enabled, both controllers can be used to pop moles)
    public void SetDualTask(bool value)
    {
        if (dualTask == value) return;
        dualTask = value;
        SetControllerEnabled("second", dualTask);

        if (mirrorEffect)
        {
            UpdateMirrorEffect();
        }

        loggerNotifier.NotifyLogger("Dual Task Set "+value, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"DualTask", value}
        });

        modifierUpdateEvent.Invoke("DualTask", value.ToString());
    }

    // Sets the prism effect. Shifts the view (around y axis) by a given angle to create a shifting between seen view and real positions.
    public void SetControllerOffset(float value)
    {
        controllerOffset = value;
        rightControllerContainer.localEulerAngles = new Vector3(0, controllerOffset, 0);
        leftControllerContainer.localEulerAngles = new Vector3(0, controllerOffset, 0);

        loggerNotifier.NotifyLogger("Controller Offset Set "+value, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"ControllerOffset", value}
        });

        modifierUpdateEvent.Invoke("ControllerOffset", value.ToString());
    }

    // Sets the prism effect. Shifts the view (around y axis) by a given angle to create a shifting between seen view and real positions.
    public void SetPrismOffset(float value)
    {
        prismOffset = value;
        prismOffsetObject.transform.localEulerAngles = new Vector3(0, prismOffset, 0);

        loggerNotifier.NotifyLogger("Prism Offset Set "+value, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"PrismOffset", value}
        });

        modifierUpdateEvent.Invoke("PrismOffset", value.ToString());
    }

    // Sets the main controller. By default it is the right handed one.
    public void SetMainController(bool rightIsMain)
    {
        if (rightControllerMain == rightIsMain) return;

        rightControllerMain = rightIsMain;
        if (rightControllerMain)
        {
            controllersList["main"] = rightController;
            controllersList["second"] = leftController;
        }
        else
        {
            controllersList["main"] = leftController;
            controllersList["second"] = rightController;
        }
        SetControllerEnabled("main");

        if (mirrorEffect)
        {
            UpdateMirrorEffect();
        }

        if (!dualTask) SetControllerEnabled("second", false);

        loggerNotifier.NotifyLogger("Right Controller Set Main "+rightIsMain, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"RightControllerMain", rightIsMain}
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

    // Enables/disables a given controller
    private void SetControllerEnabled(string controllerType, bool enable = true)
    {
        if (enable)
        {
            controllersList[controllerType].Enable();
        }
        else
        {
            controllersList[controllerType].Disable();
        }
        if (controllerType == "main") {
            rightControllerVisual.SetActive(enable);
            rightControllerBubbleDisplay.SetActive(enable);
        } else {
            leftControllerVisual.SetActive(enable);
            leftControllerBubbleDisplay.SetActive(enable);
        }
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

        loggerNotifier.NotifyLogger("Eye Patch Set "+System.Enum.GetName(typeof(ModifiersManager.EyePatch), value), EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"EyePatch", System.Enum.GetName(typeof(ModifiersManager.EyePatch), value)}
        });

        modifierUpdateEvent.Invoke("EyePatch", System.Enum.GetName(typeof(ModifiersManager.EyePatch), value));
    }
}
