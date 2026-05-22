using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// PrismExperimentLogger is the "discrete event and summary" logger for the PEGFG prototype.
//
// The project uses two custom Prism logging layers on top of the older shared LoggingManager:
// 1. PrismExperimentLogger:
//    - writes session metadata,
//    - writes important discrete events ("Block Started", "Trial Accepted", "Mole Hit", etc.),
//    - writes block summaries and aftereffect summaries.
//
// 2. PrismSampleLogger:
//    - writes regular time-series snapshots while the application is running.
//
// This split is useful analytically:
// - Event/Summary logs answer "what happened and when?"
// - Sample logs answer "what did the continuous trajectory look like between those events?"
//
// This script is therefore responsible for:
// - defining the Event and Summary CSV schemas,
// - creating/maintaining session metadata,
// - recording task-specific accepted responses,
// - translating exposure attempts into event rows that resemble the older mole-style schema,
// - deciding whether a session was Finished or Aborted,
// - and saving everything to disk through LoggingManager.
public class PrismExperimentLogger : MonoBehaviour
{
    // LoggingManager is the older shared saving backend.
    // PrismExperimentLogger builds structured rows, while LoggingManager actually stores and saves them.
    [SerializeField] private LoggingManager loggingManager;
    // Runner is used to query the current task/effect/input mode and pose snapshots.
    [SerializeField] private SandboxRunner runner;
    // Optional convenience: save logs automatically if the application closes.
    [SerializeField] private bool saveLogsOnApplicationQuit = true;

    // Names of the two main collections this logger produces.
    private const string EventCollection = "Event";
    private const string SummaryCollection = "Summary";
    // Resolved path under Assets/PrismLogging where CSVs will be saved.
    private string resolvedSavePath;
    // Prevent multiple saves or duplicate summary collection creation.
    private bool hasSavedLogs;
    private bool hasSummaryRows;
    // Session timing/event state used for duration and inter-event intervals.
    private float sessionStartTime = -1f;
    private bool gameStartedLogged;
    private float previousEventTime = -1f;
    // Stable per-session id that ties rows together across files.
    private string gameId;
    // Ensure Finished/Aborted is written only once.
    private bool sessionStateLogged;
    // Track whether the participant actually went through the intended full experiment structure.
    private bool sawBaselineBlock;
    private bool sawExposureBlock;
    private bool sawPostBlock;

    // Event CSV schema.
    //
    // This list is intentionally broad. Not every row uses every field, but keeping one stable
    // schema makes downstream analysis in R easier because the files do not change shape between
    // tasks or event types.
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
        "ConfirmMitigationMode",
        "ExperimentMode",
        "StudyInputMode",
        "StudyParticipantIndex",
        "StudyGlobalParticipantId",
        "StudySessionIndex",
        "StudyTaskOrder",
        "StudyResolvedTask",
        "StudyResolvedEffect",
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
        "TargetRadiusMeters",
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

    // Summary CSV schema.
    //
    // These rows sit one level above the discrete Event rows:
    // - one row can represent a block metric such as mean bisection error,
    // - or an aftereffect summary such as Post minus Baseline.
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
        "ConfirmMitigationMode",
        "ExperimentMode",
        "StudyInputMode",
        "StudyParticipantIndex",
        "StudyGlobalParticipantId",
        "StudySessionIndex",
        "StudyTaskOrder",
        "StudyResolvedTask",
        "StudyResolvedEffect",
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

    // Awake prepares the logging backend and creates the main Event collection.
    //
    // This is done early so task scripts can safely start writing rows as soon as they begin.
    void Awake()
    {
        if (loggingManager == null)
            loggingManager = GetComponent<LoggingManager>() ?? GameObject.Find("Logging")?.GetComponent<LoggingManager>() ?? FindFirstObjectByType<LoggingManager>();

        if (runner == null)
            runner = FindFirstObjectByType<SandboxRunner>();

        if (loggingManager != null)
        {
            string baseLogPath = Path.Combine(Application.dataPath, "PrismLogging");
            bool saveToParticipantFolder = runner != null && runner.CurrentExperimentMode == SandboxRunner.ExperimentMode.Participant;
            resolvedSavePath = saveToParticipantFolder ? Path.Combine(baseLogPath, "Participants") : baseLogPath;
            Directory.CreateDirectory(resolvedSavePath);
            loggingManager.SetSavePath(resolvedSavePath);
            Debug.Log($"[PrismExperimentLogger] CSV save path resolved to: {resolvedSavePath}");
            UpdateFilePrefixFromRunner();
            loggingManager.CreateLog(EventCollection, EventHeaders);
        }
    }

