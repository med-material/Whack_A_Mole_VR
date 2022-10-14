using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class FadingHelper : MonoBehaviour
{
    [Header("Game Object To Fade:")]
    [SerializeField]
    [Tooltip("Drag and drop any text, images, sprites, canvas groups, 2D/3D objects to fade")]
    private GameObject objToFade;

    [SerializeField]
    [Tooltip("Choose if you want the object to fade in, fade out")]
    private FadeAction fadeAction = FadeAction.In;

    [Header("Speed and Delay")]
    [SerializeField]
    [Tooltip("Time needed to fade the object")]
    private float fadeTime = 1f;

    [SerializeField]
    [Tooltip("Delay before or between the fading")]
    private float fadeDelay = 0f;

    [Header("GameObject behaviour")]
    [SerializeField]
    [Tooltip("Should this object be enabled/disabled when fading in/out")]
    private SetEnable setEnable = SetEnable.Off;

    [SerializeField]
    [Tooltip("Should this object fades in/out automatically when enabled/disabled")]
    private AutoPlay autoPlay = AutoPlay.Off;

    // Fades thanks to parameters from the inspector and using FadingUtils function
    public void Fade()
    {
        FadingUtils.FadeRoutine(this, objToFade, fadeTime, fadeAction, fadeDelay, setEnable);
    }

    void OnEnable()
    {
        if (autoPlay==AutoPlay.On && fadeAction==FadeAction.In && objToFade!=null) // plays fading in effect when object is enabled
        {
            FadingUtils.FadeRoutine(this, objToFade, fadeTime, fadeAction, fadeDelay, setEnable);
        }

        if (autoPlay==AutoPlay.On && fadeAction==FadeAction.Out && objToFade!=null) // plays fading in effect when object is enabled
        {
            FadingUtils.FadeRoutine(this, objToFade, fadeTime, fadeAction, fadeDelay, setEnable);
        }
    }
}