﻿using System;
using UnityEngine;

public class PointerTrailHandler : MonoBehaviour
{
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


    private void Awake()
    {
        trailRenderer = GetComponent<TrailRenderer>();
        spriteRenderer = GetComponentInParent<SpriteRenderer>();
        if (trailRenderer == null)
            Debug.LogError("Missing TrailRenderer component!");
        if (spriteRenderer == null)
            Debug.LogError("Missing SpriteRenderer component in parent!");
        if (performanceManager == null)
            Debug.LogError("Performance Manager not assigned in inspector!");
   

        trailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        if (isVisible)
        {
            soundManager.PlaySoundLooped(gameObject, trailSound);
            soundManager.ChangeVolume(trailSound, 0f);  // Set initial volume to 0
        }

    }

    private void Update()
    {
        speed = performanceManager.GetInstantSpeed();

        Debug.Log("Speed: " + speed);

        UpdateRuntimeVisibility();

        // Si la visibilité finale est vraie, alors mettre à jour les propriétés de la traînée
        // en: If the final visibility is true, then update the trail properties
        if (IsTrulyVisible())
        {
            UpdateTrailProperties();
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
            float targetDiameter = spriteRenderer.bounds.size.x; // assuming the sprite is a circle

            float normalizedSpeed;
            if (speed < 11)
            {
                // Normalize speed in the range 0 to 11 to the range 0 to 0.5
                normalizedSpeed = Mathf.Lerp(0f, 0.5f, speed / 11);
            }
            else
            {
                // Normalize speed in the range 11 to 19 to the range 0.5 to 1
                // Clamp to make sure speed does not exceed 19
                normalizedSpeed = Mathf.Lerp(0.5f, 1f, (Mathf.Clamp(speed, 11f, 19f) - 11) / (19 - 11));
            }

            if (speed <= 0)
            {
                // If speed is zero, set a default small trail
                float defaultWidth = targetDiameter / 10;
                float defaultOpacity = 0.5f;

                trailRenderer.time = defaultWidth;
                trailRenderer.startWidth = defaultWidth;
                trailRenderer.endWidth = 0f;
                trailRenderer.startColor = new Color(trailColor.r, trailColor.g, trailColor.b, defaultOpacity);
                trailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
                soundManager.ChangeVolume(trailSound, 0f);
            }
            else
            {
                float targetVolume = (speed < 1f) ? 0f : Mathf.Lerp(0f, 1f, normalizedSpeed);  // Adjust volume based on speed. This will mute the sound for speeds < 1, and adjust between 0 and 1 for speeds between 1 and max.
                soundManager.ChangeVolume(trailSound, targetVolume);

                float targetPitch = Mathf.Lerp(minTargetPitch, maxTargetPitch, normalizedSpeed);
                // Change the pitch of the sound based on the speed, smoothly transition to the target pitch over time
                currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * 1f); // the second parameter controls the speed of transition, adjust as needed
                soundManager.ChangePitch(trailSound, currentPitch);


                float currentLength = Mathf.Min(Mathf.Lerp(minTrailLength, maxTrailLength, normalizedSpeed), maxTrailLength);
                float currentWidth = Mathf.Lerp(0, targetDiameter, normalizedSpeed);
                Color currentColor = new Color(trailColor.r, trailColor.g, trailColor.b, Mathf.Lerp(0.5f, 1f, normalizedSpeed));

                trailRenderer.time = currentLength;
                trailRenderer.startWidth = currentWidth;
                trailRenderer.endWidth = 0f;
                trailRenderer.startColor = currentColor;
                trailRenderer.endColor = new Color(currentColor.r, currentColor.g, currentColor.b, 0f);
            }
        }
    }


    



    private void UpdateRuntimeVisibility()
    {
        bool currentRuntimeVisibility;

        // If the speed is less than 1f, set the runtime visibility to false
        if (speed < 1f)
        {
            currentRuntimeVisibility = false;
            soundManager.ChangeVolume(trailSound, 0f);
        }
        // If the speed is greater than or equal to 0.05f, set the runtime visibility to true
        else
        {
            currentRuntimeVisibility = true;
        }

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
        bool finalVisibility = IsTrulyVisible();

        trailRenderer.enabled = finalVisibility;

        //if (!finalVisibility)
        //{
        //    soundManager.StopSound(trailSound);
        //}
        //else
        //{
        //    soundManager.PlaySoundLooped(gameObject, trailSound);
        //}
    }


}
