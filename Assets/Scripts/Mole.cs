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
    private bool active = false;
    private int id = -1;

    protected virtual void Start()
    {
        EnterState(States.Disabled);
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
        return active;
    }

    public void Enable(float enabledLifeTime, bool isFake = false)
    {
        active = true;
        fake = isFake;
        lifeTime = enabledLifeTime;
        ChangeState(States.Enabling);
    }

    public void Disable()
    {
        active = false;
        ChangeState(States.Disabling);
    }

    public void Reset()
    {
        active = false;
        StopAllCoroutines();
        state = States.Disabled;
        EnterState(States.Disabled);
    }

    public void Pop()
    {
        if (state != States.Enabled && state != States.Enabling)
        {
            return;
        }
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
                PlayPop();
                break;
            case States.Enabling:
                timer = StartCoroutine(StartTimer(lifeTime));
                PlayEnabling();
                break;
            case States.Disabling:
                PlayDisabling();
                break;
        }
    }

    private IEnumerator StartTimer(float duration)
    {
        yield return new WaitForSeconds(duration);
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
}
