using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using UnityEngine.Events;

/*
Abstract class of the VR pointer used to pop moles. Like the Mole class, calls specific empty
functions on events to be overriden in its derived classes.
*/

public abstract class Pointer : MonoBehaviour
{
    protected enum States {Idle, CoolingDown}
    protected enum AimAssistStates {None, Snap, Magnetize}

    [SerializeField]
    private SteamVR_Input_Sources controller;

    [SerializeField]
    protected GameObject laserOrigin;

    [SerializeField]
    protected LaserMapper laserMapper;

    // Currently serialized. May be controlled by the UI in the future.

    [SerializeField]
    protected AimAssistStates aimAssistState;

    [SerializeField]
    protected bool directionSmoothed = false;

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

    protected LineRenderer laser;
    
    [SerializeField]
    protected LaserCursor cursor;

    private States state = States.Idle;
    private Mole hoveredMole;
    protected bool active = false;
    protected LoggerNotifier loggerNotifier;

    [SerializeField]
    public SoundManager soundManager;

    // Laser smoothing. May be put in a new class for clean code. Left here for test for now.

    [SerializeField]
    private float smoothTime = 0.05f;

    [SerializeField]
    private float SnapMagnetizeRadius = 0.2f;

    private Vector3 previousDirection;
    private Vector3 currentLaserOffset = Vector3.zero;
    private Vector3 smoothingVelocity = Vector3.zero;
    private float lastTime = -1 ;

    protected int pointerShootOrder = -1;

    [System.Serializable]
    public class OnPointerShoot : UnityEvent { }
    public OnPointerShoot onPointerShoot;

    // On Awake, gets the cursor object if there is one. Also connects the PositionUpdated function to the VR update event.
    void Awake()
    {
        gameObject.GetComponent<SteamVR_Behaviour_Pose>().onTransformUpdated.AddListener(delegate{PositionUpdated();});
    }

    // On start, inits the logger notifier.
    void Start()
    {
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

        previousDirection = laserOrigin.transform.forward;
        laser = laserOrigin.GetComponent<LineRenderer>();
    }

    public void SetPointerEnable(bool active) {
        if (active) {
            Enable();
        } else {
            Disable();
        }
    }

    // Enables the pointer
    public void Enable()
    {
        if (active) return;

        if (cursor) cursor.Enable();

        //if (laser) laser.enabled = true;
        state = States.Idle;
        active = true;
        pointerShootOrder = -1;
    }

    // Disables the pointer
    public void Disable()
    {
        if (!active) return;

        if (cursor) cursor.Disable();

        //if (laser) laser.enabled = false;
        active = false;
    }

