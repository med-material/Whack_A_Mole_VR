using UnityEngine;

/// <summary>
/// Handles event-driven animation playback for InteractiveMole using legacy Animation component.
/// Subscribe methods to InteractiveMole's UnityEvents in the Inspector.
/// Supports validation-specific animations for moles like Wasps that have different animation per validation type.
/// </summary>
public class InteractiveMoleAnimationController : MonoBehaviour
{
    [Header("Animation Component")]
    [Tooltip("Legacy Animation component holding all animation clips")]
    [SerializeField] private Animation animationComponent;

    [Header("Default Animation Clip Names")]
    [Tooltip("Default idle animation clip name (used when no validation-specific mapping exists)")]
    [SerializeField] private string idleClipName = "Idle";
    
    [Tooltip("Default spawn/enable animation clip name (used when no validation-specific mapping exists)")]
    [SerializeField] private string spawnClipName = "Spawn";
    
    [Tooltip("Default pop animation clip name (used when no validation-specific mapping exists)")]
    [SerializeField] private string popClipName = "Pop";

    [Header("Validation-Specific Animations (Optional)")]
    [Tooltip("Enable this to use validation-specific animation mappings (e.g., different animations per Gesture type)")]
    [SerializeField] private bool useValidationSpecificAnimations = false;

    [System.Serializable]
    public class ValidationAnimationMapping
    {
        [Tooltip("Validation key to match (e.g., 'WristExtension', 'WristFlexion', 'PalmarGrasp')")]
        public string validationKey;
        
        [Tooltip("Spawn animation clip name for this validation type")]
        public string spawnClip;
        
        [Tooltip("Idle animation clip name for this validation type")]
        public string idleClip;
        
        [Tooltip("Pop animation clip name for this validation type")]
        public string popClip;
    }

    [Tooltip("List of validation-specific animation mappings")]
    [SerializeField] private ValidationAnimationMapping[] validationMappings;

    [Header("Debug")]
    [Tooltip("Enable debug logging for animation playback")]
    [SerializeField] private bool debugMode = false;

    private string lastPlayedClip = "";

    private void Awake()
    {
        if (animationComponent == null)
        {
            animationComponent = GetComponent<Animation>();
        }

        if (animationComponent == null)
        {
            Debug.LogError($"InteractiveMoleAnimationController on '{gameObject.name}': No Animation component found!", this);
        }
    }

    /// <summary>
    /// Play idle animation. Hook this to InteractiveMole's onMoleIdleEvent.
    /// </summary>
    public void PlayIdleAnimation()
    {
        PlayAnimationInternal(idleClipName, "Idle");
    }

    /// <summary>
    /// Play idle animation with validation key. Hook this to InteractiveMole's onMoleIdleWithValidationEvent.
    /// </summary>
    public void PlayIdleAnimationWithValidation(string validationArg)
    {
        string clipName = GetValidationClip(validationArg, AnimationType.Idle);
        PlayAnimationInternal(clipName, $"Idle (Validation: {validationArg})");
    }

    /// <summary>
    /// Play spawn animation. Hook this to InteractiveMole's onMoleSpawnEvent.
    /// </summary>
    public void PlaySpawnAnimation()
    {
        PlayAnimationInternal(spawnClipName, "Spawn");
    }

    /// <summary>
    /// Play spawn animation with validation key. Hook this to InteractiveMole's onMoleSpawnWithValidationEvent.
    /// </summary>
    public void PlaySpawnAnimationWithValidation(string validationArg)
    {
        string clipName = GetValidationClip(validationArg, AnimationType.Spawn);
        PlayAnimationInternal(clipName, $"Spawn (Validation: {validationArg})");
    }

    /// <summary>
    /// Play pop animation. Hook this to InteractiveMole's onMolePopEvent.
    /// </summary>
    public void PlayPopAnimation()
    {
        PlayAnimationInternal(popClipName, "Pop");
    }

    /// <summary>
    /// Play pop animation with validation key. Hook this to InteractiveMole's onMolePopWithValidationEvent.
    /// </summary>
    public void PlayPopAnimationWithValidation(string validationArg)
    {
        string clipName = GetValidationClip(validationArg, AnimationType.Pop);
        PlayAnimationInternal(clipName, $"Pop (Validation: {validationArg})");
    }

