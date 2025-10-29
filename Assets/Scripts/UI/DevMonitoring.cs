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
    [SerializeField] EMGDataProcessor emgDataProcessor;

    [SerializeField] TextMeshProUGUI VRDeviceString;
    [SerializeField] Image AI_output;
    [SerializeField] Image EMG_output;
    [SerializeField] Image HapticTactor_output;

    List<InputDevice> steamVRDevices = new List<InputDevice>();
    public bool IAServerStatus { get; private set; }
    public bool IsEMGDatareceived { get; private set; }

    private int[] previusEMGData = new int[8];

    void Start()
    {
        if (VRDeviceString == null) Debug.LogError("[DevMonitoring] Ouput reference for VR Device is missing.");
        if (AI_output == null) Debug.LogError("[DevMonitoring] Ouput reference for AI Server is missing.");
        if (EMG_output == null) Debug.LogError("[DevMonitoring] Ouput reference for EMG is missing.");
        if (HapticTactor_output == null) Debug.LogError("[DevMonitoring] Ouput reference for Haptic Tactor is missing.");

        InvokeRepeating(nameof(RepeatedMethod), 0f, 2f); // Update status only one time every 2 seconds
    }

    void RepeatedMethod()
    {
        StartCoroutine(PingAIServer());
        steamVRDevices = GetConnectedSteamVRDevices();
        IsEMGDatareceived = isEMGDataUpdated();

        UpdateDevUI();
    }

    private void UpdateDevUI()
    {
        AI_output.GetComponent<Image>().color = IAServerStatus ? Color.green : Color.red;
        EMG_output.GetComponent<Image>().color = IsEMGDatareceived ? Color.green : Color.red;
        HapticTactor_output.GetComponent<Image>().color = Color.red; // ----------------------------- TODO
        VRDeviceString.text = GetDeviceStringList(steamVRDevices);
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

    private bool isEMGDataUpdated()
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
            if (device.manufacturer != null && (device.manufacturer.ToLower().Contains("valve") || device.manufacturer.ToLower().Contains("htc")))
            {
                steamVRDevices.Add(device);
            }
        }

        return steamVRDevices;
    }
}
