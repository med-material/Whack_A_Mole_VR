using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;


public enum TargetType
{
    MySql,
    CSV
}

public class LoggingManager : MonoBehaviour
{
    // sampleLog[COLUMN NAME][COLUMN NO.] = [OBJECT] (fx a float, int, string, bool)
    private Dictionary<string, LogStore> logsList = new Dictionary<string, LogStore>();

    [Header("Logging Settings")]
    [Tooltip("The Meta Collection will contain a session ID, a device ID and a timestamp.")]
    [SerializeField]
    private bool CreateMetaCollection = true;

    [Header("MySQL Save Settings")]
    [SerializeField]
    private bool enableMySQLSave = true;
    [SerializeField]
    private string email = "anonymous";

    [SerializeField]
    private ConnectToMySQL connectToMySQL;


    [Header("CSV Save Settings")]
    [SerializeField]
    private bool enableCSVSave = true;

    [Header("Logging mode")]
    [Tooltip("If set to true, the logging process will be done over time, resulting in faster saving time.\n" +
             "If set to false, the logging process will use less ressources, but the logs will take more time to be saved")]
    [SerializeField]
    private bool logStringOverTime = true;


    [Tooltip("If save path is empty, it defaults to My Documents.")]
    [SerializeField]
    private string savePath = "";

    [SerializeField]
    private string filePrefix = "log";

    [SerializeField]
    private string fileExtension = ".csv";

    private string filePath;
    private char fieldSeperator = ';';
    private string sessionID = "";
    private string deviceID = "";
    private string filestamp;

    private List<TargetType> targetsEnabled;
    private Dictionary<string, Dictionary<TargetType, bool>> originsSavedPerLog;

    // Start is called before the first frame update
    void Awake()
    {
        targetsEnabled = new List<TargetType>();
        //Initializes the list of activated targets
        if (enableCSVSave)
        {
            targetsEnabled.Add(TargetType.CSV);
        }
        if (enableMySQLSave)
        {
            targetsEnabled.Add(TargetType.MySql);
        }

        NewFilestamp();
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        if (CreateMetaCollection)
        {
            //if the log added is the Meta one and doesn't exists, we create it
            //if (AddMetaCollectionToList())
            AddMetaCollectionToList();
            //AddToLogstore(logsList["Meta"], logData);
            //
            //}
        }

        if (savePath == "")
        {
            savePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        }
        
    }


    public void NewFilestamp()
    {
        sessionID = Guid.NewGuid().ToString();
        deviceID = SystemInfo.deviceUniqueIdentifier;
        foreach (var pair in logsList)
        {
            pair.Value.SessionId = sessionID;
        }
    }

    public void SetSavePath(string path)
    {
        this.savePath = path;
    }

    private void GenerateLogString(string collectionLabel, Action callback)
    {
        var context = System.Threading.SynchronizationContext.Current;
        new Thread(() =>
        {
            logsList[collectionLabel].ExportAll<string>();
            //runs the callback in the main Thread
            context.Post(_ =>
            {
                callback();
            }, null);

        }).Start();
    }


    public void SetEmail(string newEmail)
    {
        email = newEmail;
    }

    public void CreateLog(string collectionLabel, List<string> headers = null)
    {
        if (logsList.ContainsKey(collectionLabel))
        {
            Debug.LogWarning(collectionLabel + " already exists");
            return;
        }
        LogStore logStore = new LogStore(collectionLabel, email, sessionID, logStringOverTime, headers:headers);
        logsList.Add(collectionLabel, logStore);
    }


    public void Log(string collectionLabel, Dictionary<string, object> logData)
    {
        //checks if the log was created and creates it if not
        if (logsList.TryGetValue(collectionLabel, out LogStore logStore))
        {
            AddToLogstore(logStore, logData);
        }
        //this will be executed only once if the log has not been created.
        else
        {
            LogStore newLogStore = new LogStore(collectionLabel, email, sessionID, logStringOverTime);
            AddToLogstore(newLogStore, logData);
            logsList.Add(collectionLabel, newLogStore);
        }
    }

    private void AddToLogstore(LogStore logStore, Dictionary<string, object> logData)
    {
        foreach (KeyValuePair<string, object> pair in logData)
        {
            logStore.Add(pair.Key, pair.Value);
        }
        if (logStore.LogType == LogType.LogEachRow)
        {
            logStore.EndRow();
        }
    }

    public void Log(string collectionLabel, string columnLabel, object value)
    {
        //checks if the log was created and creates it if not
        if (logsList.TryGetValue(collectionLabel, out LogStore logStore))
        {
            AddToLogstore(logStore, columnLabel, value);
        }
        //this will be executed only once if the log has not been created.
        else
        {
            LogStore newLogStore = new LogStore(collectionLabel, email, sessionID, logStringOverTime);
            logsList.Add(collectionLabel, newLogStore);
            AddToLogstore(newLogStore, columnLabel, value);
        }
    }

    private void AddToLogstore(LogStore logStore, string columnLabel, object value)
    {
        logStore.Add(columnLabel, value);
        if (logStore.LogType == LogType.LogEachRow)
        {
            logStore.EndRow();
        }
    }

