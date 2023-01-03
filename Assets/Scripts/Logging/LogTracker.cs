using System.Collections;
using System.Collections.Generic;
using Valve.VR;
using UnityEngine;

/*
Class dedicated to track the object's position and rotation it is attached to. Is called by the TrackerHub to gather datas.
Tracks the global position and global rotation. Can also optionally track the position/rotation travelled by the object.
*/

public class LogTracker : MonoBehaviour
{
    [SerializeField]
    private string identifier = "";

    [SerializeField]
    private bool trackRot = false;

    [SerializeField]
    private bool trackPos = false;

    [SerializeField]
    private bool trackTrigger = false;

    [SerializeField]
    private SteamVR_Input_Sources controller;

    private bool isTracking = false;
    private Vector3 rotTravel = Vector3.zero;
    private Vector3 previousRot;
    private Vector3 posTravel = Vector3.zero;
    private Vector3 previousPos;
    private bool trigger = false;

    // On update, if is enabled, tracks.
    void Update()
    {
        if (!isTracking)
        {
            return;
        }
        UpdateTravel(); 
    }

    // Returns the tracking datas.
    public Dictionary<string, object> GetDatas()
    {
        Dictionary<string, object> datas = new Dictionary<string, object>()
        {
            {"TrackId", identifier},
            {"PosWorldX", transform.position.x},
            {"PosWorldY", transform.position.y},
            {"PosWorldZ", transform.position.z},
            {"RotEulerX", transform.eulerAngles.x},
            {"RotEulerY", transform.eulerAngles.y},
            {"RotEulerZ", transform.eulerAngles.z}
        };

        if (trackPos)
        {
            datas.Add("PosTravelX", posTravel.x);
            datas.Add("PosTravelY", posTravel.y);
            datas.Add("PosTravelZ", posTravel.z);
        }
        if (trackRot)
        {
            datas.Add("RotTravelX", rotTravel.x);
            datas.Add("RotTravelY", rotTravel.y);
            datas.Add("RotTravelZ", rotTravel.z);
        }
        if (trackTrigger) {
            trigger = false;
            if(SteamVR.active)
            {
                if (SteamVR_Actions._default.GrabPinch.GetStateDown(controller))
                {
                    trigger = true;
                }
            }
            datas.Add("Trigger", trigger);
        }
        ResetTravel();
        return datas;
    }

    // Starts the tracking.
    public void StartTracking()
    {
        if (isTracking) return;

        if (trackPos)
        {
            previousRot = transform.eulerAngles;
        }
        if (trackRot)
        {
            previousPos = transform.position;
        }
        isTracking = true;
    }

    // Stops the tracking.
    public void StopTracking()
    {
        if (!isTracking) return;

        isTracking = false;
        ResetTravel();
    }

    // Resets the travelled position/rotation.
    private void ResetTravel()
    {
        if (trackPos)
        {
            posTravel = Vector3.zero;
            previousPos = transform.position;
        }
        if (trackRot)
        {
            rotTravel = Vector3.zero;
            previousRot = transform.eulerAngles;
        }
    }

    // Updates the travelled position/rotation.
    private void UpdateTravel()
    {
        if (trackPos)
        {
            posTravel += VectorAbs(transform.position - previousPos);
            previousPos = transform.position;
        }
        if (trackRot)
        {
            rotTravel += VectorAbs(transform.eulerAngles - previousRot);
            previousRot = transform.eulerAngles;
        }
    }

    private Vector3 VectorAbs(Vector3 vector)
    {
        return new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
    }
}
