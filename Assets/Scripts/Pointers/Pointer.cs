using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

/*
Abstract class of the VR pointer used to pop moles. Like the Mole class, calls specific empty
functions on events to be overriden in its derived classes.
*/

public abstract class Pointer : MonoBehaviour
{
    private enum States {Idle, Shooting}

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

    protected LineRenderer laser;
    protected LaserCursor cursor;
    private States state = States.Idle;
    private Mole hoveredMole;
    private bool active = false;


    // On Awake, gets the cursor object if there is one.
    void Awake()
    {
        cursor = gameObject.GetComponentInChildren<LaserCursor>();
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

    void Update()
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

    protected virtual void PlayShoot() 
    {
        state = States.Idle;
    }

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

    private void Shoot(RaycastHit hit)
    {
        Mole mole;
        state = States.Shooting;
        if (hit.collider)
        {
            if (hit.collider.gameObject.TryGetComponent<Mole>(out mole))
            {
                mole.Pop(hit.point);
            }
        }
        PlayShoot();
    }

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

        if (!cursor) return;

        cursor.SetColor(EndLaserColor);
        cursor.Disable();
    }

}
