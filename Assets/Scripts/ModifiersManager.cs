﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

/*
Manages different VR modifiers, which are setting the main hand, dual-task mode, eye-patch, mirror mode and prism offset.

WARNING: Due to the Vive overlay, it is necessary to disable the chaperone bounds in the Vive settings, otherwise the eye patch would still render the chaperone if near a boundary.
To do so, go the the VR settings WHILE INSIDE THE HEADSET -> Chaperone -> select DEVELOPER MODE and set a neutral color with the lowest opacity possible.
It is also possible to fully hide the chaperone by editing the steamvr.vrsettings file and setting "CollisionBoundsColorGammaA" to 0.
*/


public class ModifiersManager : MonoBehaviour
{
    public enum EyePatch {Left, None, Right};

    [SerializeField]
    private Pointer rightController;

    [SerializeField]
    private Pointer leftController;

    [SerializeField]
    private Transform rightControllerContainer;

    [SerializeField]
    private Transform leftControllerContainer;

    [SerializeField]
    private Camera viveCamera;

    [SerializeField]
    private Transform wallReference;

    private EyePatch eyePatch;
    private bool mirrorEffect;
    private bool dualTask;
    private bool rightControllerMain;
    private float prismEffect;
    private Dictionary<string, Pointer> controllersList;

    void Start()
    {
        controllersList = new Dictionary<string, Pointer>(){
            {"main", null},
            {"second", null}
        };
        SetMainController(true);
    }

    // Sets an eye patch. Calls WaitForCameraAndUpdate coroutine to set eye patch.
    public void SetEyePatch(EyePatch value)
    {
        if (eyePatch == value) return;
        eyePatch = value;
        StartCoroutine(WaitForCameraAndUpdate(eyePatch));
    }

    // Sets a controller position and rotation's mirroring effect. Calls UpdateMirrorEffect to set the mirror.
    public void SetMirrorEffect(bool value)
    {
        if (mirrorEffect == value) return;
        if (!controllersList["main"].isActiveAndEnabled) return;

        mirrorEffect = value;
        UpdateMirrorEffect();
    }

    // Sets the dual task mode (if dualtask is enabled, both controllers can be used to pop moles)
    public void SetDualTask(bool value)
    {
        if (dualTask == value) return;
        dualTask = value;
        SetControllerEnabled("second", dualTask);

        if (mirrorEffect)
        {
            UpdateMirrorEffect();
        }
    }

    // Sets the prism effect. Shifts the view (around y axis) by a given angle to create a shifting between seen view and real positions.
    public void SetPrismEffect(float value)
    {
        prismEffect = value;
        rightControllerContainer.localEulerAngles = new Vector3(0, prismEffect, 0);
        leftControllerContainer.localEulerAngles = new Vector3(0, prismEffect, 0);
    }

    // Sets the main controller. By default it is the right handed one.
    public void SetMainController(bool rightIsMain)
    {
        if (rightControllerMain == rightIsMain) return;

        rightControllerMain = rightIsMain;
        if (rightControllerMain)
        {
            controllersList["main"] = rightController;
            controllersList["second"] = leftController;
        }
        else
        {
            controllersList["main"] = leftController;
            controllersList["second"] = rightController;
        }
        SetControllerEnabled("main");

        if (mirrorEffect)
        {
            UpdateMirrorEffect();
        }

        if (!dualTask) SetControllerEnabled("second", false);
    }

    // Updates the mirroring effect. Is called when enabling/disabling the mirror effect or when controllers are activated/deactivated (dual task, main controller change).
    private void UpdateMirrorEffect()
    {
        if (mirrorEffect)
        {
            controllersList["main"].gameObject.GetComponent<ControllerModifierManager>().EnableMirror(viveCamera.transform, wallReference);

            if (!dualTask) 
            {
                controllersList["second"].gameObject.GetComponent<ControllerModifierManager>().DisableMirror();
            }
            else
            {
                controllersList["second"].gameObject.GetComponent<ControllerModifierManager>().EnableMirror(viveCamera.transform, wallReference);
            }
        }
        else
        {
            controllersList["main"].gameObject.GetComponent<ControllerModifierManager>().DisableMirror();

            if (dualTask)
            {
                controllersList["second"].gameObject.GetComponent<ControllerModifierManager>().DisableMirror();
            }
        }
    }

    // Enables/disables a given controller
    private void SetControllerEnabled(string controllerType, bool enable = true)
    {
        if (enable)
        {
            controllersList[controllerType].Enable();
        }
        else
        {
            controllersList[controllerType].Disable();
        }
    }

    // Sets the eye patch. Forces the camera to render a black screen for a short duration and disables an eye while the screen is black.
    // If the image wasn't forced black we would have a frozen image of the game in the disabled eye.

    /*
    WARNING: Due to the Vive overlay, it is necessary to disable the chaperone bounds in the Vive settings, otherwise the eye patch would still render the chaperone if near a boundary.
    To do so, go the the VR settings WHILE INSIDE THE HEADSET -> Chaperone -> select DEVELOPER MODE and set a neutral color with the lowest opacity possible.
    It is also possible to fully hide the chaperone by editing the steamvr.vrsettings file and setting "CollisionBoundsColorGammaA" to 0.
    */
    private IEnumerator WaitForCameraAndUpdate(EyePatch value)
    {
        viveCamera.farClipPlane = 0.02f;
        viveCamera.clearFlags = CameraClearFlags.SolidColor;
        viveCamera.backgroundColor = Color.black;

        yield return new WaitForSeconds(0.05f);

        viveCamera.farClipPlane = 1000f;
        viveCamera.clearFlags = CameraClearFlags.Skybox;

        if (value == EyePatch.Right)
        {
            viveCamera.stereoTargetEye = StereoTargetEyeMask.Left;
        }
        else if (value == EyePatch.None)
        {
            viveCamera.stereoTargetEye = StereoTargetEyeMask.Both;
        }
        else if (value == EyePatch.Left)
        {
            viveCamera.stereoTargetEye = StereoTargetEyeMask.Right;
        }
    }
}
