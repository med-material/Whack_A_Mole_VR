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
    public enum ControllerSetup {Left, Both, Right};
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
    private UnityEngine.UI.Slider prismEffectSlider;

    [SerializeField]
    private Pointer rightController;

    [SerializeField]
    private GameObject[] rightControllerVisuals;

    [SerializeField]
    private GameObject mirrorControllerR;

    [SerializeField]
    private GameObject mirrorControllerL;

    [SerializeField]
    private GameObject mirrorMotorSpace;

    [SerializeField]
    private Pointer leftController;

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

    private EyePatch eyePatch = EyePatch.None;
    private HideWall hideWall = HideWall.None;
    private ControllerSetup controllerSetup = ControllerSetup.Right;
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
        SetControllerEnabled(ControllerSetup.Right);

        // Initialization of the LoggerNotifier. Here we will only pass parameters to PersistentEvent, even if we will also raise Events.
        loggerNotifier = new LoggerNotifier(persistentEventsHeadersDefaults: new Dictionary<string, string>(){
            {"RightControllerMain", "Undefined"},
            {"MirrorEffect", "No Mirror Effect Defined"},
            {"EyePatch", "No Eye Patch Defined"},
            {"ControllerOffset", "No Controller Offset Defined"},
            {"PrismOffset", "No Controller Offset Defined"},
            {"DualTask", "No Dual Task Defined"},
            {"HideWall", "No Hide Wall Defined"},
            {"HideWallAmount", "No Hide Wall Amount Defined"}
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

        loggerNotifier.NotifyLogger("Hide Wall Effect Set "+ value, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"HideWall", value}
        });

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

    public void SetHideWallAmount(float value) {
        hideWallSlider.value = value;
    }

    public void onHideWallSliderChanged() {
        var sliderValue = (float) hideWallSlider.value;
        var highVal = (float) hideWallSlider.maxValue;
        var lowVal = (float) hideWallSlider.minValue;
        hideWallAmount = ((sliderValue - lowVal) / highVal);
        var multiplier = 1 - hideWallAmount;
        var startRange = hideWallHighestStart - hideWallLowestStart;
        var endRange = hideWallHighestEnd - hideWallLowestEnd;

        var newStart = (startRange * multiplier) + hideWallLowestStart;
        var newEnd = (endRange * multiplier) + hideWallLowestEnd;

        hideWallLeftMat.SetFloat("_FogMaxHeight", -newStart);
        hideWallLeftMat.SetFloat("_FogMinHeight", -newEnd);
        hideWallRightMat.SetFloat("_FogMaxHeight", newStart);
        hideWallRightMat.SetFloat("_FogMinHeight", newEnd);

        loggerNotifier.NotifyLogger("Hide Wall Amount: "+ hideWallAmount, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"HideWallAmount", hideWallAmount}
        });
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
        UpdateGeometricMirror(value);
    }

    public void UpdateGeometricMirror(bool enable) {
        if (enable) {
            if (controllerSetup == ModifiersManager.ControllerSetup.Right) {
                mirrorControllerR.SetActive(true);
                mirrorControllerL.SetActive(false);
            } else {
                mirrorControllerL.SetActive(true);
                mirrorControllerR.SetActive(false);
            }
        } else {
            mirrorControllerL.SetActive(false);
            mirrorControllerR.SetActive(false);
        }
    }

    // Sets the prism effect. Shifts the view (around y axis) by a given angle to create a shifting between seen view and real positions.
    public void SetControllerOffset(float value)
    {
        controllerOffset = value;
        rightControllerContainer.localEulerAngles = new Vector3(0, controllerOffset, 0);
        rightControllerContainer.localEulerAngles = new Vector3(0, controllerOffset, 0);
        leftControllerContainer.localEulerAngles = new Vector3(0, controllerOffset, 0);

        loggerNotifier.NotifyLogger("Controller Offset Set "+value, EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
        {
            {"ControllerOffset", value}
        });

        //modifierUpdateEvent.Invoke("ControllerOffset", value.ToString());
    }

    public void OnControllerOffsetSliderChanged()
    {
        var sliderValue = (float) controllerOffsetSlider.value;

        SetControllerOffset(sliderValue);
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

        //modifierUpdateEvent.Invoke("PrismOffset", value.ToString());
    }

    public void OnPrismOffsetSliderChanged()
    {
        var sliderValue = (float) prismEffectSlider.value;

        SetPrismOffset(sliderValue);
    }

    public void SetMainControllerFromString(string controller) {
        SetMainController((ModifiersManager.ControllerSetup)System.Enum.Parse( typeof(ModifiersManager.ControllerSetup), controller));
    }

    // Sets the main controller. By default it is the right handed one.
    public void SetMainController(ModifiersManager.ControllerSetup controller)
    {
        if (controllerSetup == controller) return;

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

        if (geometricMirrorEffect) {
            UpdateGeometricMirror(geometricMirrorEffect);
        }

        loggerNotifier.NotifyLogger("Controller Main Set "+System.Enum.GetName(typeof(ControllerSetup), controllerSetup), EventLogger.EventType.ModifierEvent, new Dictionary<string, object>()
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

    // Enables/disables a given controller
    private void SetControllerEnabled(ControllerSetup controllerType, bool enabled = true)
    {
        bool enableRight;
        enableRight = controllerType == ControllerSetup.Right ? true : false;
        enableRight = controllerType == ControllerSetup.Both ? true : enableRight;

        bool enableLeft;
        enableLeft = controllerType == ControllerSetup.Left ? true : false;
        enableLeft = controllerType == ControllerSetup.Both ? true : enableLeft;
        Debug.Log("Enable Right: " + enableRight + " Enable Left: " + enableLeft);

        if (enableRight)
        {
            rightController.Enable();
            foreach (var obj in rightControllerVisuals) {
                obj.SetActive(true);
            }
        } else {
            rightController.Disable();
            foreach (var obj in rightControllerVisuals) {
                obj.SetActive(false);
            }
        }

        if (enableLeft)
        {
            leftController.Enable();
            foreach (var obj in leftControllerVisuals) {
                obj.SetActive(true);
            }
        } else {
            leftController.Disable();
            foreach (var obj in leftControllerVisuals) {
                obj.SetActive(false);
            }
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
