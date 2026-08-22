using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
#if !UNITY_ANDROID
using Valve.VR;
#endif

public class MoveData
{
    public Vector3 controllerPos = Vector3.zero;
    public Vector3 cursorPos = Vector3.zero;
    public ControllerName name;
}

public class ShootData
{
    public Vector3 position = Vector3.zero;
    public RaycastHit hit;
    public ControllerName name;
    public float dwell;
}

public enum ControllerName
{
    Left,
    Right
}

/*
Abstract class of the VR pointer used to pop moles. Like the Mole class, calls specific empty
functions on events to be overriden in its derived classes.
*/

public abstract class Pointer : MonoBehaviour
{
    protected enum States { Idle, CoolingDown }
    protected enum AimAssistStates { None, Snap, Magnetize }

    //[SerializeField]
    //private Valve.VR.SteamVR_Input_Sources controller;
    //public Valve.VR.SteamVR_Input_Sources Controller { get { return controller; } }

    [SerializeField]
    protected GameObject laserOrigin;

    [SerializeField]
    protected LaserMapper laserMapper;

    // Currently serialized. May be controlled by the UI in the future.

    [SerializeField]
    protected AimAssistStates aimAssistState; // Not implemented yet.

    [SerializeField]
    protected bool directionSmoothed = false; // Not implemented yet.

    [SerializeField]
    protected Vector3 laserOffset;

    [SerializeField]
    protected Color startLaserColor;

    [SerializeField]
    protected Color EndLaserColor;

    [SerializeField]
    protected float laserWidth = .1f;

    [SerializeField]
    protected Material laserMaterial;

    [SerializeField]
    protected float maxLaserLength;

    [SerializeField]
    protected float cursorLength;

    [SerializeField]
    protected float shotCooldown;

    [SerializeReference]
    public PerformanceManager performanceManager;

    [SerializeField]
    public float dwellTime = 2f;

    [SerializeField]
    protected LaserCursor cursor;

    [SerializeField]
    public SoundManager soundManager;

    private States state = States.Idle;
    private Mole hoveredMole;

    protected LineRenderer laser;
    protected bool performanceFeedbackOperation = true;
    protected bool performanceFeedbackTask = true;
    protected bool performancefeedback = true;
    protected bool performanceText = false;
    protected bool active = false;
    protected LoggerNotifier loggerNotifier;
    protected float dwellStartTimer = 0f;
    protected int pointerShootOrder = -1;
    protected ControllerName controllerName;

    [System.Serializable]
    public class OnPointerShoot : UnityEvent { }
    public OnPointerShoot onPointerShoot;

    [System.Serializable]
    public class OnPointerMove : UnityEvent<MoveData> { }
    public OnPointerMove onPointerMove;

    protected ActionBasedController controller;
    private InputAction clickAction;
    [SerializeField]
    private float UpdateInterval = 1.0f;


    // On Awake, gets the cursor object if there is one.
    void Awake()
    {
        if (gameObject.name == "Controller (right)")
        {
            controllerName = ControllerName.Right;
        }
        else if (gameObject.name == "Controller (left)")
        {
            controllerName = ControllerName.Left;
        }
    }

    // On start, inits the logger notifier.
    void Start()
    {
        controller = gameObject.GetComponent<ActionBasedController>();
        clickAction = gameObject.GetComponent<ActionBasedController>().activateAction.action;
        //StartCoroutine(CheckUpdate());

        loggerNotifier = new LoggerNotifier(eventsHeadersDefaults: new Dictionary<string, string>(){
            {"HitPositionWorldX", "NULL"},
            {"HitPositionWorldY", "NULL"},
            {"HitPositionWorldZ", "NULL"}
        },
        // Controller smoothing state and aim assist's logs placed here temporarily. When the aim assist will be managed by the UI,
        // the UI would have to raise the event.
        persistentEventsHeadersDefaults: new Dictionary<string, string>(){
            {"ControllerSmoothed", "NULL"},
            {"ControllerHover", "NULL"},
            {"PointerShootOrder", "NULL"},
            {"ControllerName", "NULL"},
            {"ControllerAimAssistState", "NULL"},
            {"LastShotControllerRawPointingDirectionX", "NULL"},
            {"LastShotControllerRawPointingDirectionY", "NULL"},
            {"LastShotControllerRawPointingDirectionZ", "NULL"},
            {"LastShotControllerFilteredPointingDirectionX", "NULL"},
            {"LastShotControllerFilteredPointingDirectionY", "NULL"},
            {"LastShotControllerFilteredPointingDirectionZ", "NULL"}
        });

        loggerNotifier.InitPersistentEventParameters(new Dictionary<string, object>(){
            {"ControllerSmoothed", directionSmoothed},
            {"ControllerHover", "NULL"},
            {"PointerShootOrder", "NULL"},
            {"ControllerName", "NULL"},
            {"ControllerAimAssistState", System.Enum.GetName(typeof(Pointer.AimAssistStates), aimAssistState)}
        });

        laser = laserOrigin.GetComponent<LineRenderer>();
    }

