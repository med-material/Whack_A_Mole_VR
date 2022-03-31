using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


public enum LogType
{
    LogEachRow, //Regular logs - logs automatically commons columns like TimeStamp or Framecount
    OneRowOverwrite //Meta logs - logs only one line containing at least sessionID and Email
}

public class LogStore
{

    private SortedDictionary<string, List<string>> logs;
    private StringBuilder logString;
    public string Label { get; set; }

    private List<TargetType> targetsSaving;
    private bool isLogStringReady;

    public LogType LogType;

    public int RowCount { get; set; }
    private bool createStringOverTime;
    private StringBuilder currentLineLogged;
    public SortedDictionary<string, string> CurrentLogRow { get; set; }
    private const string fieldSeparator = ";";
    private const string lineSeparator = "\n";

    public Dictionary<TargetType, bool> TargetsSaved { get; set; }

    //defines the characters to be deleted and the character replacing them
    private readonly Dictionary<char, char> charsToRemove = new Dictionary<char, char>()
    {
        {';', '-'},
        {'\'', '-'},
        {'"', '-'},
        {'\0', '-'},
        {'\b', '-'},
        {'\n', ' '},
        {'\r', ' '},
        {'\t', ' '},
        {'\\', '-'},
        {'_', '-'},
        {'%', ' '},
    };

    private string email;
    public string SessionId { get; set; }


    public LogStore(string label, string email, string sessionID, bool createStringOverTime,
        LogType logType = LogType.LogEachRow, List<string> headers = null)
    {
        Init(label, email, sessionID, createStringOverTime, logType, headers);
    }

    private void Init(string label, string email, string sessionID, bool createStringOverTime,
        LogType logType, List<string> headers = null)
    {
        InitiateTargetsSaved();
        targetsSaving = new List<TargetType>();
        this.Label = label;
        logs = new SortedDictionary<string, List<string>>();
        logString = new StringBuilder();
        currentLineLogged = new StringBuilder();
        CurrentLogRow = new SortedDictionary<string, string>();
        this.createStringOverTime = createStringOverTime;
        this.email = email;
        SessionId = sessionID;
        this.LogType = logType; ;
        logs.Add("Timestamp", new List<string>());
        logs.Add("Framecount", new List<string>());
        logs.Add("SessionID", new List<string>());
        logs.Add("Email", new List<string>());
        if (headers != null) {
            foreach (string header in headers)
            {
                logs.Add(header, new List<string>());
            }
        }
    }


    private void InitiateTargetsSaved()
    {
        TargetsSaved = new Dictionary<TargetType, bool>();
        foreach (TargetType value in Enum.GetValues(typeof(TargetType)))
        {
            TargetsSaved.Add(value, false);
        }
    }

    public void ResetTargetsSaved()
    {
        foreach (var pair in TargetsSaved.ToList())
        {
            TargetsSaved[pair.Key] = false;
        }
    }

    //Adds a column to the current row of the Logs
    public void Add(string column, object data)
    {
        //if IsReadOnly is true, it is impossible to add data to the logs
        if (IsReadOnly())
        {
            Debug.LogError("Impossible to add data to log " + Label + " while saving it");
            return;
        }
        //Checks if a new header has been added to the logs, if so adds NULL to each previous value for this header
        //Because the first row is the one that defines the headers, we don't check this when logging the first line
        if (RowCount > 0 && !logs.Keys.Contains(column))
        {
            logs.Add(column, new List<string>(Enumerable.Repeat("NULL", RowCount).ToList()));
            //Currently, if a new header is added durring the logging process, the possibility of logging the datastring
            //on the fly is disabled
            if (createStringOverTime)
            {
                Debug.LogError("Header " + column + " added durring logging process...\n" +
                               "aborting logging datastring on the fly");
                createStringOverTime = false;
                logString.Clear();
            }
        }

        //If the data added to the logs is already in the current row, terminates the current row and starts another one
        //We don't do this if we are logging Meta logs because there should be only one row in this case
        if (LogType == LogType.LogEachRow && CurrentLogRow.ContainsKey(column))
        {
            EndRow();
        }

        if (isLogStringReady)
        {
            isLogStringReady = false;
        }
        string dataStr = SanitizeString(ConvertToString(data));

        AddToDictIfNotExists(CurrentLogRow, column, dataStr);
    }

    //Adds the value to the dictionnary or creates a new column and adds the value
    private void CreateOrAddToLogsDict(IDictionary<string, List<string>> dictionary, string key, string value)
    {
        if (dictionary.TryGetValue(key, out List<string> list))
        {
            list.Add(value);
        }
        else
        {
            dictionary.Add(key, new List<string>());
            dictionary[key].Add(value);
        }
    }

    private void AddToDictIfNotExists(IDictionary<string, string> dictionary, string key, string value)
    {
        if (!dictionary.ContainsKey(key))
        {
            dictionary.Add(key, value);
        }
    }

