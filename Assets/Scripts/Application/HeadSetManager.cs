using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class HeadSetManager : MonoBehaviour
{
    private TherapistUi therapistUi;

    
    void Awake()
    {
        therapistUi = FindObjectOfType<TherapistUi>();
        SteamVR_Events.DeviceConnected.Listen(OnDeviceConnected);
    }

    void OnDeviceConnected(int index, bool connected)
    {
        if (OpenVR.System.IsTrackedDeviceConnected((uint)index))  UpdateDeviceStatus(true);
        else  UpdateDeviceStatus(false);
    }

    private void UpdateDeviceStatus(bool doHaveDevice)
    {
        if (doHaveDevice)
        {
            therapistUi.UpdateDeviceWarningDisplay(false);
        }
        else
        {
            therapistUi.UpdateDeviceWarningDisplay(true);
        }
    }
}
