using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Valve.VR;

public class HeadSetManager : MonoBehaviour
{
    private TherapistUi therapistUi;

    void Awake()
    {
        therapistUi = FindObjectOfType<TherapistUi>();
        UnityEngine.XR.InputDevices.deviceConnected += OnDeviceConnected;
        UnityEngine.XR.InputDevices.deviceDisconnected += OnDeviceDisconnected;
    }

    void OnDeviceConnected(UnityEngine.XR.InputDevice action)
    {
        int a = (action.name + action.serialNumber).GetHashCode();
        OnDeviceConnected(a, true);
        return;
    }

    void OnDeviceDisconnected(UnityEngine.XR.InputDevice action)
    {
        int a = (action.name + action.serialNumber).GetHashCode();
        OnDeviceConnected(a, false);
        return;
    }

    void OnDeviceConnected(int index, bool connected)
    {
        if (OpenVR.System.IsTrackedDeviceConnected((uint)index))
        {
            UpdateDeviceStatus(true);
        }
        else
        {
            UpdateDeviceStatus(false);
        }
    }

    private void UpdateDeviceStatus(bool doHaveDevice)
    {
        therapistUi.UpdateDeviceWarningDisplay(!doHaveDevice);
    }
}
