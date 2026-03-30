using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
using XRHands = UnityEngine.XR.Hands;
using XRManagement = UnityEngine.XR.Management;
using Valve.VR;

// Shared interface used by every task script controlled by SandboxRunner.
//
// Why this exists:
// SandboxRunner should be able to control OpenLoop, LineBisection, Landmark, and Exposure
// in a uniform way instead of needing completely separate hard-coded logic for each one.
//
// What the two members mean:
// - TaskMode tells SandboxRunner what kind of task this script represents.
// - SetTaskActive lets SandboxRunner switch that task on or off when the experiment
//   changes phase.
//
// In plain language:
// this is the small "contract" every task script agrees to follow so the central runner
// can treat all tasks in a consistent way.
public interface ISandboxTask
{
SandboxRunner.TaskMode TaskMode { get; }
void SetTaskActive(bool active);
}

// Optional interface for tasks that can expose a concrete target position in world space.
//
// Not every task has a single physical target that can be hovered over.
// Open-loop pointing and exposure do, but something like Landmark is more of a comparison
// task than a "hover over this point" task.
//
// SandboxRunner uses this interface for controller hover-confirm.
// To auto-confirm, it must know:
// - where the current target center is in the virtual world,
// - which direction the target plane is facing,
// - and how large the target region is.
//
// If a task cannot answer those questions, hover-confirm is not possible and the runner
// falls back to a more ordinary confirmation method.
public interface IAimTargetProvider
{
bool TryGetAimTarget(out Vector3 worldCenter, out Vector3 planeNormal, out float targetRadiusMeters);
}

// SandboxRunner is the central coordinator for the whole prototype.
//
// This script is best understood as the "traffic controller" of the experiment.
// It does not itself decide where a line midpoint is or how exposure targets behave;
// those details live inside the individual task scripts. Instead, SandboxRunner
// takes care of the cross-cutting responsibilities that every task depends on.
//
// The most important responsibilities are:
// 1. choosing which task is active right now,
// 2. choosing which visuomotor effect is active right now,
// 3. reading XR input from controllers or hands,
// 4. turning that input into one unified format for the tasks,
// 5. calibrating the participant's position/orientation,
// 6. progressing the experiment between baseline, exposure, and post,
// 7. exposing useful context to the logging system.
//
// If a task script asks "where is the participant aiming?" or "was a response confirmed?",
// the answer usually comes from SandboxRunner.
public class SandboxRunner : MonoBehaviour
{
public enum EffectMode { None, Translation, Rotation, Skew }
public enum TaskMode { OpenLoop, LineBisection, Landmark, Exposure }
public enum Handedness { Left, Right }
public enum XRBackend { SteamVR, OpenXR }
public enum OpenXRTrackingMode { Controllers, Hands }

[Header("Mode")]
[SerializeField] private EffectMode effectMode = EffectMode.None;
[Tooltip("Active mode. Exposure is entered via key or automatically.")]
[SerializeField] private TaskMode taskMode = TaskMode.OpenLoop;

[Header("Scene References")]
 [SerializeField] private Camera mainCam;
 [SerializeField] private Transform visualWorldRoot;
 [SerializeField] private TextMeshProUGUI statReadout;
 [SerializeField] private PrismExperimentLogger experimentLogger;


[Header("XR Input")]
[SerializeField] private XRBackend xrBackend = XRBackend.OpenXR;
[SerializeField] private OpenXRTrackingMode openXRTrackingMode = OpenXRTrackingMode.Hands;
[SerializeField] private Handedness activeHand = Handedness.Right;
[SerializeField] private float handDwellSeconds = 2f;
[SerializeField, Tooltip("Lower values are more responsive. 0 disables smoothing.")]
private float handRaySmoothingSeconds = 0.06f;
[SerializeField, HideInInspector] private GameObject openXRLeftHandTrackingPrefab;
[SerializeField, HideInInspector] private GameObject openXRRightHandTrackingPrefab;

[Header("Controller Confirm Mitigation")]
[Tooltip("Standard controller behavior: the aiming hand trigger confirms.")]
public bool noHeisenbergMitigation = true;
[Tooltip("Use the opposite hand trigger to confirm while aiming with the dominant hand.")]
public bool useOppositeHandTrigger = false;
[Tooltip("Auto-confirm after dwelling near the current task target with the controller aim.")]
public bool useControllerHoverConfirm = false;
[SerializeField, Min(0.05f)] private float controllerHoverSeconds = 1.0f;
[SerializeField, Min(0.01f)] private float controllerHoverActivationRadiusMeters = 0.50f;

[Header("Task Transition")]
[SerializeField] private float taskTransitionSeconds = 2f;

[Header("Calibration")]
[SerializeField] private Transform rigRoot;
[SerializeField] private Transform hmd;
[SerializeField, Tooltip("Preferred head-height anchor. The HMD height is aligned to this object.")]
private Transform boardMid;
[SerializeField, Tooltip("Preferred body-center anchor. The rig root horizontal position is aligned to this object.")]
private Transform bodyCenterAnchor;
[SerializeField] private bool calibrateOnStart = true;
[SerializeField] private bool recenterXRTrackingOnCalibrate = true;
private float calibrateOnStartDelaySeconds = 0.25f;
private SteamVR_Action_Boolean _calibrateAction;
private SteamVR_Input_Sources calibrationHand = SteamVR_Input_Sources.RightHand;

[Header("Tasks")]
private MonoBehaviour openLoopTask;
private MonoBehaviour lineBisectionTask;
private MonoBehaviour landmarkTask;
private MonoBehaviour exposureTask;

[Header("Effects")]
[SerializeField] private TranslationEffect translation = new();
[SerializeField] private RotationEffect rotation = new();
[SerializeField] private SkewEffect skew = new();

[Header("Experiment Flow")]
[SerializeField] private KeyCode enterExposureKey = KeyCode.E;
[SerializeField] private KeyCode restartBaselineKey = KeyCode.R;
 [SerializeField] private bool autoProgressExperiment = true;
 [SerializeField] private bool stopPlayModeWhenExperimentCompletes = true;

private bool autoReturnFromExposure = true;

[Header("Debug")]
[SerializeField] private bool drawDebugRays = true;

private LineRenderer _rawRayLine;
private LineRenderer _transformedRayLine;
private Transform _rayDebugRoot;

private IEffectTransform _effect;

private Transform _leftControllerTransform;
private Transform _rightControllerTransform;
private Transform _leftControllerRayOriginTransform;
private Transform _rightControllerRayOriginTransform;
private Transform _leftPointerVisualTransform;
private Transform _rightPointerVisualTransform;
private readonly List<Transform> _leftAuxPointerVisualTransforms = new();
private readonly List<Transform> _rightAuxPointerVisualTransforms = new();
private Transform _leftHandVisualTransform;
private Transform _rightHandVisualTransform;
private GameObject _openXRHandVisualizerRoot;
private SteamVR_Action_Boolean _confirmAction;
private bool _confirmDown;
private bool _confirmPressedLastFrameLeft;
private bool _confirmPressedLastFrameRight;
private bool _calibrateDownLastFrame;
private bool _handPointingActive;
private float _handPointingStartTime = -1f;
private float _handDwellProgress01;
private bool _handDwellTriggered;
private bool _confirmDwellActive;
private float _confirmDwellProgress01;
private float _controllerHoverStartTime = -1f;
private bool _controllerHoverTriggered;
private bool _controllerHoverHadTargetLastFrame;
private Vector3 _controllerHoverLastTargetCenter;
private bool _smoothedHandRayInitialized;
private Handedness _smoothedHandRayHand;
private Vector3 _smoothedHandRayOrigin;
private Vector3 _smoothedHandRayDirection = Vector3.forward;
private bool _prevNoHeisenbergMitigation = true;
private bool _prevUseOppositeHandTrigger;
private bool _prevUseControllerHoverConfirm;
private TaskMode? _hoverFallbackWarnedTask;

public EffectMode CurrentEffectMode => effectMode;
public TaskMode CurrentTaskMode => taskMode;
public XRBackend CurrentXRBackend => xrBackend;
public OpenXRTrackingMode CurrentOpenXRTrackingMode => openXRTrackingMode;
public Handedness CurrentActiveHand => activeHand;
public EffectMode CurrentAppliedEffectMode => taskMode == TaskMode.Exposure ? effectMode : EffectMode.None;
public bool IsHandPointing => _handPointingActive;
public float HandDwellProgress01 => _handDwellProgress01;
public bool IsConfirmDwellActive => _confirmDwellActive;
public float ConfirmDwellProgress01 => _confirmDwellProgress01;
public bool IsExperimentCompleted => _experimentCompleted;
public string CurrentConfirmMitigationMode
{
    get
    {
        if (useControllerHoverConfirm)
            return "ControllerHoverConfirm";
        if (useOppositeHandTrigger)
            return "OppositeHandTrigger";
        return "NoMitigation";
    }
}
public bool IsTaskTransitionActive => _taskTransitionActive;
public float TaskTransitionProgress01
{
    get
    {
        if (!_taskTransitionActive)
            return 0f;

        float duration = Mathf.Max(0.01f, _taskTransitionDuration);
        return Mathf.Clamp01((Time.time - _taskTransitionStartTime) / duration);
    }
}
private EffectMode _lastEffectMode;
private TaskMode _lastTaskMode;
private Handedness _lastActiveHand;
private XRBackend _lastXRBackend;
private OpenXRTrackingMode _lastOpenXRTrackingMode;

private TaskMode _measurementTaskBeforeExposure = TaskMode.OpenLoop;

private float _pendingExposureReturnTime = -1f;
private bool _waitingForExposureReturn;
private XRHands.XRHandSubsystem _xrHandSubsystem;
private bool _experimentCompleted;
private bool _taskTransitionActive;
private float _taskTransitionStartTime;
private float _taskTransitionDuration;
private Coroutine _taskTransitionCoroutine;

// Initial scene setup. This is the main entry point when play mode starts.
//
// Read this method as the script's boot sequence:
// - remember the current inspector settings so later changes can be detected,
// - fill in missing scene references automatically,
// - prepare the XR input objects,
// - make sure the correct visuals/effect/task are active,
// - optionally calibrate the participant,
// - and, if the current task is a measurement task, begin its baseline block.
//
// This means that simply pressing Play is usually enough to make the experiment
// start in a sensible state without additional manual setup.
void Start()
{
    _lastEffectMode = effectMode;
    _lastTaskMode = taskMode;
    _lastActiveHand = activeHand;
    _lastXRBackend = xrBackend;
    _lastOpenXRTrackingMode = openXRTrackingMode;
    SyncControllerMitigationPreviousState();

    AutoAssignReferences();
    AutoAssignXRInput();
    UpdateOpenXRVisualMode();
    SelectEffect();
    ApplyTaskMode();

    if (calibrateOnStart)
        StartCoroutine(CalibrateOnStartRoutine());

    if (IsMeasurementTask(taskMode))
        BeginMeasurementBlock(taskMode, "Baseline");
}

// Startup calibration waits briefly for XR tracking to become valid.
//
// Why this matters:
// XR systems often need a short moment after play mode starts before the headset pose
// becomes meaningful. If calibration happened immediately, the script might calibrate
// against a default zero pose rather than the participant's actual position.
//
// So this routine does three things:
// - keep checking whether the HMD pose looks valid,
// - wait a short additional delay for stability,
// - then perform the real calibration.
IEnumerator CalibrateOnStartRoutine()
{
    // Wait until XR tracking is live so startup calibration uses a real HMD pose.
    const float maxWaitSeconds = 5f;
    float startTime = Time.time;

    while (Time.time - startTime < maxWaitSeconds)
    {
        AutoAssignReferences();

        if (HasUsableHmdPose())
            break;

        yield return null;
    }

    if (calibrateOnStartDelaySeconds > 0f)
        yield return new WaitForSeconds(calibrateOnStartDelaySeconds);

    CalibrateNow();
}

// Checks whether the headset pose appears "real" rather than still being at a default value.
//
// For a non-coder, the important idea is:
// the script is trying to detect whether the XR hardware is actually awake and tracked yet.
//
// The two checks used here are:
// - position is not still almost exactly zero,
// - rotation is not still almost exactly the identity rotation.
//
// If both still look like defaults, the script assumes it is too early to trust the HMD pose.
bool HasUsableHmdPose()
{
    if (hmd == null)
        return false;

    if (xrBackend == XRBackend.OpenXR)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (!device.isValid)
            return false;

        bool hasPosition = device.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 eyePos);
        bool hasRotation = device.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion eyeRot);

        if (hasPosition && hasRotation)
            return eyePos.sqrMagnitude > 0.0001f || Quaternion.Angle(eyeRot, Quaternion.identity) > 0.01f;
    }

    return hmd.position.sqrMagnitude > 0.0001f || Quaternion.Angle(hmd.rotation, Quaternion.identity) > 0.01f;
}

