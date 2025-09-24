using UnityEngine;
using System;
using Tdk;

public class TactorConnector : MonoBehaviour
{
    private int connectedBoardId = -1;
    [SerializeField] private int delay = 0; // Delay before vibration starts after command is received (e.g. delay can be different for each tactor)
    [SerializeField] private int pulseDuration = 250; // Duration of vibration when a pulse is sent

    [SerializeField] private string comPort = "COM4"; // Change this to your actual port

    [TextArea(10,20)]
    [SerializeField] string controls = "Numbers 1-5 (not numpad) applies settings to tactor 1-5.\n" +
    "Q applies gain and frequence settings to all tactors.\n" +
    "W applies ramping gain on all tactors.\n" +
    "E applies ramping frequencies on all tactors.\n" +
    "Each tactor has individual settings below. These should update at runtime, once applied using one of the command keys mentioned here.\n";
    //For testing purposes, "Z" pulses tactor 1 with duration = pulseDuration and delay = delay
    
    [TextArea(5,20)]
    [SerializeField] string numbersGuide = "Valid Gain Range: 1-255\n" +
    "Valid Frequency Range: 300-3550\n";
    //Duration is in ms. Minimum 1.

    // Below are variables for each tactor, editable from the inspector.
    // ---------------- Tactor 1 ----------------
    [Header("Tactor 1")]
    [SerializeField] private int gain1 = 100;
    [SerializeField] private int frequency1 = 500;
    [SerializeField] private int ramp1GainStart = 1;
    [SerializeField] private int ramp1GainEnd = 255;
    [SerializeField] private int ramp1FreqStart = 300;
    [SerializeField] private int ramp1FreqEnd = 3550;
    [SerializeField] private int ramp1Duration = 1000;
    [SerializeField] private int ramp1Func = 1;

    // ---------------- Tactor 2 ----------------
    [Header("Tactor 2")]
    [SerializeField] private int gain2 = 100;
    [SerializeField] private int frequency2 = 500;
    [SerializeField] private int ramp2GainStart = 1;
    [SerializeField] private int ramp2GainEnd = 255;
    [SerializeField] private int ramp2FreqStart = 300;
    [SerializeField] private int ramp2FreqEnd = 3550;
    [SerializeField] private int ramp2Duration = 1000;
    [SerializeField] private int ramp2Func = 1;

    // ---------------- Tactor 3 ----------------
    [Header("Tactor 3")]
    [SerializeField] private int gain3 = 100;
    [SerializeField] private int frequency3 = 500;
    [SerializeField] private int ramp3GainStart = 1;
    [SerializeField] private int ramp3GainEnd = 255;
    [SerializeField] private int ramp3FreqStart = 300;
    [SerializeField] private int ramp3FreqEnd = 3550;
    [SerializeField] private int ramp3Duration = 1000;
    [SerializeField] private int ramp3Func = 1;

    // ---------------- Tactor 4 ----------------
    [Header("Tactor 4")]
    [SerializeField] private int gain4 = 100;
    [SerializeField] private int frequency4 = 500;
    [SerializeField] private int ramp4GainStart = 1;
    [SerializeField] private int ramp4GainEnd = 255;
    [SerializeField] private int ramp4FreqStart = 300;
    [SerializeField] private int ramp4FreqEnd = 3550;
    [SerializeField] private int ramp4Duration = 1000;
    [SerializeField] private int ramp4Func = 1;

    // ---------------- Tactor 5 ----------------
    [Header("Tactor 5")]
    [SerializeField] private int gain5 = 100;
    [SerializeField] private int frequency5 = 500;
    [SerializeField] private int ramp5GainStart = 1;
    [SerializeField] private int ramp5GainEnd = 255;
    [SerializeField] private int ramp5FreqStart = 300;
    [SerializeField] private int ramp5FreqEnd = 3550;
    [SerializeField] private int ramp5Duration = 1000;
    [SerializeField] private int ramp5Func = 1;


