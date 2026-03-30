using System.Collections.Generic;
using UnityEngine;
using TMPro;

// LandmarkTask implements a comparative perceptual judgement task.
//
// Unlike OpenLoopPointing and LineBisection, the participant is not trying to place
// a response at one spatial point. Instead, two horizontal line segments are shown
// with a small gap in the middle, and the participant chooses which side appears longer.
//
// In the present prototype this task is retained mainly as an assessment-oriented task
// and future clinical extension, rather than as the main healthy-participant adaptation
// measure. Even so, it still follows the same overall baseline/post block structure and
// uses the same input pipeline from SandboxRunner.
//
// The script is responsible for:
// - constructing the current left/right line stimulus,
// - randomising which side is objectively longer,
// - converting the participant's aim into a left/right choice,
// - logging correctness and stimulus parameters,
// - computing block-level accuracy summaries.
public class LandmarkTask : MonoBehaviour, ISandboxTask
{
    public enum BlockType { Baseline, Post }
    public enum ChoiceSide { Left, Right }

    [Header("References")]
    private SandboxRunner runner;
    private Transform boardPlane;
    private Transform hmd;
    private LineRenderer leftRenderer;
    private LineRenderer rightRenderer;
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

    [Header("Landmark stimulus (metres, board local X)")]
    private float centreGap = 0.1f;
    private float totalLength = 1f;
    private float lengthDifference = 0.06f;
    private float lineZRange = 0.05f;

    [Header("Randomisation")]
    private bool randomiseLineZEachTrial = true;
    private bool randomiseTotalLengthEachTrial = false;
    private float minTotalLength = 0.25f;
    private float maxTotalLength = 0.55f;

    private bool randomiseDifferenceEachTrial = false;
    private float minDifference = 0.02f;
    private float maxDifference = 0.10f;

    [Header("Cursor snapping")]
    private bool snapToNearestSegment = true;

    [Header("Debug")]
    public bool showCursor = true;
    private bool showBoardMidpoint = false;

    int _trialIndex = 0;
    bool _confirmLatched = false;
    bool _armed = true;
    float _lastAcceptedTime = -999f;

    float _currentLineZ = 0f;

    ChoiceSide _longerSide = ChoiceSide.Left;
    float _leftLen = 0.2f;
    float _rightLen = 0.2f;

    readonly List<int> _correct = new List<int>(128);
    readonly List<int> _choices = new List<int>(128);

    float? _baselineAcc = null;
    float? _postAcc = null;
    float? _baselineAccSd = null;
    float? _postAccSd = null;

    bool _isActive;
    bool _hasBoardPoseBaseline;
    bool _loggedBoardPoseDrift;
    Vector3 _boardBaselinePos;
    Quaternion _boardBaselineRot;

    public SandboxRunner.TaskMode TaskMode => SandboxRunner.TaskMode.Landmark;
    public string GetCurrentBlockName() => blockType.ToString();
    public int GetCurrentTrialNumber() => Mathf.Clamp(_trialIndex + 1, 1, trialsPerBlock);

    // Standard Unity startup hook. We use it to find scene references automatically
    // so the task is easier to set up in the editor.
    void Awake()
    {
        AutoAssignReferences();
    }

    // Reset is called when the component is first added in the editor.
    // This makes sure the script tries to populate its references immediately.
    void Reset()
    {
        AutoAssignReferences();
    }

    // OnValidate runs in the editor when values change in the Inspector.
    // Keeping reference assignment here reduces manual setup errors.
    void OnValidate()
    {
        if (!Application.isPlaying)
            AutoAssignReferences();
    }

    // Called by SandboxRunner when this task becomes active or inactive.
    //
    // When activated, LandmarkTask:
    // - enables its visual objects,
    // - ensures the board/line objects live under WorldRoot rather than the XR rig,
    // - captures the board pose as a baseline for drift warnings,
    // - and creates the current stimulus.
    public void SetTaskActive(bool active)
    {
        _isActive = active;
        UpdateVisuals();
        EnsureLandmarkObjectsAnchoredToWorldRoot();

        if (_isActive && boardPlane != null)
            CaptureBoardPoseBaseline();

        if (_isActive && boardPlane != null && leftRenderer != null && rightRenderer != null)
            SetupNewStimulus();
    }

