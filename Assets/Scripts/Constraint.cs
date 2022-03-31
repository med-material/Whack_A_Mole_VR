using UnityEngine;

public class Constraint : MonoBehaviour
{

    // desiredHeight should align with the default height of the motorspace (1.3).
    [SerializeField]
    private float desiredHeight = 1.3f;

    //Assign these variables in the inspector, or find them some other way (eg. in Start() )
    [SerializeField]
    private Transform steamCamera;

    [SerializeField]
    private Transform steamController;

    [SerializeField]    
    private Transform cameraRig;

    public void SetReset()
    {
        if ((steamCamera != null) && (cameraRig != null))
        {
            /*ROTATION*/
            // Get current head heading in scene (y-only, to avoid tilting the floor)
            float offsetAngle = steamCamera.rotation.eulerAngles.y;
            float offsetX = steamCamera.transform.position.x;
            float offsetZ = steamCamera.transform.position.z;

            // Now rotate CameraRig in opposite direction to compensate
            cameraRig.Rotate(0f, -offsetAngle, 0f);

            /*POSITION*/
            // calculate how much to add or subtract from the height, to arrive at y 1.6
            float headHeight = steamCamera.transform.localPosition.y;
            //float heightChange = desiredHeight - headHeight;
            float xChange = transform.position.x - offsetX;
            float zChange = transform.position.z - offsetZ;

            /*FIT HEIGHT TO MOTORSPACE*/
            // instead of moving the motorspace, we should move players
            // such that they have the correct height for using the motorspace.

            // A: determine current player controller height
            float controllerHeight = steamController.position.y;
            Debug.Log("SteamController Height: " + steamController.position.y.ToString());
            // B: determine motorspace (bottom) height
            float heightChange = desiredHeight - controllerHeight;

            // C: offset player upwards, so the controller aligns with bottom of the motorspace.
            
            transform.position = new Vector3(xChange, heightChange, zChange);
            Debug.Log("Seat recentered!");
        }
        else
        {
            Debug.Log("Error: SteamVR objects not found!");
        }
    }

}