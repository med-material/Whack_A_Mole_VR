using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;


public enum HandGestureState
{
    Neutral,
    Fist,
    OpenHand,
    PalmUpward,
    PalmDownward
}

public class EMGClassifiedGestureManager : MonoBehaviour
{
    //This is Ver. 1 of this script, currently untested.
    // Todo: Confirm gesture changes are reflected in scene at runtime.
    // Todo: Integrate with EMG input system to trigger SetPose based on classified gestures.


    private SteamVR_Skeleton_Poser poser; // Reference to the SteamVR_Skeleton_Poser component
    private string[] behaviorNames = { "Neutral", "Fist", "OpenHand", "PalmUp", "PalmDown" }; // Ensure these names match the behaviors in the SteamVR_Skeleton_Poser
    private Dictionary<string, Coroutine> runningCoroutines = new(); // To keep track of running coroutines for each behavior
    private void Awake()
    {
        StartCoroutine(WaitForHandToBeInstantiatedAndGrabPoserReference()); // Start the coroutine to wait for the poser reference

    }

    private void Start()
    {
        SetPose(HandGestureState.Neutral); // Set initial pose to Neutral
    }


    private void Update()
    {
        // For testing purposes, you can change the gesture state using keyboard input
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
            SetPose(HandGestureState.PalmUpward);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SetPose(HandGestureState.PalmDownward);
        }
    }

    public void SetPose(HandGestureState gestureState)
    {
        string target = behaviorNames[(int)gestureState]; // Get the target behavior name based on the gesture state

        foreach (string name in behaviorNames) // Iterate through all behavior names
        {
            float from = 0f; // Current behavior value (assumed to be 0 for simplicity)
            float to = (name == target) ? 1f : 0f; // Set target behavior to 1, others to 0

            if (runningCoroutines.TryGetValue(name, out var running) && running != null)
            {

                StopCoroutine(running); // Stop any running coroutine for this behavior
                
            }
            runningCoroutines[name] = StartCoroutine(BlendedBehaviour(name, from, to, 0.3f)); // Start a new coroutine to blend the behavior
        }
    }

    private IEnumerator BlendedBehaviour(string name, float from, float to, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            poser.SetBlendingBehaviourValue(name, Mathf.Lerp(from, to, time / duration)); // Lerp the behavior value over time
            yield return null; // Wait for the next frame
        }
        poser.SetBlendingBehaviourValue(name, to); // Ensure the final value is set
        runningCoroutines.Remove(name); // Remove the coroutine from the tracking dictionary

        runningCoroutines[name] = null; // Clear the reference to the completed coroutine
    }




    private IEnumerator WaitForHandToBeInstantiatedAndGrabPoserReference()
    {
        // Wait until the SteamVR_Skeleton_Poser component is available
        while (poser == null)
        {
            poser = GetComponent<SteamVR_Skeleton_Poser>();
            yield return null; // Wait for the next frame
        }
    }

}
