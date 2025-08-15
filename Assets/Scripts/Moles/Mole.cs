﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Mole abstract class. Contains the main behaviour of the mole and calls actions to be played on
different events (Enable, Disable, Pop...). These actions are to be defined in its derived
classes.
Enabl
Facilitates the creation of moles with different behaviours on specific events
(when popped -> change color ? play animation?)
*/

public abstract class Mole : MonoBehaviour
{
    public enum MolePopAnswer { Ok, Fake, Expired, Disabled, Paused }
    public enum MoleOutcome { Valid, Distractor }
    public enum MoleType { SimpleTarget, DistractorLeft, DistractorRight, PalmarGrasp, PinchGrasp, WristFlexion, WristExtension }

    public bool defaultVisibility = false;


    /**
     * The states may be reduced to 3 - 4 (by removing Popping, enabling...), however this could reduce the control over the Mole
     *
     * Enabling: Transition state before 'Enabled'. Used for animations.
     * Enabled: Passive state, Mole is active.
     *
     * When Mole is Hit:
     * Popping: Shot was taken, mole pops. Either results in OK or Fake animations.
     * Popped:The final state before mole is destroyed by the parent.
     *
     * When Mole is Not Hit:
     * Expired: All moles have a set expiration time after disabling, to track any shots happening after they leave. Set to 'Disabling' right after.
     * Missed: Moles which were not shot before they disabled enters this state. Set to 'Disabling' right after.
     * Disabling: Mole is being turned off. Usedfor animations befor set to 'Disabled'.
     * Disabled: Passive state, Mole is no longer active. The final state before mole is destroyed by the parent.
     * Note: Disabling/Disabled states are explicitly NOT called after Popped.
     *
    **/
    public enum States { Enabling, Enabled, Popping, Popped, Missed, Disabling, Expired, Disabled }

    private TargetSpawner parentTargetSpawner;
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
    private bool isOnDisabledCoolDown = false;
    private bool performanceFeedback = true;
    private MoleType _moleType = MoleType.SimpleTarget;

    protected States state = States.Disabled;
    public MoleOutcome moleCategory { get; private set; } // Can't be set directly, only through moleType setter.
    protected MoleType moleType
    {
        get => _moleType;
        set
        {
            _moleType = value;
            if (value == MoleType.DistractorLeft || value == MoleType.DistractorRight)
            {
                moleCategory = MoleOutcome.Distractor;
            }
            else
            {
                moleCategory = MoleOutcome.Valid;
            }
        }
    }
    protected bool IsInit = false;

    public void Init(TargetSpawner parentSpawner) // Needed when the Mole is instantiated, to avoid calling a method before the Awake and Start methods are called.
    {
        if (IsInit) return;

        Awake();
        Start();

        parentTargetSpawner = parentSpawner;
        IsInit = true;
    }

    private void Awake()
    {
        if (IsInit) return;

        moleType = MoleType.SimpleTarget;
        SetVisibility(defaultVisibility);
    }

    protected virtual void Start()
    {
        if (IsInit) return;

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
            {"MoleType", "NULL"},
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

    private void OnDestroy()
    {
        StopCoroutine(timer);
    }

    public void SetVisibility(bool isVisible)
    {
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Renderer>() != null)
                child.GetComponent<Renderer>().enabled = isVisible;
        }
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

    public MoleType GetMoleType()
    {
        return moleType;
    }

    public bool IsFake()
    {
        bool isFake = true;
        if (moleCategory == MoleOutcome.Valid)
        {
            isFake = false;
        }
        return isFake;
    }

    public bool ShouldPerformanceFeedback()
    {
        return performanceFeedback;
    }

    public bool CanBeActivated()
    {
        if (isOnDisabledCoolDown) return false;
        return (!(state == States.Enabled || state == States.Enabling || state == States.Disabling));
    }

    public void Enable(float enabledLifeTime, float expiringDuration, MoleType type = MoleType.SimpleTarget, int moleSpawnOrder = -1)
    {
        moleType = type;
        lifeTime = enabledLifeTime;
        expiringTime = expiringDuration;
        spawnOrder = moleSpawnOrder;
        ChangeState(States.Enabling);
    }

    public void Disable()
    {
        Debug.Log(state);
        if (state == States.Enabled && moleCategory == MoleOutcome.Valid)
        {
            ChangeState(States.Missed);
        }
        else
        {
            ChangeState(States.Disabling);
        }
    }

    public void SetPause(bool pause)
    {
        isPaused = pause;
    }

    public void SetPerformanceFeedback(bool perf)
    {
        performanceFeedback = perf;
    }

