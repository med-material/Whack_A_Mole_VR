using UnityEngine;
using UnityEngine.SceneManagement;

/*
SetupSceneLoader
- Automatically loads the MainScene after setting up persistent objects.
- Place this on a GameObject in the Setup scene.
- Ensures all "keepBetweenScenes" objects are marked as DontDestroyOnLoad before transitioning.
Usage:
1. Create a "Setup" scene with all persistent objects (tagged "keepBetweenScenes").
2. Attach this script to any GameObject in the Setup scene.
3. Set mainSceneName to your main gameplay scene name.
4. The script will automatically transition after a brief delay.
*/
public class SetupSceneLoader : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Name of the main scene to load after setup")]
    private string mainSceneName = "MainScene";

    [SerializeField]
    [Tooltip("Delay before loading the main scene (seconds)")]
    private float loadDelay = 0.5f;

    void Start()
    {
   // Mark all persistent objects as DontDestroyOnLoad
        GameObject[] persistentObjects = GameObject.FindGameObjectsWithTag("keepBetweenScenes");
     
        foreach (GameObject obj in persistentObjects)
        {
            DontDestroyOnLoad(obj);
            Debug.Log($"[SetupSceneLoader] Marked '{obj.name}' as persistent.");
        }
        // Load the main scene after a short delay
        Invoke(nameof(LoadMainScene), loadDelay);
    }

    private void LoadMainScene()
    {
        Debug.Log($"[SetupSceneLoader] Loading main scene: {mainSceneName}");
        SceneManager.LoadScene(mainSceneName);
    }
}
