using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserMapper : MonoBehaviour
{

    [SerializeField]
    private GameObject controller;

    [SerializeField]
    private GameObject motorSpaceVisualizer;

    [SerializeField]
    private Vector3 motorSpaceOffset = new Vector3(0f,0f,0f);

    [SerializeField]
    private float motorSpaceWidth = 1f;

    [SerializeField]
    private float motorSpaceHeight = 1f;

    private Vector3 motorSpaceTopLeft = new Vector3(0f,0f,0f);
    private Vector3 motorSpaceTopRight = new Vector3(0f,0f,0f);
    private Vector3 motorSpaceBottomRight = new Vector3(0f,0f,0f);
    private Vector3 motorSpaceBottomLeft = new Vector3(0f,0f,0f);

    // Start is called before the first frame update
    void Start()
    {
        UpdateMotorSpaceCoords();
        UpdateMotorSpaceVisualizer();
    }

    // Update is called once per frame
    void UpdateMotorSpaceCoords()
    {
        var motorSpaceOrigin = transform.position + motorSpaceOffset;
        motorSpaceTopLeft = new Vector3(motorSpaceOrigin.x - motorSpaceWidth, motorSpaceOrigin.y + motorSpaceHeight, motorSpaceOrigin.z);
        motorSpaceTopRight = new Vector3(motorSpaceOrigin.x + motorSpaceWidth, motorSpaceOrigin.y + motorSpaceHeight, motorSpaceOrigin.z);
        motorSpaceBottomRight = new Vector3(motorSpaceOrigin.x + motorSpaceWidth, motorSpaceOrigin.y - motorSpaceHeight, motorSpaceOrigin.z);
        motorSpaceBottomLeft = new Vector3(motorSpaceOrigin.x - motorSpaceWidth, motorSpaceOrigin.y - motorSpaceHeight, motorSpaceOrigin.z);
    }

    void UpdateMotorSpaceVisualizer() {
        motorSpaceVisualizer.transform.position = transform.position + motorSpaceOffset;
        var visRect = motorSpaceVisualizer.GetComponent<RectTransform>();
        visRect.sizeDelta = new Vector2(motorSpaceWidth * 2, motorSpaceHeight * 2);

    }

    public bool CoordinateWithinMotorSpace(Vector3 coordinate) {
        return  coordinate.x < motorSpaceTopRight.x &&
                coordinate.x > motorSpaceTopLeft.x && 
                coordinate.y < motorSpaceTopLeft.y && 
                coordinate.y > motorSpaceBottomLeft.y;
    }

    void OnDrawGizmos() {
        // Draw rectangle to indicate motorspace.
        Gizmos.DrawLine(motorSpaceTopLeft, motorSpaceTopRight);
        Gizmos.DrawLine(motorSpaceTopRight, motorSpaceBottomRight);
        Gizmos.DrawLine(motorSpaceBottomRight, motorSpaceBottomLeft);
        Gizmos.DrawLine(motorSpaceBottomLeft, motorSpaceTopLeft);
    }


}