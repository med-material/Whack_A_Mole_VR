using System.Collections.Generic;
using UnityEngine;
using TMPro;

// LineBisectionTask implements the measurement task where the participant indicates
// the perceived midpoint of a horizontal line shown on the board.
//
// Compared with OpenLoopPointingTask, the motor structure is similar:
// the participant still raises the arm, aims, and confirms a response.
// The difference is perceptual rather than motor:
// instead of pointing toward a single explicit target point, the participant must infer
// where the midpoint of the visible line lies.
//
// The main dependent measure is the signed midpoint error in centimeters.
// In other words:
// "how far left or right from the true midpoint did the participant respond?"
//
// As with open-loop pointing, the important experimental distinction is that the task
// is intended as a measurement task rather than an online correction task. A cursor can
// be shown in the implementation for debugging, but the conceptual measurement is still
// based on the final accepted estimate of the midpoint.
public class LineBisectionTask : MonoBehaviour, ISandboxTask, IAimTargetProvider
{
    public enum BlockType { Baseline, Post }

    [Header("References")]
    private SandboxRunner runner;
    private Transform boardPlane;
    private Transform hmd;
    private LineRenderer lineRenderer;
    private Transform cursorMarker;
    private Transform midpointMarker;
    private TextMeshProUGUI readout;
    private PrismExperimentLogger experimentLogger;

    [Header("Block")]
    private BlockType blockType = BlockType.Baseline;
    private int trialsPerBlock = 20;
    private bool latchConfirm = true;

    [Header("Trial gating (return to start posture)")]
    public bool requireResetBetweenTrials = false;
    private float resetDropMeters = 0.6f;
    private float minSecondsBetweenTrials = 0f;

    [Header("Line settings (in metres)")]
    [SerializeField] private float lineLength = 1f;
    [SerializeField] private float lineZRange = 0.1f;
    [SerializeField] private bool clampToLineSegment = true;
    [SerializeField] private float analysisMidpointRadiusMeters = 0.03f;

    [Header("Randomisation")]
    [SerializeField] private bool randomiseLineZEachTrial = true;
    [SerializeField, Tooltip("If enabled, line length changes between trials. If disabled, the same lineLength is used for the whole block.")]
    private bool randomiseLineLengthEachTrial = true;
    [SerializeField] private float minLineLength = 1f;
    [SerializeField] private float maxLineLength = 1.25f;

    [Header("Debug")]
    public bool showCursor = true;
    [SerializeField] private bool showBoardMidpoint = true;

    int _trialIndex = 0;
    bool _confirmLatched = false;
    bool _armed = true;
    float _lastAcceptedTime = -999f;
    bool _currentTrialLogged = false;
    float? _liveErrorCm = null;

    float _currentLineZ = 0f;
    float _currentHalfLen = 0.2f;

    readonly List<float> _errorsCm = new List<float>(128);
    float? _baselineMeanCm = null;
    float? _postMeanCm = null;
    float? _baselineSdCm = null;
    float? _postSdCm = null;

    bool _isActive;

    public SandboxRunner.TaskMode TaskMode => SandboxRunner.TaskMode.LineBisection;
    public string GetCurrentBlockName() => blockType.ToString();
    public int GetCurrentTrialNumber() => Mathf.Clamp(_trialIndex + 1, 1, trialsPerBlock);

    // Standard startup hook. Auto-fills references so the script can work with minimal
    // manual inspector setup.
    void Awake()
    {
        AutoAssignReferences();
    }

    // Editor reset hook.
    void Reset()
    {
        AutoAssignReferences();
    }

    // Keep references refreshed in edit mode when values change.
    void OnValidate()
    {
        if (!Application.isPlaying)
            AutoAssignReferences();
    }

    // Called by SandboxRunner when the task should be shown or hidden.
    // If the task has just become active, it also prepares the current trial line.
    public void SetTaskActive(bool active)
    {
        _isActive = active;
        UpdateVisuals();

        if (_isActive && lineRenderer != null && boardPlane != null)
            SetupNewLine();
    }

