using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class SaveRoutine : MonoBehaviour
{
    // EMG Data Holders
    private List<int> raw_emg_Pod01;
    private List<int> raw_emg_Pod02;
    private List<int> raw_emg_Pod03;
    private List<int> raw_emg_Pod04;
    private List<int> raw_emg_Pod05;
    private List<int> raw_emg_Pod06;
    private List<int> raw_emg_Pod07;
    private List<int> raw_emg_Pod08;
    private List<DateTime> raw_emg_time;

    private List<float> prc_emg_Pod01;
    private List<float> prc_emg_Pod02;
    private List<float> prc_emg_Pod03;
    private List<float> prc_emg_Pod04;
    private List<float> prc_emg_Pod05;
    private List<float> prc_emg_Pod06;
    private List<float> prc_emg_Pod07;
    private List<float> prc_emg_Pod08;
    private List<DateTime> prc_emg_time;

    // Name of the CSV file to be created
    public static string filename;

    // Define a save switch --> 0 = idle, 1 = start recording, 2 = start saving into CSV, 3 = saving, 4 = saved
    public int saveSwitch;


    private void Start()
    {
        saveSwitch = 0;
    }

    void Update()
    {
        // Empty data holders before you start recording data
        if (saveSwitch == 1)
        {
            resetEMGholders();
        }

        // If saving is called
        if (saveSwitch == 2)
        {
            saveSwitch = 3;                 // Avoids it entering a saving loop and moving to step 2

            // Define the filename (unique by using timestamps, which avoids accidental overwriting)
            string shortFilename = "RawEMG-.csv";
            filename = AppendTimeStamp(shortFilename);

            // Only the raw data is being recorded. The data will be further processed offline in MatLab
            rawEMGRoutine(filename);        // Uncomment if saving the raw EMG
            //prcEMGRoutine(filename);      // Uncomment if saving the processed or filtered EMG

            saveSwitch = 4;                 // Notify ClientRoutine_KIRA.cs that the file has been saved
        }
    }

    // ============================== Save raw values to CSV ==============================
    public void rawEMGRoutine(string filename)
    {
        // Get raw EMG pod data values
        raw_emg_Pod01 = StoreEMG.storeEMG01;
        raw_emg_Pod02 = StoreEMG.storeEMG02;
        raw_emg_Pod03 = StoreEMG.storeEMG03;
        raw_emg_Pod04 = StoreEMG.storeEMG04;
        raw_emg_Pod05 = StoreEMG.storeEMG05;
        raw_emg_Pod06 = StoreEMG.storeEMG06;
        raw_emg_Pod07 = StoreEMG.storeEMG07;
        raw_emg_Pod08 = StoreEMG.storeEMG08;
        raw_emg_time = StoreEMG.timestamp;
        UnityEngine.Debug.Log("Raw EMG done");

        // The sizes of EMG 01 and 06 are +1 element bigger than the others
        // This is fixed by trimming them in the savePrc function


        // ------------------------- Raw EMG -------------------------
        // Write raw EMG into a CSV file
        FunctionsCSV csv = new FunctionsCSV();
        csv.saveRawList(filename, raw_emg_Pod01, raw_emg_Pod02, raw_emg_Pod03, raw_emg_Pod04, raw_emg_Pod05, raw_emg_Pod06, raw_emg_Pod07, raw_emg_Pod08, raw_emg_time);
        UnityEngine.Debug.Log("Raw EMG CSV file created!");
    }


    // ============================== Save moving average values to CSV by name ==============================
    public void prcEMGRoutine(string filename)
    {
        // Get raw EMG pod data values
        raw_emg_Pod01 = StoreEMG.storeEMG01;
        raw_emg_Pod02 = StoreEMG.storeEMG02;
        raw_emg_Pod03 = StoreEMG.storeEMG03;
        raw_emg_Pod04 = StoreEMG.storeEMG04;
        raw_emg_Pod05 = StoreEMG.storeEMG05;
        raw_emg_Pod06 = StoreEMG.storeEMG06;
        raw_emg_Pod07 = StoreEMG.storeEMG07;
        raw_emg_Pod08 = StoreEMG.storeEMG08;
        raw_emg_time = StoreEMG.timestamp;

        // Only the raw data is being recorded. The data will be further processed offline in MatLab

        // Get processed EMG pod data values
        prc_emg_Pod01 = EMG01_Controller.avg_emg_Pod01;    // To plot the live EMG graphs, change the "LineChartController_EMG0X_Plotless" to "LineChartController_EMG0X"
        prc_emg_Pod02 = EMG02_Controller.avg_emg_Pod02;
        prc_emg_Pod03 = EMG03_Controller.avg_emg_Pod03;
        prc_emg_Pod04 = EMG04_Controller.avg_emg_Pod04;
        prc_emg_Pod05 = EMG05_Controller.avg_emg_Pod05;
        prc_emg_Pod06 = EMG06_Controller.avg_emg_Pod06;
        prc_emg_Pod07 = EMG07_Controller.avg_emg_Pod07;
        prc_emg_Pod08 = EMG08_Controller.avg_emg_Pod08;
        UnityEngine.Debug.Log("Processed EMG done");

        // The sizes of EMG 01 and 06 are +1 element bigger than the others due to the Myo firmware
        // This is fixed by trimming them in the savePrc function

        // ------------------------- Raw EMG -------------------------
        // Write raw EMG into a CSV file
        FunctionsCSV csv = new FunctionsCSV();
        csv.saveRawList(filename, raw_emg_Pod01, raw_emg_Pod02, raw_emg_Pod03, raw_emg_Pod04, raw_emg_Pod05, raw_emg_Pod06, raw_emg_Pod07, raw_emg_Pod08, raw_emg_time);
        UnityEngine.Debug.Log("Raw EMG CSV created!");

        // ------------------------- Processed EMG -------------------------
        // Read timestamps for processed EMG
        var values = csv.readEMGCSV("RawEMG_Data.csv");
        int len = prc_emg_Pod01.Count;
        UnityEngine.Debug.Log("Processed EMG CSV creating... (1/3)");

        // Convert string back to timestamp for CSV. 
        // Avoids the output in a CSV being System.string[] instead of the actual timestamp
        DateTime[] prc_emg_time = new DateTime[len];
        string[] emg_time = values.Item9;

        int counter = 0;
        for (int i = 1; i < len + 1; i++)
        {
            prc_emg_time[counter] = DateTime.ParseExact(emg_time[i], "yyyy-MM-dd H:mm:ss.fff", null);
            counter = counter + 1;
        }
        UnityEngine.Debug.Log("Processed EMG CSV creating... (2/3)");

        // Write processed EMG into a CSV file
        csv.savePrc("EMG_processed.csv", prc_emg_Pod01, prc_emg_Pod02, prc_emg_Pod03, prc_emg_Pod04, prc_emg_Pod05, prc_emg_Pod06, prc_emg_Pod07, prc_emg_Pod08, prc_emg_time);
        UnityEngine.Debug.Log("Processed EMG CSV creating... (3/3)");

    }


    // ============================== Function to reset all variables that store data for the CSV ==============================
    public void resetEMGholders()
    {
        // Empty raw EMG data holders
        StoreEMG.storeEMG01.Clear();
        StoreEMG.storeEMG02.Clear();
        StoreEMG.storeEMG03.Clear();
        StoreEMG.storeEMG04.Clear();
        StoreEMG.storeEMG05.Clear();
        StoreEMG.storeEMG06.Clear();
        StoreEMG.storeEMG07.Clear();
        StoreEMG.storeEMG08.Clear();
        StoreEMG.timestamp.Clear();


        // Empty processed EMG data holders
        EMG01_Controller.avg_emg_Pod01.Clear();
        EMG02_Controller.avg_emg_Pod02.Clear();
        EMG03_Controller.avg_emg_Pod03.Clear();
        EMG04_Controller.avg_emg_Pod04.Clear();
        EMG05_Controller.avg_emg_Pod05.Clear();
        EMG06_Controller.avg_emg_Pod06.Clear();
        EMG07_Controller.avg_emg_Pod07.Clear();
        EMG08_Controller.avg_emg_Pod08.Clear();
    }

    // ============================== Append timestamp to filename ==============================
    public string AppendTimeStamp(string fileName)
    {
        return string.Concat(
            Path.GetFileNameWithoutExtension(fileName),
            DateTime.Now.ToString("yyyy-MM-dd-HH.mm.ss"),
            Path.GetExtension(fileName)
            );
    }

}