// Per-frame update loop.
//
// This is the method that continually keeps the experiment "alive".
// A useful way to read it is as a checklist that happens every frame:
//
// 1. react to debug keyboard shortcuts,
// 2. see whether exposure should automatically return to post,
// 3. detect whether inspector settings changed while running,
// 4. refresh input and confirmation state,
// 5. make sure the correct effect references are wired up,
// 6. apply the active camera effect,
// 7. refresh the debug rays.
//
// The repeated comparisons like "_lastEffectMode != effectMode" exist so the runner
// only rebuilds state when something actually changed, instead of doing everything
// from scratch every frame.
void Update()
{
    HandleKeyboardShortcuts();

    if (_waitingForExposureReturn && Time.time >= _pendingExposureReturnTime)
    {
        _waitingForExposureReturn = false;
        ReturnFromExposureToPost();
    }

    if (_lastEffectMode != effectMode)
    {
        _lastEffectMode = effectMode;
        SelectEffect();
    }

    if (_lastTaskMode != taskMode)
    {
        _lastTaskMode = taskMode;
        ApplyTaskMode();
        SelectEffect();
    }

    if (_lastActiveHand != activeHand)
    {
        _lastActiveHand = activeHand;
        ResetHandRaySmoothing();
        SelectEffect();
    }

    if (_lastXRBackend != xrBackend || _lastOpenXRTrackingMode != openXRTrackingMode)
    {
        _lastXRBackend = xrBackend;
        _lastOpenXRTrackingMode = openXRTrackingMode;
        ResetHandRaySmoothing();
        UpdateOpenXRVisualMode();
        SelectEffect();
    }

    AutoAssignXRInput();
    UpdateOpenXRVisualMode();
    UpdateXRConfirm();
    UpdateCalibrationInput();
    SyncEffectReferences();

    if (_effect == null || mainCam == null)
        return;

    _effect.ApplyCameraEffect(mainCam);

    UpdateDebugLines();
}

// When the runner is disabled, clean up the active visual effect and hide debug rays
// so the scene is not left with stale effect state.
void OnDisable()
{
    if (mainCam != null)
        _effect?.ResetCameraEffect(mainCam);

    SetDebugLinesActive(false);
}

// Early one-time reference lookup before Start().
void Awake()
{
    AutoAssignReferences();
    AutoAssignXRInput();
}

// Called when the component is added or reset in the Unity editor.
void Reset()
{
    AutoAssignReferences();
    AutoAssignXRInput();
}

// Editor-only validation hook.
// Keeps the inspector booleans in a consistent state and fills obvious references
// while the scene is being configured, without requiring play mode.
void OnValidate()
{
    EnforceSingleControllerMitigationMode();

    if (!Application.isPlaying)
    {
        AutoAssignReferences();
        AutoAssignOpenXRHandPrefabs();
        AutoAssignXRInput();
    }
}

// Tries to auto-find the scene objects this runner depends on.
//
// This method is long, but its purpose is straightforward:
// if the user did not manually drag all references into the inspector,
// SandboxRunner tries to find the important scene objects by common names.
//
// This is especially useful in a prototype, where scene hierarchy names are often stable
// but not every reference has been manually assigned.
//
// A few notable choices:
// - several possible camera names are tried because XR rigs differ between setups,
// - boardMid is allowed to resolve to the center exposure target, because that object
//   acts as a practical head-height anchor,
// - bodyCenterAnchor defaults to the first child of DepthQue, because the project uses
//   that object as a stable reference for body placement.
void AutoAssignReferences()
{
    if (mainCam == null)
    {
        var camObj =
            GameObject.Find("Camera") ??
            GameObject.Find("Camera (eye)") ??
            GameObject.Find("Main Camera");

        if (camObj != null)
            mainCam = camObj.GetComponent<Camera>();

        if (mainCam == null)
            mainCam = Camera.main;

        if (mainCam == null)
            mainCam = FindFirstObjectByType<Camera>();
    }

    if (visualWorldRoot == null)
    {
        var obj = GameObject.Find("WorldRoot");
        if (obj != null)
            visualWorldRoot = obj.transform;
    }

    if (statReadout == null)
        statReadout = GameObject.Find("StatText")?.GetComponent<TextMeshProUGUI>();

    if (experimentLogger == null)
        experimentLogger = FindFirstObjectByType<PrismExperimentLogger>();

    if (rigRoot == null)
    {
        var rigObj =
            GameObject.Find("XR Origin") ??
            GameObject.Find("[CameraRig]") ??
            GameObject.Find("CameraRig");
        if (rigObj != null)
            rigRoot = rigObj.transform;
    }

    if (hmd == null && mainCam != null)
        hmd = mainCam.transform;

    if (rigRoot == null && hmd != null)
        rigRoot = hmd.root;

    if (boardMid == null)
    {
        var boardObj =
            GameObject.Find("ExposureTarget2") ??
            GameObject.Find("ExposureTargetCenter") ??
            GameObject.Find("BoardMid") ??
            GameObject.Find("Board Mid") ??
            GameObject.Find("MidPointMarker") ??
            GameObject.Find("MidpointMarker") ??
            GameObject.Find("Midpoint");
        if (boardObj != null)
            boardMid = boardObj.transform;
    }

    if (bodyCenterAnchor == null)
    {
        GameObject depthQueObj = GameObject.Find("DepthQue");
        if (depthQueObj != null)
        {
            Transform depthQue = depthQueObj.transform;
            if (depthQue.childCount > 0)
                bodyCenterAnchor = depthQue.GetChild(0);
        }
    }

    if (openLoopTask == null)
        openLoopTask = GetComponent<OpenLoopPointingTask>();

    if (lineBisectionTask == null)
        lineBisectionTask = GetComponent<LineBisectionTask>();

    if (landmarkTask == null)
        landmarkTask = GetComponent<LandmarkTask>();

    if (exposureTask == null)
        exposureTask = GetComponent<ExposureTask>();

}

