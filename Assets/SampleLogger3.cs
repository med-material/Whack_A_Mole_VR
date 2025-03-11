using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class SampleLogger3 : MonoBehaviour
{
    bool log = true;
    int samplingFrequency = 5; // 5ms, = 125Hz, 4ms = 200Hz, 2ms = 500Hz
    List<int> numbers = new List<int>();
    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    Task sampleTask;
    Stopwatch writeStopwatch = new Stopwatch();
    private LoggingManager loggingManager;
    private bool manualFramecount = true;

    void Start ()
    {
        StartLog();
    }

    // Start is called before the first frame update
    void OnApplicationQuit ()
    {
        StopLog();
    }

    public void StartLog() {
        loggingManager = GetComponent<LoggingManager>();
        loggingManager.CreateLog("Sample", headers: new List<string>() {"Event","TestVar"}, manualFramecount);
        writeStopwatch.Start();
        sampleTask = SampleLog(cancellationTokenSource.Token);
    }

    public void StopLog() {
        cancellationTokenSource.Cancel();
        sampleTask.Wait();
        writeStopwatch.Stop();
        loggingManager.SaveLog("Sample", false);
        TimeSpan writeTs = writeStopwatch.Elapsed;
        string writeElapsedTime = String.Format("{0:00}:{1:0000}",
            writeTs.Seconds, writeTs.Milliseconds);
        Debug.Log(" numbers appended in " + writeElapsedTime);
        Debug.Log(numbers.Count);
    }

    // Generates a "logs" row (see class description) from the given datas. Adds mandatory parameters and 
    // the PersistentEvents parameters to the row when generating it.
    public Task SampleLog(CancellationToken token)
    {
        return Task.Run(() =>
                {
                    int testVar = 0;
                    while (true) {
                        Dictionary<string, object> sampleLog = new Dictionary<string, object>() {
                            {"Event", "Sample"},
                            {"TestVar", testVar},
                        };

                        loggingManager.Log("Sample", sampleLog);
                        testVar++;

                        // Check if cancellation is requested
                        if (token.IsCancellationRequested)
                        {
                            Console.WriteLine("Task has been canceled.");
                            break;  // Exit the task if cancellation is requested
                        }

                        Thread.Sleep(samplingFrequency); 
                    }
                });
    }

}
