using System.Collections.Generic;
using UnityEngine;

public class HapticLogger : DataProvider
{
    public GameObject hapticTactorManager;
    [SerializeField] private TactorConnector connector;

    public override Dictionary<string, object> GetData()
    {
        // Basic safe defaults
        if (connector == null) Debug.LogError("[HapticLogger]: No TactorConnector found on HapticTactorManager GameObject.");

        // Build the dictionary with per-tactor columns.
        Dictionary<string, object> data = new Dictionary<string, object>();
        TactorValues[] tactorValues = connector.currentValues ?? new TactorValues[0];

        for (int i = 0; i < tactorValues.Length; i++)
        {
            data[$"Haptic_Tactor{i + 1}_Frequency"] = tactorValues[i].frequency;
            data[$"Haptic_Tactor{i + 1}_Gain"] = tactorValues[i].gain;
            data[$"Haptic_Tactor{i + 1}_Duration"] = tactorValues[i].duration;
            data[$"Haptic_Tactor{i + 1}_Active"] = (tactorValues[i].gain != 0 || tactorValues[i].frequency != 0 || tactorValues[i].duration != 0) ? 1 : 0;

        }
        return data;
    }
}
