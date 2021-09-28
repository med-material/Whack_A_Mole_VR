using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Globalization;

/*
Class dedicated to gather logs, organize, format and save them.
The EventLogger collects logs through events (raised by LoggerNotifier). There are two types of events:
    -   "Event", the usual event. Has a mandatory event descriptor ("Mole Spawned, Game Difficulty Set to Hard...") and optionally other
        parameters (Mole coordinates in the case of a Mole activation for example). An Event corresponds to a row in the resulting logs.
    -   "PersistentEvent", an event updating parameters that will consistently be logged. This event doesn't have an event descriptor parameter, 
        only parameters to update, and isn't logged as a row in the resulting logs. This event is used to update parameters like Game Time 
        Duration, Game Difficuty... We want to log these parameters for every event (that's why they are called "persistent"), but we don't
        want to log a row every time that they are updated due to their update frequency and that their update isn't worth logging (we don't 
        want to generate a new row every frame for the Game Time Left for example).
    
The logs are currently saved in a CSV format when the game is stopped, as well as sent to the CREATE SQL database.
Each column represents a parameter, each row represents an event.

Before saving, the Event are stored in the logs Dictionary as following: Dictionary(key: string, value: Dictionary(key: int, value: string))
                                                                                            ^                            ^             ^
                                                                                    An event parameter            A row where it     Its value 
                                                                                                                   is referenced    for the given row 
*/

public class EventLogger : MonoBehaviour
{
    public enum EventType{MoleEvent, WallEvent, GameEvent, ModifierEvent, PointerEvent, DefaultEvent}

    [SerializeField]
    private PupilLabs.TimeSync timeSync;

    private float previousEventTime = -1;
    private TrackerHub trackerHub = new TrackerHub();
    private Dictionary<string, object> persistentLog = new Dictionary<string, object>();
    private Dictionary<string, object> currentMoleLog = new Dictionary<string, object>();
    private Dictionary<string, Dictionary<int, string>> logs = new Dictionary<string, Dictionary<int, string>>();
    private Dictionary<string, string> defaultValues = new Dictionary<string, string>();
    private int logCount = 0;
    private string email = "";
    private LoggingManager loggingManager;
    private WallStateTracker wallStateTracker;
    private string gameId = "NULL";


    // On start, init the logs with the mandatory columns.
    void Awake()
    {
        // connectToMySQL = gameObject.GetComponent<ConnectToMySQL>();
        loggingManager = gameObject.GetComponent<LoggingManager>();
        wallStateTracker = gameObject.GetComponent<WallStateTracker>();

        // logs.Add("TimeStamp", new Dictionary<int, string>());
        // logs.Add("Time", new Dictionary<int, string>());
        // logs.Add("Framecount", new Dictionary<int, string>());
        // logs.Add("Date", new Dictionary<int, string>());
        // logs.Add("TimeSinceLastEvent", new Dictionary<int, string>());
        // logs.Add("PupilTime", new Dictionary<int, string>());
        // logs.Add("UnityToPupilTimeOffset", new Dictionary<int, string>());
        // logs.Add("GameId", new Dictionary<int, string>());
        // persistentLog.Add("ParticipantId", 0);
        // persistentLog.Add("TestId", 0);
        loggingManager.Log("Meta", "DeviceID", SystemInfo.deviceUniqueIdentifier, LogMode.Overwrite);
        trackerHub.Init();
    }

    // Updates the logged mail.
    public void UpdateEmail(string newEmail)
    {
        if (email == newEmail) return;
        email = newEmail.Replace("\n", "").Replace("\r", ""); // sanitize e-mail, to not contain newline characters.
        loggingManager.SetEmail(email);
        loggingManager.Log("Meta", "Email", email, LogMode.Overwrite);
    }

    // // Function mostly called from the LoggerNotifier. Adds to the logs the columns (parameters) that will be used.
    // // Also stores for each column the default value to use when it is not indicated in the event parameters
    // public void UpdateHeadersAndDefaults(Dictionary<string, string> headersToAdd)
    // {
    //     foreach(KeyValuePair<string, string> entry in headersToAdd)
    //     {
    //         if(!logs.ContainsKey(entry.Key))
    //         {
    //             logs.Add(entry.Key, new Dictionary<int, string>());
    //         }
    //         if(!defaultValues.ContainsKey(entry.Key))
    //         {
    //             defaultValues.Add(entry.Key, entry.Value);
    //         }
    //     }
    // }

