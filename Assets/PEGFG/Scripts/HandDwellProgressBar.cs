using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
// HandDwellProgressBar is a small UI utility that visualizes "progress toward confirmation".
//
// It began as a hand-dwell bar, but in the current project it serves two related purposes:
// - showing dwell progress while the participant holds a confirming gesture,
// - showing task-transition progress while the experiment waits before switching phase.
//
// The script is intentionally self-contained:
// if the background/fill graphics do not exist yet, it creates them automatically.
public class HandDwellProgressBar : MonoBehaviour
{
    [SerializeField] private SandboxRunner runner;
    [SerializeField] private Vector2 barSize = new(320f, 22f);
    [SerializeField] private float fillPadding = 3f;
    [SerializeField] private float cornerRadius = 11f;
    [SerializeField] private bool autoHideWhenIdle = true;
    [SerializeField] private Color backgroundColor = new(0.13f, 0.18f, 0.23f, 0.95f);
    [SerializeField] private Color fillColor = new(0.28f, 0.77f, 0.74f, 1f);
    [SerializeField] private RoundedRectGraphic backgroundGraphic;
    [SerializeField] private RoundedRectGraphic fillGraphic;

    private RectTransform _rectTransform;
    private RectTransform _fillRect;
    private bool _layoutDirty = true;

    // Reset is called when the component is first added or manually reset in the editor.
    // The goal is to make the bar usable immediately without extra setup.
    void Reset()
    {
        AutoAssignRunner();
        EnsureVisualTree();
        MarkDirty();
    }

    // Standard startup hook in play mode.
    void Awake()
    {
        AutoAssignRunner();
        EnsureVisualTree();
        MarkDirty();
    }

    // Rebuild again when the object is enabled, for example after being hidden and shown.
    void OnEnable()
    {
        EnsureVisualTree();
        MarkDirty();
    }

    // Any inspector change should force the layout/colors to be recalculated.
    void OnValidate()
    {
        MarkDirty();
    }

    // Main update loop.
    //
    // Structural work only happens when layoutDirty is true.
    // Live progress refresh still happens every frame because the fill amount can change every frame.
    void Update()
    {
        if (_layoutDirty)
        {
            EnsureVisualTree();
            ApplyLayout();
            ApplyColors();
            _layoutDirty = false;
        }

        RefreshVisualState();
    }

    // Marks that one of the configurable values changed and the visual layout should be rebuilt.
    void MarkDirty()
    {
        _layoutDirty = true;
    }

    // If the runner reference was not assigned manually, find the active SandboxRunner in the scene.
    void AutoAssignRunner()
    {
        if (runner == null)
            runner = FindFirstObjectByType<SandboxRunner>();
    }

    // Ensures the expected UI hierarchy exists:
    // this object
    //   -> Background
    //        -> Fill
    //
    // This lets the bar generate its own visuals instead of relying on a hand-built prefab.
    void EnsureVisualTree()
    {
        _rectTransform = GetComponent<RectTransform>();

        if (backgroundGraphic == null)
        {
            Transform child = transform.Find("Background");
            if (child == null)
            {
                var go = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(RoundedRectGraphic));
                go.transform.SetParent(transform, false);
                child = go.transform;
            }

            backgroundGraphic = child.GetComponent<RoundedRectGraphic>();
            backgroundGraphic.raycastTarget = false;
        }

        if (fillGraphic == null)
        {
            Transform child = backgroundGraphic.transform.Find("Fill");
            if (child == null)
            {
                var go = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(RoundedRectGraphic));
                go.transform.SetParent(backgroundGraphic.transform, false);
                child = go.transform;
            }

            fillGraphic = child.GetComponent<RoundedRectGraphic>();
            fillGraphic.raycastTarget = false;
        }

        _fillRect = fillGraphic.rectTransform;
    }

    // Applies the static RectTransform layout for the outer bar and the inner fill.
    // The fill is anchored to the left so increasing progress simply increases its width.
    void ApplyLayout()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();

        _rectTransform.sizeDelta = barSize;

        if (backgroundGraphic != null)
        {
            var backgroundRect = backgroundGraphic.rectTransform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
        }

        if (_fillRect != null)
        {
            _fillRect.anchorMin = new Vector2(0f, 0f);
            _fillRect.anchorMax = new Vector2(0f, 1f);
            _fillRect.pivot = new Vector2(0f, 0.5f);
            _fillRect.anchoredPosition = Vector2.zero;
            _fillRect.offsetMin = new Vector2(0f, fillPadding);
            _fillRect.offsetMax = new Vector2(0f, -fillPadding);
        }
    }

    // Applies the current colors and rounded-corner settings.
    // The fill gets a slightly smaller radius so it still looks neatly inset after padding.
    void ApplyColors()
    {
        if (backgroundGraphic != null)
        {
            backgroundGraphic.color = backgroundColor;
            backgroundGraphic.CornerRadius = cornerRadius;
        }

        if (fillGraphic != null)
        {
            fillGraphic.color = fillColor;
            fillGraphic.CornerRadius = Mathf.Max(0f, cornerRadius - fillPadding);
        }
    }

    // Reads the relevant state from SandboxRunner and updates:
    // - whether the bar should be visible,
    // - how much of the bar should be filled.
    //
    // There are two display modes:
    // 1. transition mode, which uses TaskTransitionProgress01,
    // 2. confirmation mode, which uses ConfirmDwellProgress01.
    void RefreshVisualState()
    {
        AutoAssignRunner();
        EnsureVisualTree();

        float progress = 0f;
        bool shouldShow = !autoHideWhenIdle;

        if (runner != null)
        {
            if (runner.IsTaskTransitionActive)
            {
                // During task transitions, reuse the same bar as a loading indicator.
                progress = Mathf.Clamp01(runner.TaskTransitionProgress01);
                shouldShow = true;
            }
            else
            {
                // Otherwise, show dwell/confirm buildup if such a confirmation is active.
                progress = Mathf.Clamp01(runner.ConfirmDwellProgress01);
                shouldShow |= runner.IsConfirmDwellActive || progress > 0f;
            }
        }

        if (backgroundGraphic != null)
            backgroundGraphic.enabled = shouldShow;

        if (fillGraphic != null)
            fillGraphic.enabled = shouldShow;

        if (_fillRect == null)
            _fillRect = fillGraphic != null ? fillGraphic.rectTransform : null;

        if (_fillRect == null)
            return;

        // The bar grows horizontally from left to right.
        // Height stays constant; only width depends on progress.
        float height = Mathf.Max(0f, barSize.y - (fillPadding * 2f));
        float width = Mathf.Max(0f, barSize.x * progress);

        _fillRect.sizeDelta = new Vector2(width, height);
    }
}
