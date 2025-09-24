using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class MotorSpaceInfo
{
    public float width;
    public float height;
    public Vector3 pos;
    public float multiplier = 1f;
    public MotorCalcMode mode = MotorCalcMode.center;

    public object Clone()
    {
        return this.MemberwiseClone();
    }
}

public enum MotorCalcMode
{
    center,
    bottom
}

public enum Side
{
    Left,
    Top,
    Right,
    Bottom,
    None,
}

public enum MotorRestriction
{
    none,
    restrict
}

public class LaserMapper : MonoBehaviour
{

    [SerializeField]
    private MotorSpaceInfo defaultMotorSpace;

    [SerializeField]
    private MotorSpaceInfo dualtaskMotorSpace;

    private MotorSpaceInfo previousMotorSpace;

    [SerializeField]
    private GameObject[] activeControllers;

    [SerializeField]
    private GameObject motorSpaceCalib;

    [SerializeField]
    private BubbleDisplay[] bubbleDisplay;

    [SerializeField]
    private GameObject motorSpaceVisualizer;

    [SerializeField]
    private Vector3 motorSpaceOffset = new Vector3(0f, 0f, 0f);

    [SerializeField]
    private float motorSpaceWidth = 1f;

    [SerializeField]
    private float motorSpaceHeight = 1f;

    // padding is used when evaluating whether coordinates are within the motorspace.
    [SerializeField]
    private float padding = 0.04f;

    [SerializeField]
    private bool showMotorspace = false;

    [SerializeField]
    private MotorRestriction motorRestriction = MotorRestriction.none;
    private float upperRestriction = 1f; // 1f = right, 0.5f = middle, 0.0f = left
    private float lowerRestriction = 0.5f;

    private float multiplier = 1f;

    private Vector3 motorSpaceTopLeft = new Vector3(0f, 0f, 0f);
    private Vector3 motorSpaceTopRight = new Vector3(0f, 0f, 0f);
    private Vector3 motorSpaceBottomRight = new Vector3(0f, 0f, 0f);
    private Vector3 motorSpaceBottomLeft = new Vector3(0f, 0f, 0f);

    [SerializeField]
    private float wallSpaceMargin = 1f;

    [SerializeField]
    private WallManager wallManager;

    private Vector3 wallSpaceTopLeft = Vector3.zero;
    private Vector3 wallSpaceTopRight = Vector3.zero;
    private Vector3 wallSpaceBottomRight = Vector3.zero;
    private Vector3 wallSpaceBottomLeft = Vector3.zero;

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
    private float gainX = -1f;
    private float gainY = -1f;
    private List<GameObject> calibPointList = new List<GameObject>();

    private Queue<IEnumerator> coroutineQueue = new Queue<IEnumerator>();

    [SerializeField]
    private LoggingManager loggingManager;

    [System.Serializable]
    public class OnMotorSpaceChanged : UnityEvent<MotorSpaceInfo> { }
    public OnMotorSpaceChanged onMotorSpaceChanged;

    // Start is called before the first frame update
    void Start()
    {
        ShowMotorspace(showMotorspace);
    }
    void OnEnable()
    {
        StartCoroutine(CoroutineCoordinator());
        coroutineQueue = new Queue<IEnumerator>();
        if (defaultMotorSpace.pos != Vector3.zero)
        {
            SetDefaultMotorSpace();
        }

        wallManager.stateUpdateEvent.AddListener(OnWallUpdated);
        OnWallUpdated(wallManager.CreateWallInfo());
        //CalculateMotorSpace();
        //CalculateGain();
        //UpdateMotorSpaceVisualizer();
        //onMotorSpaceChanged.Invoke(new MotorSpaceInfo { width = motorSpaceWidth, height = motorSpaceHeight, pos = transform.position} );
    }

