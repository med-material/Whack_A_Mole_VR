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
    private Vector3? localEyePosition0;
    private Vector3? localEyePosition1;
    private Quaternion? localEyeRotation0;
    private Quaternion? localEyeRotation1;
    private Vector3? gazeNormal0;
    private Vector3? gazeNormal1;
    private bool GazeHit0;
    private bool GazeHit1;
    private Vector3? GazeHitPosition0;
    private Vector3? GazeHitPosition1;
    private string? GazeHitObject0;
    private string? GazeHitObject1;
    private bool PupilValid0;
    private bool PupilValid1;
    private float? PupilDiameter0;
    private float? PupilDiameter1;
    private Vector2? PupilPosition0;
    private Vector2? PupilPosition1;
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
            gazeNormal0 = localEyeRotation0 * Vector3.forward;
            Debug.DrawRay(localEyePosition0??Vector3.zero, gazeNormal0??Vector3.forward, Color.red, 0.05f, false);
            RaycastHit hit;
            if (Physics.Raycast(localEyePosition0 ?? Vector3.zero, gazeNormal0 ?? Vector3.forward, out hit))
            {
                GazeHit0 = true;
                GazeHitPosition0 = hit.point;
                GazeHitObject0 = hit.collider.gameObject.name;
            }
            else
            {
                GazeHit0 = false;
                GazeHitPosition0 = null;
                GazeHitObject0 = null;
            }
        }
        else
        {
            GazeValid0 = false;
            GazeHit0 = false;
            GazeHitPosition0 = null;
            gazeNormal0 = null;
            GazeHitObject0 = null;
            localEyePosition0 = null;
            localEyeRotation0 = null;
            GazeValid0 = false;
        }
        if (EYE_OPENNESS_THRESHOLD < Openness1 && rightGaze.isValid)
        {
            localEyePosition1 = rightGaze.gazePose.position.ToUnityVector();
            localEyeRotation1 = rightGaze.gazePose.orientation.ToUnityQuaternion();
            GazeValid1 = true;
            gazeNormal1 = localEyeRotation1 * Vector3.forward;
            Debug.DrawRay(localEyePosition1 ?? Vector3.forward, gazeNormal1 ?? Vector3.forward, Color.red, 0.05f, false);
            RaycastHit hit;
            if (Physics.Raycast(localEyePosition1 ?? Vector3.zero, gazeNormal1 ?? Vector3.forward, out hit))
            {
                GazeHit1 = true;
                GazeHitPosition1 = hit.point;
                GazeHitObject1 = hit.collider.gameObject.name;
            }
            else
            {
                GazeHit1 = false;
                GazeHitPosition1 = null;
                GazeHitObject1 = null;
            }
        }
        else
        {
            GazeValid1 = false;
            localEyePosition1 = null;
            localEyeRotation1 = null;
            gazeNormal1 = null;
            GazeHit1 = false;
            GazeHitPosition1 = null;
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
            PupilPosition0 = null;
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
            PupilPosition1 = null;
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
            {"GazeNormal0X", gazeNormal0?.x},
            {"GazeNormal1X", gazeNormal1?.x},
            {"GazeNormal0Y", gazeNormal0?.y},
            {"GazeNormal1Y", gazeNormal1?.y},
            {"GazeNormal0Z", gazeNormal0?.z},
            {"GazeNormal1Z", gazeNormal1?.z},
            {"GazeHit0", GazeHit0},
            {"GazeHit1", GazeHit1},
            {"GazeHitPosition0X", GazeHitPosition0?.x},
            {"GazeHitPosition1X", GazeHitPosition1?.x},
            {"GazeHitPosition0Y", GazeHitPosition0?.y},
            {"GazeHitPosition1Y", GazeHitPosition1?.y},
            {"GazeHitPosition0Z", GazeHitPosition0?.z},
            {"GazeHitPosition1Z", GazeHitPosition1?.z},
            {"GazeHitObject0", GazeHitObject0},
            {"GazeHitObject1", GazeHitObject1},
            {"PupilValid0", PupilValid0},
            {"PupilValid1", PupilValid1},
            {"PupilDiameter0", PupilDiameter0},
            {"PupilDiameter1", PupilDiameter1},
            {"PupilPosition0X", PupilPosition0?.x},
            {"PupilPosition1X", PupilPosition1?.x},
            {"PupilPosition0Y", PupilPosition0?.y},
            {"PupilPosition1Y", PupilPosition1?.y},
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
