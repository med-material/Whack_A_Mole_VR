using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR;

/*
Abstract class of the VR pointer used to pop moles. Like the Mole class, calls specific empty
functions on events to be overriden in its derived classes.
*/

public abstract class Pointer : MonoBehaviour
{
    protected enum States { Idle, CoolingDown }
    protected enum AimAssistStates { None, Snap, Magnetize }

    [SerializeField]
    private SteamVR_Input_Sources controller;
    public SteamVR_Input_Sources Controller { get { return controller; } }

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

    [SerializeField]
    protected float dwellTime = 2f;

    [SerializeField]
    protected LaserCursor cursor;

    [SerializeField]
    public SoundManager soundManager;

    private States state = States.Idle;
    private Mole hoveredMole;

    protected LineRenderer laser;
    protected bool performancefeedback = true;
    protected bool active = false;
    protected LoggerNotifier loggerNotifier;
    protected float dwellStartTimer = 0f;
    protected int pointerShootOrder = -1;

    [System.Serializable]
    public class OnPointerShoot : UnityEvent { }
    public OnPointerShoot onPointerShoot;

    // On start, inits the logger notifier.
    void Start()
    {
        gameObject.GetComponent<SteamVR_Behaviour_Pose>().onTransformUpdated.AddListener(delegate { PositionUpdated(); });

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

    public void SetPerformanceFeedback(bool perf) => performancefeedback = perf;

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
                PlayShoot(true);
                soundManager.PlaySound(gameObject, SoundManager.Sound.greenMoleHit);
                break;

            case Mole.MolePopAnswer.Fake:
                PlayShoot(false);
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

        if (SteamVR.active)
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
                    Debug.Log("Pointer Shoot !!!!");
                    Shoot(hit);
                }
            }
        }
    }

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