    // Automatically finds the scene objects LandmarkTask depends on.
    //
    // This includes:
    // - SandboxRunner for input and progression,
    // - the board plane for geometric calculations,
    // - the HMD for reset gating,
    // - the two line renderers that display the landmark stimulus,
    // - optional cursor/midpoint markers,
    // - the UI readout,
    // - the experiment logger.
    void AutoAssignReferences()
    {
        if (runner == null)
            runner = FindFirstObjectByType<SandboxRunner>();

        if (boardPlane == null)
        {
            var worldRoot = GameObject.Find("WorldRoot")?.transform;
            boardPlane = worldRoot != null ? worldRoot.Find("Board") : null;
            if (boardPlane == null)
                boardPlane = GameObject.Find("Board")?.transform;
        }

        if (hmd == null)
        {
            var camObj = GameObject.Find("Camera") ?? GameObject.Find("Camera (eye)") ?? GameObject.Find("Main Camera");
            if (camObj != null) hmd = camObj.transform;
            else if (Camera.main != null) hmd = Camera.main.transform;
        }

        if (leftRenderer == null)
        {
            var worldRoot = GameObject.Find("WorldRoot")?.transform;
            var leftObj = worldRoot != null ? worldRoot.Find("LandmarkLeft") : null;
            leftRenderer = (leftObj != null ? leftObj.GetComponent<LineRenderer>() : null) ??
                           GameObject.Find("LandmarkLeft")?.GetComponent<LineRenderer>();
        }

        if (rightRenderer == null)
        {
            var worldRoot = GameObject.Find("WorldRoot")?.transform;
            var rightObj = worldRoot != null ? worldRoot.Find("LandmarkRight") : null;
            rightRenderer = (rightObj != null ? rightObj.GetComponent<LineRenderer>() : null) ??
                            GameObject.Find("LandmarkRight")?.GetComponent<LineRenderer>();
        }

        if (cursorMarker == null)
            cursorMarker = GameObject.Find("Hitmarker")?.transform ?? GameObject.Find("HitMarker")?.transform;

        if (midpointMarker == null)
            midpointMarker = GameObject.Find("MidPointMarker")?.transform ?? GameObject.Find("MidpointMarker")?.transform;

        if (readout == null)
            readout = runner != null ? runner.GetStatReadout() : GameObject.Find("StatText")?.GetComponent<TextMeshProUGUI>();

        if (experimentLogger == null)
            experimentLogger = runner != null ? runner.GetExperimentLogger() : FindFirstObjectByType<PrismExperimentLogger>();

        EnsureLandmarkObjectsAnchoredToWorldRoot();
    }

    // The landmark lines and board must remain fixed in world space.
    //
    // If they accidentally become children of the HMD rig or another moving object,
    // the stimulus would drift with head movement, which would invalidate the task.
    // This helper therefore reparents them to WorldRoot if needed.
    void EnsureLandmarkObjectsAnchoredToWorldRoot()
    {
        Transform worldRoot = GameObject.Find("WorldRoot")?.transform;
        if (worldRoot == null) return;

        if (boardPlane != null && boardPlane.parent != worldRoot)
        {
            Debug.LogWarning($"[LandmarkTask] Reparenting Board ('{boardPlane.name}') to WorldRoot to prevent head-coupled drift.");
            boardPlane.SetParent(worldRoot, true);
        }

        if (leftRenderer != null && leftRenderer.transform.parent != worldRoot)
        {
            Debug.LogWarning($"[LandmarkTask] Reparenting '{leftRenderer.name}' to WorldRoot to prevent head-coupled drift.");
            leftRenderer.transform.SetParent(worldRoot, true);
        }

        if (rightRenderer != null && rightRenderer.transform.parent != worldRoot)
        {
            Debug.LogWarning($"[LandmarkTask] Reparenting '{rightRenderer.name}' to WorldRoot to prevent head-coupled drift.");
            rightRenderer.transform.SetParent(worldRoot, true);
        }
    }