    // Function called by the LoggerNotifier to log an event. Needs a LogEventContainer as argument, which contains
    // an Event and/or a PersistentEvent (see LogEventContainer for more details).
    public void EventNotification(LogEventContainer datas)
    {
        if (previousEventTime == -1)
        {
            previousEventTime = Time.time;
        }

        if(datas.logEventParameters.Count > 0)
        {
            LogEvent(datas.logEventParameters);
        }

        if(datas.persistentLogEventParameters.Count > 0)
        {
            LogPersistentEvent(datas.persistentLogEventParameters);
        }
    }

    // Function to log an Event. Checks the descriptor to do certains actions if needed (save the logs on Game Stopped or Game Finished for example).
    private void LogEvent(Dictionary<string, object> datas)
    {
        switch(datas["Event"])
        {
            case "Game Started":
                trackerHub.StartTrackers();
                gameId = GenerateUid();
                //InitFile();
                SaveEventDatas(datas);
                break;
            case "Game Stopped":
            case "Game Finished":
                trackerHub.StopTrackers();
                SaveEventDatas(datas);
                //loggingManager.SaveAllLogs();
                //loggingManager.ClearAllLogs();
                //loggingManager.NewFilestamp();
                break;
            case "Mole Spawned":
                SaveEventDatas(datas, true, true);
                UpdateCurrentMoleLog(datas);
                break;
            case "Fake Mole Spawned":
                SaveEventDatas(datas, true, true);
                break;
            case "Mole Missed":
                datas = AddClosestActiveMole(datas);
                SaveEventDatas(datas, true, true);
                break;
            case "Mole Hit":
            case "Mole Expired":
            case "Fake Mole Expired":
            case "Wrong Mole Hit":
                if (currentMoleLog.ContainsKey("CurrentMoleId"))
                {
                    SaveEventDatas(datas, false, true);
                }
                else
                {
                    SaveEventDatas(datas, true, true);
                }
                break;
            default:
                SaveEventDatas(datas);
                break;
        }
    }

    // Function to log PersistentEvents.
    private void LogPersistentEvent(Dictionary<string, object> datas)
    {
        foreach (KeyValuePair<string, object> pair in datas)
        {
            persistentLog[pair.Key] = pair.Value;
        }
    }

    // Initialization of the "Current Mole" log, keeping track of the current Mole to hit.
    private void InitCurrentMoleLog()
    {
        currentMoleLog.Clear();
        currentMoleLog.Add("CurrentMoleToHitId", "NULL");
        currentMoleLog.Add("CurrentMoleToHitIndexX", "NULL");
        currentMoleLog.Add("CurrentMoleToHitIndexY", "NULL");
        currentMoleLog.Add("CurrentMoleToHitPositionWorldX", "NULL");
        currentMoleLog.Add("CurrentMoleToHitPositionWorldY", "NULL");
        currentMoleLog.Add("CurrentMoleToHitPositionWorldZ", "NULL");
        currentMoleLog.Add("CurrentMoleToHitPositionLocalX", "NULL");
        currentMoleLog.Add("CurrentMoleToHitPositionLocalY", "NULL");
        currentMoleLog.Add("CurrentMoleToHitPositionLocalZ", "NULL");

        Dictionary<string, string> tempDict = new Dictionary<string, string>();
        foreach(KeyValuePair<string, object> pair in currentMoleLog)
        {
            tempDict.Add(pair.Key, pair.Value.ToString().Replace(",", "."));
        }
        //UpdateHeadersAndDefaults(tempDict);
    }

    // Update of the "Current Mole" log, called when a new Mole is activated (see LogEvent function)
    private void UpdateCurrentMoleLog(Dictionary<string, object> datas)
    {
        currentMoleLog["CurrentMoleToHitId"] = datas["MoleId"];
        currentMoleLog["CurrentMoleToHitIndexX"] = datas["MoleIndexX"];
        currentMoleLog["CurrentMoleToHitIndexY"] = datas["MoleIndexY"];
        currentMoleLog["CurrentMoleToHitPositionWorldX"] = datas["MolePositionWorldX"];
        currentMoleLog["CurrentMoleToHitPositionWorldY"] = datas["MolePositionWorldY"];
        currentMoleLog["CurrentMoleToHitPositionWorldZ"] = datas["MolePositionWorldZ"];
        currentMoleLog["CurrentMoleToHitPositionLocalX"] = datas["MolePositionLocalX"];
        currentMoleLog["CurrentMoleToHitPositionLocalY"] = datas["MolePositionLocalY"];
        currentMoleLog["CurrentMoleToHitPositionLocalZ"] = datas["MolePositionLocalZ"];
    }

