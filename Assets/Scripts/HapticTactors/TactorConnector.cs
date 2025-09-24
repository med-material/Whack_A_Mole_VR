using System;
using Tdk;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TactorConnector))]
public class TactorConnectorEditor : Editor
{
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
                    // Use the public method to pulse the tactor
                    connector.PulseTactor(i + 1);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
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

public class TactorConnector : MonoBehaviour
{
    private int connectedBoardId = -1;

    [Header("General Settings")]
    [Tooltip("Delay before vibration starts after command is received (ms)")]
    [SerializeField] private int delay = 0;

    [Tooltip("Duration of vibration when a pulse is sent (ms)")]
    [SerializeField] private int pulseDuration = 250;

    [Tooltip("Serial COM port for the tactor device")]
    [SerializeField] private string comPort = "COM4";

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



    void Start() // Opens a connection to the tactor device software and binds the controller id to connectedBoardId.
    {
        Debug.Log("Initializing TDK...");
        CheckError(TdkInterface.InitializeTI());

        Debug.Log($"Connecting to {comPort}...");
        int boardId = TdkInterface.Connect(comPort, (int)TdkDefines.DeviceTypes.Serial, IntPtr.Zero);

        if (boardId >= 0)
        {
            connectedBoardId = boardId;
            Debug.Log($"Connected! Board ID: {connectedBoardId}");
        }
        else
        {
            Debug.LogError("Failed to connect: " + TdkDefines.GetLastEAIErrorString());
        }
    }

    void Update() // Here we just listen for key presses to trigger various commands.
    {
        if (connectedBoardId >= 0 && Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Pulsing tactor 1 for 250 ms...");
            CheckError(TdkInterface.Pulse(connectedBoardId, 1, pulseDuration, delay));

        }
        if (connectedBoardId < 0) return;

        for (int i = 0; i < tactors.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                ApplySettingsToTactor(i + 1, tactors[i]);
        }
    }

    void ApplySettingsToTactor(int tactorID, TactorSettings settings)
    {
        Debug.Log($"[Tactor {tactorID}] Setting Gain: {settings.gain}, Freq: {settings.frequency}");
        CheckError(TdkInterface.ChangeGain(connectedBoardId, tactorID, settings.gain, delay));
        CheckError(TdkInterface.ChangeFreq(connectedBoardId, tactorID, settings.frequency, delay));
    }

    void RampGain(int tactorID, TactorSettings settings)
    {
        Debug.Log($"[Tactor {tactorID}] Ramping Gain: {settings.rampGainStart} → {settings.rampGainEnd}");
        CheckError(TdkInterface.RampGain(connectedBoardId, tactorID, settings.rampGainStart, settings.rampGainEnd, settings.rampDuration, settings.rampFunc, delay));
    }

    void RampFrequency(int tactorID, TactorSettings settings)
    {
        Debug.Log($"[Tactor {tactorID}] Ramping Frequency: {settings.rampFreqStart}Hz → {settings.rampFreqEnd}Hz");
        CheckError(TdkInterface.RampFreq(connectedBoardId, tactorID, settings.rampFreqStart, settings.rampFreqEnd, settings.rampDuration, settings.rampFunc, delay));
    }

    public void ApplyAllStaticSettings()
    {
        for (int i = 0; i < tactors.Length; i++)
            ApplySettingsToTactor(i + 1, tactors[i]);
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

    public void TriggerGraspingStateFeedback()
    {
        for (int i = 0; i < tactors.Length; i++)
            ApplySettingsToTactor(i + 1, tactors[i]);

        for (int i = 0; i < tactors.Length; i++)
            TdkInterface.Pulse(connectedBoardId, i + 1, 250, 0);
    }

    public void TriggerPinchingStateFeedback()
    {
        for (int i = 0; i < tactors.Length; i++)
            ApplySettingsToTactor(i + 1, tactors[i]);

        for (int i = 0; i < tactors.Length; i++)
            TdkInterface.Pulse(connectedBoardId, i + 1, 250, 0);
    }

    public void TriggerDefaultStateFeedback()
    {
        for (int i = 0; i < tactors.Length; i++)
            ApplySettingsToTactor(i + 1, tactors[i]);

        for (int i = 0; i < tactors.Length; i++)
            TdkInterface.Pulse(connectedBoardId, i + 1, 250, 0);
    }

    private void CheckError(int ret) // Checks the tactor device software for errors
    {
        if (ret < 0)
        {
            Debug.LogError("TDK Error: " + TdkDefines.GetLastEAIErrorString());
        }
    }

    void OnApplicationQuit()
    {
        if (connectedBoardId >= 0)
        {
            Debug.Log("Closing connection...");
            CheckError(TdkInterface.Close(connectedBoardId));
        }

        Debug.Log("Shutting down TDK...");
        CheckError(TdkInterface.ShutdownTI());
    }

    private void OnValidate()
    {
        ApplyAllStaticSettings();
        RampAllGains();
        RampAllFrequencies();
    }

    public void PulseTactor(int tactorID) // Public method to pulse a specific tactor, can be called from other scripts.
    {
        if (connectedBoardId >= 0)
        {
            Debug.Log($"Inspector: Pulsing tactor {tactorID} for {pulseDuration} ms (delay {delay} ms)");
            CheckError(TdkInterface.Pulse(connectedBoardId, tactorID, pulseDuration, delay));
        }
        else
        {
            Debug.LogWarning("No tactor board connected.");
        }
    }


}
