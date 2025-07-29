using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valve.VR;

public class PointerTypeSelector : MonoBehaviour
{
    [SerializeField] PointerType defaultPointerType = PointerType.BasicPointer;
    private Dictionary<PointerType, Pointer> pointerList = new Dictionary<PointerType, Pointer>();
    private SteamVR_Behaviour_Pose behaviour_Pose;

    void Start()
    {
        behaviour_Pose = GetComponent<SteamVR_Behaviour_Pose>();
        Pointer[] pointers = GetComponents<Pointer>();

        // Populate the pointerList dictionary with pointers found in the GameObject
        foreach (Pointer pointer in pointers)
        {
            if (Enum.TryParse(pointer.GetType().Name, out PointerType pointerType))
            {
                pointerList[pointerType] = pointer;
            }
            else
            {
                Debug.LogError($"Pointer type {pointer.GetType().Name} is not defined in PointerType enum.");
            }
        }

        // Activate the default pointer type at the start
        ActivatePointer(defaultPointerType);
    }

    public void ActivatePointer(PointerType pointerType)
    {
        // Disable all pointers before activating the selected one
        pointerList.Values.ToList().ForEach(pointer => pointer.enabled = false);

        // Enable the selected pointer
        if (pointerList.TryGetValue(pointerType, out Pointer pointer))
        {
            pointer.enabled = true;
            behaviour_Pose.inputSource = pointer.Controller; // Change the tracking device for the pointer
        }
        else
        {
            Debug.LogError($"Pointer type {pointerType} not found.");
        }
    }
}


// Important: Ensure to update the PointerType enum when new pointer type script is added /!\
public enum PointerType
{
    BasicPointer,
    EMGPointer
}
