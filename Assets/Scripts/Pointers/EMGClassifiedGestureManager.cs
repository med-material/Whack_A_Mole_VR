using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    //This is Ver. 2 of this script.
    // Todo: Confirm gesture changes are reflected in scene at runtime. STILL ONGOING
    // Todo: Integrate with EMG input system to trigger SetPose based on classified gestures.


    private SteamVR_Skeleton_Poser poser; // Reference to the SteamVR_Skeleton_Poser component
    private Dictionary<string, Coroutine> runningCoroutines = new(); // To keep track of running coroutines for each behavior
    private Coroutine currentBlendCoroutine;
    private void Awake()
    {
        StartCoroutine(WaitForHandToBeInstantiatedAndGrabPoserReference()); // Start the coroutine to wait for the poser reference

    }

    private void Start()
    {
    }


    private void Update()
    {

        if (poser == null)
        {
            poser = GetComponentInChildren<SteamVR_Skeleton_Poser>(true);
            if (poser != null)
            {
                Debug.Log("Late-caught SteamVR_Skeleton_Poser in Update.");
                SetPose(HandGestureState.Neutral);
            }
        }


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

        if (poser == null)
        {
            Debug.LogWarning($"Attempted to set pose {gestureState}, but poser is not initialized yet.");
            return;
        }
        string target = gestureState.ToString(); // Get the target behavior name based on the gesture state

        if (currentBlendCoroutine != null)
        {
            StopCoroutine(currentBlendCoroutine); // Stop any ongoing blend coroutine
        }

        currentBlendCoroutine = StartCoroutine(CrossFadePose(target, 0.3f));

        

    }


    private IEnumerator CrossFadePose(string targetPose, float duration)
    {
        var behaviors = new[] { "Neutral", "Fist", "OpenHand", "PalmUp", "PalmDown" };
        var startValues = behaviors.ToDictionary(b => b, b => poser.GetBlendingBehaviourValue(b));

        float time = 0f;
        while(time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            foreach (var behavior in behaviors)
            {
                float targetValue = (behavior == targetPose) ? 1f : 0f;
                float val = Mathf.Lerp(startValues[behavior], targetValue, t);
                poser.SetBlendingBehaviourValue(behavior, val);
            }
            yield return null; // Wait for the next frame
        }

        // Ensure every behavior hits its intended end value
        foreach (var b in behaviors)
        {
            poser.SetBlendingBehaviourValue(b, (b == targetPose) ? 1f : 0f);
        }
    }

  




    private IEnumerator WaitForHandToBeInstantiatedAndGrabPoserReference()
    {
        // Wait until the SteamVR_Skeleton_Poser component is available
        while (poser == null)
        {
            poser = GetComponentInChildren<SteamVR_Skeleton_Poser>();


            yield return null; // Wait for the next frame
        }


        Debug.Log("SteamVR_Skeleton_Poser component found and reference grabbed.");
        SetPose(HandGestureState.Neutral); // Set initial pose to Neutral

    }

}
