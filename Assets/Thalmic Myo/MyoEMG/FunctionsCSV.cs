using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System;
using System.Linq;

public class FunctionsCSV : MonoBehaviour
{

    // Parameters for saving to CSV
    private List<string[]> rowHeader = new List<string[]>();
    private List<int[]> emg_data = new List<int[]>();
    private string newHeader;
    private string newLine;

    private int emg_Pod01;
    private int emg_Pod02;
    private int emg_Pod03;
    private int emg_Pod04;
    private int emg_Pod05;
    private int emg_Pod06;
    private int emg_Pod07;
    private int emg_Pod08;
    private DateTime timestamp;

    private int n_windows;

    // Parameters for reading CSV
    private int[] i_emg_Pod01;
    private int[] i_emg_Pod02;
    private int[] i_emg_Pod03;
    private int[] i_emg_Pod04;
    private int[] i_emg_Pod05;
    private int[] i_emg_Pod06;
    private int[] i_emg_Pod07;
    private int[] i_emg_Pod08;
    private string[] emg_time;

    private List<float> avg_emg_Pod01 = new List<float>();
    private List<float> avg_emg_Pod02 = new List<float>();
    private List<float> avg_emg_Pod03 = new List<float>();
    private List<float> avg_emg_Pod04 = new List<float>();
    private List<float> avg_emg_Pod05 = new List<float>();
    private List<float> avg_emg_Pod06 = new List<float>();
    private List<float> avg_emg_Pod07 = new List<float>();
    private List<float> avg_emg_Pod08 = new List<float>();
    private List<float> avg_emg_time;


    // ==================================== Save raw EMG to CSV file (array) ====================================
    public void saveRawArray(string filename, int[] emg_list, DateTime timestamp)
    {
        string[] rowDataTemp = new string[9];

        emg_Pod01 = emg_list[0];
        emg_Pod02 = emg_list[1];
        emg_Pod03 = emg_list[2];
        emg_Pod04 = emg_list[3];
        emg_Pod05 = emg_list[4];
        emg_Pod06 = emg_list[5];
        emg_Pod07 = emg_list[6];
        emg_Pod08 = emg_list[7];
        //emg_Pod08 = 0;

        rowDataTemp[0] = emg_Pod01.ToString();
        rowDataTemp[1] = emg_Pod02.ToString();
        rowDataTemp[2] = emg_Pod03.ToString();
        rowDataTemp[3] = emg_Pod04.ToString();
        rowDataTemp[4] = emg_Pod05.ToString();
        rowDataTemp[5] = emg_Pod06.ToString();
        rowDataTemp[6] = emg_Pod07.ToString();
        rowDataTemp[7] = emg_Pod08.ToString();
        rowDataTemp[8] = timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");


        string newLine = rowDataTemp[0] + "," + rowDataTemp[1] + "," +
            rowDataTemp[2] + "," + rowDataTemp[3] + "," + rowDataTemp[4] + "," +
            rowDataTemp[5] + "," + rowDataTemp[6] + "," + rowDataTemp[7] + "," + rowDataTemp[8] +
            Environment.NewLine;

        string filePath = getPath(filename);

        // If the file doesn't exist, create it and add header
        if (!File.Exists(filePath))
        {
            // Creating First row of titles 
            string[] rowHeader = new string[9];

            rowHeader[0] = "EMG - Pod01";
            rowHeader[1] = "EMG - Pod02";
            rowHeader[2] = "EMG - Pod03";
            rowHeader[3] = "EMG - Pod04";
            rowHeader[4] = "EMG - Pod05";
            rowHeader[5] = "EMG - Pod06";
            rowHeader[6] = "EMG - Pod07";
            rowHeader[7] = "EMG - Pod08";
            rowHeader[8] = "Timestamp";

            string newHeader = rowHeader[0] + "," + rowHeader[1] + "," +
                rowHeader[2] + "," + rowHeader[3] + "," + rowHeader[4] + "," +
                rowHeader[5] + "," + rowHeader[6] + "," + rowHeader[7] + "," + rowHeader[8] +
                Environment.NewLine;

            File.WriteAllText(filePath, newHeader);
        }

        File.AppendAllText(filePath, newLine);
    }


