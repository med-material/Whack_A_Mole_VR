// This script should be called "SetMirroredPosition"
// and should be attached to a Camera object
// in Unity which acts as a mirror camera behind a
// mirror. Once a Quad object is specified as the
// "mirrorQuad" and a "mainCamera" is set, the script
// computes the mirrored position of the "mainCamera"
// and places the script's camera at that position.
using UnityEngine;

[ExecuteInEditMode]

public class SetMirroredPosition : MonoBehaviour
{

    public GameObject mirrorQuad;
    public Camera mainCamera;
    public bool isMainCameraStereo;
    public bool useRightEye;

    void LateUpdate()
    {
        if (null != mirrorQuad && null != mainCamera &&
            null != mainCamera.GetComponent<Camera>())
        {
            Vector3 mainCameraPosition;
            if (!isMainCameraStereo)
            {
                mainCameraPosition = mainCamera.transform.position;
            }
            else
            {
                Matrix4x4 viewMatrix = mainCamera.GetStereoViewMatrix(
                    useRightEye ? Camera.StereoscopicEye.Right :
                    Camera.StereoscopicEye.Left);
                mainCameraPosition = viewMatrix.inverse.GetColumn(3);
            }
            Vector3 positionInMirrorSpace =
                mirrorQuad.transform.InverseTransformPoint(mainCameraPosition);
            positionInMirrorSpace.z = -positionInMirrorSpace.z;
            transform.position =
                mirrorQuad.transform.TransformPoint(
                    positionInMirrorSpace);
        }
    }
}