    // Adds to the datas the closest active Mole entries.
    private Dictionary<string, object> AddClosestActiveMole(Dictionary<string, object> datas)
    {
        if (!datas.ContainsKey("HitPositionWorldX") || !datas.ContainsKey("HitPositionWorldY") || !datas.ContainsKey("HitPositionWorldZ")) return datas;

        Vector2 closestActiveMoleDist = wallStateTracker.GetClosestDistPointToMole(new Vector3((float)datas["HitPositionWorldX"], (float)datas["HitPositionWorldY"], (float)datas["HitPositionWorldZ"]));
        datas.Add("ClosestActiveMoleDistanceX", closestActiveMoleDist.x);
        datas.Add("ClosestActiveMoleDistanceY", closestActiveMoleDist.y);

        return datas;
    }

    // Save the datas of an Event. Amends to the datas extra parameters if asked to.
    private void SaveEventDatas(Dictionary<string, object> datas, bool includeMoleToHit = false, bool logTrack = false)
    {
        datas["EventType"] = System.Enum.GetName(typeof(EventLogger.EventType), datas["EventType"]);

        if (includeMoleToHit)
        {
            foreach(KeyValuePair<string, object> pair in currentMoleLog)
            {
                datas.Add(pair.Key, pair.Value);
            }
        }
        if (logTrack)
        {
            // Adds the parameters of the objects tracked by the TrackerHub's trackers
            foreach (KeyValuePair<string, object> log in trackerHub.GetTracks())
            {
                datas.Add(log.Key, log.Value);
            }
        }

        datas["TimeSinceLastEvent"] = GetPreviousEventTimeDiff().ToString("0.0000").Replace(",", ".");
        datas["PupilTime"] = timeSync != null ? timeSync.GetPupilTimestamp().ToString().Replace(",", ".") : "NULL";
        datas["UnityToPupilTimeOffset"] = timeSync != null ? timeSync.UnityToPupilTimeOffset.ToString().Replace(",", ".") : "NULL";
        datas["GameId"] = gameId;

        foreach(KeyValuePair<string, object> pair in persistentLog)
        {
            datas.Add(pair.Key, pair.Value.ToString().Replace(",", "."));
        }

        loggingManager.Log("Event", datas);
    }

    // // Generates a "logs" row (see class description) from the given datas. Adds mandatory parameters and 
    // // the PersistentEvents parameters to the row when generating it.
    // private void GenerateLine(Dictionary<string, object> log)
    // {
    //     logs["Framecount"].Add(logCount, Time.frameCount.ToString());
    //     logs["TimeStamp"].Add(logCount, GetTimeStamp());
    //     logs["Date"].Add(logCount, System.DateTime.Now.ToString("yyyy-MM-dd"));
    //     logs["Time"].Add(logCount, System.DateTime.Now.ToString("HH:mm:ss.ffff"));
    //     logs["TimeSinceLastEvent"].Add(logCount, GetPreviousEventTimeDiff().ToString("0.0000").Replace(",", "."));
    //     logs["PupilTime"].Add(logCount, timeSync != null ? timeSync.GetPupilTimestamp().ToString() : "NULL");
    //     logs["UnityToPupilTimeOffset"].Add(logCount, timeSync != null ? timeSync.UnityToPupilTimeOffset.ToString() : "NULL");
    //     logs["GameId"].Add(logCount, uid);

    //     foreach (KeyValuePair<string, object> pair in log)
    //     {
    //         if (logs.ContainsKey(pair.Key))
    //         {
    //             logs[pair.Key].Add(logCount, ConvertToString(pair.Value));
    //         }
    //         else
    //         {
    //             logs.Add(pair.Key, new Dictionary<int, string>{{logCount, ConvertToString(pair.Value)}});
    //         }
    //     }

    //     foreach (KeyValuePair<string, object> pair in persistentLog)
    //     {
    //         if (logs.ContainsKey(pair.Key))
    //         {
    //             logs[pair.Key].Add(logCount, ConvertToString(pair.Value));
    //         }
    //         else
    //         {
    //             logs.Add(pair.Key, new Dictionary<int, string>{{logCount, ConvertToString(pair.Value)}});
    //         }
    //     }
    //     logCount++;
    // }


    // private void SaveSqlLogs()
    // {
    //     Dictionary<string, List<string>> logCollection = new Dictionary<string, List<string>>();
    //     logCollection.Add("Email", new List<string>());
    //     for(int i = 0; i < logCount; i++)
    //     {
    //         logCollection["Email"].Add(email);
    //     }
    //     string temp;

