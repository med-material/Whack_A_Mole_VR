using System.Collections.Generic;
using System.Linq;
using Thalmic.Myo;
using UnityEngine;

public class MyoEMGLogging : MonoBehaviour
{
    [SerializeField] private LoggingManager loggingManager;
    [SerializeField] private EMGPointer rightHandEMGPointer;
    [SerializeField] private EMGPointer leftHandEMGPointer;
    [SerializeField] public ThalmicMyo thalmicMyo;

    private EMGPointer activeEMGPointer; // dynamically resolved active EMG pointer based on current controller setup
    private GameDirector gameDirector;

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

        // Subscribe to GameDirector state updates so logging starts/stops with gameplay
        gameDirector = FindObjectOfType<GameDirector>();
        if (gameDirector != null)
        {
            gameDirector.stateUpdate.AddListener(OnGameDirectorStateUpdate);
        }
        else
        {
            Debug.LogWarning("[MyoEMGLogging] GameDirector not found. EMG logging will not auto-start with game state.");
        }

        // Try to auto-resolve active EMG pointer if not explicitly wired
        RefreshActiveEMGPointer();

        if (activeEMGPointer == null)
        {
            Debug.LogWarning("[MyoEMGLogging] No active EMGPointer found. Will try to resolve dynamically when EMG data arrives.");
        }

        // Define EMG column headers and additional log columns.
        EMGCol = new List<string> { "EMG1", "EMG2", "EMG3", "EMG4", "EMG5", "EMG6", "EMG7", "EMG8" };
        List<string> logCols = new List<string>(EMGCol)
        {
            "CurrentGestures",
            "Threshold",
            "PredictionConfidence"
        };

        // Initialize EMG log collection with specified columns.
        loggingManager.CreateLog("EMG", logCols);
    }

    private void OnDestroy()
    {
        if (gameDirector != null)
        {
            gameDirector.stateUpdate.RemoveListener(OnGameDirectorStateUpdate);
        }
        FinishLogging();
    }

    private void RefreshActiveEMGPointer()
    {
        // Prefer explicitly assigned left/right based on which is enabled, otherwise fall back to any enabled EMGPointer in scene
        EMGPointer candidate = null;

        if (leftHandEMGPointer != null && leftHandEMGPointer.isActiveAndEnabled)
        {
            candidate = leftHandEMGPointer;
        }
        else if (rightHandEMGPointer != null && rightHandEMGPointer.isActiveAndEnabled)
        {
            candidate = rightHandEMGPointer;
        }
        else
        {
            // find whichever EMGPointer is currently enabled
            candidate = FindObjectsOfType<EMGPointer>()
                .FirstOrDefault(p => p != null && p.isActiveAndEnabled && p.gameObject.activeInHierarchy);
        }

        activeEMGPointer = candidate;
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
            // Copy raw EMG data to avoid accessing event args off-thread later
            int[] emgCopy = new int[8];
            for (int i = 0; i < 8; i++) emgCopy[i] = data.Emg[i];

            // Do ALL Unity API work on the main thread
            MainThreadDispatcher.Enqueue(() =>
            {
                // Ensure we are logging from the currently active EMG pointer (Left/Right depending on setup)
                if (activeEMGPointer == null || !activeEMGPointer.isActiveAndEnabled)
                {
                    RefreshActiveEMGPointer();
                }

                // Fallback: if still null, try right then left just for data continuity
                EMGPointer pointerForLog = activeEMGPointer ?? rightHandEMGPointer ?? leftHandEMGPointer;

                // Format the EMG data into a dictionary {"EMG_i", data.Emg[i]}
                Dictionary<string, object> emgData = EMGCol
                    .Select((col, i) => new { col, value = emgCopy[i] })
                    .ToDictionary(x => x.col, x => (object)x.value);

                if (pointerForLog != null)
                {
                    emgData["CurrentGestures"] = pointerForLog.GetCurrentGesture().ToString();
                    emgData["Threshold"] = pointerForLog.getThresholdState();
                    emgData["PredictionConfidence"] = pointerForLog.GetCurrentGestureConfidence().ToString();
                }
                else
                {
                    emgData["CurrentGestures"] = "Unknown";
                    emgData["Threshold"] = "below";
                    emgData["PredictionConfidence"] = "Uncertain";
                }

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
    }
}
