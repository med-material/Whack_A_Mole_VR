using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Globalization;

public enum LogMode {
    Append,
    Overwrite
}

public class LogCollection {
    public string label;
    public int count = 0;
    public bool saveHeaders = true;
    public Dictionary<string, Dictionary<int, object>> log = new Dictionary<string, Dictionary<int, object>>();
}

public class LoggingManager : MonoBehaviour
{
    private Dictionary<string, string> statelogs = new Dictionary<string, string>();
    private Dictionary<string, Dictionary<int, string>> logs = new Dictionary<string, Dictionary<int, string>>();

    // sampleLog[COLUMN NAME][COLUMN NO.] = [OBJECT] (fx a float, int, string, bool)
    private Dictionary<string, LogCollection> collections = new Dictionary<string, LogCollection>();

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

    [Tooltip("If save path is empty, it defaults to My Documents.")]
    [SerializeField]
    private string savePath = "";

    [SerializeField]
    private string filePrefix = "log";

    [SerializeField]
    private string fileExtension = ".csv";

    private string filePath;
    private char fieldSeperator = ',';
    private string sessionID = "";
    private string deviceID = "";
    private string filestamp;

    // Start is called before the first frame update
    void Awake()
    {
        filestamp = GetTimeStamp().Replace('/', '-').Replace(":", "-");
        if (CreateMetaCollection) {
            GenerateUIDs();
            Log("Meta", "SessionID", sessionID, LogMode.Overwrite);
            Log("Meta", "DeviceID", deviceID, LogMode.Overwrite);
        }
        if (savePath == "") {
            savePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        }
    }

    public void GenerateUIDs() {
        sessionID = Md5Sum(System.DateTime.Now.ToString(SystemInfo.deviceUniqueIdentifier + "yyyy:MM:dd:HH:mm:ss.ffff").Replace(" ", "").Replace("/", "").Replace(":", ""));
        deviceID = SystemInfo.deviceUniqueIdentifier;
    }

    public Dictionary<string, Dictionary<int, object>> GetLog(string collectionLabel) {
        return new Dictionary<string, Dictionary<int, object>>(collections[collectionLabel].log);
    }

    public void SaveAllLogs() {
        foreach(KeyValuePair<string, LogCollection> pair in collections) {
            SaveLog(pair.Value.label);
        }
    }

    public void SaveLog(string collectionLabel) {
        if (collections.ContainsKey(collectionLabel)) {
            if (Application.platform != RuntimePlatform.WebGLPlayer) {
                SaveToCSV(collectionLabel);
            }
            SaveToSQL(collectionLabel);
        } else {
            Debug.LogError("No Collection Called " + collectionLabel);
        }
    }

    public void SetEmail(string newEmail) {
        email = newEmail;
    }

    public void CreateLog(string collectionLabel) {
        collections.Add(collectionLabel, new LogCollection());
    }

    public void Log(string collectionLabel, Dictionary<string, object> logData, LogMode logMode=LogMode.Append) {
        if (!collections.ContainsKey(collectionLabel)) {
            collections.Add(collectionLabel, new LogCollection());
            collections[collectionLabel].label = collectionLabel;
        }
        foreach(KeyValuePair<string, object> pair in logData) {
            if (!collections[collectionLabel].log.ContainsKey(pair.Key)) {
                collections[collectionLabel].log.Add(pair.Key, new Dictionary<int, object>());

                if (!collections[collectionLabel].log.ContainsKey("Timestamp")) {
                    collections[collectionLabel].log["Timestamp"] = new Dictionary<int, object>();
                }
                if (!collections[collectionLabel].log.ContainsKey("Framecount")) {
                    collections[collectionLabel].log["Framecount"] = new Dictionary<int, object>();
                }
                if (!collections[collectionLabel].log.ContainsKey("SessionID")) {
                collections[collectionLabel].log["SessionID"] = new Dictionary<int, object>();
                }
                if (!collections[collectionLabel].log.ContainsKey("Email")) {
                collections[collectionLabel].log["Email"] = new Dictionary<int, object>();
                }
            }
            int count = collections[collectionLabel].count;
            if (logMode == LogMode.Append) {
                if (collections[collectionLabel].log[pair.Key].ContainsKey(count)) {
                    collections[collectionLabel].count++;
                    count = collections[collectionLabel].count;
                }
            }

            collections[collectionLabel].log["Timestamp"][count] = GetTimeStamp();
            collections[collectionLabel].log["Framecount"][count] = GetFrameCount();
            collections[collectionLabel].log["SessionID"][count] = sessionID;
            collections[collectionLabel].log["Email"][count] = email;
            collections[collectionLabel].log[pair.Key][count] = pair.Value;
        }
    }

