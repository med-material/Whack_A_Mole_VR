using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/*
Mole abstract class. Contains the main behaviour of the mole and calls actions to be played on
different events (Enable, Disable, Pop...). These actions are to be defined in its derived
classes.
Facilitates the creation of moles with different behaviours on specific events 
(when popped -> change color ? play animation?) 
*/

public abstract class Mole : MonoBehaviour
{
    public enum MolePopAnswer {Ok, Fake, Expired, Disabled, Paused}

    // The states may be reduced to 3 - 4 (by removing Popping, enabling...), however this could reduce the control over the Mole
    public enum States {Disabled, Enabled, Expired, Popping, Popped, Enabling, Disabling}

    [SerializeField]
    private float disableCooldown = 3f;

    protected States state = States.Disabled;
    protected bool fake = false;

    private class StateUpdateEvent: UnityEvent<bool, Mole>{};
    private StateUpdateEvent stateUpdateEvent = new StateUpdateEvent();
    private Coroutine timer;
    private float lifeTime;
    private float expiringTime;
    private int id = -1;
    private int spawnOrder = -1;
    private float activatedTimeLeft;
    private float expiringTimeLeft;
    private bool isPaused = false;
    private Vector2 normalizedIndex;
    private LoggerNotifier loggerNotifier;
    private float disabledTimeLeft = 0f;
    private bool isOnDisabledCoolDown = false;

    protected virtual void Start()
    {
        Reset();

        // Initialization of the LoggerNotifier. Here we will only raise Event, and we will use a function to pass and update 
        // certain parameters values every time we raise an event (UpdateLogNotifierGeneralValues). We don't set any starting values.
        loggerNotifier = new LoggerNotifier(UpdateLogNotifierGeneralValues, new Dictionary<string, string>(){
            {"MolePositionWorldX", "NULL"},
            {"MolePositionWorldY", "NULL"},
            {"MolePositionWorldZ", "NULL"},
            {"MolePositionLocalX", "NULL"},
            {"MolePositionLocalY", "NULL"},
            {"MolePositionLocalZ", "NULL"},
            {"MoleSize", "NULL"},
            {"MoleLifeTime", "NULL"},
            {"MoleActivatedDuration", "NULL"},
            {"MoleId", "NULL"},
            {"MoleSpawnOrder", "NULL"},
            {"MoleIndexX", "NULL"},
            {"MoleIndexY", "NULL"},
            {"MoleNormalizedIndexX", "NULL"},
            {"MoleNormalizedIndexY", "NULL"},
            {"MoleSurfaceHitLocationX", "NULL"},
            {"MoleSurfaceHitLocationY", "NULL"}
        });
    }

    public void SetSpawnOrder(int newSpawnOrder)
    {
        spawnOrder = newSpawnOrder;
    }

    public void SetId(int newId)
    {
        id = newId;
    }

    public void SetNormalizedIndex(Vector2 newNormalizedIndex)
    {
        normalizedIndex = newNormalizedIndex;
    }

    public int GetSpawnOrder()
    {
        return spawnOrder;
    }

    public int GetId()
    {
        return id;
    }

    public States GetState()
    {
        return state;
    }

    public bool IsFake()
    {
        return fake;
    }

    public bool CanBeActivated()
    {
        if (isOnDisabledCoolDown) return false;
        return (!(state == States.Enabled || state == States.Enabling || state == States.Disabling));
    }

    public void Enable(float enabledLifeTime, float expiringDuration, bool isFake = false, int moleSpawnOrder = -1)
    {
        fake = isFake;
        lifeTime = enabledLifeTime;
        expiringTime = expiringDuration;
        spawnOrder = moleSpawnOrder;
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
        isOnDisabledCoolDown = false;
        isPaused = false;
        state = States.Disabled;
        PlayReset();
    }

