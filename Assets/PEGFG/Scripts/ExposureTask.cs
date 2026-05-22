using UnityEngine;
using TMPro;
using UnityEngine.Serialization;

// ExposureTask implements the adaptation-inducing part of the workflow.
//
// This task differs fundamentally from the measurement tasks:
// - the participant does not reset fully between every response,
// - one of three targets is repeatedly acquired,
// - hit/miss feedback is part of the intended design,
// - and the currently selected perturbation is active during this phase.
//
// In other words, this is the task meant to drive adaptation, not merely measure it.
//
// The script is responsible for:
// - spawning/selecting the active exposure target,
// - deciding whether a response counts as hit or miss,
// - tracking successful hits and total attempts,
// - updating target visuals and bullseye cueing,
// - logging exposure attempts and configuration,
// - notifying SandboxRunner once exposure is complete.
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
    private Transform centerBullseyeRing;
    private Transform centerBullseyeInnerRing;
    private Transform centerBullseyeDot;

    [Header("Exposure Block")]
    [FormerlySerializedAs("successfulHitsToComplete")]
    [Tooltip("Number of total attempts required before exposure is complete. Both hits and misses count toward this total.")]
    public int attemptsToComplete = 90;

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

    [Header("Bullseye target cue")]
    [SerializeField] private bool showCenterBullseye = true;
    [SerializeField] private Color bullseyeRingColor = Color.black;
    [SerializeField] private Color bullseyeInnerRingColor = Color.black;
    [SerializeField] private Color bullseyeDotColor = Color.white;
    [SerializeField, Range(0.1f, 1f)] private float bullseyeRingScale = 0.62f;
    [SerializeField, Range(0.05f, 1f)] private float bullseyeInnerRingScale = 0.30f;
    [SerializeField, Range(0.05f, 1f)] private float bullseyeDotScale = 0.24f;
    [SerializeField] private float bullseyeRingSurfaceOffsetMeters = 0.0015f;
    [SerializeField] private float bullseyeInnerRingSurfaceOffsetMeters = 0.0022f;
    [SerializeField] private float bullseyeDotSurfaceOffsetMeters = 0.0030f;

    [Header("Debug")]
    public bool logAttempts = true;

    bool _isActive;
    bool _confirmLatched = false;
    bool _armed = true;
    float _lastAcceptedTime = -999f;
    float? _liveDistanceCm = null;
    float? _lastAttemptDistanceCm = null;
    bool? _lastAttemptWasHit = null;
    float _configuredHitRadiusMeters = -1f;

    int _successCount = 0;
    int _attemptCount = 0;
    int _currentTargetIndex = 1;
    int _lastTargetIndex = -1;

    public SandboxRunner.TaskMode TaskMode => SandboxRunner.TaskMode.Exposure;
    public int GetCurrentAttemptNumber() => _attemptCount + 1;
    public int GetCurrentTargetIndex() => _currentTargetIndex;

    // Editor reset hook.
    void Reset()
    {
        AutoAssignReferences();
    }

    // Keep references updated in the editor.
    // Avoid mesh/material reassignment here because Unity does not allow those
    // changes during OnValidate/CheckConsistency.
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            AutoAssignReferences();
            AutoFillRenderers();
        }
    }

    // Called by SandboxRunner when Exposure becomes active or inactive.
    public void SetTaskActive(bool active)
    {
        _isActive = active;
        UpdateVisualsFromState();

        if (_isActive)
            RefreshTargetVisuals();
    }

    // Standard startup hook.
    void Awake()
    {
        AutoAssignReferences();
        AutoFillRenderers();
        EnsureCenterTargetBullseye();
    }

    // Ensure the visual state matches the current active/inactive state once play starts.
    void Start()
    {
        UpdateVisualsFromState();
    }

    // Small wrapper used to synchronize target/cursor/hit-marker visibility with task activity.
    void UpdateVisualsFromState()
    {
        SetVisualsVisible(_isActive);
    }

    // Auto-finds the scene objects this task depends on:
    // runner, board, HMD, three exposure targets, markers, readout, and logger.
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

    // Starts a fresh exposure block.
    //
    // This resets the participant-facing and logging-facing state:
    // - hit counter,
    // - attempt counter,
    // - gate/latch state,
    // - current live and last-attempt readouts,
    // - configured hit radius for the whole block.
    //
    // It then activates visuals, logs the block configuration, chooses the first target,
    // and updates the readout.
    public void StartExposureBlock()
    {
        _successCount = 0;
        _attemptCount = 0;
        _confirmLatched = false;
        _armed = true;
        _lastAcceptedTime = -999f;
        _liveDistanceCm = null;
        _lastAttemptDistanceCm = null;
        _lastAttemptWasHit = null;
        _configuredHitRadiusMeters = ResolveConfiguredHitRadiusMeters();

        AutoFillRenderers();
        EnsureCenterTargetBullseye();
        SetVisualsVisible(true);
        experimentLogger?.LogExposureConfig(_configuredHitRadiusMeters);
        if (hitMarker != null)
            hitMarker.gameObject.SetActive(showHitMarker);
        PickNextTarget(forceCenterFirst: true);
        if (targets[_currentTargetIndex] != null)
            experimentLogger?.LogExposureTargetSpawned(_currentTargetIndex, targets[_currentTargetIndex].position);
        RefreshTargetVisuals();
        UpdateReadout();
    }

    // Main exposure loop.
    //
    // Chronological flow of one attempt:
    // 1. make sure the task is active and references are valid,
    // 2. update the bullseye cue visuals,
    // 3. get the participant's transformed input,
    // 4. apply confirm-latching and optional reset gating,
    // 5. intersect the aim ray with the board,
    // 6. compute distance from the active target,
    // 7. if confirmed, judge hit or miss,
    // 8. log the attempt,
    // 9. if hit, advance toward completion and choose the next target,
    // 10. if enough hits were made, notify SandboxRunner that exposure is complete.