    //     foreach(KeyValuePair<string, Dictionary<int, string>> pair in logs)
    //     {
    //         logCollection.Add(pair.Key, new List<string>());
            
    //         for(int i = 0; i < logCount; i++)
    //         {
    //             if (pair.Value.TryGetValue(i, out temp))
    //             {
    //                 logCollection[pair.Key].Add(temp);
    //             }
    //             else
    //             {
    //                 if(defaultValues.TryGetValue(pair.Key, out temp))
    //                 {
    //                     logCollection[pair.Key].Add(temp);
    //                 }
    //                 else
    //                 {
    //                     logCollection[pair.Key].Add("NULL");
    //                 }
    //             }
    //         }
    //     }
    //     SendSqlLogs(logCollection);
    // }


    // private void SendSqlLogs(Dictionary<string, List<string>> logCollection)
    // {
    //     // connectToMySQL.AddToUploadQueue(logCollection);
    //     // connectToMySQL.UploadNow();
    // }


    // // Formats the logs to a CSV row format and saves them. Calls the CSV headers generation beforehand.
    // // If a parameter doesn't have a value for a given row, uses the given value given previously (see 
    // // UpdateHeadersAndDefaults).
    // private void SaveCsvLogs()
    // {
    //     if(!saveLocally) return;

    //     GenerateHeaders();
    //     string temp;
    //     for (int i = 0; i < logCount; i++)
    //     {
    //         string line = "";
    //         foreach (KeyValuePair<string, Dictionary<int, string>> log in logs)
    //         {
    //             if (line != "")
    //             {
    //                 line += fieldSeperator;
    //             }

    //             if (log.Value.TryGetValue(i, out temp))
    //             {
    //                 line += temp;
    //             }
    //             else
    //             {
    //                 if(defaultValues.TryGetValue(log.Key, out temp))
    //                 {
    //                     line += temp;
    //                 }
    //                 else
    //                 {
    //                     line += "NULL";
    //                 }
    //             }
    //         }
    //         SaveToFile(line);
    //     }
    // }

    // // Generates the headers in a CSV format and saves them to the CSV file
    // private void GenerateHeaders()
    // {
    //     string headers = "";
    //     foreach (string key in logs.Keys)
    //     {
    //         if (headers != "")
    //         {
    //             headers += fieldSeperator;
    //         }
    //         headers += key;
    //     }
    //     SaveToFile(headers);
    // }

    // Generates a unique ID for the test
    private string GenerateUid()
    {
        object participantId;
        object testId;
        persistentLog.TryGetValue("ParticipantId", out participantId);
        persistentLog.TryGetValue("TestId", out testId);
        return participantId.ToString() + testId.ToString() + System.DateTime.Now.ToString("yyyy:MM:dd:HH:mm:ss").Replace(" ", "").Replace("/", "").Replace(":", "");
    }

    // // Converts the values of the parameters (in a "object format") to a string, formatting them to the
    // // correct format in the process.
    // private string ConvertToString(object arg)
    // {
    //     if (arg is float)
    //     {
    //         return ((float)arg).ToString("0.0000").Replace(",", ".");
    //     }
    //     else if (arg is Vector3)
    //     {
    //         return ((Vector3)arg).ToString("0.0000").Replace(",", ".");
    //     }
    //     else
    //     {
    //         return arg.ToString();
    //     }
    // }

    // Updates and returns the "TimeSinceLastEvent" value.
    private float GetPreviousEventTimeDiff()
    {
        float tempTime = Time.time - previousEventTime;
        previousEventTime = Time.time;
        return tempTime;
    }

    // Returns a time stamp including the milliseconds.
    private string GetTimeStamp()
    {
        return System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
    }

    // // Initialises the CSV file parameters (name and file path).
    // private void InitFile()
    // {
    //     completeFileName = fileName + "_" + GetTimeStamp().Replace('/', '-').Replace(":", "-");
    //     filePath = savePath + "/" + completeFileName + ".csv";
    // }

    // // Saves the given CSV line to the CSV file.
    // private void SaveToFile(string line, bool end = true)
    // {
    //     string tempLine = line;

    //     if (end)
    //     {
    //         tempLine += Environment.NewLine;
    //     }
    //     File.AppendAllText(filePath, tempLine    );
    // }

    // // Clears the logs, "Current Mole" log, log count and unique test ID. Used to clear the logs when a new game is started.
    // private void ResetLogs()
    // {
    //     currentMoleLog.Clear();
        
    //     foreach(Dictionary<int, string> dict in logs.Values)
    //     {
    //         dict.Clear();
    //     }
    //     logCount = 0;
    //     GenerateUid();
    // }
}