    void Awake() // Don't mind this. Just making sure that the TextAreas in the inspector load properly.
    {
        controls = controls;
        numbersGuide = numbersGuide;
    }


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
        // Press spacebar to pulse tactor 1
        //if (connectedBoardId >= 0 && Input.GetKeyDown(KeyCode.Space))
        //{
            //Debug.Log("Pulsing tactor 1 for 250 ms...");
            //CheckError(TdkInterface.Pulse(connectedBoardId, 1, 250, 0));
            //if (Input.GetKeyDown(KeyCode.Z)) TdkInterface.Pulse(connectedBoardId, 1, 250, delay); // pulse tactor 1
        //}
         if (connectedBoardId < 0) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) ApplySettingsToTactor(1, gain1, frequency1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ApplySettingsToTactor(2, gain2, frequency2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ApplySettingsToTactor(3, gain3, frequency3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ApplySettingsToTactor(4, gain4, frequency4);
        if (Input.GetKeyDown(KeyCode.Alpha5)) ApplySettingsToTactor(5, gain5, frequency5);

        if (Input.GetKeyDown(KeyCode.Q)) ApplyAllStaticSettings();
        if (Input.GetKeyDown(KeyCode.W)) RampAllGains();
        if (Input.GetKeyDown(KeyCode.E)) RampAllFrequencies();
        if (Input.GetKeyDown(KeyCode.Z)) TdkInterface.Pulse(connectedBoardId, 1, pulseDuration, delay); // pulse tactor1
    }

    void OnApplicationQuit() // Shut down the connection to the tactor software when application closes.
    {
        if (connectedBoardId >= 0)
        {
            Debug.Log("Closing connection...");
            CheckError(TdkInterface.Close(connectedBoardId));
        }

        Debug.Log("Shutting down TDK...");
        CheckError(TdkInterface.ShutdownTI());
    }

    private void CheckError(int ret) // Checks the tactor device software for errors
    {
        if (ret < 0)
        {
            Debug.LogError("TDK Error: " + TdkDefines.GetLastEAIErrorString());
        }
    }

    void ApplySettingsToTactor(int tactorID, int gain, int frequency) // Applies gain and frequency settings to the specified tactor
    {
        Debug.Log($"[Tactor {tactorID}] Setting Gain: {gain}, Freq: {frequency}");
        CheckError(TdkInterface.ChangeGain(connectedBoardId, tactorID, gain, delay));
        CheckError(TdkInterface.ChangeFreq(connectedBoardId, tactorID, frequency, delay));
    }

    void RampGain(int tactorID, int start, int end, int duration, int func) // Ramps the gain of the specified tactor
    {
        Debug.Log($"[Tactor {tactorID}] Ramping Gain: {start} → {end}");
        CheckError(TdkInterface.RampGain(connectedBoardId, tactorID, start, end, duration, func, delay));
    }

    void RampFrequency(int tactorID, int start, int end, int duration, int func) // Ramps the vibration frequency of the specified tactor
    {
        Debug.Log($"[Tactor {tactorID}] Ramping Frequency: {start}Hz → {end}Hz");
        CheckError(TdkInterface.RampFreq(connectedBoardId, tactorID, start, end, duration, func, delay));
    }

    public void ApplyAllStaticSettings() // Applies gain and frequency settings to all 5 tactors
    {
        ApplySettingsToTactor(1, gain1, frequency1);
        ApplySettingsToTactor(2, gain2, frequency2);
        ApplySettingsToTactor(3, gain3, frequency3);
        ApplySettingsToTactor(4, gain4, frequency4);
        ApplySettingsToTactor(5, gain5, frequency5);
    }

    public void RampAllGains() // Ramps the gain of all 5 tactors from rampGainStart to rampGainEnd
    {
        RampGain(1, ramp1GainStart, ramp1GainEnd, ramp1Duration, ramp1Func);
        RampGain(2, ramp2GainStart, ramp2GainEnd, ramp2Duration, ramp2Func);
        RampGain(3, ramp3GainStart, ramp3GainEnd, ramp3Duration, ramp3Func);
        RampGain(4, ramp4GainStart, ramp4GainEnd, ramp4Duration, ramp4Func);
        RampGain(5, ramp5GainStart, ramp5GainEnd, ramp5Duration, ramp5Func);
    }

    public void RampAllFrequencies() // Ramps the frequencies of all 5 tactors from rampFreqStart to rampFreqEnd
    {
        RampFrequency(1, ramp1FreqStart, ramp1FreqEnd, ramp1Duration, ramp1Func);
        RampFrequency(2, ramp2FreqStart, ramp2FreqEnd, ramp2Duration, ramp2Func);
        RampFrequency(3, ramp3FreqStart, ramp3FreqEnd, ramp3Duration, ramp3Func);
        RampFrequency(4, ramp4FreqStart, ramp4FreqEnd, ramp4Duration, ramp4Func);
        RampFrequency(5, ramp5FreqStart, ramp5FreqEnd, ramp5Duration, ramp5Func);
    }

    public void TriggerGraspingStateFeedback()
    {
        // Change values here to match our intended tactile feedback
        ApplySettingsToTactor(1, gain1, frequency1);
        ApplySettingsToTactor(2, gain2, frequency2);
        ApplySettingsToTactor(2, gain2, frequency3);
        ApplySettingsToTactor(2, gain2, frequency4);
        ApplySettingsToTactor(2, gain2, frequency5);

        TdkInterface.Pulse(connectedBoardId, 1, 250, 0);
        TdkInterface.Pulse(connectedBoardId, 2, 250, 0);
        TdkInterface.Pulse(connectedBoardId, 3, 250, 0);
        TdkInterface.Pulse(connectedBoardId, 4, 250, 0);
        TdkInterface.Pulse(connectedBoardId, 5, 250, 0);
    }

    public void TriggerPinchingStateFeedback()
    {
        // Change values here to match our intended tactile feedback
        ApplySettingsToTactor(1, gain1, frequency1);
        ApplySettingsToTactor(2, gain2, frequency2);
        ApplySettingsToTactor(2, gain2, frequency3);
        ApplySettingsToTactor(2, gain2, frequency4);
        ApplySettingsToTactor(2, gain2, frequency5);

        TdkInterface.Pulse(connectedBoardId, 1, 250, 0);
        TdkInterface.Pulse(connectedBoardId, 2, 250, 0);
        TdkInterface.Pulse(connectedBoardId, 3, 250, 0);
        TdkInterface.Pulse(connectedBoardId, 4, 250, 0);
        TdkInterface.Pulse(connectedBoardId, 5, 250, 0);
    }

    public void TriggerDefaultStateFeedback()
    {
        // Change values here to match our intended tactile feedback
        ApplySettingsToTactor(1, gain1, frequency1);
        ApplySettingsToTactor(2, gain2, frequency2);
        ApplySettingsToTactor(2, gain2, frequency3);
        ApplySettingsToTactor(2, gain2, frequency4);
        ApplySettingsToTactor(2, gain2, frequency5);

        TdkInterface.Pulse(connectedBoardId, 1, 250, 0);
        TdkInterface.Pulse(connectedBoardId, 2, 250, 0);
        TdkInterface.Pulse(connectedBoardId, 3, 250, 0);
        TdkInterface.Pulse(connectedBoardId, 4, 250, 0);
        TdkInterface.Pulse(connectedBoardId, 5, 250, 0);
    }

    // In case we need to trigger tactors based on some variable at runtime.
    public void CustomFeedbackTrigger(int tactor1Gain, int tactor2Gain, int tactor3Gain, int tactor4Gain, int tactor5Gain, int tactor1Freq, int tactor2Freq, int tactor3Freq, int tactor4Freq, int tactor5Freq, int pulseduration, int pulsedelay)
    {
        // Clamp Gains [1, 200]
        tactor1Gain = Mathf.Clamp(tactor1Gain, 1, 200);
        tactor2Gain = Mathf.Clamp(tactor2Gain, 1, 200);
        tactor3Gain = Mathf.Clamp(tactor3Gain, 1, 200);
        tactor4Gain = Mathf.Clamp(tactor4Gain, 1, 200);
        tactor5Gain = Mathf.Clamp(tactor5Gain, 1, 200);

        // Clamp Frequencies [300, 3000]
        tactor1Freq = Mathf.Clamp(tactor1Freq, 300, 3000);
        tactor2Freq = Mathf.Clamp(tactor2Freq, 300, 3000);
        tactor3Freq = Mathf.Clamp(tactor3Freq, 300, 3000);
        tactor4Freq = Mathf.Clamp(tactor4Freq, 300, 3000);
        tactor5Freq = Mathf.Clamp(tactor5Freq, 300, 3000);

        // Clamp miscellaneous values
        pulseduration = Mathf.Clamp(pulseduration, 1, 1000);
        pulsedelay = Mathf.Clamp(pulsedelay, 0, 1000);

        ApplySettingsToTactor(1, tactor1Gain, tactor1Freq);
        ApplySettingsToTactor(2, tactor2Gain, tactor2Freq);
        ApplySettingsToTactor(2, tactor3Gain, tactor3Freq);
        ApplySettingsToTactor(2, tactor4Gain, tactor4Freq);
        ApplySettingsToTactor(2, tactor5Gain, tactor5Freq);

        TdkInterface.Pulse(connectedBoardId, 1, pulseduration, pulsedelay);
        TdkInterface.Pulse(connectedBoardId, 2, pulseduration, pulsedelay);
        TdkInterface.Pulse(connectedBoardId, 3, pulseduration, pulsedelay);
        TdkInterface.Pulse(connectedBoardId, 4, pulseduration, pulsedelay);
        TdkInterface.Pulse(connectedBoardId, 5, pulseduration, pulsedelay);
    }
}
