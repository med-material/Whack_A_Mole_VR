using System;
using System.IO;
using UnityEngine;

public class ScreenShot : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Select which display to capture (0 = primary monitor)")]
    private int displayIndex = 0;

    [SerializeField]
    [Tooltip("Base name for the screenshot files")]
    private string screenName = "Screenshot";

    [SerializeField]
    [Tooltip("Resolution multiplier for screenshot quality (higher = better quality but larger file)")]
    [Range(1, 16)]
    private int superSize = 16;

    [SerializeField]
    [Tooltip("Use game view resolution instead of camera resolution")]
    private bool useGameViewResolution = false;

    private string screenshotDirectory = @"C:\Users\stud\Pictures\Whack-A-MoleScreenShots";

    void Start()
    {
        // Create the screenshot directory if it doesn't exist
        if (!Directory.Exists(screenshotDirectory))
        {
            Directory.CreateDirectory(screenshotDirectory);
            Debug.Log($"Created screenshot directory: {screenshotDirectory}");
        }

        // Log available displays
        LogAvailableDisplays();
    }

    void Update()
    {
        //Press W to take a Screen Capture
        if (Input.GetKeyDown(KeyCode.W))
        {
            CaptureScreenshot();
        }
    }

    private void CaptureScreenshot()
    {
        // Validate display index
        if (displayIndex < 0 || displayIndex >= Display.displays.Length)
        {
            Debug.LogError($"Invalid display index {displayIndex}. Available displays: 0-{Display.displays.Length - 1}");
            displayIndex = 0; // Fallback to primary
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string displayName = GetDisplayName(displayIndex);
        string filename = $"{screenName}_Display{displayIndex}_{displayName}_{superSize}x_{timestamp}.png";
        string fullPath = Path.Combine(screenshotDirectory, filename);

        // Use RenderTexture method for better control over resolution
        StartCoroutine(CaptureDisplayAsTexture(fullPath, displayIndex));
    }

    private System.Collections.IEnumerator CaptureDisplayAsTexture(string fullPath, int display)
    {
        // Wait for end of frame to ensure rendering is complete
        yield return new WaitForEndOfFrame();

        // Find camera rendering to this display
        Camera targetCamera = null;
        foreach (Camera cam in Camera.allCameras)
        {
            if (cam.targetDisplay == display && cam.enabled)
            {
                targetCamera = cam;
                break;
            }
        }

        if (targetCamera == null)
        {
            Debug.LogError($"No active camera found rendering to display {display}!");
            yield break;
        }

        // Calculate target resolution
        int width, height;
        
        if (useGameViewResolution)
        {
            // Use current game view resolution
            width = Screen.width * superSize;
            height = Screen.height * superSize;
        }
        else
        {
            // Use camera's actual rendering resolution (full display)
            if (display < Display.displays.Length)
            {
                width = Display.displays[display].renderingWidth * superSize;
                height = Display.displays[display].renderingHeight * superSize;
            }
            else
            {
                width = targetCamera.pixelWidth * superSize;
                height = targetCamera.pixelHeight * superSize;
            }
        }

        Debug.Log($"Capturing {width}x{height} (base: {width/superSize}x{height/superSize}, multiplier: {superSize}x)");

        // Create render texture with high quality settings
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing);
        
        // Store original camera settings
        RenderTexture originalRT = targetCamera.targetTexture;
        int originalPixelWidth = targetCamera.pixelWidth;
        int originalPixelHeight = targetCamera.pixelHeight;
        
        // Temporarily set camera to render to our high-res texture
        targetCamera.targetTexture = rt;
        targetCamera.Render();
        
        // Read pixels from the render texture
        RenderTexture.active = rt;
        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();
        
        // Save to file
        byte[] bytes = screenshot.EncodeToPNG();
        System.IO.File.WriteAllBytes(fullPath, bytes);
        
        // Calculate file size
        float fileSizeMB = bytes.Length / (1024f * 1024f);
        
        // Cleanup
        targetCamera.targetTexture = originalRT;
        RenderTexture.active = null;
        Destroy(rt);
        Destroy(screenshot);
        
        string displayName = GetDisplayName(display);
        Debug.Log($"✓ Screenshot Saved: {fullPath}");
        Debug.Log($"  Display: {display} ({displayName})");
        Debug.Log($"  Resolution: {width}x{height} ({superSize}x multiplier)");
        Debug.Log($"  File Size: {fileSizeMB:F2} MB");
    }

    private string GetDisplayName(int index)
    {
        if (index == 0)
        {
            return "Primary";
        }
        else if (index < Display.displays.Length)
        {
            Display display = Display.displays[index];
            return $"{display.renderingWidth}x{display.renderingHeight}";
        }
        return "Unknown";
    }

    private void LogAvailableDisplays()
    {
        Debug.Log($"=== Screenshot Configuration ===");
        Debug.Log($"Save Location: {screenshotDirectory}");
        Debug.Log($"Super Sampling: {superSize}x");
        Debug.Log($"Use Game View Resolution: {useGameViewResolution}");
        Debug.Log($"");
        Debug.Log($"=== Available Displays ({Display.displays.Length}) ===");
        
        for (int i = 0; i < Display.displays.Length; i++)
        {
            Display display = Display.displays[i];
            string status = display.active ? "Active" : "Inactive";
            
            // Find cameras rendering to this display
            int cameraCount = 0;
            string cameraNames = "";
            foreach (Camera cam in Camera.allCameras)
            {
                if (cam.targetDisplay == i && cam.enabled)
                {
                    cameraCount++;
                    cameraNames += (cameraNames.Length > 0 ? ", " : "") + cam.name;
                }
            }
            
            int expectedWidth = display.renderingWidth * superSize;
            int expectedHeight = display.renderingHeight * superSize;
            
            Debug.Log($"Display {i}: {display.renderingWidth}x{display.renderingHeight} - {status}");
            Debug.Log($"  → Cameras: {cameraCount} ({cameraNames})");
            Debug.Log($"  → Screenshot will be: {expectedWidth}x{expectedHeight}");
        }
        
        Debug.Log($"");
        Debug.Log($"Current selection: Display {displayIndex}");
        Debug.Log($"Press W in Play Mode to capture screenshot");
        Debug.Log("=====================================");
    }

    // Helper method to activate a display if needed
    [ContextMenu("Activate Selected Display")]
    public void ActivateSelectedDisplay()
    {
        if (displayIndex > 0 && displayIndex < Display.displays.Length)
        {
            if (!Display.displays[displayIndex].active)
            {
                Display.displays[displayIndex].Activate();
                Debug.Log($"✓ Activated Display {displayIndex}");
            }
            else
            {
                Debug.Log($"Display {displayIndex} is already active");
            }
        }
    }

    // Test capture with current settings
    [ContextMenu("Test Capture Now")]
    public void TestCaptureNow()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Must be in Play Mode to capture screenshots!");
            return;
        }
        CaptureScreenshot();
    }

    void OnValidate()
    {
        if (Display.displays != null && Display.displays.Length > 0 && displayIndex >= Display.displays.Length)
        {
            Debug.LogWarning($"Display index {displayIndex} out of range. Maximum: {Display.displays.Length - 1}");
        }
    }
}
