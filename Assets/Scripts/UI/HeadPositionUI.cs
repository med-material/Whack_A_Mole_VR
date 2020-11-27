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

    private LoggerNotifier loggerNotifier;

    // Start is called before the first frame update
    void Start()
    {
        headPosTemplate = headPosText.text;
        headRotTemplate = headRotText.text;
        loggerNotifier = new LoggerNotifier();
    }

    // Update is called once per frame
    void Update()
    {
        headPosText.text = string.Format(headPosTemplate, headObject.transform.position.x.ToString("0.00"), headObject.transform.position.y.ToString("0.00"), headObject.transform.position.z.ToString("0.00"));
        headRotText.text = string.Format(headRotTemplate, headObject.transform.eulerAngles.x.ToString("0"), headObject.transform.eulerAngles.y.ToString("0"), headObject.transform.eulerAngles.z.ToString("0"));
    }

    public void LogCurrentPosition() {
        loggerNotifier.NotifyLogger("Player Position Set", EventLogger.EventType.ModifierEvent);
    }
}