// Tries to auto-find the currently relevant XR objects and input actions.
//
// Why the method branches so much:
// the project supports two XR backends (SteamVR and OpenXR), and OpenXR itself
// supports two interaction styles (controllers and hands). Each combination stores
// its scene objects in slightly different places, so one search strategy is not enough.
//
// This method therefore decides:
// - where the active controller objects are,
// - where the ray origins are,
// - where the hand visuals are,
// - and which trigger/calibrate actions should be listened to.
void AutoAssignXRInput()
{
    if (xrBackend == XRBackend.OpenXR)
    {
        EnsureOpenXRHandVisuals();

        if (_openXRHandVisualizerRoot == null)
        {
            var handVisualizer = GameObject.Find("Hand Visualizer");
            if (handVisualizer != null)
                _openXRHandVisualizerRoot = handVisualizer;
        }

        if (_leftHandVisualTransform == null)
        {
            var leftHandVisual = GameObject.Find("Left Hand Tracking");
            if (leftHandVisual != null)
                _leftHandVisualTransform = leftHandVisual.transform;
        }

        if (_rightHandVisualTransform == null)
        {
            var rightHandVisual = GameObject.Find("Right Hand Tracking");
            if (rightHandVisual != null)
                _rightHandVisualTransform = rightHandVisual.transform;
        }

        if (openXRTrackingMode == OpenXRTrackingMode.Controllers)
        {
            if (_leftControllerTransform == null)
            {
                var leftObj =
                    GameObject.Find("Left Controller") ??
                    GameObject.Find("LeftHand Controller");
                if (leftObj != null)
                    _leftControllerTransform = leftObj.transform;
            }

            if (_rightControllerTransform == null)
            {
                var rightObj =
                    GameObject.Find("Right Controller") ??
                    GameObject.Find("RightHand Controller");
                if (rightObj != null)
                    _rightControllerTransform = rightObj.transform;
            }

            if (_leftControllerRayOriginTransform == null)
            {
                var leftRayOrigin = GameObject.Find("Left Controller Stabilized Attach");
                if (leftRayOrigin != null)
                    _leftControllerRayOriginTransform = leftRayOrigin.transform;
            }

            if (_rightControllerRayOriginTransform == null)
            {
                var rightRayOrigin = GameObject.Find("Right Controller Stabilized Attach");
                if (rightRayOrigin != null)
                    _rightControllerRayOriginTransform = rightRayOrigin.transform;
            }

            if (_leftControllerRayOriginTransform == null)
                _leftControllerRayOriginTransform = _leftControllerTransform;

            if (_rightControllerRayOriginTransform == null)
                _rightControllerRayOriginTransform = _rightControllerTransform;

            if (_leftPointerVisualTransform == null && _leftControllerTransform != null)
                _leftPointerVisualTransform = FindDeepChild(_leftControllerTransform, "UniversalController");

            if (_rightPointerVisualTransform == null && _rightControllerTransform != null)
                _rightPointerVisualTransform = FindDeepChild(_rightControllerTransform, "UniversalController");

            CacheAuxControllerVisuals(_leftControllerTransform, _leftAuxPointerVisualTransforms);
            CacheAuxControllerVisuals(_rightControllerTransform, _rightAuxPointerVisualTransforms);
        }

        return;
    }

    if (_leftControllerTransform == null)
    {
        var leftObj = GameObject.Find("Controller (left)");
        if (leftObj != null)
            _leftControllerTransform = leftObj.transform;
    }

    if (_rightControllerTransform == null)
    {
        var rightObj = GameObject.Find("Controller (right)");
        if (rightObj != null)
            _rightControllerTransform = rightObj.transform;
    }

    if (_confirmAction == null)
    {
        // Prefer the generated SteamVR action if it exists
        _confirmAction = SteamVR_Actions.default_Trigger;

        // Fallback to path lookup
        if (_confirmAction == null)
            _confirmAction = SteamVR_Input.GetBooleanAction("/actions/default/in/Trigger");
    }

    if (_calibrateAction == null)
    {
        _calibrateAction = SteamVR_Actions.default_Calibrate;

        if (_calibrateAction == null)
            _calibrateAction = SteamVR_Input.GetBooleanAction("/actions/default/in/Calibrate");
    }
}

// Returns the transform of the currently active controller body.
// In OpenXR hand-tracking mode there is no controller, so this returns null.
Transform GetActiveControllerTransform()
{
    if (xrBackend == XRBackend.OpenXR && openXRTrackingMode == OpenXRTrackingMode.Hands)
        return null;

    return activeHand == Handedness.Left ? _leftControllerTransform : _rightControllerTransform;
}

// Returns the transform used as the ray origin for the active controller.
// This is allowed to be different from the controller body, for example if a
// stabilized attach point is used for more consistent aiming.
Transform GetActiveControllerRayOriginTransform()
{
    if (xrBackend == XRBackend.OpenXR)
        return activeHand == Handedness.Left ? _leftControllerRayOriginTransform : _rightControllerRayOriginTransform;

    return GetActiveControllerTransform();
}

// Returns the visual GameObject that should be manipulated by visual effects such as skew.
//
// This is an important distinction:
// the "thing the user sees" and the "thing input is read from" are not always the same object.
// For example, the ray may originate from a stabilized attach transform, while the visible
// controller mesh is elsewhere. Effects like skew need the visible pointer object so the
// participant actually sees the perturbation.
Transform GetActivePointerVisual()
{
    if (xrBackend == XRBackend.OpenXR)
    {
        if (openXRTrackingMode == OpenXRTrackingMode.Controllers)
        {
            var pointerVisual = activeHand == Handedness.Left ? _leftPointerVisualTransform : _rightPointerVisualTransform;
            return pointerVisual != null ? pointerVisual : GetActiveControllerTransform();
        }

        var handVisual = activeHand == Handedness.Left ? _leftHandVisualTransform : _rightHandVisualTransform;
        if (handVisual != null)
            return handVisual;

        return _openXRHandVisualizerRoot != null ? _openXRHandVisualizerRoot.transform : rigRoot;
    }

    Transform controller = GetActiveControllerTransform();
    if (controller == null) return null;

    Transform model = controller.Find("Model");
    return model != null ? model : controller;
}

// Unified confirmation update.
//
// The design goal here is that the task scripts should not care how confirmation works.
// A task only needs to know "was a response just confirmed?"
//
// SandboxRunner hides the implementation differences:
// - embodied input confirms through a held pointing gesture,
// - controller input confirms through trigger input or a hover rule,
// - transitions temporarily suppress confirmation so the participant cannot accidentally
//   confirm while the experiment is changing phase.
void UpdateXRConfirm()
{
    bool down = false;

    if (_taskTransitionActive)
    {
        _confirmPressedLastFrameLeft = false;
        _confirmPressedLastFrameRight = false;
        ResetHandDwellConfirmState();
        ResetControllerHoverConfirmState();
        _confirmDown = false;
        return;
    }

    if (xrBackend == XRBackend.OpenXR)
    {
        if (openXRTrackingMode == OpenXRTrackingMode.Hands)
        {
            ResetControllerHoverConfirmState();
            down = UpdateHandDwellConfirm();
        }
        else
        {
            ResetHandDwellConfirmState();
            down = UpdateControllerConfirmForControllers();
        }
    }
    else
    {
        ResetHandDwellConfirmState();
        down = UpdateControllerConfirmForControllers();
    }

    _confirmDown = down;
}

// Separate input path for manual calibration.
//
// Calibration should never be confused with task confirmation, so it is read through
// a separate button/action path. This lets the participant recalibrate without affecting
// the current task state.
void UpdateCalibrationInput()
{
    bool down = false;

    if (xrBackend == XRBackend.OpenXR)
    {
        down = GetOpenXRButtonEdge(CommonUsages.primaryButton, activeHand, ref _calibrateDownLastFrame);
    }
    else if (_calibrateAction != null)
    {
        down = _calibrateAction.GetStateDown(calibrationHand);
    }

    if (down)
    {
        CalibrateNow();
    }
}

[ContextMenu("Calibrate Now")]
public void CalibrateNow()
{
    AutoAssignReferences();

    if (!rigRoot || !hmd)
    {
        Debug.LogError("[SandboxRunner] Missing calibration references (rigRoot/hmd).");
        return;
    }

    if (recenterXRTrackingOnCalibrate)
    {
        TryRecenterXRTracking();
    }

    ApplyCalibrationAnchors();
}

// Height-only calibration. Useful for debugging or for manually testing just
// the vertical alignment behavior without changing horizontal body placement.
//
// The key line is:
//   deltaY = boardMid.position.y - hmd.position.y
// which simply asks:
// "how much higher or lower should the whole rig move so the headset ends up
// at the same height as the chosen head anchor?"
[ContextMenu("Calibrate Height Now")]
public void CalibrateHeight()
{
    AutoAssignReferences();

    if (!rigRoot || !hmd || !boardMid)
    {
        Debug.LogError("[SandboxRunner] Missing calibration references (rigRoot/hmd/boardMid).");
        return;
    }

    float deltaY = boardMid.position.y - hmd.position.y;

    Vector3 p = rigRoot.position;
    p.y += deltaY;
    rigRoot.position = p;

    Debug.Log($"[SandboxRunner] Applied calibration deltaY={deltaY:0.000}m. HMD is now at board mid height.");
}

// Horizontal body-center calibration only.
//
// This version ignores height and only places the participant left/right/forward/backward
// relative to the chosen body anchor.
[ContextMenu("Calibrate Body Center Now")]
public void CalibrateBodyCenter()
{
    AutoAssignReferences();

    if (!rigRoot || !bodyCenterAnchor)
    {
        Debug.LogError("[SandboxRunner] Missing calibration references (rigRoot/bodyCenterAnchor).");
        return;
    }

    Vector3 p = rigRoot.position;
    Vector3 anchorPos = bodyCenterAnchor.position;
    p.x = anchorPos.x;
    p.z = anchorPos.z;
    rigRoot.position = p;

    Debug.Log($"[SandboxRunner] Applied body-center calibration to anchor '{bodyCenterAnchor.name}' at xz=({anchorPos.x:0.000}, {anchorPos.z:0.000}).");
}

