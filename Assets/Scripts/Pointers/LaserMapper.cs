using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[System.Serializable]
public class MotorSpaceInfo {
    public float width;
    public float height;
    public Vector3 pos;
    public float multiplier = 1f;
}

public class LaserMapper : MonoBehaviour
{

    [SerializeField]
    private MotorSpaceInfo defaultMotorSpace;

    [SerializeField]
    private MotorSpaceInfo dualtaskMotorSpace;

    private MotorSpaceInfo previousMotorSpace;

    [SerializeField]
    private GameObject controllerRight;

    [SerializeField]
    private GameObject motorSpaceCalib;

    [SerializeField]
    private BubbleDisplay[] bubbleDisplay;

    [SerializeField]
    private GameObject motorSpaceVisualizer;

    [SerializeField]
    private Vector3 motorSpaceOffset = new Vector3(0f,0f,0f);

    [SerializeField]
    private float motorSpaceWidth = 1f;

    [SerializeField]
    private float motorSpaceHeight = 1f;


    [SerializeField]
    private Slider motorSpaceSlider;
    private float multiplier = 1f;

    private Vector3 motorSpaceTopLeft = new Vector3(0f,0f,0f);
    private Vector3 motorSpaceTopRight = new Vector3(0f,0f,0f);
    private Vector3 motorSpaceBottomRight = new Vector3(0f,0f,0f);
    private Vector3 motorSpaceBottomLeft = new Vector3(0f,0f,0f);

    [SerializeField]
    private float wallSpaceMargin = 1f;

    private Vector3 wallSpaceTopLeft = new Vector3(0f,0f,0f);
    private Vector3 wallSpaceTopRight = new Vector3(0f,0f,0f);
    private Vector3 wallSpaceBottomRight = new Vector3(0f,0f,0f);
    private Vector3 wallSpaceBottomLeft = new Vector3(0f,0f,0f);

    private Vector3 wallSpaceCoord;

    private bool motorCalibration = false;
    private float distanceFromLastPoint = -1f;
    private float minDistancePoint = 0.050f;
    private Vector3 lastPos = Vector3.zero;
    private Vector3 newPos = Vector3.zero;
    private float minX = -1f;
    private float maxX = -1f;
    private float minY = -1f;
    private float maxY = -1f;
    private float minZ = -1f;
    private float maxZ = -1f;
    private Vector3 newCenter;
    private List<GameObject> calibPointList = new List<GameObject>();

    [SerializeField]
    private LoggingManager loggingManager;

    [System.Serializable]
    public class OnMotorSpaceChanged : UnityEvent<MotorSpaceInfo> { }
    public OnMotorSpaceChanged onMotorSpaceChanged;

    // Start is called before the first frame update
    void Start()
    {
        CalculateMotorSpace();
        UpdateMotorSpaceVisualizer();
    }

    void Update()
    {
        if (motorCalibration) {
            Vector3 newPos = controllerRight.transform.position;
            if (lastPos != Vector3.zero) distanceFromLastPoint = Vector3.Distance(lastPos, newPos);
            if  (distanceFromLastPoint > minDistancePoint) {
                CreateCalibSphere(lastPos);
                lastPos = newPos;
            }
            if (lastPos == Vector3.zero) {
                lastPos = newPos;
                CreateCalibSphere(lastPos);
            }

            if (minX == -1) minX = controllerRight.transform.position.x;
            if (maxX == -1) maxX = controllerRight.transform.position.x;
            if (minY == -1) minY = controllerRight.transform.position.y;
            if (maxY == -1) maxY = controllerRight.transform.position.y;
            if (minZ == -1) minZ = controllerRight.transform.position.z;
            if (maxZ == -1) maxZ = controllerRight.transform.position.z;

            if (minX > controllerRight.transform.position.x) minX = controllerRight.transform.position.x;
            if (maxX < controllerRight.transform.position.x) maxX = controllerRight.transform.position.x;
            if (minY > controllerRight.transform.position.y) minY = controllerRight.transform.position.y;
            if (maxY < controllerRight.transform.position.y) maxY = controllerRight.transform.position.y;
            if (minZ > controllerRight.transform.position.z) minZ = controllerRight.transform.position.z;
            if (maxZ < controllerRight.transform.position.z) maxZ = controllerRight.transform.position.z;
            newCenter = new Vector3( minX + ((maxX - minX) * 0.5f) , minY + ((maxY - minY) * 0.5f), minZ + ((maxZ - minZ) * 0.5f));
            motorSpaceWidth = (maxX - minX) / 2;
            motorSpaceHeight = (maxY - minY) / 2;
        }
    }

