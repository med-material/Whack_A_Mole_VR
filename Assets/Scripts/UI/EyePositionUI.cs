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
    private Vector3 localGazeDirection;
    private Vector3 worldGazeDirection;
    private float gazeConfidence = -1f;
    public float sphereCastRadius = 0.05f;

    // Start is called before the first frame update
    void Start()
    {
        eyeInfoTemplate = eyeInfoText.text;
        eyePositionTemplate = eyePositionText.text;
        gazeController.OnReceive3dGaze += ReceiveGaze;
    }

    // Update is called once per frame
    void Update()
    {
        if (isGazing) {
            worldGazeOrigin = gazeOrigin.position;
            worldGazeDirection = gazeOrigin.TransformDirection(localGazeDirection);

            if (Physics.SphereCast(worldGazeOrigin, sphereCastRadius, worldGazeDirection, out RaycastHit hit, Mathf.Infinity))
            {
                eyePositionText.text = string.Format(eyePositionTemplate, hit.point.x.ToString("0.00"), hit.point.y.ToString("0.00"), hit.point.z.ToString("0.00"));
                eyeInfoText.text = string.Format(eyeInfoTemplate, gazeConfidence.ToString("0.00"), hit.transform.gameObject.name);
            }
        } else {
                eyePositionText.text = string.Format(eyePositionTemplate, "-", "-", "-");
                eyeInfoText.text = string.Format(eyeInfoTemplate, "-", "-");
        }
    }

    public void ReceiveGaze(GazeData gazeData) {
        if (gazeData.MappingContext != GazeData.GazeMappingContext.Binocular)
        {
            isGazing = false;
            return;
        }
        isGazing = true;
        
        localGazeDirection = gazeData.GazeDirection;
        gazeConfidence = gazeData.Confidence;
    }
}