    // Function called on VR update, since it can be faster/not synchronous to Update() function. Makes the Pointer slightly more reactive.
    public virtual void PositionUpdated()
    {
        if (!active) return;

        Vector2 pos = new Vector2(laserOrigin.transform.position.x, laserOrigin.transform.position.y);
        Vector3 mappedPosition = laserMapper.ConvertMotorSpaceToWallSpace(pos);
        Vector3 origin = laserOrigin.transform.position;
        Vector3 rayDirection = (mappedPosition - origin).normalized;

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

        if(SteamVR.active)
        {
            if (SteamVR_Actions._default.GrabPinch.GetStateDown(controller))
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
                    Shoot(hit);
                }
            }
        }
    }

    // Functions to call in the class implementation to add extra animation/effect behavior on shoot/cooldown.
    protected virtual void PlayShoot(bool correctHit) {}
    protected virtual void PlayCooldownEnd() {}

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
                hoveredMole.OnHoverEnter();
            }
        }
        else
        {
            if (hoveredMole)
            {
                hoveredMole.OnHoverLeave();
                hoveredMole = null;
            }
        }
    }

    // Shoots a raycast. If Mole is hit, calls its Pop() function. Depending on the hit result, plays the hit/missed shooting animation
    // and raises a "Mole Missed" event.
    protected virtual void Shoot(RaycastHit hit)
    {
        Mole mole;

        state = States.CoolingDown;
        StartCoroutine(WaitForCooldown());

        onPointerShoot.Invoke();
        if (hit.collider)
        {
            if (hit.collider.gameObject.TryGetComponent<Mole>(out mole))
            {
                Mole.MolePopAnswer moleAnswer = mole.Pop(hit.point);

                if (moleAnswer == Mole.MolePopAnswer.Ok) 
                { 
                    PlayShoot(moleAnswer == Mole.MolePopAnswer.Ok);
                    soundManager.PlaySound(gameObject, SoundManager.Sound.greenMoleHit);
                } 
                else if (moleAnswer == Mole.MolePopAnswer.Fake)
                {
                    soundManager.PlaySound(gameObject, SoundManager.Sound.redMoleHit);
                } 
                else if (moleAnswer == Mole.MolePopAnswer.Disabled)
                {
                    RaiseMoleMissedEvent(hit.point);
                    soundManager.PlaySound(gameObject, SoundManager.Sound.neutralMoleHit);
                }
                return;
            }
            RaiseMoleMissedEvent(hit.point);
            soundManager.PlaySound(gameObject, SoundManager.Sound.missedMole);
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

    private Vector3 GetRayDirection()
    {
        Vector3 direction = Vector3.zero;
        switch(aimAssistState)
        {
            case AimAssistStates.Snap:
                direction = GetSnappedDirection();
                break;
            case AimAssistStates.Magnetize:
                direction = GetMagnetizedDirection();
                break;
            case AimAssistStates.None:
            default:
                direction = laserOrigin.transform.forward;
                break;
        }

        if(directionSmoothed) direction = GetSmoothedDirection(direction);

        return direction;
    }

    private Vector3 GetSmoothedDirection(Vector3 aimedDirection)
    {
        var tempPreviousDirection = previousDirection;
        float delta = 0 ;
        if( lastTime > 0 )
        {
            delta = Time.time - lastTime ;
        }
        lastTime = Time.time ;

        previousDirection = aimedDirection;
        currentLaserOffset += (previousDirection - tempPreviousDirection);
        currentLaserOffset = Vector3.SmoothDamp(currentLaserOffset, Vector3.zero, ref smoothingVelocity, smoothTime, 1000f, delta);
        
        return aimedDirection - currentLaserOffset;
    }

    private Vector3 GetSnappedDirection()
    {
        RaycastHit hit;
        if (Physics.Raycast(laserOrigin.transform.position + laserOffset, laserOrigin.transform.forward, out hit, 100f, Physics.DefaultRaycastLayers))
        {
            Collider[] collidersHit = Physics.OverlapSphere(hit.point, SnapMagnetizeRadius);

            List<Transform> molesHit = new List<Transform>();
            foreach(Collider collider in collidersHit)
            {
                if (collider.gameObject.GetComponent<Mole>() != null)
                {
                    molesHit.Add(collider.gameObject.transform);
                }
            }

            if (molesHit.Count == 0) return laserOrigin.transform.forward;

            float closestDistance = 1000f;
            Vector3 closestMolePosition = Vector3.zero;

            foreach(Transform moleTransform in molesHit)
            {
                float moleDistance = Vector3.Distance(hit.point, moleTransform.position);
                if (moleDistance < closestDistance)
                {
                    closestDistance = moleDistance;
                    closestMolePosition = moleTransform.position;
                }
            }
            return (closestMolePosition + new Vector3(0f, 0.005f, 0f) - laserOrigin.transform.position).normalized;
        }
        return laserOrigin.transform.forward;
    }

    private Vector3 GetMagnetizedDirection()
    {
        RaycastHit hit;
        if (Physics.Raycast(laserOrigin.transform.position + laserOffset, laserOrigin.transform.forward, out hit, 100f, Physics.DefaultRaycastLayers))
        {
            Collider[] collidersHit = Physics.OverlapSphere(hit.point, SnapMagnetizeRadius);

            List<Transform> molesHit = new List<Transform>();
            foreach(Collider collider in collidersHit)
            {
                if (collider.gameObject.GetComponent<Mole>() != null)
                {
                    molesHit.Add(collider.gameObject.transform);
                }
            }

            if (molesHit.Count == 0) return laserOrigin.transform.forward;

            float closestDistance = 1000f;
            Vector3 closestMolePosition = Vector3.zero;

            foreach(Transform moleTransform in molesHit)
            {
                float moleDistance = Vector3.Distance(hit.point, moleTransform.position);
                if (moleDistance < closestDistance)
                {
                    closestDistance = moleDistance;
                    closestMolePosition = moleTransform.position;
                }
            }
            return (((closestMolePosition + hit.point) / 2f) + new Vector3(0f, 0.005f, 0f) - laserOrigin.transform.position).normalized;
        }
        return laserOrigin.transform.forward;
    }

    // // Inits the laser.
    // private void InitLaser()
    // {
    //     laser = laserOrigin.AddComponent<LineRenderer>();
    //     laser.useWorldSpace = false;
    //     laser.material = laserMaterial;
    //     laser.SetPositions(new Vector3[2]{laserOffset, laserOffset + Vector3.forward * maxLaserLength});
    //     laser.startColor = startLaserColor;
    //     laser.endColor = EndLaserColor;
    //     laser.startWidth = laserWidth;
    //     laser.endWidth = laserWidth;
    //     laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

    //     if (!cursor) return;

    //     cursor.SetColor(EndLaserColor);
    //     cursor.Disable();

    //     // Smoothing init

    //     
    // }

    // Waits the CoolDown duration.
    private IEnumerator WaitForCooldown()
    {
        yield return new WaitForSeconds(shotCooldown);
        state = States.Idle;
        PlayCooldownEnd();
    }
}
