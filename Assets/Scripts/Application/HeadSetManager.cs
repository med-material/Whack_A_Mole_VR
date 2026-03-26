using UnityEngine;
using Valve.VR;

public class HeadSetManager : MonoBehaviour
{
    private TherapistUi therapistUi;

    void Awake()
    {
        therapistUi = FindFirstObjectByType<TherapistUi>();
        SteamVR_Events.DeviceConnected.Listen(OnDeviceConnected);
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
