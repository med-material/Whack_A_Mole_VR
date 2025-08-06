using PupilLabs;
using System.Collections.Generic;
using UnityEngine;

public class GazeLogger : MonoBehaviour
{

    public Transform gazeOrigin;
    public GazeController gazeController;

    private GameObject objectHit;
    private Vector3 worldGazeOrigin;
    private Vector3 localGazeDirection;
    private Vector3 worldGazeDirection;
    private Vector3 gazeHitPosition;
    private float gazeDistance = -1f;
    private float gazeConfidence = -1f;
    private string gameObjectHit;
    private int moleHitX;
    private int moleHitY;
    private int moleHitID;
    private double pupilTime;
    private double unityToPupilTimeOffset;
    private Vector3 eyeCenter0;
    private Vector3 eyeCenter1;
    private Vector3 gazeNormal0;
    private Vector3 gazeNormal1;

    private bool isGazing = false;
    Dictionary<string, object> gazeData;

    [Range(0.01f, 0.1f)]
    public float sphereCastRadius = 0.05f;

    [SerializeField]
    private PupilLabs.TimeSync timeSync;

    void Awake()
    {
        ResetGazeData();
    }

    // Start is called before the first frame update
    void Start()
    {
        gazeController.OnReceive3dGaze += ReceiveGaze;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void ReceiveGaze(GazeData gazeData)
    {
        if (gazeData.MappingContext != GazeData.GazeMappingContext.Binocular)
        {
            isGazing = false;
            return;
        }
        isGazing = true;

        localGazeDirection = gazeData.GazeDirection;
        gazeDistance = gazeData.GazeDistance;
        gazeConfidence = gazeData.Confidence;
        pupilTime = gazeData.PupilTimestamp;
        eyeCenter0 = gazeData.EyeCenter0;
        eyeCenter1 = gazeData.EyeCenter1;
        gazeNormal0 = gazeData.GazeNormal0;
        gazeNormal1 = gazeData.GazeNormal1;
    }

    public Dictionary<string, object> GetGazeData()
    {
        if (isGazing)
        {
            worldGazeOrigin = gazeOrigin.position;
            worldGazeDirection = gazeOrigin.TransformDirection(localGazeDirection);

            if (Physics.SphereCast(worldGazeOrigin, sphereCastRadius, worldGazeDirection, out RaycastHit hit, Mathf.Infinity))
            {
                //Debug.DrawRay(worldGazeOrigin, worldGazeDirection * hit.distance, Color.yellow);
                gazeHitPosition = hit.point;
                objectHit = hit.transform.gameObject;
            }
            // We cannot ask timeSync for a timestamp directly, because it leads to too many requests. Instead
            // we should just rely on the pupil time stamp we get from the ReceiveGaze() connection.
            //gazeData["PupilTime"] = timeSync != null ? timeSync.GetPupilTimestamp().ToString().Replace(",", ".") : "NULL";
            gazeData["UnityToPupilTimeOffset"] = timeSync != null ? timeSync.UnityToPupilTimeOffset.ToString().Replace(",", ".") : "NULL";
            gazeData["PupilTimeSample"] = pupilTime;
            gazeData["GazeConfidence"] = gazeConfidence;
            gazeData["EyeCenter0X"] = eyeCenter0.x;
            gazeData["EyeCenter0Y"] = eyeCenter0.y;
            gazeData["EyeCenter0Z"] = eyeCenter0.z;
            gazeData["EyeCenter1X"] = eyeCenter1.x;
            gazeData["EyeCenter1Y"] = eyeCenter1.y;
            gazeData["EyeCenter1Z"] = eyeCenter1.z;
            gazeData["GazeNormal0X"] = gazeNormal0.x;
            gazeData["GazeNormal0Y"] = gazeNormal0.y;
            gazeData["GazeNormal0Z"] = gazeNormal0.z;
            gazeData["GazeNormal1X"] = gazeNormal1.x;
            gazeData["GazeNormal1Y"] = gazeNormal1.y;
            gazeData["GazeNormal1Z"] = gazeNormal1.z;
            gazeData["LocalGazeDirectionX"] = localGazeDirection.x;
            gazeData["LocalGazeDirectionY"] = localGazeDirection.y;
            gazeData["LocalGazeDirectionZ"] = localGazeDirection.z;
            gazeData["GazeDistance"] = gazeDistance;
            gazeData["WorldGazeOriginX"] = worldGazeOrigin.x;
            gazeData["WorldGazeOriginY"] = worldGazeOrigin.y;
            gazeData["WorldGazeOriginZ"] = worldGazeOrigin.z;
            gazeData["WorldGazeDirectionX"] = worldGazeDirection.x;
            gazeData["WorldGazeDirectionY"] = worldGazeDirection.y;
            gazeData["WorldGazeDirectionZ"] = worldGazeDirection.z;
            gazeData["WorldGazeHitPositionX"] = gazeHitPosition.x;
            gazeData["WorldGazeHitPositionY"] = gazeHitPosition.y;
            gazeData["WorldGazeHitPositionZ"] = gazeHitPosition.z;
            if (objectHit != null)
            {
                gazeData["WorldGazeHitObjectName"] = objectHit.name;
                gazeData["WorldGazeHitObjectMoleID"] = objectHit.TryGetComponent(out DiskMole mole) ? mole.GetId() : -1;
                gazeData["WorldGazeHitObjectIsWall"] = objectHit.TryGetComponent(out WallManager wall) ? "TRUE" : "FALSE";
            }
            else
            {
                gazeData["WorldGazeHitObjectName"] = "NULL";
                gazeData["WorldGazeHitObjectMoleID"] = -1;
                gazeData["WorldGazeHitObjectIsWall"] = "FALSE";
            }
        }
        else
        {
            ResetGazeData();
        }
        return gazeData;
    }

    private void ResetGazeData()
    {
        gazeData = new Dictionary<string, object>()
        {
            {"PupilTime", "NULL"},
            {"UnityToPupilTimeOffset", "NULL"},
            {"GazeConfidence", "NULL"},
            {"EyeCenter0X", "NULL"},
            {"EyeCenter0Y", "NULL"},
            {"EyeCenter0Z", "NULL"},
            {"EyeCenter1X", "NULL"},
            {"EyeCenter1Y", "NULL"},
            {"EyeCenter1Z", "NULL"},
            {"GazeNormal0X", "NULL"},
            {"GazeNormal0Y", "NULL"},
            {"GazeNormal0Z", "NULL"},
            {"GazeNormal1X", "NULL"},
            {"GazeNormal1Y", "NULL"},
            {"GazeNormal1Z", "NULL"},
            {"LocalGazeDirectionX", "NULL"},
            {"LocalGazeDirectionY", "NULL"},
            {"LocalGazeDirectionZ", "NULL"},
            {"GazeDistance", "NULL"},
            {"WorldGazeOriginX", "NULL"},
            {"WorldGazeOriginY", "NULL"},
            {"WorldGazeOriginZ", "NULL"},
            {"WorldGazeDirectionX", "NULL"},
            {"WorldGazeDirectionY", "NULL"},
            {"WorldGazeDirectionZ", "NULL"},
            {"WorldGazeHitPositionX", "NULL"},
            {"WorldGazeHitPositionY", "NULL"},
            {"WorldGazeHitPositionZ", "NULL"},
            {"WorldGazeHitObjectName", "NULL"},
            {"WorldGazeHitObjectIsMole", "NULL"},
            {"WorldGazeHitObjectMoleID", "NULL"},
            {"WorldGazeHitObjectIsWall", "NULL"}
        };
    }

}
