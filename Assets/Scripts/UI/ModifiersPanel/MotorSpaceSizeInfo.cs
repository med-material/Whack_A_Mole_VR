using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MotorSpaceSizeInfo : MonoBehaviour
{

    [SerializeField]
    private LaserMapper laserMapperL;

    [SerializeField]
    private LaserMapper laserMapperR;

    [SerializeField]
    private Text widthText;
    private string widthTextTemplate = "";

    [SerializeField]
    private Text heightText;
    private string heightTextTemplate = "";

    string mRWidth = "None";
    string mLWidth = "None";
    
    string mRHeight = "None";
    string mLHeight = "None";

    bool awake = false;

    // Start is called before the first frame update
    void Awake()
    {
        heightTextTemplate = heightText.text;
        widthTextTemplate = widthText.text;
        awake = true;
    }

    void Start()
    {
        widthText.text = string.Format(widthTextTemplate, "None", "None");
        heightText.text = string.Format(heightTextTemplate, "None", "None");
        
        var mL = laserMapperL.GetMotorSpace();
        mLWidth = mL.width.ToString("0.00");
        mLHeight = mL.height.ToString("0.00");

        var mR = laserMapperR.GetMotorSpace();
        mRWidth = mR.width.ToString("0.00");
        mRHeight = mR.height.ToString("0.00");
        UpdateInfo();

    }

    public void OnMotorSpaceRightChanged(MotorSpaceInfo m) {
        mRWidth = m.width.ToString("0.00");
        mRHeight = m.height.ToString("0.00");
        if (awake) {
            UpdateInfo();
        }
    }

    public void OnMotorSpaceLeftChanged(MotorSpaceInfo m) {
        mLWidth = m.width.ToString("0.00");
        mLHeight = m.height.ToString("0.00");
        if (awake) {
            UpdateInfo();
        }
    }

    private void UpdateInfo() {
        widthText.text = string.Format(widthTextTemplate, mRWidth, mLWidth);
        heightText.text = string.Format(heightTextTemplate, mRHeight, mLHeight);
        widthText.text = string.Format(widthTextTemplate, mRWidth, mLWidth);
        heightText.text = string.Format(heightTextTemplate, mRHeight, mLHeight);
    }

}

