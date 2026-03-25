using UnityEngine;
using TMPro;

public class ExposureTask : MonoBehaviour, ISandboxTask, IAimTargetProvider
{
    private const string HitMarkerObjectName = "Hitmarker";

    [Header("References")]
    private SandboxRunner runner;
    private Transform boardPlane;
    private Transform hmd;

    [Tooltip("Three targets in a 1x3 horizontal layout: Left, Center, Right.")]
    private Transform[] targets = new Transform[3];

    [Tooltip("Optional renderers matching the target order. If left empty, they will be fetched from the target objects.")]
    private Renderer[] targetRenderers = new Renderer[3];

    private Transform cursorMarker;
    private Transform hitMarker;
    private TextMeshProUGUI readout;
    private PrismExperimentLogger experimentLogger;

    [Header("Exposure Block")]
    [Tooltip("Number of successful target hits required before exposure is complete.")]
    public int successfulHitsToComplete = 90;

    [Tooltip("If true, target order is randomized. If false, cycles Left-Center-Right.")]
    private bool randomizeTargetOrder = true;

    [Tooltip("Prevent immediate repetition of the same target when randomizing.")]
    private bool avoidImmediateRepeat = true;

    [Header("Hit Logic")]
    [Tooltip("Radius around the target that counts as a hit, in metres.")]
    private float hitRadiusMeters = 0.112f;

    [Tooltip("If true, the cursor marker is shown on the board plane.")]
    public bool showCursor = false;

    [Tooltip("If true, the hit marker is shown at the accepted click position.")]
    public bool showHitMarker = true;

    [Header("Trial gating (return to start posture)")]
    public bool requireResetBetweenTrials = false;
    private float resetDropMeters = 0.6f;
    private float minSecondsBetweenAttempts = 0.10f;
    private bool latchConfirm = true;

    [Header("Colors")]
    private Color idleColor = Color.white;
    private Color activeColor = Color.green;
    private Color hitColor = Color.cyan;
    private Color missColor = Color.red;

    [Header("Debug")]
    public bool logAttempts = true;

    bool _isActive;
    bool _confirmLatched = false;
    bool _armed = true;
    float _lastAcceptedTime = -999f;

    int _successCount = 0;
    int _attemptCount = 0;
    int _currentTargetIndex = 1;
    int _lastTargetIndex = -1;

    public SandboxRunner.TaskMode TaskMode => SandboxRunner.TaskMode.Exposure;

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
        UpdateVisualsFromState();

