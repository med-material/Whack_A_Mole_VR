using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TactorConnector))]
public class TactorConnectorEditor : Editor
{
    private bool isScanning = false;
    private string scanStatus = "";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        TactorConnector connector = (TactorConnector)target;

        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tactor Debug Controls", EditorStyles.boldLabel);

            for (int i = 0; i < connector.tactors.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Tactor {i + 1}", GUILayout.Width(70));
                if (GUILayout.Button("Pulse", GUILayout.Width(60)))
                {
                    connector.ActivateTactor(i + 1);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(isScanning))
            {
                if (GUILayout.Button("Auto Connect Tactor Control Unit"))
                {
                    AutoConnectPort(connector);
                }
            }

            if (isScanning) EditorGUILayout.LabelField("Scanning ports... " + scanStatus);
        }
    }

    private void AutoConnectPort(TactorConnector connector)
    {
        isScanning = true;
        scanStatus = "Initializing...";
        System.Threading.Tasks.Task.Run(() =>
        {
            for (int i = 1; i <= 20; i++)
            {
                string portName = $"COM{i}";
                int boardId = TdkInterface.Connect(portName, (int)TdkDefines.DeviceTypes.Serial, IntPtr.Zero);
                if (boardId >= 0)
                {
                    EditorApplication.delayCall += () =>
                    {
                        connector.GetType().GetField("comPort", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            .SetValue(connector, portName);
                        connector.GetType().GetField("connectedBoardId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            .SetValue(connector, boardId);

                        Debug.Log($"[TactorConnector]AutoConnect: Connected to {portName} (Board ID: {boardId})");


                        EditorUtility.SetDirty(connector);

                        isScanning = false;
                        scanStatus = $"Connected to {portName}";
                    };
                    return;
                }
                else
                {
                    string error = TdkDefines.GetLastEAIErrorString();
                    scanStatus = $"{portName} failed: {error}";
                }
            }

            EditorApplication.delayCall += () =>
            {
                isScanning = false;
                scanStatus = "No Engineering Acoustics, Incorporated (EAI) tactor control unit found";
            };
        });
    }
}



[Serializable]
public class TactorSettings
{
    [Tooltip("Gain (1-255)")]
    [Range(1, 255)]
    public int gain = 100;

    [Tooltip("Frequency (300-3550 Hz)")]
    [Range(300, 3550)]
    public int frequency = 500;

    [Header("Ramping")]
    [Tooltip("Ramp Gain Start (1-255)")]
    [Range(1, 255)]
    public int rampGainStart = 1;

    [Tooltip("Ramp Gain End (1-255)")]
    [Range(1, 255)]
    public int rampGainEnd = 255;

    [Tooltip("Ramp Frequency Start (300-3550 Hz)")]
    [Range(300, 3550)]
    public int rampFreqStart = 300;

    [Tooltip("Ramp Frequency End (300-3550 Hz)")]
    [Range(300, 3550)]
    public int rampFreqEnd = 3550;

    [Tooltip("Ramp Duration (ms)")]
    [Range(1, 5000)]
    public int rampDuration = 1000;

    [Tooltip("Ramp Function")]
    [Range(1, 5)]
    public int rampFunc = 1;
}

public class TactorValues
{
    public int gain;
    public int frequency;
    public int duration;
    public TactorValues(int gain, int frequency, int duration)
    {
        this.gain = gain;
        this.frequency = frequency;
        this.duration = duration;
    }
}

public class TactorConnector : MonoBehaviour
{
    public bool feedbackEnabled = true; // Enable or disable haptic feedback globally (default: enabled)
    private int connectedBoardId = -1;
    private Coroutine[] resetCoroutines;

    // Public property to check if tactor is connected
    public bool IsConnected() => connectedBoardId >= 0;



    [Header("General Settings")]
    [Tooltip("Delay before vibration starts after command is received (ms)")]
    [SerializeField] private int delay = 0;

    [Tooltip("Duration of vibration when a pulse is sent (ms)")]
    [SerializeField] private int pulseDuration = 250;

    [Tooltip("Tactor number out of the connected tactors")]
    [SerializeField] private int tactorNumber = 1;

    [Tooltip("Serial COM port for the tactor device")]
    [SerializeField] private string comPort;

    [TextArea(10, 20)]
    [SerializeField]
    private string controls =
        "Numbers 1-5 (not numpad) applies settings to tactor 1-5.\n" +
        "Q applies gain and frequency settings to all tactors.\n" +
        "W applies ramping gain on all tactors.\n" +
        "E applies ramping frequencies on all tactors.\n" +
        "Each tactor has individual settings below. These should update at runtime, once applied using one of the command keys mentioned here.\n" +
        "For testing purposes, 'Space' pulses tactor 1 with duration = pulseDuration and delay = delay";

    [TextArea(5, 20)]
    [SerializeField]
    private string numbersGuide =
        "Valid Gain Range: 1-255\n" +
        "Valid Frequency Range: 300-3550\n" +
        "Duration is in ms. Minimum 1.";

    [Header("Tactor Settings")]
    [Tooltip("Settings for each tactor (1-5)")]
    [SerializeField] public TactorSettings[] tactors;
    public TactorValues[] currentValues;


    void Start()
    {
        Debug.Log("[TactorConnector] Initializing TDK...");
        CheckError(TdkInterface.InitializeTI());

        // Initialize per-tactor current values for readability and tracking.
        currentValues = (tactors != null && tactors.Length > 0)
            ? tactors.Select(_ => new TactorValues(0, 0, 0)).ToArray()
            : Array.Empty<TactorValues>();

        // init coroutine handles so we can cancel per-tactor resets safely
        resetCoroutines = (tactors != null && tactors.Length > 0)
            ? Enumerable.Repeat<Coroutine>(null, tactors.Length).ToArray()
            : Array.Empty<Coroutine>();
    }

    void Update()
    {
        if (connectedBoardId >= 0 && Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"[TactorConnector] Pulsing tactor {tactorNumber} for {pulseDuration}");
            ActivateTactor(tactorNumber, durationSetting: pulseDuration);
        }
        if (connectedBoardId < 0) return; //To prevent "ERROR_BADPARAMETER" error due to attempting to pass values when no board connected.

        for (int i = 0; i < tactors.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                ActivateTactor(tactorID: i + 1);
        }
    }

    public void TriggerTactor(int connectedBoardId, int tactorNumber, int pulseDuration, int delay)
    {
        // Delegate to ActivateTactor so behavior is centralized and lastActivated* fields are correct.
        ActivateTactor(tactorNumber, durationSetting: pulseDuration);
    }


    // Pulse a specific tactor, with optional overrides for gain, frequency, and duration
    // If not provided, uses static settings from the inspector
    public void ActivateTactor(int tactorID, int? gainSetting = null, int? frequencySetting = null, int? durationSetting = null)
    {
        if (tactors == null || tactors.Length == 0)
        {
            Debug.LogWarning("[TactorConnector] No tactor settings available.");
            return;
        }

        int index = tactorID - 1; // convert to 0-based
        if (index < 0 || index >= tactors.Length)
        {
            Debug.LogWarning($"[TactorConnector] Invalid tactorID {tactorID}. Valid ranges: 1..{tactors.Length}.");
            return;
        }

        EnsureArraysInitialized(); // <- centralize init checks

        int gain = gainSetting ?? tactors[index].gain;
        int frequency = frequencySetting ?? tactors[index].frequency;
        int duration = durationSetting ?? pulseDuration;

        // create snapshot and assign once (avoid writing fields then overwriting)
        TactorValues snapshot = new TactorValues(gain, frequency, duration);
        currentValues[index] = snapshot;

        if (resetCoroutines[index] != null)
        {
            try { StopCoroutine(resetCoroutines[index]); }
            catch { /* ignore StopCoroutine race conditions */ }
            resetCoroutines[index] = null;
        }

        if (connectedBoardId < 0)
        {
            Debug.LogWarning("[TactorConnector] No tactor board connected.");
            return;
        }

        Debug.Log($"[TactorConnector] [Tactor Number {tactorID} ] Activating with Gain: {snapshot.gain}, Duration: {snapshot.duration}ms, Frequency: {snapshot.frequency}Hz");
        // Use the 1-based tactor id when calling the native TDK API (assumed).
        CheckError(TdkInterface.ChangeGain(connectedBoardId, tactorID, gain, delay));
        CheckError(TdkInterface.Pulse(connectedBoardId, tactorID, duration, delay));

        try
        {
            resetCoroutines[index] = StartCoroutine(ResetTactorValuesCoroutine(index, snapshot, duration));
        }
        catch (Exception e)
        {
            Debug.LogError($"[TactorConnector] Error starting reset coroutine for tactor {tactorID}: {e.Message}");
        }


    }

    void RampGain(int tactorID, TactorSettings settings)
    {
        Debug.Log($"[TactorConnector] [Tactor {tactorID}] Ramping Gain: {settings.rampGainStart} → {settings.rampGainEnd}");
        CheckError(TdkInterface.RampGain(connectedBoardId, tactorID, settings.rampGainStart, settings.rampGainEnd, settings.rampDuration, settings.rampFunc, delay));
    }

    void RampFrequency(int tactorID, TactorSettings settings)
    {
        Debug.Log($"[TactorConnector] [Tactor {tactorID}] Ramping Frequency: {settings.rampFreqStart}Hz → {settings.rampFreqEnd}Hz");
        CheckError(TdkInterface.RampFreq(connectedBoardId, tactorID, settings.rampFreqStart, settings.rampFreqEnd, settings.rampDuration, settings.rampFunc, delay));
    }

    public void ApplyAllStaticSettings()
    {
        for (int i = 0; i < tactors.Length; i++)
            ActivateTactor(i + 1, tactors[i].gain, tactors[i].frequency, pulseDuration);
    }

    public void RampAllGains()
    {
        for (int i = 0; i < tactors.Length; i++)
            RampGain(i + 1, tactors[i]);
    }

    public void RampAllFrequencies()
    {
        for (int i = 0; i < tactors.Length; i++)
            RampFrequency(i + 1, tactors[i]);
    }

    // Returns the index (0-based) and values of the last activated tactor with non-zero gain, frequency, or duration.
    public (int index, TactorValues values) GetActiveTactor()
    {
        if (currentValues == null || currentValues.Length == 0)
            return (-1, null);

        for (int i = currentValues.Length - 1; i >= 0; i--)
        {
            TactorValues v = currentValues[i];
            if (v != null && (v.gain != 0 || v.frequency != 0 || v.duration != 0))
                return (i, v);
        }

        return (-1, null);
    }

    private void CheckError(int ret)
    {
        if (ret < 0)
        {
            Debug.LogError("[TactorConnector] TDK Error: " + TdkDefines.GetLastEAIErrorString());
        }
    }

    void OnApplicationQuit()
    {
        if (connectedBoardId >= 0)
        {
            Debug.Log("[TactorConnector] Closing connection...");
            CheckError(TdkInterface.Close(connectedBoardId));
        }

        Debug.Log("[TactorConnector] Shutting down TDK...");
        CheckError(TdkInterface.ShutdownTI());
    }

    private void OnValidate()
    {
        if (connectedBoardId < 0) return; //To prevent "ERROR_BADPARAMETER" error due to attempting to pass values when no board connected.
        ApplyAllStaticSettings();
        RampAllGains();
        RampAllFrequencies();
    }

    private IEnumerator ResetTactorValuesCoroutine(int index, TactorValues snapshot, int durationMs)
    {
        float waitSeconds = Mathf.Max((float)0.001, durationMs / 1000f);
        yield return new WaitForSeconds(waitSeconds);

        currentValues[index] = new TactorValues(0, 0, 0);

        if (resetCoroutines != null && index >= 0 && index < resetCoroutines.Length) resetCoroutines[index] = null;


    }

    private void EnsureArraysInitialized()
    {
        if (tactors == null || tactors.Length == 0)
        {
            currentValues = Array.Empty<TactorValues>();
            resetCoroutines = Array.Empty<Coroutine>();
            return;
        }

        if (currentValues == null || resetCoroutines == null || currentValues.Length != tactors.Length || resetCoroutines.Length != tactors.Length)
        {
            currentValues = tactors.Select(_ => new TactorValues(0, 0, 0)).ToArray();
            resetCoroutines = Enumerable.Repeat<Coroutine>(null, tactors.Length).ToArray();
        }
    }
}
