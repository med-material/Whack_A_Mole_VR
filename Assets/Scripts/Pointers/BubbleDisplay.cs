using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Assets.Scripts.HUD;
using System;
using Valve.VR;

public class EnterMotorSpaceInfo
{
    public Side side; // side from which it entered/exited
    public bool enter = true; // enter (true), exit (false)
    public Vector3 motorLastPos; // motorspace last position
    public Vector3 wallLastPos; // wall space last position
}

public enum MotorAction
{
    Enter,
    Inside,
    Exit,
    Outside,
    None,
}

public enum ArrowType
{
    None,
    DynamicCenter,
    DynamicCenterReversed,
    StaticPointing
}

public class BubbleDisplay : MonoBehaviour
{
    [SerializeField]
    private LoggingManager loggingManager;

    [SerializeField]
    public SoundManager soundManager;
    // The parent we will follow in terms of object position.
    [SerializeField]
    private GameObject parent;

    [SerializeField]
    private GameObject bubbleRender;

    [SerializeField]
    private GameObject bubbleSphere;

    [SerializeField]
    private GameObject bubbleOutline;

    [SerializeField]
    private LineRenderer laserRender;

    private ControllerModifierManager controllerModifierManager;

    [SerializeField]
    private GameObject OutOfBoundContainer;

    [SerializeField]
    private GameObject OutOfBoundPrefab;

    [SerializeField]
    private int numberOfObjects;

    [SerializeField]
    private bool parentX = true;

    [SerializeField]
    private bool parentY = true;

    [SerializeField]
    private bool parentZ = true;

    [SerializeField]
    private float offsetX = 0f;

    [SerializeField]
    private float offsetY = 0f;

    [SerializeField]
    private float offsetZ = 0f;

    [SerializeField]
    private bool showBubble = false;

    [SerializeField]
    private LaserMapper laserMapper;

    [SerializeField]
    private Image motorSpaceRender;

    [SerializeField]
    private Color motorActiveColor;

    [SerializeField]
    private Color motorDisabledColor;

    [SerializeReference]
    private HUDManager hudPanel;

    [SerializeReference]
    private OutOfBoundIndicator staticArrowIndicator;

    [SerializeReference]
    private OutOfBoundIndicator dynamicCenterPointingIndicator;

    [SerializeReference]
    private OutOfBoundIndicator dynamicCenterReversedPointingIndicator;

    private OutOfBoundIndicator outOfBoundIndicatorManager;  // The current active indicator
    public ArrowType CurrentArrowType { get; private set; }

    [SerializeReference]
    public PointerTrailHandler pointerTrailHandler;

    public Side LastLaserMapperNearestSide { get; private set; }
    public GameObject CurrentController { get; private set; }


    private float newPosX;
    private float newPosY;
    private float newPosZ;

    private float prevPosX;
    private float prevPosY;
    private float prevPosZ;

    private Vector3 ownPosition;

    [System.Serializable]
    public class EnterMotorSpaceEvent : UnityEvent<EnterMotorSpaceInfo> { }
    public EnterMotorSpaceEvent enterMotorStateEvent;

    [System.Serializable]
    public class PointerPositionChangedEvent : UnityEvent<Vector3> { }

    public PointerPositionChangedEvent onPointerPositionChanged;


    private MotorAction action = MotorAction.None;

    // Start is called before the first frame update
    void Awake()
    {
        ownPosition = transform.position;
    }

    void Start()
    {
        bubbleRender.SetActive(true);
        laserRender.enabled = showBubble;
        bubbleOutline.SetActive(showBubble);
        bubbleSphere.SetActive(showBubble);
        controllerModifierManager = parent.GetComponent<ControllerModifierManager>();
        controllerModifierManager.SetControllerVisibility(true);
        motorSpaceRender.color = motorDisabledColor;

        //outOfBoundIndicatorManager = staticArrowIndicator;
        //CurrentArrowType = ArrowType.StaticPointing;
        outOfBoundIndicatorManager = dynamicCenterPointingIndicator;
        CurrentArrowType = ArrowType.DynamicCenter;
        //outOfBoundIndicatorManager = dynamicCenterReversedPointingIndicator;
        //CurrentArrowType = ArrowType.DynamicCenterReversed;
        CurrentController = laserMapper.GetCurrentController();
    }

