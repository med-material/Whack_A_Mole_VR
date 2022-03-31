using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;


public class WriteToCSV
{
    private string fileName;
    private string savePath;
    private string filePath;

    private LogStore logStore;


    public WriteToCSV(LogStore logStore, string savePath, string filePrefix, string fileExtension)
    {
        this.fileName = filePrefix + "_" +
                        DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_ffff") + "_" + logStore.Label + fileExtension;
        Init(logStore, savePath);
    }

    public WriteToCSV(LogStore logStore, string savePath, string fileName)
    {
        this.fileName = fileName;
        Init(logStore, savePath);
    }

    public WriteToCSV(LogStore logStore, string filePath)
    {
        this.filePath = filePath;
        this.logStore = logStore;
    }

    private void Init(LogStore logStore, string path)
    {
        this.savePath = path == "" ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : path;
        this.filePath = Path.Combine(savePath, fileName);
        this.logStore = logStore;
    }

    //Writes all the logs into the file at the given filepath
    public void WriteAll(Action callback)
    {
        //This part has to be executed in the main Thread 
        if (logStore.CurrentLogRow.Count != 0)
        {
            logStore.EndRow();
        }

        new Thread(() =>
        {
            Write();
            logStore.RemoveSavingTarget(TargetType.CSV);
            callback();
        }).Start();
    }



    private void Write()
    {
        Stopwatch exportToStringStopwatch = new Stopwatch();
        exportToStringStopwatch.Start();

        string dataString = logStore.ExportAll<string>();

        exportToStringStopwatch.Stop();
        TimeSpan exportToStringTs = exportToStringStopwatch.Elapsed;
        string exportToStringElapsedTime = String.Format("{0:00}:{1:0000}",
            exportToStringTs.Seconds, exportToStringTs.Milliseconds);
        Debug.Log(logStore.Label + " string exported in " + exportToStringElapsedTime);

        string headers = logStore.GenerateHeaders();

        Stopwatch writeStopwatch = new Stopwatch();
        writeStopwatch.Start();

        try { 
            using (var file = new StreamWriter(filePath, true))
            {
                file.WriteLine(headers);
                file.Write(dataString);
            }
        } catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            Debug.Log("Attempting to log to Documents instead.");
            filePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), fileName);
            using (var file = new StreamWriter(filePath, true))
            {
                file.WriteLine(headers);
                file.Write(dataString);
            }
        }

        writeStopwatch.Stop();
        TimeSpan writeTs = writeStopwatch.Elapsed;
        string writeElapsedTime = String.Format("{0:00}:{1:0000}",
            writeTs.Seconds, writeTs.Milliseconds);
        Debug.Log(logStore.Label + " logs wrote to file in " + writeElapsedTime);
    }

}