void Update()
{
    // If exposure is not the current task, do nothing.
    if (!_isActive) return;

    // Exposure requires the runner, board, and target array to function.
    if (runner == null || boardPlane == null || targets == null || targets.Length < 3) return;

    UpdateCenterBullseyeVisuals();

    // SandboxRunner already resolves controller vs hand input and any active effect.
    var (ray, pose, confirm) = runner.GetTransformedInput();

    if (latchConfirm)
    {
        // Prevent a single held confirmation from being counted more than once.
        if (!confirm) _confirmLatched = false;
        if (confirm && _confirmLatched) confirm = false;
    }

    if (requireResetBetweenTrials && hmd != null)
    {
        // Optional return-to-start gating, though exposure usually runs more continuously
        // than the measurement tasks.
        float resetY = hmd.position.y - resetDropMeters;

        if (!_armed && pose.position.y <= resetY)
            _armed = true;

        if (confirm && !_armed)
            confirm = false;
    }

    // If the aim ray does not hit the board, there is no valid live cursor position.
    if (!IntersectRayWithBoard(ray, out Vector3 hitPoint))
    {
        _liveDistanceCm = null;
        UpdateReadout();
        return;
    }

    if (cursorMarker && showCursor)
        cursorMarker.position = hitPoint;

    Transform currentTarget = targets[_currentTargetIndex];

    // Live distance is shown in centimeters because that is easier to read in the task UI.
    if (currentTarget != null)
        _liveDistanceCm = Vector3.Distance(hitPoint, currentTarget.position) * 100f;
    else
        _liveDistanceCm = null;

    if (confirm)
    {
        // Always keep a short minimum gap between accepted attempts.
        // Exposure usually runs without full reset gating, but we still do not want
        // two near-simultaneous confirm edges to count as two separate attempts.
        if (Time.time - _lastAcceptedTime < minSecondsBetweenAttempts)
        {
            UpdateReadout();
            return;
        }

        _lastAcceptedTime = Time.time;

        if (requireResetBetweenTrials)
        {
            _armed = false;
        }

        if (latchConfirm)
            _confirmLatched = true;

        _attemptCount++;

        var target = targets[_currentTargetIndex];

        // The block uses one resolved hit radius consistently across attempts.
        float currentHitRadius = _configuredHitRadiusMeters;
        float dist = Vector3.Distance(hitPoint, target.position);
        bool isHit = dist <= currentHitRadius;

        _lastAttemptDistanceCm = dist * 100f;
        _lastAttemptWasHit = isHit;

        // Two logging calls are used here:
        // one for the raw pointer shot event, one for the interpreted exposure attempt.
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
            // Hit marker shows where the accepted response landed on the board.
            hitMarker.gameObject.SetActive(true);
            hitMarker.position = hitPoint;
        }

        if (isHit)
        {
            // Hits still matter analytically, but completion is now based on total
            // attempts so participants who miss often do not get trapped in an
            // overly long exposure block.
            _successCount++;

            if (logAttempts)
                Debug.Log($"[Exposure] HIT success={_successCount}, attempt={_attemptCount}/{attemptsToComplete} on target {_currentTargetIndex} (dist={dist:0.000}m)");

            FlashSingleTarget(_currentTargetIndex, hitColor);
        }
        else
        {
            // Misses are still counted as attempts for block completion.
            if (logAttempts)
                Debug.Log($"[Exposure] MISS attempt={_attemptCount}/{attemptsToComplete} on target {_currentTargetIndex} (dist={dist:0.000}m)");

            FlashSingleTarget(_currentTargetIndex, missColor);
        }

        if (_attemptCount >= attemptsToComplete)
        {
            // Exposure is complete once the configured number of total attempts
            // has been reached, regardless of hit/miss ratio.
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

        // Play sound LAST, after the accepted attempt has fully completed.
        runner?.PlayAcceptedClickSound();
    }

    UpdateReadout();
}

    // Shows or hides the targets and marker visuals belonging to exposure.
    // Unlike the measurement tasks, the targets themselves are part of the core task display.
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

    // Tries to find the hit marker inside WorldRoot first, then falls back to a global search.
    // This is more robust to different scene layouts.
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

    // Ensures the renderer array matches the target array, auto-filling renderers from target children.
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

    // Chooses the next active target.
    //
    // Rules:
    // - the very first target can be forced to center,
    // - otherwise targets can cycle deterministically or be randomized,
    // - optional immediate-repeat avoidance prevents the same target from being selected twice in a row.
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

    // Colors the targets so the current one is visually distinguished from the idle ones.
    void RefreshTargetVisuals()
    {
        if (!_isActive) return;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null) continue;
            SetRendererColor(targetRenderers[i], i == _currentTargetIndex ? activeColor : idleColor);
        }

        UpdateCenterBullseyeVisuals();
    }

    // Temporary color flash for a single target to indicate hit or miss outcome.
    void FlashSingleTarget(int index, Color color)
    {
        if (index < 0 || index >= targetRenderers.Length) return;
        if (targetRenderers[index] == null) return;

        SetRendererColor(targetRenderers[index], color);
    }

    // Uses a MaterialPropertyBlock so target colors can be changed without permanently modifying materials.
    void SetRendererColor(Renderer r, Color c)
    {
        if (r == null) return;
        var props = new MaterialPropertyBlock();
        r.GetPropertyBlock(props);
        props.SetColor("_Color", c);
        if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor"))
            props.SetColor("_BaseColor", c);
        r.SetPropertyBlock(props);
    }

    // Ensures the currently active target can receive the bullseye cue overlays used to make it visually distinctive.
    void EnsureCenterTargetBullseye()
    {
        if (!showCenterBullseye || targets == null || targets.Length == 0)
            return;

        Renderer sourceRenderer = GetBullseyeSourceRenderer();
        if (sourceRenderer == null)
            return;

        if (centerBullseyeRing == null)
            centerBullseyeRing = CreateBullseyeOverlay("CenterBullseyeRing", sourceRenderer, bullseyeRingScale);

        if (centerBullseyeInnerRing == null)
            centerBullseyeInnerRing = CreateBullseyeOverlay("CenterBullseyeInnerRing", sourceRenderer, bullseyeInnerRingScale);

        if (centerBullseyeDot == null)
            centerBullseyeDot = CreateBullseyeOverlay("CenterBullseyeDot", sourceRenderer, bullseyeDotScale);

        UpdateCenterBullseyeVisuals();
    }

    // Creates one overlay object for the bullseye cue.
    // The overlay reuses the target's mesh and materials, then relies on color/scale/offset differences
    // to produce the bullseye appearance.
    Transform CreateBullseyeOverlay(string objectName, Renderer sourceRenderer, float scaleMultiplier)
    {
        Transform existing = sourceRenderer.transform.Find(objectName);
        if (existing != null)
            return existing;

        GameObject overlay = new GameObject(objectName);
        overlay.transform.SetParent(sourceRenderer.transform, false);
        overlay.transform.localPosition = Vector3.zero;
        overlay.transform.localRotation = Quaternion.identity;
        overlay.transform.localScale = Vector3.one * scaleMultiplier;

        MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
        if (sourceFilter != null && sourceFilter.sharedMesh != null)
        {
            MeshFilter meshFilter = overlay.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = sourceFilter.sharedMesh;
        }

        MeshRenderer meshRenderer = overlay.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
        return overlay.transform;
    }

    // Returns the renderer belonging to the currently selected target.
    Renderer GetBullseyeSourceRenderer()
    {
        if (targets == null || targets.Length == 0)
            return null;

        int targetIndex = Mathf.Clamp(_currentTargetIndex, 0, targets.Length - 1);
        Renderer sourceRenderer = targetRenderers != null && targetIndex < targetRenderers.Length
            ? targetRenderers[targetIndex]
            : null;

        if (sourceRenderer == null && targets[targetIndex] != null)
            sourceRenderer = targets[targetIndex].GetComponentInChildren<Renderer>();

        return sourceRenderer;
    }

    // Reattaches one bullseye overlay to the currently active target and synchronizes
    // its mesh/material references with that target's renderer.
    void SyncBullseyeOverlay(Transform overlay, Renderer sourceRenderer)
    {
        if (overlay == null || sourceRenderer == null)
            return;

        if (overlay.parent != sourceRenderer.transform)
            overlay.SetParent(sourceRenderer.transform, false);

        overlay.localPosition = Vector3.zero;
        overlay.localRotation = Quaternion.identity;

        MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
        MeshFilter overlayFilter = overlay.GetComponent<MeshFilter>();
        if (overlayFilter != null)
            overlayFilter.sharedMesh = sourceFilter != null ? sourceFilter.sharedMesh : null;

        MeshRenderer overlayRenderer = overlay.GetComponent<MeshRenderer>();
        if (overlayRenderer != null)
            overlayRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
    }

    // Keeps the bullseye visuals synchronized with the current inspector settings and the current target.
    // This updates visibility, parent target, local offsets, scales, rotations, and colors.
    void UpdateCenterBullseyeVisuals()
    {
        if (centerBullseyeRing == null || centerBullseyeInnerRing == null || centerBullseyeDot == null)
            return;

        Renderer sourceRenderer = GetBullseyeSourceRenderer();
        bool overlaysVisible = showCenterBullseye && (!Application.isPlaying || _isActive) && sourceRenderer != null;
        centerBullseyeRing.gameObject.SetActive(overlaysVisible);
        centerBullseyeInnerRing.gameObject.SetActive(overlaysVisible);
        centerBullseyeDot.gameObject.SetActive(overlaysVisible);

        if (sourceRenderer == null)
            return;

        SyncBullseyeOverlay(centerBullseyeRing, sourceRenderer);
        SyncBullseyeOverlay(centerBullseyeInnerRing, sourceRenderer);
        SyncBullseyeOverlay(centerBullseyeDot, sourceRenderer);

        centerBullseyeRing.localPosition = Vector3.up * bullseyeRingSurfaceOffsetMeters;
        centerBullseyeInnerRing.localPosition = Vector3.up * bullseyeInnerRingSurfaceOffsetMeters;
        centerBullseyeDot.localPosition = Vector3.up * bullseyeDotSurfaceOffsetMeters;
        centerBullseyeRing.localRotation = Quaternion.identity;
        centerBullseyeInnerRing.localRotation = Quaternion.identity;
        centerBullseyeDot.localRotation = Quaternion.identity;
        centerBullseyeRing.localScale = Vector3.one * bullseyeRingScale;
        centerBullseyeInnerRing.localScale = Vector3.one * bullseyeInnerRingScale;
        centerBullseyeDot.localScale = Vector3.one * bullseyeDotScale;

        SetRendererColor(centerBullseyeRing.GetComponent<Renderer>(), bullseyeRingColor);
        SetRendererColor(centerBullseyeInnerRing.GetComponent<Renderer>(), bullseyeInnerRingColor);
        SetRendererColor(centerBullseyeDot.GetComponent<Renderer>(), bullseyeDotColor);
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

    // Updates the participant-facing readout for the exposure task.
    // Exposure emphasizes current target, hit progress, attempt count, live distance, and last outcome.
    void UpdateReadout()
    {
        if (!readout || !_isActive) return;

        string gateTxt = (requireResetBetweenTrials && hmd != null) ? (_armed ? "ARMED" : "RESET") : "—";
        string targetName = TargetLabel(_currentTargetIndex);
        string liveTxt = _liveDistanceCm.HasValue ? $"{_liveDistanceCm.Value:0.0} cm" : "—";
        string lastTxt = "—";

        if (_lastAttemptDistanceCm.HasValue && _lastAttemptWasHit.HasValue)
        {
            string outcome = _lastAttemptWasHit.Value ? "Hit" : "Miss";
            lastTxt = $"{outcome}: {_lastAttemptDistanceCm.Value:0.0} cm";
        }

        readout.text =
            $"Exposure\n" +
            $"Effect: {runner.CurrentEffectMode}\n" +
            $"Target: {targetName}\n" +
            $"Hits: {_successCount}\n" +
            $"Attempts: {_attemptCount}/{attemptsToComplete}\n" +
            $"Aim: {liveTxt}\n" +
            $"Last: {lastTxt}\n" +
            $"Gate: {gateTxt}";
    }

    // Converts the internal numeric target index into a human-readable label.
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

    // Exposes the currently active exposure target to SandboxRunner.
    // This is used for controller hover-confirm and trajectory/phase interpretation.
    public bool TryGetAimTarget(out Vector3 worldCenter, out Vector3 planeNormal, out float targetRadiusMeters)
    {
        worldCenter = Vector3.zero;
        planeNormal = Vector3.up;
        targetRadiusMeters = _configuredHitRadiusMeters > 0f ? _configuredHitRadiusMeters : ResolveConfiguredHitRadiusMeters();

        if (!_isActive || boardPlane == null || targets == null || _currentTargetIndex < 0 || _currentTargetIndex >= targets.Length)
            return false;

        Transform target = targets[_currentTargetIndex];
        if (target == null)
            return false;

        worldCenter = target.position;
        planeNormal = boardPlane.up;
        return true;
    }

    // Resolves the hit radius that should be used for the whole exposure block.
    // The current implementation prefers the center target, then falls back to any valid target.
    float ResolveConfiguredHitRadiusMeters()
    {
        if (targets != null && targets.Length > 1 && targets[1] != null)
            return EstimateTargetRadiusMeters(targets[1]);

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
                return EstimateTargetRadiusMeters(targets[i]);
        }

        return hitRadiusMeters;
    }

    // Estimates target radius from the target object's world-space scale.
    // If that estimate fails, it falls back to the configured default hitRadiusMeters.
    float EstimateTargetRadiusMeters(Transform target)
    {
        if (target == null)
            return hitRadiusMeters;

        Vector3 lossy = target.lossyScale;
        float estimated = 0.5f * Mathf.Max(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y), Mathf.Abs(lossy.z));
        return estimated > 0.0001f ? estimated : hitRadiusMeters;
    }
}
