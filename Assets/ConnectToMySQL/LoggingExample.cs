using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoggingExample : MonoBehaviour
{

    private LoggingManager loggingManager;

    private int highscore = 42;
    private string player = "player1";
    private float soundvol = 100f;

    // Start is called before the first frame update
    void Start()
    {
        // Find the logging Manager in the scene.
        loggingManager = GameObject.Find("Logging").GetComponent<LoggingManager>();

        // Tell the logging manager to store a piece of data into a column called "CoolData".
        // For SQL connections, make sure that the label matches the label in your JSON file.
        loggingManager.Log("MyLabel", "Highscore", highscore);

        // You can also send a dictionary with multiple data entries at once.
        Dictionary<string, object> otherData = new Dictionary<string, object>() {
            {"SoundVolume", soundvol},
            {"PlayerName", player}
        };

        loggingManager.Log("MyLabel", otherData);

        // Tell the logging manager to save the data (to disk and SQL by default).
        loggingManager.SaveLog("MyLabel");

        // After saving the data, you can tell the logging manager to clear its logs.
        // Now its ready to save more data.
        loggingManager.ClearLog("MyLabel");
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
