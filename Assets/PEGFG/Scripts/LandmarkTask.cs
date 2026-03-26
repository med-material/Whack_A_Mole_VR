using System.Collections.Generic;
using UnityEngine;
using TMPro;

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
    private float centreGap = 0.02f;
    private float totalLength = 0.4f;
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

    void Awake()
    {
        AutoAssignReferences();
    }

    void Reset()
    {
        AutoAssignReferences();
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
            AutoAssignReferences();
    }

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

    void Update()
    {
        if (!_isActive) return;
        if (runner == null || boardPlane == null || leftRenderer == null || rightRenderer == null) return;

        WarnIfBoardPoseDrifts();
        UpdateVisuals();

        if (midpointMarker && showBoardMidpoint)
            midpointMarker.position = boardPlane.position;

        if (_trialIndex >= trialsPerBlock)
        {
            UpdateReadout(final: true);
            return;
        }

        var (ray, pose, confirm) = runner.GetTransformedInput();

        if (latchConfirm)
        {
            if (!confirm) _confirmLatched = false;
            if (confirm && _confirmLatched) confirm = false;
        }

        if (requireResetBetweenTrials && hmd != null)
        {
            float resetY = hmd.position.y - resetDropMeters;

            if (!_armed && pose.position.y <= resetY)
                _armed = true;

            if (confirm && !_armed)
                confirm = false;
        }

        if (!IntersectRayWithBoard(ray, out Vector3 hit))
        {
            UpdateReadout(final: false);
            return;
        }

        Vector3 local = WorldToBoardMeters(hit);
        local.z = _currentLineZ;

        ChoiceSide chosenSide = (local.x >= 0f) ? ChoiceSide.Right : ChoiceSide.Left;

        Vector3 snappedLocal = snapToNearestSegment
            ? SnapToSegment(local, chosenSide)
            : ClampToSegment(local, chosenSide);

        Vector3 snappedWorld = BoardMetersToWorld(snappedLocal);

        if (cursorMarker && showCursor)
            cursorMarker.position = snappedWorld;

        if (confirm)
        {
            if (requireResetBetweenTrials)
            {
                if (Time.time - _lastAcceptedTime < minSecondsBetweenTrials)
                {
                    UpdateReadout(final: false);
                    return;
                }
                _lastAcceptedTime = Time.time;
                _armed = false;
            }

            if (latchConfirm) _confirmLatched = true;

            bool isCorrect = (chosenSide == _longerSide);

            _correct.Add(isCorrect ? 1 : 0);
            _choices.Add(chosenSide == ChoiceSide.Right ? 1 : -1);
            _trialIndex++;

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

            SetupNewStimulus();
        }

        UpdateReadout(final: false);
    }

    void CaptureBoardPoseBaseline()
    {
        _boardBaselinePos = boardPlane.position;
        _boardBaselineRot = boardPlane.rotation;
        _hasBoardPoseBaseline = true;
        _loggedBoardPoseDrift = false;
    }

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

    public void ClearSummaries()
    {
        _baselineAcc = null;
        _postAcc = null;
        _baselineAccSd = null;
        _postAccSd = null;
    }

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

    Vector3 ClampToSegment(Vector3 local, ChoiceSide side)
    {
        return SnapToSegment(local, side);
    }

    bool IntersectRayWithBoard(Ray r, out Vector3 hit)
    {
        Vector3 n = boardPlane.up;
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

    static float Accuracy01(List<int> xs)
    {
        if (xs.Count == 0) return 0f;
        int sum = 0;
        for (int i = 0; i < xs.Count; i++) sum += xs[i];
        return (float)sum / xs.Count;
    }

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

    static float MeanChoice(List<int> xs)
    {
        if (xs.Count == 0) return 0f;
        int sum = 0;
        for (int i = 0; i < xs.Count; i++) sum += xs[i];
        return (float)sum / xs.Count;
    }

    Vector3 BoardMetersToWorld(Vector3 localMeters)
    {
        Vector3 right = Vector3.ProjectOnPlane(boardPlane.right, boardPlane.up).normalized;
        Vector3 forward = Vector3.ProjectOnPlane(boardPlane.forward, boardPlane.up).normalized;
        return boardPlane.position + (right * localMeters.x) + (forward * localMeters.z);
    }

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
