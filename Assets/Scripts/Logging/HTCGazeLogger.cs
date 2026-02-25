using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using VIVE.OpenXR;
using VIVE.OpenXR.EyeTracker;

public class HTCGazeLogger : DataProvider
{
    private bool GazeValid0;
    private bool GazeValid1;
    private Vector3 localEyePosition0;
    private Vector3 localEyePosition1;
    private Quaternion localEyeRotation0;
    private Quaternion localEyeRotation1;
    private bool GazeHit0;
    private bool GazeHit1;
    private Vector3 GazeHitPosition0;
    private Vector3 GazeHitPosition1;
    private string? GazeHitObject0;
    private string? GazeHitObject1;
    private bool PupilValid0;
    private bool PupilValid1;
    private float? PupilDiameter0;
    private float? PupilDiameter1;
    private Vector2 PupilPosition0;
    private Vector2 PupilPosition1;
    private bool GeometryValid0;
    private bool GeometryValid1;
    private float? Openness0;
    private float? EyeSqueeze0;
    private float? EyeWide0;
    private float? Openness1;
    private float? EyeSqueeze1;
    private float? EyeWide1;

    private const float EYE_OPENNESS_THRESHOLD = 0.4f;

    void Update()
    {
        //updateData();
    }

    private void updateData()
    {
        XrSingleEyeGazeDataHTC leftGaze,  rightGaze;
        XrSingleEyeGeometricDataHTC leftGeo, rightGeo;
        XrSingleEyePupilDataHTC leftPup, rightPup;
        try
        {
            XR_HTC_eye_tracker.Interop.GetEyeGazeData(out XrSingleEyeGazeDataHTC[] out_gazes);
            leftGaze = out_gazes[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
            rightGaze = out_gazes[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];
            XR_HTC_eye_tracker.Interop.GetEyeGeometricData(out XrSingleEyeGeometricDataHTC[] out_geo);
            leftGeo = out_geo[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
            rightGeo = out_geo[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];
            XR_HTC_eye_tracker.Interop.GetEyePupilData(out XrSingleEyePupilDataHTC[] out_pup);
            leftPup = out_pup[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
            rightPup = out_pup[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];
        }
        catch (NullReferenceException e)
        {
            Debug.LogWarning("Could not fetch eye data : " + e);
            leftGeo = new XrSingleEyeGeometricDataHTC();
            leftGeo.isValid = false;
            rightGeo = new XrSingleEyeGeometricDataHTC();
            rightGeo.isValid = false;
            leftGaze = new XrSingleEyeGazeDataHTC();
            leftGaze.isValid = false;
            rightGaze = new XrSingleEyeGazeDataHTC();
            rightGaze.isValid = false;
            leftPup = new XrSingleEyePupilDataHTC();
            leftPup.isDiameterValid = false;
            leftPup.isPositionValid = false;
            rightPup = new XrSingleEyePupilDataHTC();
            rightPup.isDiameterValid = false;
            rightPup.isPositionValid = false;
        }

        if (leftGeo.isValid)
        {
            GeometryValid0 = true;
            Openness0 = leftGeo.eyeOpenness;
            EyeSqueeze0 = leftGeo.eyeSqueeze;
            EyeWide0 = leftGeo.eyeWide;
        }
        else
        {
            GeometryValid0 = false;
            Openness0 = null;
            EyeSqueeze0 = null;
            EyeWide0 = null;
        }
        if (rightGeo.isValid)
        {
            GeometryValid1 = true;
            Openness1 = rightGeo.eyeOpenness;
            EyeSqueeze1 = rightGeo.eyeSqueeze;
            EyeWide1 = rightGeo.eyeWide;
        }
        else
        {
            GeometryValid1 = false;
            Openness1 = null;
            EyeSqueeze1 = null;
            EyeWide1 = null;
        }

        if (EYE_OPENNESS_THRESHOLD < Openness0 && leftGaze.isValid)
        {
            localEyePosition0 = leftGaze.gazePose.position.ToUnityVector();
            localEyeRotation0 = leftGaze.gazePose.orientation.ToUnityQuaternion();
            GazeValid0 = true;
            Vector3 gazeNormal0 = localEyeRotation0 * Vector3.forward;
            Debug.DrawRay(localEyePosition0, gazeNormal0, Color.red, 0.05f, false);
            RaycastHit hit;
            if (Physics.Raycast(localEyePosition0, gazeNormal0, out hit))
            {
                GazeHit0 = true;
                GazeHitPosition0 = hit.point;
                GazeHitObject0 = hit.collider.gameObject.name;
            }
            else
            {
                GazeHit0 = false;
                GazeHitPosition0 = Vector3.zero;
                GazeHitObject0 = null;
            }
        }
        else
        {
            GazeValid0 = false;
            GazeHit0 = false;
            GazeHitPosition0 = Vector3.zero;
            GazeHitObject0 = null;
            localEyePosition0 = Vector3.zero;
            localEyeRotation0 = Quaternion.identity;
            GazeValid0 = false;
        }
        if (EYE_OPENNESS_THRESHOLD < Openness1 && rightGaze.isValid)
        {
            localEyePosition1 = rightGaze.gazePose.position.ToUnityVector();
            localEyeRotation1 = rightGaze.gazePose.orientation.ToUnityQuaternion();
            GazeValid1 = true;
            Vector3 gazeNormal1 = localEyeRotation1 * Vector3.forward;
            Debug.DrawRay(localEyePosition1, gazeNormal1, Color.red, 0.05f, false);
            RaycastHit hit;
            if (Physics.Raycast(localEyePosition1, gazeNormal1, out hit))
            {
                GazeHit1 = true;
                GazeHitPosition1 = hit.point;
                GazeHitObject1 = hit.collider.gameObject.name;
            }
            else
            {
                GazeHit1 = false;
                GazeHitPosition1 = Vector3.zero;
                GazeHitObject1 = null;
            }
        }
        else
        {
            GazeValid1 = false;
            localEyePosition1 = Vector3.zero;
            localEyeRotation1 = Quaternion.identity;
            GazeHit1 = false;
            GazeHitPosition1 = Vector3.zero;
            GazeHitObject1 = null;
        }

        if (EYE_OPENNESS_THRESHOLD < Openness0 && leftPup.isDiameterValid && leftPup.isPositionValid)
        {
            PupilValid0 = true;
            PupilDiameter0 = leftPup.pupilDiameter;
            PupilPosition0 = new Vector2(leftPup.pupilPosition.x, leftPup.pupilPosition.y);
        }
        else
        {
            PupilValid0 = false;
            PupilDiameter0 = null;
            PupilPosition0 = Vector2.zero;
        }
        if (EYE_OPENNESS_THRESHOLD < Openness1 && rightPup.isDiameterValid && rightPup.isPositionValid)
        {
            PupilValid1 = true;
            PupilDiameter1 = rightPup.pupilDiameter;
            PupilPosition1 = new Vector2(rightPup.pupilPosition.x, rightPup.pupilPosition.y);
        }
        else
        {
            PupilValid1 = false;
            PupilDiameter1 = null;
            PupilPosition1 = Vector2.zero;
        }

    }

    // Returns a dictionnary with the calculated gaze data.
    // 0 for left eye, 1 for right eye.
    public override Dictionary<string, object> GetData()
    {
        updateData();
        return new Dictionary<string, object>(){
            {"GazeValid0", GazeValid0},
            {"GazeValid1", GazeValid1},
            {"LocalEyePosition0X", localEyePosition0.x},
            {"LocalEyePosition1X", localEyePosition1.x},
            {"LocalEyePosition0Y", localEyePosition0.y},
            {"LocalEyePosition1Y", localEyePosition1.y},
            {"LocalEyePosition0Z", localEyePosition0.z},
            {"LocalEyePosition1Z", localEyePosition1.z},
            {"LocalEyeRotation0X", localEyeRotation0.x},
            {"LocalEyeRotation1X", localEyeRotation1.x},
            {"LocalEyeRotation0Y", localEyeRotation0.y},
            {"LocalEyeRotation1Y", localEyeRotation1.y},
            {"LocalEyeRotation0Z", localEyeRotation0.z},
            {"LocalEyeRotation1Z", localEyeRotation1.z},
            {"LocalEyeRotation0W", localEyeRotation0.w},
            {"LocalEyeRotation1W", localEyeRotation1.w},
            {"GazeHit0", GazeHit0},
            {"GazeHit1", GazeHit1},
            {"GazeHitPosition0X", GazeHitPosition0.x},
            {"GazeHitPosition1X", GazeHitPosition1.x},
            {"GazeHitPosition0Y", GazeHitPosition0.y},
            {"GazeHitPosition1Y", GazeHitPosition1.y},
            {"GazeHitPosition0Z", GazeHitPosition0.z},
            {"GazeHitPosition1Z", GazeHitPosition1.z},
            {"GazeHitObject0", GazeHitObject0},
            {"GazeHitObject1", GazeHitObject1},
            {"PupilValid0", PupilValid0},
            {"PupilValid1", PupilValid1},
            {"PupilDiameter0", PupilDiameter0},
            {"PupilDiameter1", PupilDiameter1},
            {"PupilPosition0X", PupilPosition0.x},
            {"PupilPosition1X", PupilPosition1.x},
            {"PupilPosition0Y", PupilPosition0.y},
            {"PupilPosition1Y", PupilPosition1.y},
            {"GeometryValid0", GeometryValid0},
            {"GeometryValid1", GeometryValid1},
            {"Openness0", Openness0},
            {"Openness1", Openness1},
            {"EyeSqueeze0", EyeSqueeze0},
            {"EyeSqueeze1", EyeSqueeze1},
            {"EyeWide0", EyeWide0},
            {"EyeWide1", EyeWide1}
        };
    }
}
