using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;


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
    //This is Ver. 1 of this script, currently untested.
    // Todo: Confirm gesture changes are reflected in scene at runtime.
    // Todo: Integrate with EMG input system to trigger SetPose based on classified gestures.


    private SteamVR_Skeleton_Poser poser; // Reference to the SteamVR_Skeleton_Poser component
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
            SetPose(HandGestureState.PalmUp);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SetPose(HandGestureState.PalmDown);
        }
    }

    public void SetPose(HandGestureState gestureState)
    {
        string target = gestureState.ToString(); // Get the target behavior name based on the gesture state

        float from = 0f; // Current behavior value (assumed to be 0 for simplicity)
        float to = 1f; // Set target behavior to 1
        if (runningCoroutines.TryGetValue(target, out var running) && running != null)
        {
            StopCoroutine(running); // Stop any running coroutine for this behavior
        }

        runningCoroutines[target] = StartCoroutine(BlendedBehaviour(target, from, to, 0.3f)); // Start a new coroutine to blend the behavior


    }

    private IEnumerator BlendedBehaviour(string name, float from, float to, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            if (poser != null)
            {
                poser.SetBlendingBehaviourValue(name, Mathf.Lerp(from, to, time / duration)); // Lerp the behavior value over time
            }
            else
            {
                Debug.LogWarning("Poser is null while trying to set blending behavior value.");
            }

            yield return null; // Wait for the next frame
        }
        Debug.Log("Poser is null: " + (poser == null));
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

        Debug.Log("SteamVR_Skeleton_Poser component found and reference grabbed.");
    }

}
