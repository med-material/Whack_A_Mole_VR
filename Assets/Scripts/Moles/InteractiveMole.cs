using System.Collections;
using UnityEngine;
using UnityEngine.Events;


public class InteractiveMole : Mole
{
    [Header("Animation Events")]
    [Tooltip("UnityEvent invoked when the mole spawns/enables. Use this with InteractiveMoleAnimationController to trigger spawn animations.")]
    [SerializeField] private UnityEvent onMoleSpawnEvent = new UnityEvent();

    [Tooltip("UnityEvent invoked when the mole enters idle state. Use this with InteractiveMoleAnimationController to trigger idle animations.")]
    [SerializeField] private UnityEvent onMoleIdleEvent = new UnityEvent();

    [Tooltip("UnityEvent invoked when the mole starts popping. Use this to trigger additional animations/effects like splash effects, particles, sounds, etc.")]
    [SerializeField] private UnityEvent onMolePopEvent = new UnityEvent();

    [Header("Validation-Aware Animation Events (Optional)")]
    [Tooltip("UnityEvent with validation string parameter. Invoked when the mole spawns. Use for validation-specific spawn animations (e.g., different Wasp types).")]
    [SerializeField] private UnityEvent<string> onMoleSpawnWithValidationEvent = new UnityEvent<string>();

    [Tooltip("UnityEvent with validation string parameter. Invoked when the mole enters idle state. Use for validation-specific idle animations.")]
    [SerializeField] private UnityEvent<string> onMoleIdleWithValidationEvent = new UnityEvent<string>();

    [Tooltip("UnityEvent with validation string parameter. Invoked when the mole pops. Use for validation-specific pop animations.")]
    [SerializeField] private UnityEvent<string> onMolePopWithValidationEvent = new UnityEvent<string>();

    [Header("Animation Controller Reference (Optional)")]
    [Tooltip("Reference to InteractiveMoleAnimationController for getting animation durations. If not set, will try to find it automatically.")]
    [SerializeField] private InteractiveMoleAnimationController animationController;

    [SerializeField]
    private AudioClip enableSound;

    [SerializeField]
    private GameObject hoverInfoContainer;

    [SerializeField]
    private HoverInfo[] hoverInfos;

    [System.Serializable]
    public class HoverInfo
    {
        public string key;
        public GameObject value;
    }

    private Coroutine idleFloatCoroutine;
    private Vector3 startLocalPosition;
    private AudioSource audioSource;
    private bool hoverInfoShouldBeShown = false;

    public override void Init(TargetSpawner parentSpawner)
    {
        audioSource = gameObject.GetComponent<AudioSource>();

        // Try to find the animation controller if not assigned
        if (animationController == null)
        {
            animationController = GetComponent<InteractiveMoleAnimationController>();
        }

        // initialize hover info hidden
        showHoverInfo(false);

        base.Init(parentSpawner);
        startLocalPosition = transform.localPosition;
    }

    protected override IEnumerator PlayEnabling()
    {
        // prepare hover info for this mole when enabling
        showHoverInfo(false);
        updateHoverInfo();

        // stop idle visuals if any
        StopIdleVisuals();

        // Invoke spawn event for external animation controllers
        onMoleSpawnEvent?.Invoke();
        
        // Also invoke validation-aware event
        onMoleSpawnWithValidationEvent?.Invoke(GetValidationArg());

        // Wait for spawn animation to complete if we have an animation controller
        if (animationController != null)
        {
            float duration = animationController.GetLastPlayedAnimationDuration();
            if (duration > 0f)
            {
                yield return new WaitForSeconds(Mathf.Max(0.05f, duration));
            }
        }

        yield return base.PlayEnabling();
    }

    protected override void PlayEnabled()
    {
        // Invoke idle event for external animation controllers
        onMoleIdleEvent?.Invoke();
        
        // Also invoke validation-aware event
        onMoleIdleWithValidationEvent?.Invoke(GetValidationArg());
    }

    protected override void PlayHoverEnter()
    {
        showHoverInfo(true);
    }

    public override bool checkShootingValidity(string arg = "")
    {
        if (arg == GetValidationArg())
        {
            return base.checkShootingValidity(arg);
        }
        return false;
    }

    protected override void PlayHoverLeave()
    {
        showHoverInfo(false);
    }

    public override void SetLoadingValue(float percent)
    {
        //This command can be used to set a loading bar on the mole, if desired.
    }

    protected override IEnumerator PlayPopping()
    {
        showHoverInfo(false);
        StopIdleVisuals();

        // Invoke pop event for layered effects (splash, particles, etc.)
        onMolePopEvent?.Invoke();
        
        // Also invoke validation-aware event
        onMolePopWithValidationEvent?.Invoke(GetValidationArg());

        // Wait for pop animation to complete if we have an animation controller
        if (animationController != null)
        {
            float duration = animationController.GetLastPlayedAnimationDuration();
            if (duration > 0f)
            {
                yield return new WaitForSeconds(Mathf.Max(0.05f, duration));
            }
        }

        yield return base.PlayPopping();
    }

    protected override IEnumerator PlayDisabling()
    {
        showHoverInfo(false);
        StopIdleVisuals();

        PlaySound(enableSound);

        yield return base.PlayDisabling();
    }

    protected override void PlayMissed()
    {
        showHoverInfo(false);
        StopIdleVisuals();

        base.PlayMissed();
    }

    private void updateHoverInfo()
    {
        hoverInfoShouldBeShown = false;
        if (hoverInfos == null || hoverInfos.Length == 0)
        {
            return;
        }

        string val = GetValidationArg();
        foreach (HoverInfo hoverInfo in hoverInfos)
        {
            if (hoverInfo == null) continue;
            bool match = hoverInfo.key == val;
            if (hoverInfo.value != null)
            {
                hoverInfo.value.SetActive(match);
            }
            if (match) hoverInfoShouldBeShown = true;
        }
    }

    private void showHoverInfo(bool status)
    {
        if (hoverInfoContainer == null)
        {
            return;
        }

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
        if (idleFloatCoroutine != null)
        {
            try { StopCoroutine(idleFloatCoroutine); } catch { }
            idleFloatCoroutine = null;
        }
    }

    private void PlaySound(AudioClip audioClip)
    {
        if (!audioSource)
        {
            return;
        }
        audioSource.clip = audioClip;
        audioSource.Play();
    }
}