    private void CreateCalibSphere(Vector3 pos) {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(motorSpaceCalib.transform);
        sphere.transform.position = pos;
        sphere.transform.localScale = new Vector3(0.025f, 0.025f, 0.025f);
        calibPointList.Add(sphere);
    }

    public void ToggleMotorCalibration(bool value) {
        if (value == motorCalibration) return;
        motorCalibration = value;
        motorSpaceCalib.SetActive(value);

        if (!motorCalibration) {
            transform.position = newCenter;
            LogMotorSpaceChange("MotorSpace Calibration End");
            CalculateMotorSpace();
            UpdateMotorSpaceVisualizer();
            ResetCalibrationValues();
        } else {
            LogMotorSpaceChange("MotorSpace Calibration Start");
        }
    }

    private void ResetCalibrationValues() {
        minX = -1f;
        maxX = -1f;
        minY = -1f;
        maxY = -1f;
        minZ = -1f;
        maxZ = -1f;
        distanceFromLastPoint = -1f;
        newCenter = Vector3.zero; 
        foreach(var obj in calibPointList) {
            GameObject.Destroy(obj);
        }

    }

    void LogMotorSpaceChange(string eventLabel) {
        loggingManager.Log("Event", new Dictionary<string, object>()
        {
            {"Event", eventLabel},
            {"EventType", "MotorSpaceEvent"},
            {"MotorSpaceWidth", motorSpaceWidth},
            {"MotorSpaceHeight", motorSpaceHeight},
            {"MotorSpaceMultiplier", multiplier},
            {"MotorSpaceCenterPositionX", transform.position.x},
            {"MotorSpaceCenterPositionY", transform.position.y},
            {"MotorSpaceCenterPositionZ", transform.position.z},
        });
    }

    // Update is called once per frame
    void CalculateMotorSpace()
    {
        var motorSpaceOrigin = transform.position + motorSpaceOffset;
        motorSpaceTopLeft = new Vector3(motorSpaceOrigin.x - (motorSpaceWidth * multiplier), motorSpaceOrigin.y + (motorSpaceHeight * multiplier), motorSpaceOrigin.z);
        motorSpaceTopRight = new Vector3(motorSpaceOrigin.x + (motorSpaceWidth * multiplier), motorSpaceOrigin.y + (motorSpaceHeight * multiplier), motorSpaceOrigin.z);
        motorSpaceBottomRight = new Vector3(motorSpaceOrigin.x + (motorSpaceWidth * multiplier), motorSpaceOrigin.y - (motorSpaceHeight * multiplier), motorSpaceOrigin.z);
        motorSpaceBottomLeft = new Vector3(motorSpaceOrigin.x - (motorSpaceWidth * multiplier), motorSpaceOrigin.y - (motorSpaceHeight * multiplier), motorSpaceOrigin.z);
        foreach (var bub in bubbleDisplay) {
            bub.UpdateOwnPosition(transform.position);
        }
        onMotorSpaceChanged.Invoke(new MotorSpaceInfo { width = motorSpaceWidth, height = motorSpaceHeight, pos = transform.position} );
        LogMotorSpaceChange("MotorSpace Size Change");
    }

    void UpdateMotorSpaceVisualizer() {
        motorSpaceVisualizer.transform.position = transform.position + motorSpaceOffset;
        var visRect = motorSpaceVisualizer.GetComponent<RectTransform>();
        visRect.sizeDelta = new Vector2(motorSpaceWidth * 2 * multiplier, motorSpaceHeight * 2 * multiplier);

    }

    public bool CoordinateWithinMotorSpace(Vector3 coordinate) {
        return  coordinate.x < motorSpaceTopRight.x &&
                coordinate.x > motorSpaceTopLeft.x && 
                coordinate.y < motorSpaceTopLeft.y && 
                coordinate.y > motorSpaceBottomLeft.y;
    }

    private void CalculateWallSpace(WallInfo w) {
        // Use the wall's own reported boundaries and add some margin.
        wallSpaceTopLeft = new Vector3(w.lowestX - wallSpaceMargin, w.highestY + wallSpaceMargin, w.lowestZ);
        wallSpaceTopRight = new Vector3(w.highestX + wallSpaceMargin, w.highestY + wallSpaceMargin, w.lowestZ);
        wallSpaceBottomRight = new Vector3(w.highestX + wallSpaceMargin, w.lowestY - wallSpaceMargin, w.lowestZ);
        wallSpaceBottomLeft = new Vector3(w.lowestX - wallSpaceMargin, w.lowestY - wallSpaceMargin, w.lowestZ);
    }

