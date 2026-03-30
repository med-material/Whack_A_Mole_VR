using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// PrismSampleLogger is the "continuous snapshot" counterpart to PrismExperimentLogger.
//
// Whereas PrismExperimentLogger writes the important discrete moments of a run
// (trial accepted, block started, mole hit, summary rows, etc.),
// PrismSampleLogger writes a regularly spaced stream of samples.
//
// This is the file that later supports:
// - trajectory plots,
// - speed profiles,
// - acceleration/deceleration/hover analysis,
// - and other time-series inspection in RShiny.
//
// In practical terms, every sample row captures:
// - the current task/block context,
// - the currently transformed pointer ray,
// - the HMD pose,
// - controller and ray poses when available,
// - movement-anchor poses,
// - and the current confirm/hand-dwell state.
public class PrismSampleLogger : MonoBehaviour
{
    // Shared low-level logging backend.
    [SerializeField] private LoggingManager loggingManager;
    // Runner provides the unified input ray and the current experiment context.
    [SerializeField] private SandboxRunner runner;
    // Sampling interval in seconds. 0.02 means about 50 Hz.
    [SerializeField] private float samplingFrequencySeconds = 0.02f;

    private Coroutine sampleCoroutine;

    // Sample CSV schema.
    //
    // Like the event schema, this is deliberately wider than any single mode needs so the file
    // shape stays stable across controller/embodied mode and across tasks.
    static readonly List<string> SampleHeaders = new List<string>
    {
        "Event",
        "TaskMode",
        "BlockType",
        "EffectMode",
        "XRBackend",
        "TrackingMode",
        "ActiveHand",
        "ConfirmMitigationMode",
        "TrialIndex",
        "AttemptIndex",
        "TargetIndex",
        "IsHandPointing",
        "HandDwellProgress01",
        "ConfirmDown",
        "PointerOriginX",
        "PointerOriginY",
        "PointerOriginZ",
        "PointerForwardX",
        "PointerForwardY",
        "PointerForwardZ",
        "PointerRotationX",
        "PointerRotationY",
        "PointerRotationZ",
        "PointerRotationW",
        "HmdPosX",
        "HmdPosY",
        "HmdPosZ",
        "HmdRotX",
        "HmdRotY",
        "HmdRotZ",
        "HmdRotW",
        "HeadCameraPosWorldX",
        "HeadCameraPosWorldY",
        "HeadCameraPosWorldZ",
        "HeadCameraRotEulerX",
        "HeadCameraRotEulerY",
        "HeadCameraRotEulerZ",
        "RightControllerPosWorldX",
        "RightControllerPosWorldY",
        "RightControllerPosWorldZ",
        "RightControllerRotEulerX",
        "RightControllerRotEulerY",
        "RightControllerRotEulerZ",
        "RightControllerLaserPosWorldX",
        "RightControllerLaserPosWorldY",
        "RightControllerLaserPosWorldZ",
        "RightControllerLaserRotEulerX",
        "RightControllerLaserRotEulerY",
        "RightControllerLaserRotEulerZ",
        "RightControllerTrigger",
        "RightMovementAnchorPosWorldX",
        "RightMovementAnchorPosWorldY",
        "RightMovementAnchorPosWorldZ",
        "RightMovementAnchorRotEulerX",
        "RightMovementAnchorRotEulerY",
        "RightMovementAnchorRotEulerZ",
        "RightMovementAnchorSource",
        "LeftControllerPosWorldX",
        "LeftControllerPosWorldY",
        "LeftControllerPosWorldZ",
        "LeftControllerRotEulerX",
        "LeftControllerRotEulerY",
        "LeftControllerRotEulerZ",
        "LeftControllerLaserPosWorldX",
        "LeftControllerLaserPosWorldY",
        "LeftControllerLaserPosWorldZ",
        "LeftControllerLaserRotEulerX",
        "LeftControllerLaserRotEulerY",
        "LeftControllerLaserRotEulerZ",
        "LeftControllerTrigger",
        "LeftMovementAnchorPosWorldX",
        "LeftMovementAnchorPosWorldY",
        "LeftMovementAnchorPosWorldZ",
        "LeftMovementAnchorRotEulerX",
        "LeftMovementAnchorRotEulerY",
        "LeftMovementAnchorRotEulerZ",
        "LeftMovementAnchorSource",
    };