    // Tries to find the scene objects this task depends on:
    // runner, board, HMD, line renderer, cursor/midpoint markers, readout, and logger.
    void AutoAssignReferences()
    {
        if (runner == null)
            runner = FindFirstObjectByType<SandboxRunner>();

        if (boardPlane == null)
            boardPlane = GameObject.Find("Board")?.transform;

        if (hmd == null)
        {
            var camObj = GameObject.Find("Camera") ?? GameObject.Find("Camera (eye)") ?? GameObject.Find("Main Camera");
            if (camObj != null) hmd = camObj.transform;
            else if (Camera.main != null) hmd = Camera.main.transform;
        }

        if (lineRenderer == null)
            lineRenderer = GameObject.Find("Bisectionline")?.GetComponent<LineRenderer>();

        if (cursorMarker == null)
            cursorMarker = GameObject.Find("Hitmarker")?.transform ?? GameObject.Find("HitMarker")?.transform;

        if (midpointMarker == null)
            midpointMarker = GameObject.Find("MidPointMarker")?.transform ?? GameObject.Find("MidpointMarker")?.transform;

        if (readout == null)
            readout = runner != null ? runner.GetStatReadout() : GameObject.Find("StatText")?.GetComponent<TextMeshProUGUI>();

        if (experimentLogger == null)
            experimentLogger = runner != null ? runner.GetExperimentLogger() : FindFirstObjectByType<PrismExperimentLogger>();
    }

    // Shows or hides the task-specific visuals.
    // lineRenderer is part of the actual stimulus.
    // midpointMarker is a board reference.
    // cursorMarker is mostly useful as a debug/development aid.
    void UpdateVisuals()
    {
        if (lineRenderer)
            lineRenderer.gameObject.SetActive(_isActive);

        if (cursorMarker)
            cursorMarker.gameObject.SetActive(_isActive && showCursor);

        if (midpointMarker)
            midpointMarker.gameObject.SetActive(_isActive && showBoardMidpoint);
    }

