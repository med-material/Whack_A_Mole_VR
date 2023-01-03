using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class ControllerModifierManager : MonoBehaviour
{
    private Transform cameraTransform;
    private Transform wallTransform;
    private SteamVR_Action_Pose controllerPose;
    private SteamVR_Input_Sources inputSource;
    private bool isMirroring = false;
    private Pointer controllerPointer;


    void Start()
    {
        inputSource = gameObject.GetComponent<SteamVR_Behaviour_Pose>().inputSource;
        controllerPose = gameObject.GetComponent<SteamVR_Behaviour_Pose>().poseAction;
        controllerPointer = gameObject.GetComponent<Pointer>();
    }

    // Enables mirroring. Disables the controller position update and uses OnPoseUpdate to set a custom mirrored position on  tracked controller position update
    public void EnableMirror(Transform camera, Transform wall)
    {
        if (isMirroring) return;
        cameraTransform = camera;
        wallTransform = wall;
        // Disables default position update
        gameObject.GetComponent<SteamVR_Behaviour_Pose>().enabled = false;
        // Uses its own position update
        SteamVR_Input.onPosesUpdated += OnPoseUpdated;
        isMirroring = true;
    }

    // Disables mirroring. Reset position update as default and removes the custom position updating function to the update event call list
    public void DisableMirror()
    {
        if (!isMirroring) return;
        gameObject.GetComponent<SteamVR_Behaviour_Pose>().enabled = true;
        SteamVR_Input.onPosesUpdated -= OnPoseUpdated;
        isMirroring = false;
    }

    // On VR update, update the position and rotation so they are mirrored. The "mirroring plane" has the angle of the wall and the position of the head.
    private void OnPoseUpdated(bool obj)
    {
        if(controllerPointer)
        {
            controllerPointer.PositionUpdated();
        }
        // Defines the local position of the controller, its position relative to the head and its rotation
        Vector3 controllerLocalPosition = controllerPose.GetLocalPosition(inputSource);
        Vector3 controllerToHead = controllerLocalPosition - cameraTransform.localPosition;
        Vector3 controllerAngles = controllerPose.GetLocalRotation(inputSource).eulerAngles;

        // Defines the "mirroring plane" Quaternion (rotation), taking into account the angle of the y axis of the wall
        Quaternion mirrorPlane = Quaternion.AngleAxis(wallTransform.localEulerAngles.y, Vector3.up);

        // Mirrors the position of the controller using the mirroring plane rotation
        Vector3 reflectedController = Vector3.Reflect(controllerToHead, mirrorPlane * Vector3.right);

        // Sets the position of the mirrored controller relative to the head
        transform.localPosition = cameraTransform.localPosition + reflectedController;

        // Reverts the rotation of the controller on the y and z axis, taking into account the rotation of the y axis of the wall.
        // The x axis osn't reverted otherwise the pointer will point down when pointing up.
        Vector3 mirroredControllerAngle = new Vector3(controllerAngles.x, (2 * wallTransform.eulerAngles.y) - controllerAngles.y , -controllerAngles.z);

        //Sets the rotation of the mirored controller
        transform.localRotation = Quaternion.Euler(mirroredControllerAngle);
    }

}