    void Update()
    {
        if (motorCalibration)
        {
            GameObject controller = null;
            foreach (GameObject con in activeControllers)
            {
                if (con.activeSelf)
                {
                    controller = con;
                }
            }
            if (controller == null)
            {
                return;
            }
            Vector3 newPos = controller.transform.position;
            if (lastPos != Vector3.zero) distanceFromLastPoint = Vector3.Distance(lastPos, newPos);
            if (distanceFromLastPoint > minDistancePoint)
            {
                CreateCalibSphere(lastPos);
                lastPos = newPos;
            }
            if (lastPos == Vector3.zero)
            {
                lastPos = newPos;
                CreateCalibSphere(lastPos);
            }

            if (minX == -1) minX = controller.transform.position.x;
            if (maxX == -1) maxX = controller.transform.position.x;
            if (minY == -1) minY = controller.transform.position.y;
            if (maxY == -1) maxY = controller.transform.position.y;
            if (minZ == -1) minZ = controller.transform.position.z;
            if (maxZ == -1) maxZ = controller.transform.position.z;

            if (minX > controller.transform.position.x) minX = controller.transform.position.x;
            if (maxX < controller.transform.position.x) maxX = controller.transform.position.x;
            if (minY > controller.transform.position.y) minY = controller.transform.position.y;
            if (maxY < controller.transform.position.y) maxY = controller.transform.position.y;
            if (minZ > controller.transform.position.z) minZ = controller.transform.position.z;
            if (maxZ < controller.transform.position.z) maxZ = controller.transform.position.z;
            newCenter = new Vector3(minX + ((maxX - minX) * 0.5f), minY + ((maxY - minY) * 0.5f), minZ + ((maxZ - minZ) * 0.5f));
            motorSpaceWidth = (maxX - minX) / 2;
            motorSpaceHeight = (maxY - minY) / 2;
        }
    }

    public void SetMotorRestriction(MotorRestriction restriction, float restrictionLower, float restrictionUpper)
    {
        upperRestriction = restrictionUpper;
        lowerRestriction = restrictionLower;
        motorRestriction = restriction;
    }