    // Start writes the metadata that should exist once per session rather than once per event.
    // This becomes the high-level description of the run in RShiny and other analyses.
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
            loggingManager.Log("Meta", "ConfirmMitigationMode", runner.CurrentConfirmMitigationMode);
            loggingManager.Log("Meta", "ExperimentMode", runner.CurrentExperimentMode.ToString());
            loggingManager.Log("Meta", "StudyInputMode", runner.CurrentStudyInputMode.ToString());
            loggingManager.Log("Meta", "StudyParticipantIndex", runner.CurrentStudyParticipantIndex);
            loggingManager.Log("Meta", "StudyGlobalParticipantId", runner.CurrentStudyGlobalParticipantId);
            loggingManager.Log("Meta", "StudySessionIndex", runner.CurrentStudySessionIndex);
            loggingManager.Log("Meta", "StudyTaskOrder", runner.CurrentStudyTaskOrderLabel);
            loggingManager.Log("Meta", "StudyResolvedTask", runner.CurrentStudyResolvedTaskLabel);
            loggingManager.Log("Meta", "StudyResolvedEffect", runner.CurrentStudyResolvedEffectLabel);
        }
    }

    // If the application closes unexpectedly or the user stops play mode, we still try to mark
    // the session as Aborted rather than leaving it without a terminal state.
    void OnApplicationQuit()
    {
        LogAbortedSessionIfNeeded();

        if (saveLogsOnApplicationQuit)
            SaveLogs();
    }

    // Public save method used when the run ends cleanly.
    // hasSavedLogs prevents duplicate disk writes from repeated calls.
    public void SaveLogs()
    {
        if (hasSavedLogs || loggingManager == null)
            return;

        UpdateFilePrefixFromRunner();
        Debug.Log($"[PrismExperimentLogger] Saving logs to: {resolvedSavePath}");
        hasSavedLogs = true;
        loggingManager.SaveAllLogs(clear: false);
    }

    // File prefix is updated from the current input mode so controller/embodied sessions are
    // easier to tell apart directly from the filenames.
    void UpdateFilePrefixFromRunner()
    {
        if (loggingManager == null)
            return;

        string mode = GetInputModeLabel().ToLowerInvariant();
        loggingManager.SetFilePrefix($"prism_{mode}");
    }

    // Converts the runner's tracking mode into the simpler label used in analysis:
    // "controller" or "embodied".
    string GetInputModeLabel()
    {
        if (runner == null)
            return "unknown";

        return runner.CurrentOpenXRTrackingMode == SandboxRunner.OpenXRTrackingMode.Hands
            ? "embodied"
            : "controller";
    }

    // Records the start of a baseline/post block.
    //
    // The first block start also creates a "Game Started" row and starts the session timer.
    // The sawBaselineBlock / sawPostBlock flags are later used to decide whether the session
    // counts as a full finished experiment.
    public void LogBlockStarted(string taskMode, string blockType)
    {
        if (!gameStartedLogged)
        {
            gameStartedLogged = true;
            sessionStartTime = Time.time;
            LogEvent("Game Started", "GameEvent", taskMode, blockType, null);
        }

        if (blockType == "Baseline")
            sawBaselineBlock = true;
        else if (blockType == "Post")
            sawPostBlock = true;

        LogEvent("Block Started", "BlockEvent", taskMode, blockType, null);
    }

    // Exposure is treated as its own middle phase in the overall workflow.
    // We store where exposure came from so it is still possible to know which measurement task
    // surrounded it in a particular run.
    public void LogExposureStarted(string sourceTaskMode)
    {
        sawExposureBlock = true;

        var data = new Dictionary<string, object>
        {
            { "SourceTaskMode", sourceTaskMode }
        };
        LogEvent("Exposure Started", "BlockEvent", "Exposure", "Exposure", data);
    }

    // The hit radius is written to metadata once per session so RShiny can later reuse the
    // exact same value for target-circle visualisation and phase logic.
    public void LogExposureConfig(float hitRadiusMeters)
    {
        if (loggingManager == null)
            return;

        loggingManager.Log("Meta", "ExposureHitRadiusMeters", hitRadiusMeters);
        loggingManager.Log("Meta", "ExposureHitRadiusCm", hitRadiusMeters * 100f);
    }

    // Generic accepted-trial logger used by the measurement tasks.
    // The task passes in its accepted world-space response plus task-specific fields.
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

    // Logs the start of a measurement trial before the participant has responded.
    //
    // This is useful because later analysis can know:
    // - what target/stimulus the participant was supposed to aim at,
    // - which trial number it was,
    // - and the target radius used for hover-style phase analysis.
    public void LogMeasurementTrialStarted(
        string taskMode,
        string blockType,
        int trialIndex,
        int trialsPerBlock,
        Vector3 targetWorld,
        float targetRadiusMeters,
        Dictionary<string, object> extraData = null)
    {
        var data = new Dictionary<string, object>
        {
            { "TrialIndex", trialIndex },
            { "TrialsPerBlock", trialsPerBlock },
            { "TargetWorldX", targetWorld.x },
            { "TargetWorldY", targetWorld.y },
            { "TargetWorldZ", targetWorld.z },
            { "TargetRadiusMeters", targetRadiusMeters },
        };

        Merge(data, extraData);
        LogEvent("Trial Started", "TaskEvent", taskMode, blockType, data);
    }

    // Simple wrapper for block-completed events.
    public void LogBlockCompleted(string taskMode, string blockType, Dictionary<string, object> summaryData)
    {
        LogEvent("Block Completed", "BlockEvent", taskMode, blockType, summaryData);
    }

    // Writes one Summary row describing one metric for one block.
    //
    // Example:
    // - OpenLoop / Baseline / SignedOffsetCm / mean + sd
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

    // Writes one Summary row describing the change from baseline to post.
    //
    // signedDelta preserves direction,
    // magnitude removes direction,
    // normalizedMagnitude optionally rescales by baseline variability.
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

    // SummaryCollection is created lazily because not every test run necessarily reaches a point
    // where summary rows are produced.
    void EnsureSummaryCollection()
    {
        if (loggingManager == null || hasSummaryRows)
            return;

        loggingManager.CreateLog(SummaryCollection, SummaryHeaders);
    }

    // Exposure attempt logger.
    //
    // Even though this project is no longer "Whack-a-mole", several fields are intentionally
    // mirrored into mole-style names (MoleId, MolePositionWorldX, etc.) so that downstream plots
    // and modular analysis code can still reuse the same assumptions.
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

    // Separate raw pointer-shot event, distinct from the interpreted hit/miss attempt outcome.
    // This preserves the lower-level action even when the attempt is later classified as a miss.
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

    // Marks successful completion of the exposure phase.
    public void LogExposureCompleted(int successCount, int attemptCount)
    {
        var data = new Dictionary<string, object>
        {
            { "SuccessCount", successCount },
            { "AttemptIndex", attemptCount }
        };
        LogEvent("Exposure Completed", "BlockEvent", "Exposure", "Exposure", data);
    }

    // Logs when a new exposure target becomes active.
    // Again, mole-style field names are duplicated for compatibility with existing analysis code.
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

    // Called when SandboxRunner believes the experiment is over.
    //
    // Important rule:
    // a session only counts as Finished if it actually included Baseline, Exposure, and Post.
    // Otherwise it is marked Aborted even if play technically reached an ending path.
    public void LogExperimentCompleted(string taskMode, string blockType)
    {
        if (sessionStateLogged)
            return;

        if (!(sawBaselineBlock && sawExposureBlock && sawPostBlock))
        {
            loggingManager?.Log("Meta", "SessionState", "Aborted");
            loggingManager?.Log("Meta", "SessionDuration", sessionStartTime >= 0f ? Time.time - sessionStartTime : 0f);
            LogEvent("Game Finished", "GameEvent", taskMode, "Aborted", null);
            LogEvent("Experiment Aborted", "GameEvent", taskMode, "Aborted", null);
            sessionStateLogged = true;
            return;
        }

        loggingManager?.Log("Meta", "SessionState", "Finished");
        loggingManager?.Log("Meta", "SessionDuration", sessionStartTime >= 0f ? Time.time - sessionStartTime : 0f);
        LogEvent("Game Finished", "GameEvent", taskMode, blockType, null);
        LogEvent("Experiment Completed", "GameEvent", taskMode, blockType, null);
        sessionStateLogged = true;
    }

    // Safety net for sessions that end without explicit completion.
    void LogAbortedSessionIfNeeded()
    {
        if (loggingManager == null)
            return;

        if (sessionStateLogged)
            return;

        if (runner != null && runner.IsExperimentCompleted)
            return;

        loggingManager.Log("Meta", "SessionState", "Aborted");
        loggingManager.Log("Meta", "SessionDuration", sessionStartTime >= 0f ? Time.time - sessionStartTime : 0f);
        LogEvent("Game Finished", "GameEvent", runner != null ? runner.CurrentTaskMode.ToString() : "Unknown", "Aborted", null);
        LogEvent("Experiment Aborted", "GameEvent", runner != null ? runner.CurrentTaskMode.ToString() : "Unknown", "Aborted", null);
        sessionStateLogged = true;
    }

    // Core helper that assembles one Event row.
    //
    // The order of operations matters:
    // 1. start with the fields common to all events,
    // 2. add current runner state if available,
    // 3. merge the task-specific extra data,
    // 4. copy mole-style aliases if relevant,
    // 5. finally write the row.
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
            // These fields are repeated on each row so later analysis does not need to reconstruct
            // the participant's condition from far-away metadata only.
            data["EffectMode"] = runner.CurrentAppliedEffectMode.ToString();
            data["XRBackend"] = runner.CurrentXRBackend.ToString();
            data["TrackingMode"] = runner.CurrentOpenXRTrackingMode.ToString();
            data["ActiveHand"] = runner.CurrentActiveHand.ToString();
            data["ConfirmMitigationMode"] = runner.CurrentConfirmMitigationMode;
            data["ExperimentMode"] = runner.CurrentExperimentMode.ToString();
            data["StudyInputMode"] = runner.CurrentStudyInputMode.ToString();
            data["StudyParticipantIndex"] = runner.CurrentStudyParticipantIndex > 0 ? runner.CurrentStudyParticipantIndex : "";
            data["StudyGlobalParticipantId"] = runner.CurrentStudyGlobalParticipantId > 0 ? runner.CurrentStudyGlobalParticipantId : "";
            data["StudySessionIndex"] = runner.CurrentStudySessionIndex > 0 ? runner.CurrentStudySessionIndex : "";
            data["StudyTaskOrder"] = runner.CurrentStudyTaskOrderLabel;
            data["StudyResolvedTask"] = runner.CurrentStudyResolvedTaskLabel;
            data["StudyResolvedEffect"] = runner.CurrentStudyResolvedEffectLabel;
            // Add a pose snapshot at the time of this event.
            AddTrackerSnapshot(data);
        }

        Merge(data, extraData);
        // If the row already contains mole fields, duplicate them into "CurrentMoleToHit..."
        // aliases expected by some existing downstream tooling.
        CopyCurrentMoleFields(data);
        loggingManager.Log(EventCollection, data);
    }

    // Utility merge that lets task-specific fields override shared defaults if necessary.
    static void Merge(Dictionary<string, object> destination, Dictionary<string, object> source)
    {
        if (source == null)
            return;

        foreach (var pair in source)
            destination[pair.Key] = pair.Value;
    }

    // Stable per-session identifier lazily created on first use.
    string GetGameId()
    {
        if (string.IsNullOrEmpty(gameId))
            gameId = Guid.NewGuid().ToString();

        return gameId;
    }

    // Measures elapsed time between consecutive Event rows.
    // This is useful for reconstructing pacing without needing every event timestamp to be post-processed.
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

    // Adds a pose snapshot of the HMD and both hands/controllers at the moment of an event.
    // Event rows are sparse in time compared with Sample rows, so including these snapshots makes
    // important moments easier to inspect later without needing to join immediately to Sample.csv.
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

    // Compatibility helper: copy the current target/mole information into the older
    // "CurrentMoleToHit..." field names if those fields are not already populated.
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

    // Small helper used by CopyCurrentMoleFields.
    static void CopyIfPresent(Dictionary<string, object> data, string sourceKey, string targetKey)
    {
        if (data.ContainsKey(targetKey))
            return;

        if (data.TryGetValue(sourceKey, out object value))
            data[targetKey] = value;
    }

    // Adds the pose snapshot for one hand/controller.
    //
    // For controller mode this is literally the controller body + ray.
    // For embodied mode these fields are often empty for the controller pose, but the movement
    // anchor fields may still be populated if a tracked hand anchor is available.
    void AddControllerSnapshot(Dictionary<string, object> data, SandboxRunner.Handedness hand, string prefix)
    {
        if (runner == null)
            return;

        bool hasPose = runner.TryGetControllerPose(hand, out Pose controllerPose, out Pose rayPose);
        bool hasMovementAnchor = runner.TryGetMovementAnchorPose(hand, out Pose movementAnchorPose, out string movementAnchorSource);
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
        data[$"{prefix.Replace("Controller", "MovementAnchor")}PosWorldX"] = hasMovementAnchor ? movementAnchorPose.position.x : "";
        data[$"{prefix.Replace("Controller", "MovementAnchor")}PosWorldY"] = hasMovementAnchor ? movementAnchorPose.position.y : "";
        data[$"{prefix.Replace("Controller", "MovementAnchor")}PosWorldZ"] = hasMovementAnchor ? movementAnchorPose.position.z : "";
        data[$"{prefix.Replace("Controller", "MovementAnchor")}RotEulerX"] = hasMovementAnchor ? movementAnchorPose.rotation.eulerAngles.x : "";
        data[$"{prefix.Replace("Controller", "MovementAnchor")}RotEulerY"] = hasMovementAnchor ? movementAnchorPose.rotation.eulerAngles.y : "";
        data[$"{prefix.Replace("Controller", "MovementAnchor")}RotEulerZ"] = hasMovementAnchor ? movementAnchorPose.rotation.eulerAngles.z : "";
        data[$"{prefix.Replace("Controller", "MovementAnchor")}Source"] = hasMovementAnchor ? movementAnchorSource : "";
    }

    // Creates the common part of a Summary row.
    // The caller then fills in metric values such as baseline/post or signed delta.
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
            data["ConfirmMitigationMode"] = runner.CurrentConfirmMitigationMode;
            data["ExperimentMode"] = runner.CurrentExperimentMode.ToString();
            data["StudyInputMode"] = runner.CurrentStudyInputMode.ToString();
            data["StudyParticipantIndex"] = runner.CurrentStudyParticipantIndex > 0 ? runner.CurrentStudyParticipantIndex : "";
            data["StudyGlobalParticipantId"] = runner.CurrentStudyGlobalParticipantId > 0 ? runner.CurrentStudyGlobalParticipantId : "";
            data["StudySessionIndex"] = runner.CurrentStudySessionIndex > 0 ? runner.CurrentStudySessionIndex : "";
            data["StudyTaskOrder"] = runner.CurrentStudyTaskOrderLabel;
            data["StudyResolvedTask"] = runner.CurrentStudyResolvedTaskLabel;
            data["StudyResolvedEffect"] = runner.CurrentStudyResolvedEffectLabel;
        }

        return data;
    }
}