    /// <summary>
    /// Internal method to play an animation clip
    /// </summary>
    private void PlayAnimationInternal(string clipName, string debugLabel)
    {
        if (animationComponent != null && HasClip(clipName))
        {
            if (debugMode) Debug.Log($"[{gameObject.name}] Playing {debugLabel} animation: {clipName}");
            
            // Stop all other animations first, then play the requested one
            animationComponent.Stop();
            animationComponent.Play(clipName);
            lastPlayedClip = clipName;
        }
        else if (debugMode)
        {
            Debug.LogWarning($"[{gameObject.name}] Cannot play {debugLabel} animation. Component: {animationComponent != null}, HasClip({clipName}): {HasClip(clipName)}");
        }
    }

    /// <summary>
    /// Get the appropriate animation clip name based on validation key and animation type.
    /// Falls back to default clips if no validation-specific mapping exists.
    /// </summary>
    private string GetValidationClip(string validationArg, AnimationType animType)
    {
        // If validation-specific animations are disabled, use defaults
        if (!useValidationSpecificAnimations)
        {
            return GetDefaultClip(animType);
        }

        // If no validation arg provided, use default
        if (string.IsNullOrEmpty(validationArg))
        {
            return GetDefaultClip(animType);
        }

        // Search for matching validation mapping
        if (validationMappings != null)
        {
            foreach (ValidationAnimationMapping mapping in validationMappings)
            {
                if (string.IsNullOrEmpty(mapping.validationKey)) continue;
                
                if (string.Equals(mapping.validationKey, validationArg, System.StringComparison.CurrentCultureIgnoreCase))
                {
                    string clipName = GetClipFromMapping(mapping, animType);
                    
                    // If mapping has a valid clip, use it; otherwise fall back to default
                    if (!string.IsNullOrEmpty(clipName) && HasClip(clipName))
                    {
                        return clipName;
                    }
                    break;
                }
            }
        }

        // Fall back to default clip if no mapping found or clip doesn't exist
        return GetDefaultClip(animType);
    }

    /// <summary>
    /// Get the clip name from a validation mapping based on animation type
    /// </summary>
    private string GetClipFromMapping(ValidationAnimationMapping mapping, AnimationType animType)
    {
        switch (animType)
        {
            case AnimationType.Spawn:
                return mapping.spawnClip;
            case AnimationType.Idle:
                return mapping.idleClip;
            case AnimationType.Pop:
                return mapping.popClip;
            default:
                return null;
        }
    }

    /// <summary>
    /// Get the default clip name based on animation type
    /// </summary>
    private string GetDefaultClip(AnimationType animType)
    {
        switch (animType)
        {
            case AnimationType.Spawn:
                return spawnClipName;
            case AnimationType.Idle:
                return idleClipName;
            case AnimationType.Pop:
                return popClipName;
            default:
                return null;
        }
    }

    /// <summary>
    /// Get the duration of the last played animation clip.
    /// Useful for waiting before transitioning to the next state.
    /// </summary>
    public float GetLastPlayedAnimationDuration()
    {
        if (animationComponent == null || string.IsNullOrEmpty(lastPlayedClip)) return 0f;
        AnimationClip clip = animationComponent.GetClip(lastPlayedClip);
        return clip != null ? clip.length : 0f;
    }

    /// <summary>
    /// Get the duration of a specific animation clip by name.
    /// </summary>
    public float GetAnimationDuration(string clipName)
    {
        if (animationComponent == null || string.IsNullOrEmpty(clipName)) return 0f;
        AnimationClip clip = animationComponent.GetClip(clipName);
        return clip != null ? clip.length : 0f;
    }

    /// <summary>
    /// Stop idle animation. Hook this to cleanup events if needed.
    /// </summary>
    public void StopIdleAnimation()
    {
        if (animationComponent != null && HasClip(idleClipName))
        {
            if (debugMode) Debug.Log($"[{gameObject.name}] Stopping Idle animation: {idleClipName}");
            animationComponent.Stop(idleClipName);
        }
    }

    /// <summary>
    /// Stop all animations. Useful for cleanup.
    /// </summary>
    public void StopAllAnimations()
    {
        if (animationComponent != null)
        {
            if (debugMode) Debug.Log($"[{gameObject.name}] Stopping all animations");
            animationComponent.Stop();
        }
    }

    private bool HasClip(string clipName)
    {
        return !string.IsNullOrEmpty(clipName) && animationComponent != null && animationComponent.GetClip(clipName) != null;
    }

    private enum AnimationType
    {
        Spawn,
        Idle,
        Pop
    }
}