    private Vector3 lastValue = Vector3.zero;

    private IEnumerator CheckUpdate()
    {
        Vector3 v = controller.transform.position;
        if (v != lastValue)
        {
            lastValue = v;
            PositionUpdated();
        }
        yield return new WaitForSeconds(UpdateInterval);
    }
    public ControllerName GetControllerName()
    {
        return controllerName;
    }


    // Used steamvr to vibrate controllers. Need openXR reimplementation
    public void Pulse(float duration, float frequency, float amplitude)
    {
        //if (!SteamVR.active) return;
        //// duration in seconds
        //// frequency in hz
        //// amplitude in 75

        //hapticAction.Execute(0, duration, frequency, amplitude, controller);
    }

    public void SetPointerEnable(bool active)
    {
        if (active)
        {
            Enable();
        }
        else
        {
            Disable();
        }
    }

    public void ResetShootOrder()
    {
        if (pointerShootOrder == -1) return; // If the pointerShootOrder is already -1, it means it has already been reset or Pointer was never instantiated.
        pointerShootOrder = -1;
        loggerNotifier.NotifyLogger(overrideEventParameters: new Dictionary<string, object>()
        {
            {"PointerShootOrder", "NULL"}
        });
    }


    public void SetOperationPerformanceFeedback(bool perf)
    {
        performanceFeedbackOperation = perf;
    }

    public void SetTaskPerformanceFeedback(bool perf)
    {
        performanceFeedbackTask = perf;
    }

    public void SetActionPerformanceFeedback(bool perf, bool withText)
    {
        performancefeedback = perf;
        performanceText = withText;
    }

    // Enables the pointer
    public virtual void Enable()
    {
        if (active) return;
        if (cursor) cursor.Enable();

        //if (laser) laser.enabled = true;
        state = States.Idle;
        active = true;
        pointerShootOrder = -1;
    }

    // Disables the pointer
    public virtual void Disable()
    {
        if (!active) return;
        if (cursor) cursor.Disable();

        //if (laser) laser.enabled = false;
        active = false;
    }

    protected virtual void Shoot(Mole mole)
    {
        state = States.CoolingDown;
        StartCoroutine(WaitForCooldown());

        onPointerShoot.Invoke();
        Mole.MolePopAnswer moleAnswer = mole.Pop(mole.transform.position);

        switch (moleAnswer)
        {
            case Mole.MolePopAnswer.Ok:
                PlayShoot(correctHit: true);
                soundManager.PlaySound(gameObject, SoundManager.Sound.greenMoleHit);
                Debug.Log($"Mole hit. Score: {mole.GetMoleScore()}");
                break;

            case Mole.MolePopAnswer.Fake:
                PlayShoot(correctHit: false);
                SoundManager.Sound sound = performancefeedback ? SoundManager.Sound.redMoleHit : SoundManager.Sound.greenMoleHit;
                soundManager.PlaySound(gameObject, sound);
                break;

            case Mole.MolePopAnswer.Disabled:
                RaiseMoleMissedEvent(mole.transform.position);
                soundManager.PlaySound(gameObject, SoundManager.Sound.neutralMoleHit);
                break;
        }
    }

    // Function called on VR update, since it can be faster/not synchronous to Update() function. Makes the Pointer slightly more reactive.
    public virtual void PositionUpdated()
    {
        if (!active) return;

        Vector2 pos = new Vector2(laserOrigin.transform.position.x, laserOrigin.transform.position.y);
        Vector3 mappedPosition = laserMapper.ConvertMotorSpaceToWallSpace(pos);
        Vector3 origin = laserOrigin.transform.position;
        Vector3 rayDirection = (mappedPosition - origin).normalized;

        onPointerMove.Invoke(new MoveData
        {
            controllerPos = pos,
            cursorPos = mappedPosition,
            name = controllerName
        });

        RaycastHit hit;
        if (Physics.Raycast(laserOrigin.transform.position + laserOffset, rayDirection, out hit, 100f, Physics.DefaultRaycastLayers))
        {
            //UpdateLaser(true, hitPosition: laserOrigin.transform.InverseTransformPoint(hit.point), rayDirection: laserOrigin.transform.InverseTransformDirection(rayDirection));
            Vector3 hitPosition = laserOrigin.transform.InverseTransformPoint(hit.point);
            laser.SetPosition(1, hitPosition);
            cursor.SetPosition(hitPosition);
            hoverMole(hit);
        }
        else
        {
            Vector3 rayPosition = laserOrigin.transform.InverseTransformDirection(rayDirection) * maxLaserLength;
            laser.SetPosition(1, rayPosition);
            cursor.SetPosition(rayPosition);
            //UpdateLaser(false, rayDirection: laserOrigin.transform.InverseTransformDirection(rayDirection) * maxLaserLength);
        }

        if (/*Valve.VR.SteamVR.active*/ true) //TODO find equivalent to SteamVR.active or remove
        {
            if (clickAction.WasPerformedThisFrame())
            {
                if (state == States.Idle)
                {
                    pointerShootOrder++;
                    loggerNotifier.NotifyLogger(overrideEventParameters: new Dictionary<string, object>(){
                        {"ControllerSmoothed", directionSmoothed},
                        {"ControllerAimAssistState", System.Enum.GetName(typeof(Pointer.AimAssistStates), aimAssistState)},
                        {"LastShotControllerRawPointingDirectionX", transform.forward.x},
                        {"LastShotControllerRawPointingDirectionY", transform.forward.y},
                        {"LastShotControllerRawPointingDirectionZ", transform.forward.z},
                        {"LastShotBubbleRawPointingDirectionX", laserOrigin.transform.forward.x},
                        {"LastShotBubbleRawPointingDirectionY", laserOrigin.transform.forward.y},
                        {"LastShotBubbleRawPointingDirectionZ", laserOrigin.transform.forward.z},
                        {"LastShotBubbleFilteredPointingDirectionX", rayDirection.x},
                        {"LastShotBubbleFilteredPointingDirectionY", rayDirection.y},
                        {"LastShotBubbleFilteredPointingDirectionZ", rayDirection.z},
                    });

                    loggerNotifier.NotifyLogger("Pointer Shoot", EventLogger.EventType.PointerEvent, new Dictionary<string, object>()
                    {
                        {"PointerShootOrder", pointerShootOrder}
                    });
                    Debug.Log("Pointer Shoot !!!!");
                    Shoot(hit);
                }
            }
        }
    }