// Requests an OpenXR recenter through the available XR input subsystems.
// This only works for OpenXR, and even there it depends on the active runtime/device.
bool TryRecenterXRTracking()
{
    if (xrBackend != XRBackend.OpenXR)
        return false;

    List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
    SubsystemManager.GetSubsystems(subsystems);

    bool anySucceeded = false;
    for (int i = 0; i < subsystems.Count; i++)
    {
        XRInputSubsystem subsystem = subsystems[i];
        if (subsystem == null || !subsystem.running)
            continue;

        if (subsystem.TryRecenter())
            anySucceeded = true;
    }

    if (anySucceeded)
        Debug.Log("[SandboxRunner] OpenXR tracking recentered.");
    else
        Debug.LogWarning("[SandboxRunner] OpenXR recenter was requested but no XRInputSubsystem accepted TryRecenter().");

    return anySucceeded;
}

// Older helper that recenters the rig around the current headset pose.
// The current calibration workflow mainly uses named anchors instead, but this remains
// as a useful utility when testing rig-relative recentering behavior.
void RecenterRigToCurrentHmd()
{
    AutoAssignReferences();

    if (!rigRoot || !hmd)
    {
        Debug.LogWarning("[SandboxRunner] Cannot recenter rig: missing rigRoot or hmd.");
        return;
    }

    Vector3 currentForward = Vector3.ProjectOnPlane(hmd.forward, Vector3.up);
    Vector3 desiredForward = Vector3.ProjectOnPlane(rigRoot.forward, Vector3.up);

    if (currentForward.sqrMagnitude > 0.0001f && desiredForward.sqrMagnitude > 0.0001f)
    {
        float yawDelta = Vector3.SignedAngle(currentForward.normalized, desiredForward.normalized, Vector3.up);
        rigRoot.RotateAround(hmd.position, Vector3.up, yawDelta);
    }

    Vector3 hmdOffset = hmd.position - rigRoot.position;
    hmdOffset.y = 0f;
    rigRoot.position -= hmdOffset;

    Debug.Log("[SandboxRunner] Applied rig-root recenter from current HMD pose.");
}

// Applies the actual calibration logic using the configured anchors.
//
// Conceptually this does three separate jobs:
// 1. rotate the participant to face the board correctly,
// 2. lift/lower the rig so the HMD matches the head-height anchor,
// 3. shift the rig horizontally so the participant's tracked body center lines up
//    with the chosen body anchor.
//
// Those three steps are deliberately separated because each one solves a different
// calibration problem: facing direction, head height, and body placement.
void ApplyCalibrationAnchors()
{
    bool appliedHeight = false;
    bool appliedBodyCenter = false;
    string heightAnchorName = boardMid ? boardMid.name : "none";
    string bodyAnchorName = bodyCenterAnchor ? bodyCenterAnchor.name : "none";

    if (boardMid != null && bodyCenterAnchor != null && rigRoot != null && hmd != null)
    {
        // Only yaw is adjusted here.
        // In other words: the participant is rotated left/right to face the board,
        // but the rig is not tilted upward/downward.
        Vector3 desiredForward = Vector3.ProjectOnPlane(boardMid.position - bodyCenterAnchor.position, Vector3.up);
        Vector3 currentForward = Vector3.ProjectOnPlane(hmd.forward, Vector3.up);

        if (desiredForward.sqrMagnitude > 0.0001f && currentForward.sqrMagnitude > 0.0001f)
        {
            float yawDelta = Vector3.SignedAngle(currentForward.normalized, desiredForward.normalized, Vector3.up);
            rigRoot.RotateAround(hmd.position, Vector3.up, yawDelta);
        }
    }

    if (boardMid != null)
    {
        float deltaY = boardMid.position.y - hmd.position.y;
        Vector3 p = rigRoot.position;
        p.y += deltaY;
        rigRoot.position = p;
        appliedHeight = true;
    }

    if (bodyCenterAnchor != null)
    {
        Vector3 anchorPos = bodyCenterAnchor.position;
        // XR Origin often contains an internal camera offset.
        // So if we aligned only rigRoot.xz to the anchor, the participant could still
        // end up physically shifted to the side. Subtracting the current HMD horizontal
        // offset fixes that and aligns the tracked body, not just the root transform.
        Vector3 hmdOffset = hmd.position - rigRoot.position;
        hmdOffset.y = 0f;

        Vector3 p = rigRoot.position;
        p.x = anchorPos.x - hmdOffset.x;
        p.z = anchorPos.z - hmdOffset.z;
        rigRoot.position = p;
        appliedBodyCenter = true;
    }

    if (!appliedHeight && !appliedBodyCenter)
    {
        Debug.LogError("[SandboxRunner] Missing calibration anchors (boardMid/bodyCenterAnchor).");
        return;
    }

    Debug.Log(
        $"[SandboxRunner] Calibration applied. " +
        $"Head anchor: {(appliedHeight ? heightAnchorName : "none")} | " +
        $"Body anchor: {(appliedBodyCenter ? bodyAnchorName : "none")} | " +
        $"Rig position: {rigRoot.position}");
}

// Chooses which visuomotor effect implementation should currently be active.
//
// A crucial experimental design choice is encoded here:
// visual perturbations are only active during Exposure.
//
// That means a participant can still have "Skew" selected in the inspector,
// but while running baseline or post, the runner deliberately uses NoEffect.
// This ensures baseline and post remain measurement phases rather than adaptation phases.
void SelectEffect()
{
    AutoAssignReferences();
    AutoAssignXRInput();

    if (mainCam != null)
        _effect?.ResetCameraEffect(mainCam);

    bool effectEnabledForCurrentTask = taskMode == TaskMode.Exposure;

    _effect = !effectEnabledForCurrentTask ? new NoEffect() : effectMode switch
    {
        EffectMode.None => new NoEffect(),
        EffectMode.Translation => translation,
        EffectMode.Rotation => rotation,
        EffectMode.Skew => skew,
        _ => new NoEffect()
    };

    SyncEffectReferences();
}

// Updates the optional debug rays that show the raw pointing ray and the transformed ray
// after the active effect has been applied.
void UpdateDebugLines()
{
    if (!drawDebugRays)
    {
        SetDebugLinesActive(false);
        return;
    }

    EnsureDebugLines();

    var raw = GetRawPointerRay();
    var trn = _effect != null ? _effect.TransformRay(raw) : raw;

    SetDebugLine(_rawRayLine, raw.origin, raw.origin + raw.direction * 2f);
    SetDebugLine(_transformedRayLine, trn.origin, trn.origin + trn.direction * 2f);

    SetDebugLinesActive(true);
}

// Ensures the debug line objects exist in the scene.
void EnsureDebugLines()
{
    if (_rayDebugRoot == null)
    {
        var existing = GameObject.Find("RayDebug");
        if (existing != null)
        {
            _rayDebugRoot = existing.transform;
        }
        else
        {
            var go = new GameObject("RayDebug");
            _rayDebugRoot = go.transform;
        }
    }

    if (_rawRayLine == null)
        _rawRayLine = GetOrCreateDebugLine("RawRayLine");

    if (_transformedRayLine == null)
        _transformedRayLine = GetOrCreateDebugLine("TransformedRayLine");
}

// Creates or reuses a LineRenderer with sensible defaults for debug visualization.
LineRenderer GetOrCreateDebugLine(string name)
{
    Transform child = _rayDebugRoot.Find(name);
    GameObject go;

    if (child != null)
    {
        go = child.gameObject;
    }
    else
    {
        go = new GameObject(name);
        go.transform.SetParent(_rayDebugRoot, false);
    }

    var lr = go.GetComponent<LineRenderer>();
    if (lr == null)
        lr = go.AddComponent<LineRenderer>();

    lr.positionCount = 2;
    lr.useWorldSpace = true;
    lr.widthMultiplier = 0.01f;
    lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    lr.receiveShadows = false;
    lr.alignment = LineAlignment.View;

    // Simple built-in material choice
    if (lr.sharedMaterial == null)
        lr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

    if (name == "RawRayLine")
    {
        lr.startColor = Color.white;
        lr.endColor = Color.white;
    }
    else
    {
        lr.startColor = Color.yellow;
        lr.endColor = Color.yellow;
    }

    return lr;
}

// Writes two world-space points into a LineRenderer.
void SetDebugLine(LineRenderer lr, Vector3 a, Vector3 b)
{
    if (lr == null) return;
    lr.SetPosition(0, a);
    lr.SetPosition(1, b);
}

// Convenience helper for enabling/disabling both debug rays together.
void SetDebugLinesActive(bool active)
{
    if (_rawRayLine != null)
        _rawRayLine.enabled = active;

    if (_transformedRayLine != null)
        _transformedRayLine.enabled = active;
}

// Enables exactly one task component at a time according to taskMode.
void ApplyTaskMode()
{
    SetTaskActive(openLoopTask, taskMode == TaskMode.OpenLoop);
    SetTaskActive(lineBisectionTask, taskMode == TaskMode.LineBisection);
    SetTaskActive(landmarkTask, taskMode == TaskMode.Landmark);
    SetTaskActive(exposureTask, taskMode == TaskMode.Exposure);
}

// Calls the common task activation interface, if the task implements it.
static void SetTaskActive(MonoBehaviour taskMb, bool active)
{
    if (taskMb == null) return;

    if (taskMb is ISandboxTask task)
        task.SetTaskActive(active);
    else
        Debug.LogWarning($"[SandboxRunner] Task does not implement ISandboxTask: {taskMb.name}");
}

// The main input abstraction used by the tasks.
//
// This is one of the most important methods in the whole file.
// It gives the tasks a clean, already-processed input package:
// - a ray for aiming,
// - a pose for the pointing device/hand,
// - and a confirmation signal.
//
// The task does not need to know:
// - whether the participant uses controller or hands,
// - whether the active backend is SteamVR or OpenXR,
// - whether a perturbation is currently applied.
//
// SandboxRunner resolves all of that here, then hands the task one unified answer.
public (Ray ray, Pose pose, bool confirm) GetTransformedInput()
{
    AutoAssignXRInput();

    if (_effect == null)
        SelectEffect();

    if (_effect == null)
        return (new Ray(Vector3.zero, Vector3.forward), new Pose(Vector3.zero, Quaternion.identity), false);

    Pose rawPose;
    Ray rawRay;
    if (!TryGetRawInput(out rawRay, out rawPose))
        return (new Ray(Vector3.zero, Vector3.forward), new Pose(Vector3.zero, Quaternion.identity), false);

    var confirm = _taskTransitionActive ? false : _confirmDown;

    var ray = _effect.TransformRay(rawRay);
    var pose = _effect.TransformPose(rawPose);

    return (ray, pose, confirm);
}