    // ==================================== Save raw EMG to CSV file (list) ====================================
    public void saveRawList(string filename, List<int> dat_01, List<int> dat_02, List<int> dat_03, List<int> dat_04, List<int> dat_05, List<int> dat_06, List<int> dat_07, List<int> dat_08, List<DateTime> dat_time)
    {
        // Identify the array with the least elements
        int[] compareLen = { dat_01.Count, dat_02.Count, dat_03.Count, dat_04.Count, dat_05.Count, dat_06.Count, dat_07.Count, dat_08.Count };
        int len = compareLen.Min();

        // Trim to size
        dat_01 = TrimToSizeInt(dat_01, len);
        dat_02 = TrimToSizeInt(dat_02, len);
        dat_03 = TrimToSizeInt(dat_03, len);
        dat_04 = TrimToSizeInt(dat_04, len);
        dat_05 = TrimToSizeInt(dat_05, len);
        dat_06 = TrimToSizeInt(dat_06, len);
        dat_07 = TrimToSizeInt(dat_07, len);
        dat_08 = TrimToSizeInt(dat_08, len);


        // Define Jagged array
        int[][] jagged_dat = new int[8][]
        {
            dat_01.ToArray(),
            dat_02.ToArray(),
            dat_03.ToArray(),
            dat_04.ToArray(),
            dat_05.ToArray(),
            dat_06.ToArray(),
            dat_07.ToArray(),
            dat_08.ToArray()
        };

        // Do the same for the timestamp array
        DateTime[] newTime_dat = new DateTime[len];

        if (dat_time.Count != len)
        {
            // Return new array with correct length
            int counter = 0;
            for (int idx = dat_time.Count - len - 1; idx < len; idx++)
            {
                newTime_dat[counter] = dat_time[idx];
                counter = counter + 1;
            }

        }

        else
        {
            newTime_dat = dat_time.ToArray();
            UnityEngine.Debug.Log("Length of the EMG arrays and timestamp arrays are the same");
        }

        // Prepare data to be converted to string
        string[] rowDataTemp = new string[9];

        for (int i = 0; i < len; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                rowDataTemp[j] = jagged_dat[j][i].ToString();
            }
            rowDataTemp[8] = newTime_dat[i].ToString("yyyy-MM-dd HH:mm:ss.fff");

            string newLine = rowDataTemp[0] + "," + rowDataTemp[1] + "," +
            rowDataTemp[2] + "," + rowDataTemp[3] + "," + rowDataTemp[4] + "," +
            rowDataTemp[5] + "," + rowDataTemp[6] + "," + rowDataTemp[7] + "," + rowDataTemp[8] +
            Environment.NewLine;

            string filePath = getPath(filename);

            // If the file doesn't exist, create it and add header
            if (!File.Exists(filePath))
            {
                // Creating First row of titles 
                string[] rowHeader = new string[9];

                rowHeader[0] = "Raw EMG - Pod01";
                rowHeader[1] = "Raw EMG - Pod02";
                rowHeader[2] = "Raw EMG - Pod03";
                rowHeader[3] = "Raw EMG - Pod04";
                rowHeader[4] = "Raw EMG - Pod05";
                rowHeader[5] = "Raw EMG - Pod06";
                rowHeader[6] = "Raw EMG - Pod07";
                rowHeader[7] = "Raw EMG - Pod08";
                rowHeader[8] = "Timestamp";

                string newHeader = rowHeader[0] + "," + rowHeader[1] + "," +
                    rowHeader[2] + "," + rowHeader[3] + "," + rowHeader[4] + "," +
                    rowHeader[5] + "," + rowHeader[6] + "," + rowHeader[7] + "," + rowHeader[8] +
                    Environment.NewLine;

                File.WriteAllText(filePath, newHeader);
            }

            File.AppendAllText(filePath, newLine);
        }

    }


    // ==================================== Save processed EMG to CSV file ====================================
    public void savePrc(string filename, List<float> dat_01, List<float> dat_02, List<float> dat_03, List<float> dat_04, List<float> dat_05, List<float> dat_06, List<float> dat_07, List<float> dat_08, DateTime[] dat_time)
    {
        // Identify the array with the least elements
        int[] compareLen = { dat_01.Count, dat_02.Count, dat_03.Count, dat_04.Count, dat_05.Count, dat_06.Count, dat_07.Count, dat_08.Count };
        int len = compareLen.Min();
        UnityEngine.Debug.Log("Minimum array length: " + len);

        // It seems like EMG data array 01 and 06 are always +1 element bigger than the other. If so, trim to size
        dat_01 = TrimToSizeFloat(dat_01, len);
        dat_02 = TrimToSizeFloat(dat_02, len);
        dat_03 = TrimToSizeFloat(dat_03, len);
        dat_04 = TrimToSizeFloat(dat_04, len);
        dat_05 = TrimToSizeFloat(dat_05, len);
        dat_06 = TrimToSizeFloat(dat_06, len);
        dat_07 = TrimToSizeFloat(dat_07, len);
        dat_08 = TrimToSizeFloat(dat_08, len);

        // Check in terminal that the size of the timestamp array is the same
        if (dat_time.Length == len)
        {
            UnityEngine.Debug.Log("Length of the timestamp array and emg array are the same");
        }
        else
        {
            UnityEngine.Debug.Log("Length of the timestamp array and emg array are NOT the same");
        }

        // Define Jagged array
        float[][] jagged_dat = new float[8][]
        {
            dat_01.ToArray(),
            dat_02.ToArray(),
            dat_03.ToArray(),
            dat_04.ToArray(),
            dat_05.ToArray(),
            dat_06.ToArray(),
            dat_07.ToArray(),
            dat_08.ToArray()
        };

        // Prepare data to be converted to string
        string[] rowDataTemp = new string[9];

        for (int i = 0; i < len; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                rowDataTemp[j] = jagged_dat[j][i].ToString();
            }
            rowDataTemp[8] = dat_time[i].ToString("yyyy-MM-dd HH:mm:ss.fff");

            string newLine = rowDataTemp[0] + "," + rowDataTemp[1] + "," +
            rowDataTemp[2] + "," + rowDataTemp[3] + "," + rowDataTemp[4] + "," +
            rowDataTemp[5] + "," + rowDataTemp[6] + "," + rowDataTemp[7] + "," + rowDataTemp[8] +
            Environment.NewLine;

            string filePath = getPath(filename);

            // If the file doesn't exist, create it and add header
            if (!File.Exists(filePath))
            {
                // Creating First row of titles 
                string[] rowHeader = new string[9];

                rowHeader[0] = "Avg EMG - Pod01";
                rowHeader[1] = "Avg EMG - Pod02";
                rowHeader[2] = "Avg EMG - Pod03";
                rowHeader[3] = "Avg EMG - Pod04";
                rowHeader[4] = "Avg EMG - Pod05";
                rowHeader[5] = "Avg EMG - Pod06";
                rowHeader[6] = "Avg EMG - Pod07";
                rowHeader[7] = "Avg EMG - Pod08";
                rowHeader[8] = "Start Timestamp";

                string newHeader = rowHeader[0] + "," + rowHeader[1] + "," +
                    rowHeader[2] + "," + rowHeader[3] + "," + rowHeader[4] + "," +
                    rowHeader[5] + "," + rowHeader[6] + "," + rowHeader[7] + "," + rowHeader[8] +
                    Environment.NewLine;

                File.WriteAllText(filePath, newHeader);
            }

            File.AppendAllText(filePath, newLine);
        }

    }

    List<int> TrimToSizeInt(List<int> myList, int mySize)
    {
        while (myList.Count > mySize)
        {
            myList.RemoveAt(myList.Count - 1);
        }

        return myList;
    }

    List<float> TrimToSizeFloat(List<float> myList, int mySize)
    {
        while (myList.Count > mySize)
        {
            myList.RemoveAt(myList.Count - 1);
        }

        return myList;
    }

    // ==================================== Read from CSV file ====================================
    public (int[], int[], int[], int[], int[], int[], int[], int[], string[]) readEMGCSV(string filename)
    {
        FunctionsCSV csv = new FunctionsCSV();
        string filePath = csv.getPath(filename);

        List<string> col_01 = new List<string>();
        List<string> col_02 = new List<string>();
        List<string> col_03 = new List<string>();
        List<string> col_04 = new List<string>();
        List<string> col_05 = new List<string>();
        List<string> col_06 = new List<string>();
        List<string> col_07 = new List<string>();
        List<string> col_08 = new List<string>();
        List<string> col_09 = new List<string>();

        using (var reader = new StreamReader(filePath))
        {

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');

                col_01.Add(values[0]);
                col_02.Add(values[1]);
                col_03.Add(values[2]);
                col_04.Add(values[3]);
                col_05.Add(values[4]);
                col_06.Add(values[5]);
                col_07.Add(values[6]);
                col_08.Add(values[7]);
                col_09.Add(values[8]);
            }

        }

        // Convert list to array
        string[] emg_Pod01 = col_01.ToArray();
        string[] emg_Pod02 = col_02.ToArray();
        string[] emg_Pod03 = col_03.ToArray();
        string[] emg_Pod04 = col_04.ToArray();
        string[] emg_Pod05 = col_05.ToArray();
        string[] emg_Pod06 = col_06.ToArray();
        string[] emg_Pod07 = col_07.ToArray();
        string[] emg_Pod08 = col_08.ToArray();
        string[] emg_time = col_09.ToArray();


        int[] i_emg_Pod01 = arrStr2arrInt(emg_Pod01);
        int[] i_emg_Pod02 = arrStr2arrInt(emg_Pod02);
        int[] i_emg_Pod03 = arrStr2arrInt(emg_Pod03);
        int[] i_emg_Pod04 = arrStr2arrInt(emg_Pod04);
        int[] i_emg_Pod05 = arrStr2arrInt(emg_Pod05);
        int[] i_emg_Pod06 = arrStr2arrInt(emg_Pod06);
        int[] i_emg_Pod07 = arrStr2arrInt(emg_Pod07);
        int[] i_emg_Pod08 = arrStr2arrInt(emg_Pod08);


        return (i_emg_Pod01, i_emg_Pod02, i_emg_Pod03, i_emg_Pod04, i_emg_Pod05, i_emg_Pod06, i_emg_Pod07, i_emg_Pod08, emg_time);
    }

    // ==================================== Save Path ====================================
    // Following method is used to retrive the relative path as device platform
    public string getPath(string filename)
    {
#if UNITY_EDITOR
        return Application.dataPath + "/Thalmic Myo/MyoEMG/CSV/" + filename;
#elif UNITY_ANDROID
                return Application.persistentDataPath+filename;
#elif UNITY_IPHONE
                return Application.persistentDataPath+"/"+filename;
#else
                return Application.dataPath +"/"+filename;
#endif
    }

    // ==================================== Convert string arrays into int[] ====================================
    // Elapsed time for all each array converted from string to int: 15 ms approx for one Pod and 16,886 rows 
    public int[] arrStr2arrInt(string[] data)
    {
        int length = data.Length;
        int[] arrInt = new int[length];

        for (int i = 1; i < length; i++)    // Omit the first value as it will be the header
        {
            var number = Convert.ToInt32(data[i]);
            arrInt[i] = number;
        }

        return arrInt;
    }

}