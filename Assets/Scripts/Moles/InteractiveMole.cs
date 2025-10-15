using System.Collections;
using UnityEngine;


public class InteractiveMole : Mole
{
    [Header("Animation")]
    [Tooltip("If provided, Animation component will be used to play idle/pop animations. Validation-specific mappings (below) will override these defaults when present.")]
    [SerializeField] private Animation animationPlayer;

    [Tooltip("Name of the Idle clip inside the Animation component's clip list.")]
    [SerializeField] private string idleClipName = "Idle";

    [Header("Check if you want the mole to use an idle animation, \n make sure the idle animation clip name \n matches the string provided.")]
    [Tooltip("Enable to play the idle animation clip specified above when the mole is enabled.")]
    [SerializeField] private bool useIdleAnimation = false; // False by default, to avoid errors when idle animation is not assigned.

    [Header("Pop animation clip names - editable per-prefab (used as FALLBACK)")]
    [Tooltip("Name of the generic pop animation (used when performance feedback disabled). This is used as a fallback \n if no validation-specific mapping exists.")]
    [SerializeField] private string popClipName = "Pop";
    [Tooltip("Name of the pop animation played when correct mole is popped. This is used as a fallback if no validation-specific mapping exists.")]
    [SerializeField] private string popCorrectClipName = "PopCorrectMole";
    [Tooltip("Name of the pop animation played when wrong/distractor mole is popped. This is used as a fallback if no validation-specific mapping exists.")]
    [SerializeField] private string popWrongClipName = "PopWrongMole";

    [System.Serializable]
    private class ValidationAnimation
    {
        [Tooltip("Validation key string to match (case-insensitive)")]
        public string validationKey;
        [Tooltip("Clip to play for generic pop when mapped")]
        public string popClip;
        [Tooltip("Clip to play for correct pop when mapped (optional)")]
        public string popCorrectClip;
        [Tooltip("Clip to play for wrong pop when mapped (optional)")]
        public string popWrongClip;
    }

    [Header("Validation-specific animations (optional)")]
    [Tooltip("List of validation string -> animation clip mappings. Mapped clips are preferred; if a mapping is missing or the named clip is not present, the fallback clips above will be used.")]
    [SerializeField] private ValidationAnimation[] validationAnimations;

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


    private string playingClip = "";

    private bool hoverInfoShouldBeShown = false;

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
        yield return base.PlayEnabling();
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

    // Helper: find mapping for a validation key (case-insensitive)
    private ValidationAnimation FindValidationAnimation(string key)
    {
        if (string.IsNullOrEmpty(key) || validationAnimations == null) return null;
        foreach (ValidationAnimation v in validationAnimations)
        {
            if (string.IsNullOrEmpty(v.validationKey)) continue;
            if (string.Equals(v.validationKey, key, System.StringComparison.CurrentCultureIgnoreCase))
                return v;
        }
        return null;
    }

    // Returns the best clip name to play for a pop given current validation arg, performance feedback and correctness.
    private string GetMappedPopClip(bool performanceFeedback, bool isCorrect)
    {
        // prefer prefab fallbacks unless a specific validation clip is explicitly provided
        ValidationAnimation map = FindValidationAnimation(GetValidationArg());

        if (animationPlayer == null) return null;

        if (!performanceFeedback)
        {
            // Generic pop: use validation-specific popClip only if provided; otherwise use prefab fallback
            if (map != null && !string.IsNullOrEmpty(map.popClip) && HasAnimationClip(map.popClip)) return map.popClip;
            if (HasAnimationClip(popClipName)) return popClipName;
            return null;
        }

        // Performance feedback mode
        if (isCorrect)
        {
            // Prefer validation-specific correct clip if explicitly provided, else prefab correct clip, else generic prefab
            if (map != null && !string.IsNullOrEmpty(map.popCorrectClip) && HasAnimationClip(map.popCorrectClip)) return map.popCorrectClip;
            if (HasAnimationClip(popCorrectClipName)) return popCorrectClipName;
            if (HasAnimationClip(popClipName)) return popClipName;
            return null;
        }
        else
        {
            // Wrong case: prefer validation-specific wrong clip if provided, else prefab wrong clip, else generic prefab
            if (map != null && !string.IsNullOrEmpty(map.popWrongClip) && HasAnimationClip(map.popWrongClip)) return map.popWrongClip;
            if (HasAnimationClip(popWrongClipName)) return popWrongClipName;
            if (HasAnimationClip(popClipName)) return popClipName;
            return null;
        }
    }

    protected override IEnumerator PlayPopping()
    {
        showHoverInfo(false);
        StopIdleVisuals();

        if (animationPlayer != null)
        {
            string clipToPlay = GetMappedPopClip(ShouldPerformanceFeedback(), moleOutcome == MoleOutcome.Valid);
            if (!string.IsNullOrEmpty(clipToPlay))
            {
                PlayAnimation(clipToPlay);
            }

            float duration = GetAnimationDuration();
            yield return new WaitForSeconds(Mathf.Max(0.05f, duration));
        }

        yield return base.PlayPopping();
    }

    protected override IEnumerator PlayDisabling()
    {
        showHoverInfo(false);
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
        showHoverInfo(false);
        StopIdleVisuals();

        if (animationPlayer != null && ShouldPerformanceFeedback())
        {
            // For missed/expired, prefer prefab wrong clip (do not fallback to validation generic unless explicitly provided)
            if (HasAnimationClip(popWrongClipName)) PlayAnimation(popWrongClipName);
            else if (HasAnimationClip(popClipName)) PlayAnimation(popClipName);
        }

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