    // Shows or hides the task visuals depending on whether the task is currently active.
    //
    // The cursor can be useful to indicate where the current selection falls,
    // but it is not itself the task outcome. The actual measured output is the final
    // categorical choice and whether it matched the objectively longer side.
    void UpdateVisuals()
    {
        if (leftRenderer)
            leftRenderer.gameObject.SetActive(_isActive);

        if (rightRenderer)
            rightRenderer.gameObject.SetActive(_isActive);

        if (cursorMarker)
            cursorMarker.gameObject.SetActive(_isActive && showCursor);

        if (midpointMarker)
            midpointMarker.gameObject.SetActive(_isActive && showBoardMidpoint);
    }

    // Main per-frame loop for the landmark task.
    //
    // Chronological trial flow:
    // 1. verify that the task is active and references exist,
    // 2. monitor whether the board/stimulus is drifting unexpectedly,
    // 3. stop early if the block is already complete,
    // 4. obtain the participant's transformed input from SandboxRunner,
    // 5. apply confirm-latching and optional reset gating,
    // 6. intersect the pointing ray with the board,
    // 7. convert that hit into a left or right categorical choice,
    // 8. if confirmed, accept and log the response,
    // 9. if the block ends, compute summary accuracy,
    // 10. otherwise create the next stimulus.
    void Update()
    {
        // If the task is not currently the active task, do nothing.
        if (!_isActive) return;
        // LandmarkTask depends on the runner, board, and both line renderers.
        if (runner == null || boardPlane == null || leftRenderer == null || rightRenderer == null) return;

        WarnIfBoardPoseDrifts();
        UpdateVisuals();

        // Optional midpoint marker, mainly as a spatial reference/debug aid.
        if (midpointMarker && showBoardMidpoint)
            midpointMarker.position = boardPlane.position;

        // Once all trials for this block are complete, just keep showing the summary.
        if (_trialIndex >= trialsPerBlock)
        {
            UpdateReadout(final: true);
            return;
        }

        // Input is already unified by SandboxRunner, regardless of controller or hand mode.
        var (ray, pose, confirm) = runner.GetTransformedInput();

        if (latchConfirm)
        {
            // Prevent a held button/gesture from being interpreted as multiple choices.
            if (!confirm) _confirmLatched = false;
            if (confirm && _confirmLatched) confirm = false;
        }

        if (requireResetBetweenTrials && hmd != null)
        {
            // Optional return-to-start gating, similar to the other measurement tasks.
            float resetY = hmd.position.y - resetDropMeters;

            if (!_armed && pose.position.y <= resetY)
                _armed = true;

            if (confirm && !_armed)
                confirm = false;
        }

        // If the aim ray misses the board entirely, there is no valid current choice.
        if (!IntersectRayWithBoard(ray, out Vector3 hit))
        {
            UpdateReadout(final: false);
            return;
        }

        // Convert the world-space hit into board-local metres.
        Vector3 local = WorldToBoardMeters(hit);
        // Landmark lines live at the current task depth, not necessarily exactly at board origin.
        local.z = _currentLineZ;

        // The sign of x tells us which side the participant is currently selecting.
        ChoiceSide chosenSide = (local.x >= 0f) ? ChoiceSide.Right : ChoiceSide.Left;

        // The cursor can optionally be snapped onto the chosen segment, so the visual indicator
        // stays on the displayed line rather than floating in empty board space.
        Vector3 snappedLocal = snapToNearestSegment
            ? SnapToSegment(local, chosenSide)
            : ClampToSegment(local, chosenSide);

        Vector3 snappedWorld = BoardMetersToWorld(snappedLocal);

        if (cursorMarker && showCursor)
            // This marker shows the currently selected side/position, not correctness.
            cursorMarker.position = snappedWorld;

        if (confirm)
        {
            if (requireResetBetweenTrials)
            {
                // Avoid immediately double-counting two near-simultaneous confirms.
                if (Time.time - _lastAcceptedTime < minSecondsBetweenTrials)
                {
                    UpdateReadout(final: false);
                    return;
                }
                _lastAcceptedTime = Time.time;
                _armed = false;
            }

            if (latchConfirm) _confirmLatched = true;

            // The response is correct only if the chosen side matches the objectively longer side.
            bool isCorrect = (chosenSide == _longerSide);

            // _correct stores 1 or 0 so accuracy can be computed later.
            _correct.Add(isCorrect ? 1 : 0);
            // _choices stores left/right tendency numerically, allowing a simple choice-bias measure.
            _choices.Add(chosenSide == ChoiceSide.Right ? 1 : -1);
            _trialIndex++;

            // Log both the response and the exact stimulus parameters for this trial.
            experimentLogger?.LogMeasurementTrial(
                TaskMode.ToString(),
                blockType.ToString(),
                _trialIndex,
                trialsPerBlock,
                snappedWorld,
                new Dictionary<string, object>
                {
                    { "ChoiceSide", chosenSide.ToString() },
                    { "LongerSide", _longerSide.ToString() },
                    { "IsCorrect", isCorrect ? 1 : 0 },
                    { "GapMeters", centreGap },
                    { "LeftLengthMeters", _leftLen },
                    { "RightLengthMeters", _rightLen },
                    { "LineZMeters", _currentLineZ }
                });

            Debug.Log($"[Landmark] block={blockType} trial={_trialIndex}/{trialsPerBlock} choice={chosenSide} longer={_longerSide} correct={(isCorrect ? 1 : 0)} " +
                      $"(L={_leftLen:0.000}m R={_rightLen:0.000}m gap={centreGap:0.000}m z={_currentLineZ:0.000}m)");

            if (_trialIndex >= trialsPerBlock)
            {
                // Block-level summary: proportion correct and its variability across trials.
                float acc = Accuracy01(_correct);
                float accSd = AccuracySd01(_correct);
                if (blockType == BlockType.Baseline) _baselineAcc = acc;
                if (blockType == BlockType.Post) _postAcc = acc;
                if (blockType == BlockType.Baseline) _baselineAccSd = accSd;
                if (blockType == BlockType.Post) _postAccSd = accSd;

                Debug.Log($"[Landmark] block={blockType} COMPLETE acc={acc * 100f:0.0}% n={_correct.Count}");

                experimentLogger?.LogTaskMetricSummary(
                    TaskMode.ToString(),
                    blockType.ToString(),
                    "Accuracy",
                    "ratio",
                    _correct.Count,
                    acc,
                    accSd,
                    "Proportion correct in landmark task");

                if (_baselineAcc.HasValue && _postAcc.HasValue)
                {
                    // Post minus baseline is the simplest way to express change in accuracy across the run.
                    float delta = (_postAcc.Value - _baselineAcc.Value) * 100f;
                    Debug.Log($"[Landmark] CHANGE (Post - Baseline) = {delta:0.0} percentage points");

                    experimentLogger?.LogBlockCompleted(
                        TaskMode.ToString(),
                        blockType.ToString(),
                        new Dictionary<string, object>
                        {
                            { "Accuracy01", acc },
                            { "DeltaAccuracyPct", delta },
                            { "TrialsPerBlock", _correct.Count }
                        });

                    experimentLogger?.LogTaskAftereffectSummary(
                        TaskMode.ToString(),
                        "Accuracy",
                        "ratio",
                        _correct.Count,
                        _baselineAcc.Value,
                        _baselineAccSd,
                        _postAcc.Value,
                        _postAccSd,
                        _postAcc.Value - _baselineAcc.Value,
                        "Post minus baseline landmark accuracy");
                }
                else
                {
                    experimentLogger?.LogBlockCompleted(
                        TaskMode.ToString(),
                        blockType.ToString(),
                        new Dictionary<string, object>
                        {
                            { "Accuracy01", acc },
                            { "TrialsPerBlock", _correct.Count }
                        });
                }

                UpdateReadout(final: true);
                runner?.NotifyMeasurementBlockCompleted(TaskMode, blockType.ToString());
                return;
            }

            // Prepare the next left/right comparison stimulus.
            SetupNewStimulus();
        }

        UpdateReadout(final: false);
    }

