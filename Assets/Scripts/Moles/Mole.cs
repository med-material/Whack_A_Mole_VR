using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Mole abstract class. Contains the main behaviour of the mole and calls actions to be played on
different events (Enable, Disable, Pop...). These actions are to be defined in its derived
classes.
Facilitates the creation of moles with different behaviours on specific events 
(when popped -> change color ? play animation?) 
*/

public abstract class Mole : MonoBehaviour
{
    protected enum States {Disabled, Enabled, Popping, Enabling, Disabling}
    protected States state = States.Disabled;
    protected bool fake = false;
    private Coroutine timer;
    private float lifeTime;
    private int id = -1;
    private float activatedTimeLeft;
    private bool isPaused = false;
    private LoggerNotifier loggerNotifier;
    private Vector3 lastHitPoint = new Vector3();

    protected virtual void Start()
    {
        EnterState(States.Disabled);

        // Initialization of the LoggerNotifier. Here we will only raise Event, and we will use a function to pass and update 
        // certain parameters values every time we raise an event (UpdateLogNotifierGeneralValues). We don't set any starting values.
        loggerNotifier = new LoggerNotifier(UpdateLogNotifierGeneralValues, new Dictionary<string, string>(){
            {"MolePositionWorldX", "NULL"},
            {"MolePositionWorldY", "NULL"},
            {"MolePositionWorldZ", "NULL"},
            {"MolePositionLocalX", "NULL"},
            {"MolePositionLocalY", "NULL"},
            {"MolePositionLocalZ", "NULL"},
            {"MoleLifeTime", "NULL"},
            {"MoleActivatedDuration", "NULL"},
            {"MoleId", "NULL"},
            {"MoleIndexX", "NULL"},
            {"MoleIndexY", "NULL"},
            {"MoleSurfaceHitLocationX", "NULL"},
            {"MoleSurfaceHitLocationY", "NULL"}
        });

    }

    public void SetId(int newId)
    {
        id = newId;
    }

    public int GetId()
    {
        return id;
    }

    public bool IsActive()
    {
        return (state == States.Enabled || state == States.Enabling);
    }

    public void Enable(float enabledLifeTime, bool isFake = false)
    {
        fake = isFake;
        lifeTime = enabledLifeTime;
        ChangeState(States.Enabling);
    }

    public void Disable()
    {
        ChangeState(States.Disabling);
    }

    public void SetPause(bool pause)
    {
        isPaused = pause;
    }

    public void Reset()
    {
        StopAllCoroutines();
        state = States.Disabled;
        EnterState(States.Disabled);
    }

    public void Pop(Vector3 hitPoint)
    {
        if (isPaused) return;
        if (state != States.Enabled && state != States.Enabling)
        {
            return;
        }
        lastHitPoint = Quaternion.AngleAxis(-transform.rotation.y,Vector3.up) * (hitPoint - transform.position);
        ChangeState(States.Popping);
    }

    public void OnHoverEnter()
    {
        if (state != States.Enabled)
        {
            return;
        }
        PlayHoverEnter();
    }

    public void OnHoverLeave()
    {
        if (state != States.Enabled)
        {
            return;
        }
        PlayHoverLeave();
    }

    protected virtual void PlayEnable() {}
    protected virtual void PlayDisable() {}
    protected virtual void PlayHoverEnter() {}
    protected virtual void PlayHoverLeave() {}

    /*
    Transition states. Need to be called at the end of its override in the derived class to
    finish the transition.
    */
    protected virtual void PlayEnabling() 
    {
        ChangeState(States.Enabled);
    }
    protected virtual void PlayDisabling() 
    {
        ChangeState(States.Disabled);
    }
    protected virtual void PlayPop() 
    {
        ChangeState(States.Disabled);
    }

    private void ChangeState(States newState)
    {
        if (newState == state)
        {
            return;
        }
        LeaveState(state);
        state = newState;
        EnterState(state);
    }

    private void LeaveState(States state)
    {
        switch(state)
        {
            case States.Disabled:
                break;
            case States.Enabled:
                StopCoroutine(timer);
                break;
            case States.Popping:
                break;
            case States.Enabling:
                break;
            case States.Disabling:
                break;
        }
    }

    private void EnterState(States state)
    {
        switch(state)
        {
            case States.Disabled:
                PlayDisable();
                break;
            case States.Enabled:
                PlayEnable();
                break;
            case States.Popping:
                if (!fake) 
                {
                    loggerNotifier.NotifyLogger("Mole Hit", new Dictionary<string, object>()
                    {
                        {"MoleActivatedDuration", lifeTime - activatedTimeLeft},
                        {"MoleSurfaceHitLocationX", lastHitPoint.x},
                        {"MoleSurfaceHitLocationY", lastHitPoint.y}
                    });
                }
                else 
                {
                    loggerNotifier.NotifyLogger("Fake Mole Hit", new Dictionary<string, object>()
                    {
                        {"MoleActivatedDuration", lifeTime - activatedTimeLeft},
                        {"MoleSurfaceHitLocationX", lastHitPoint.x},
                        {"MoleSurfaceHitLocationY", lastHitPoint.y}
                    });
                }

                PlayPop();
                break;
            case States.Enabling:

                if (!fake) loggerNotifier.NotifyLogger("Mole Spawned");
                else loggerNotifier.NotifyLogger("Fake Mole Spawned");

                timer = StartCoroutine(StartTimer(lifeTime));
                PlayEnabling();
                break;
            case States.Disabling:
                loggerNotifier.NotifyLogger("Mole Missed");
                PlayDisabling();
                break;
        }
    }

    private IEnumerator StartTimer(float duration)
    {
        activatedTimeLeft = duration;
        while (activatedTimeLeft > 0)
        {
            if (!isPaused)
            {
                activatedTimeLeft -= Time.deltaTime;
            }
            yield return null;
        }
        OnTimeout();
    }

    private void OnTimeout()
    {
        if (state != States.Enabled)
        {
            return;
        }
        Disable();
    }

    // Function that will be called by the LoggerNotifier every time an event is raised, to automatically update
    // and pass certain parameters' values.
    private LogEventContainer UpdateLogNotifierGeneralValues()
    {
        return new LogEventContainer(new Dictionary<string, object>(){
            {"MolePositionWorldX", transform.position.x},
            {"MolePositionWorldY", transform.position.y},
            {"MolePositionWorldZ", transform.position.z},
            {"MolePositionLocalX", transform.localPosition.x},
            {"MolePositionLocalY", transform.localPosition.y},
            {"MolePositionLocalZ", transform.localPosition.z},
            {"MoleLifeTime", lifeTime},
            {"MoleId", id.ToString("0000")},
            {"MoleIndexX", (int)Mathf.Floor(id/100)},
            {"MoleIndexY", (id % 100)}   
        });
    }
}
