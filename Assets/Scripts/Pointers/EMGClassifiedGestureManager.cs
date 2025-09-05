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


    // Reference to the SteamVR Skeleton Poser component
    [SerializeField]
    private SteamVR_Skeleton_Poser poser;

    // Predefined poses
    [SerializeField, Tooltip("Pose for Neutral gesture")]
    private SteamVR_Skeleton_Pose neutralPose;
    [SerializeField, Tooltip("Pose for Fist gesture")]
    private SteamVR_Skeleton_Pose fistPose;
    [SerializeField, Tooltip("Pose for Open Hand gesture")]
    private SteamVR_Skeleton_Pose openHandPose;
    [SerializeField, Tooltip("Pose for Palm Upward gesture")]
    private SteamVR_Skeleton_Pose palmUpwardPose;
    [SerializeField, Tooltip("Pose for Palm Downward gesture")]
    private SteamVR_Skeleton_Pose palmDownwardPose;

    private SteamVR_Skeleton_Pose[] poses;


    // Current state tracking
    private HandGestureState currentState = HandGestureState.Neutral;
    private float blendWeight = 0f;

    private void Awake()
    {
        //Wait until the hand is instantiated before attempting to get the poser component
        StartCoroutine(ListenForHandInstantiation());

        if (poses == null)
        {
            poses = new SteamVR_Skeleton_Pose[5];
            poses[0] = neutralPose;
            poses[1] = fistPose;
            poses[2] = openHandPose;
            poses[3] = palmUpwardPose;
            poses[4] = palmDownwardPose;
        }

    }

    void Start()
    {
        if (!poser)
        {
            Debug.LogError("SteamVR_Skeleton_Poser not found! Please assign it in the inspector.");

            return;
        }

        SetupBlendingBehaviors();
        SetPose(HandGestureState.Neutral);

    }

    private void Update()
    {
        //Keyboard driven testing of poses
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetPose(HandGestureState.Neutral);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetPose(HandGestureState.Fist);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetPose(HandGestureState.OpenHand);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetPose(HandGestureState.PalmUpward);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetPose(HandGestureState.PalmDownward);
    }

    private void SetupBlendingBehaviors()
    {
        if (!poser)
        {
            Debug.LogError("SteamVR_Skeleton_Poser not assigned.");
            return;
        }

        // Remove existing behaviors
        poser.blendingBehaviours.Clear();

        // Create manual blending behaviors for each pose
        for (int i = 0; i < poses.Length; i++)
        {
            SteamVR_Skeleton_Poser.PoseBlendingBehaviour behavior = new SteamVR_Skeleton_Poser.PoseBlendingBehaviour();
            behavior.pose = i; // Use the index to reference the pose
            behavior.influence = 0f;
            behavior.type = SteamVR_Skeleton_Poser.PoseBlendingBehaviour.BlenderTypes.Manual;
            behavior.name = $"Pose_{i}_Behavior";
            poser.blendingBehaviours.Add(behavior);
        }
    }

    // Set pose based on the new gesture state
    public void SetPose(HandGestureState newState)
    {
        // Validate inputs
        if (poses == null)
        {
            Debug.LogError("Poses array is null.");
            return;
        }
        // Validate newState index
        if ((int)newState >= poses.Length || newState == currentState)
            return;
        // Validate the specific pose is not null
        if (poses[(int)newState] == null)
        {
            Debug.LogError($"Pose at index {(int)newState} is null.");
            return;
        }

        // Apply the new pose
        float targetWeight = GetTargetBlendWeight(newState);
        currentState = newState;
        StartCoroutine(BlendToNewPose(targetWeight));
    }

    //Blend weight logic to determine if we should blend directly or through neutral
    private float GetTargetBlendWeight(HandGestureState newState)
    {
        // Always blend through neutral state for unrelated poses
        if (newState != HandGestureState.Neutral &&
            !IsRelatedPose(currentState, newState))
            return 0f;

        return 1f;
    }

    // Determine if two poses are related for direct blending
    private bool IsRelatedPose(HandGestureState state1, HandGestureState state2)
    {
        // Define related poses
        bool palmRelated =
            (state1 == HandGestureState.PalmUpward || state1 == HandGestureState.PalmDownward) &&
            (state2 == HandGestureState.PalmUpward || state2 == HandGestureState.PalmDownward);

        bool fistOpenRelated =
            (state1 == HandGestureState.Fist && state2 == HandGestureState.OpenHand) ||
            (state1 == HandGestureState.OpenHand && state2 == HandGestureState.Fist);
        // Return true if either relation is true
        return palmRelated || fistOpenRelated;
    }

    // Coroutine to smoothly blend to the new pose
    private IEnumerator BlendToNewPose(float targetWeight)
    {
        // Blend duration
        const float blendDuration = 0.3f;
        float elapsedTime = 0f;
        // Validate poser and behaviors
        if (poser == null || poser.blendingBehaviours == null)
        {
            Debug.LogError("Poser or blendingBehaviours is null.");
            yield break;
        }
        // Smoothly interpolate blend weight
        while (elapsedTime < blendDuration)
        {
            blendWeight = Mathf.Lerp(blendWeight, targetWeight, elapsedTime / blendDuration);
            elapsedTime += Time.deltaTime;

            foreach (SteamVR_Skeleton_Poser.PoseBlendingBehaviour behavior in poser.blendingBehaviours)
                behavior.influence = blendWeight;

            yield return null;
        }
        // Ensure final weight is set
        blendWeight = targetWeight;
        foreach (SteamVR_Skeleton_Poser.PoseBlendingBehaviour behavior in poser.blendingBehaviours)
            behavior.influence = blendWeight;
    }

    private IEnumerator ListenForHandInstantiation()
    {
        // Wait until the SteamVR_Skeleton_Poser is assigned
        while (poser == null)
        {
            poser = GetComponent<SteamVR_Skeleton_Poser>();
            yield return null;
        }
        // Once assigned, set up blending behaviors
        SetupBlendingBehaviors();
        SetPose(HandGestureState.Neutral);
    }
}
