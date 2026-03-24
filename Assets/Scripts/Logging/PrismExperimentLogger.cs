using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PrismExperimentLogger : MonoBehaviour
{
    [SerializeField] private LoggingManager loggingManager;
    [SerializeField] private SandboxRunner runner;
    [SerializeField] private bool saveLogsOnApplicationQuit = true;

    private const string EventCollection = "Event";
    private const string SummaryCollection = "Summary";
    private string resolvedSavePath;
    private bool hasSavedLogs;
    private bool hasSummaryRows;
    private float sessionStartTime = -1f;
    private bool gameStartedLogged;
    private float previousEventTime = -1f;
    private string gameId;

    static readonly List<string> EventHeaders = new List<string>
    {
        "Event",
        "EventType",
        "TaskMode",
        "BlockType",
        "EffectMode",
        "XRBackend",
        "TrackingMode",
        "ActiveHand",
        "TrialIndex",
        "TrialsPerBlock",
        "AttemptIndex",
        "SuccessCount",
        "TargetIndex",
        "IsHit",
        "IsCorrect",
        "ChoiceSide",
        "LongerSide",
        "SignedOffsetCm",
        "ErrorCm",
        "Accuracy01",
        "AfterEffectCm",
        "DeltaAccuracyPct",
        "MeanCm",
        "SdCm",
        "HitDistanceMeters",
        "LineZMeters",
        "LineLengthMeters",
        "GapMeters",
        "LeftLengthMeters",
        "RightLengthMeters",
        "TargetWorldX",
        "TargetWorldY",
        "TargetWorldZ",
        "HitWorldX",
        "HitWorldY",
        "HitWorldZ",
        "SourceTaskMode",
        "MoleId",
        "MoleIndexX",
        "MoleIndexY",
        "MolePositionWorldX",
        "MolePositionWorldY",
        "MolePositionWorldZ",
        "MolePositionLocalX",
        "MolePositionLocalY",
        "MolePositionLocalZ",
        "HitPositionWorldX",
        "HitPositionWorldY",
        "HitPositionWorldZ",
        "CurrentMoleToHitId",
        "CurrentMoleToHitIndexX",
        "CurrentMoleToHitIndexY",
        "CurrentMoleToHitPositionWorldX",
        "CurrentMoleToHitPositionWorldY",
        "CurrentMoleToHitPositionWorldZ",
        "CurrentMoleToHitPositionLocalX",
        "CurrentMoleToHitPositionLocalY",
        "CurrentMoleToHitPositionLocalZ",
        "TimeSinceLastEvent",
        "GameId",
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
    };

    static readonly List<string> SummaryHeaders = new List<string>
    {
        "TaskMode",
        "BlockType",
        "SummaryType",
        "MetricName",
        "MetricUnits",
        "ConfiguredEffectMode",
        "AppliedEffectMode",
        "XRBackend",
        "TrackingMode",
        "ActiveHand",
        "TrialCount",
        "BaselineValue",
        "BaselineSd",
        "PostValue",
        "PostSd",
        "SignedDelta",
        "Magnitude",
        "NormalizedMagnitude",
        "Notes",
    };

    void Awake()
    {
        if (loggingManager == null)
            loggingManager = GetComponent<LoggingManager>() ?? GameObject.Find("Logging")?.GetComponent<LoggingManager>() ?? FindFirstObjectByType<LoggingManager>();

        if (runner == null)
            runner = FindFirstObjectByType<SandboxRunner>();

        if (loggingManager != null)
        {
            resolvedSavePath = Path.Combine(Application.dataPath, "PrismLogging");
            Directory.CreateDirectory(resolvedSavePath);
            loggingManager.SetSavePath(resolvedSavePath);
            UpdateFilePrefixFromRunner();
            loggingManager.CreateLog(EventCollection, EventHeaders);
        }
    }

    void Start()
    {
        if (loggingManager == null)
            return;

        loggingManager.Log("Meta", "Product", "PrismEffectAdaptation");
        loggingManager.Log("Meta", "Scene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        if (runner != null)
        {
            loggingManager.Log("Meta", "RightControllerMain", runner.CurrentActiveHand == SandboxRunner.Handedness.Right ? "TRUE" : "FALSE");
            loggingManager.Log("Meta", "MainController", runner.CurrentActiveHand == SandboxRunner.Handedness.Right ? "Right Controller" : "Left Controller");
            loggingManager.Log("Meta", "TrackingMode", runner.CurrentOpenXRTrackingMode.ToString());
            loggingManager.Log("Meta", "InputModeLabel", GetInputModeLabel());
            loggingManager.Log("Meta", "XRBackend", runner.CurrentXRBackend.ToString());
        }
    }

    void OnApplicationQuit()
    {
        LogAbortedSessionIfNeeded();

        if (saveLogsOnApplicationQuit)
            SaveLogs();
    }

    public void SaveLogs()
    {
        if (hasSavedLogs || loggingManager == null)
            return;

        UpdateFilePrefixFromRunner();
        hasSavedLogs = true;
        loggingManager.SaveAllLogs(clear: false);
    }

    void UpdateFilePrefixFromRunner()
    {
        if (loggingManager == null)
            return;

        string mode = GetInputModeLabel().ToLowerInvariant();
        loggingManager.SetFilePrefix($"prism_{mode}");
    }

    string GetInputModeLabel()
    {
        if (runner == null)
            return "unknown";

        return runner.CurrentOpenXRTrackingMode == SandboxRunner.OpenXRTrackingMode.Hands
            ? "embodied"
            : "controller";
    }

    public void LogBlockStarted(string taskMode, string blockType)
    {
        if (!gameStartedLogged)
        {
            gameStartedLogged = true;
            sessionStartTime = Time.time;
            LogEvent("Game Started", "GameEvent", taskMode, blockType, null);
        }

        LogEvent("Block Started", "BlockEvent", taskMode, blockType, null);
    }

    public void LogExposureStarted(string sourceTaskMode)
    {
        var data = new Dictionary<string, object>
        {
            { "SourceTaskMode", sourceTaskMode }
        };
        LogEvent("Exposure Started", "BlockEvent", "Exposure", "Exposure", data);
    }

    public void LogExposureConfig(float hitRadiusMeters)
    {
        if (loggingManager == null)
            return;

        loggingManager.Log("Meta", "ExposureHitRadiusMeters", hitRadiusMeters);
        loggingManager.Log("Meta", "ExposureHitRadiusCm", hitRadiusMeters * 100f);
    }

    public void LogMeasurementTrial(string taskMode, string blockType, int trialIndex, int trialsPerBlock, Vector3 hitWorld, Dictionary<string, object> extraData)
    {
        var data = new Dictionary<string, object>
        {
            { "TrialIndex", trialIndex },
            { "TrialsPerBlock", trialsPerBlock },
            { "HitWorldX", hitWorld.x },
            { "HitWorldY", hitWorld.y },
            { "HitWorldZ", hitWorld.z },
        };

        Merge(data, extraData);
        LogEvent("Trial Accepted", "TaskEvent", taskMode, blockType, data);
    }

    public void LogBlockCompleted(string taskMode, string blockType, Dictionary<string, object> summaryData)
    {
        LogEvent("Block Completed", "BlockEvent", taskMode, blockType, summaryData);
    }

    public void LogTaskMetricSummary(
        string taskMode,
        string blockType,
        string metricName,
        string metricUnits,
        int trialCount,
        float metricValue,
        float? metricSd = null,
        string notes = null)
    {
        if (loggingManager == null)
            return;

        EnsureSummaryCollection();
        var data = CreateSummaryRow(taskMode, blockType, "BlockMetric", metricName, metricUnits, trialCount, notes);
        data["PostValue"] = metricValue;
        data["PostSd"] = metricSd.HasValue ? metricSd.Value : "";
        loggingManager.Log(SummaryCollection, data);
        hasSummaryRows = true;
    }

    public void LogTaskAftereffectSummary(
        string taskMode,
        string metricName,
        string metricUnits,
        int trialCount,
        float baselineValue,
        float? baselineSd,
        float postValue,
        float? postSd,
        float signedDelta,
        string notes = null)
    {
        if (loggingManager == null)
            return;

        EnsureSummaryCollection();
        float magnitude = Mathf.Abs(signedDelta);
        float? normalizedMagnitude = null;
        if (baselineSd.HasValue && baselineSd.Value > 0.0001f)
            normalizedMagnitude = magnitude / baselineSd.Value;

        var data = CreateSummaryRow(taskMode, "Post", "Aftereffect", metricName, metricUnits, trialCount, notes);
        data["BaselineValue"] = baselineValue;
        data["BaselineSd"] = baselineSd.HasValue ? baselineSd.Value : "";
        data["PostValue"] = postValue;
        data["PostSd"] = postSd.HasValue ? postSd.Value : "";
        data["SignedDelta"] = signedDelta;
        data["Magnitude"] = magnitude;
        data["NormalizedMagnitude"] = normalizedMagnitude.HasValue ? normalizedMagnitude.Value : "";
        loggingManager.Log(SummaryCollection, data);
        hasSummaryRows = true;
    }

    void EnsureSummaryCollection()
    {
        if (loggingManager == null || hasSummaryRows)
            return;

        loggingManager.CreateLog(SummaryCollection, SummaryHeaders);
    }

    public void LogExposureAttempt(int attemptIndex, int successCount, int targetIndex, bool isHit, float hitDistanceMeters, Vector3 hitWorld, Vector3 targetWorld)
    {
        var data = new Dictionary<string, object>
        {
            { "AttemptIndex", attemptIndex },
            { "SuccessCount", successCount },
            { "TargetIndex", targetIndex },
            { "IsHit", isHit ? 1 : 0 },
            { "HitDistanceMeters", hitDistanceMeters },
            { "HitWorldX", hitWorld.x },
            { "HitWorldY", hitWorld.y },
            { "HitWorldZ", hitWorld.z },
            { "TargetWorldX", targetWorld.x },
            { "TargetWorldY", targetWorld.y },
            { "TargetWorldZ", targetWorld.z },
            { "MoleId", targetIndex + 1 },
            { "MoleIndexX", targetIndex },
            { "MoleIndexY", 0 },
            { "MolePositionWorldX", targetWorld.x },
            { "MolePositionWorldY", targetWorld.y },
            { "MolePositionWorldZ", targetWorld.z },
            { "MolePositionLocalX", targetIndex - 1 },
            { "MolePositionLocalY", 0 },
            { "MolePositionLocalZ", 0 },
            { "HitPositionWorldX", hitWorld.x },
            { "HitPositionWorldY", hitWorld.y },
            { "HitPositionWorldZ", hitWorld.z },
        };

        LogEvent(isHit ? "Mole Hit" : "Mole Missed", isHit ? "MoleEvent" : "PointerEvent", "Exposure", "Exposure", data);
    }

    public void LogPointerShoot(int attemptIndex, int successCount, int targetIndex, Vector3 hitWorld, Vector3 targetWorld)
    {
        var data = new Dictionary<string, object>
        {
            { "AttemptIndex", attemptIndex },
            { "SuccessCount", successCount },
            { "TargetIndex", targetIndex },
            { "HitWorldX", hitWorld.x },
            { "HitWorldY", hitWorld.y },
            { "HitWorldZ", hitWorld.z },
            { "TargetWorldX", targetWorld.x },
            { "TargetWorldY", targetWorld.y },
            { "TargetWorldZ", targetWorld.z },
            { "MoleId", targetIndex + 1 },
            { "MoleIndexX", targetIndex },
            { "MoleIndexY", 0 },
            { "MolePositionWorldX", targetWorld.x },
            { "MolePositionWorldY", targetWorld.y },
            { "MolePositionWorldZ", targetWorld.z },
            { "MolePositionLocalX", targetIndex - 1 },
            { "MolePositionLocalY", 0 },
            { "MolePositionLocalZ", 0 },
            { "HitPositionWorldX", hitWorld.x },
            { "HitPositionWorldY", hitWorld.y },
            { "HitPositionWorldZ", hitWorld.z },
        };

        LogEvent("Pointer Shoot", "PointerEvent", "Exposure", "Exposure", data);
    }

    public void LogExposureCompleted(int successCount, int attemptCount)
    {
        var data = new Dictionary<string, object>
        {
            { "SuccessCount", successCount },
            { "AttemptIndex", attemptCount }
        };
        LogEvent("Exposure Completed", "BlockEvent", "Exposure", "Exposure", data);
    }

    public void LogExposureTargetSpawned(int targetIndex, Vector3 targetWorld)
    {
        var data = new Dictionary<string, object>
        {
            { "TargetIndex", targetIndex },
            { "TargetWorldX", targetWorld.x },
            { "TargetWorldY", targetWorld.y },
            { "TargetWorldZ", targetWorld.z },
            { "MoleId", targetIndex + 1 },
            { "MoleIndexX", targetIndex },
            { "MoleIndexY", 0 },
            { "MolePositionWorldX", targetWorld.x },
            { "MolePositionWorldY", targetWorld.y },
            { "MolePositionWorldZ", targetWorld.z },
            { "MolePositionLocalX", targetIndex - 1 },
            { "MolePositionLocalY", 0 },
            { "MolePositionLocalZ", 0 },
        };

        LogEvent("Mole Spawned", "MoleEvent", "Exposure", "Exposure", data);
    }

    public void LogExperimentCompleted(string taskMode, string blockType)
    {
        loggingManager?.Log("Meta", "SessionState", "Finished");
        loggingManager?.Log("Meta", "SessionDuration", sessionStartTime >= 0f ? Time.time - sessionStartTime : 0f);
        LogEvent("Game Finished", "GameEvent", taskMode, blockType, null);
        LogEvent("Experiment Completed", "GameEvent", taskMode, blockType, null);
    }

    void LogAbortedSessionIfNeeded()
    {
        if (loggingManager == null)
            return;

        if (runner != null && runner.IsExperimentCompleted)
            return;

        loggingManager.Log("Meta", "SessionState", "Aborted");
        loggingManager.Log("Meta", "SessionDuration", sessionStartTime >= 0f ? Time.time - sessionStartTime : 0f);
        LogEvent("Game Finished", "GameEvent", runner != null ? runner.CurrentTaskMode.ToString() : "Unknown", "Aborted", null);
        LogEvent("Experiment Aborted", "GameEvent", runner != null ? runner.CurrentTaskMode.ToString() : "Unknown", "Aborted", null);
    }

    void LogEvent(string eventName, string eventType, string taskMode, string blockType, Dictionary<string, object> extraData)
    {
        if (loggingManager == null)
            return;

        var data = new Dictionary<string, object>
        {
            { "Event", eventName },
            { "EventType", eventType },
            { "TaskMode", taskMode },
            { "BlockType", blockType },
            { "TimeSinceLastEvent", GetTimeSinceLastEvent() },
            { "GameId", GetGameId() },
        };

        if (runner != null)
        {
            data["EffectMode"] = runner.CurrentAppliedEffectMode.ToString();
            data["XRBackend"] = runner.CurrentXRBackend.ToString();
            data["TrackingMode"] = runner.CurrentOpenXRTrackingMode.ToString();
            data["ActiveHand"] = runner.CurrentActiveHand.ToString();
            AddTrackerSnapshot(data);
        }

        Merge(data, extraData);
        CopyCurrentMoleFields(data);
        loggingManager.Log(EventCollection, data);
    }

    static void Merge(Dictionary<string, object> destination, Dictionary<string, object> source)
    {
        if (source == null)
            return;

        foreach (var pair in source)
            destination[pair.Key] = pair.Value;
    }

    string GetGameId()
    {
        if (string.IsNullOrEmpty(gameId))
            gameId = Guid.NewGuid().ToString();

        return gameId;
    }

    float GetTimeSinceLastEvent()
    {
        if (previousEventTime < 0f)
        {
            previousEventTime = Time.time;
            return 0f;
        }

        float delta = Mathf.Max(0f, Time.time - previousEventTime);
        previousEventTime = Time.time;
        return delta;
    }

    void AddTrackerSnapshot(Dictionary<string, object> data)
    {
        Transform hmd = Camera.main != null ? Camera.main.transform : null;
        Vector3 hmdPos = hmd != null ? hmd.position : Vector3.zero;
        Vector3 hmdEuler = hmd != null ? hmd.eulerAngles : Vector3.zero;

        data["HeadCameraPosWorldX"] = hmdPos.x;
        data["HeadCameraPosWorldY"] = hmdPos.y;
        data["HeadCameraPosWorldZ"] = hmdPos.z;
        data["HeadCameraRotEulerX"] = hmdEuler.x;
        data["HeadCameraRotEulerY"] = hmdEuler.y;
        data["HeadCameraRotEulerZ"] = hmdEuler.z;

        AddControllerSnapshot(data, SandboxRunner.Handedness.Right, "RightController");
        AddControllerSnapshot(data, SandboxRunner.Handedness.Left, "LeftController");
    }

    static void CopyCurrentMoleFields(Dictionary<string, object> data)
    {
        if (!data.TryGetValue("MoleId", out object moleId))
            return;

        CopyIfPresent(data, "MoleId", "CurrentMoleToHitId");
        CopyIfPresent(data, "MoleIndexX", "CurrentMoleToHitIndexX");
        CopyIfPresent(data, "MoleIndexY", "CurrentMoleToHitIndexY");
        CopyIfPresent(data, "MolePositionWorldX", "CurrentMoleToHitPositionWorldX");
        CopyIfPresent(data, "MolePositionWorldY", "CurrentMoleToHitPositionWorldY");
        CopyIfPresent(data, "MolePositionWorldZ", "CurrentMoleToHitPositionWorldZ");
        CopyIfPresent(data, "MolePositionLocalX", "CurrentMoleToHitPositionLocalX");
        CopyIfPresent(data, "MolePositionLocalY", "CurrentMoleToHitPositionLocalY");
        CopyIfPresent(data, "MolePositionLocalZ", "CurrentMoleToHitPositionLocalZ");
    }

    static void CopyIfPresent(Dictionary<string, object> data, string sourceKey, string targetKey)
    {
        if (data.ContainsKey(targetKey))
            return;

        if (data.TryGetValue(sourceKey, out object value))
            data[targetKey] = value;
    }

    void AddControllerSnapshot(Dictionary<string, object> data, SandboxRunner.Handedness hand, string prefix)
    {
        if (runner == null)
            return;

        bool hasPose = runner.TryGetControllerPose(hand, out Pose controllerPose, out Pose rayPose);
        bool trigger = runner.GetControllerTriggerState(hand);

        data[$"{prefix}PosWorldX"] = hasPose ? controllerPose.position.x : "";
        data[$"{prefix}PosWorldY"] = hasPose ? controllerPose.position.y : "";
        data[$"{prefix}PosWorldZ"] = hasPose ? controllerPose.position.z : "";
        data[$"{prefix}RotEulerX"] = hasPose ? controllerPose.rotation.eulerAngles.x : "";
        data[$"{prefix}RotEulerY"] = hasPose ? controllerPose.rotation.eulerAngles.y : "";
        data[$"{prefix}RotEulerZ"] = hasPose ? controllerPose.rotation.eulerAngles.z : "";
        data[$"{prefix}LaserPosWorldX"] = hasPose ? rayPose.position.x : "";
        data[$"{prefix}LaserPosWorldY"] = hasPose ? rayPose.position.y : "";
        data[$"{prefix}LaserPosWorldZ"] = hasPose ? rayPose.position.z : "";
        data[$"{prefix}LaserRotEulerX"] = hasPose ? rayPose.rotation.eulerAngles.x : "";
        data[$"{prefix}LaserRotEulerY"] = hasPose ? rayPose.rotation.eulerAngles.y : "";
        data[$"{prefix}LaserRotEulerZ"] = hasPose ? rayPose.rotation.eulerAngles.z : "";
        data[$"{prefix}Trigger"] = trigger ? 1 : 0;
    }

    Dictionary<string, object> CreateSummaryRow(
        string taskMode,
        string blockType,
        string summaryType,
        string metricName,
        string metricUnits,
        int trialCount,
        string notes)
    {
        var data = new Dictionary<string, object>
        {
            { "TaskMode", taskMode },
            { "BlockType", blockType },
            { "SummaryType", summaryType },
            { "MetricName", metricName },
            { "MetricUnits", metricUnits },
            { "TrialCount", trialCount },
            { "Notes", string.IsNullOrEmpty(notes) ? "" : notes },
        };

        if (runner != null)
        {
            data["ConfiguredEffectMode"] = runner.CurrentEffectMode.ToString();
            data["AppliedEffectMode"] = runner.CurrentAppliedEffectMode.ToString();
            data["XRBackend"] = runner.CurrentXRBackend.ToString();
            data["TrackingMode"] = runner.CurrentOpenXRTrackingMode.ToString();
            data["ActiveHand"] = runner.CurrentActiveHand.ToString();
        }

        return data;
    }
}