    // Main task loop.
    //
    // Chronologically, one trial works like this:
    // 1. make sure the task is active and its references exist,
    // 2. keep the line/midpoint visuals in sync,
    // 3. stop early if the block is already complete,
    // 4. ensure the current trial has been logged as started,
    // 5. get the participant's transformed input from SandboxRunner,
    // 6. apply latching and optional reset gating,
    // 7. intersect the aim ray with the board,
    // 8. project that hit onto the current line,
    // 9. convert the result into signed midpoint error,
    // 10. if confirmed, accept/log the trial and possibly finish the block.
    void Update()
    {
        // If this task is not currently active, do nothing.
        if (!_isActive) return;
        // The task cannot run without the runner, board plane, and line renderer.
        if (runner == null || boardPlane == null || lineRenderer == null) return;

        UpdateVisuals();

        // The midpoint marker stays at the board center.
        // This is a spatial reference, not online correctness feedback.
        if (midpointMarker && showBoardMidpoint)
            midpointMarker.position = boardPlane.position;

        // Once the block is complete, only show the final summary.
        if (_trialIndex >= trialsPerBlock)
        {
            UpdateReadout(final: true);
            return;
        }

        // Make sure the logger knows the current trial exists before it is accepted.
        EnsureCurrentTrialLogged();

        // SandboxRunner already resolves modality/effect differences,
        // so the task just consumes the unified input package.
        var (ray, pose, confirm) = runner.GetTransformedInput();

        if (latchConfirm)
        {
            // Prevent a single long press from being counted as multiple responses.
            if (!confirm) _confirmLatched = false;
            if (confirm && _confirmLatched) confirm = false;
        }

        if (requireResetBetweenTrials && hmd != null)
        {
            // Optional return-to-start gating, identical in spirit to the one used in open-loop pointing.
            float resetY = hmd.position.y - resetDropMeters;

            if (!_armed && pose.position.y <= resetY)
                _armed = true;

            if (confirm && !_armed)
                confirm = false;
        }

        // If the ray does not hit the board plane, there is no valid current estimate.
        if (!IntersectRayWithBoard(ray, out Vector3 hit))
        {
            _liveErrorCm = null;
            UpdateReadout(final: false);
            return;
        }

        // Convert the world hit into board-local coordinates.
        Vector3 local = WorldToBoardMeters(hit);
        // Force the point onto the current line's depth, since the participant is estimating
        // the midpoint of the shown line rather than an arbitrary depth on the board.
        local.z = _currentLineZ;
        // Live signed error before segment clamping, used mainly for debug/readout.
        _liveErrorCm = local.x * 100f;

        if (clampToLineSegment)
            // Restrict the accepted point to the visible length of the displayed line.
            local.x = Mathf.Clamp(local.x, -_currentHalfLen, _currentHalfLen);

        Vector3 constrainedWorld = BoardMetersToWorld(local);

        if (cursorMarker && showCursor)
            // If shown, the cursor visual indicates the currently aimed location on the line.
            // This is helpful during development, but it should not be mistaken for the
            // conceptual basis of the measurement design.
            cursorMarker.position = constrainedWorld;

        if (confirm)
        {
            if (requireResetBetweenTrials)
            {
                // Prevent immediate double-acceptance on near-simultaneous inputs.
                if (Time.time - _lastAcceptedTime < minSecondsBetweenTrials)
                {
                    UpdateReadout(final: false);
                    return;
                }
                _lastAcceptedTime = Time.time;
                _armed = false;
            }

            if (latchConfirm) _confirmLatched = true;

            // The signed error is the horizontal board coordinate relative to the true midpoint.
            float errorMeters = local.x;
            float errorCm = errorMeters * 100f;

            // Store the accepted result and advance the trial counter.
            _errorsCm.Add(errorCm);
            _trialIndex++;
            _currentTrialLogged = false;

            // Log both the accepted point and the stimulus configuration for this trial.
            experimentLogger?.LogMeasurementTrial(
                TaskMode.ToString(),
                blockType.ToString(),
                _trialIndex,
                trialsPerBlock,
                constrainedWorld,
                new Dictionary<string, object>
                {
                    { "ErrorCm", errorCm },
                    { "LineZMeters", _currentLineZ },
                    { "LineLengthMeters", _currentHalfLen * 2f }
                });

            Debug.Log($"[LineBisection] block={blockType} trial={_trialIndex}/{trialsPerBlock} error={errorCm:0.0} cm (lineZ={_currentLineZ:0.000}m len={_currentHalfLen * 2f:0.000}m)");

            if (_trialIndex >= trialsPerBlock)
            {
                // End-of-block summary statistics.
                var (mean, sd) = MeanAndSd(_errorsCm);
                if (blockType == BlockType.Baseline)
                {
                    _baselineMeanCm = mean;
                    _baselineSdCm = sd;
                }
                if (blockType == BlockType.Post)
                {
                    _postMeanCm = mean;
                    _postSdCm = sd;
                }

                Debug.Log($"[LineBisection] block={blockType} COMPLETE mean={mean:0.0} cm sd={sd:0.0} cm n={_errorsCm.Count}");

                experimentLogger?.LogTaskMetricSummary(
                    TaskMode.ToString(),
                    blockType.ToString(),
                    // BisectionError is the analysis-facing name used for this task's main measure.
                    "BisectionError",
                    "cm",
                    _errorsCm.Count,
                    mean,
                    sd,
                    "Mean signed line bisection error");

                if (_baselineMeanCm.HasValue && _postMeanCm.HasValue)
                {
                    // Aftereffect is defined directionally as Post - Baseline.
                    float afterEffect = _postMeanCm.Value - _baselineMeanCm.Value;
                    Debug.Log($"[LineBisection] AFTER-EFFECT (Post - Baseline) = {afterEffect:0.0} cm");

                    experimentLogger?.LogBlockCompleted(
                        TaskMode.ToString(),
                        blockType.ToString(),
                        new Dictionary<string, object>
                        {
                            { "MeanCm", mean },
                            { "SdCm", sd },
                            { "AfterEffectCm", afterEffect },
                            { "TrialsPerBlock", _errorsCm.Count }
                        });

                    experimentLogger?.LogTaskAftereffectSummary(
                        TaskMode.ToString(),
                        "BisectionError",
                        "cm",
                        _errorsCm.Count,
                        _baselineMeanCm.Value,
                        _baselineSdCm,
                        _postMeanCm.Value,
                        _postSdCm,
                        afterEffect,
                        "Post minus baseline bisection error");
                }
                else
                {
                    experimentLogger?.LogBlockCompleted(
                        TaskMode.ToString(),
                        blockType.ToString(),
                        new Dictionary<string, object>
                        {
                            { "MeanCm", mean },
                            { "SdCm", sd },
                            { "TrialsPerBlock", _errorsCm.Count }
                        });
                }

                UpdateReadout(final: true);
                runner?.NotifyMeasurementBlockCompleted(TaskMode, blockType.ToString());
                return;
            }

            // Prepare the next stimulus line for the next trial.
            SetupNewLine();
            EnsureCurrentTrialLogged();
        }

        UpdateReadout(final: false);
    }