    // Store the board pose at task start so we can warn if it begins moving later.
    void CaptureBoardPoseBaseline()
    {
        _boardBaselinePos = boardPlane.position;
        _boardBaselineRot = boardPlane.rotation;
        _hasBoardPoseBaseline = true;
        _loggedBoardPoseDrift = false;
    }

    // Landmark is meant to be judged against a stable world-fixed stimulus.
    // If the board starts moving, log a warning once so the issue is not silently missed.
    void WarnIfBoardPoseDrifts()
    {
        if (!_hasBoardPoseBaseline || _loggedBoardPoseDrift || boardPlane == null)
            return;

        float posDrift = Vector3.Distance(_boardBaselinePos, boardPlane.position);
        float rotDrift = Quaternion.Angle(_boardBaselineRot, boardPlane.rotation);

        if (posDrift > 0.005f || rotDrift > 0.5f)
        {
            _loggedBoardPoseDrift = true;
            Debug.LogWarning($"[LandmarkTask] Board pose is changing during task (pos {posDrift:0.000}m, rot {rotDrift:0.00}deg). " +
                             "If lines move with head motion, ensure Board/Landmark lines are not under the HMD rig.");
        }
    }

    // Starts a fresh baseline or post block.
    // This resets counters, accepted choices, and gating/latch state.
    public void StartNewBlock(BlockType newBlock)
    {
        blockType = newBlock;
        _trialIndex = 0;
        _confirmLatched = false;

        _correct.Clear();
        _choices.Clear();

        _armed = true;
        _lastAcceptedTime = -999f;

        if (_isActive)
            SetupNewStimulus();

        UpdateReadout(final: false);
    }

