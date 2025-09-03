using System.Collections.Generic;
using System.Linq;
using Thalmic.Myo;
using UnityEngine;

public class MyoEMGLogging : MonoBehaviour
{
    [SerializeField] private LoggingManager loggingManager;
    [SerializeField] public ThalmicMyo thalmicMyo;

    List<string> EMGCol;
    private bool isLoggingStarted = false;

    public static string CurrentGestures = "NULL";
    public static string Threshold = "below";

    void Start()
    {
        // Define EMG column headers and additional log columns.
        EMGCol = new List<string> { "EMG1", "EMG2", "EMG3", "EMG4", "EMG5", "EMG6", "EMG7", "EMG8" };
        List<string> logCols = new List<string>(EMGCol)
        {
            "CurrentGestures",
            "Threshold"
        };

        // Initialize EMG log collection with specified columns.
        loggingManager.CreateLog("EMG", logCols);
    }

    public void OnGameDirectorStateUpdate(GameDirector.GameState newState)
    {
        switch (newState)
        {
            case GameDirector.GameState.Stopped:
                FinishLogging();
                break;
            case GameDirector.GameState.Playing:
                StartLogging();
                break;
            case GameDirector.GameState.Paused:
                // TODO
                break;
        }
    }

    private void StartLogging()
    {
        if (isLoggingStarted) return;

        // Add event handlers to the Myo device to receive EMG data.
        thalmicMyo._myo.EmgData += onReceiveData;

        isLoggingStarted = true;
    }

    private void onReceiveData(object sender, EmgDataEventArgs data)
    {
        // Format the EMG data into a dictionary {"EMG_i", data.Emg[i]}
        Dictionary<string, object> emgData = EMGCol
            .Select((col, i) => new { col, value = data.Emg[i] })
            .ToDictionary(x => x.col, x => (object)x.value);

        // Add CurrentGestures and Threshold columns with their current values
        emgData["CurrentGestures"] = CurrentGestures;
        emgData["Threshold"] = Threshold;

        // Time.frameCount (used in LogStore) can only be accessed from the main
        // thread so we use MainThreadDispatcher to enqueue the logging action.
        MainThreadDispatcher.Enqueue(() =>
        {
            loggingManager.Log("EMG", emgData);
        });
    }

    void FinishLogging()
    {
        thalmicMyo._myo.EmgData -= onReceiveData;
        isLoggingStarted = false;
    }
}
