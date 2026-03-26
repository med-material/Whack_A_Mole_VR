using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrismSampleLogger : MonoBehaviour
{
    [SerializeField] private LoggingManager loggingManager;
    [SerializeField] private SandboxRunner runner;
    [SerializeField] private float samplingFrequencySeconds = 0.02f;

    private Coroutine sampleCoroutine;

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

    void Awake()
    {
        if (loggingManager == null)
            loggingManager = GetComponent<LoggingManager>() ?? GameObject.Find("Logging")?.GetComponent<LoggingManager>() ?? FindFirstObjectByType<LoggingManager>();

        if (runner == null)
            runner = FindFirstObjectByType<SandboxRunner>();

        if (loggingManager != null)
            loggingManager.CreateLog("Sample", SampleHeaders);
    }

    void OnEnable()
    {
        if (sampleCoroutine == null)
            sampleCoroutine = StartCoroutine(SampleLoop());
    }

    void OnDisable()
    {
        if (sampleCoroutine != null)
        {
            StopCoroutine(sampleCoroutine);
            sampleCoroutine = null;
        }
    }

    IEnumerator SampleLoop()
    {
        float waitSeconds = Mathf.Max(0.005f, samplingFrequencySeconds);
        var wait = new WaitForSeconds(waitSeconds);

        while (true)
        {
            if (loggingManager != null && runner != null)
                loggingManager.Log("Sample", BuildSampleRow());

            yield return wait;
        }
    }

    Dictionary<string, object> BuildSampleRow()
    {
        var (ray, pose, confirm) = runner.GetTransformedInput();
        runner.GetLoggingContext(out string blockType, out int? trialIndex, out int? attemptIndex, out int? targetIndex);

        Transform hmd = Camera.main != null ? Camera.main.transform : null;
        Quaternion hmdRotation = hmd != null ? hmd.rotation : Quaternion.identity;
        Vector3 hmdPosition = hmd != null ? hmd.position : Vector3.zero;
        Vector3 hmdEuler = hmd != null ? hmd.eulerAngles : Vector3.zero;
        bool hasRightController = runner.TryGetControllerPose(SandboxRunner.Handedness.Right, out Pose rightControllerPose, out Pose rightLaserPose);
        bool hasLeftController = runner.TryGetControllerPose(SandboxRunner.Handedness.Left, out Pose leftControllerPose, out Pose leftLaserPose);
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

        return new Dictionary<string, object>
        {
            { "Event", "Sample" },
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
            { "IsHandPointing", runner.IsHandPointing ? 1 : 0 },
            { "HandDwellProgress01", runner.HandDwellProgress01 },
            { "ConfirmDown", confirm ? 1 : 0 },
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
            { "HmdPosX", hmdPosition.x },
            { "HmdPosY", hmdPosition.y },
            { "HmdPosZ", hmdPosition.z },
            { "HmdRotX", hmdRotation.x },
            { "HmdRotY", hmdRotation.y },
            { "HmdRotZ", hmdRotation.z },
            { "HmdRotW", hmdRotation.w },
            { "HeadCameraPosWorldX", hmdPosition.x },
            { "HeadCameraPosWorldY", hmdPosition.y },
            { "HeadCameraPosWorldZ", hmdPosition.z },
            { "HeadCameraRotEulerX", hmdEuler.x },
            { "HeadCameraRotEulerY", hmdEuler.y },
            { "HeadCameraRotEulerZ", hmdEuler.z },
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