    // Prepare references and create the Sample collection if the logging backend exists.
    void Awake()
    {
        if (loggingManager == null)
            loggingManager = GetComponent<LoggingManager>() ?? GameObject.Find("Logging")?.GetComponent<LoggingManager>() ?? FindFirstObjectByType<LoggingManager>();

        if (runner == null)
            runner = FindFirstObjectByType<SandboxRunner>();

        if (loggingManager != null)
            loggingManager.CreateLog("Sample", SampleHeaders);
    }

    // Start the background sampling coroutine whenever this component becomes enabled.
    void OnEnable()
    {
        if (sampleCoroutine == null)
            sampleCoroutine = StartCoroutine(SampleLoop());
    }

    // Stop sampling cleanly if the component is disabled.
    void OnDisable()
    {
        if (sampleCoroutine != null)
        {
            StopCoroutine(sampleCoroutine);
            sampleCoroutine = null;
        }
    }

    // Simple timed loop that appends one sample row, waits, then repeats.
    //
    // The minimum wait is clamped so extremely small Inspector values do not accidentally
    // create an unreasonable sample rate.
    IEnumerator SampleLoop()
    {
        float waitSeconds = Mathf.Max(0.005f, samplingFrequencySeconds);
        var wait = new WaitForSeconds(waitSeconds);

        while (true)
        {
            if (loggingManager != null && runner != null)
                // Build one complete sample row from the current live application state.
                loggingManager.Log("Sample", BuildSampleRow());

            yield return wait;
        }
    }

