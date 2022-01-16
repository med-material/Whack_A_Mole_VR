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

    public void SetMotorSpaceSmall() {
        SetMotorSpace((MotorSpaceInfo) motorSpaceSmall.Clone());
    }

    public void SetMotorSpaceMedium() {
        SetMotorSpace((MotorSpaceInfo) motorSpaceMedium.Clone());
    }

    public void SetMotorSpaceLarge() {
        SetMotorSpace((MotorSpaceInfo) motorSpaceLarge.Clone());
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