// Returns the raw, unmodified pointing ray before any effect transform is applied.
Ray GetRawPointerRay()
{
    AutoAssignXRInput();

    if (!TryGetRawInput(out Ray ray, out _))
        return new Ray(Vector3.zero, Vector3.forward);

    return ray;
}

// Collects the raw input source in world space.
//
// "Raw" here means "before translation/rotation/skew effects are applied".
//
// The split is:
// - hands: build an aim ray from tracked finger joints,
// - controllers: use the controller ray origin transform directly.
bool TryGetRawInput(out Ray ray, out Pose pose)
{
    if (xrBackend == XRBackend.OpenXR && openXRTrackingMode == OpenXRTrackingMode.Hands)
        return TryGetOpenXRHandInput(out ray, out pose);

    Transform controller = GetActiveControllerTransform();
    Transform rayOrigin = GetActiveControllerRayOriginTransform();
    if (controller == null || rayOrigin == null)
    {
        ray = new Ray(Vector3.zero, Vector3.forward);
        pose = new Pose(Vector3.zero, Quaternion.identity);
        return false;
    }

    pose = new Pose(rayOrigin.position, rayOrigin.rotation);
    ray = new Ray(rayOrigin.position, rayOrigin.forward);
    return true;
}

// Convenience wrappers around OpenXR devices for the currently relevant hand.
InputDevice GetOpenXRInputDevice()
{
    return GetOpenXRInputDevice(activeHand);
}

InputDevice GetOpenXRInputDevice(Handedness hand)
{
    XRNode xrNode = hand == Handedness.Left ? XRNode.LeftHand : XRNode.RightHand;
    return InputDevices.GetDeviceAtXRNode(xrNode);
}

// Reads a boolean input usage from OpenXR.
bool GetOpenXRButtonDown(InputFeatureUsage<bool> usage)
{
    return GetOpenXRButtonDown(usage, activeHand);
}

bool GetOpenXRButtonDown(InputFeatureUsage<bool> usage, Handedness hand)
{
    var device = GetOpenXRInputDevice(hand);
    if (!device.isValid)
        return false;

    if (!device.TryGetFeatureValue(usage, out bool isPressed))
        return false;

    return isPressed;
}

// Detects the "edge" of a button press: true only on the frame where the button
// changed from not pressed to pressed. This avoids repeated triggers while held down.
bool GetOpenXRButtonEdge(InputFeatureUsage<bool> usage, Handedness hand, ref bool pressedLastFrame)
{
    bool isPressed = GetOpenXRButtonDown(usage, hand);
    bool pressedThisFrame = isPressed && !pressedLastFrame;
    pressedLastFrame = isPressed;
    return pressedThisFrame;
}

// Embodied confirmation logic.
//
// The underlying idea is a dwell gesture:
// once the hand pose is recognized as a pointing gesture, a timer starts.
// If the gesture is maintained long enough, the response is confirmed.
//
// This avoids button pressing for embodied mode and gives the UI enough information
// to show a progress bar while the dwell is building up.
bool UpdateHandDwellConfirm()
{
    bool isPointing = IsActiveHandPointingGesture();
    _handPointingActive = isPointing;
    _confirmDwellActive = isPointing;

    if (!isPointing)
    {
        _handPointingStartTime = -1f;
        _handDwellProgress01 = 0f;
        _confirmDwellProgress01 = 0f;
        _handDwellTriggered = false;
        return false;
    }

    if (_handPointingStartTime < 0f)
        _handPointingStartTime = Time.time;

    float dwellDuration = Mathf.Max(0.05f, handDwellSeconds);
    float elapsed = Time.time - _handPointingStartTime;
    _handDwellProgress01 = Mathf.Clamp01(elapsed / dwellDuration);
    _confirmDwellProgress01 = _handDwellProgress01;

    if (!_handDwellTriggered && elapsed >= dwellDuration)
    {
        _handDwellTriggered = true;
        return true;
    }

    return false;
}

// Controller confirmation logic.
// Depending on inspector settings, this becomes:
// - normal dominant-hand trigger
// - opposite-hand trigger
// - controller hover confirm
bool UpdateControllerConfirmForControllers()
{
    if (useControllerHoverConfirm)
        return UpdateControllerHoverConfirm();

    ResetControllerHoverConfirmState();

    Handedness confirmHand = useOppositeHandTrigger ? GetOppositeHand(activeHand) : activeHand;
    return GetControllerConfirmEdge(confirmHand);
}

// Controller hover confirm.
//
// This is the controller-side mitigation for the "Heisenberg effect" discussed in the project:
// pressing a trigger can slightly disturb the user's aim at the critical moment.
//
// So instead of confirming on button press, this method:
// - finds the current task target,
// - projects the transformed controller ray onto that target plane,
// - checks whether the hit point is inside an allowed hover region,
// - starts a dwell timer,
// - confirms automatically once the dwell time is long enough.
//
// If the task cannot provide a concrete target, the code falls back to normal trigger input,
// because hover confirm would otherwise have nothing meaningful to hover over.
bool UpdateControllerHoverConfirm()
{
    if (!TryGetCurrentAimTarget(out Vector3 targetCenter, out Vector3 planeNormal, out float taskTargetRadius))
    {
        ResetControllerHoverConfirmState();

        if (_hoverFallbackWarnedTask != taskMode)
        {
            Debug.LogWarning($"[SandboxRunner] Controller hover confirm is not supported for task {taskMode}. Falling back to the dominant-hand trigger.");
            _hoverFallbackWarnedTask = taskMode;
        }

        return GetControllerConfirmEdge(activeHand);
    }

    _hoverFallbackWarnedTask = null;

    if (_effect == null)
        SelectEffect();

    if (!TryGetRawInput(out Ray rawRay, out _))
    {
        ResetControllerHoverConfirmState();
        return false;
    }

    Ray transformedRay = _effect != null ? _effect.TransformRay(rawRay) : rawRay;
    if (!IntersectRayWithPlane(transformedRay, targetCenter, planeNormal, out Vector3 hitPoint))
    {
        ResetControllerHoverConfirmState();
        return false;
    }

    float activationRadius = Mathf.Max(controllerHoverActivationRadiusMeters, taskTargetRadius);
    // If the active target changed, the dwell timer must restart from zero.
    bool targetChanged = !_controllerHoverHadTargetLastFrame ||
                         Vector3.Distance(_controllerHoverLastTargetCenter, targetCenter) > 0.001f;
    bool insideHoverZone = Vector3.Distance(hitPoint, targetCenter) <= activationRadius;

    _controllerHoverLastTargetCenter = targetCenter;
    _controllerHoverHadTargetLastFrame = true;

    if (targetChanged)
    {
        _controllerHoverStartTime = -1f;
        _controllerHoverTriggered = false;
    }

    if (!insideHoverZone)
    {
        _controllerHoverStartTime = -1f;
        _controllerHoverTriggered = false;
        _confirmDwellActive = false;
        _confirmDwellProgress01 = 0f;
        return false;
    }

    if (_controllerHoverStartTime < 0f)
        _controllerHoverStartTime = Time.time;

    float dwellDuration = Mathf.Max(0.05f, controllerHoverSeconds);
    float elapsed = Time.time - _controllerHoverStartTime;
    _confirmDwellActive = true;
    _confirmDwellProgress01 = Mathf.Clamp01(elapsed / dwellDuration);

    if (!_controllerHoverTriggered && elapsed >= dwellDuration)
    {
        _controllerHoverTriggered = true;
        return true;
    }

    return false;
}

// Clears all embodied dwell-confirm state.
void ResetHandDwellConfirmState()
{
    _handPointingActive = false;
    _handPointingStartTime = -1f;
    _handDwellProgress01 = 0f;
    _handDwellTriggered = false;
}

// Clears all controller hover-confirm state.
void ResetControllerHoverConfirmState()
{
    _confirmDwellActive = false;
    _confirmDwellProgress01 = 0f;
    _controllerHoverStartTime = -1f;
    _controllerHoverTriggered = false;
    _controllerHoverHadTargetLastFrame = false;
}

// Small helper for choosing the non-dominant hand.
Handedness GetOppositeHand(Handedness hand)
{
    return hand == Handedness.Left ? Handedness.Right : Handedness.Left;
}

// Reads a controller trigger press from either OpenXR or SteamVR, depending on backend.
bool GetControllerConfirmEdge(Handedness hand)
{
    if (xrBackend == XRBackend.OpenXR)
    {
        if (hand == Handedness.Left)
            return GetOpenXRButtonEdge(CommonUsages.triggerButton, hand, ref _confirmPressedLastFrameLeft);

        return GetOpenXRButtonEdge(CommonUsages.triggerButton, hand, ref _confirmPressedLastFrameRight);
    }

    if (_confirmAction == null)
        return false;

    var source = hand == Handedness.Left
        ? SteamVR_Input_Sources.LeftHand
        : SteamVR_Input_Sources.RightHand;

    return _confirmAction.GetStateDown(source);
}

// Asks the currently active task whether it can provide a concrete aim target.
bool TryGetCurrentAimTarget(out Vector3 worldCenter, out Vector3 planeNormal, out float targetRadiusMeters)
{
    worldCenter = Vector3.zero;
    planeNormal = Vector3.up;
    targetRadiusMeters = 0f;

    var taskComponent = GetTaskComponent(taskMode);
    IAimTargetProvider targetProvider = taskComponent as IAimTargetProvider;
    if (targetProvider == null)
        return false;

    return targetProvider.TryGetAimTarget(out worldCenter, out planeNormal, out targetRadiusMeters);
}

