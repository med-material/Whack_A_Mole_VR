using System.Collections;
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
    public enum MoleOutcome { Valid, Distractor } // Node: if a third option is added, revise every condition in Mole.cs, childs and classes that use it (e.g. PaternInterface.cs, etc.)
    public enum MoleType { SimpleTarget, BallMole, DistractorLeft, DistractorRight, GestureMole, Invisible, BalloonMole, WaspMole, KeyMole } // ALWAYS add new types at the end of the enum to keep compatibility with previous versions. Related to witch prefab is used. (Look at TargetSpawner.cs prefab list)

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

    protected States state = States.Disabled;
    protected MoleOutcome moleOutcome = MoleOutcome.Valid;
    protected MoleType moleType = MoleType.SimpleTarget;
    protected string validationArg = ""; // Not used by default: can be used to store data for specific needs (e.g. gesture type for gesture moles)


    public virtual void Init(TargetSpawner parentSpawner) // Needed when the Mole is instantiated, to avoid calling a method before the Awake and Start methods are called.
    {

        // Security check: ensure the Mole is on the correct layer for detection.
        string layerName = LayerMask.LayerToName(gameObject.layer);
        if (layerName != "Target")
        {
            Debug.LogError($"Mole: The layer assigned to the Mole prefab '{gameObject.name}' is '{layerName}'. It should be 'Target' for correct detection.");
        }

        parentTargetSpawner = parentSpawner;

        moleType = MoleType.SimpleTarget;
        SetVisibility(defaultVisibility);

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
            {"MoleSurfaceHitLocationY", "NULL"},
            {"ExpectedGesture", "NULL"}
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

    public void SetId(string newId)
    {
        if (int.TryParse(newId, out int parsedId))
        {
            id = parsedId;
        }
        else
        {
            Debug.LogError($"SetId: Unable to cast '{newId}' to int.");
            id = -1;
        }
    }

    // Method call by pointer to check if the mole can be shoot
    public virtual bool checkShootingValidity(string arg = "")
    {
        return Mole.States.Enabled == state;
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
    public string GetValidationArg() => validationArg;
    public void SetValidationArg(string data) => validationArg = data;

    public States GetState()
    {
        return state;
    }

    public MoleType GetMoleType()
    {
        return moleType;
    }

    public bool IsFake() => (moleOutcome == MoleOutcome.Distractor);
    public bool IsValid() => (moleOutcome == MoleOutcome.Valid);

    public bool ShouldPerformanceFeedback()
    {
        return performanceFeedback;
    }

    public bool CanBeActivated()
    {
        if (isOnDisabledCoolDown) return false;
        return (!(state == States.Enabled || state == States.Enabling || state == States.Disabling));
    }

    public void Enable(float enabledLifeTime, float expiringDuration, MoleType type, MoleOutcome outcome, int moleSpawnOrder = -1)
    {
        moleType = type;
        moleOutcome = outcome;
        lifeTime = enabledLifeTime;
        expiringTime = expiringDuration;
        spawnOrder = moleSpawnOrder;
        ChangeState(States.Enabling);
    }

    public void Disable()
    {
        if (state == States.Enabled && moleOutcome == MoleOutcome.Valid)
        {
            ChangeState(States.Missed);
        }
        else
        {
            ChangeState(States.Disabling);
        }
    }

    public void SetPause(bool pause) => isPaused = pause;

    public void SetPerformanceFeedback(bool perf) => performanceFeedback = perf;

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

        if (moleOutcome == MoleOutcome.Valid)
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

    protected virtual IEnumerator PlayEnabling()
    {
        if (moleOutcome == MoleOutcome.Valid) loggerNotifier.NotifyLogger("Mole Spawned", EventLogger.EventType.MoleEvent, new Dictionary<string, object>()
                            {
                                {"MoleType", System.Enum.GetName(typeof(MoleType), moleType)}
                            });
        else loggerNotifier.NotifyLogger(System.Enum.GetName(typeof(MoleType), moleType) + " Mole Spawned", EventLogger.EventType.MoleEvent, new Dictionary<string, object>()
                            {
                                {"MoleType", System.Enum.GetName(typeof(MoleType), moleType)}
                            });

        timer = StartCoroutine(StartActivatedTimer(lifeTime));

        ChangeState(States.Enabled);
        yield break;
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

    protected virtual IEnumerator PlayDisabling()
    {
        if (moleOutcome == MoleOutcome.Valid) loggerNotifier.NotifyLogger("Mole Expired", EventLogger.EventType.MoleEvent, new Dictionary<string, object>()
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
        yield break;
    }

    protected virtual IEnumerator PlayPopping()
    {
        ChangeState(States.Popped);
        yield break;
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
                StartCoroutine(PlayEnabling());
                break;
            case States.Enabled:
                PlayEnabled();
                break;

            case States.Disabling:
                StartCoroutine(PlayDisabling());
                break;
            case States.Disabled:
                parentTargetSpawner.DespawnMole();
                break;

            case States.Popping:
                StartCoroutine(PlayPopping());
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
        string moleId = id.ToString("0000000"); // Formated as ZZZXXYY (ZZZ is mole rank, XX is the X index, YY is the Y index)
        if (moleId.Length != 7) Debug.LogError("Mole ID is not 7 digits long: " + moleId);

        string MoleIndexX = moleId.Substring(3, 2);
        string MoleIndexY = moleId.Substring(5, 2);

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
            {"MoleId", moleId},
            {"MoleSpawnOrder", spawnOrder.ToString("0000")},
            {"MoleIndexX", MoleIndexX},
            {"MoleIndexY", MoleIndexY},
            {"MoleNormalizedIndexX", normalizedIndex.x},
            {"MoleNormalizedIndexY", normalizedIndex.y},
            {"ExpectedGesture", validationArg}
        });
    }
}