    private void CreateCalibSphere(Vector3 pos)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(motorSpaceCalib.transform);
        sphere.transform.position = pos;
        sphere.transform.localScale = new Vector3(0.025f, 0.025f, 0.025f);
        calibPointList.Add(sphere);
    }

    public void ToggleMotorCalibration(bool value)
    {
        if (value == motorCalibration) return;
        motorCalibration = value;
        motorSpaceCalib.SetActive(value);

        if (!motorCalibration)
        {
            transform.position = newCenter;
            LogMotorSpaceChange("MotorSpace Calibration End");
            CalculateMotorSpace();
            CalculateGain();
            UpdateMotorSpaceVisualizer();
            ResetCalibrationValues();
        }
        else
        {
            LogMotorSpaceChange("MotorSpace Calibration Start");
        }
    }

    private void ResetCalibrationValues()
    {
        minX = -1f;
        maxX = -1f;
        minY = -1f;
        maxY = -1f;
        minZ = -1f;
        maxZ = -1f;
        distanceFromLastPoint = -1f;
        newCenter = Vector3.zero;
        foreach (GameObject obj in calibPointList)
        {
            GameObject.Destroy(obj);
        }

    }

    void LogMotorSpaceChange(string eventLabel)
    {
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
            {"MotorSpaceGainX", gainX},
            {"MotorSpaceGainY", gainY},
            {"MotorSpaceName", gameObject.name},
        });
    }

    // Update is called once per frame
    void CalculateMotorSpace(MotorCalcMode mode = MotorCalcMode.center, bool log = true)
    {
        Vector3 motorSpaceOrigin = transform.position + motorSpaceOffset;
        motorSpaceTopLeft = new Vector3(motorSpaceOrigin.x - (motorSpaceWidth * multiplier), motorSpaceOrigin.y + (motorSpaceHeight * multiplier), motorSpaceOrigin.z);
        motorSpaceTopRight = new Vector3(motorSpaceOrigin.x + (motorSpaceWidth * multiplier), motorSpaceOrigin.y + (motorSpaceHeight * multiplier), motorSpaceOrigin.z);
        motorSpaceBottomRight = new Vector3(motorSpaceOrigin.x + (motorSpaceWidth * multiplier), motorSpaceOrigin.y - (motorSpaceHeight * multiplier), motorSpaceOrigin.z);
        motorSpaceBottomLeft = new Vector3(motorSpaceOrigin.x - (motorSpaceWidth * multiplier), motorSpaceOrigin.y - (motorSpaceHeight * multiplier), motorSpaceOrigin.z);

        if (mode == MotorCalcMode.bottom)
        {
            motorSpaceTopLeft = new Vector3(motorSpaceOrigin.x - (motorSpaceWidth * multiplier), motorSpaceOrigin.y + (motorSpaceHeight * 2 * multiplier), motorSpaceOrigin.z);
            motorSpaceTopRight = new Vector3(motorSpaceOrigin.x + (motorSpaceWidth * multiplier), motorSpaceOrigin.y + (motorSpaceHeight * 2 * multiplier), motorSpaceOrigin.z);
            motorSpaceBottomRight = new Vector3(motorSpaceOrigin.x + (motorSpaceWidth * multiplier), motorSpaceOrigin.y, motorSpaceOrigin.z);
            motorSpaceBottomLeft = new Vector3(motorSpaceOrigin.x - (motorSpaceWidth * multiplier), motorSpaceOrigin.y, motorSpaceOrigin.z);
        }

        foreach (BubbleDisplay bub in bubbleDisplay)
        {
            bub.UpdateOwnPosition(transform.position);
        }
        onMotorSpaceChanged.Invoke(new MotorSpaceInfo { width = motorSpaceWidth, height = motorSpaceHeight, pos = transform.position });
        if (log)
        {
            LogMotorSpaceChange("MotorSpace Size Update");
        }
    }

    void UpdateMotorSpaceVisualizer(MotorCalcMode mode = MotorCalcMode.center)
    {
        if (mode == MotorCalcMode.center)
        {
            motorSpaceVisualizer.transform.position = transform.position + motorSpaceOffset;
        }
        else if (mode == MotorCalcMode.bottom)
        {
            Vector3 newPos = transform.position + motorSpaceOffset;
            motorSpaceVisualizer.transform.position = new Vector3(newPos.x, newPos.y + (motorSpaceHeight), newPos.z);
        }
        RectTransform visRect = motorSpaceVisualizer.GetComponent<RectTransform>();
        visRect.sizeDelta = new Vector2(motorSpaceWidth * 2 * multiplier, motorSpaceHeight * 2 * multiplier);
    }

    public void ShowMotorspace(bool show)
    {
        Image visImage = motorSpaceVisualizer.GetComponentInChildren<Image>();
        visImage.enabled = show;
    }

    public bool CoordinateWithinMotorSpace(Vector3 coordinate)
    {
        float paddingX = (motorSpaceTopRight.x - motorSpaceTopLeft.x) * padding;
        float paddingY = (motorSpaceTopLeft.y - motorSpaceBottomLeft.y) * padding;
        return coordinate.x < motorSpaceTopRight.x - paddingX &&
                coordinate.x > motorSpaceTopLeft.x + paddingX &&
                coordinate.y < motorSpaceTopLeft.y - paddingY &&
                coordinate.y > motorSpaceBottomLeft.y + paddingY;
    }

    public Vector3 ClipToMotorSpace(Vector3 coordinate)
    {
        float paddingX = (motorSpaceTopRight.x - motorSpaceTopLeft.x) * padding;
        float paddingY = (motorSpaceTopLeft.y - motorSpaceBottomLeft.y) * padding;
        float newCoordX = coordinate.x < motorSpaceTopRight.x - paddingX ? coordinate.x : motorSpaceTopRight.x - paddingX;
        newCoordX = coordinate.x > motorSpaceTopLeft.x + paddingX ? newCoordX : motorSpaceTopLeft.x + paddingX;
        float newCoordY = coordinate.y < motorSpaceTopLeft.y - paddingY ? coordinate.y : motorSpaceTopLeft.y - paddingY;
        newCoordY = coordinate.y > motorSpaceBottomLeft.y + paddingY ? newCoordY : motorSpaceBottomLeft.y + paddingY;
        return new Vector3(newCoordX, newCoordY, coordinate.z);
    }

    public Vector3 RubberClipToMotorSpace(Vector3 coordinate)
    {
        float paddingX = (motorSpaceTopRight.x - motorSpaceTopLeft.x) * padding;
        float paddingY = (motorSpaceTopLeft.y - motorSpaceBottomLeft.y) * padding;
        float CoordXDiff = Mathf.Abs((motorSpaceTopRight.x - motorSpaceTopLeft.x) * ((coordinate.x) - (motorSpaceTopRight.x - paddingX))) + 1f;
        float newCoordX = coordinate.x < motorSpaceTopRight.x - paddingX ? coordinate.x : motorSpaceTopRight.x - paddingX + Mathf.Log(CoordXDiff, 5) / (padding * 100);
        CoordXDiff = Mathf.Abs((motorSpaceTopRight.x - motorSpaceTopLeft.x) * ((motorSpaceTopLeft.x + paddingX) - (coordinate.x))) + 1f;

        newCoordX = coordinate.x > motorSpaceTopLeft.x + paddingX ? newCoordX : motorSpaceTopLeft.x + paddingX - Mathf.Log(CoordXDiff, 5) / (padding * 100);
        float CoordYDiff = Mathf.Abs((motorSpaceTopLeft.y - motorSpaceBottomLeft.y) * ((coordinate.y) - (motorSpaceTopLeft.y - paddingY))) + 1f;
        float newCoordY = coordinate.y < motorSpaceTopLeft.y - paddingY ? coordinate.y : motorSpaceTopLeft.y - paddingY + Mathf.Log(CoordYDiff, 5) / (padding * 100);

        CoordYDiff = Mathf.Abs((motorSpaceTopLeft.y - motorSpaceBottomLeft.y) * ((coordinate.y) - (motorSpaceBottomLeft.y + paddingY))) + 1f;
        newCoordY = coordinate.y > motorSpaceBottomLeft.y + paddingY ? newCoordY : motorSpaceBottomLeft.y + paddingY - Mathf.Log(CoordYDiff, 5) / (padding * 100);
        return new Vector3(newCoordX, newCoordY, coordinate.z);
    }

    public Side NearestSide(Vector3 coordinate)
    {
        float paddingX = (motorSpaceTopRight.x - motorSpaceTopLeft.x) * padding;
        float paddingY = (motorSpaceTopLeft.y - motorSpaceBottomLeft.y) * padding;
        float Right = Mathf.Abs(coordinate.x - (motorSpaceTopRight.x - paddingX));
        float Left = Mathf.Abs(coordinate.x - (motorSpaceTopLeft.x + paddingX));
        float Top = Mathf.Abs(coordinate.y - (motorSpaceTopLeft.y - paddingY));
        float Bottom = Mathf.Abs(coordinate.y - (motorSpaceBottomLeft.y + paddingY));

        Side side = Side.None;
        side = Right < Left && Right < Top && Right < Bottom ? Side.Right : side;
        side = Left < Right && Left < Top && Left < Bottom ? Side.Left : side;
        side = Top < Right && Top < Left && Top < Bottom ? Side.Top : side;
        side = Bottom < Right && Bottom < Left && Bottom < Top ? Side.Bottom : side;
        return side;
    }

    private void CalculateWallSpace(WallInfo w)
    {
        // Use the wall's own reported boundaries and add some margin.
        wallSpaceTopLeft = new Vector3(w.lowestX - wallSpaceMargin, w.highestY + wallSpaceMargin, w.lowestZ);
        wallSpaceTopRight = new Vector3(w.highestX + wallSpaceMargin, w.highestY + wallSpaceMargin, w.lowestZ);
        wallSpaceBottomRight = new Vector3(w.highestX + wallSpaceMargin, w.lowestY - wallSpaceMargin, w.lowestZ);
        wallSpaceBottomLeft = new Vector3(w.lowestX - wallSpaceMargin, w.lowestY - wallSpaceMargin, w.lowestZ);
    }

    private void CalculateGain()
    {
        if (wallSpaceTopLeft == Vector3.zero)
        {
            gainX = -1f;
            gainY = -1f;
        }
        else if (motorSpaceTopRight == Vector3.zero)
        {
            gainX = -1f;
            gainY = -1f;
        }
        else
        {
            gainX = (wallSpaceTopRight.x - wallSpaceTopLeft.x) / (motorSpaceTopRight.x - motorSpaceTopLeft.x);
            gainY = (wallSpaceTopRight.y - wallSpaceBottomRight.y) / (motorSpaceTopRight.y - motorSpaceBottomRight.y);
        }
    }

    // Whenever the wall udpates we want to recalculate the wallspace.
    public void OnWallUpdated(WallInfo wall)
    {
        CalculateWallSpace(wall);
        CalculateGain();
        LogMotorSpaceChange("Wall Size Update");
    }

    public void SetMultiplier(float m)
    {
        multiplier = m;
        CalculateMotorSpace();
        UpdateMotorSpaceVisualizer();
    }

    public void SetMotorSpaceWidth(float newWidth, bool log = true)
    {
        motorSpaceWidth = newWidth;
        CalculateMotorSpace(log: log);
        CalculateGain();
        UpdateMotorSpaceVisualizer();
    }

    public void SetMotorSpaceHeight(float newHeight, bool log = true)
    {
        motorSpaceHeight = newHeight;
        CalculateMotorSpace(log: log);
        CalculateGain();
        UpdateMotorSpaceVisualizer();
    }

    public Vector3 ConvertMotorSpaceToWallSpace(Vector3 coord)
    {
        // We convert our motorspace and our coordinate to be within a range where 0 is lowest.
        // Then we perform the normalization with division.
        // (coordinate within range) / (total range of  motorspace)
        float coordX = coord.x;

        if (motorRestriction == MotorRestriction.restrict)
        {
            float upperRestrictionNormalized = ((wallSpaceTopRight.x - wallSpaceTopLeft.x) * upperRestriction);
            float lowerRestrictionNormalized = ((wallSpaceTopRight.x - wallSpaceTopLeft.x) * lowerRestriction);
            float newLeft = wallSpaceTopLeft.x + lowerRestrictionNormalized;
            float newRight = wallSpaceTopLeft.x + upperRestrictionNormalized;

            if ((coord.x - transform.position.x) < newLeft)
            {
                coordX = (newLeft + transform.position.x);
            }
            if ((coord.x - transform.position.x) > newRight)
            {
                coordX = (newRight + transform.position.x);
            }
        }

        float normalizedX = (coordX - motorSpaceTopLeft.x) / (motorSpaceTopRight.x - motorSpaceTopLeft.x);
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

    public void SetMotorSpace(MotorSpaceInfo motorspace)
    {
        coroutineQueue.Enqueue(ScaleMotorSpace(motorspace, 0.4f));

        multiplier = motorspace.multiplier;
        UpdateMotorSpaceVisualizer(motorspace.mode);
        CalculateMotorSpace(motorspace.mode);
        CalculateGain();
    }

    public void SetDefaultMotorSpace(MotorSpaceInfo motorspace = null)
    {
        if (motorspace != null)
        {
            defaultMotorSpace = motorspace;
        }
        SetMotorSpace(defaultMotorSpace);
    }

    public MotorSpaceInfo GetMotorSpace()
    {
        MotorSpaceInfo info = new MotorSpaceInfo();
        info.width = motorSpaceWidth;
        info.height = motorSpaceHeight;
        info.pos = transform.position;
        info.multiplier = multiplier;
        return info;
    }

    private IEnumerator ScaleMotorSpace(MotorSpaceInfo motorspace, float duration)
    {
        float timer = 0;
        float rate = 1 / duration;

        Vector3 motorSpacedPosition = transform.position;
        Vector3 destinationPosition = Vector3.zero;
        if (motorspace.mode == MotorCalcMode.center)
        {
            destinationPosition = motorspace.pos;
        }
        else if (motorspace.mode == MotorCalcMode.bottom)
        {
            destinationPosition = new Vector3(motorspace.pos.x, motorspace.pos.y + motorspace.height, motorspace.pos.z);
        }

        while (timer < 1)
        {
            ShowMotorspace(true);

            timer += Time.deltaTime * rate;

            motorSpaceHeight = Mathf.Lerp(motorSpaceHeight, motorspace.height, timer);
            motorSpaceWidth = Mathf.Lerp(motorSpaceWidth, motorspace.width, timer);
            transform.position = Vector3.Lerp(motorSpacedPosition, destinationPosition, timer);

            SetMotorSpaceHeight(motorSpaceHeight, false);
            SetMotorSpaceWidth(motorSpaceWidth, false);

            yield return null;
        }
        ShowMotorspace(false);
    }

    private IEnumerator CoroutineCoordinator()
    {
        while (true)
        {
            while (coroutineQueue.Count > 0)
                yield return StartCoroutine(coroutineQueue.Dequeue());
            yield return null;
        }
    }
}