    // Starts a fresh baseline or post block.
    // Resets all counters, accepted errors, and gating state.
    public void StartNewBlock(BlockType newBlock)
    {
        blockType = newBlock;
        _trialIndex = 0;
        _confirmLatched = false;
        _errorsCm.Clear();
        _currentTrialLogged = false;

        _armed = true;
        _lastAcceptedTime = -999f;

        if (_isActive)
            SetupNewLine();

        EnsureCurrentTrialLogged();
        UpdateReadout(final: false);
    }

    // Clears stored block summaries so a new experiment run does not inherit old aftereffect values.
    public void ClearSummaries()
    {
        _baselineMeanCm = null;
        _postMeanCm = null;
        _baselineSdCm = null;
        _postSdCm = null;
    }

    // Chooses the visual line used for the current trial.
    //
    // Two stimulus properties can vary:
    // - its total length,
    // - its depth position on the board.
    //
    // The resulting world-space endpoints are then written into the LineRenderer.
    void SetupNewLine()
    {
        float len = lineLength;
        if (randomiseLineLengthEachTrial)
            len = Random.Range(minLineLength, maxLineLength);

        _currentHalfLen = Mathf.Max(0.01f, len * 0.5f);

        if (randomiseLineZEachTrial)
            _currentLineZ = Random.Range(-lineZRange, lineZRange);
        else
            _currentLineZ = 0f;

        Vector3 aLocal = new Vector3(-_currentHalfLen, 0f, _currentLineZ);
        Vector3 bLocal = new Vector3(+_currentHalfLen, 0f, _currentLineZ);

        Vector3 aWorld = BoardMetersToWorld(aLocal);
        Vector3 bWorld = BoardMetersToWorld(bLocal);

        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.SetPosition(0, aWorld);
        lineRenderer.SetPosition(1, bWorld);
    }

    // Ensures the logger has a "trial started" entry for the current line stimulus.
    // The midpointWorld value records the true midpoint the participant is meant to estimate.
    void EnsureCurrentTrialLogged()
    {
        if (_currentTrialLogged || !_isActive || boardPlane == null || experimentLogger == null || _trialIndex >= trialsPerBlock)
            return;

        Vector3 midpointWorld = BoardMetersToWorld(new Vector3(0f, 0f, _currentLineZ));

        experimentLogger.LogMeasurementTrialStarted(
            TaskMode.ToString(),
            blockType.ToString(),
            _trialIndex + 1,
            trialsPerBlock,
            midpointWorld,
            analysisMidpointRadiusMeters,
            new Dictionary<string, object>
            {
                { "LineZMeters", _currentLineZ },
                { "LineLengthMeters", _currentHalfLen * 2f }
            });

        _currentTrialLogged = true;
    }

    // Finds where the aim ray intersects the board plane.
    bool IntersectRayWithBoard(Ray r, out Vector3 hit)
    {
        Vector3 n = boardPlane.up;
        // If the ray is nearly parallel to the board, there is no stable intersection.
        float denom = Vector3.Dot(n, r.direction);
        if (Mathf.Abs(denom) < 1e-4f)
        {
            hit = default;
            return false;
        }

        var plane = new Plane(n, boardPlane.position);
        if (plane.Raycast(r, out float enter) && enter > 0f && enter < 10f)
        {
            hit = r.GetPoint(enter);
            return true;
        }

        hit = default;
        return false;
    }

