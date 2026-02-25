using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoSaveAndQuit : MonoBehaviour
{
    [SerializeField]
    LoggingManager logManager;
    [SerializeField]
    SampleLogger logger;
    private void Start()
    {
        logManager.CreateLog("Sample");
        logger.StartLogging();
    }

    public void SaveAndStop()
    {
        logger.FinishLogging();
        logManager.SaveLog("Sample", clear: true);
#if UNITY_STANDALONE
        Application.Quit();
#endif
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