    // Pops the Mole. Returns an answer correspondind to its poping state.
    public MolePopAnswer Pop(Vector3 hitPoint)
    {
        if (isPaused) return MolePopAnswer.Paused;
        if (state != States.Enabled && state != States.Enabling && state != States.Expired) return MolePopAnswer.Disabled;

        Vector3 localHitPoint = Quaternion.AngleAxis(-transform.rotation.y, Vector3.up) * (hitPoint - transform.position);

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

        if (moleCategory == MoleOutcome.Valid)
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
                {"MoleSurfaceHitLocationY", localHitPoint.y},
                {"MoleType", System.Enum.GetName(typeof(MoleType), moleType)}
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

    protected virtual void PlayEnabled() { }
    protected virtual void PlayDisabled() { }
    protected virtual void PlayHoverEnter() { }
    protected virtual void PlayHoverLeave() { }
    public virtual void SetLoadingValue(float percent) { }

    /*
    Transition states. Need to be called at the end of its override in the derived class to
    finish the transition.
    */

    protected virtual void PlayEnabling()
    {
        if (moleCategory == MoleOutcome.Valid) loggerNotifier.NotifyLogger("Mole Spawned", EventLogger.EventType.MoleEvent, new Dictionary<string, object>()
                            {
                                {"MoleType", System.Enum.GetName(typeof(MoleType), moleType)}
                            });
        else loggerNotifier.NotifyLogger(System.Enum.GetName(typeof(MoleType), moleType) + " Mole Spawned", EventLogger.EventType.MoleEvent, new Dictionary<string, object>()
                            {
                                {"MoleType", System.Enum.GetName(typeof(MoleType), moleType)}
                            });

        timer = StartCoroutine(StartActivatedTimer(lifeTime));

        ChangeState(States.Enabled);
    }

    protected virtual void PlayMissed()
    {
        loggerNotifier.NotifyLogger("Mole Missed", EventLogger.EventType.MoleEvent, new Dictionary<string, object>()
                                            {
                                                {"MoleActivatedDuration", lifeTime},
                                                {"MoleType", System.Enum.GetName(typeof(MoleType), moleType)}
                                            });

        ChangeState(States.Disabling);
    }

    protected virtual void PlayDisabling()
    {
        if (moleCategory == MoleOutcome.Valid) loggerNotifier.NotifyLogger("Mole Expired", EventLogger.EventType.MoleEvent, new Dictionary<string, object>()
                            {
                                {"MoleActivatedDuration", lifeTime},
                                {"MoleType", System.Enum.GetName(typeof(MoleType), moleType)}
                            });
        else loggerNotifier.NotifyLogger(System.Enum.GetName(typeof(MoleType), moleType) + " Mole Expired", EventLogger.EventType.MoleEvent, new Dictionary<string, object>()
                            {
                                {"MoleActivatedDuration", lifeTime},
                                {"MoleType", System.Enum.GetName(typeof(MoleType), moleType)}
                            });

        ChangeState(States.Expired);
    }

    protected virtual void PlayPopping()
    {
        ChangeState(States.Popped);
    }

    private void ChangeState(States newState)
    {
        if (newState == state) return;
        LeaveState(state);
        state = newState;
        EnterState(state);
    }

    // Does certain actions when leaving a state.
    private void LeaveState(States state)
    {
        switch (state)
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
        switch (state)
        {
            case States.Enabling:
                PlayEnabling();
                break;
            case States.Enabled:
                PlayEnabled();
                break;

            case States.Disabling:
                PlayDisabling();
                break;
            case States.Disabled:
                parentTargetSpawner.DespawnMole();
                break;

            case States.Popping:
                PlayPopping();
                break;
            case States.Popped:
                parentTargetSpawner.DespawnMole();
                break;

            case States.Expired:
                StartCoroutine(StartExpiringTimer(expiringTime));
                break;
            case States.Missed:
                PlayMissed();
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
        while (expiringTimeLeft > 0)
        {
            if (!isPaused)
            {
                expiringTimeLeft -= Time.deltaTime;
            }
            yield return null;
        }

        ChangeState(States.Disabled);
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
            {"MoleType", moleType},
            {"MoleId", id.ToString("0000")},
            {"MoleSpawnOrder", spawnOrder.ToString("0000")},
            {"MoleIndexX", (int)Mathf.Floor(id/100)},
            {"MoleIndexY", (id % 100)},
            {"MoleNormalizedIndexX", normalizedIndex.x},
            {"MoleNormalizedIndexY", normalizedIndex.y},
        });
    }
}