    // Update is called once per frame
    /// <summary>
    /// This function is called every frame to update the object's position and handle its actions based on whether it is within or outside the MotorSpace.
    /// Actions Enter and Exit are only called once, while Inside and Outside are called every frame. So with this, we can track the object's position and actions
    /// and we know exactly when it enters and exits the MotorSpace and when it is inside or outside.
    /// </summary>
    void Update()
    {
        // Update our world position to be equivalent to the parent, for the axis chosen.
        newPosX = parentX ? parent.transform.position.x : ownPosition.x;
        newPosY = parentY ? parent.transform.position.y : ownPosition.y;
        newPosZ = parentZ ? parent.transform.position.z : ownPosition.z;

        // This code takes in the position of the laser pointer and sets the position of the object to that position.
        Vector3 newPos = new Vector3(newPosX, newPosY, newPosZ);
        onPointerPositionChanged?.Invoke(newPos);
        Vector3 clipPos = laserMapper.RubberClipToMotorSpace(newPos);
        this.transform.position = new Vector3(clipPos.x + offsetX, clipPos.y + offsetY, clipPos.z + offsetZ);

        LastLaserMapperNearestSide = laserMapper.NearestSide(newPos);

        prevPosX = newPosX;
        prevPosY = newPosY;
        prevPosZ = newPosZ;


        if (laserMapper.CoordinateWithinMotorSpace(newPos))  // Check if the coordinate is within the range of the motors
        {
            // If the object was previously outside or had no action, execute the following block.
            if (action == MotorAction.Outside || action == MotorAction.None)
            {
                action = MotorAction.Enter;  // Set the action status to 'Enter'.


                // Activating the renderers and setting color for visual feedback in the scene.
                bubbleRender.SetActive(true);
                laserRender.enabled = showBubble;
                bubbleOutline.SetActive(showBubble);
                bubbleSphere.SetActive(showBubble);
                controllerModifierManager.SetControllerVisibility(true);
                motorSpaceRender.color = motorActiveColor;

                // Hide the out-of-bound indicator.
                outOfBoundIndicatorManager?.HideIndicator();
                hudPanel.Reset();

                // Invoke the event for entering the MotorSpace with all relevant information.
                enterMotorStateEvent.Invoke(new EnterMotorSpaceInfo
                {
                    side = LastLaserMapperNearestSide,
                    enter = true,
                    motorLastPos = newPos,
                    wallLastPos = laserMapper.ConvertMotorSpaceToWallSpace(newPos),
                });

                // Log the event for entering the MotorSpace.
                loggingManager.Log("Event", new Dictionary<string, object>()
                {
                    {"Event", "Pointer Inside MotorSpace"},
                    {"EventType", "MotorSpaceEvent"},
                    {"Side", LastLaserMapperNearestSide.ToString()},
                    {"ControllerName", CurrentController.name},
                    {"ControllerOffset", CurrentController.GetComponent<SteamVR_Behaviour_Pose>().inputSource},
                });

                // If a sound manager exists, play the 'laserInMotorSpace' sound.
                if (soundManager != null)
                {
                    soundManager.PlaySound(gameObject, SoundManager.Sound.laserInMotorSpace);
                }

                // Now, as the object has entered the MotorSpace, set the action status to 'Inside'.
                action = MotorAction.Inside;
            }
        }
        else // If the coordinate is outside the range of the motors
        {
            if (action == MotorAction.Inside || action == MotorAction.None)
            {
                // If the object was previously inside the MotorSpace or had no action, execute the following block.

                action = MotorAction.Exit;  // Set the action status to 'Exit'.

                // Activating the renderers and setting color for visual feedback in the scene.
                bubbleRender.SetActive(true);
                laserRender.enabled = showBubble;
                bubbleOutline.SetActive(showBubble);
                bubbleSphere.SetActive(showBubble);
                controllerModifierManager.SetControllerVisibility(true);
                motorSpaceRender.color = motorDisabledColor;

                // Show the out-of-bound indicator.
                outOfBoundIndicatorManager?.ShowIndicator(newPos, laserMapper.GetWallMeshCenter(), LastLaserMapperNearestSide);
                hudPanel.ActivateGradient(LastLaserMapperNearestSide, FadeAction.In);

                // Invoke the event for exiting the MotorSpace with all relevant information.
                enterMotorStateEvent.Invoke(new EnterMotorSpaceInfo
                {
                    side = LastLaserMapperNearestSide,
                    enter = false,
                    motorLastPos = newPos,
                    wallLastPos = laserMapper.ConvertMotorSpaceToWallSpace(newPos),
                });

                // Log the event for exiting the MotorSpace.
                loggingManager.Log("Event", new Dictionary<string, object>()
                    {
                        {"Event", "Pointer Outside MotorSpace"},
                        {"EventType", "MotorSpaceEvent"},
                        {"Side", LastLaserMapperNearestSide.ToString()},
                        {"ControllerName", CurrentController.name},
                        {"ControllerOffset", CurrentController.GetComponent<SteamVR_Behaviour_Pose>().inputSource},
                    });

                // If a sound manager exists, play the 'laserOutMotorSpace' sound.
                if (soundManager != null)
                {
                    soundManager.PlaySound(gameObject, SoundManager.Sound.laserOutMotorSpace);
                }

                // Now, as the object has exited the MotorSpace, set the action status to 'Outside'.
                action = MotorAction.Outside;
            }
        }
    }

