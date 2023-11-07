using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.HUD;

public enum ArrowType
{
    None,
    DynamicCenter,
    DynamicCenterReversed,
    StaticPointing
}

public class OutOfBoundManager : MonoBehaviour
{



    [SerializeReference]
    private OutOfBoundIndicator staticArrowIndicator;

    [SerializeReference]
    private OutOfBoundIndicator dynamicCenterPointingIndicator;

    [SerializeReference]
    private OutOfBoundIndicator dynamicCenterReversedPointingIndicator;

    private OutOfBoundIndicator outOfBound;  // The current active indicator
    public ArrowType CurrentArrowType { get; private set; }

    private bool active = false; // determined by game state

    // Start is called before the first frame update
    void Start()
    {
        //outOfBoundIndicatorManager = staticArrowIndicator;
        //CurrentArrowType = ArrowType.StaticPointing;
        outOfBound = dynamicCenterPointingIndicator;
        CurrentArrowType = ArrowType.DynamicCenter;
        //outOfBoundIndicatorManager = dynamicCenterReversedPointingIndicator;
        //CurrentArrowType = ArrowType.DynamicCenterReversed;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowIndicator(Vector3 position, Vector3 motorSpaceCenter, Side side) {
        if (!active) return;
        if (!enabled) return;
        outOfBound?.ShowIndicator(position, motorSpaceCenter, side);
    }
    
    public void HideIndicator() {
        outOfBound?.HideIndicator();
    }

   public void ChangeIndicator(ArrowType arrowType)
    {
        // Hide current indicator
        if (outOfBound != null)
        {
            outOfBound?.HideIndicator();
        }

        outOfBound = arrowType switch
        {
            ArrowType.StaticPointing => staticArrowIndicator,
            ArrowType.DynamicCenter => dynamicCenterPointingIndicator,
            ArrowType.DynamicCenterReversed => dynamicCenterReversedPointingIndicator,
            ArrowType.None => null,
            _ => staticArrowIndicator,
        };
        CurrentArrowType = arrowType;
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

    public void OnGameStateChanged(GameDirector.GameState newState) {
        switch(newState)
        {
            case GameDirector.GameState.Stopped:
                active = false;
                outOfBound?.HideIndicator();
                break;
            case GameDirector.GameState.Playing:
                active = true;
                break;
            case GameDirector.GameState.Paused:
                active = false;
                outOfBound?.HideIndicator();
                break;
        }
    }

}
