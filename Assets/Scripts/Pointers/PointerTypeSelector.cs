using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valve.VR;

/*
PointerTypeSelector manages which Pointer scripts should be enabled/disabled on the gameObject.
*/
public class PointerTypeSelector : MonoBehaviour
{
    private Dictionary<ModifiersManager.PointerType, Pointer> pointerList = new Dictionary<ModifiersManager.PointerType, Pointer>();
    private SteamVR_Behaviour_Pose behaviour_Pose;

    void Start()
    {
        behaviour_Pose = GetComponent<SteamVR_Behaviour_Pose>();
        Pointer[] pointers = GetComponents<Pointer>();

        // Populate the pointerList dictionary with pointers found in the GameObject
        foreach (Pointer pointer in pointers)
        {
            if (Enum.TryParse(pointer.GetType().Name, out ModifiersManager.PointerType pointerType))
            {
                pointerList[pointerType] = pointer;
            }
            else
            {
                Debug.LogError($"Pointer type {pointer.GetType().Name} is not defined in PointerType enum.");
            }
        }

        // Activate the default pointer type at the start
        ActivatePointer(ModifiersManager.defaultPointerType);
    }

    public void ActivatePointer(ModifiersManager.PointerType pointerType)
    {
        // Disable all pointers before activating the selected one
        pointerList.Values.ToList().ForEach(pointer => { pointer.enabled = false; pointer.Disable(); });

        // Enable the selected pointer
        if (pointerList.TryGetValue(pointerType, out Pointer pointer))
        {
            pointer.enabled = true;
            pointer.Enable();
            behaviour_Pose.inputSource = pointer.Controller; // Change the tracking device for the pointer
        }
        else
        {
            Debug.LogError($"Pointer type {pointerType} not found on {this.name}.");
        }
    }

    public Pointer GetActivePointer()
    {
        // Return the currently active pointer
        return pointerList.Values.FirstOrDefault(pointer => pointer.enabled);
    }
}