    // Clears stored baseline/post summaries so a new run starts cleanly.
    public void ClearSummaries()
    {
        _baselineAcc = null;
        _postAcc = null;
        _baselineAccSd = null;
        _postAccSd = null;
    }

    // Creates the current landmark stimulus.
    //
    // Each trial can vary in:
    // - total line length,
    // - left-vs-right difference,
    // - depth position on the board.
    //
    // One side is chosen to be objectively longer, then the exact left/right lengths
    // are computed and drawn.
    void SetupNewStimulus()
    {
        if (randomiseLineZEachTrial)
            _currentLineZ = Random.Range(-lineZRange, lineZRange);
        else
            _currentLineZ = 0f;

        float tot = totalLength;
        if (randomiseTotalLengthEachTrial)
            tot = Random.Range(minTotalLength, maxTotalLength);

        float diff = lengthDifference;
        if (randomiseDifferenceEachTrial)
            diff = Random.Range(minDifference, maxDifference);

        _longerSide = (Random.value < 0.5f) ? ChoiceSide.Left : ChoiceSide.Right;

        float a = (tot + diff) * 0.5f;
        float b = (tot - diff) * 0.5f;

        if (_longerSide == ChoiceSide.Left)
        {
            _leftLen = Mathf.Max(0.01f, a);
            _rightLen = Mathf.Max(0.01f, b);
        }
        else
        {
            _leftLen = Mathf.Max(0.01f, b);
            _rightLen = Mathf.Max(0.01f, a);
        }

        DrawSegments();
    }

    // Writes the current left and right segment endpoints into the two line renderers.
    //
    // The centre gap means the line is intentionally split into two visible segments
    // rather than shown as one continuous line.
    void DrawSegments()
    {
        float halfGap = Mathf.Max(0f, centreGap) * 0.5f;

        Vector3 l0 = new Vector3(-halfGap - _leftLen, 0f, _currentLineZ);
        Vector3 l1 = new Vector3(-halfGap, 0f, _currentLineZ);

        Vector3 r0 = new Vector3(+halfGap, 0f, _currentLineZ);
        Vector3 r1 = new Vector3(+halfGap + _rightLen, 0f, _currentLineZ);

        Vector3 l0w = BoardMetersToWorld(l0);
        Vector3 l1w = BoardMetersToWorld(l1);
        Vector3 r0w = BoardMetersToWorld(r0);
        Vector3 r1w = BoardMetersToWorld(r1);

        leftRenderer.positionCount = 2;
        leftRenderer.useWorldSpace = true;
        leftRenderer.SetPosition(0, l0w);
        leftRenderer.SetPosition(1, l1w);

        rightRenderer.positionCount = 2;
        rightRenderer.useWorldSpace = true;
        rightRenderer.SetPosition(0, r0w);
        rightRenderer.SetPosition(1, r1w);
    }

    // Projects the cursor onto whichever segment is currently being selected.
    // This keeps the marker on the left segment when the user is choosing left,
    // and on the right segment when the user is choosing right.
    Vector3 SnapToSegment(Vector3 local, ChoiceSide side)
    {
        float halfGap = Mathf.Max(0f, centreGap) * 0.5f;

        if (side == ChoiceSide.Left)
        {
            float minX = -halfGap - _leftLen;
            float maxX = -halfGap;
            local.x = Mathf.Clamp(local.x, minX, maxX);
        }
        else
        {
            float minX = +halfGap;
            float maxX = +halfGap + _rightLen;
            local.x = Mathf.Clamp(local.x, minX, maxX);
        }

        return local;
    }

