using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SampleLogger : MonoBehaviour
{
    [SerializeField] protected float samplingFrequency = 0.02f;
    [SerializeField] public DataProvider[] dataProviders; // Array of data providers to gather data from

    private TrackerHub trackerHub;
    private LoggingManager loggingManager;

    private bool isLoggingStarted = false;

    // Start is called before the first frame update
    void Awake()
    {
        loggingManager = GetComponent<LoggingManager>();
        trackerHub = GetComponent<TrackerHub>();
    }

    void Start()
    {
        loggingManager.Log("Meta", "SamplingFrequency", samplingFrequency);
    }

    public void OnGameDirectorStateUpdate(GameDirector.GameState newState)
    {
        switch (newState)
        {
            case GameDirector.GameState.Stopped:
                FinishLogging();
                break;
            case GameDirector.GameState.Playing:
                StartLogging();
                break;
            case GameDirector.GameState.Paused:
                // TODO
                break;
        }
    }

    public void StartLogging()
    {
        if (isLoggingStarted) return; // If the sample logger is already started, return. To avoid some useless GC alloc.

        trackerHub.StartTrackers();
        StartCoroutine("SampleLog", samplingFrequency);
        isLoggingStarted = true;
    }

    public void FinishLogging()
    {
        GetComponent<TrackerHub>().StopTrackers();
        StopCoroutine("SampleLog");
        isLoggingStarted = false;
    }

    // Generates a "logs" row (see class description) from the given datas. Adds mandatory parameters and
    // the PersistentEvents parameters to the row when generating it.
    private IEnumerator SampleLog(float sampleFreq)
    {
        while (true)
        {
            Dictionary<string, object> sampleLog = new Dictionary<string, object>() {
                {"Event", "Sample"},
            };

            // Adds the parameters of the objects tracked by the TrackerHub's trackers
            Dictionary<string, object> trackedLogs = trackerHub.GetTracks();
            foreach (KeyValuePair<string, object> pair in trackedLogs)
            {
                sampleLog[pair.Key] = pair.Value;
            }

            // Adds the parameters from all DataProvider components
            foreach (DataProvider provider in dataProviders)
            {
                if (provider != null)
                {
                    Dictionary<string, object> providerLogs = provider.GetData();
                    foreach (KeyValuePair<string, object> pair in providerLogs)
                    {
                        sampleLog[pair.Key] = pair.Value;
                    }
                }
            }
            loggingManager.Log("Sample", sampleLog);

            yield return new WaitForSeconds(sampleFreq);
        }
    }
}

