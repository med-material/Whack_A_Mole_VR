using UnityEngine;
using UnityEngine.UI;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;

/*
WristSpinnerSetupHelper
Unity Editor utility to automatically create and configure a Wrist Dwell Spinner
on a virtual hand prefab.

Usage:
1. Select the Virtual Hand GameObject in your scene or prefab
2. Go to GameObject > UI > Create Wrist Dwell Spinner
3. The spinner will be automatically created and configured
*/

public class WristSpinnerSetupHelper : MonoBehaviour
{
    [MenuItem("GameObject/UI/Create Wrist Dwell Spinner", false, 10)]
    static void CreateWristSpinner()
    {
        GameObject selected = Selection.activeGameObject;
        
        if (selected == null)
        {
            EditorUtility.DisplayDialog("No GameObject Selected", 
                "Please select the Virtual Hand GameObject first.", "OK");
            return;
        }

        // Create Canvas
        GameObject canvasObj = new GameObject("WristCanvas");
        canvasObj.transform.SetParent(selected.transform, false);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        
        GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
        
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200, 200);
        canvasRect.localPosition = new Vector3(0f, 0.05f, 0.02f);
        canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        
        // Create Spinner Image (this will be the parent container)
        GameObject spinnerObj = new GameObject("WristSpinner");
        spinnerObj.transform.SetParent(canvasObj.transform, false);
        
        RectTransform spinnerRect = spinnerObj.GetComponent<RectTransform>();
        if (spinnerRect == null)
        {
            spinnerRect = spinnerObj.AddComponent<RectTransform>();
        }
        spinnerRect.sizeDelta = new Vector2(120, 120); // Slightly larger to contain both elements
        spinnerRect.anchorMin = new Vector2(0.5f, 0.5f);
        spinnerRect.anchorMax = new Vector2(0.5f, 0.5f);
        spinnerRect.pivot = new Vector2(0.5f, 0.5f);
        spinnerRect.anchoredPosition = Vector2.zero;
        
        // Create Background Image (behind)
        GameObject backgroundObj = new GameObject("Background");
        backgroundObj.transform.SetParent(spinnerObj.transform, false);
        
        Image backgroundImage = backgroundObj.AddComponent<Image>();
        
        // Try to find the "Background" sprite
        Sprite backgroundSprite = FindSpriteByName("Background");
        if (backgroundSprite != null)
        {
            backgroundImage.sprite = backgroundSprite;
        }
        else
        {
            // Use default Unity sprite if Background not found
            backgroundImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            Debug.LogWarning("Could not find sprite named 'Background'. Using Unity default. Please assign the correct sprite in the Inspector.");
        }
        
        backgroundImage.type = Image.Type.Tiled;
        backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.7f); // Dark grey with transparency
        
        RectTransform backgroundRect = backgroundObj.GetComponent<RectTransform>();
        backgroundRect.sizeDelta = new Vector2(120, 120);
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.anchoredPosition = Vector2.zero;
        
        // Create Spinner Fill Image (in front)
        GameObject spinnerFillObj = new GameObject("SpinnerFill");
        spinnerFillObj.transform.SetParent(spinnerObj.transform, false);
        
        Image spinnerImage = spinnerFillObj.AddComponent<Image>();
        // Try to find a default circular sprite
        Sprite circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        if (circleSprite != null)
        {
            spinnerImage.sprite = circleSprite;
        }
        spinnerImage.color = Color.white;
        
        RectTransform spinnerFillRect = spinnerFillObj.GetComponent<RectTransform>();
        spinnerFillRect.sizeDelta = new Vector2(100, 100);
        spinnerFillRect.anchorMin = new Vector2(0.5f, 0.5f);
        spinnerFillRect.anchorMax = new Vector2(0.5f, 0.5f);
        spinnerFillRect.pivot = new Vector2(0.5f, 0.5f);
        spinnerFillRect.anchoredPosition = Vector2.zero;
        
        // Add WristDwellSpinner component to the parent
        WristDwellSpinner spinner = spinnerObj.AddComponent<WristDwellSpinner>();
        
        // Use reflection to set private serialized fields
        var imageField = typeof(WristDwellSpinner).GetField("spinnerImage", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (imageField != null)
        {
            imageField.SetValue(spinner, spinnerImage);
        }
        
        var bgImageField = typeof(WristDwellSpinner).GetField("backgroundImage", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (bgImageField != null)
        {
            bgImageField.SetValue(spinner, backgroundImage);
        }
        
        // Add CanvasGroup
        CanvasGroup canvasGroup = spinnerObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f; // Start hidden
        
        // Select the new spinner
        Selection.activeGameObject = spinnerObj;
        
        Debug.Log("Wrist Dwell Spinner with background created successfully!");
        EditorUtility.DisplayDialog("Success", 
            "Wrist Dwell Spinner created with background!\n\n" +
            "The background will appear when hovering over any mole.\n\n" +
            "Next steps:\n" +
            "1. Configure spinner settings in the Inspector\n" +
            "2. Assign the 'Background' sprite if not found automatically\n" +
            "3. Set the reference in EMGPointer component\n" +
            "4. Test in Play mode", "OK");
    }
    
    static Sprite FindSpriteByName(string spriteName)
    {
        // Search in Resources folders
        Sprite sprite = Resources.Load<Sprite>(spriteName);
        if (sprite != null) return sprite;
        
        // Search all assets
        string[] guids = AssetDatabase.FindAssets($"t:Sprite {spriteName}");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            foreach (Sprite s in sprites)
            {
                if (s.name == spriteName)
                {
                    return s;
                }
            }
        }
        
        return null;
    }
    
    [MenuItem("GameObject/UI/Create Wrist Dwell Spinner", true)]
    static bool ValidateCreateWristSpinner()
    {
        return Selection.activeGameObject != null;
    }
}
#endif
