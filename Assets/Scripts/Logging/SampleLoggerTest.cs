using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using UnityEngine;

public class sampleLoggerTest : SampleLogger
{
    private int count = 0;
    [SerializeField]
    private int MAX_COUNT = 60;

    private LoggingManager loggingManager;
    void Awake()
    {
        loggingManager = GetComponent<LoggingManager>();
    }

    // Start is called before the first frame update
    void Start()
    {
        loggingManager.CreateLog("Sample");
        loggingManager.Log("Meta", "SamplingFrequency", samplingFrequency);
        StartLogging();
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("===HERE===");
        Debug.Log(GetComponent<TrackerHub>());
        if (count < MAX_COUNT)
        {
            Debug.Log("Increasing count");
            count++;
        }
        else
        {
            Debug.Log("Finishing");
            FinishLogging();
#if UNITY_STANDALONE
            Application.Quit();
#endif 
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }

    public void FinishLogging()
    {
        base.FinishLogging();
        Debug.Log(loggingManager.logsList["Sample"].RowCount);
        loggingManager.SaveLog("Sample", clear: true);
    }
}