    // Whenever the wall udpates we want to recalculate the wallspace.
    public void OnWallUpdated(WallInfo wall) {
        CalculateWallSpace(wall);
    }

    public void OnSliderSizeValueChanged() {
        var sliderValue = (float) motorSpaceSlider.value;
        var highVal = (float) motorSpaceSlider.maxValue;
        var lowVal = (float) motorSpaceSlider.minValue;
        multiplier = (sliderValue - lowVal) / highVal;
        CalculateMotorSpace();
        UpdateMotorSpaceVisualizer();
    }

    public void SetMotorSpaceWidth(float newWidth) {
        motorSpaceWidth = newWidth;
        CalculateMotorSpace();
        UpdateMotorSpaceVisualizer();
    }

    public void SetMotorSpaceHeight(float newHeight) {
        motorSpaceHeight = newHeight;
        CalculateMotorSpace();
        UpdateMotorSpaceVisualizer();
    }

    public Vector3 ConvertMotorSpaceToWallSpace(Vector3 coord) {
        // We convert our motorspace and our coordinate to be within a range where 0 is lowest.
        // Then we perform the normalization with division.
        // (coordinate within range) / (total range of  motorspace)
        float normalizedX = (coord.x - motorSpaceTopLeft.x) / (motorSpaceTopRight.x - motorSpaceTopLeft.x);
        // We now multiply our normalized value with the total range of the wall space.
        // Finally, as the wallSpace does not start from 0, we need to add back the negative starting point.
        float wallX = ((wallSpaceTopRight.x - wallSpaceTopLeft.x) * normalizedX) + wallSpaceTopLeft.x;

        // Repeat for Y coordinate.
        float normalizedY = (coord.y - motorSpaceBottomRight.y) / (motorSpaceTopRight.y - motorSpaceBottomRight.y);
        float wallY = ((wallSpaceTopRight.y - wallSpaceBottomRight.y) * normalizedY) + wallSpaceBottomRight.y;

        // The motor-space is two-dimensional so we will just use the Z coordinate directly.
        float wallZ = wallSpaceTopRight.z;
        wallSpaceCoord = new Vector3(wallX, wallY, wallZ);
        return wallSpaceCoord;
    }

    void OnDrawGizmos() {
        // Draw rectangle to indicate motorspace.
        // Gizmos.DrawLine(motorSpaceTopLeft, motorSpaceTopRight);
        // Gizmos.DrawLine(motorSpaceTopRight, motorSpaceBottomRight);
        // Gizmos.DrawLine(motorSpaceBottomRight, motorSpaceBottomLeft);
        // Gizmos.DrawLine(motorSpaceBottomLeft, motorSpaceTopLeft);

        // // Draw rectangle to indicate wallspace
        // Gizmos.DrawLine(wallSpaceTopLeft, wallSpaceTopRight);
        // Gizmos.DrawLine(wallSpaceTopRight, wallSpaceBottomRight);
        // Gizmos.DrawLine(wallSpaceBottomRight, wallSpaceBottomLeft);
        // Gizmos.DrawLine(wallSpaceBottomLeft, wallSpaceTopLeft);

        // // Draw cube to visualize the latest calculated wall space coordinate.
        // Gizmos.DrawCube(wallSpaceCoord, new Vector3(0.05f,0.05f,0.05f)); 
    }

    public void SetPosition(bool isRightHand) {
        if (isRightHand) {
            this.transform.position = new Vector3(-this.transform.position.x, this.transform.position.y, this.transform.position.z);
            LogMotorSpaceChange("MotorSpace Set Right");
        } else {
            this.transform.position = new Vector3(-this.transform.position.x, this.transform.position.y, this.transform.position.z);
            LogMotorSpaceChange("MotorSpace Set Left");
        }
    }

    public void SetDefaultMotorSpace() {
        motorSpaceWidth = defaultMotorSpace.width;
        motorSpaceHeight = defaultMotorSpace.height;
        transform.position = defaultMotorSpace.pos;
        multiplier = defaultMotorSpace.multiplier;
        CalculateMotorSpace();
        UpdateMotorSpaceVisualizer();
    }

}