        if (_isActive)
            RefreshTargetVisuals();
    }

    void Awake()
    {
        AutoAssignReferences();
        AutoFillRenderers();
    }

    void Start()
    {
        UpdateVisualsFromState();
    }

    void UpdateVisualsFromState()
    {
        SetVisualsVisible(_isActive);
    }

    void AutoAssignReferences()
    {
        if (runner == null)
            runner = FindObjectOfType<SandboxRunner>();

        if (boardPlane == null)
            boardPlane = GameObject.Find("Board")?.transform;

        if (hmd == null)
        {
            var camObj = GameObject.Find("Camera") ?? GameObject.Find("Camera (eye)") ?? GameObject.Find("Main Camera");
            if (camObj != null) hmd = camObj.transform;
            else if (Camera.main != null) hmd = Camera.main.transform;
        }

        if (cursorMarker == null)
            cursorMarker = FindHitMarker();

        if (hitMarker == null)
            hitMarker = FindHitMarker();

        if (readout == null)
            readout = runner != null ? runner.GetStatReadout() : GameObject.Find("StatText")?.GetComponent<TextMeshProUGUI>();

        if (experimentLogger == null)
            experimentLogger = runner != null ? runner.GetExperimentLogger() : FindFirstObjectByType<PrismExperimentLogger>();

        if (targets == null || targets.Length != 3)
            targets = new Transform[3];

        if (targets[0] == null) targets[0] = GameObject.Find("ExposureTarget1")?.transform ?? GameObject.Find("ExposureTargetLeft")?.transform;
        if (targets[1] == null) targets[1] = GameObject.Find("ExposureTarget2")?.transform ?? GameObject.Find("ExposureTargetCenter")?.transform;
        if (targets[2] == null) targets[2] = GameObject.Find("ExposureTarget3")?.transform ?? GameObject.Find("ExposureTargetRight")?.transform;
    }

    public void StartExposureBlock()
    {
        _successCount = 0;
        _attemptCount = 0;
        _confirmLatched = false;
        _armed = true;
        _lastAcceptedTime = -999f;

        AutoFillRenderers();
        SetVisualsVisible(true);
        experimentLogger?.LogExposureConfig(hitRadiusMeters);
        if (hitMarker != null)
            hitMarker.gameObject.SetActive(showHitMarker);
        PickNextTarget(forceCenterFirst: true);
        if (targets[_currentTargetIndex] != null)
            experimentLogger?.LogExposureTargetSpawned(_currentTargetIndex, targets[_currentTargetIndex].position);
        RefreshTargetVisuals();
        UpdateReadout();
    }

    void Update()
    {
        if (!_isActive) return;
        if (runner == null || boardPlane == null || targets == null || targets.Length < 3) return;

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

        if (!IntersectRayWithBoard(ray, out Vector3 hitPoint))
        {
            UpdateReadout();
            return;
        }

        if (cursorMarker && showCursor)
            cursorMarker.position = hitPoint;

        if (confirm)
        {
            if (requireResetBetweenTrials)
            {
                if (Time.time - _lastAcceptedTime < minSecondsBetweenAttempts)
                {
                    UpdateReadout();
                    return;
                }

                _lastAcceptedTime = Time.time;
                _armed = false;
            }

            if (latchConfirm)
                _confirmLatched = true;

            _attemptCount++;

            var target = targets[_currentTargetIndex];
            float dist = Vector3.Distance(hitPoint, target.position);
            bool isHit = dist <= hitRadiusMeters;

            experimentLogger?.LogPointerShoot(
                _attemptCount,
                _successCount,
                _currentTargetIndex,
                hitPoint,
                target.position);

            experimentLogger?.LogExposureAttempt(
                _attemptCount,
                _successCount,
                _currentTargetIndex,
                isHit,
                dist,
                hitPoint,
                target.position);

            if (hitMarker && showHitMarker)
            {
                hitMarker.gameObject.SetActive(true);
                hitMarker.position = hitPoint;
            }

            if (isHit)
            {
                _successCount++;

                if (logAttempts)
                    Debug.Log($"[Exposure] HIT {_successCount}/{successfulHitsToComplete} on target {_currentTargetIndex} (attempt {_attemptCount}, dist={dist:0.000}m)");

                FlashSingleTarget(_currentTargetIndex, hitColor);

                if (_successCount >= successfulHitsToComplete)
                {
                    experimentLogger?.LogExposureCompleted(_successCount, _attemptCount);
                    UpdateReadout();
                    Debug.Log("[Exposure] Exposure block complete.");
                    runner?.NotifyExposureCompleted();
                    return;
                }

                PickNextTarget(forceCenterFirst: false);
                if (targets[_currentTargetIndex] != null)
                    experimentLogger?.LogExposureTargetSpawned(_currentTargetIndex, targets[_currentTargetIndex].position);
                RefreshTargetVisuals();
            }
            else
            {
                if (logAttempts)
                    Debug.Log($"[Exposure] MISS target {_currentTargetIndex} (attempt {_attemptCount}, dist={dist:0.000}m)");

                FlashSingleTarget(_currentTargetIndex, missColor);
            }
        }

        UpdateReadout();
    }

    void SetVisualsVisible(bool visible)
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
                targets[i].gameObject.SetActive(visible);
        }

        if (cursorMarker)
            cursorMarker.gameObject.SetActive(visible && showCursor);

        if (hitMarker)
            hitMarker.gameObject.SetActive(visible && showHitMarker);

        if (visible)
            RefreshTargetVisuals();
    }

    Transform FindHitMarker()
    {
        var worldRoot = GameObject.Find("WorldRoot")?.transform;
        if (worldRoot != null)
        {
            var child = worldRoot.Find(HitMarkerObjectName) ?? worldRoot.Find("HitMarker");
            if (child != null)
                return child;
        }

        return GameObject.Find(HitMarkerObjectName)?.transform ?? GameObject.Find("HitMarker")?.transform;
    }

    void AutoFillRenderers()
    {
        if (targets == null) return;

        if (targetRenderers == null || targetRenderers.Length != targets.Length)
            targetRenderers = new Renderer[targets.Length];

        for (int i = 0; i < targets.Length; i++)
        {
            if (targetRenderers[i] == null && targets[i] != null)
                targetRenderers[i] = targets[i].GetComponentInChildren<Renderer>();
        }
    }

    void PickNextTarget(bool forceCenterFirst)
    {
        _lastTargetIndex = _currentTargetIndex;

        if (forceCenterFirst)
        {
            _currentTargetIndex = Mathf.Clamp(1, 0, targets.Length - 1);
            return;
        }

        if (!randomizeTargetOrder)
        {
            _currentTargetIndex++;
            if (_currentTargetIndex >= targets.Length)
                _currentTargetIndex = 0;
            return;
        }

        int next = _currentTargetIndex;
        int safety = 0;

        while (safety < 20)
        {
            next = Random.Range(0, targets.Length);

            if (!avoidImmediateRepeat || next != _lastTargetIndex)
                break;

            safety++;
        }

        _currentTargetIndex = next;
    }

    void RefreshTargetVisuals()
    {
        if (!_isActive) return;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null) continue;
            SetRendererColor(targetRenderers[i], i == _currentTargetIndex ? activeColor : idleColor);
        }
    }

    void FlashSingleTarget(int index, Color color)
    {
        if (index < 0 || index >= targetRenderers.Length) return;
        if (targetRenderers[index] == null) return;

        SetRendererColor(targetRenderers[index], color);
    }

    void SetRendererColor(Renderer r, Color c)
    {
        if (r == null || r.material == null) return;
        r.material.color = c;
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

    void UpdateReadout()
    {
        if (!readout || !_isActive) return;

        string gateTxt = (requireResetBetweenTrials && hmd != null) ? (_armed ? "ARMED" : "RESET") : "—";
        string targetName = TargetLabel(_currentTargetIndex);

        readout.text =
            $"Exposure\n" +
            $"Effect: {runner.CurrentEffectMode}\n" +
            $"Target: {targetName}\n" +
            $"Hits: {_successCount}/{successfulHitsToComplete}\n" +
            $"Attempts: {_attemptCount}\n" +
            $"Gate: {gateTxt}";
    }

    string TargetLabel(int i)
    {
        return i switch
        {
            0 => "Left",
            1 => "Center",
            2 => "Right",
            _ => $"T{i}"
        };
    }

    public bool TryGetAimTarget(out Vector3 worldCenter, out Vector3 planeNormal, out float targetRadiusMeters)
    {
        worldCenter = Vector3.zero;
        planeNormal = Vector3.up;
        targetRadiusMeters = hitRadiusMeters;

        if (!_isActive || boardPlane == null || targets == null || _currentTargetIndex < 0 || _currentTargetIndex >= targets.Length)
            return false;

        Transform target = targets[_currentTargetIndex];
        if (target == null)
            return false;

        worldCenter = target.position;
        planeNormal = boardPlane.up;
        return true;
    }
}
