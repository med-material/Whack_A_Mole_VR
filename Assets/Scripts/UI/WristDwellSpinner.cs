using UnityEngine;
using UnityEngine.UI;

/*
WristDwellSpinner
Displays a circular progress indicator above the wrist to show dwell time progress.
This spinner remains visible even when the mole is occluded by the virtual hand,
ensuring the user always has visual feedback of their progress towards popping a mole.

Usage:
- Attach to a GameObject parented to the virtual hand
- Position above the wrist area
- Call UpdateProgress() to sync with mole dwell time
- Call Show()/Hide() to control visibility
*/
public class WristDwellSpinner : MonoBehaviour
{
    [Header("Spinner Visual Settings")]
    [SerializeField]
    [Tooltip("The UI Image component that displays the radial fill")]
    private Image spinnerImage;

    [SerializeField]
    [Tooltip("Color when hovering over a valid target")]
    private Color validTargetColor = new Color(0.2f, 0.8f, 0.2f, 0.9f);

    [SerializeField]
    [Tooltip("Color when hovering over a distractor/invalid target")]
    private Color invalidTargetColor = new Color(0.8f, 0.2f, 0.2f, 0.9f);

    [SerializeField]
    [Tooltip("Color when not hovering over anything")]
    private Color idleColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

    [Header("Background Settings")]
    [SerializeField]
    [Tooltip("The UI Image component for the background (optional)")]
    private Image backgroundImage;

    [SerializeField]
    [Tooltip("Show background when hovering over any mole")]
    private bool showBackgroundOnHover = true;

