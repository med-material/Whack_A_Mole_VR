using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR;

public class DevMonitoring : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI tempOutput;
    List<InputDevice> steamVRDevices = new List<InputDevice>();
    public bool IAServerStatus;

    void Start()
    {
        if (tempOutput != null) Debug.LogError("[DevMonitoring] Output is not assigned in the inspector.");
        InvokeRepeating(nameof(RepeatedMethod), 0f, 2f); // Update status only one time avery 2 seconds
    }

    void RepeatedMethod()
    {
        StartCoroutine(PingAIServer());
        UpdateDeviceList();
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

    private void UpdateDeviceList()
    {
        steamVRDevices = GetConnectedSteamVRDevices();

        if (steamVRDevices.Count == 0)
        {
            tempOutput.text = "No SteamVR devices connected.";
        }
        else
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("Connected SteamVR Devices:");
            foreach (InputDevice device in steamVRDevices)
            {
                sb.AppendLine($"- {device.name} ({device.manufacturer})");
            }
            tempOutput.text = sb.ToString();
        }
    }

    public List<InputDevice> GetConnectedSteamVRDevices() // !!!! TODO: Not tested on actual hardware yet
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevices(devices);
        List<InputDevice> steamVRDevices = new List<InputDevice>();

        foreach (InputDevice device in devices)
        {
            // Check if device is a SteamVR device by manufacturer or characteristics
            if (device.manufacturer != null && device.manufacturer.ToLower().Contains("valve"))
            {
                steamVRDevices.Add(device);
            }
        }

        return steamVRDevices;
    }
}
