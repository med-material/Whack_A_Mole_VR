using System.Collections.Generic;
using UnityEngine;


/*
Class dedicated to collect datas from the LogTrackers (see LogTracker class).
*/

public class TrackerHub : MonoBehaviour
{
    [SerializeField]
    private List<LogTracker> trackers = new List<LogTracker>();


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

    /// <summary>
    /// Registers a tracker at runtime. Used for dynamically instantiated objects that need tracking.
    /// </summary>
    public void RegisterTracker(LogTracker tracker)
    {
        if (tracker == null)
        {
            Debug.LogWarning("TrackerHub: Attempted to register a null tracker.");
            return;
        }

        if (trackers.Contains(tracker))
        {
            Debug.LogWarning($"TrackerHub: Tracker '{tracker.gameObject.name}' is already registered.");
            return;
        }

        trackers.Add(tracker);
        Debug.Log($"TrackerHub: Registered tracker '{tracker.gameObject.name}'");
        
        // If tracking is already active, start tracking this new tracker
        if (trackers.Count > 0 && trackers[0] != null)
        {
            // Check if any tracker is currently tracking (indicates StartTrackers was called)
            bool isTrackingActive = false;
            foreach (LogTracker t in trackers)
            {
                if (t != null && t.gameObject.activeInHierarchy)
                {
                    // Assume tracking is active if trackers exist and are active
                    isTrackingActive = true;
                    break;
                }
            }
            
            if (isTrackingActive)
            {
                tracker.StartTracking();
            }
        }
    }

    /// <summary>
    /// Unregisters a tracker at runtime. Used when dynamically instantiated objects are destroyed.
    /// </summary>
    public void UnregisterTracker(LogTracker tracker)
    {
        if (tracker == null)
        {
            Debug.LogWarning("TrackerHub: Attempted to unregister a null tracker.");
            return;
        }

        if (trackers.Remove(tracker))
        {
            Debug.Log($"TrackerHub: Unregistered tracker '{tracker.gameObject.name}'");
            tracker.StopTracking();
        }
        else
        {
            Debug.LogWarning($"TrackerHub: Tracker '{tracker.gameObject.name}' was not found in the list.");
        }
    }
}
