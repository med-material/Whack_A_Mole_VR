using UnityEngine;

/// <summary>
/// Simple controller for splash effects. Subscribes to InteractiveMole's pop event
/// and plays its own animation when triggered.
/// </summary>
public class PopEffectController : MonoBehaviour
{
    [Tooltip("Reference to the Animator that plays the splash sprite animation")]
    [SerializeField] private Animator splashAnimator;

    [Tooltip("Name of the animation state to play")]
    [SerializeField] private string splashStateName = "SmackAnimation";

    [Tooltip("Optionally use a ParticleSystem instead of Animator")]
    [SerializeField] private ParticleSystem splashParticleSystem;

    [Header("Validation-Specific Splash (Optional)")]
    [Tooltip("Enable to use different splash effects per validation type")]
    [SerializeField] private bool useValidationSpecificSplash = false;

    [System.Serializable]
    public class ValidationSplash
    {
        [Tooltip("Validation key to match (e.g., 'WristExtension', 'WristFlexion', 'PalmarGrasp')")]
        public string validationKey;
        [Tooltip("Animation state name for this validation type")]
        public string animationStateName;
        [Tooltip("Particle color override (optional)")]
        public Color particleColor = Color.white;
    }

    [Tooltip("Validation-specific splash configurations")]
    [SerializeField] private ValidationSplash[] validationSplashes;

    private void Awake()
    {
        // Disable animator so it doesn't auto-play
        if (splashAnimator != null)
        {
            splashAnimator.enabled = false;
        }

        // Stop particle system if present
        if (splashParticleSystem != null)
        {
            splashParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        
    }

    /// <summary>
    /// Called by UnityEvent when mole pops (no validation awareness).
    /// Add this method to the InteractiveMole's "On Mole Pop Event" in the Inspector.
    /// </summary>
    public void PlayPopEffect()
    {
        PlayPopInternal(splashStateName, Color.white);
    }

    /// <summary>
    /// Called by UnityEvent when mole pops WITH validation string.
    /// Add this method to the InteractiveMole's "On Mole Pop With Validation Event" in the Inspector.
    /// </summary>
    /// <param name="validationArg">Validation string from InteractiveMole</param>
    public void PlayPopEffectsWithValidation(string validationArg)
    {
        if (useValidationSpecificSplash && validationSplashes != null)
        {
            // Find matching validation splash
            foreach (ValidationSplash vs in validationSplashes)
            {
                if (string.Equals(vs.validationKey, validationArg, System.StringComparison.CurrentCultureIgnoreCase))
                {
                    PlayPopInternal(vs.animationStateName, vs.particleColor);
                    return;
                }
            }
        }

        // Fallback to default splash
        PlayPopInternal(splashStateName, Color.white);
    }

    private void PlayPopInternal(string animStateName, Color particleColor)
    {
        if (splashAnimator != null)
        {
            gameObject.SetActive(true);
            splashAnimator.enabled = true;
            splashAnimator.Play(animStateName, 0, 0f);

            // Auto-disable after animation completes
            float animLength = GetAnimationLength(animStateName);
            if (animLength > 0)
            {
                Invoke(nameof(DisableSplash), animLength);
            }
        }

        if (splashParticleSystem != null)
        {
            ParticleSystem.MainModule main = splashParticleSystem.main;
            main.startColor = particleColor;
            splashParticleSystem.Play();
        }
    }

    private void DisableSplash()
    {
        if (splashAnimator != null)
        {
            splashAnimator.enabled = false;
            gameObject.SetActive(false);
        }
    }

    private float GetAnimationLength(string stateName)
    {
        if (splashAnimator == null) return 0f;

        AnimatorClipInfo[] clipInfo = splashAnimator.GetCurrentAnimatorClipInfo(0);
        if (clipInfo.Length > 0)
        {
            return clipInfo[0].clip.length;
        }

        // Fallback: try to get from controller
        RuntimeAnimatorController ac = splashAnimator.runtimeAnimatorController;
        if (ac != null)
        {
            foreach (AnimationClip clip in ac.animationClips)
            {
                if (clip.name == stateName)
                {
                    return clip.length;
                }
            }
        }

        return 0.5f; // Default fallback
    }

    private void OnDestroy()
    {
        CancelInvoke();
    }
}
