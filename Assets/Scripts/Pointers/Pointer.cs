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
    private enum States {Idle, CoolingDown}

    [SerializeField]
    private SteamVR_Input_Sources controller;

    [SerializeField]
    protected Vector3 laserOrigin;

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
    protected float shotCooldown;

    protected LineRenderer laser;
    protected LaserCursor cursor;

    private States state = States.Idle;
    private Mole hoveredMole;
    private bool active = false;
    private LoggerNotifier loggerNotifier;


    // On Awake, gets the cursor object if there is one. Also connects the PositionUpdated function to the VR update event.
    void Awake()
    {
        cursor = gameObject.GetComponentInChildren<LaserCursor>();
        gameObject.GetComponent<SteamVR_Behaviour_Pose>().onTransformUpdated.AddListener(delegate{PositionUpdated();});
    }

    // On start, inits the logger notifier.
    void Start()
    {
        loggerNotifier = new LoggerNotifier(eventsHeadersDefaults: new Dictionary<string, string>(){
            {"HitPositionWorldX", "NULL"},
            {"HitPositionWorldY", "NULL"},
            {"HitPositionWorldZ", "NULL"}
        });
    }


    // Enables the pointer
    public void Enable()
    {
        if (active) return;

        if (cursor) cursor.Enable();

        if (!laser)
        {
            InitLaser();
        }
        else
        {
            laser.enabled = true;
        }
        state = States.Idle;
        active = true;
    }

    // Disables the pointer
    public void Disable()
    {
        if (!active) return;

        if (cursor) cursor.Disable();

        if (laser) laser.enabled = false;
        active = false;
    }

    // Function called on VR update, since it can be faster/not synchronous to Update() function. Makes the Pointer slightly more reactive.
    public void PositionUpdated()
    {
        if (!active) return;

        RaycastHit hit;
        if (Physics.Raycast(transform.position + laserOrigin, transform.forward, out hit, 100f))
        {
            UpdateLaser(true, hit.distance);
            hoverMole(hit);
        }
        else
        {
            UpdateLaser(false, maxLaserLength);
        }

        if(SteamVR.active)
        {
            if (SteamVR_Input._default.inActions.GrabPinch.GetStateDown(controller))
            {
                if (state == States.Idle)
                {
                    Shoot(hit);
                }
            }
        }
    }

    // Functions to call in the class implementation to add extra animation/effect behavior on shoot/cooldown.
    protected virtual void PlayShoot(bool correctHit) {}
    protected virtual void PlayCooldownEnd() {}

    // Checks if a Mole is hovered and tells it to play the hovered efect.
    private void hoverMole(RaycastHit hit)
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
    private void Shoot(RaycastHit hit)
    {
        Mole mole;

        state = States.CoolingDown;
        StartCoroutine(WaitForCooldown());

        if (hit.collider)
        {
            if (hit.collider.gameObject.TryGetComponent<Mole>(out mole))
            {
                Mole.MolePopAnswer moleAnswer = mole.Pop(hit.point);

                if (moleAnswer == Mole.MolePopAnswer.Disabled) RaiseMoleMissedEvent(hit.point);
                PlayShoot(moleAnswer == Mole.MolePopAnswer.Ok);
                return;
            }
            RaiseMoleMissedEvent(hit.point);
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

    // Updates the laser position. If the raycast hits something, places the cursor in consequence.
    private void UpdateLaser(bool hit, float distance)
    {
        laser.SetPosition(1, laserOrigin + Vector3.forward * distance);

        if (!cursor) return;

        if (cursor.IsEnabled() != hit)
        {
            if (hit) cursor.Enable();
            else cursor.Disable();
        }
        if (hit) cursor.SetPosition(laserOrigin + Vector3.forward * distance);
    }

    // Inits the laser.
    private void InitLaser()
    {
        laser = gameObject.AddComponent<LineRenderer>();
        laser.useWorldSpace = false;
        laser.material = laserMaterial;
        laser.SetPositions(new Vector3[2]{laserOrigin, laserOrigin + Vector3.forward * maxLaserLength});
        laser.startColor = startLaserColor;
        laser.endColor = EndLaserColor;
        laser.startWidth = laserWidth;
        laser.endWidth = laserWidth;
        laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        if (!cursor) return;

        cursor.SetColor(EndLaserColor);
        cursor.Disable();
    }

    // Waits the CoolDown duration.
    private IEnumerator WaitForCooldown()
    {
        yield return new WaitForSeconds(shotCooldown);
        state = States.Idle;
        PlayCooldownEnd();
    }
}
