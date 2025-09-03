using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EMGDataProcessor : MonoBehaviour
{
    [SerializeField] private ThalmicMyo thalmicMyo;
    [SerializeField] private int smoothingWindowSize = 250;

    [SerializeField] private int[] rawEMGData = new int[8];
    [SerializeField] private float rawAbsAverage;
    [SerializeField] private float smoothedAbsAverage;

    private Queue<int[]> rawEMGDataBuffer = new Queue<int[]>();
    private Queue<float> rawAbsAverageBuffer = new Queue<float>();

    private void Start()
    {
        Debug.Assert(thalmicMyo != null, "ThalmicMyo reference is missing in EMGDataExposure.");
        thalmicMyo._myo.EmgData += onReceiveData;
    }

    private void onReceiveData(object sender, Thalmic.Myo.EmgDataEventArgs data)
    {
        // The EMG Pod 08 is unreadable during the first loop iteration. i.e. In the first loop the emg[] size is 7, not 8
        if (data.Emg == null || data.Emg.Length != 8) return;

        rawEMGData = data.Emg;
        rawAbsAverage = (float)rawEMGData.Select(x => Mathf.Abs(x)).Average();

        // Update EMG data buffer
        rawEMGDataBuffer.Enqueue(rawEMGData);
        rawAbsAverageBuffer.Enqueue(rawAbsAverage);
        if (rawEMGDataBuffer.Count > smoothingWindowSize)
        {
            rawEMGDataBuffer.Dequeue();
            rawAbsAverageBuffer.Dequeue();
        }

        smoothedAbsAverage = rawAbsAverageBuffer.Average();
    }

    // ================ Getters ================
    public int[] GetRawEMGData() => rawEMGData;
    public float GetRawAbsAverage() => rawAbsAverage;
    public float GetSmoothedAbsAverage() => smoothedAbsAverage;
}
