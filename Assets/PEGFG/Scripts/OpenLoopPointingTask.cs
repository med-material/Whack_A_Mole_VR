using System.Collections.Generic;
using UnityEngine;
using TMPro;

// OpenLoopPointingTask implements the baseline/post measurement task where the participant
// points toward a single target location and confirms the response.
//
// Conceptually, this task measures horizontal endpoint error:
// "how far left or right of the intended target did the participant indicate?"
//
// It is called "open-loop" because the important data point is the final chosen endpoint,
// not the full continuous trajectory after commitment.
//
// An important experimental distinction:
// the intended task does NOT rely on continuous online correction feedback.
// A live aim marker exists in the implementation, but that is a debugging/development aid
// controlled by showLiveAimMarker, not the conceptual basis of the measurement task.
// The intended outcome variable is still the participant's final perceived target location.
//
// This script is responsible for:
// - managing baseline and post blocks,
// - reading transformed input from SandboxRunner,
// - intersecting the aim ray with the board,
// - converting that hit into a signed horizontal error,
// - enforcing optional reset-between-trials gating,
// - updating the on-screen readout,
// - logging trial-level and block-level summaries.
public class OpenLoopPointingTask : MonoBehaviour, ISandboxTask, IAimTargetProvider
{
    public enum BlockType { Baseline, Post }

    [Header("References")]
    private SandboxRunner runner;
    private Transform boardPlane;
    private TextMeshProUGUI readout;
    private Transform hitMarker;
    private Transform midpointMarker;
    private Transform hmd;
    private PrismExperimentLogger experimentLogger;

    [Header("OLP Constraints")]
    private bool lockToHorizontalMidline = true;

    [Header("Settings")]
    private float boardHalfWidth = 1f;
    private float boardHalfHeight = 1f;
    private bool clampToBoard = true;
    [SerializeField] private float analysisTargetRadiusMeters = 0.03f;

    [Header("Trial gating (return to start posture)")]
    public bool requireResetBetweenTrials = false;
    private float resetDropMeters = 0.6f;
    private float minSecondsBetweenTrials = 0.15f;

    [Header("Block")]
    private BlockType blockType = BlockType.Baseline;
    private int trialsPerBlock = 30;
    private bool latchConfirm = true;

    [Header("Debug")]
    public bool showLiveAimMarker = true;
    private bool showBoardMidpoint = true;

    int _trialIndex = 0;
    bool _confirmLatched = false;
    bool _armed = true;
    float _lastAcceptedTime = -999f;
    bool _currentTrialLogged = false;
    float? _liveSignedCm = null;

    readonly List<float> _offsetsCm = new List<float>(128);
    float? _baselineMeanCm = null;
    float? _postMeanCm = null;
    float? _baselineSdCm = null;
    float? _postSdCm = null;

    bool _isActive;

    public SandboxRunner.TaskMode TaskMode => SandboxRunner.TaskMode.OpenLoop;
    public string GetCurrentBlockName() => blockType.ToString();
    public int GetCurrentTrialNumber() => Mathf.Clamp(_trialIndex + 1, 1, trialsPerBlock);

    // Standard startup hook. The task tries to auto-find its scene references
    // so it can work with minimal manual inspector wiring.
    void Awake()
    {
        AutoAssignReferences();
    }

    // Editor reset hook.
    void Reset()
    {
        AutoAssignReferences();
    }

    // In the editor, keep references refreshed if the component is changed.
    void OnValidate()
    {
        if (!Application.isPlaying)
            AutoAssignReferences();
    }

    // Called by SandboxRunner when this task becomes the active task or is turned off.
    // The task keeps its own _isActive flag rather than relying on the component being disabled,
    // because SandboxRunner switches tasks through a common interface.
    public void SetTaskActive(bool active)
    {
        _isActive = active;
        UpdateVisuals();
    }

    // Tries to find the objects this task needs in the scene:
    // - the central runner,
    // - the board plane,
    // - the HMD,
    // - the readout text,
    // - the logging component,
    // - live visual markers.
    //
    // This makes the task more robust to scene reloads and reduces manual assignment work.
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

