using System.Collections.Generic;
using UnityEngine;
using TMPro;

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

    readonly List<float> _offsetsCm = new List<float>(128);
    float? _baselineMeanCm = null;
    float? _postMeanCm = null;
    float? _baselineSdCm = null;
    float? _postSdCm = null;

    bool _isActive;

    public SandboxRunner.TaskMode TaskMode => SandboxRunner.TaskMode.OpenLoop;

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
    }

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

    void UpdateVisuals()
    {
        if (hitMarker)
            hitMarker.gameObject.SetActive(_isActive && showLiveAimMarker);

        if (midpointMarker)
            midpointMarker.gameObject.SetActive(_isActive && showBoardMidpoint);
    }

    void Update()
    {
        if (!_isActive) return;
        if (runner == null || boardPlane == null) return;

        UpdateVisuals();

        if (midpointMarker && showBoardMidpoint)
            midpointMarker.position = boardPlane.position;

        if (_trialIndex >= trialsPerBlock)
        {
            UpdateReadout(final: true);
            return;
        }

        EnsureCurrentTrialLogged();

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

        if (clampToBoard)
            hit = ClampToBoard(hit);

        if (lockToHorizontalMidline)
            hit = LockToMidline(hit);

        if (showLiveAimMarker && hitMarker)
            hitMarker.position = hit;

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

            if (!showLiveAimMarker && hitMarker)
                hitMarker.position = hit;

            float signedMeters = SignedOffsetOnBoard(hit);
            float signedCm = signedMeters * 100f;

            _offsetsCm.Add(signedCm);
            _trialIndex++;
            _currentTrialLogged = false;

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
                    "SignedEndpointError",
                    "cm",
                    _offsetsCm.Count,
                    mean,
                    sd,
                    "Mean signed horizontal endpoint error");

                if (_baselineMeanCm.HasValue && _postMeanCm.HasValue)
                {
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

            EnsureCurrentTrialLogged();
            UpdateReadout(final: _trialIndex >= trialsPerBlock);
            if (_trialIndex >= trialsPerBlock)
                runner?.NotifyMeasurementBlockCompleted(TaskMode, blockType.ToString());
        }
        else
        {
            UpdateReadout(final: false);
        }
    }

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

    public void ClearSummaries()
    {
        _baselineMeanCm = null;
        _postMeanCm = null;
        _baselineSdCm = null;
        _postSdCm = null;
    }

    void UpdateReadout(bool final)
    {
        if (!readout || !_isActive) return;

        if (_offsetsCm.Count == 0)
        {
            string armedTxt = (requireResetBetweenTrials && hmd != null) ? (_armed ? "ARMED" : "RESET") : "—";
            readout.text = $"OLP ({blockType})\nTrial {_trialIndex}/{trialsPerBlock}\nOffset: —\nGate: {armedTxt}";
            return;
        }

        var (mean, sd) = MeanAndSd(_offsetsCm);

        if (!final)
        {
            float last = _offsetsCm[_offsetsCm.Count - 1];
            string armedTxt = (requireResetBetweenTrials && hmd != null) ? (_armed ? "ARMED" : "RESET") : "—";

            readout.text =
                $"OLP ({blockType})\n" +
                $"Trial {_trialIndex}/{trialsPerBlock}\n" +
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
            float afterEffect = _postMeanCm.Value - _baselineMeanCm.Value;
            summary += $"\nAfter-effect: {afterEffect:0.0} cm";
        }

        readout.text = summary;
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

    Vector3 ClampToBoard(Vector3 hitWorld)
    {
        Vector3 local = boardPlane.InverseTransformPoint(hitWorld);

        Vector3 s = boardPlane.lossyScale;
        float halfXLocal = boardHalfWidth / (Mathf.Abs(s.x) < 1e-6f ? 1f : Mathf.Abs(s.x));
        float halfZLocal = boardHalfHeight / (Mathf.Abs(s.z) < 1e-6f ? 1f : Mathf.Abs(s.z));

        local.x = Mathf.Clamp(local.x, -halfXLocal, halfXLocal);
        local.z = Mathf.Clamp(local.z, -halfZLocal, halfZLocal);

        return boardPlane.TransformPoint(local);
    }

    Vector3 LockToMidline(Vector3 hitWorld)
    {
        Vector3 local = boardPlane.InverseTransformPoint(hitWorld);
        local.z = 0f;
        return boardPlane.TransformPoint(local);
    }

    float SignedOffsetOnBoard(Vector3 hitWorld)
    {
        Vector3 local = boardPlane.InverseTransformPoint(hitWorld);
        return local.x;
    }

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
