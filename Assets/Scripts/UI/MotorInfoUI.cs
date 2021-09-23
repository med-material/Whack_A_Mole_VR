using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MotorInfoUI : MonoBehaviour
{

    [SerializeField]
    private Text infoRight;
    private string infoRightTemplate;

    [SerializeField]
    private Text infoLeft;
    private string infoLeftTemplate;

    [SerializeField]
    private Text infoError;
    private string infoErrorTemplate;

    [SerializeField]
    private Color errorColor;
    private Color defaultColor;

    [SerializeField]
    private GameObject controllerRight; 

    [SerializeField]
    private GameObject controllerLeft;

    private string textR;
    private string textL;
    private string textError;
    private Color colorR;
    private Color colorL;
    private Color colorError;
    private Vector3 lastPosRight = Vector3.zero;
    private Vector3 lastPosLeft = Vector3.zero;

    void OnEnable() {
    }

    // Start is called before the first frame update
    void Start()
    {
        infoRightTemplate = infoRight.text;
        infoLeftTemplate = infoLeft.text;
        infoErrorTemplate = infoError.text;
        defaultColor = infoRight.color;
    }

    // Update is called once per frame
    void Update()
    {
        /*Right Controller*/
        if (controllerRight.transform.position != lastPosRight) {
            textR = controllerRight.transform.position.x.ToString("0.00");
            colorR = defaultColor;
            lastPosRight = controllerRight.transform.position;
        } else {
            textR = "OFF";
            colorR = errorColor;
        }

        /*Left Controller*/
        if (controllerLeft.transform.position != lastPosLeft) {
            textL = controllerLeft.transform.position.x.ToString("0.00");
            colorL = defaultColor;
            lastPosLeft = controllerLeft.transform.position;
        } else {
            textL = "OFF";
            colorL = errorColor;
        }

        //Detect if the controller are swapped
        if (controllerRight.transform.position.x < controllerLeft.transform.position.x
            || controllerLeft.transform.position.x > controllerRight.transform.position.x)
        {
            colorL = errorColor;
            colorR = errorColor;
            colorError = errorColor;
            textError = "Controller Swapped !";
        } else {
            colorError = Color.clear;
        }

        infoRight.text = string.Format(infoRightTemplate, textR);
        infoLeft.text = string.Format(infoLeftTemplate, textL);
        infoError.text = string.Format(infoErrorTemplate, textError);
        infoRight.color = colorR;
        infoLeft.color = colorL;
        infoError.color = colorError;
    }

}

