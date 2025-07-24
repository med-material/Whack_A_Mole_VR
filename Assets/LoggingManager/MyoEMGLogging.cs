using System.Collections.Generic;
using System.Linq;
using Thalmic.Myo;
using UnityEngine;

public class MyoEMGLogging : MonoBehaviour
{
    [SerializeField] private LoggingManager loggingManager;
    [SerializeField] public ThalmicMyo thalmicMyo;

    List<string> EMGCol;

    void Start()
    {
        // List of column headers for EMG data.
        List<string> EMGCol = new List<string>() { "EMG_1", "EMG_2", "EMG_3", "EMG_4", "EMG_5", "EMG_6", "EMG_7", "EMG_8" };

        // Start by telling logging manager to create a new collection of logs
        // and optionally pass the column headers.
        loggingManager.CreateLog("EMG", EMGCol);

        // Add event handlers to the Myo device to receive EMG data.
        thalmicMyo._myo.EmgData += onReceiveData;
    }

    private void onReceiveData(object sender, EmgDataEventArgs data)
    {
        // Format the EMG data into a dictionary {"EMG_i", data.Emg[i]}
        Dictionary<string, object> emgData = EMGCol
            .Select((col, i) => new { col, value = data.Emg[i] })
            .ToDictionary(x => x.col, x => (object)x.value);

        // Time.frameCount (used in LogStore) can only be accessed from the main
        // thread so we use MainThreadDispatcher to enqueue the logging action.
        MainThreadDispatcher.Enqueue(() =>
        {
            loggingManager.Log("EMG", emgData);
        });
    }

    // Write the logs to disk when the application quits.
    void OnApplicationQuit()
    {
        Debug.Log("Application is quitting, saving EMG logs...");
        loggingManager.SaveLog("EMG", true);
    }
}
