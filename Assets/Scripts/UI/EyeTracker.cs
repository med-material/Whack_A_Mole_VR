using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VIVE.OpenXR;
using VIVE.OpenXR.EyeTracker;

public class  EyeData
{
    public Vector3 pos;
    public Quaternion rot;
    public bool validGaze = true;
    public bool validGeo = true;
    public float openness;
    public float squeeze;
    public float wide;
}

public class EyeTracker : MonoBehaviour
{
    [NonSerialized]
    public EyeData left, right;

    [SerializeField]
    public Transform leftEyeTarget, rightEyeTarget;

    // Start is called before the first frame update
    void Start()
    {
        left = new EyeData();
        right = new EyeData();
    }

    // Update is called once per frame
    void Update()
    {
        XR_HTC_eye_tracker.Interop.GetEyeGazeData(out XrSingleEyeGazeDataHTC[] out_gazes);
        XrSingleEyeGazeDataHTC leftGaze = out_gazes[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
        XrSingleEyeGazeDataHTC rightGaze = out_gazes[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];
        XR_HTC_eye_tracker.Interop.GetEyeGeometricData(out XrSingleEyeGeometricDataHTC[] out_geo);
        XrSingleEyeGeometricDataHTC leftGeo = out_geo[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
        XrSingleEyeGeometricDataHTC rightGeo = out_geo[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];

        if (leftGaze.isValid)
        {
            left.pos = leftGaze.gazePose.position.ToUnityVector();
            left.rot = leftGaze.gazePose.orientation.ToUnityQuaternion();
            left.validGaze = true;
        }
        else
        {
            leftEyeTarget.transform.localPosition = new Vector3(0, -1000, 0);
            left.validGaze = false;
        }

        if (rightGaze.isValid)
        {
            right.pos = rightGaze.gazePose.position.ToUnityVector();
            right.rot = rightGaze.gazePose.orientation.ToUnityQuaternion();
        }
        else
        {
            rightEyeTarget.transform.localPosition = new Vector3(0, -1000, 0);
        }

        if(leftGeo.isValid)
        {
            left.validGeo = true;
            left.openness = leftGeo.eyeOpenness;
            left.squeeze = leftGeo.eyeSqueeze;
            left.wide = leftGeo.eyeWide;
        }
        else
        {
            left.validGeo = false;
        }
        if (rightGeo.isValid)
        {
            right.validGeo = true;
            right.openness = rightGeo.eyeOpenness;
            right.squeeze = rightGeo.eyeSqueeze;
            right.wide = rightGeo.eyeWide;
        }
        else
        {
            right.validGeo = false;
        }
    }
}
