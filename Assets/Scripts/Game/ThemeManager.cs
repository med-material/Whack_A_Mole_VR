using UnityEngine;
using UnityEngine.SceneManagement;

public class ThemeManager : MonoBehaviour
{
    [SerializeField] private Theme themeName;
    [SerializeField] private bool change;

    private void Update()
    {
        if (change) // Check "change" in the inspector to change theme during runtime (Debug and test purposes)
        {
            LoadTheme(themeName);
            change = false;
        }
    }

    public void LoadTheme(Theme themeToLoad)
    {
        try
        {
            SceneManager.LoadScene(themeToLoad.ToString());
            Debug.Log("ThemeManager: Theme " + themeToLoad.ToString() + " loaded.");
            themeName = themeToLoad;
        }
        catch (System.Exception)
        {
            Debug.LogError("ThemeManager: Theme " + themeToLoad.ToString() + " not found. Make sure to add the corresponding scene to Build Settings.");
        }
    }
}
public enum Theme // Add new themes here and create corresponding scenes (Make sure to add them to Build Settings)
{
    Test1, // Temporary theme for testing
    MainScene
}
