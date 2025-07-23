using UnityEngine;
using System.Collections.Generic;
using System;

public class StoreEMG : MonoBehaviour
{
    [SerializeField] public int currentEMG01;
    [SerializeField] public int currentEMG02;
    [SerializeField] public int currentEMG03;
    [SerializeField] public int currentEMG04;
    [SerializeField] public int currentEMG05;
    [SerializeField] public int currentEMG06;
    [SerializeField] public int currentEMG07;
    [SerializeField] public int currentEMG08;

    public static List<DateTime> storeTimestamp;
    public static List<int> storeEMG01 = new List<int>();
    public static List<int> storeEMG02 = new List<int>();
    public static List<int> storeEMG03 = new List<int>();
    public static List<int> storeEMG04 = new List<int>();
    public static List<int> storeEMG05 = new List<int>();
    public static List<int> storeEMG06 = new List<int>();
    public static List<int> storeEMG07 = new List<int>();
    public static List<int> storeEMG08 = new List<int>();
    public static List<DateTime> timestamp = new List<DateTime>();

    public void Update()
    {
        //storeData(ThalmicMyo.emg);
        updateData(ThalmicMyo.emg);
    }

    private void Start()
    {
        // TODO refaire l'init des list + en faire un obj struct ?
    }

    public void updateData(int[] emg)
    {
        // The EMG Pod 08 is unreadable during the first loop iteration. i.e. In the first loop the emg[] size is 7, not 8
        if (emg == null || emg.Length != 8)
        {
            Debug.LogWarning("EMG Data malformed: " + emg);
            return;
        }

        currentEMG01 = emg[0];
        currentEMG02 = emg[1];
        currentEMG03 = emg[2];
        currentEMG04 = emg[3];
        currentEMG05 = emg[4];
        currentEMG06 = emg[5];
        currentEMG07 = emg[6];
        currentEMG08 = emg[7];
    }

    public void storeData(int[] emg)
    {
        // The EMG Pod 08 is unreadable during the first loop iteration. i.e. In the first loop the emg[] size is 7, not 8
        if (emg == null || emg.Length != 8)
        {
            Debug.LogWarning("EMG Data malformed: " + emg);
            return;
        }

        // Store data in lists
        storeEMG01.Add(emg[0]);
        storeEMG02.Add(emg[1]);
        storeEMG03.Add(emg[2]);
        storeEMG04.Add(emg[3]);
        storeEMG05.Add(emg[4]);
        storeEMG06.Add(emg[5]);
        storeEMG07.Add(emg[6]);
        storeEMG08.Add(emg[7]);

        timestamp.Add(DateTime.Now);   // Get current local time and date
    }
}