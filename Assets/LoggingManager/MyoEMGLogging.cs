using System;
using System.Collections.Generic;
using System.Linq;
using Thalmic.Myo;
using UnityEngine;

public class MyoEMGLogging : MonoBehaviour
{
    [SerializeField] private LoggingManager loggingManager;
    [SerializeField] private EMGPointer rightHandEMGPointer;
    [SerializeField] public ThalmicMyo thalmicMyo;

    List<string> EMGCol;
    private bool isLoggingStarted = false;

    void Start()
    {
        // Check for null references and log errors if any are found.
        if (loggingManager == null)
        {
            Debug.LogError("[MyoEMGLogging] LoggingManager reference is not set in MyoEMGLogging.");
            return;
        }
        if (thalmicMyo == null)
        {
            Debug.LogError("[MyoEMGLogging] ThalmicMyo reference is not set in MyoEMGLogging.");
            return;
        }
        if (rightHandEMGPointer == null)
        {
            Debug.LogError("[MyoEMGPointer reference is not set in MyoEMGLogging.");
            return;
        }

        // Define EMG column headers and additional log columns.
        EMGCol = new List<string> { "EMG1", "EMG2", "EMG3", "EMG4", "EMG5", "EMG6", "EMG7", "EMG8" };
        List<string> logCols = new List<string>(EMGCol);
        logCols.AddRange(new List<string>
        {
            "CurrentGestures",
            "Threshold",
            "PredictionConfidence",
            "TargetGesture",        // Which gesture mole they're aiming for
            "GestureMaxEMG",        // Max EMG for that specific gesture
            "GlobalMaxEMG"          // Global max EMG (for reference)
        });

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

        if (thalmicMyo == null || thalmicMyo._myo == null)
        {
            Debug.LogWarning("[MyoEMGLogging] Cannot start EMG logging: ThalmicMyo is not ready.");
            return;
        }

        Debug.Log($"[MyoEMGLogging] Starting EMG logging with gesture-specific normalization support.");

        // Add event handlers to the Myo device to receive EMG data.
        thalmicMyo._myo.EmgData += onReceiveData;

        isLoggingStarted = true;
    }

    private void onReceiveData(object sender, EmgDataEventArgs data)
    {
        // Some devices send a first packet with only 7 channels; skip until we have all 8.
        if (data == null || data.Emg == null || data.Emg.Length != 8) return;

        try
        {
            // Format the EMG data into a dictionary {"EMG_i", data.Emg[i]}
            Dictionary<string, object> emgData = EMGCol
                .Select((col, i) => new { col, value = data.Emg[i] })
                .ToDictionary(x => x.col, x => (object)x.value);

            emgData["CurrentGestures"] = rightHandEMGPointer.GetCurrentGesture().ToString();
            emgData["Threshold"] = rightHandEMGPointer.getThresholdState();
            emgData["PredictionConfidence"] = rightHandEMGPointer.GetCurrentGestureConfidence().ToString();
            
            // NEW: Add gesture-specific normalization data
            emgData["TargetGesture"] = rightHandEMGPointer.GetTargetGesture();
            emgData["GestureMaxEMG"] = rightHandEMGPointer.GetTargetGestureMaxEMG();
            emgData["GlobalMaxEMG"] = rightHandEMGPointer.GetGlobalMaxEMG();

            // Time.frameCount (used in LogStore) can only be accessed from the main
            // thread so we use MainThreadDispatcher to enqueue the logging action.
            MainThreadDispatcher.Enqueue(() =>
            {
                loggingManager.Log("EMG", emgData);
            });
        }
        catch (System.Exception ex)
        {
            // Never let exceptions bubble up to the Myo event thread.
            Debug.LogException(ex);
        }
    }

    void FinishLogging()
    {
        if (thalmicMyo != null && thalmicMyo._myo != null) thalmicMyo._myo.EmgData -= onReceiveData;
        isLoggingStarted = false;
        
        Debug.Log($"[MyoEMGLogging] Finished logging.");
    }
}