// Intersects a ray with the task plane so hover confirm can determine where the
// transformed controller ray lands relative to the target.
bool IntersectRayWithPlane(Ray ray, Vector3 planePoint, Vector3 planeNormal, out Vector3 hitPoint)
{
    hitPoint = Vector3.zero;

    Plane plane = new Plane(planeNormal, planePoint);
    if (!plane.Raycast(ray, out float enter) || enter <= 0f || enter >= 10f)
        return false;

    hitPoint = ray.GetPoint(enter);
    return true;
}

// Keeps the three controller-mitigation booleans mutually exclusive in the inspector.
// This avoids ambiguous situations such as hover confirm and opposite-hand confirm
// being active at the same time.
void EnforceSingleControllerMitigationMode()
{
    bool noChanged = noHeisenbergMitigation != _prevNoHeisenbergMitigation;
    bool oppositeChanged = useOppositeHandTrigger != _prevUseOppositeHandTrigger;
    bool hoverChanged = useControllerHoverConfirm != _prevUseControllerHoverConfirm;

    if (hoverChanged && useControllerHoverConfirm)
    {
        noHeisenbergMitigation = false;
        useOppositeHandTrigger = false;
    }
    else if (oppositeChanged && useOppositeHandTrigger)
    {
        noHeisenbergMitigation = false;
        useControllerHoverConfirm = false;
    }
    else if (noChanged && noHeisenbergMitigation)
    {
        useOppositeHandTrigger = false;
        useControllerHoverConfirm = false;
    }
    else if (!noHeisenbergMitigation && !useOppositeHandTrigger && !useControllerHoverConfirm)
    {
        noHeisenbergMitigation = true;
    }

    int trueCount = (noHeisenbergMitigation ? 1 : 0) +
                    (useOppositeHandTrigger ? 1 : 0) +
                    (useControllerHoverConfirm ? 1 : 0);

    if (trueCount > 1)
    {
        if (useControllerHoverConfirm)
        {
            noHeisenbergMitigation = false;
            useOppositeHandTrigger = false;
        }
        else if (useOppositeHandTrigger)
        {
            noHeisenbergMitigation = false;
            useControllerHoverConfirm = false;
        }
        else
        {
            noHeisenbergMitigation = true;
            useOppositeHandTrigger = false;
            useControllerHoverConfirm = false;
        }
    }

    SyncControllerMitigationPreviousState();
}

// Stores the previous inspector state so OnValidate() can detect which checkbox changed.
void SyncControllerMitigationPreviousState()
{
    _prevNoHeisenbergMitigation = noHeisenbergMitigation;
    _prevUseOppositeHandTrigger = useOppositeHandTrigger;
    _prevUseControllerHoverConfirm = useControllerHoverConfirm;
}

// Returns the OpenXR hand subsystem used for tracked hand data.
XRHands.XRHandSubsystem GetXRHandSubsystem()
{
    if (_xrHandSubsystem != null && _xrHandSubsystem.running)
        return _xrHandSubsystem;

    var generalSettings = XRManagement.XRGeneralSettings.Instance;
    var loader = generalSettings != null ? generalSettings.Manager.activeLoader : null;
    _xrHandSubsystem = loader != null ? loader.GetLoadedSubsystem<XRHands.XRHandSubsystem>() : null;
    return _xrHandSubsystem;
}

// Builds a pointing ray from the user's index finger.
//
// A non-coder way to read this:
// the system looks at the tracked index finger, takes the fingertip as the "start"
// of the pointing ray, then estimates the direction of the finger from the finger bones.
//
// This creates a virtual pointer for hand-tracking mode even though there is no physical controller.
bool TryGetOpenXRHandInput(out Ray ray, out Pose pose)
{
    var handSubsystem = GetXRHandSubsystem();
    if (handSubsystem == null)
    {
        ray = new Ray(Vector3.zero, Vector3.forward);
        pose = new Pose(Vector3.zero, Quaternion.identity);
        return false;
    }

    var hand = activeHand == Handedness.Left ? handSubsystem.leftHand : handSubsystem.rightHand;
    if (!TryGetTrackedJointPose(hand, XRHands.XRHandJointID.IndexTip, out Pose indexTipPose) ||
        !TryGetTrackedJointPose(hand, XRHands.XRHandJointID.IndexIntermediate, out Pose indexKnucklePose))
    {
        ray = new Ray(Vector3.zero, Vector3.forward);
        pose = new Pose(Vector3.zero, Quaternion.identity);
        return false;
    }

    Vector3 worldOrigin = TransformTrackingPointToWorld(indexTipPose.position);
    Vector3 worldKnuckle = TransformTrackingPointToWorld(indexKnucklePose.position);
    // The basic finger direction is inferred from knuckle-to-tip.
    // That is usually more intuitive and visually stable than trusting the fingertip
    // rotation on its own.
    Vector3 worldDirection = (worldOrigin - worldKnuckle).normalized;

    if (worldDirection.sqrMagnitude < 0.0001f)
        worldDirection = TransformTrackingRotationToWorld(indexTipPose.rotation) * Vector3.forward;

    ApplyHandRaySmoothing(ref worldOrigin, ref worldDirection);

    Quaternion worldRotation = Quaternion.LookRotation(worldDirection, Vector3.up);
    pose = new Pose(worldOrigin, worldRotation);
    ray = new Ray(worldOrigin, worldDirection);
    return true;
}

// Checks whether the current hand pose looks like a pointing gesture.
//
// The gesture definition is intentionally simple:
// - the index finger should be fairly straight,
// - the middle, ring, and little finger should be more curled.
//
// This gives the system a robust "yes/no" rule for embodied confirmation instead of
// relying on a more ambiguous open hand pose.
bool IsActiveHandPointingGesture()
{
    var handSubsystem = GetXRHandSubsystem();
    if (handSubsystem == null)
        return false;

    var hand = activeHand == Handedness.Left ? handSubsystem.leftHand : handSubsystem.rightHand;
    if (!hand.isTracked)
        return false;

    bool indexStraight = TryGetFingerStraightness(hand, XRHands.XRHandJointID.IndexProximal, XRHands.XRHandJointID.IndexIntermediate, XRHands.XRHandJointID.IndexDistal, XRHands.XRHandJointID.IndexTip, out float indexStraightness)
        && indexStraightness >= 0.75f;

    bool middleCurled = TryGetFingerStraightness(hand, XRHands.XRHandJointID.MiddleProximal, XRHands.XRHandJointID.MiddleIntermediate, XRHands.XRHandJointID.MiddleDistal, XRHands.XRHandJointID.MiddleTip, out float middleStraightness)
        && middleStraightness < 0.70f;

    bool ringCurled = TryGetFingerStraightness(hand, XRHands.XRHandJointID.RingProximal, XRHands.XRHandJointID.RingIntermediate, XRHands.XRHandJointID.RingDistal, XRHands.XRHandJointID.RingTip, out float ringStraightness)
        && ringStraightness < 0.70f;

    bool littleCurled = TryGetFingerStraightness(hand, XRHands.XRHandJointID.LittleProximal, XRHands.XRHandJointID.LittleIntermediate, XRHands.XRHandJointID.LittleDistal, XRHands.XRHandJointID.LittleTip, out float littleStraightness)
        && littleStraightness < 0.70f;

    return indexStraight && middleCurled && ringCurled && littleCurled;
}

// Converts four joints on one finger into a simple "straightness" score.
//
// If the finger is straight, consecutive finger segments point in nearly the same direction.
// If the finger is bent, those directions diverge.
//
// The dot products below measure that alignment numerically:
// values near 1 mean "same direction", while lower values mean "more bent".
bool TryGetFingerStraightness(
    XRHands.XRHand hand,
    XRHands.XRHandJointID proximalId,
    XRHands.XRHandJointID intermediateId,
    XRHands.XRHandJointID distalId,
    XRHands.XRHandJointID tipId,
    out float straightness)
{
    straightness = 0f;

    if (!TryGetTrackedJointPose(hand, proximalId, out Pose proximalPose) ||
        !TryGetTrackedJointPose(hand, intermediateId, out Pose intermediatePose) ||
        !TryGetTrackedJointPose(hand, distalId, out Pose distalPose) ||
        !TryGetTrackedJointPose(hand, tipId, out Pose tipPose))
    {
        return false;
    }

    Vector3 segmentA = (intermediatePose.position - proximalPose.position).normalized;
    Vector3 segmentB = (distalPose.position - intermediatePose.position).normalized;
    Vector3 segmentC = (tipPose.position - distalPose.position).normalized;

    if (segmentA.sqrMagnitude < 0.5f || segmentB.sqrMagnitude < 0.5f || segmentC.sqrMagnitude < 0.5f)
        return false;

    straightness = (Vector3.Dot(segmentA, segmentB) + Vector3.Dot(segmentB, segmentC)) * 0.5f;
    return true;
}

// Reads a tracked pose for one XR hand joint.
bool TryGetTrackedJointPose(XRHands.XRHand hand, XRHands.XRHandJointID jointId, out Pose jointPose)
{
    jointPose = default;
    var joint = hand.GetJoint(jointId);
    return joint.TryGetPose(out jointPose);
}

// Hand tracking coordinates come from XR tracking space, so they are converted into
// world space relative to rigRoot before the rest of the system uses them.
Vector3 TransformTrackingPointToWorld(Vector3 trackingSpacePoint)
{
    return rigRoot != null ? rigRoot.TransformPoint(trackingSpacePoint) : trackingSpacePoint;
}

