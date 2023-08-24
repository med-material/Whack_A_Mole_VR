using System;
using UnityEngine;

public class PointerTrailHandler : MonoBehaviour
{

    [SerializeField]
    private Pointer controllerParent;

    [SerializeField]
    private PerformanceManager performanceManager;
    [SerializeField]
    private Color trailColor = new Color(0.118f, 0.486f, 0.718f, 1f);  // this is now the field for trail color

    [SerializeField]
    private SoundManager soundManager;

    [SerializeField]
    private SoundManager.Sound trailSound;
    private float currentPitch = 0.5f;  // Initial pitch


    private SpriteRenderer spriteRenderer;
    private TrailRenderer trailRenderer;

    private float speed;
    [SerializeField, Range(0.1f, 2f)]
    private float maxTrailLength = 1.0f; // max trail length
    [SerializeField, Range(0.1f, 2f)]
    private float minTrailLength = 0.0f;

    [SerializeField, Range(0.1f, 2f)]
    private float minTargetPitch = 0.7f;

    [SerializeField, Range(1f, 4f)]
    private float maxTargetPitch = 1.3f;


    private bool isVisible = true;
    private bool configVisibility = true;
    private bool lastRuntimeVisibilityState;

    private void OnEnable()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        if (!trailRenderer)
        {
            trailRenderer = GetComponent<TrailRenderer>();
            if (trailRenderer == null)
                Debug.LogError("Missing TrailRenderer component!");
        }

        if (!spriteRenderer)
        {
            spriteRenderer = GetComponentInParent<SpriteRenderer>();
            if (spriteRenderer == null)
                Debug.LogError("Missing SpriteRenderer component in parent!");
        }

        trailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
    }



    private void Awake()
    {
        if (isVisible)
        {
            soundManager.PlaySoundLooped(gameObject, trailSound);
            soundManager.ChangeVolume(trailSound, 0f);  // Set initial volume to 0
        }
    }

    private void Update()
    {
        if (!trailRenderer || !spriteRenderer)
            return;

        ControllerName controllerName = controllerParent.GetControllerName();
        speed = performanceManager.GetInstantJudgement(controllerName);

        UpdateRuntimeVisibility();
        // If the final visibility is true, then update the trail properties
        if (IsTrulyVisible())
        {
            UpdateTrailProperties();
        }
        else
        {
            soundManager.ChangeVolume(trailSound, 0f);
        }
    }
    private void OnDestroy()
    {
        // Stop playing the sound when the script is destroyed
        soundManager.StopSound(trailSound);
    }


    private void UpdateTrailProperties()
    {
        if (trailRenderer != null)
        {
            float normalizedSpeed = speed;

            float targetDiameter = spriteRenderer.bounds.size.x;

            soundManager.ChangeVolume(trailSound, (speed > 0.1f) ? Mathf.Lerp(0f, 1, speed) : 0f);

            float targetPitch = Mathf.Lerp(minTargetPitch, maxTargetPitch, normalizedSpeed);
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * 1f);
            soundManager.ChangePitch(trailSound, currentPitch);

            float currentLength = Mathf.Min(Mathf.Lerp(minTrailLength, maxTrailLength, normalizedSpeed), maxTrailLength);
            float currentWidth = Mathf.Lerp(0, targetDiameter * 0.8f, normalizedSpeed);
            Color currentColor = new Color(trailColor.r, trailColor.g, trailColor.b, Mathf.Lerp(0.5f, 1f, normalizedSpeed));

            trailRenderer.time = currentLength;
            trailRenderer.startWidth = currentWidth;
            trailRenderer.endWidth = 0f;
            trailRenderer.startColor = currentColor;
            trailRenderer.endColor = new Color(currentColor.r, currentColor.g, currentColor.b, 0f);
        }
    }



    private float GetNonLinearSpeed()
    {
        return Mathf.Pow(speed, 0.3f);
    }







    private void UpdateRuntimeVisibility()
    {
        bool currentRuntimeVisibility;

        //// If the speed is less than 0.05f, set the runtime visibility to false
        //if (speed < 0.05f)
        //{
        //    currentRuntimeVisibility = false;
        //    soundManager.ChangeVolume(trailSound, 0f);
        //}
        // //If the speed is greater than or equal to 0.05f, set the runtime visibility to true
        //else
        currentRuntimeVisibility = true;
        //{
        //}

        // Only change the runtime visibility if it is different from the previous state
        if (currentRuntimeVisibility != lastRuntimeVisibilityState)
        {
            lastRuntimeVisibilityState = currentRuntimeVisibility;
            SetRuntimeVisibility(currentRuntimeVisibility);
        }
    }




    private bool IsTrulyVisible()
    {
        // final visibility is the combination of config visibility and runtime visibility
        return configVisibility && isVisible;
    }

    // Used to set the visibility of the trail from the modifiers manager
    internal void SetConfigVisibility(bool visibility)
    {
        configVisibility = visibility;
        UpdateVisibility();
    }

    // Used to set the visibility of the trail at runtime (e.g. when the speed is too low)
    internal void SetRuntimeVisibility(bool visibility)
    {
        isVisible = visibility;
        UpdateVisibility();
    }

    // Used to update the visibility of the trail based on the final visibility state
    private void UpdateVisibility()
    {
        if (!trailRenderer)
            return;

        bool finalVisibility = IsTrulyVisible();
        trailRenderer.enabled = finalVisibility;
    }
}