    public void Show(bool show)
    {
        showBubble = show;
    }

    public void UpdateOwnPosition(Vector3 newPosition)
    {
        ownPosition = newPosition;
    }

    public void InstantiateInCircle(GameObject container, Vector3 location, int howMany, float radius, float zPosition)
    {
        int i = 0;
        float angleSection = (Mathf.PI * 2f / howMany);
        foreach (Transform prefab in container.transform)
        {
            float angle = i * angleSection + (3 * Mathf.PI / 4);
            Vector3 newPos = location + new Vector3(Mathf.Cos(angle) * 1.5f, Mathf.Sin(angle), 0) * radius;
            newPos.z = zPosition;
            prefab.transform.position = newPos;
            i++;
        }
    }


    public void ChangeIndicator(ArrowType arrowType)
    {
        // Hide current indicator
        if (outOfBoundIndicatorManager != null)
        {
            outOfBoundIndicatorManager?.HideIndicator();
        }

        outOfBoundIndicatorManager = arrowType switch
        {
            ArrowType.StaticPointing => staticArrowIndicator,
            ArrowType.DynamicCenter => dynamicCenterPointingIndicator,
            ArrowType.DynamicCenterReversed => dynamicCenterReversedPointingIndicator,
            ArrowType.None => null,
            _ => staticArrowIndicator,
        };
        CurrentArrowType = arrowType;

        // If the user is outside of the MotorSpace, display the new indicator
        if (action == MotorAction.Outside)
        {
            Vector3 newPos = new Vector3(newPosX, newPosY, newPosZ);
            Side side = laserMapper.NearestSide(newPos);
            outOfBoundIndicatorManager?.ShowIndicator(newPos, laserMapper.transform.position, side);
        }
    }



    public void ChangeIndicatorToStatic()
    {
        ChangeIndicator(ArrowType.StaticPointing);
        Debug.Log("Changed Out Of Bounds indicator to static");
    }

    public void ChangeIndicatorToDynamic()
    {
        ChangeIndicator(ArrowType.DynamicCenter);
        Debug.Log("Changed Out Of Bounds indicator to dynamic");
    }


    internal void ChangeIndicatorToDynamicReversed()
    {
        ChangeIndicator(ArrowType.DynamicCenterReversed);
        Debug.Log("Changed Out Of Bounds indicator to dynamic reversed");
    }

    internal void DisableMotorSpaceOutOfBoundsIndicator()
    {
        ChangeIndicator(ArrowType.None);
        Debug.Log("Changed Out Of Bounds indicator to dynamic reversed");
    }

    internal void SetPerformanceOperationFeedback(bool v)
    {
        pointerTrailHandler.SetConfigVisibility(v);
    }

    
}