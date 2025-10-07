using System.Collections;
using UnityEngine;

public class InteractiveMole : Mole
{
    [Header("Animator (optional)")]
    [Tooltip("If provided, animator will be used to play idle/pop animations. Otherwise fallbacks are used.")]
    [SerializeField] private Animator animator;

    [Tooltip("Animator state name to play for idle (optional).")]
    [SerializeField] private string idleStateName = "Idle";

    [Tooltip("Animator trigger name used to play popping animation (optional).")]
    [SerializeField] private string popTriggerName = "Pop";

    [Header("Fallback idle float (used when no animator or as visual enhancement)")]
    [SerializeField] private bool useIdleFloating = false;
    [SerializeField] private float floatAmplitude = 0f;
    [SerializeField] private float floatFrequency = 0f;

    [Header("Pop animation fallback")]
    [Tooltip("If > 0 will wait this many seconds after triggering animator (or as fallback wait). Set to the known pop animation length for reliable timing.")]
    [SerializeField] private float popAnimationDuration = 0.35f;

    [SerializeField]
    private AudioClip enableSound;

    private Coroutine idleFloatCoroutine;
    private Vector3 startLocalPosition;
    private AudioSource audioSource;


    // Ensure base initialization runs
    public override void Init(TargetSpawner parentSpawner)
    {
        base.Init(parentSpawner);
        startLocalPosition = transform.localPosition;
    }

    protected override void PlayEnabled()
    {
        if (animator)
        {
            if (!string.IsNullOrEmpty(idleStateName))
            {
                animator.Play(idleStateName, 0, 0f);
            }
        }

        if (useIdleFloating && idleFloatCoroutine == null)
        {
            idleFloatCoroutine = StartCoroutine(IdleFloatCoroutine());
        }
    }

    protected override void PlayHoverEnter()
    {
        // Derived class can react to hover if needed; keep default behavior by calling base if needed.
        // No-op here (safe).
    }

    public override bool checkShootingValidity(string arg = "")
    {
        if (arg == moleType.ToString())
        {
            return base.checkShootingValidity(arg);
        }
        return false;
    }

    protected override void PlayHoverLeave()
    {
        // No-op by default.
    }

    public override void SetLoadingValue(float percent)
    {
        //This command can be used to set a loading bar on the mole, if desired.
    }

    protected override IEnumerator PlayPopping()
    {
        if (animator)
        {
            if (!string.IsNullOrEmpty(popTriggerName) && animator.HasParameterOfType(popTriggerName, AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger(popTriggerName);
            }
            else
            {
                // Try to play a state named "Pop" if trigger not set
                animator.Play("Pop", 0, 0f);
            }

            if (popAnimationDuration > 0f)
            {
                yield return new WaitForSeconds(popAnimationDuration);
            }
            else
            {
                // allow animator to update and then read current state's length (safe fallback)
                yield return null;
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                float wait = Mathf.Max(0.05f, info.length);
                yield return new WaitForSeconds(wait);
            }
        }

        if (idleFloatCoroutine != null)
        {
            try { StopCoroutine(idleFloatCoroutine); } catch { }
            idleFloatCoroutine = null;
            transform.localPosition = startLocalPosition;
        }

        yield return base.PlayPopping();
    }

    protected override IEnumerator PlayDisabling()
    {
        if (idleFloatCoroutine != null)
        {
            try { StopCoroutine(idleFloatCoroutine); } catch { }
            idleFloatCoroutine = null;
            transform.localPosition = startLocalPosition;
        }

        PlaySound(enableSound);


        yield return base.PlayDisabling();
    }

    protected override void PlayMissed()
    {
        if (idleFloatCoroutine != null)
        {
            try { StopCoroutine(idleFloatCoroutine); } catch { }
            idleFloatCoroutine = null;
            transform.localPosition = startLocalPosition;
        }

        base.PlayMissed();
    }

    private IEnumerator IdleFloatCoroutine()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * floatFrequency * Mathf.PI * 2f;
            float y = Mathf.Sin(t) * floatAmplitude;
            transform.localPosition = startLocalPosition + new Vector3(0f, y, 0f);
            yield return null;
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

public static class AnimatorExtensions
{
    public static bool HasParameterOfType(this Animator animator, string name, AnimatorControllerParameterType type)
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.type == type && p.name == name) return true;
        }
        return false;
    }
}
