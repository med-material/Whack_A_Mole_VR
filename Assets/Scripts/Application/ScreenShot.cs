using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class ScreenShot : MonoBehaviour
{
    [SerializeField]
    private string screenName;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //Press W to take a Screen Capture
        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), screenName + ".png"), 8);
            Debug.Log("Screenshot Captured");
        }
    }
}