Quaternion TransformTrackingRotationToWorld(Quaternion trackingSpaceRotation)
{
    return rigRoot != null ? rigRoot.rotation * trackingSpaceRotation : trackingSpaceRotation;
}

// Shows either controller visuals or hand visuals depending on the current OpenXR mode.
// This keeps the scene visually consistent with the actual input method in use.
void UpdateOpenXRVisualMode()
{
    if (xrBackend != XRBackend.OpenXR)
        return;

    bool useHands = openXRTrackingMode == OpenXRTrackingMode.Hands;

    if (_openXRHandVisualizerRoot != null)
        _openXRHandVisualizerRoot.SetActive(useHands);

    SetHandVisualState(Handedness.Left, useHands);
    SetHandVisualState(Handedness.Right, useHands);

    SetControllerVisualState(Handedness.Left, !useHands && activeHand == Handedness.Left);
    SetControllerVisualState(Handedness.Right, !useHands && activeHand == Handedness.Right);
}

// Small accessors used by tasks and logging helpers.
public TextMeshProUGUI GetStatReadout()
{
    AutoAssignReferences();
    return statReadout;
}

public PrismExperimentLogger GetExperimentLogger()
{
    AutoAssignReferences();
    return experimentLogger;
}

// Called by measurement tasks when a baseline or post block finishes.
// This is where automatic experiment progression is coordinated.
public void NotifyMeasurementBlockCompleted(TaskMode completedTask, string blockName)
{
    if (!autoProgressExperiment || _experimentCompleted)
        return;

    if (blockName == "Baseline")
    {
        if (taskMode == completedTask)
            BeginExposure();
        return;
    }

    if (blockName == "Post")
    {
        CompleteExperiment();
    }
}

// Shows or hides the chosen controller model and any auxiliary poke/direct-interactor visuals.
void SetControllerVisualState(Handedness hand, bool active)
{
    Transform explicitVisual = hand == Handedness.Left ? _leftPointerVisualTransform : _rightPointerVisualTransform;
    List<Transform> auxVisuals = hand == Handedness.Left ? _leftAuxPointerVisualTransforms : _rightAuxPointerVisualTransforms;
    if (explicitVisual != null)
    {
        explicitVisual.gameObject.SetActive(active);
    }
    else
    {
        Transform controllerTransform = hand == Handedness.Left ? _leftControllerTransform : _rightControllerTransform;
        Transform controllerVisual = controllerTransform != null ? FindDeepChild(controllerTransform, "UniversalController") : null;
        if (controllerVisual != null)
            controllerVisual.gameObject.SetActive(active);
    }

    for (int i = 0; i < auxVisuals.Count; i++)
    {
        if (auxVisuals[i] != null)
            auxVisuals[i].gameObject.SetActive(active);
    }
}

// Shows or hides the tracked hand visual.
void SetHandVisualState(Handedness hand, bool active)
{
    Transform handVisual = hand == Handedness.Left ? _leftHandVisualTransform : _rightHandVisualTransform;
    if (handVisual != null)
        handVisual.gameObject.SetActive(active);
}

// Instantiates the OpenXR hand-visualizer prefabs when needed.
void EnsureOpenXRHandVisuals()
{
    if (rigRoot == null)
        return;

    if (_leftHandVisualTransform == null && openXRLeftHandTrackingPrefab != null)
        _leftHandVisualTransform = InstantiateOpenXRHandPrefab(openXRLeftHandTrackingPrefab, "Left Hand Tracking");

    if (_rightHandVisualTransform == null && openXRRightHandTrackingPrefab != null)
        _rightHandVisualTransform = InstantiateOpenXRHandPrefab(openXRRightHandTrackingPrefab, "Right Hand Tracking");
}

// Smooths hand-ray input so fingertip jitter does not produce extremely unstable aiming.
//
// The smoothing is exponential rather than a simple average.
// In practice that means:
// - recent movement still matters,
// - old movement quickly loses influence,
// - and the user can still aim responsively while avoiding noisy flicker.
//
// The parameter handRaySmoothingSeconds acts like a responsiveness knob:
// larger values = steadier but slower,
// smaller values = quicker but noisier.
void ApplyHandRaySmoothing(ref Vector3 worldOrigin, ref Vector3 worldDirection)
{
    float smoothTime = Mathf.Max(0f, handRaySmoothingSeconds);
    if (smoothTime <= 0.0001f)
    {
        _smoothedHandRayInitialized = false;
        return;
    }

    if (!_smoothedHandRayInitialized || _smoothedHandRayHand != activeHand)
    {
        _smoothedHandRayInitialized = true;
        _smoothedHandRayHand = activeHand;
        _smoothedHandRayOrigin = worldOrigin;
        _smoothedHandRayDirection = worldDirection;
        return;
    }

    float t = 1f - Mathf.Exp(-Time.deltaTime / smoothTime);
    _smoothedHandRayOrigin = Vector3.Lerp(_smoothedHandRayOrigin, worldOrigin, t);
    _smoothedHandRayDirection = Vector3.Slerp(_smoothedHandRayDirection, worldDirection, t).normalized;

    worldOrigin = _smoothedHandRayOrigin;
    worldDirection = _smoothedHandRayDirection;
}

// Resets the smoothing state when input mode/hand changes.
void ResetHandRaySmoothing()
{
    _smoothedHandRayInitialized = false;
}

// Creates a hand visualizer instance under the XR rig if it does not already exist.
Transform InstantiateOpenXRHandPrefab(GameObject prefab, string objectName)
{
    Transform existing = FindDeepChild(rigRoot, objectName);
    if (existing != null)
        return existing;

    Transform handVisualParent = FindDeepChild(rigRoot, "Camera Offset");
    if (handVisualParent == null)
        handVisualParent = rigRoot;

    var instance = Instantiate(prefab, handVisualParent);
    instance.name = objectName;
    instance.transform.localPosition = Vector3.zero;
    instance.transform.localRotation = Quaternion.identity;
    instance.transform.localScale = Vector3.one;

    return instance.transform;
}

// Editor-time helper for loading the sample XR Hands visualizer prefabs from the project.
void AutoAssignOpenXRHandPrefabs()
{
#if UNITY_EDITOR
    if (openXRLeftHandTrackingPrefab == null)
    {
        openXRLeftHandTrackingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Samples/XR Hands/1.5.1/HandVisualizer/Prefabs/Left Hand Tracking.prefab");
    }

    if (openXRRightHandTrackingPrefab == null)
    {
        openXRRightHandTrackingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Samples/XR Hands/1.5.1/HandVisualizer/Prefabs/Right Hand Tracking.prefab");
    }
#endif
}

// Recursive scene-hierarchy search by exact child name.
static Transform FindDeepChild(Transform root, string childName)
{
    if (root == null)
        return null;

    var stack = new Stack<Transform>();
    stack.Push(root);

    while (stack.Count > 0)
    {
        var current = stack.Pop();
        if (current.name == childName)
            return current;

        for (int i = 0; i < current.childCount; i++)
            stack.Push(current.GetChild(i));
    }

    return null;
}

// Recursive scene-hierarchy search for all children whose names contain a substring.
static void FindDeepChildrenContaining(Transform root, string partialName, List<Transform> results)
{
    if (root == null || string.IsNullOrEmpty(partialName))
        return;

    var stack = new Stack<Transform>();
    stack.Push(root);

    while (stack.Count > 0)
    {
        var current = stack.Pop();
        if (current.name.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0)
            results.Add(current);

        for (int i = 0; i < current.childCount; i++)
            stack.Push(current.GetChild(i));
    }
}

// Caches secondary controller visuals so effects such as skew can move/hide them too,
// not just the main controller mesh.
void CacheAuxControllerVisuals(Transform controllerRoot, List<Transform> cache)
{
    if (controllerRoot == null || cache == null)
        return;

    cache.Clear();
    FindDeepChildrenContaining(controllerRoot, "poke", cache);
    FindDeepChildrenContaining(controllerRoot, "interactor", cache);
    FindDeepChildrenContaining(controllerRoot, "direct", cache);

    Transform primaryVisual = FindDeepChild(controllerRoot, "UniversalController");
    cache.RemoveAll(t => t == null || t == controllerRoot || t == primaryVisual);
}

// Some effects need up-to-date scene references. At the moment skew is the main one.
void SyncEffectReferences()
{
    if (effectMode != EffectMode.Skew)
        return;

    skew.visualWorldRoot = visualWorldRoot;
    skew.visualPointerRoot = GetActivePointerVisual();
    skew.visualAuxRoots = activeHand == Handedness.Left ? _leftAuxPointerVisualTransforms : _rightAuxPointerVisualTransforms;
}

// Keyboard shortcuts are mainly for debugging and development inside the editor.
void HandleKeyboardShortcuts()
{
    if (Input.GetKeyDown(restartBaselineKey))
    {
        RestartBaselineFromAnywhere();
    }

    if (Input.GetKeyDown(enterExposureKey))
    {
        if (taskMode != TaskMode.Exposure && IsMeasurementTask(taskMode))
            BeginExposure();
    }
}

// Forces the experiment back to a baseline block from whatever state the runner is in.
void RestartBaselineFromAnywhere()
{
    _waitingForExposureReturn = false;
    _pendingExposureReturnTime = -1f;

    TaskMode baselineTask = taskMode == TaskMode.Exposure
        ? _measurementTaskBeforeExposure
        : taskMode;

    if (!IsMeasurementTask(baselineTask))
        baselineTask = TaskMode.OpenLoop;

    taskMode = baselineTask;
    ApplyTaskMode();

    BeginMeasurementBlock(taskMode, "Baseline");
}

// Begins the transition from a measurement task into Exposure.
public void BeginExposure()
{
    if (!IsMeasurementTask(taskMode) || _taskTransitionActive)
        return;

    _measurementTaskBeforeExposure = taskMode;
    RunTaskTransition(BeginExposureNow);
}

