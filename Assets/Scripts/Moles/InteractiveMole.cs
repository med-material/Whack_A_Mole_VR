/*
InteractiveMole
Summary:
- Event-driven controller for Mole lifecycle (spawn, idle, pop) using UnityEvents.
- Supports validation-aware variants with UnityEvent<string> for context-specific animations.
- Optional autoHookValidationEvents: auto-wires validation events to PlayAnimatorStateByName at runtime.
- Public PlayAnimatorStateByName: plays Animator states matching validation keys, warns if missing.
- Manages hover info keyed by validationArg and optional enable sound.
Usage:
- Assign/auto-detect Animator; set animatorLayerIndex (default 0).
- Enable autoHookValidationEvents OR manually wire validation events to PlayAnimatorStateByName (Dynamic string) in Inspector.
- Ensure spawner sets validationArg and Animator contains states named exactly like validation keys (case sensitive).
*/
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class InteractiveMole : Mole
{
    [Header("Spawn Behavior")]
    [Tooltip("If true, mole becomes interactive immediately. If false, waits for spawn animation to complete.")]
    [SerializeField] private bool immediateInteraction = false;

    [Header("Animation Events")]
    [Tooltip("UnityEvent invoked when the mole spawns/enables.")]
    [SerializeField] private UnityEvent onMoleSpawnEvent = new UnityEvent();

    [Tooltip("UnityEvent invoked when the mole enters idle state.")]
    [SerializeField] private UnityEvent onMoleIdleEvent = new UnityEvent();

    [Tooltip("Unity event invoked whe nthe mole gets hovered over.")]
    [SerializeField] private UnityEvent onMoleHoverEnterEvent = new UnityEvent();

    [Tooltip("UnityEvent invoked when the mole leaves hover state.")]
    [SerializeField] private UnityEvent onMoleHoverLeaveEvent = new UnityEvent();

    [Tooltip("UnityEvent invoked when the mole starts popping.")]
    [SerializeField] private UnityEvent onMolePopEvent = new UnityEvent();

    [Header("Dwell Progress Visual")]
    [Tooltip("Optional UI Image for radial fill progress indicator")]
    [SerializeField] private UnityEngine.UI.Image dwellProgressImage;

    [Header("Validation-Aware Animation Events (Optional)")]
    [Tooltip("Auto-subscribe validation events to PlayAnimatorStateByName in OnEnable.")]
    [SerializeField] private bool autoHookValidationEvents = false;

    [Tooltip("Invoked when the mole spawns with a validation string.")]
    [SerializeField] private UnityEvent<string> onMoleSpawnWithValidationEvent = new UnityEvent<string>();

    [Tooltip("Invoked when the mole idles with a validation string.")]
    [SerializeField] private UnityEvent<string> onMoleIdleWithValidationEvent = new UnityEvent<string>();

    [Tooltip("Invoked when the mole pops with a validation string.")]
    [SerializeField] private UnityEvent<string> onMolePopWithValidationEvent = new UnityEvent<string>();

    [Header("Animator Settings")]
    [Tooltip("Animator component controlling this mole.")]
    [SerializeField] private Animator animator;

    [Tooltip("Animator layer index to query for animation durations (usually 0).")]
    [SerializeField] private int animatorLayerIndex = 0;

    [SerializeField] private AudioClip enableSound;
    [SerializeField] private AudioClip popSound;

    [SerializeField] private GameObject hoverInfoContainer;
    [SerializeField] private HoverInfo[] hoverInfos;

    [System.Serializable]
    public class HoverInfo
    {
        public string key;
        public GameObject value;
    }

    private Coroutine idleFloatCoroutine;
    private Vector3 startLocalPosition;
    private AudioSource audioSource;
    private bool hoverInfoShouldBeShown;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (autoHookValidationEvents) HookValidationAnimationEvents();
        showHoverInfo(false);
    }

    private void OnDisable()
    {
        if (autoHookValidationEvents) UnhookValidationAnimationEvents();
    }

    public override void Init(TargetSpawner parentSpawner)
    {
        base.Init(parentSpawner);
        startLocalPosition = transform.localPosition;
    }

    protected override IEnumerator PlayEnabling()
    {
        showHoverInfo(false);
        updateHoverInfo();
        StopIdleVisuals();

        onMoleSpawnEvent?.Invoke();
        onMoleSpawnWithValidationEvent?.Invoke(GetValidationArg());

        // If immediateInteraction is enabled, transition to Enabled state immediately
        // Otherwise, wait for spawn animation to complete (for moles with Animator-based spawn animations)
        if (immediateInteraction)
        {
            yield return base.PlayEnabling();
        }
        else
        {
            float duration = GetCurrentAnimationDuration();
            if (duration > 0f) yield return new WaitForSeconds(Mathf.Max(0.05f, duration));
            yield return base.PlayEnabling();
        }
    }

    protected override void PlayEnabled()
    {
        onMoleIdleEvent?.Invoke();
        onMoleIdleWithValidationEvent?.Invoke(GetValidationArg());
    }

    protected override void PlayHoverEnter()
    {
        showHoverInfo(true);
        onMoleHoverEnterEvent?.Invoke();
    }
        

    public override bool checkShootingValidity(string arg = "")
    {
        string validationArg = GetValidationArg();
        
        Debug.Log($"[InteractiveMole] checkShootingValidity called. Arg: '{arg}', ValidationArg: '{validationArg}', State: {GetState()}");
        
        // If no validation argument is required (empty string), allow any gesture
        if (string.IsNullOrEmpty(validationArg))
        {
            Debug.Log($"[InteractiveMole] No validation required - accepting any gesture");
            return base.checkShootingValidity(arg);
        }
        
        // If validation is required, check if the gesture matches
        // Special case: "Neutral" means EMG is below threshold, treat it as "waiting for gesture"
        // Don't reject it, but don't accept it either - let the dwell timer wait for correct gesture
        if (arg == "Neutral" || arg == "Unknown")
        {
            Debug.Log($"[InteractiveMole] Neutral/Unknown gesture - waiting for active gesture");
            return false; // Don't progress dwell, but also don't show as "invalid"
        }
        
        if (arg == validationArg) 
        {
            Debug.Log($"[InteractiveMole] Gesture matches! '{arg}' == '{validationArg}'");
            return base.checkShootingValidity(arg);
        }
        
        Debug.Log($"[InteractiveMole] Gesture mismatch! '{arg}' != '{validationArg}' - returning false");
        return false;
    }

    protected override void PlayHoverLeave()
    {
        showHoverInfo(false);
        onMoleHoverLeaveEvent?.Invoke();

    }

    public override void SetLoadingValue(float percent) 
    {
        if (dwellProgressImage != null)
        {
            dwellProgressImage.fillAmount = Mathf.Clamp01(percent);
        }
    }

    protected override IEnumerator PlayPopping()
    {
        showHoverInfo(false);
        StopIdleVisuals();

        PlaySound(popSound);

        onMolePopEvent?.Invoke();
        onMolePopWithValidationEvent?.Invoke(GetValidationArg());

        float duration = GetCurrentAnimationDuration();
        if (duration > 0f) yield return new WaitForSeconds(Mathf.Max(0.05f, duration));

        yield return base.PlayPopping();
    }

    protected override IEnumerator PlayDisabling()
    {

        showHoverInfo(false);
        StopIdleVisuals();
        yield return base.PlayDisabling();
    }

    protected override void PlayMissed()
    {
        showHoverInfo(false);
        StopIdleVisuals();
        base.PlayMissed();
    }

    private float GetCurrentAnimationDuration()
    {
        if (animator != null && animator.isActiveAndEnabled)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(animatorLayerIndex);
            return stateInfo.length;
        }
        return 0f;
    }

    private void updateHoverInfo()
    {
        hoverInfoShouldBeShown = false;
        if (hoverInfos == null || hoverInfos.Length == 0) return;

        string val = GetValidationArg();
        foreach (HoverInfo hoverInfo in hoverInfos)
        {
            if (hoverInfo == null) continue;
            bool match = hoverInfo.key == val;
            if (hoverInfo.value != null) hoverInfo.value.SetActive(match);
            if (match) hoverInfoShouldBeShown = true;
        }
    }

    private void showHoverInfo(bool status)
    {
        if (hoverInfoContainer == null) return;
        hoverInfoContainer.SetActive(status && hoverInfoShouldBeShown);
    }

    private void StopIdleVisuals()
    {
        if (idleFloatCoroutine != null)
        {
            try { StopCoroutine(idleFloatCoroutine); } catch { }
            idleFloatCoroutine = null;
            transform.localPosition = startLocalPosition;
        }
    }

    private void OnDestroy()
    {
        if (autoHookValidationEvents) UnhookValidationAnimationEvents();
        if (idleFloatCoroutine != null)
        {
            try { StopCoroutine(idleFloatCoroutine); } catch { }
            idleFloatCoroutine = null;
        }
    }

    private void PlaySound(AudioClip audioClip)
    {
        if (!audioSource) return;
        audioSource.clip = audioClip;
        audioSource.Play();
    }

    public void PlayAnimatorStateByName(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return;
        if (!HasAnimatorState(stateName))
        {
            Debug.LogWarning($"[InteractiveMole:{gameObject.name}] Animator state not found: '{stateName}' on layer {animatorLayerIndex}");
            return;
        }
        animator.CrossFade(stateName, 0f, animatorLayerIndex, 0f);
    }

    private bool HasAnimatorState(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return false;
        int shortHash = Animator.StringToHash(stateName);
        if (animator.HasState(animatorLayerIndex, shortHash)) return true;
        int fullHash = Animator.StringToHash($"Base Layer.{stateName}");
        return animator.HasState(animatorLayerIndex, fullHash);
    }

    private void HookValidationAnimationEvents()
    {
        UnityEvent<string>[] events =
        {
            onMoleSpawnWithValidationEvent,
            onMoleIdleWithValidationEvent,
            onMolePopWithValidationEvent
        };
        foreach (UnityEvent<string> evt in events)
        {
            if (evt == null) continue;
            evt.RemoveListener(PlayAnimatorStateByName);
            evt.AddListener(PlayAnimatorStateByName);
        }
    }

    private void UnhookValidationAnimationEvents()
    {
        UnityEvent<string>[] events =
        {
            onMoleSpawnWithValidationEvent,
            onMoleIdleWithValidationEvent,
            onMolePopWithValidationEvent
        };
        foreach (UnityEvent<string> evt in events)
        {
            if (evt == null) continue;
            evt.RemoveListener(PlayAnimatorStateByName);
        }
    }
}
