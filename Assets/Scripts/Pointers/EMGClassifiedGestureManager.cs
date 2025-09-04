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

    void Start()
    {
        // Always get the poser component at runtime
        poser = GetComponent<SteamVR_Skeleton_Poser>();

        // Create blending behaviors for each pose
        SetupBlendingBehaviors();

        // Set initial neutral state
        SetPose(HandGestureState.Neutral);

        // Internal list of the poses for use in code later down the line
        poses = new SteamVR_Skeleton_Pose[]
        {
            neutralPose,
            fistPose,
            openHandPose,
            palmUpwardPose,
            palmDownwardPose
        };
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

    public void SetPose(HandGestureState newState)
    {
        // Handle invalid states
        if ((int)newState >= poses.Length || newState == currentState)
            return;

        // Calculate target blend weight based on state transition
        float targetWeight = GetTargetBlendWeight(newState);

        // Update current state
        currentState = newState;

        // Apply the new pose with blending
        StartCoroutine(BlendToNewPose(targetWeight));
    }

    private float GetTargetBlendWeight(HandGestureState newState)
    {
        // Always blend through neutral state for unrelated poses
        if (newState != HandGestureState.Neutral &&
            !IsRelatedPose(currentState, newState))
            return 0f;

        return 1f;
    }

    private bool IsRelatedPose(HandGestureState state1, HandGestureState state2)
    {
        bool palmRelated =
            (state1 == HandGestureState.PalmUpward || state1 == HandGestureState.PalmDownward) &&
            (state2 == HandGestureState.PalmUpward || state2 == HandGestureState.PalmDownward);

        bool fistOpenRelated =
            (state1 == HandGestureState.Fist && state2 == HandGestureState.OpenHand) ||
            (state1 == HandGestureState.OpenHand && state2 == HandGestureState.Fist);

        return palmRelated || fistOpenRelated;
    }

    private IEnumerator BlendToNewPose(float targetWeight)
    {
        const float blendDuration = 0.3f; // Adjust this value for desired blend speed
        float elapsedTime = 0f;

        while (elapsedTime < blendDuration)
        {
            blendWeight = Mathf.Lerp(blendWeight, targetWeight, elapsedTime / blendDuration);
            elapsedTime += Time.deltaTime;

            // Update all blending behaviors
            foreach (SteamVR_Skeleton_Poser.PoseBlendingBehaviour behavior in poser.blendingBehaviours)
                behavior.influence = blendWeight;

            yield return null;
        }

        // Ensure exact target weight is reached
        blendWeight = targetWeight;
        foreach (SteamVR_Skeleton_Poser.PoseBlendingBehaviour behavior in poser.blendingBehaviours)
            behavior.influence = blendWeight;
    }
}
