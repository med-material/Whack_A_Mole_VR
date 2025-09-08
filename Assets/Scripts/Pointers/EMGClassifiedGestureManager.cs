using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valve.VR;


//Enum for hand gesture states
//Each value should correspond exactly (including naming and casing) to the Manual Blending behavior names created in
//SteamVR's Blending Editor
public enum HandGestureState
{
    Neutral,
    Fist,
    OpenHand,
    PalmUp,
    PalmDown
}

public class EMGClassifiedGestureManager : MonoBehaviour
{
    //This is Ver. 2 of this script.
    // Todo: Confirm gesture changes are reflected in scene at runtime. STILL ONGOING
    // Todo: Integrate with EMG input system to trigger SetPose based on classified gestures.


    private SteamVR_Skeleton_Poser poser; // Reference to the SteamVR_Skeleton_Poser component, drives pose blending at runtime
    private Coroutine currentBlendCoroutine; //Tracks currently running blending coroutine, later used for smooth transitioning
    private void Awake()
    {
        StartCoroutine(WaitForHandToBeInstantiatedAndGrabPoserReference()); // Start the coroutine to wait for the hand model (with SteamVR_Skeleton_Poser) to spawn, grabs reference once available.

    }

  

    private void Update()
    {

        //If for some reason it failed to get the poser in Awake (late instantiation)
        //continuously try to locate it until found

        if (poser == null)
        {
            poser = GetComponentInChildren<SteamVR_Skeleton_Poser>(true);
            if (poser != null)
            {
                Debug.Log("Late-caught SteamVR_Skeleton_Poser in Update.");
                SetPose(HandGestureState.Neutral); //Initialize to a default pose
            }
        }


        //For testing purposes, you can change the gesture state using keyboard input at runtime
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetPose(HandGestureState.Neutral);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetPose(HandGestureState.Fist);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetPose(HandGestureState.OpenHand);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SetPose(HandGestureState.PalmUp);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SetPose(HandGestureState.PalmDown);
        }
    }

    //Triggers a smooth transition to the specified pose
    public void SetPose(HandGestureState gestureState)
    {

        if (poser == null)
        {
            Debug.LogWarning($"Attempted to set pose {gestureState}, but poser is not initialized yet.");
            return;
        }
        string target = gestureState.ToString(); //Get the target behavior name based on the gesture state. Must match Blending Editor names exactly.

        if (currentBlendCoroutine != null)//If a previous blend is already ongoing, stops it to avoid overlapping blends
        {
            StopCoroutine(currentBlendCoroutine); //Stop any ongoing blend coroutine
        }

        currentBlendCoroutine = StartCoroutine(CrossFadePose(target, 0.3f)); //Start a new blend coroutine to transition to the target pose over 0.3 seconds



    }


    //Coroutine to smoothly transition between poses over a specified duration
    private IEnumerator CrossFadePose(string targetPose, float duration)
    {
        var behaviors = new[] { "Neutral", "Fist", "OpenHand", "PalmUp", "PalmDown" }; //List of all blending behaviors to manage
        var startValues = behaviors.ToDictionary(b => b, b => poser.GetBlendingBehaviourValue(b)); //Capture the starting values of all behaviors

        float time = 0f; //Elapsed time tracker
        while (time < duration) //Loop until the duration is reached
        {
            time += Time.deltaTime;
            float t = time / duration; //Normalized time (0 to 1)
            foreach (var behavior in behaviors) //Iterate through each behavior to update its value
            {
                float targetValue = (behavior == targetPose) ? 1f : 0f; //Target value is 1 for the target pose, 0 for others
                float val = Mathf.Lerp(startValues[behavior], targetValue, t); //Linearly interpolate between start and target values
                poser.SetBlendingBehaviourValue(behavior, val); //Apply the interpolated value
            }
            yield return null; // Wait for the next frame
        }

        // Ensure every behavior hits its intended end value
        foreach (string b in behaviors) //Final adjustment to ensure exact values
        {
            poser.SetBlendingBehaviourValue(b, (b == targetPose) ? 1f : 0f); //Set target pose to 1, others to 0
        }
    }





    //Coroutine to wait until the SteamVR_Skeleton_Poser component is available in the instantiated hand model
    private IEnumerator WaitForHandToBeInstantiatedAndGrabPoserReference()
    {
        //Wait until the SteamVR_Skeleton_Poser component is available
        while (poser == null) // Keep checking until we find the poser
        {
            poser = GetComponentInChildren<SteamVR_Skeleton_Poser>(); //Try to get the poser component from children


            yield return null; // Wait for the next frame
        }


        Debug.Log("SteamVR_Skeleton_Poser component found and reference grabbed.");
        SetPose(HandGestureState.Neutral); // Set initial pose to Neutral

    }

}
