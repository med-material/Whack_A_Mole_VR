using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MotorSpaceManager : MonoBehaviour
{

    public enum ActiveMotorSpace { Right, Left, Both, Off };

    public ActiveMotorSpace motorspace = ActiveMotorSpace.Right;

    [SerializeField]
    public LaserMapper MotorSpaceRight;

    [SerializeField]
    public LaserMapper MotorSpaceLeft;

    [SerializeField]
    public MotorSpaceMirror MotorSpaceMirrorRight;

    [SerializeField]
    public MotorSpaceMirror MotorSpaceMirrorLeft;

    [SerializeField]
    private Slider motorSpaceSlider;

    [SerializeField]
    private MotorSpaceInfo motorSpaceTiny1;

    [SerializeField]
    private MotorSpaceInfo motorSpaceTiny2;

    [SerializeField]
    private MotorSpaceInfo motorSpaceTiny3;

    [SerializeField]
    private MotorSpaceInfo motorSpaceSmall;
    [SerializeField]
    private MotorSpaceInfo motorSpaceMedium;
    [SerializeField]
    private MotorSpaceInfo motorSpaceLarge;

    bool isMirror = false;

    void Start() {
        MotorSpaceInfo mRight = (MotorSpaceInfo) motorSpaceLarge.Clone();
        MotorSpaceRight.SetDefaultMotorSpace(mRight);

        MotorSpaceInfo mLeft = (MotorSpaceInfo)mRight.Clone();
        mLeft.pos = new Vector3(mLeft.pos.x * (-1), mLeft.pos.y, mLeft.pos.z); // inverse position for left side.
        MotorSpaceLeft.SetDefaultMotorSpace(mLeft);
    }

    public void SetActiveMotorSpace(string newMotorSpace) {
        motorspace = (MotorSpaceManager.ActiveMotorSpace)System.Enum.Parse( typeof(MotorSpaceManager.ActiveMotorSpace), newMotorSpace);

        bool R = motorspace == ActiveMotorSpace.Right ? true : false;
        R = motorspace == ActiveMotorSpace.Both ? true : R;
        bool L = motorspace == ActiveMotorSpace.Left ? true : false;
        L = motorspace == ActiveMotorSpace.Both ? true : L;
        bool mirrorR = isMirror;
        mirrorR = motorspace == ActiveMotorSpace.Right ? mirrorR : false;
        mirrorR = motorspace == ActiveMotorSpace.Both ? false : mirrorR;
        bool mirrorL = isMirror;
        mirrorL = motorspace == ActiveMotorSpace.Left ? mirrorL : false;
        mirrorL = motorspace == ActiveMotorSpace.Both ? false : mirrorL;

        MotorSpaceRight.gameObject.SetActive(R);
        MotorSpaceLeft.gameObject.SetActive(L);
        MotorSpaceMirrorRight.gameObject.SetActive(mirrorR);
        MotorSpaceMirrorLeft.gameObject.SetActive(mirrorL);
    }

    public void SetMotorRestriction(MotorRestriction restriction, float lower, float upper) {
        var mirrorUpper = 1f - lower;
        var mirrorLower = 1f - upper;

        if (motorspace == ActiveMotorSpace.Right) {
            MotorSpaceRight.SetMotorRestriction(restriction, lower, upper);
        } else if (motorspace == ActiveMotorSpace.Left) {
            MotorSpaceLeft.SetMotorRestriction(restriction, mirrorLower, mirrorUpper);
        } else if (motorspace == ActiveMotorSpace.Both) {
            MotorSpaceRight.SetMotorRestriction(restriction, lower, upper);
            MotorSpaceLeft.SetMotorRestriction(restriction, mirrorLower, mirrorUpper);
        }



        if (isMirror) {
            if (motorspace == ActiveMotorSpace.Right) {
                LaserMapper MotorSpaceMirrorR = MotorSpaceMirrorRight.gameObject.GetComponent<LaserMapper>();
               MotorSpaceMirrorR.SetMotorRestriction(restriction, mirrorLower, mirrorUpper);
            } else if (motorspace == ActiveMotorSpace.Left) {
                LaserMapper MotorSpaceMirrorL = MotorSpaceMirrorLeft.gameObject.GetComponent<LaserMapper>();
                MotorSpaceMirrorL.SetMotorRestriction(restriction, lower, upper);
            }
        }

    }

    public void SetMirror(bool setMirror) {
        isMirror = setMirror;
        if (!isMirror) {
            MotorSpaceMirrorLeft.gameObject.SetActive(false);
            MotorSpaceMirrorRight.gameObject.SetActive(false);
            return;
        }
        if (motorspace == ActiveMotorSpace.Both) {
            return;
        }

        if (motorspace == ActiveMotorSpace.Right) {
            MotorSpaceMirrorRight.gameObject.SetActive(true);
            MotorSpaceMirrorLeft.gameObject.SetActive(false);
            MotorSpaceMirrorRight.UpdateMotorSpaceToMirror(MotorSpaceRight);
        } else if (motorspace == ActiveMotorSpace.Left) {
            MotorSpaceMirrorLeft.gameObject.SetActive(true);
            MotorSpaceMirrorRight.gameObject.SetActive(false);
            MotorSpaceMirrorLeft.UpdateMotorSpaceToMirror(MotorSpaceLeft);
        }
        
    }

    public void CalibrateActiveMotorspace(bool calibrate) {
        if (motorspace == ActiveMotorSpace.Right) {
            MotorSpaceRight.ToggleMotorCalibration(calibrate);
        } else if (motorspace == ActiveMotorSpace.Left) {
            MotorSpaceLeft.ToggleMotorCalibration(calibrate);
        } else if (motorspace == ActiveMotorSpace.Both) {
            MotorSpaceRight.ToggleMotorCalibration(calibrate);
            MotorSpaceLeft.ToggleMotorCalibration(calibrate);
        }
    }

    public void ResetActiveMotorspace() {
        if (motorspace == ActiveMotorSpace.Right) {
            MotorSpaceRight.SetDefaultMotorSpace();
        } else if (motorspace == ActiveMotorSpace.Left) {
            MotorSpaceLeft.SetDefaultMotorSpace();
        } else if (motorspace == ActiveMotorSpace.Both) {
            MotorSpaceRight.SetDefaultMotorSpace();
            MotorSpaceLeft.SetDefaultMotorSpace();
        }
    }

    public void SetActiveMotorSpaceMultiplier() {
        if (motorspace == ActiveMotorSpace.Right) {

        }
    }

    public void SetMotorSpaceTiny3()
    {
        SetMotorSpace((MotorSpaceInfo)motorSpaceTiny3.Clone());
    }

    public void SetMotorSpaceTiny2()
    {
        SetMotorSpace((MotorSpaceInfo)motorSpaceTiny2.Clone());
    }

    public void SetMotorSpaceTiny1()
    {
        SetMotorSpace((MotorSpaceInfo)motorSpaceTiny1.Clone());
    }

    public void SetMotorSpaceSmall() {
        SetMotorSpace((MotorSpaceInfo) motorSpaceSmall.Clone());
    }

    public void SetMotorSpaceMedium() {
        SetMotorSpace((MotorSpaceInfo) motorSpaceMedium.Clone());
    }

    public void SetMotorSpaceLarge() {
        SetMotorSpace((MotorSpaceInfo) motorSpaceLarge.Clone());
    }
    
    public void ShowTaskFeedback(float duration, Dictionary<int, float> molePerf, float animationDelay) {
        if(motorspace == ActiveMotorSpace.Right)
            MotorSpaceRight.GetCurrentController().GetComponent<Pointer>().ShowTaskFeedback(duration, molePerf, animationDelay);
        else if (motorspace == ActiveMotorSpace.Left)
            MotorSpaceLeft.GetCurrentController().GetComponent<Pointer>().ShowTaskFeedback(duration, molePerf, animationDelay);
        else if (motorspace == ActiveMotorSpace.Both)
        {
            MotorSpaceRight.GetCurrentController().GetComponent<Pointer>().ShowTaskFeedback(duration, molePerf, animationDelay);
            MotorSpaceRight.GetCurrentController().GetComponent<Pointer>().ShowTaskFeedback(duration, molePerf, animationDelay);
        }
    }

    public void SetMotorSpaceOutOfBoundsSignifierStatic()
    {
        if(motorspace == ActiveMotorSpace.Right)
            MotorSpaceRight.SetMotorSpaceOutOfBoundsSignifierStatic();
        else if (motorspace == ActiveMotorSpace.Left)
            MotorSpaceLeft.SetMotorSpaceOutOfBoundsSignifierStatic();
        else if (motorspace == ActiveMotorSpace.Both)
        {
            MotorSpaceRight.SetMotorSpaceOutOfBoundsSignifierStatic();
            MotorSpaceLeft.SetMotorSpaceOutOfBoundsSignifierStatic();
        }
    }

    public void SetMotorSpaceOutOfBoundsSignifierDynamic()
    {
        if (motorspace == ActiveMotorSpace.Right)
            MotorSpaceRight.SetMotorSpaceOutOfBoundsSignifierDynamic();
        else if (motorspace == ActiveMotorSpace.Left)
            MotorSpaceLeft.SetMotorSpaceOutOfBoundsSignifierDynamic();
        else if (motorspace == ActiveMotorSpace.Both)
        {
            MotorSpaceRight.SetMotorSpaceOutOfBoundsSignifierDynamic();
            MotorSpaceLeft.SetMotorSpaceOutOfBoundsSignifierDynamic();
        }
    }

    internal void SetMotorSpaceOutOfBoundsSignifierDynamicReversed()
    {
        if (motorspace == ActiveMotorSpace.Right)
            MotorSpaceRight.SetMotorSpaceOutOfBoundsSignifierDynamicReversed();
        else if (motorspace == ActiveMotorSpace.Left)
            MotorSpaceLeft.SetMotorSpaceOutOfBoundsSignifierDynamicReversed();
        else if (motorspace == ActiveMotorSpace.Both)
        {
            MotorSpaceRight.SetMotorSpaceOutOfBoundsSignifierDynamicReversed();
            MotorSpaceLeft.SetMotorSpaceOutOfBoundsSignifierDynamicReversed();
        }
    }

    internal void DisableMotorSpaceOutOfBoundsSignifier()
    {
        if (motorspace == ActiveMotorSpace.Right)
            MotorSpaceRight.DisableMotorSpaceOutOfBoundsSignifier();
        else if (motorspace == ActiveMotorSpace.Left)
            MotorSpaceLeft.DisableMotorSpaceOutOfBoundsSignifier();
        else if (motorspace == ActiveMotorSpace.Both)
        {
            MotorSpaceRight.DisableMotorSpaceOutOfBoundsSignifier();
            MotorSpaceLeft.DisableMotorSpaceOutOfBoundsSignifier();
        }
    }

    internal void SetTaskPerformanceFeedback(bool val)
    {
            if (motorspace == ActiveMotorSpace.Right)
                MotorSpaceRight.GetCurrentController().GetComponent<Pointer>().SetTaskPerformanceFeedback(val);
            else if (motorspace == ActiveMotorSpace.Left)
                MotorSpaceLeft.GetCurrentController().GetComponent<Pointer>().SetTaskPerformanceFeedback(val);
            else if (motorspace == ActiveMotorSpace.Both)
            {
                MotorSpaceRight.GetCurrentController().GetComponent<Pointer>().SetTaskPerformanceFeedback(val);
                MotorSpaceLeft.GetCurrentController().GetComponent<Pointer>().SetTaskPerformanceFeedback(val);
            }
    }

    internal void SetOperationPerformanceFeedback(bool v, bool withText)
    {
        if (!v)
        {
            MotorSpaceRight.SetOperationPerformanceFeedback(false, withText);
            MotorSpaceLeft.SetOperationPerformanceFeedback(false, withText);
        } else
        {
            if (motorspace == ActiveMotorSpace.Right) {
                MotorSpaceRight.SetOperationPerformanceFeedback(true, withText);
                MotorSpaceRight.GetCurrentController().GetComponent<Pointer>().SetOperationPerformanceFeedback(v);
            } else if (motorspace == ActiveMotorSpace.Left) {
                MotorSpaceLeft.SetOperationPerformanceFeedback(true, withText);
                MotorSpaceLeft.GetCurrentController().GetComponent<Pointer>().SetOperationPerformanceFeedback(v);
            } else if (motorspace == ActiveMotorSpace.Both)
            {
                MotorSpaceRight.SetOperationPerformanceFeedback(true, withText);
                MotorSpaceRight.GetCurrentController().GetComponent<Pointer>().SetOperationPerformanceFeedback(v);
                MotorSpaceLeft.SetOperationPerformanceFeedback(true, withText);
                MotorSpaceLeft.GetCurrentController().GetComponent<Pointer>().SetOperationPerformanceFeedback(v);
            }
        }
    }

    public void SetMotorSpace(MotorSpaceInfo m) {
        bool mRState = MotorSpaceRight.gameObject.activeSelf;
        bool mLState = MotorSpaceLeft.gameObject.activeSelf;

        MotorSpaceRight.gameObject.SetActive(true);
        MotorSpaceRight.SetMotorSpace(m);
        MotorSpaceRight.gameObject.SetActive(mRState);

        MotorSpaceInfo m2 = (MotorSpaceInfo) m.Clone();
        m2.pos = new Vector3(m2.pos.x * (-1), m2.pos.y, m2.pos.z);

        MotorSpaceLeft.gameObject.SetActive(true);
        MotorSpaceLeft.SetMotorSpace(m2);
        MotorSpaceLeft.gameObject.SetActive(mLState);

        if (isMirror) MotorSpaceMirrorRight.UpdateMotorSpaceToMirror(MotorSpaceRight);
        if (isMirror) MotorSpaceMirrorLeft.UpdateMotorSpaceToMirror(MotorSpaceLeft);
    }

    public void OnMultiplierSliderChanged() {
        var sliderValue = (float) motorSpaceSlider.value;
        var highVal = (float) motorSpaceSlider.maxValue;
        var lowVal = (float) motorSpaceSlider.minValue;
        float multiplier = (sliderValue - lowVal) / highVal;

        if (motorspace == ActiveMotorSpace.Right) {
            MotorSpaceRight.SetMultiplier(multiplier);
            if (isMirror) MotorSpaceMirrorRight.UpdateMotorSpaceToMirror(MotorSpaceRight);
        } else if (motorspace == ActiveMotorSpace.Left) {
            MotorSpaceLeft.SetMultiplier(multiplier);
            if (isMirror) MotorSpaceMirrorLeft.UpdateMotorSpaceToMirror(MotorSpaceLeft);
        } else if (motorspace == ActiveMotorSpace.Both) {
            MotorSpaceRight.SetMultiplier(multiplier);
            MotorSpaceLeft.SetMultiplier(multiplier);
        }
        
    }

    
}
