using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using UnityEngine.Events;

public enum SaveStatus
{
    ReadyToSave,
    IsSaving,
    Saved
}

public class SaveStateInfo
{
	public SaveStatus status = SaveStatus.ReadyToSave;
    public int numberOfSavedFiles;
    public int totalNumberOfFilesToSave;
}

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

    [Serializable]
	public class OnSaveInfoChanged : UnityEvent<SaveStateInfo> {}
	public OnSaveInfoChanged onSaveInfoChanged;

    public SaveStateInfo saveStateInfo = new SaveStateInfo();

    private int numberOfSavedFiles;
    private int totalNumberOfFilesToSave;

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

        onSaveInfoChanged.Invoke(new SaveStateInfo { status = SaveStatus.ReadyToSave, numberOfSavedFiles = 0, totalNumberOfFilesToSave = 0} );
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

    public void SaveAllLogs(bool clear)
    {
         List<string> labelList = new List<string>();

        foreach(KeyValuePair<string, LogStore> key in logsList)
        {
            labelList.Add(key.Key);
        }

        for(int i = 0; i < labelList.Count; i++)
        {
            SaveLog(labelList[i], clear);
        }

        labelList.Clear();
    }

    public void SaveAllLogs(bool clear,TargetType targetType)
    {
        List<string> labelList = new List<string>();

        totalNumberOfFilesToSave = logsList.Count;
        numberOfSavedFiles = 0;

        saveStateInfo.status = SaveStatus.IsSaving;
        saveStateInfo.numberOfSavedFiles = numberOfSavedFiles;
        saveStateInfo.totalNumberOfFilesToSave = totalNumberOfFilesToSave;

        onSaveInfoChanged.Invoke(saveStateInfo);

        foreach(KeyValuePair<string, LogStore> key in logsList)
        {
            labelList.Add(key.Key);
        }

        for(int i = 0; i < labelList.Count; i++)
        {
            SaveLog(labelList[i], clear, targetType);
        }

        labelList.Clear();
    }

    public void SaveLog(string collectionLabel, bool clear)
    {
        if (logsList.ContainsKey(collectionLabel))
        {
            //while the game is running, the LogStores with LogType OneRowOverwrite need to stay at 0 in the RowCount property.
            //when we want to save, we need to call EndRow function to specify that the LogStore is full.
            //So, during the save, the RowCount of these LogStores will be equals to 1.
            if(logsList[collectionLabel].LogType == LogType.OneRowOverwrite)
            {
                logsList[collectionLabel].EndRow();
            }
            LogStore tmpLogStore = logsList[collectionLabel];
            if(clear)
            {
                logsList.Remove(collectionLabel);
            }
            Save(collectionLabel, tmpLogStore, TargetType.CSV);
            Save(collectionLabel, tmpLogStore, TargetType.MySql);

            //meta collection is a special collection that contains all informations about the game and the patient.
            //we need to have this collection in the logsList before that the game start.
            //if we remove Meta collection in the logsList, we create it again after its removal.
            if(collectionLabel == "Meta")
            {
                AddMetaCollectionToList();
            }
        }
        else
        {
            Debug.LogError("No Collection Called " + collectionLabel);
        }
    }

    public void SaveLog(string collectionLabel, bool clear, TargetType targetType)
    {
        if(logsList.ContainsKey(collectionLabel))
        {
            //while the game is running, the LogStores with LogType OneRowOverwrite need to stay at 0 in the RowCount property.
            //when we want to save, we need to call EndRow function to specify that the LogStore is full.
            //So, during the save, the RowCount of these LogStores will be equals to 1.
            if(logsList[collectionLabel].LogType == LogType.OneRowOverwrite)
            {
                logsList[collectionLabel].EndRow();
            }
            LogStore tmpLogStore = logsList[collectionLabel];
            if(clear)
            {
                logsList.Remove(collectionLabel);
            }
            Save(collectionLabel, tmpLogStore, targetType);

            //meta collection is a special collection that contains all informations about the game and the patient.
            //we need to have this collection in the logsList before that the game start.
            //if we remove Meta collection in the logsList, we create it again after its removal.
            if(collectionLabel == "Meta")
            {
                AddMetaCollectionToList();
            }
        }
        else
        {
            Debug.LogError("No Collection Called " + collectionLabel);
        }
    }

    private void Save(string collectionLabel, LogStore logStore, TargetType targetType)
    {
        if (targetType == TargetType.CSV)
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                SaveToCSV(collectionLabel, logStore);
            }
            return;
        }
        if (targetType == TargetType.MySql)
        {
            SaveToSQL(collectionLabel, logStore);
        }
    }

    // Formats the logs to a CSV row format and saves them. Calls the CSV headers generation beforehand.
    // If a parameter doesn't have a value for a given row, uses the given value given previously (see 
    // UpdateHeadersAndDefaults).
    private void SaveToCSV(string label, LogStore logStore)
    {
        if (!enableCSVSave) return;

        if (logStore.RowCount == 0)
        {
            Debug.LogError("Collection " + label + " is empty. Aborting.");
            return;
        }

        WriteToCSV writeToCsv = new WriteToCSV(logStore, savePath, filePrefix, fileExtension);
        writeToCsv.WriteAll(() =>
        {
            UpdateSaveInfos();
        });
    }

    private void SaveToSQL(string label, LogStore logStore)
    {
        if (!enableMySQLSave) { return; }

        if (logStore.RowCount == 0)
        {
            Debug.LogError("Collection " + label + " is empty. Aborting.");
            return;
        }

        connectToMySQL.AddToUploadQueue(logStore, label);
        connectToMySQL.UploadNow(() =>
        {
            UpdateSaveInfos();
        });
    }

    private void UpdateSaveInfos()
    {
        numberOfSavedFiles++;
        if(numberOfSavedFiles == totalNumberOfFilesToSave)
        {
            saveStateInfo.status = SaveStatus.Saved;
            saveStateInfo.numberOfSavedFiles = numberOfSavedFiles;
            saveStateInfo.totalNumberOfFilesToSave = totalNumberOfFilesToSave;

            onSaveInfoChanged.Invoke(saveStateInfo);        
        }
        else
        {
            saveStateInfo.status = SaveStatus.IsSaving;
            saveStateInfo.numberOfSavedFiles = numberOfSavedFiles;
            saveStateInfo.totalNumberOfFilesToSave = totalNumberOfFilesToSave;

            onSaveInfoChanged.Invoke(saveStateInfo);
        }
    }
}