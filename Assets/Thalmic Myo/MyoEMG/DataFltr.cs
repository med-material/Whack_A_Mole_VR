using UnityEngine;
using UnityEditor;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;

using System.Diagnostics;


public class DataFltr : MonoBehaviour
{
    private int fr;
    private string[] avg_timestamp;

    // ==================================== Simple "Block" Avg ====================================
    public string[] BlockAvg(int frameSize, int[] data)
    {
        // Debug.Log("Check size of data here: " + data.Length);

        int sum = 0;
        string[] avgPoints = new string[data.Length / frameSize];
        for (int counter = 0; counter < (data.Length / frameSize); counter++)   // Start Outer Loop, loop should run till the index where index + framesize 
                                                                                // does not throw array index out of bound exception. So it should run till 
                                                                                // counter < Total Array Length / FrameSize
        {
            for (int i = 0; i < frameSize; i++) {       // We start from the second element as the first one is the header
                sum = sum + data[i + (counter*frameSize) + 1];
            }

            int avg = sum / (frameSize-1);
            string avg_str = avg.ToString();
            avgPoints[counter] = avg_str;   // Find the avg and store it in another array which holds result

            sum = 0;
        }

        return avgPoints;
    }

    // ==================================== Simple Moving Avg Filter ====================================
    public float MovingAvg(int frameSize, List<int> data)
    {
        float sum = 0;
        int len = data.Count;
        //string[] avgPoints = new string[data.Length - frameSize + 1];
        for (int index = len - frameSize; index < len; index++)
        {
            sum = sum + Math.Abs(data[index-1]);
            // string result_str = result.ToString();
        }

        float result = sum / frameSize;
        sum = 0;

        return result;
    }
}