    //Adds the commons columns to the current row 
    private void AddCommonColumns()
    {
        string timeStamp = GetTimeStamp();
        string frameCount = GetFrameCount();
        AddToDictIfNotExists(CurrentLogRow, "Timestamp", timeStamp);
        AddToDictIfNotExists(CurrentLogRow, "Framecount", frameCount);
        AddToDictIfNotExists(CurrentLogRow, "SessionID", SessionId);
        AddToDictIfNotExists(CurrentLogRow, "Email", email);
    }

    //Terminates the current row 
    public void EndRow()
    {
        if (LogType == LogType.OneRowOverwrite && RowCount >= 1)
        {
            Debug.Log("Unable to log more than one row in OneRowOverwrite mode");
            return;
        }
        AddCommonColumns();
        foreach (var logsKey in logs.Keys)
        {
            if (!CurrentLogRow.ContainsKey(logsKey))
            {
                CurrentLogRow.Add(logsKey, "NULL");
            }
        }
        foreach (var pair in CurrentLogRow)
        {
            CreateOrAddToLogsDict(logs, pair.Key, pair.Value);
            if (createStringOverTime)
            {
                if (currentLineLogged.Length != 0)
                {
                    currentLineLogged.Append(fieldSeparator);
                }
                currentLineLogged.Append(pair.Value);
            }
        }

        if (createStringOverTime)
        {
            currentLineLogged.Append(lineSeparator);
            logString.Append(currentLineLogged);
            currentLineLogged.Clear();
        }
        CurrentLogRow.Clear();
        RowCount++;
    }

    public void AddSavingTarget(TargetType targetType)
    {
        targetsSaving.Add(targetType);
    }

    public void RemoveSavingTarget(TargetType targetType)
    {
        targetsSaving.Remove(targetType);
    }

    //if targetsSaving is not empty, then the logStore is read-only
    public bool IsReadOnly()
    {
        return targetsSaving.Count != 0;
    }

    //Clears all the logs
    public void Clear()
    {
        //if IsReadOnly is true, it is impossible to clear the logs
        if (IsReadOnly())
        {
            Debug.LogError("Impossible to clear the log " + Label + " while saving it");
            return;
        }
        if (isLogStringReady)
        {
            isLogStringReady = false;
        }
        logs.Clear();
        CurrentLogRow.Clear();
        logString.Clear();
        RowCount = 0;
        Debug.Log("Log " + Label + " cleared");
    }

    //Exports the logs into a string or into a Dictionarry<string,string>
    public T ExportAll<T>()
    {
        var type = typeof(T);
        if (type == typeof(SortedDictionary<string, List<string>>))
        {
            return (T)Convert.ChangeType(logs, type);
        }
        if (type == typeof(string))
        {
            return (T)Convert.ChangeType(ExportToString(), type);
        }
        throw new Exception("Export type must be SortedDictionnary<string,List<string>> or string");
    }

    public SortedDictionary<string, List<string>> ExportLogs()
    {
        return logs;
    }

    //Exports the logs to a string
    public string ExportToString()
    {
        //if createStringOverTime is true, returns directy the dataString
        //else, if isLogStringReady is true, then the datastring is already generated, so returns it
        if (!createStringOverTime || !isLogStringReady)
        {
            logString.Clear();
            for (int i = 0; i < RowCount; i++)
            {
                string line = "";
                foreach (string key in logs.Keys)
                {
                    if (line != "")
                    {
                        line += fieldSeparator;
                    }
                    line += logs[key][i];
                }

                logString.Append(line + lineSeparator);
            }

            isLogStringReady = true;
        }
        return logString.ToString();
    }

    //Returns the headers as a string 
    public string GenerateHeaders()
    {
        string headers = "";
        foreach (string key in logs.Keys)
        {
            if (headers != "")
            {
                headers += fieldSeparator;
            }
            headers += key;
        }
        return headers;
    }


    // Converts the values of the parameters (in a "object format") to a string, formatting them to the
    // correct format in the process.
    private static string ConvertToString(object arg)
    {
        if (arg is float)
        {
            return ((float)arg).ToString("0.0000").Replace(",", ".");
        }
        if (arg is int)
        {
            return arg.ToString();
        }
        if (arg is bool)
        {
            return ((bool)arg) ? "TRUE" : "FALSE";
        }
        if (arg is Vector3)
        {
            return ((Vector3)arg).ToString("0.0000").Replace(",", ".");
        }
        return arg.ToString();
    }

    private string SanitizeString(string str)
    {
        foreach (var pair in charsToRemove)
        {
            str = str.Replace(pair.Key, pair.Value);
        }
        return str;
    }

    private string GetTimeStamp()
    {
        return System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
    }

    private string GetFrameCount()
    {
        return Time.frameCount == 0 ? "-1" : Time.frameCount.ToString();
    }

}