    // Pops the Mole. Returns an answer correspondind to its poping state.
    public MolePopAnswer Pop(Vector3 hitPoint)
    {
        if (isPaused) return MolePopAnswer.Paused;
        if (state != States.Enabled && state != States.Enabling && state != States.Expired) return MolePopAnswer.Disabled;

        Vector3 localHitPoint = Quaternion.AngleAxis(-transform.rotation.y,Vector3.up) * (hitPoint - transform.position);

        if (state == States.Expired)
        {
            loggerNotifier.NotifyLogger("Expired Mole Hit", EventLogger.EventType.MoleEvent, new Dictionary<string, object>()
            {
                {"MoleExpiredDuration", expiringTime - expiringTimeLeft},
                {"MoleSurfaceHitLocationX", localHitPoint.x},
                {"MoleSurfaceHitLocationY", localHitPoint.y}
            });
            return MolePopAnswer.Expired;
        }

        if (!fake) 
        {
            loggerNotifier.NotifyLogger("Mole Hit", EventLogger.EventType.MoleEvent, new Dictionary<string, object>()
            {
                {"MoleActivatedDuration", lifeTime - activatedTimeLeft},
                {"MoleSurfaceHitLocationX", localHitPoint.x},
                {"MoleSurfaceHitLocationY", localHitPoint.y}
            });

            ChangeState(States.Popping);
            return MolePopAnswer.Ok;
        }
        else 
        {
            loggerNotifier.NotifyLogger("Fake Mole Hit", EventLogger.EventType.MoleEvent, new Dictionary<string, object>()
            {
                {"MoleActivatedDuration", lifeTime - activatedTimeLeft},
                {"MoleSurfaceHitLocationX", localHitPoint.x},
                {"MoleSurfaceHitLocationY", localHitPoint.y}
            });

            ChangeState(States.Popping);
            return MolePopAnswer.Fake;
        }
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

    public UnityEvent<bool, Mole> GetUpdateEvent()
    {
        return stateUpdateEvent;
    }

    protected virtual void PlayEnable() {}
    protected virtual void PlayDisable() {}
    protected virtual void PlayReset() {}
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
        ChangeState(States.Expired);
    }

    protected virtual void PlayPop() 
    {
        ChangeState(States.Popped);
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

    // Does certain actions when leaving a state.
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

    // Does certain actions when entering a state.
    private void EnterState(States state)
    {
        switch(state)
        {
            case States.Disabled:
                PlayDisable();
                isOnDisabledCoolDown = true;
                StartCoroutine(StartDisabledCooldownTimer(disableCooldown));
                break;
            case States.Enabled:
                PlayEnable();
                break;
            case States.Popping:
                PlayPop();
                break;
            case States.Enabling:

                if (!fake) loggerNotifier.NotifyLogger("Mole Spawned", EventLogger.EventType.MoleEvent);
                else loggerNotifier.NotifyLogger("Fake Mole Spawned", EventLogger.EventType.MoleEvent);

                if (!fake) stateUpdateEvent.Invoke(true, this);

                timer = StartCoroutine(StartActivatedTimer(lifeTime));
                PlayEnabling();
                break;
            case States.Disabling:
                if (!fake) loggerNotifier.NotifyLogger("Mole Expired", EventLogger.EventType.MoleEvent, new Dictionary<string, object>()
                            {
                                {"MoleActivatedDuration", lifeTime}
                            });
                else loggerNotifier.NotifyLogger("Fake Mole Expired", EventLogger.EventType.MoleEvent, new Dictionary<string, object>()
                            {
                                {"MoleActivatedDuration", lifeTime}
                            });

                if (!fake) stateUpdateEvent.Invoke(false, this);

                PlayDisabling();
                break;
            case States.Expired:
                StartCoroutine(StartExpiringTimer(expiringTime));
                break;
        }
    }

    // IEnumerator starting the enabled timer.
    private IEnumerator StartActivatedTimer(float duration)
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

        if (state == States.Enabled)
        {
            Disable();
        }
    }

    // IEnumerator starting the expiring timer.
    private IEnumerator StartExpiringTimer(float duration)
    {
        expiringTimeLeft = duration;
        while (activatedTimeLeft > 0)
        {
            if (!isPaused)
            {
                expiringTimeLeft -= Time.deltaTime;
            }
            yield return null;
        }

        EnterState(States.Disabled);
    }

    private IEnumerator StartDisabledCooldownTimer(float duration)
    {
        disabledTimeLeft = duration;
        while (disabledTimeLeft > 0)
        {
            if (!isPaused)
            {
                disabledTimeLeft -= Time.deltaTime;
            }
            yield return null;
        }

        isOnDisabledCoolDown = false;
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
            {"MoleSize", (this.GetComponentsInChildren<Renderer>()[0].bounds.max.x - this.GetComponentsInChildren<Renderer>()[0].bounds.min.x)},
            {"MoleLifeTime", lifeTime},
            {"MoleId", id.ToString("0000")},
            {"MoleSpawnOrder", spawnOrder.ToString("0000")},
            {"MoleIndexX", (int)Mathf.Floor(id/100)},
            {"MoleIndexY", (id % 100)},
            {"MoleNormalizedIndexX", normalizedIndex.x},
            {"MoleNormalizedIndexY", normalizedIndex.y},
        });
    }
}