        if (readout == null)
            readout = runner != null ? runner.GetStatReadout() : GameObject.Find("StatText")?.GetComponent<TextMeshProUGUI>();

        if (experimentLogger == null)
            experimentLogger = runner != null ? runner.GetExperimentLogger() : FindFirstObjectByType<PrismExperimentLogger>();

        if (hitMarker == null)
            hitMarker = GameObject.Find("Hitmarker")?.transform ?? GameObject.Find("HitMarker")?.transform;

        if (midpointMarker == null)
            midpointMarker = GameObject.Find("MidPointMarker")?.transform ?? GameObject.Find("MidpointMarker")?.transform;
    }

    // Shows or hides the visual aids that belong specifically to this task.
    //
    // Important distinction:
    // midpointMarker is part of the task's reference setup,
    // whereas hitMarker is mainly a development/debugging aid when live aim visualization is enabled.
    void UpdateVisuals()
    {
        if (hitMarker)
            hitMarker.gameObject.SetActive(_isActive && showLiveAimMarker);

        if (midpointMarker)
            midpointMarker.gameObject.SetActive(_isActive && showBoardMidpoint);
    }

    // Main task loop.
    //
    // This method is where the actual open-loop trial logic lives.
    // A useful way to read it is as the chronological flow of one trial:
    //
    // 1. make sure the task is active and references are valid,
    // 2. update the visual markers,
    // 3. stop early if the block is already complete,
    // 4. make sure the current trial start is logged,
    // 5. fetch the participant's transformed input from SandboxRunner,
    // 6. apply confirmation-gating rules,
    // 7. intersect the pointing ray with the board,
    // 8. convert that hit into the signed error metric,
    // 9. if the user confirms, accept and log the trial,
    // 10. if the block is complete, compute block summaries and notify SandboxRunner.
    void Update()
    {
        // If this task is not the currently active one, do nothing.
        if (!_isActive) return;
        // The task cannot function without the central runner or the board plane.
        if (runner == null || boardPlane == null) return;

        UpdateVisuals();

        // The midpoint marker, if enabled, is simply kept at the board center.
        // This is a reference location, not trial-by-trial performance feedback.
        if (midpointMarker && showBoardMidpoint)
            midpointMarker.position = boardPlane.position;

        // Once the block is complete, the task stops accepting input and just shows the final summary.
        if (_trialIndex >= trialsPerBlock)
        {
            UpdateReadout(final: true);
            return;
        }

        // Make sure the logger knows that the current trial exists before the response is accepted.
        EnsureCurrentTrialLogged();

        // SandboxRunner already hides whether the user is using a controller or hands,
        // and whether an effect is active. The task just receives one unified answer here.
        var (ray, pose, confirm) = runner.GetTransformedInput();

        if (latchConfirm)
        {
            // "Latch confirm" means one long button hold should count as one response, not many.
            // If the button is released, the latch resets and the next press can be accepted.
            if (!confirm) _confirmLatched = false;
            if (confirm && _confirmLatched) confirm = false;
        }

        if (requireResetBetweenTrials && hmd != null)
        {
            // Reset gating forces the participant to lower the response pose before the next trial.
            // This creates a more repeatable "arms down -> raise -> point" structure.
            float resetY = hmd.position.y - resetDropMeters;

            // Once the tracked pose drops far enough, the task becomes armed again.
            if (!_armed && pose.position.y <= resetY)
                _armed = true;

            // If the participant has not yet returned to the reset posture,
            // a confirm input is ignored.
            if (confirm && !_armed)
                confirm = false;
        }

        // Project the participant's aim ray onto the board.
        // If the ray does not hit the board plane, we cannot compute a valid response.
        if (!IntersectRayWithBoard(ray, out Vector3 hit))
        {
            _liveSignedCm = null;
            UpdateReadout(final: false);
            return;
        }

        // ClampToBoard prevents responses from drifting beyond the measurement area.
        if (clampToBoard)
            hit = ClampToBoard(hit);

        // Open-loop pointing only cares about horizontal error,
        // so the hit is forced onto the horizontal midline of the board.
        if (lockToHorizontalMidline)
            hit = LockToMidline(hit);

        // Convert the current aim point into centimeters for on-screen readout/debug state.
        // The core analysis still depends on the accepted final endpoint, not continuous correction.
        _liveSignedCm = SignedOffsetOnBoard(hit) * 100f;

        if (showLiveAimMarker && hitMarker)
            // If enabled, this marker shows the current aimed location.
            // This is useful during development, but it should not be mistaken for
            // the intended "no online correction feedback" experimental design.
            hitMarker.position = hit;

        if (confirm)
        {
            if (requireResetBetweenTrials)
            {
                // This short minimum delay prevents double-acceptance when the user confirms
                // at nearly the same moment twice.
                if (Time.time - _lastAcceptedTime < minSecondsBetweenTrials)
                {
                    UpdateReadout(final: false);
                    return;
                }

                _lastAcceptedTime = Time.time;
                // After accepting a response, the task becomes disarmed until the reset posture is reached again.
                _armed = false;
            }

            if (latchConfirm) _confirmLatched = true;

            // If the live marker is hidden, the marker can still be placed at the accepted hit.
            // In practice this is mainly useful for debugging and verification rather than
            // as a defining part of the open-loop task logic.
            if (!showLiveAimMarker && hitMarker)
                hitMarker.position = hit;

            // This is the core outcome variable of the task:
            // signed horizontal distance from board center.
            // Positive and negative values indicate direction as well as size.
            float signedMeters = SignedOffsetOnBoard(hit);
            float signedCm = signedMeters * 100f;

            // Store the accepted result in memory for this block.
            _offsetsCm.Add(signedCm);
            // Move to the next trial.
            _trialIndex++;
            // The next trial has not yet been logged as started.
            _currentTrialLogged = false;

            // Log the accepted measurement trial, including its final hit position and signed offset.
            experimentLogger?.LogMeasurementTrial(
                TaskMode.ToString(),
                blockType.ToString(),
                _trialIndex,
                trialsPerBlock,
                hit,
                new Dictionary<string, object>
                {
                    { "SignedOffsetCm", signedCm }
                });

            Debug.Log($"[OLP] block={blockType} trial={_trialIndex}/{trialsPerBlock} offset={signedCm:0.0} cm hit={hit}");

            if (_trialIndex >= trialsPerBlock)
            {
                // Once the block finishes, compute the summary statistics for the whole block.
                var (mean, sd) = MeanAndSd(_offsetsCm);

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

                Debug.Log($"[OLP] block={blockType} COMPLETE mean={mean:0.0} cm sd={sd:0.0} cm n={_offsetsCm.Count}");

                experimentLogger?.LogTaskMetricSummary(
                    TaskMode.ToString(),
                    blockType.ToString(),
                    // "SignedEndpointError" is the analysis-facing label for the main dependent measure.
                    "SignedEndpointError",
                    "cm",
                    _offsetsCm.Count,
                    mean,
                    sd,
                    "Mean signed horizontal endpoint error");

                if (_baselineMeanCm.HasValue && _postMeanCm.HasValue)
                {
                    // Aftereffect is defined here as Post - Baseline.
                    // This preserves direction, not just magnitude.
                    float afterEffect = _postMeanCm.Value - _baselineMeanCm.Value;
                    Debug.Log($"[OLP] AFTER-EFFECT (Post - Baseline) = {afterEffect:0.0} cm");

                    experimentLogger?.LogBlockCompleted(
                        TaskMode.ToString(),
                        blockType.ToString(),
                        new Dictionary<string, object>
                        {
                            { "MeanCm", mean },
                            { "SdCm", sd },
                            { "AfterEffectCm", afterEffect },
                            { "TrialsPerBlock", _offsetsCm.Count }
                        });

                    experimentLogger?.LogTaskAftereffectSummary(
                        TaskMode.ToString(),
                        "SignedEndpointError",
                        "cm",
                        _offsetsCm.Count,
                        _baselineMeanCm.Value,
                        _baselineSdCm,
                        _postMeanCm.Value,
                        _postSdCm,
                        afterEffect,
                        "Post minus baseline horizontal endpoint error");
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
                            { "TrialsPerBlock", _offsetsCm.Count }
                        });
                }
            }

            // Immediately log the start of the next trial if there is one.
            EnsureCurrentTrialLogged();
            UpdateReadout(final: _trialIndex >= trialsPerBlock);
            // Tell SandboxRunner that this block is done so the global experiment flow can continue.
            if (_trialIndex >= trialsPerBlock)
                runner?.NotifyMeasurementBlockCompleted(TaskMode, blockType.ToString());
        }
        else
        {
            UpdateReadout(final: false);
        }
    }

    // Starts a fresh baseline or post block.
    // This resets trial counters, accepted measurements, gating state, and readout state.
    public void StartNewBlock(BlockType newBlock)
    {
        blockType = newBlock;
        _trialIndex = 0;
        _confirmLatched = false;
        _offsetsCm.Clear();
        _currentTrialLogged = false;

        _armed = true;
        _lastAcceptedTime = -999f;

        EnsureCurrentTrialLogged();
        UpdateReadout(final: false);
    }

    // Clears stored baseline/post summaries so a completely new experiment run
    // does not accidentally reuse old aftereffect values.
    public void ClearSummaries()
    {
        _baselineMeanCm = null;
        _postMeanCm = null;
        _baselineSdCm = null;
        _postSdCm = null;
    }

    // Updates the task readout text shown in the scene UI.
    //
    // There are three display situations:
    // 1. no accepted trials yet -> mainly show live aim and gate state,
    // 2. block in progress -> show live aim, last trial, running mean and SD,
    // 3. block complete -> show final summary and, if available, aftereffect.
    void UpdateReadout(bool final)
    {
        if (!readout || !_isActive) return;

        if (_offsetsCm.Count == 0)
        {
            string armedTxt = (requireResetBetweenTrials && hmd != null) ? (_armed ? "ARMED" : "RESET") : "—";
            string liveTxt = _liveSignedCm.HasValue ? $"{_liveSignedCm.Value:0.0} cm" : "—";
            readout.text = $"OLP ({blockType})\nTrial {_trialIndex}/{trialsPerBlock}\nAim: {liveTxt}\nGate: {armedTxt}";
            return;
        }

        var (mean, sd) = MeanAndSd(_offsetsCm);

        if (!final)
        {
            // During the block, the readout combines immediate feedback ("Aim" and "Last")
            // with running descriptive statistics ("Mean" and "SD").
            float last = _offsetsCm[_offsetsCm.Count - 1];
            string armedTxt = (requireResetBetweenTrials && hmd != null) ? (_armed ? "ARMED" : "RESET") : "—";
            string liveTxt = _liveSignedCm.HasValue ? $"{_liveSignedCm.Value:0.0} cm" : "—";

            readout.text =
                $"OLP ({blockType})\n" +
                $"Trial {_trialIndex}/{trialsPerBlock}\n" +
                $"Aim: {liveTxt}\n" +
                $"Last: {last:0.0} cm\n" +
                $"Mean: {mean:0.0} cm (SD {sd:0.0})\n" +
                $"Gate: {armedTxt}";
            return;
        }

        string summary =
            $"OLP ({blockType}) COMPLETE\n" +
            $"n={_offsetsCm.Count}\n" +
            $"Mean: {mean:0.0} cm (SD {sd:0.0})";

        if (_baselineMeanCm.HasValue && _postMeanCm.HasValue)
        {
            // Only after post is complete do we have enough information to show an aftereffect.
            float afterEffect = _postMeanCm.Value - _baselineMeanCm.Value;
            summary += $"\nAfter-effect: {afterEffect:0.0} cm";
        }

        readout.text = summary;
    }

    // Finds where the current aim ray intersects the board plane.
    //
    // Geometry overview:
    // - boardPlane.up is treated as the plane normal,
    // - the boardPlane position is treated as a point on the plane,
    // - the ray is tested against that plane.
    //
    // If the ray is nearly parallel to the plane, there is no stable intersection.
    bool IntersectRayWithBoard(Ray r, out Vector3 hit)
    {
        Vector3 n = boardPlane.up;
        // denom tells us how much the ray direction points toward the plane normal.
        // If it is near zero, the ray is almost parallel to the plane.
        float denom = Vector3.Dot(n, r.direction);
        if (Mathf.Abs(denom) < 1e-4f)
        {
            hit = default;
            return false;
        }

        var plane = new Plane(n, boardPlane.position);
        // enter is the distance along the ray to the hit point.
        // The method also rejects absurdly distant hits to avoid accidental intersections.
        if (plane.Raycast(r, out float enter) && enter > 0f && enter < 10f)
        {
            hit = r.GetPoint(enter);
            return true;
        }

        hit = default;
        return false;
    }

    // Makes sure the logging system has a "trial started" entry for the current trial.
    // The boolean _currentTrialLogged prevents the same trial from being logged repeatedly every frame.
    void EnsureCurrentTrialLogged()
    {
        if (_currentTrialLogged || !_isActive || boardPlane == null || experimentLogger == null || _trialIndex >= trialsPerBlock)
            return;

        experimentLogger.LogMeasurementTrialStarted(
            TaskMode.ToString(),
            blockType.ToString(),
            _trialIndex + 1,
            trialsPerBlock,
            boardPlane.position,
            analysisTargetRadiusMeters,
            null);

        _currentTrialLogged = true;
    }

    // Restricts a world-space hit point so it remains inside the configured board bounds.
    Vector3 ClampToBoard(Vector3 hitWorld)
    {
        Vector3 localMeters = WorldToBoardMeters(hitWorld);
        localMeters.x = Mathf.Clamp(localMeters.x, -boardHalfWidth, boardHalfWidth);
        localMeters.z = Mathf.Clamp(localMeters.z, -boardHalfHeight, boardHalfHeight);
        return BoardMetersToWorld(localMeters);
    }

    // Forces a hit to lie on the board's horizontal center line.
    // This is what turns the task into a 1D left-right measurement rather than a full 2D pointing task.
    Vector3 LockToMidline(Vector3 hitWorld)
    {
        Vector3 localMeters = WorldToBoardMeters(hitWorld);
        localMeters.z = 0f;
        return BoardMetersToWorld(localMeters);
    }

    // Returns the signed horizontal board coordinate of the hit.
    // Left and right remain distinguishable because the sign is preserved.
    float SignedOffsetOnBoard(Vector3 hitWorld)
    {
        return WorldToBoardMeters(hitWorld).x;
    }

    // Converts a board-local measurement in meters back into a world-space point.
    //
    // Important detail:
    // the code does not rely on the board's scale directly.
    // Instead, it uses the board's right/forward directions projected onto the plane,
    // so the measurement remains meaningful in world meters.
    Vector3 BoardMetersToWorld(Vector3 localMeters)
    {
        Vector3 right = Vector3.ProjectOnPlane(boardPlane.right, boardPlane.up).normalized;
        Vector3 forward = Vector3.ProjectOnPlane(boardPlane.forward, boardPlane.up).normalized;
        return boardPlane.position + (right * localMeters.x) + (forward * localMeters.z);
    }

    // Converts a world-space point into board-local meters.
    // This is the inverse of BoardMetersToWorld() for the task's measurement coordinates.
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

    // Computes the mean and sample standard deviation of the accepted trial offsets.
    //
    // Mean tells us the average bias.
    // SD tells us how consistent or variable the participant's responses were.
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

    // Exposes the current analysis target to SandboxRunner for controller hover-confirm.
    // For open-loop pointing, the effective target is the board center on the board plane.
    public bool TryGetAimTarget(out Vector3 worldCenter, out Vector3 planeNormal, out float targetRadiusMeters)
    {
        worldCenter = Vector3.zero;
        planeNormal = Vector3.up;
        targetRadiusMeters = analysisTargetRadiusMeters;

        if (!_isActive || boardPlane == null || _trialIndex >= trialsPerBlock)
            return false;

        worldCenter = boardPlane.position;
        planeNormal = boardPlane.up;
        return true;
    }
}