    [SerializeField]
    [Tooltip("Background color (dark grey with transparency)")]
    private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);

    [SerializeField]
    [Tooltip("Background sprite name to search for in Resources")]
    private string backgroundSpriteName = "Background";

    [Header("Behavior Settings")]
    [SerializeField]
    [Tooltip("Should the spinner rotate as it fills?")]
    private bool rotateWhileFilling = true;

    [SerializeField]
    [Tooltip("Rotation speed in degrees per second")]
    private float rotationSpeed = 90f;

    [SerializeField]
    [Tooltip("Scale multiplier when active (pulse effect)")]
    private float activeScaleMultiplier = 1.1f;

    [SerializeField]
    [Tooltip("Speed of scale animation")]
    private float scaleAnimationSpeed = 5f;

    [Header("Layout Settings")]
    [SerializeField]
    [Tooltip("Offset from the wrist position (local space)")]
    private Vector3 wristOffset = new Vector3(0f, 0.05f, 0.02f);

    [SerializeField]
    [Tooltip("Should the spinner always face the camera?")]
    private bool faceCamera = true;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private float targetScale = 1f;
    private Transform cameraTransform;
    private bool isActive = false;

    void Awake()
    {
        // Get or add required components
        if (spinnerImage == null)
        {
            spinnerImage = GetComponent<Image>();
            if (spinnerImage == null)
            {
                Debug.LogError("WristDwellSpinner: No Image component found! Please assign spinnerImage in the inspector.");
                enabled = false;
                return;
            }
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }

        // Setup image for radial fill
        spinnerImage.type = Image.Type.Filled;
        spinnerImage.fillMethod = Image.FillMethod.Radial360;
        spinnerImage.fillOrigin = (int)Image.Origin360.Bottom;
        spinnerImage.fillClockwise = true;

        originalScale = transform.localScale;

        // Apply wrist offset - try both methods for compatibility
        // For World Space Canvas, use regular transform
        transform.localPosition = wristOffset;
        // For Screen/Overlay Canvas, use RectTransform
        rectTransform.anchoredPosition3D = wristOffset;
        

        // Setup background if it exists
        SetupBackground();

        // Initialize hidden
        Hide();
    }

    void Start()
    {
        // Find main camera
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
        }
        else
        {
            Debug.LogWarning("WristDwellSpinner: Main camera not found. Billboard effect disabled.");
            faceCamera = false;
        }
    }

    void Update()
    {
        if (!isActive) return;

        // Rotate the spinner if enabled
        if (rotateWhileFilling && spinnerImage.fillAmount > 0f)
        {
            rectTransform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }

        // Animate scale (pulse effect)
        float currentScale = Mathf.Lerp(transform.localScale.x / originalScale.x, targetScale, Time.deltaTime * scaleAnimationSpeed);
        transform.localScale = originalScale * currentScale;

        // Billboard effect - face camera
        if (faceCamera && cameraTransform != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - cameraTransform.position);
        }
    }

    private void SetupBackground()
    {
        // Auto-detect background image if not assigned
        if (backgroundImage == null)
        {
            Transform bgTransform = transform.Find("Background");
            if (bgTransform != null)
            {
                backgroundImage = bgTransform.GetComponent<Image>();
            }
        }

        // If background image is assigned, configure it
        if (backgroundImage != null)
        {
            // Try to load the Background sprite if not already assigned
            if (backgroundImage.sprite == null)
            {
                Sprite bgSprite = Resources.Load<Sprite>(backgroundSpriteName);
                if (bgSprite == null)
                {
                    // Try to find it in the current scene or project
                    Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                    foreach (Sprite sprite in allSprites)
                    {
                        if (sprite.name == backgroundSpriteName)
                        {
                            bgSprite = sprite;
                            break;
                        }
                    }
                }

                if (bgSprite != null)
                {
                    backgroundImage.sprite = bgSprite;
                }
            }

            // Configure background appearance
            backgroundImage.type = Image.Type.Tiled;
            backgroundImage.color = backgroundColor;

            // Ensure background is behind the spinner
            backgroundImage.transform.SetAsFirstSibling();

            // Start hidden (disable the entire GameObject)
            backgroundImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Updates the spinner's fill amount to match dwell time progress
    /// </summary>
    /// <param name="progress">Progress value from 0.0 to 1.0</param>
    public void UpdateProgress(float progress)
    {
        if (spinnerImage != null)
        {
            spinnerImage.fillAmount = Mathf.Clamp01(progress);
            
            // Pulse effect when filling
            targetScale = progress > 0f ? activeScaleMultiplier : 1f;

            // Hide background GameObject when fill reaches 1.0 (mole popped)
            if (progress >= 1f && backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Shows the spinner and sets its color based on target validity
    /// </summary>
    /// <param name="isValidTarget">True if hovering over valid target, false for distractor</param>
    public void Show(bool isValidTarget = true)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        if (spinnerImage != null)
        {
            spinnerImage.color = isValidTarget ? validTargetColor : invalidTargetColor;
        }

        // Show background GameObject if enabled
        if (showBackgroundOnHover && backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(true);
        }

        isActive = true;
    }

    /// <summary>
    /// Hides the spinner and resets progress
    /// </summary>
    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (spinnerImage != null)
        {
            spinnerImage.fillAmount = 0f;
            spinnerImage.color = idleColor;
        }

        // Hide background GameObject
        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(false);
        }

        targetScale = 1f;
        transform.localScale = originalScale;
        isActive = false;
    }

    /// <summary>
    /// Updates the color of the spinner
    /// </summary>
    public void SetColor(Color color)
    {
        if (spinnerImage != null)
        {
            spinnerImage.color = color;
        }
    }

    /// <summary>
    /// Sets whether the spinner should be visible
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (visible)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    /// <summary>
    /// Gets the current fill amount
    /// </summary>
    public float GetProgress()
    {
        return spinnerImage != null ? spinnerImage.fillAmount : 0f;
    }

    /// <summary>
    /// Sets the background image reference (can be used at runtime)
    /// </summary>
    public void SetBackgroundImage(Image background)
    {
        backgroundImage = background;
        SetupBackground();
    }

    /// <summary>
    /// Gets the background image reference
    /// </summary>
    public Image GetBackgroundImage()
    {
        return backgroundImage;
    }
}
