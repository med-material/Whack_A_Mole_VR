using System.Collections;
using UnityEngine;


public class InteractiveMole : Mole
{
    [Header("Legacy Animation (preferred)")]
    [Tooltip("If provided, legacy Animation component will be used to play idle/pop animations.")]
    [SerializeField] private Animation animationPlayer;

    [Tooltip("Name of the Idle clip inside the Animation component's clip list.")]
    [SerializeField] private string idleClipName = "Idle";

    [Header("Check if you want the mole to use an idle animation, \n make sure the idle animation clip name \n matches the string provided.")]
    [SerializeField] private bool useIdleAnimation = false; // False by default, to avoid errors when idle animation is not assigned.


    [SerializeField]
    private AudioClip enableSound;

    private Coroutine idleFloatCoroutine;
    private Vector3 startLocalPosition;
    private AudioSource audioSource;


    private string playingClip = "";

    public override void Init(TargetSpawner parentSpawner)
    {
        if (animationPlayer == null) animationPlayer = gameObject.GetComponent<Animation>();
        audioSource = gameObject.GetComponent<AudioSource>();

        if (useIdleAnimation && (animationPlayer == null || !HasAnimationClip(idleClipName)))
        {
            Debug.LogWarning($"InteractiveMole on '{gameObject.name}': 'useIdleAnimation' is true but Idle clip '{idleClipName}' or Animation component is missing. Assign the clip/component or disable 'useIdleAnimation'.", this);
        }

        if (useIdleAnimation && HasAnimationClip(idleClipName))
        {
            PlayAnimation(idleClipName);
        }
        else if (animationPlayer != null)
        {
            PlayAnimation("EnableDisable");
        }

        base.Init(parentSpawner);
        startLocalPosition = transform.localPosition;
    }

    protected override void PlayEnabled()
    {
        if (useIdleAnimation && HasAnimationClip(idleClipName))
        {
            PlayAnimation(idleClipName);
        }
        else if (animationPlayer != null)
        {
            PlayAnimation("EnableDisable");
        }

    }

    protected override void PlayHoverEnter()
    {
        // No-op by default (derived classes can override).
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
        // No-op by default. Can be used to revert hover visual changes if desired.
    }

    public override void SetLoadingValue(float percent)
    {
        //This command can be used to set a loading bar on the mole, if desired.
    }

    protected override IEnumerator PlayPopping()
    {
        StopIdleVisuals();

        if (animationPlayer != null)
        {
            if (ShouldPerformanceFeedback())
            {
                if (moleOutcome == MoleOutcome.Valid) PlayAnimation("PopCorrectMole");

                else PlayAnimation("PopWrongMole");
            }
            else
            {
                PlayAnimation("Pop");
            }

            float duration = GetAnimationDuration();
            yield return new WaitForSeconds(Mathf.Max(0.05f, duration));
        }

        yield return base.PlayPopping();
    }

    protected override IEnumerator PlayDisabling()
    {
        StopIdleVisuals();

        if (animationPlayer != null)
        {
            PlayAnimation("EnableDisable");
            float duration = GetAnimationDuration();
            if (duration > 0f) yield return new WaitForSeconds(duration);
        }

        PlaySound(enableSound);

        yield return base.PlayDisabling();
    }

    protected override void PlayMissed()
    {
        StopIdleVisuals();

        if (animationPlayer != null && ShouldPerformanceFeedback())
        {
            PlayAnimation("PopWrongMole");
        }

        base.PlayMissed();
    }

    private void StopIdleVisuals()
    {
        if (idleFloatCoroutine != null)
        {
            try { StopCoroutine(idleFloatCoroutine); } catch { }
            idleFloatCoroutine = null;
            transform.localPosition = startLocalPosition;
        }


        if (useIdleAnimation && HasAnimationClip(idleClipName) && animationPlayer != null)
        {
            animationPlayer.Stop(idleClipName);
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

    private void PlayAnimation(string animationName)
    {
        if (animationPlayer == null) return;
        playingClip = animationName;
        animationPlayer.PlayQueued(animationName);
    }

    private float GetAnimationDuration()
    {
        if (animationPlayer == null || string.IsNullOrEmpty(playingClip)) return 0f;
        AnimationClip clip = animationPlayer.GetClip(playingClip);
        return clip != null ? clip.length : 0f;
    }

    private bool HasAnimationClip(string name)
    {
        return animationPlayer != null && !string.IsNullOrEmpty(name) && animationPlayer.GetClip(name) != null;
    }
}
