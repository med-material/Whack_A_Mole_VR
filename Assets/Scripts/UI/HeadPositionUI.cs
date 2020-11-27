using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeadPositionUI : MonoBehaviour
{

    [SerializeField]
    private Text headPosText;
    private string headPosTemplate;

    [SerializeField]
    private Text headRotText;
    private string headRotTemplate;

    [SerializeField]
    private GameObject headObject;

    [SerializeField]
    private GameObject controllerR;
    [SerializeField]
    private GameObject controllerL;

    [SerializeField]
    private LoggingManager loggingManager;

    // Start is called before the first frame update
    void Start()
    {
        headPosTemplate = headPosText.text;
        headRotTemplate = headRotText.text;
    }

    // Update is called once per frame
    void Update()
    {
        headPosText.text = string.Format(headPosTemplate, headObject.transform.position.x.ToString("0.00"), headObject.transform.position.y.ToString("0.00"), headObject.transform.position.z.ToString("0.00"));
        headRotText.text = string.Format(headRotTemplate, headObject.transform.eulerAngles.x.ToString("0"), headObject.transform.eulerAngles.y.ToString("0"), headObject.transform.eulerAngles.z.ToString("0"));
    }

    public void LogCurrentPosition() {
        loggingManager.Log("Event", new Dictionary<string, object>()
        {
            {"Event", "Player Position Set"},
            {"EventType", "CalibrationEvent"},
            {"HeadCameraPosWorldX", headObject.transform.position.x},
            {"HeadCameraPosWorldY", headObject.transform.position.y},
            {"HeadCameraPosWorldZ", headObject.transform.position.z},
            {"HeadCameraRotEulerX", headObject.transform.eulerAngles.x},
            {"HeadCameraRotEulerY", headObject.transform.eulerAngles.y},
            {"HeadCameraRotEulerZ", headObject.transform.eulerAngles.z},
            {"RightControllerPosWorldX", controllerR != null ? controllerR.transform.position.x : -1f},
            {"RightControllerPosWorldY", controllerR != null ? controllerR.transform.position.x : -1f},
            {"RightControllerPosWorldZ", controllerR != null ? controllerR.transform.position.x : -1f},
            {"RightControllerRotEulerX", controllerR != null ? controllerR.transform.eulerAngles.x : -1f},
            {"RightControllerRotEulerY", controllerR != null ? controllerR.transform.eulerAngles.y : -1f},
            {"RightControllerRotEulerZ", controllerR != null ? controllerR.transform.eulerAngles.z : -1f},
            {"LeftControllerPosWorldX", controllerL != null ? controllerL.transform.position.x : -1f},
            {"LeftControllerPosWorldY", controllerL != null ? controllerL.transform.position.x : -1f},
            {"LeftControllerPosWorldZ", controllerL != null ? controllerL.transform.position.x : -1f},
            {"LeftControllerRotEulerX", controllerL != null ? controllerL.transform.eulerAngles.x : -1f},
            {"LeftControllerRotEulerY", controllerL != null ? controllerL.transform.eulerAngles.y : -1f},
            {"LeftControllerRotEulerZ", controllerL != null ? controllerL.transform.eulerAngles.z : -1f},
        });
    }
}
