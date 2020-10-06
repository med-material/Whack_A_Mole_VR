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

    [SerializeField]
    private float wallSpaceMargin = 1f;

    private Vector3 wallSpaceTopLeft = new Vector3(0f,0f,0f);
    private Vector3 wallSpaceTopRight = new Vector3(0f,0f,0f);
    private Vector3 wallSpaceBottomRight = new Vector3(0f,0f,0f);
    private Vector3 wallSpaceBottomLeft = new Vector3(0f,0f,0f);

    private Vector3 wallSpaceCoord;

    // Start is called before the first frame update
    void Start()
    {
        CalculateMotorSpace();
        UpdateMotorSpaceVisualizer();
    }

    // Update is called once per frame
    void CalculateMotorSpace()
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
        Gizmos.DrawLine(motorSpaceTopLeft, motorSpaceTopRight);
        Gizmos.DrawLine(motorSpaceTopRight, motorSpaceBottomRight);
        Gizmos.DrawLine(motorSpaceBottomRight, motorSpaceBottomLeft);
        Gizmos.DrawLine(motorSpaceBottomLeft, motorSpaceTopLeft);

        // Draw rectangle to indicate wallspace
        Gizmos.DrawLine(wallSpaceTopLeft, wallSpaceTopRight);
        Gizmos.DrawLine(wallSpaceTopRight, wallSpaceBottomRight);
        Gizmos.DrawLine(wallSpaceBottomRight, wallSpaceBottomLeft);
        Gizmos.DrawLine(wallSpaceBottomLeft, wallSpaceTopLeft);

        // Draw cube to visualize the latest calculated wall space coordinate.
        Gizmos.DrawCube(wallSpaceCoord, new Vector3(0.05f,0.05f,0.05f)); 
    }


}