    // Creates one Sample row.
    //
    // This method is intentionally long because the row is a complete "snapshot" of the current
    // state at one instant in time. The row combines:
    // - experiment context from SandboxRunner,
    // - the transformed pointer ray,
    // - HMD pose,
    // - controller body/ray poses,
    // - movement-anchor poses,
    // - input-state flags such as confirm and dwell.
    Dictionary<string, object> BuildSampleRow()
    {
        // Unified pointer input after any currently active effect has been applied.
        var (ray, pose, confirm) = runner.GetTransformedInput();
        // Current task-local indexing context so samples can be tied to a trial/attempt/target later.
        runner.GetLoggingContext(out string blockType, out int? trialIndex, out int? attemptIndex, out int? targetIndex);

        // HMD pose is duplicated in both quaternion and Euler form for convenience in downstream analysis.
        Transform hmd = Camera.main != null ? Camera.main.transform : null;
        Quaternion hmdRotation = hmd != null ? hmd.rotation : Quaternion.identity;
        Vector3 hmdPosition = hmd != null ? hmd.position : Vector3.zero;
        Vector3 hmdEuler = hmd != null ? hmd.eulerAngles : Vector3.zero;
        // Controller body pose + controller ray pose for each hand, when available.
        bool hasRightController = runner.TryGetControllerPose(SandboxRunner.Handedness.Right, out Pose rightControllerPose, out Pose rightLaserPose);
        bool hasLeftController = runner.TryGetControllerPose(SandboxRunner.Handedness.Left, out Pose leftControllerPose, out Pose leftLaserPose);
        // Movement anchors are especially important for embodied-mode movement-speed analysis.
        bool hasRightMovementAnchor = runner.TryGetMovementAnchorPose(SandboxRunner.Handedness.Right, out Pose rightMovementAnchorPose, out string rightMovementAnchorSource);
        bool hasLeftMovementAnchor = runner.TryGetMovementAnchorPose(SandboxRunner.Handedness.Left, out Pose leftMovementAnchorPose, out string leftMovementAnchorSource);
        Vector3 rightControllerEuler = hasRightController ? rightControllerPose.rotation.eulerAngles : Vector3.zero;
        Vector3 rightLaserEuler = hasRightController ? rightLaserPose.rotation.eulerAngles : Vector3.zero;
        Vector3 rightMovementAnchorEuler = hasRightMovementAnchor ? rightMovementAnchorPose.rotation.eulerAngles : Vector3.zero;
        Vector3 leftControllerEuler = hasLeftController ? leftControllerPose.rotation.eulerAngles : Vector3.zero;
        Vector3 leftLaserEuler = hasLeftController ? leftLaserPose.rotation.eulerAngles : Vector3.zero;
        Vector3 leftMovementAnchorEuler = hasLeftMovementAnchor ? leftMovementAnchorPose.rotation.eulerAngles : Vector3.zero;
        bool rightTrigger = runner.GetControllerTriggerState(SandboxRunner.Handedness.Right);
        bool leftTrigger = runner.GetControllerTriggerState(SandboxRunner.Handedness.Left);

        // Return the complete snapshot as one dictionary row.
        return new Dictionary<string, object>
        {
            { "Event", "Sample" },
            // High-level context.
            { "TaskMode", runner.CurrentTaskMode.ToString() },
            { "BlockType", blockType },
            { "EffectMode", runner.CurrentAppliedEffectMode.ToString() },
            { "XRBackend", runner.CurrentXRBackend.ToString() },
            { "TrackingMode", runner.CurrentOpenXRTrackingMode.ToString() },
            { "ActiveHand", runner.CurrentActiveHand.ToString() },
            { "ConfirmMitigationMode", runner.CurrentConfirmMitigationMode },
            { "TrialIndex", trialIndex.HasValue ? trialIndex.Value : "" },
            { "AttemptIndex", attemptIndex.HasValue ? attemptIndex.Value : "" },
            { "TargetIndex", targetIndex.HasValue ? targetIndex.Value : "" },
            // Live hand/confirmation state.
            { "IsHandPointing", runner.IsHandPointing ? 1 : 0 },
            { "HandDwellProgress01", runner.HandDwellProgress01 },
            { "ConfirmDown", confirm ? 1 : 0 },
            // Transformed pointer ray and pose used by the active task logic.
            { "PointerOriginX", ray.origin.x },
            { "PointerOriginY", ray.origin.y },
            { "PointerOriginZ", ray.origin.z },
            { "PointerForwardX", ray.direction.x },
            { "PointerForwardY", ray.direction.y },
            { "PointerForwardZ", ray.direction.z },
            { "PointerRotationX", pose.rotation.x },
            { "PointerRotationY", pose.rotation.y },
            { "PointerRotationZ", pose.rotation.z },
            { "PointerRotationW", pose.rotation.w },
            // HMD pose in quaternion form.
            { "HmdPosX", hmdPosition.x },
            { "HmdPosY", hmdPosition.y },
            { "HmdPosZ", hmdPosition.z },
            { "HmdRotX", hmdRotation.x },
            { "HmdRotY", hmdRotation.y },
            { "HmdRotZ", hmdRotation.z },
            { "HmdRotW", hmdRotation.w },
            // HMD pose duplicated in the older head-camera naming expected by some analysis views.
            { "HeadCameraPosWorldX", hmdPosition.x },
            { "HeadCameraPosWorldY", hmdPosition.y },
            { "HeadCameraPosWorldZ", hmdPosition.z },
            { "HeadCameraRotEulerX", hmdEuler.x },
            { "HeadCameraRotEulerY", hmdEuler.y },
            { "HeadCameraRotEulerZ", hmdEuler.z },
            // Right-hand controller and ray pose.
            { "RightControllerPosWorldX", hasRightController ? rightControllerPose.position.x : "" },
            { "RightControllerPosWorldY", hasRightController ? rightControllerPose.position.y : "" },
            { "RightControllerPosWorldZ", hasRightController ? rightControllerPose.position.z : "" },
            { "RightControllerRotEulerX", hasRightController ? rightControllerEuler.x : "" },
            { "RightControllerRotEulerY", hasRightController ? rightControllerEuler.y : "" },
            { "RightControllerRotEulerZ", hasRightController ? rightControllerEuler.z : "" },
            { "RightControllerLaserPosWorldX", hasRightController ? rightLaserPose.position.x : "" },
            { "RightControllerLaserPosWorldY", hasRightController ? rightLaserPose.position.y : "" },
            { "RightControllerLaserPosWorldZ", hasRightController ? rightLaserPose.position.z : "" },
            { "RightControllerLaserRotEulerX", hasRightController ? rightLaserEuler.x : "" },
            { "RightControllerLaserRotEulerY", hasRightController ? rightLaserEuler.y : "" },
            { "RightControllerLaserRotEulerZ", hasRightController ? rightLaserEuler.z : "" },
            { "RightControllerTrigger", rightTrigger ? 1 : 0 },
            { "RightMovementAnchorPosWorldX", hasRightMovementAnchor ? rightMovementAnchorPose.position.x : "" },
            { "RightMovementAnchorPosWorldY", hasRightMovementAnchor ? rightMovementAnchorPose.position.y : "" },
            { "RightMovementAnchorPosWorldZ", hasRightMovementAnchor ? rightMovementAnchorPose.position.z : "" },
            { "RightMovementAnchorRotEulerX", hasRightMovementAnchor ? rightMovementAnchorEuler.x : "" },
            { "RightMovementAnchorRotEulerY", hasRightMovementAnchor ? rightMovementAnchorEuler.y : "" },
            { "RightMovementAnchorRotEulerZ", hasRightMovementAnchor ? rightMovementAnchorEuler.z : "" },
            { "RightMovementAnchorSource", hasRightMovementAnchor ? rightMovementAnchorSource : "" },
            // Left-hand controller and ray pose.
            { "LeftControllerPosWorldX", hasLeftController ? leftControllerPose.position.x : "" },
            { "LeftControllerPosWorldY", hasLeftController ? leftControllerPose.position.y : "" },
            { "LeftControllerPosWorldZ", hasLeftController ? leftControllerPose.position.z : "" },
            { "LeftControllerRotEulerX", hasLeftController ? leftControllerEuler.x : "" },
            { "LeftControllerRotEulerY", hasLeftController ? leftControllerEuler.y : "" },
            { "LeftControllerRotEulerZ", hasLeftController ? leftControllerEuler.z : "" },
            { "LeftControllerLaserPosWorldX", hasLeftController ? leftLaserPose.position.x : "" },
            { "LeftControllerLaserPosWorldY", hasLeftController ? leftLaserPose.position.y : "" },
            { "LeftControllerLaserPosWorldZ", hasLeftController ? leftLaserPose.position.z : "" },
            { "LeftControllerLaserRotEulerX", hasLeftController ? leftLaserEuler.x : "" },
            { "LeftControllerLaserRotEulerY", hasLeftController ? leftLaserEuler.y : "" },
            { "LeftControllerLaserRotEulerZ", hasLeftController ? leftLaserEuler.z : "" },
            { "LeftControllerTrigger", leftTrigger ? 1 : 0 },
            { "LeftMovementAnchorPosWorldX", hasLeftMovementAnchor ? leftMovementAnchorPose.position.x : "" },
            { "LeftMovementAnchorPosWorldY", hasLeftMovementAnchor ? leftMovementAnchorPose.position.y : "" },
            { "LeftMovementAnchorPosWorldZ", hasLeftMovementAnchor ? leftMovementAnchorPose.position.z : "" },
            { "LeftMovementAnchorRotEulerX", hasLeftMovementAnchor ? leftMovementAnchorEuler.x : "" },
            { "LeftMovementAnchorRotEulerY", hasLeftMovementAnchor ? leftMovementAnchorEuler.y : "" },
            { "LeftMovementAnchorRotEulerZ", hasLeftMovementAnchor ? leftMovementAnchorEuler.z : "" },
            { "LeftMovementAnchorSource", hasLeftMovementAnchor ? leftMovementAnchorSource : "" },
        };
    }
}