    public void Log(string collectionLabel, string columnLabel, object value, LogMode logMode = LogMode.Append) {
        if (!collections.ContainsKey(collectionLabel)) {
            collections.Add(collectionLabel, new LogCollection());
            collections[collectionLabel].label = collectionLabel;
        }

        if (!collections[collectionLabel].log.ContainsKey(columnLabel)) {
            collections[collectionLabel].log.Add(columnLabel, new Dictionary<int, object>());

                if (!collections[collectionLabel].log.ContainsKey("Timestamp")) {
                    collections[collectionLabel].log["Timestamp"] = new Dictionary<int, object>();
                }
                if (!collections[collectionLabel].log.ContainsKey("Framecount")) {
                    collections[collectionLabel].log["Framecount"] = new Dictionary<int, object>();
                }
                if (!collections[collectionLabel].log.ContainsKey("SessionID")) {
                collections[collectionLabel].log["SessionID"] = new Dictionary<int, object>();
                }
                if (!collections[collectionLabel].log.ContainsKey("Email")) {
                collections[collectionLabel].log["Email"] = new Dictionary<int, object>();
                }
        }

        int count = collections[collectionLabel].count;
        if (logMode == LogMode.Append) {
            if (collections[collectionLabel].log[columnLabel].ContainsKey(count)) {
                collections[collectionLabel].count++;
                count = collections[collectionLabel].count;
            }
        }

        collections[collectionLabel].log["Timestamp"][count] = GetTimeStamp();
        collections[collectionLabel].log["Framecount"][count] = GetFrameCount();
        collections[collectionLabel].log["SessionID"][count] = sessionID;
        collections[collectionLabel].log["Email"][count] = email;
        collections[collectionLabel].log[columnLabel][count] = value;
    }

    public void ClearAllLogs() {
        foreach (KeyValuePair<string, LogCollection> pair in collections) {
            collections[pair.Key].log.Clear();
            collections[pair.Key].count = 0;
        }
    }

    public void ClearLog(string collectionLabel) {
        if (collections.ContainsKey(collectionLabel)) {
            collections[collectionLabel].log.Clear();
            collections[collectionLabel].count = 0;
        } else {
            Debug.LogError("Collection " + collectionLabel + " does not exist.");
            return;
        }
    }

    // Formats the logs to a CSV row format and saves them. Calls the CSV headers generation beforehand.
    // If a parameter doesn't have a value for a given row, uses the given value given previously (see 
    // UpdateHeadersAndDefaults).
    private void SaveToCSV(string label)
    {
        if(!enableCSVSave) return;
        string headerLine = "";
        if (collections[label].saveHeaders) {
            headerLine = GenerateHeaders(collections[label]);
        }
        object temp;
        string filename = collections[label].label;
        string filePath = savePath + "/" + filePrefix + filestamp + filename + fileExtension;
        using (var file = new StreamWriter(filePath, true)) {
            if (collections[label].saveHeaders) {
                file.WriteLine(headerLine);
                collections[label].saveHeaders = false;
            }
            for (int i = 0; i <= collections[label].count; i++)
            {
                string line = "";
                foreach (KeyValuePair<string, Dictionary<int, object>> log in collections[label].log)
                {
                    if (line != "")
                    {
                        line += fieldSeperator;
                    }
                    
                    if (log.Value.TryGetValue(i, out temp))
                    {
                        line += ConvertToString(temp);
                    }
                    else
                    {
                        line += "NULL";
                    }
                }
                file.WriteLine(line);
            }
        }
        Debug.Log( label + " logs with " + collections[label].count+1 + " rows saved to " + savePath);
    }


    // Generates the headers in a CSV format and saves them to the CSV file
    private string GenerateHeaders(LogCollection collection)
    {
        string headers = "";
        foreach (string key in collection.log.Keys)
        {
            if (headers != "")
            {
                headers += fieldSeperator;
            }
            headers += key;
        }
        return headers;
    }

    private void SaveToSQL(string label)
    {
        if (!enableMySQLSave) { return; }

        if (!collections.ContainsKey(label)) {
            Debug.LogError("Could not find collection " + label + ". Aborting.");
            return;
        }

        if (collections[label].log.Keys.Count == 0) {
            Debug.LogError("Collection " + label + " is empty. Aborting.");
            return;
        }
        
        connectToMySQL.AddToUploadQueue(collections[label].log, collections[label].label);    
        connectToMySQL.UploadNow();
    }

    public string Md5Sum(string strToEncrypt)
    {
        System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
        byte[] bytes = ue.GetBytes(strToEncrypt);
    
        // encrypt bytes
        System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
        byte[] hashBytes = md5.ComputeHash(bytes);
    
        // Convert the encrypted bytes back to a string (base 16)
        string hashString = "";
    
        for (int i = 0; i < hashBytes.Length; i++)
        {
            hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
        }
    
        return hashString.PadLeft(32, '0');
    }

    // Converts the values of the parameters (in a "object format") to a string, formatting them to the
    // correct format in the process.
    private string ConvertToString(object arg)
    {
        if (arg is float)
        {
            return ((float)arg).ToString("0.0000").Replace(",", ".");
        } else if (arg is int) {
            return arg.ToString();
        } else if (arg is bool) {
            return ((bool)arg) ? "TRUE" : "FALSE";
        } else if (arg is Vector3)
        {
            return ((Vector3)arg).ToString("0.0000").Replace(",", ".");
        }
        else
        {
            return arg.ToString();
        }
    }

    // Returns a time stamp including the milliseconds.
    private string GetTimeStamp()
    {
        return System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
    }

    private string GetFrameCount() {
        return Time.frameCount == null ? "-1" : Time.frameCount.ToString();
    }

}
