using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PupilLabs;

public class EyePositionUI : MonoBehaviour
{

    [SerializeField]
    private Text eyePositionText;
    private string eyePositionTemplate;

    [SerializeField]
    private Text eyeInfoText;
    private string eyeInfoTemplate;

    [SerializeField]
    private GazeController gazeController;

    [SerializeField]
    private Transform gazeOrigin;

    private bool isGazing = false;
    private Vector3 worldGazeOrigin;
    private Vector3 localGazeDirection = Vector3.zero;
    private Vector3 worldGazeDirection;
    private float gazeConfidence = -1f;
    public float sphereCastRadius = 0.05f;
    private Vector3 gazeNormal0;
    private Vector3 gazeNormal1;
    private float updateTimer = 0f;
    private float updateTime = 0.1f;

    // Start is called before the first frame update
    void Awake()
    {
        eyeInfoTemplate = eyeInfoText.text;
        eyePositionTemplate = eyePositionText.text;
    }

    public void SubscribeGazeRecorder() {
        gazeController.OnReceive3dGaze += ReceiveGaze;
    }

    public void UnsubscribeGazeRecorder() {
        gazeController.OnReceive3dGaze -= ReceiveGaze;
    }

    // Update is called once per frame
    void Update()
    {
        if (updateTimer < updateTime) {
            updateTimer += Time.deltaTime;
            return;
        } else {
            updateTimer = 0f;
            if (localGazeDirection != Vector3.zero) {
                worldGazeOrigin = gazeOrigin.position;
                worldGazeDirection = gazeOrigin.TransformDirection(localGazeDirection);
                //if (Physics.SphereCast(worldGazeOrigin, sphereCastRadius, worldGazeDirection, out RaycastHit hit, Mathf.Infinity))
                //{
                    eyePositionText.text = string.Format(eyePositionTemplate, localGazeDirection.x.ToString("0.00"), localGazeDirection.y.ToString("0.00"), localGazeDirection.z.ToString("0.00"));
                    eyeInfoText.text = string.Format(eyeInfoTemplate, gazeConfidence.ToString("0.00"), gazeNormal0.x.ToString("0.00"), gazeNormal1.x.ToString("0.00"));
                //}
            } else {
                    eyePositionText.text = string.Format(eyePositionTemplate, "-", "-", "-");
                    eyeInfoText.text = string.Format(eyeInfoTemplate, "-", "-", "-");
            }
        }
    }

    public void ReceiveGaze(GazeData gazeData) {
        if (gazeData.MappingContext != GazeData.GazeMappingContext.Binocular)
        {
            return;
        }
        
        localGazeDirection = gazeData.GazeDirection;
        gazeConfidence = gazeData.Confidence;
        gazeNormal0 = gazeData.GazeNormal0;
        gazeNormal0 = gazeData.GazeNormal1;
    }
}