    // Updates the on-screen readout for the task.
    //
    // Similar structure to open-loop pointing:
    // - before any accepted trials: mainly live estimate and gate state,
    // - during block: live estimate + last result + running mean/SD,
    // - after block: final summary and aftereffect if available.
    void UpdateReadout(bool final)
    {
        if (!readout || !_isActive) return;

        string gateTxt = (requireResetBetweenTrials && hmd != null) ? (_armed ? "ARMED" : "RESET") : "—";

        if (_errorsCm.Count == 0)
        {
            readout.text =
                $"Line Bisection ({blockType})\n" +
                $"Trial {_trialIndex}/{trialsPerBlock}\n" +
                $"Aim: {(_liveErrorCm.HasValue ? $"{_liveErrorCm.Value:0.0} cm" : "—")}\n" +
                $"Gate: {gateTxt}";
            return;
        }

        var (mean, sd) = MeanAndSd(_errorsCm);

        if (!final)
        {
            float last = _errorsCm[_errorsCm.Count - 1];
            readout.text =
                $"Line Bisection ({blockType})\n" +
                $"Trial {_trialIndex}/{trialsPerBlock}\n" +
                $"Aim: {(_liveErrorCm.HasValue ? $"{_liveErrorCm.Value:0.0} cm" : "—")}\n" +
                $"Last: {last:0.0} cm\n" +
                $"Mean: {mean:0.0} cm (SD {sd:0.0})\n" +
                $"Gate: {gateTxt}";
            return;
        }

        string summary =
            $"Line Bisection ({blockType}) COMPLETE\n" +
            $"n={_errorsCm.Count}\n" +
            $"Mean: {mean:0.0} cm (SD {sd:0.0})";

        if (_baselineMeanCm.HasValue && _postMeanCm.HasValue)
        {
            float afterEffect = _postMeanCm.Value - _baselineMeanCm.Value;
            summary += $"\nAfter-effect: {afterEffect:0.0} cm";
        }

        readout.text = summary;
    }

    // Computes mean and sample standard deviation for the accepted signed errors.
    static (float mean, float sd) MeanAndSd(List<float> xs)
    {
        int n = xs.Count;
        if (n <= 0) return (0f, 0f);

        double sum = 0.0;
        for (int i = 0; i < n; i++) sum += xs[i];
        double mean = sum / n;

        if (n == 1) return ((float)mean, 0f);

        double var = 0.0;
        for (int i = 0; i < n; i++)
        {
            double d = xs[i] - mean;
            var += d * d;
        }
        var /= (n - 1);
        double sd = System.Math.Sqrt(var);

        return ((float)mean, (float)sd);
    }

    // Exposes the current target midpoint to SandboxRunner for controller hover-confirm.
    // In line bisection, that target is the true midpoint of the currently displayed line.
    public bool TryGetAimTarget(out Vector3 worldCenter, out Vector3 planeNormal, out float targetRadiusMeters)
    {
        worldCenter = Vector3.zero;
        planeNormal = Vector3.up;
        targetRadiusMeters = analysisMidpointRadiusMeters;

        if (!_isActive || boardPlane == null || _trialIndex >= trialsPerBlock)
            return false;

        worldCenter = BoardMetersToWorld(new Vector3(0f, 0f, _currentLineZ));
        planeNormal = boardPlane.up;
        return true;
    }

    // Converts board-local coordinates in meters to world-space positions.
    // This uses the board's orientation in world space rather than relying on raw local scale.
    Vector3 BoardMetersToWorld(Vector3 localMeters)
    {
        Vector3 right = Vector3.ProjectOnPlane(boardPlane.right, boardPlane.up).normalized;
        Vector3 forward = Vector3.ProjectOnPlane(boardPlane.forward, boardPlane.up).normalized;
        return boardPlane.position + (right * localMeters.x) + (forward * localMeters.z);
    }

    // Inverse of BoardMetersToWorld(): converts a world-space point into board-local meters.
    Vector3 WorldToBoardMeters(Vector3 worldPoint)
    {
        Vector3 relative = worldPoint - boardPlane.position;
        Vector3 right = Vector3.ProjectOnPlane(boardPlane.right, boardPlane.up).normalized;
        Vector3 forward = Vector3.ProjectOnPlane(boardPlane.forward, boardPlane.up).normalized;
        return new Vector3(
            Vector3.Dot(relative, right),
            0f,
            Vector3.Dot(relative, forward));
    }
}
