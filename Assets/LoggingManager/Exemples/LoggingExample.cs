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

        // Start by telling logging manager to create a new collection og logs
        // and optionally pass the column headers.
        // Column headers can also be added dynamically, but declaring headers
        // from the beginning gives best performance.
        loggingManager.CreateLog("MyLabel", headers: new List<string>() { "Highscore", "SoundVolume","PlayerName"});

        // Tell the logging manager to store a piece of data into a column called "highscore".
        loggingManager.Log("MyLabel", "Highscore", highscore);

        // You can also send a dictionary with multiple data entries at once.
        Dictionary<string, object> otherData = new Dictionary<string, object>() {
            {"SoundVolume", soundvol},
            {"PlayerName", player}
        };

        loggingManager.Log("MyLabel", otherData);

        // Tell the logging manager to save the data (to disk and SQL by default).
        // Saving the data is an asynchronous process. 
        // If you wish to clear the logs after saving, specify clear:true.
        loggingManager.SaveAllLogs(clear:true);

        // If you want to start a new file, you can ask loggingManager to generate
        // a new file timestamp. Saving data hereafter will go to the new file.
        loggingManager.NewFilestamp();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