    // Functions to call in the class implementation to add extra animation/effect behavior on shoot/cooldown.
    public virtual void ShowTaskFeedback(float duration, List<(int id, float val)> molePerf, float animationDelay) { }

    // Functions to call in the class implementation to add extra animation/effect behavior on shoot/cooldown.
    protected virtual void PlayShoot(bool correctHit) { }
    protected virtual void PlayCooldownEnd() { }

    // Checks if a Mole is hovered and tells it to play the hovered efect.
    protected virtual void hoverMole(RaycastHit hit)
    {
        Mole mole;
        if (hit.collider.gameObject.TryGetComponent<Mole>(out mole))
        {
            if (mole != hoveredMole)
            {
                if (hoveredMole)
                {
                    hoveredMole.OnHoverLeave();
                }
                hoveredMole = mole;
                dwellStartTimer = Time.time;
                hoveredMole.OnHoverEnter();
            }
        }
        else if (hoveredMole)
        {
            hoveredMole.OnHoverLeave();
            hoveredMole = null;
        }
    }

    // Shoots a raycast. If Mole is hit, calls its Pop() function. Depending on the hit result, plays the hit/missed shooting animation
    // and raises a "Mole Missed" event.
    protected virtual void Shoot(RaycastHit hit)
    {
        Mole mole;

        state = States.CoolingDown;
        StartCoroutine(WaitForCooldown());

        performanceManager.OnPointerShoot(new ShootData
        {
            hit = hit,
            dwell = this.dwellTime,
            name = controllerName
        });

        onPointerShoot.Invoke();
        if (hit.collider)
        {
            if (hit.collider.gameObject.TryGetComponent<Mole>(out mole))
            {
                Mole.MolePopAnswer moleAnswer = mole.Pop(hit.point);

                switch (moleAnswer)
                {
                    case Mole.MolePopAnswer.Ok:
                        PlayShoot(true);
                        soundManager.PlaySound(gameObject, SoundManager.Sound.greenMoleHit);
                        Debug.Log($"Mole hit. Score: {mole.GetMoleScore()}");
                        break;

                    case Mole.MolePopAnswer.Fake:
                        PlayShoot(false);
                        SoundManager.Sound sound = performancefeedback ? SoundManager.Sound.redMoleHit : SoundManager.Sound.greenMoleHit;
                        soundManager.PlaySound(gameObject, sound);
                        break;

                    case Mole.MolePopAnswer.Disabled:
                        RaiseMoleMissedEvent(hit.point);
                        soundManager.PlaySound(gameObject, SoundManager.Sound.neutralMoleHit);
                        break;
                }
                return;
            }
            RaiseMoleMissedEvent(hit.point);
            if (performancefeedback)
            {
                soundManager.PlaySound(gameObject, SoundManager.Sound.missedMole);
            }
        }
        else
        {
            soundManager.PlaySound(gameObject, SoundManager.Sound.outOfBoundClick);
        }
        PlayShoot(false);

    }

    // Function raising a "Mole Missed" event.
    private void RaiseMoleMissedEvent(Vector3 hitPosition)
    {
        loggerNotifier.NotifyLogger("Mole Missed", EventLogger.EventType.MoleEvent, new Dictionary<string, object>(){
            {"HitPositionWorldX", hitPosition.x},
            {"HitPositionWorldY", hitPosition.y},
            {"HitPositionWorldZ", hitPosition.z}
        });
    }

    // Waits the CoolDown duration.
    private IEnumerator WaitForCooldown()
    {
        yield return new WaitForSeconds(shotCooldown);
        state = States.Idle;
        PlayCooldownEnd();
    }
}
