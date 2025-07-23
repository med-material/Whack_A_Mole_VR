using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class LoggingExampleAdvanced
{

    private LoggingManager loggingManager;


    private int highscore = 42;
    private string player = "player1";
    private float soundvol = 100f;

    // Start is called before the first frame update
    void Start()
    {
        // Find the logging Manager in the scene.
        loggingManager = GameObject.Find("LoggingManager").GetComponent<LoggingManager>();


        // Tell the logging manager to create the log called MyLabel
        // For SQL connections, make sure that the label matches the label in your JSON file.
        loggingManager.CreateLog("MyLabel");


        // Tell the logging manager to store a piece of data into a column called "Highscore".
        loggingManager.Log("MyLabel", "Highscore", highscore);

        // You can also send a dictionary with multiple data entries at once.
        Dictionary<string, object> otherData = new Dictionary<string, object>() {
            {"SoundVolume", soundvol},
            {"PlayerName", player}
        };

        loggingManager.Log("MyLabel", otherData);


        // In each case, the logged line will be terminated, whether you have logged one or more data entries
        // This means that if you want to log 2 entries in one row, you must use the appropriate function

        // If enabled, you can also add data to the meta log
        loggingManager.Log("Meta", "age",19);


        // You can either set the SavePath in the editor or in the code
        // If you want to to this in code, use this function
        loggingManager.SetSavePath("/path/to/file.csv");

        // Same thing for the email
        loggingManager.SetEmail("example@create.aau.dk");


        // Tell the logging manager to save the data (to disk and SQL by default).
        // You can set the boolean to true if you want to clear the logs once saved
        loggingManager.SaveLog("MyLabel",clear:true);

        // You can specify to save only to disk or to SQL
        loggingManager.SaveLog("MyLabel",clear:true,TargetType.CSV);
        loggingManager.SaveLog("MyLabel", clear:true, TargetType.MySql);


        // Use this function to save all the logs at once
        // You can also choose to save only one target here 
        loggingManager.SaveAllLogs(clear:true);
        loggingManager.SaveAllLogs(clear:true,TargetType.MySql);


        // If you want to start new logs, you can ask loggingManager to generate
        // a new random unique id. The data saved will now have a new SessionID value.
        loggingManager.NewFilestamp();

    }

    // Update is called once per frame
    void Update()
    {

    }


}