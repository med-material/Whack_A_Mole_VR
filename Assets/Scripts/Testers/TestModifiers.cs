using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestModifiers : MonoBehaviour
{
    private ModifiersManager modifier;

    private ModifiersManager.EyePatch eyePatch = ModifiersManager.EyePatch.Left;
    private ModifiersManager.ControllerSetup rightMain = ModifiersManager.ControllerSetup.Right;
    private bool dualTask = false;
    private float controllerOffset = -15f;
    private bool mirrorEffect = false;

    void Start()
    {
        modifier = gameObject.GetComponent<ModifiersManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Jump"))
        {
            if (eyePatch == ModifiersManager.EyePatch.Left) eyePatch = ModifiersManager.EyePatch.None;
            else if (eyePatch == ModifiersManager.EyePatch.None) eyePatch = ModifiersManager.EyePatch.Right;
            else if (eyePatch == ModifiersManager.EyePatch.Right) eyePatch = ModifiersManager.EyePatch.Left;

            Debug.Log("EyePatch : " + eyePatch);
            modifier.SetEyePatch(eyePatch);
        }
        if (Input.GetButtonDown("Fire1"))
        {
            rightMain = ModifiersManager.ControllerSetup.Right;
            Debug.Log("SetMainController : " + rightMain);
            modifier.SetMainController(rightMain);
        }
        if (Input.GetButtonDown("Submit"))
        {
            controllerOffset += 15f;
            Debug.Log("Offset : " + controllerOffset);
            modifier.SetControllerOffset(controllerOffset);
        }
        if (Input.GetButtonDown("Cancel"))
        {
            mirrorEffect = !mirrorEffect;
            Debug.Log("Mirror : " + mirrorEffect);
            modifier.SetMirrorEffect(mirrorEffect);
        }
    }
}
