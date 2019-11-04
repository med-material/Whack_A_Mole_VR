using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/*
Class dedicated to collect datas from the LogTrackers (see LogTracker class).
*/

public class TrackerHub: UnityEngine.Object
{
    private List<LogTracker> trackers = new List<LogTracker>();

    // On init, finds and references all LogTracker present in the scene.
    public void Init()
    {
        foreach (LogTracker tracker in FindObjectsOfType<LogTracker>())
        {
            trackers.Add(tracker);
        }
    }

    // Returns the logs from the LogTrackers after formatting.
    public Dictionary<string, object> GetTracks()
    {
        Dictionary<string, object> logs = new Dictionary<string, object>();
        foreach (LogTracker tracker in trackers)
        {
            Dictionary<string, object> trackDatas = tracker.GetDatas();
            string trackerId = trackDatas["TrackId"].ToString();
            trackDatas.Remove("TrackId");
            foreach (string key in trackDatas.Keys)
            {
                logs.Add(trackerId + key, trackDatas[key]);
            }
        }
        return logs;
    }

    // Starts the trackers' tracking'.
    public void StartTrackers()
    {
        foreach (LogTracker tracker in trackers)
        {
            tracker.StartTracking();
        }
    }

    // Stops the trackers' tracking.
    public void StopTrackers()
    {
        foreach (LogTracker tracker in trackers)
        {
            tracker.StopTracking();
        }
    }
}