    //returns true if the Meta log was created
    private bool AddMetaCollectionToList()
    {
        if (logsList.ContainsKey("Meta"))
        {
            return false;
        }
        LogStore metaLog = new LogStore("Meta", email, sessionID, logStringOverTime, LogType.OneRowOverwrite);
        logsList.Add("Meta", metaLog);
        metaLog.Add("SessionID", sessionID);
        metaLog.Add("DeviceID", deviceID);
        return true;
    }



    public void ClearAllLogs()
    {
        foreach (KeyValuePair<string, LogStore> pair in logsList)
        {
            pair.Value.Clear();
        }
    }

    public void ClearLog(string collectionLabel)
    {
        if (logsList.ContainsKey(collectionLabel))
        {
            logsList[collectionLabel].Clear();
        }
        else
        {
            Debug.LogError("Collection " + collectionLabel + " does not exist.");
        }
    }

    public void DeleteLog(string collectionLabel)
    {
        if (logsList.ContainsKey(collectionLabel))
        {
            logsList.Remove(collectionLabel);
        }
        else
        {
            Debug.LogError("Collection " + collectionLabel + " does not exist.");
        }
    }


    public void DeleteAllLogs()
    {
        foreach (var keyValuePair in logsList)
        {
            logsList.Remove(keyValuePair.Key);
        }
    }

    public void SaveLog(string collectionLabel, bool clear)
    {
        if (logsList.ContainsKey(collectionLabel))
        {
 
            foreach (var targetType in targetsEnabled)
            {
                logsList[collectionLabel].AddSavingTarget(targetType);
            }

            //we generate the string and then we save the logs in the callback
            //by doing this, we are sure that the logs will be exported only once
            GenerateLogString(collectionLabel, () =>
            {
                Save(collectionLabel, clear, TargetType.CSV);
                Save(collectionLabel, clear, TargetType.MySql);
            });
        }
        else
        {
            Debug.LogError("No Collection Called " + collectionLabel);
        }
    }

    public void SaveLog(string collectionLabel, bool clear, TargetType targetType)
    {
        if (logsList.ContainsKey(collectionLabel))
        {
            //we generate the string and then we save the logs in the callback
            //by doing this, we are sure that the logs will be exported only once
            GenerateLogString(collectionLabel, () =>
            {
                Save(collectionLabel, clear, targetType);
            });
        }
        else
        {
            Debug.LogError("No Collection Called " + collectionLabel);
        }
    }

    private void Save(string collectionLabel, bool clear, TargetType targetType)
    {
        if (targetType == TargetType.CSV)
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                SaveToCSV(collectionLabel, clear);
            }
            return;
        }
        if (targetType == TargetType.MySql)
        {
            SaveToSQL(collectionLabel, clear);
        }
    }


    public void SaveAllLogs(bool clear)
    {
        foreach (KeyValuePair<string, LogStore> pair in logsList)
        {
            SaveLog(pair.Key, clear);
        }
    }

    public void SaveAllLogs(bool clear,TargetType targetType)
    {
        foreach (KeyValuePair<string, LogStore> pair in logsList)
        {
            SaveLog(pair.Key, clear,targetType);
        }
    }

    private void SaveCallback(LogStore logStore, bool clear)
    {
        if (!clear) return;

        //checks if all the targets have been saved, if not returns
        foreach (var targetType in targetsEnabled)
        {
            if (!logStore.TargetsSaved[targetType])
            {
                return;
            }
        }
        //All targets have been saved, we can clear the logs
        logStore.Clear();
        logStore.ResetTargetsSaved();
    }

    // Formats the logs to a CSV row format and saves them. Calls the CSV headers generation beforehand.
    // If a parameter doesn't have a value for a given row, uses the given value given previously (see 
    // UpdateHeadersAndDefaults).
    private void SaveToCSV(string label, bool clear)
    {
        if (!enableCSVSave) return;
        if (logsList.TryGetValue(label, out LogStore logStore))
        {
            WriteToCSV writeToCsv = new WriteToCSV(logStore, savePath, filePrefix, fileExtension);
            writeToCsv.WriteAll(() =>
            {
                logStore.TargetsSaved[TargetType.CSV] = true;
                SaveCallback(logStore, clear);
            });
        }
        else
        {
            Debug.LogWarning("Trying to save to CSV " + label + " collection but it doesn't exist.");
        }
    }

    private void SaveToSQL(string label, bool clear)
    {
        if (!enableMySQLSave) { return; }

        if (!logsList.ContainsKey(label))
        {
            Debug.LogError("Could not find collection " + label + ". Aborting.");
            logsList[label].RemoveSavingTarget(TargetType.MySql);
            return;
        }

        if (logsList[label].RowCount == 0)
        {
            Debug.LogError("Collection " + label + " is empty. Aborting.");
            logsList[label].RemoveSavingTarget(TargetType.MySql);
            return;
        }

        connectToMySQL.AddToUploadQueue(logsList[label], label);
        connectToMySQL.UploadNow(() =>
        {
            logsList[label].RemoveSavingTarget(TargetType.MySql);
            logsList[label].TargetsSaved[TargetType.MySql] = true;
            SaveCallback(logsList[label], clear);
        });
    }


}