// Called by ExposureTask when its required number of hits/attempts is complete.
public void NotifyExposureCompleted()
{
    if (!autoReturnFromExposure || _taskTransitionActive)
        return;

    _waitingForExposureReturn = false;
    _pendingExposureReturnTime = -1f;
    RunTaskTransition(ReturnFromExposureToPost);
}

// Returns from Exposure into the post-measurement version of the same task that
// was used before exposure started.
public void ReturnFromExposureToPost()
{
    _waitingForExposureReturn = false;

    if (!IsMeasurementTask(_measurementTaskBeforeExposure))
        _measurementTaskBeforeExposure = TaskMode.OpenLoop;

    taskMode = _measurementTaskBeforeExposure;
    ApplyTaskMode();
    SelectEffect();

    BeginMeasurementBlock(taskMode, "Post");
}

// Starts either a baseline or post block for the chosen measurement task.
//
// Notice that the runner does not directly know how to initialize every task block.
// Instead it:
// - activates the correct task,
// - logs that the block started,
// - optionally clears old summaries for baseline,
// - then uses reflection to call task-specific setup methods if they exist.
//
// This keeps SandboxRunner generic while still allowing each task script to manage
// its own internal block state.
void BeginMeasurementBlock(TaskMode measurementTask, string blockName)
{
    var targetTask = GetTaskComponent(measurementTask);
    if (targetTask == null) return;

    taskMode = measurementTask;
    ApplyTaskMode();
    SelectEffect();
    experimentLogger?.LogBlockStarted(measurementTask.ToString(), blockName);

    if (blockName == "Baseline")
        TryInvokeNoArg(targetTask, "ClearSummaries");
    TryInvokeBlockMethod(targetTask, "StartNewBlock", blockName);
}

// Internal helper that actually activates Exposure after the transition delay ends.
void BeginExposureNow()
{
    experimentLogger?.LogExposureStarted(_measurementTaskBeforeExposure.ToString());

    taskMode = TaskMode.Exposure;
    ApplyTaskMode();
    SelectEffect();

    TryInvokeNoArg(exposureTask, "StartExposureBlock");
}

// Runs the generic "loading bar / transition pause" between major experiment phases.
//
// This is not just a cosmetic pause. It also gives the participant a clear separation
// between phases and prevents accidental confirmation during the switch.
void RunTaskTransition(Action onTransitionComplete)
{
    if (_taskTransitionCoroutine != null)
        StopCoroutine(_taskTransitionCoroutine);

    _taskTransitionCoroutine = StartCoroutine(TaskTransitionRoutine(onTransitionComplete));
}

// Simple coroutine that blocks confirmation while a transition is active,
// waits for the configured duration, then continues into the next phase.
IEnumerator TaskTransitionRoutine(Action onTransitionComplete)
{
    _taskTransitionActive = true;
    _taskTransitionStartTime = Time.time;
    _taskTransitionDuration = Mathf.Max(0f, taskTransitionSeconds);

    if (_taskTransitionDuration > 0f)
        yield return new WaitForSeconds(_taskTransitionDuration);

    _taskTransitionActive = false;
    _taskTransitionCoroutine = null;
    onTransitionComplete?.Invoke();
}

// Marks the experiment finished, saves logs, and optionally stops play mode.
void CompleteExperiment()
{
    if (_experimentCompleted)
        return;

    _experimentCompleted = true;
    experimentLogger?.LogExperimentCompleted(taskMode.ToString(), "Post");
    experimentLogger?.SaveLogs();

    if (!stopPlayModeWhenExperimentCompletes)
        return;

#if UNITY_EDITOR
    EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
}

// Exposes raw controller and ray-origin poses for logging/analysis code.
public bool TryGetControllerPose(Handedness hand, out Pose controllerPose, out Pose rayPose)
{
    AutoAssignXRInput();

    Transform controller = hand == Handedness.Left ? _leftControllerTransform : _rightControllerTransform;
    Transform rayOrigin = hand == Handedness.Left ? _leftControllerRayOriginTransform : _rightControllerRayOriginTransform;

    controllerPose = controller != null
        ? new Pose(controller.position, controller.rotation)
        : new Pose(Vector3.zero, Quaternion.identity);

    rayPose = rayOrigin != null
        ? new Pose(rayOrigin.position, rayOrigin.rotation)
        : controllerPose;

    return controller != null || rayOrigin != null;
}

// Returns the most meaningful movement anchor for the current input modality.
// For hand tracking this is the palm. For controllers this is the controller body.
// This is primarily used for movement-speed analysis and logging.
public bool TryGetMovementAnchorPose(Handedness hand, out Pose anchorPose, out string anchorSource)
{
    anchorPose = new Pose(Vector3.zero, Quaternion.identity);
    anchorSource = "Unavailable";

    if (xrBackend == XRBackend.OpenXR && openXRTrackingMode == OpenXRTrackingMode.Hands)
    {
        var handSubsystem = GetXRHandSubsystem();
        if (handSubsystem != null)
        {
            var xrHand = hand == Handedness.Left ? handSubsystem.leftHand : handSubsystem.rightHand;
            if (xrHand.isTracked)
            {
                if (TryGetTrackedJointPose(xrHand, XRHands.XRHandJointID.Palm, out Pose palmPose))
                {
                    anchorPose = new Pose(
                        TransformTrackingPointToWorld(palmPose.position),
                        TransformTrackingRotationToWorld(palmPose.rotation));
                    anchorSource = "Palm";
                    return true;
                }
            }
        }

        return false;
    }

    bool hasControllerPose = TryGetControllerPose(hand, out Pose controllerPose, out Pose rayPose);
    if (hasControllerPose)
    {
        anchorPose = controllerPose;
        anchorSource = "ControllerBody";
        return true;
    }

    return false;
}

// Returns the current trigger state without edge detection. This is useful for logging.
public bool GetControllerTriggerState(Handedness hand)
{
    if (xrBackend == XRBackend.OpenXR)
        return GetOpenXRButtonDown(CommonUsages.triggerButton, hand);

    if (_confirmAction == null)
        return false;

    var source = hand == Handedness.Left
        ? SteamVR_Input_Sources.LeftHand
        : SteamVR_Input_Sources.RightHand;

    return _confirmAction.GetState(source);
}

// Bundles contextual experiment state for the logging system.
//
// The logger uses this so every event/sample can later be interpreted correctly in R.
// For example:
// - measurement tasks care about block name and trial number,
// - exposure cares about attempt number and target index.
//
// Without this context, later analysis would have to infer too much from timestamps alone.
public void GetLoggingContext(out string blockType, out int? trialIndex, out int? attemptIndex, out int? targetIndex)
{
    blockType = "";
    trialIndex = null;
    attemptIndex = null;
    targetIndex = null;

    switch (taskMode)
    {
        case TaskMode.OpenLoop:
            if (openLoopTask is OpenLoopPointingTask openLoop)
            {
                blockType = openLoop.GetCurrentBlockName();
                trialIndex = openLoop.GetCurrentTrialNumber();
            }
            break;

        case TaskMode.LineBisection:
            if (lineBisectionTask is LineBisectionTask lineBisection)
            {
                blockType = lineBisection.GetCurrentBlockName();
                trialIndex = lineBisection.GetCurrentTrialNumber();
            }
            break;

        case TaskMode.Landmark:
            if (landmarkTask is LandmarkTask landmark)
            {
                blockType = landmark.GetCurrentBlockName();
                trialIndex = landmark.GetCurrentTrialNumber();
            }
            break;

        case TaskMode.Exposure:
            blockType = "Exposure";
            if (exposureTask is ExposureTask exposure)
            {
                attemptIndex = exposure.GetCurrentAttemptNumber();
                targetIndex = exposure.GetCurrentTargetIndex();
            }
            break;
    }
}

// Maps a TaskMode enum to the corresponding task component.
MonoBehaviour GetTaskComponent(TaskMode mode)
{
    return mode switch
    {
        TaskMode.OpenLoop => openLoopTask,
        TaskMode.LineBisection => lineBisectionTask,
        TaskMode.Landmark => landmarkTask,
        TaskMode.Exposure => exposureTask,
        _ => null
    };
}

// Convenience classification used throughout the experiment flow code.
bool IsMeasurementTask(TaskMode mode)
{
    return mode == TaskMode.OpenLoop ||
           mode == TaskMode.LineBisection ||
           mode == TaskMode.Landmark;
}

// Reflection helper for optional task methods with no parameters.
//
// Reflection is used here so SandboxRunner can ask a task:
// "Do you happen to have a method with this name?"
// without needing every task to share one rigid implementation class.
//
// That makes the runner more flexible, but it also means these calls are a little more
// indirect than ordinary method calls.
static void TryInvokeNoArg(MonoBehaviour target, string methodName)
{
    if (target == null) return;

    var method = target.GetType().GetMethod(
        methodName,
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
        null,
        Type.EmptyTypes,
        null);

    method?.Invoke(target, null);
}

// Reflection helper for optional task methods that accept a single enum parameter,
// for example StartNewBlock(Baseline/Post).
//
// The string enumValueName is converted into whatever enum type that task expects.
// This lets SandboxRunner say "start the Baseline block" without hard-coding the exact
// enum type used inside each task script.
static void TryInvokeBlockMethod(MonoBehaviour target, string methodName, string enumValueName)
{
    if (target == null) return;

    var methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    foreach (var method in methods)
    {
        if (method.Name != methodName) continue;

        var ps = method.GetParameters();
        if (ps.Length != 1) continue;

        var paramType = ps[0].ParameterType;
        if (!paramType.IsEnum) continue;

        try
        {
            object enumValue = Enum.Parse(paramType, enumValueName);
            method.Invoke(target, new[] { enumValue });
            return;
        }
        catch { return; }
    }
}
}
