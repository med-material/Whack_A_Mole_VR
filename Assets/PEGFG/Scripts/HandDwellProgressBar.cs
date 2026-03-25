using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
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

    void Reset()
    {
        AutoAssignRunner();
        EnsureVisualTree();
        MarkDirty();
    }

    void Awake()
    {
        AutoAssignRunner();
        EnsureVisualTree();
        MarkDirty();
    }

    void OnEnable()
    {
        EnsureVisualTree();
        MarkDirty();
    }

    void OnValidate()
    {
        MarkDirty();
    }

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

    void MarkDirty()
    {
        _layoutDirty = true;
    }

    void AutoAssignRunner()
    {
        if (runner == null)
            runner = FindFirstObjectByType<SandboxRunner>();
    }

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
                progress = Mathf.Clamp01(runner.TaskTransitionProgress01);
                shouldShow = true;
            }
            else
            {
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

        float height = Mathf.Max(0f, barSize.y - (fillPadding * 2f));
        float width = Mathf.Max(0f, barSize.x * progress);

        _fillRect.sizeDelta = new Vector2(width, height);
    }
}
