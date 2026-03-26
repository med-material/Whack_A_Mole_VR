using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;

public class vivetracking : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionProperty positionAction;
    public InputActionProperty rotationAction;

    [Header("Logging")]
    public bool logToConsole = true;
    public bool logToFile = true;
    public float logInterval = 0.1f; // seconds between logs

    private string logFilePath;
    private float timer;

    void Start()
    {
        logFilePath = Path.Combine(Application.persistentDataPath, "vive_tracking_log.txt");
        Debug.Log("Logging to: " + logFilePath);

        if (logToFile)
            File.WriteAllText(logFilePath, "Timestamp, PosX, PosY, PosZ, RotX, RotY, RotZ, RotW\n");

        positionAction.action.Enable();
        rotationAction.action.Enable();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < logInterval) return;
        timer = 0f;

        Vector3 position = positionAction.action.ReadValue<Vector3>();
        Quaternion rotation = rotationAction.action.ReadValue<Quaternion>();

        string line = $"{Time.time:F3}, {position.x:F4}, {position.y:F4}, {position.z:F4}, " +
                      $"{rotation.x:F4}, {rotation.y:F4}, {rotation.z:F4}, {rotation.w:F4}";

        if (logToConsole)
            Debug.Log("Tracker | " + line);

        if (logToFile)
            File.AppendAllText(logFilePath, line + "\n");
    }

    void OnDisable()
    {
        positionAction.action.Disable();
        rotationAction.action.Disable();
    }
}
