using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR;

public class DevMonitoring : MonoBehaviour
{
    [SerializeField] Color rightColor = Color.green;
    [SerializeField] Color wrongColor = Color.gray;

    [SerializeField] EMGDataProcessor emgDataProcessor;
    [SerializeField] TactorConnector tactorConnector;

    // Light indicators
    [SerializeField] Image AI_output;
    [SerializeField] Image EMG_output;
    [SerializeField] Image HapticTactor_output;
    [SerializeField] Image Headset_output;
    [SerializeField] Image BaseStation_output;
    [SerializeField] Image Tracker_output;

    // Text indicators
    [SerializeField] TextMeshProUGUI BaseStationCount;
    [SerializeField] TextMeshProUGUI TrackerCount;
    [SerializeField] Text maxButtonText;
    [SerializeField] Text minButtonText;

    List<InputDevice> steamVRDevices = new List<InputDevice>();
    public bool IAServerStatus { get; private set; }
    public bool IsEMGDatareceived { get; private set; }
    public bool IsTactorConnected { get; private set; }

    private int[] previusEMGData = new int[8];

    void Start()
    {
        if (AI_output == null) Debug.LogError("[DevMonitoring] Ouput reference for AI Server is missing.");
        if (EMG_output == null) Debug.LogError("[DevMonitoring] Ouput reference for EMG is missing.");
        if (HapticTactor_output == null) Debug.LogError("[DevMonitoring] Ouput reference for Haptic Tactor is missing.");
        if (tactorConnector == null) Debug.LogError("[DevMonitoring] Reference to TactorConnector is missing.");

        InvokeRepeating(nameof(RepeatedMethod), 0f, 2f); // Update status only one time every 2 seconds
    }

    void RepeatedMethod()
    {
        StartCoroutine(PingAIServer());
        steamVRDevices = GetConnectedSteamVRDevices();
        IsEMGDatareceived = IsEMGDataUpdated();
        IsTactorConnected = GetTactorConnected();

        UpdateDevUI();
    }

    private void UpdateDevUI()
    {
        AI_output.color = IAServerStatus ? rightColor : wrongColor;
        EMG_output.color = IsEMGDatareceived ? rightColor : wrongColor;
        HapticTactor_output.color = IsTactorConnected ? rightColor : wrongColor;

        bool hasHeadset = steamVRDevices.Any(d => d.name != null && d.name.Contains("Headset"));
        Headset_output.color = hasHeadset ? rightColor : wrongColor;

        int baseStationCount = steamVRDevices.Count(d => d.name != null && d.name.Contains("Tracking Reference"));
        BaseStation_output.color = baseStationCount >= 2 ? rightColor : wrongColor;
        BaseStationCount.text = $"({baseStationCount})";

        int trackerCount = steamVRDevices.Count(d => d.name != null && d.name.Contains("Tracker"));
        Tracker_output.color = trackerCount >= 1 ? rightColor : wrongColor;
        TrackerCount.text = $"({trackerCount})";

        // Count total connected devices
        int count = (IAServerStatus ? 1 : 0)
            + (IsEMGDatareceived ? 1 : 0)
            + (IsTactorConnected ? 1 : 0)
            + (hasHeadset ? 1 : 0)
            + baseStationCount
            + trackerCount;

        string connectionCountText = count == 0 ? "No connection" :
            $"{count} connection{(count > 1 ? "s" : "")}";

        maxButtonText.text = connectionCountText;
        minButtonText.text = connectionCountText;
    }


    IEnumerator PingAIServer()
    {
        using (UnityWebRequest www = new UnityWebRequest("http://127.0.0.1:8000/ping", "GET"))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            www.timeout = 2; // UnityWebRequest timeout is in seconds and must be an integer
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.ProtocolError)
            {
                IAServerStatus = false;
            }
            else
            {
                IAServerStatus = true;
            }
        }
    }

    private bool GetTactorConnected()
    {
        return tactorConnector != null && tactorConnector.IsConnected();
    }

    private bool IsEMGDataUpdated()
    {
        bool isEMGDataUpdated = !emgDataProcessor.GetRawEMGData().SequenceEqual(previusEMGData); // Check if current EMG data differs from previous
        previusEMGData = (int[])emgDataProcessor.GetRawEMGData().Clone(); // Clone to avoid reference issues

        return isEMGDataUpdated;
    }

    private string GetDeviceStringList(List<InputDevice> steamVRDevices)
    {
        if (steamVRDevices.Count == 0) return "No SteamVR devices connected.";

        return string.Join(
            System.Environment.NewLine,
            steamVRDevices.Select(d => $"- {d.name ?? "Unknown"} ({d.manufacturer ?? "Unknown"})")
        );
    }

    private List<InputDevice> GetConnectedSteamVRDevices()
    {
        List<InputDevice> devices = new List<InputDevice>();
        List<InputDevice> steamVRDevices = new List<InputDevice>();
        InputDevices.GetDevices(devices);

        foreach (InputDevice device in devices)
        {
            // Check if device is a SteamVR device by manufacturer or characteristics
            if (device.manufacturer != null && (device.manufacturer.ToLower().Contains("valve") || device.manufacturer.ToLower().Contains("htc") || device.manufacturer.ToLower().Contains("htc_rr")))
            {
                steamVRDevices.Add(device);
            }
        }

        return steamVRDevices;
    }
}
