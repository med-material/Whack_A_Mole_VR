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
    private OutOfBoundIndicator staticArrowIndicatorL;

    [SerializeReference]
    private OutOfBoundIndicator staticArrowIndicatorR;

    [SerializeReference]
    private OutOfBoundIndicator dynamicCenterPointingIndicatorL;

    [SerializeReference]
    private OutOfBoundIndicator dynamicCenterPointingIndicatorR;

    [SerializeReference]
    private OutOfBoundIndicator dynamicCenterReversedPointingIndicatorL;

    [SerializeReference]
    private OutOfBoundIndicator dynamicCenterReversedPointingIndicatorR;

    private OutOfBoundIndicator outOfBoundR;  // The current active indicator
    private OutOfBoundIndicator outOfBoundL;
    [SerializeField]
    public ArrowType CurrentArrowType;

    private bool active = false; // determined by game state

    // Start is called before the first frame update
    void Start()
    {
        //outOfBoundIndicatorManager = staticArrowIndicator;
        //CurrentArrowType = ArrowType.StaticPointing;
        outOfBoundR = dynamicCenterPointingIndicatorR;
        outOfBoundL = dynamicCenterPointingIndicatorL;
        CurrentArrowType = ArrowType.DynamicCenter;
        //outOfBoundIndicatorManager = dynamicCenterReversedPointingIndicator;
        //CurrentArrowType = ArrowType.DynamicCenterReversed;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowIndicator(Vector3 position, Vector3 motorSpaceCenter, Side side, string controllerName) {
        if (!active) return;
        if (!enabled) return;
        
        GetOutOfBoundObject(controllerName)?.ShowIndicator(position, motorSpaceCenter, side);
    }

    private void SetOutOfBoundObject(ArrowType arrowType, string controllerName) {
        if (controllerName == "Controller (right)") {
            outOfBoundR = arrowType switch
            {
                ArrowType.StaticPointing => GetStaticArrow(controllerName),
                ArrowType.DynamicCenter => GetDynamicCenter(controllerName),
                ArrowType.DynamicCenterReversed => GetDynamicCenterReversed(controllerName),
                ArrowType.None => null,
                _ => GetStaticArrow(controllerName),
            };
        } else {
            outOfBoundL = arrowType switch
            {
                ArrowType.StaticPointing => GetStaticArrow(controllerName),
                ArrowType.DynamicCenter => GetDynamicCenter(controllerName),
                ArrowType.DynamicCenterReversed => GetDynamicCenterReversed(controllerName),
                ArrowType.None => null,
                _ => GetStaticArrow(controllerName),
            };
        }
    }

    private OutOfBoundIndicator GetOutOfBoundObject(string controllerName) {
        return controllerName == "Controller (right)" ? outOfBoundR : outOfBoundL;
    }

    private OutOfBoundIndicator GetStaticArrow(string controllerName) {
        return controllerName == "Controller (right)" ? staticArrowIndicatorR : staticArrowIndicatorL;
    }

    private OutOfBoundIndicator GetDynamicCenter(string controllerName) {
        return controllerName == "Controller (right)" ? dynamicCenterPointingIndicatorR : dynamicCenterPointingIndicatorL;
    }

    private OutOfBoundIndicator GetDynamicCenterReversed(string controllerName) {
        return controllerName == "Controller (right)" ? dynamicCenterReversedPointingIndicatorR : dynamicCenterReversedPointingIndicatorL;
    }

    public void HideAllIndicators() {
        outOfBoundR?.HideIndicator();
        outOfBoundL?.HideIndicator();
    }

    public void HideIndicator(string controllerName) {

        GetOutOfBoundObject(controllerName)?.HideIndicator();
    }

   public void ChangeIndicator(ArrowType arrowType, string controllerName)
    {
        var outOfBoundObject = GetOutOfBoundObject(controllerName);

        // Hide current indicator
        if (outOfBoundObject != null)
        {
            outOfBoundObject?.HideIndicator();
        }

        SetOutOfBoundObject(arrowType, controllerName);

        CurrentArrowType = arrowType;
    }

    public void ChangeIndicatorToStatic(string controllerName)
    {
        ChangeIndicator(ArrowType.StaticPointing, controllerName);
        Debug.Log("Changed Out Of Bounds indicator to static");
    }

    public void ChangeIndicatorToDynamic(string controllerName)
    {
        ChangeIndicator(ArrowType.DynamicCenter, controllerName);
        Debug.Log("Changed Out Of Bounds indicator to dynamic");
    }


    internal void ChangeIndicatorToDynamicReversed(string controllerName)
    {
        ChangeIndicator(ArrowType.DynamicCenterReversed,controllerName);
        Debug.Log("Changed Out Of Bounds indicator to dynamic reversed");
    }

    internal void DisableMotorSpaceOutOfBoundsIndicator(string controllerName)
    {
        ChangeIndicator(ArrowType.None, controllerName);
        Debug.Log("Changed Out Of Bounds indicator to dynamic reversed");
    }

    public void OnGameStateChanged(GameDirector.GameState newState) {
        switch(newState)
        {
            case GameDirector.GameState.Stopped:
                active = false;
                HideAllIndicators();
                break;
            case GameDirector.GameState.Playing:
                active = true;
                break;
            case GameDirector.GameState.Paused:
                active = false;
                HideAllIndicators();
                break;
        }
    }

}
