using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MotorSpaceManager : MonoBehaviour
{

    public enum ActiveMotorSpace { R, L, Both };

    public ActiveMotorSpace motorspace = ActiveMotorSpace.R;

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

    bool isMirror = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetActiveMotorSpace(string newMotorSpace) {
        motorspace = (MotorSpaceManager.ActiveMotorSpace)System.Enum.Parse( typeof(MotorSpaceManager.ActiveMotorSpace), newMotorSpace);
        bool R = motorspace == ActiveMotorSpace.R ? true : false;
        R = motorspace == ActiveMotorSpace.Both ? true : R;
        bool L = motorspace == ActiveMotorSpace.L ? true : false;
        L = motorspace == ActiveMotorSpace.Both ? true : L;
        bool mirrorR = isMirror;
        mirrorR = motorspace == ActiveMotorSpace.R ? mirrorR : false;
        mirrorR = motorspace == ActiveMotorSpace.Both ? false : mirrorR;
        bool mirrorL = isMirror;
        mirrorL = motorspace == ActiveMotorSpace.L ? mirrorL : false;
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

        if (motorspace == ActiveMotorSpace.R) {
            MotorSpaceMirrorRight.gameObject.SetActive(true);
            MotorSpaceMirrorLeft.gameObject.SetActive(false);
            MotorSpaceMirrorRight.UpdateMotorSpaceToMirror(MotorSpaceRight);
        } else if (motorspace == ActiveMotorSpace.L) {
            MotorSpaceMirrorLeft.gameObject.SetActive(true);
            MotorSpaceMirrorRight.gameObject.SetActive(false);
            MotorSpaceMirrorLeft.UpdateMotorSpaceToMirror(MotorSpaceLeft);
        }
        
    }

    public void CalibrateActiveMotorspace(bool calibrate) {
        if (motorspace == ActiveMotorSpace.R) {
            MotorSpaceRight.ToggleMotorCalibration(calibrate);
        } else if (motorspace == ActiveMotorSpace.L) {
            MotorSpaceLeft.ToggleMotorCalibration(calibrate);
        } else if (motorspace == ActiveMotorSpace.Both) {
            MotorSpaceRight.ToggleMotorCalibration(calibrate);
            MotorSpaceLeft.ToggleMotorCalibration(calibrate);
        }
    }

    public void ResetActiveMotorspace() {
        if (motorspace == ActiveMotorSpace.R) {
            MotorSpaceRight.SetDefaultMotorSpace();
        } else if (motorspace == ActiveMotorSpace.L) {
            MotorSpaceLeft.SetDefaultMotorSpace();
        } else if (motorspace == ActiveMotorSpace.Both) {
            MotorSpaceRight.SetDefaultMotorSpace();
            MotorSpaceLeft.SetDefaultMotorSpace();
        }
    }

    public void SetActiveMotorSpaceMultiplier() {
        if (motorspace == ActiveMotorSpace.R) {

        }
    }

    public void OnMultiplierSliderChanged() {
        var sliderValue = (float) motorSpaceSlider.value;
        var highVal = (float) motorSpaceSlider.maxValue;
        var lowVal = (float) motorSpaceSlider.minValue;
        float multiplier = (sliderValue - lowVal) / highVal;

        if (motorspace == ActiveMotorSpace.R) {
            MotorSpaceRight.SetMultiplier(multiplier);
            if (isMirror) MotorSpaceMirrorRight.UpdateMotorSpaceToMirror(MotorSpaceRight);
        } else if (motorspace == ActiveMotorSpace.L) {
            MotorSpaceLeft.SetMultiplier(multiplier);
            if (isMirror) MotorSpaceMirrorLeft.UpdateMotorSpaceToMirror(MotorSpaceLeft);
        } else if (motorspace == ActiveMotorSpace.Both) {
            MotorSpaceRight.SetMultiplier(multiplier);
            MotorSpaceLeft.SetMultiplier(multiplier);
        }
        
    }
}