    // ClampToSegment currently uses the same behaviour as SnapToSegment.
    // It exists as a separate function so the behaviour could later diverge
    // without having to rewrite the calling code.
    Vector3 ClampToSegment(Vector3 local, ChoiceSide side)
    {
        return SnapToSegment(local, side);
    }

    // Finds where the current aim ray intersects the board plane.
    bool IntersectRayWithBoard(Ray r, out Vector3 hit)
    {
        Vector3 n = boardPlane.up;
        // If the ray is almost parallel to the board, the intersection is unstable or absent.
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

    // Updates the small task readout shown in the scene.
    //
    // Before any response is accepted:
    // - shows trial number and gate state.
    //
    // During the block:
    // - shows running accuracy and a simple left/right choice bias.
    //
    // After block completion:
    // - shows final accuracy, bias, and post-baseline change if both blocks exist.
    void UpdateReadout(bool final)
    {
        if (!readout || !_isActive) return;

        string gateTxt = (requireResetBetweenTrials && hmd != null) ? (_armed ? "ARMED" : "RESET") : "—";

        if (_correct.Count == 0)
        {
            readout.text =
                $"Landmark ({blockType})\n" +
                $"Trial {_trialIndex}/{trialsPerBlock}\n" +
                $"Acc: —\n" +
                $"Gate: {gateTxt}";
            return;
        }

        float acc = Accuracy01(_correct);
        float bias = MeanChoice(_choices);

        if (!final)
        {
            readout.text =
                $"Landmark ({blockType})\n" +
                $"Trial {_trialIndex}/{trialsPerBlock}\n" +
                $"Acc: {acc * 100f:0.0}%\n" +
                $"Bias (R=+1): {bias:0.00}\n" +
                $"Gate: {gateTxt}";
            return;
        }

        string summary =
            $"Landmark ({blockType}) COMPLETE\n" +
            $"n={_correct.Count}\n" +
            $"Acc: {acc * 100f:0.0}%\n" +
            $"Bias (R=+1): {bias:0.00}";

        if (_baselineAcc.HasValue && _postAcc.HasValue)
        {
            float delta = (_postAcc.Value - _baselineAcc.Value) * 100f;
            summary += $"\nΔAcc: {delta:0.0} pp";
        }

        readout.text = summary;
    }

    // Returns proportion correct as a value between 0 and 1.
    static float Accuracy01(List<int> xs)
    {
        if (xs.Count == 0) return 0f;
        int sum = 0;
        for (int i = 0; i < xs.Count; i++) sum += xs[i];
        return (float)sum / xs.Count;
    }

    // Sample standard deviation of the binary correctness list.
    static float AccuracySd01(List<int> xs)
    {
        if (xs.Count <= 1) return 0f;
        float mean = Accuracy01(xs);
        float sumSq = 0f;
        for (int i = 0; i < xs.Count; i++)
        {
            float d = xs[i] - mean;
            sumSq += d * d;
        }
        return Mathf.Sqrt(sumSq / (xs.Count - 1));
    }

    // Mean signed side choice:
    // -1 means always left,
    // +1 means always right,
    // values near 0 mean no strong side bias.
    static float MeanChoice(List<int> xs)
    {
        if (xs.Count == 0) return 0f;
        int sum = 0;
        for (int i = 0; i < xs.Count; i++) sum += xs[i];
        return (float)sum / xs.Count;
    }

    // Converts board-local coordinates in metres to a world-space position.
    Vector3 BoardMetersToWorld(Vector3 localMeters)
    {
        Vector3 right = Vector3.ProjectOnPlane(boardPlane.right, boardPlane.up).normalized;
        Vector3 forward = Vector3.ProjectOnPlane(boardPlane.forward, boardPlane.up).normalized;
        return boardPlane.position + (right * localMeters.x) + (forward * localMeters.z);
    }

    // Inverse of BoardMetersToWorld(): converts a world-space point into board-local metres.
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
