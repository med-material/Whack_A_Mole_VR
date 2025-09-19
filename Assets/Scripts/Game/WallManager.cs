using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/*
Spawns, references and activates the moles. Is the only component to directly interact with the moles.
*/

public class WallInfo
{
    public bool active = false;
    public Dictionary<int, TargetSpawner> targetSpawners;
    public Vector3 wallSize;
    public Vector3 wallCenter; // not the center of the wall (?)
    public float highestX = -1f;
    public float highestY = -1f;
    public float lowestX = -1f;
    public float lowestY = -1f;
    public float lowestZ = -1f;
    public float highestZ = -1f;
    public float heightOffset;
    public Vector3 meshCenter = new Vector3(-1f, -1f, -1f);
    public float meshBoundsXmax = -1f;
    public float meshBoundsYmax = -1f;
    public float meshBoundsZmax = -1f;
    public float meshBoundsXmin = -1f;
    public float meshBoundsYmin = -1f;
    public float meshBoundsZmin = -1f;
}

[System.Serializable]
public class WallSettings
{
    public int rowCount;
    public int columnCount;
    public float heightOffset;
    public Vector3 wallSize;
    public float xCurveRatio;
    public float yCurveRatio;
    public float maxAngle;
    public Vector3 moleScale;
}

public class WallManager : MonoBehaviour
{
    [SerializeField] public LoggingManager loggingManager;

    [Header("Default Wall Settings")]
    [SerializeField] private WallSettings defaultWall = new WallSettings();

    [Header("Runtime Wall Settings")]
    [SerializeField, Tooltip("The count of rows to generate")]
    private int rowCount;

    [SerializeField, Tooltip("The count of columns to generate")]
    private int columnCount;

    [SerializeField, Tooltip("Offset of the height of the wall")]
    private float heightOffset;

    [SerializeField, Tooltip("The size of the wall")]
    private Vector3 wallSize;

    [SerializeField, Range(0.1f, 1f), Tooltip("Coefficient of the X curvature of the wall. 1 = PI/2, 0 = straight line")]
    private float xCurveRatio = 1f;

    [SerializeField, Range(0.1f, 1f), Tooltip("Coefficient of the Y curvature of the wall. 1 = PI/2, 0 = straight line")]
    private float yCurveRatio = 1f;

    [SerializeField, Range(0f, 90f), Tooltip("The angle of the edge moles if a curve ratio of 1 is given")]
    private float maxAngle = 90f;

    [SerializeField, Tooltip("The scale of the Mole. Ideally shouldn't be scaled on the Z axis (to preserve the animations)")]
    private Vector3 moleScale = Vector3.one;

    [SerializeField] private Material invisibleMaterial;
    [SerializeField] private MeshRenderer greyBackground;

    [System.Serializable]
    public class StateUpdateEvent : UnityEvent<WallInfo> { }
    public StateUpdateEvent stateUpdateEvent;
    public Dictionary<int, TargetSpawner> targetSpawners { get; private set; }

    private WallGenerator wallGenerator;
    private Vector3 wallCenter;
    private Vector3 wallCenterWorld = Vector3.zero;
    private bool active = false;
    private bool isInit = false;
    private float updateCooldownDuration = .1f;
    private LoggerNotifier loggerNotifier;
    private int moleCount = 0;
    private int spawnOrder = 0;
    private bool wallVisible = true;
    private bool performanceFeedback = true;

    // Wall boundaries
    private float highestX = -1f;
    private float highestY = -1f;
    private float lowestX = -1f;
    private float lowestY = -1f;
    private float lowestZ = -1f;
    private float highestZ = -1f;

    // Mesh boundaries
    Vector3 meshCenter = new Vector3(-1f, -1f, -1f);
    float meshBoundsXmax = -1f;
    float meshBoundsYmax = -1f;
    float meshBoundsZmax = -1f;
    float meshBoundsXmin = -1f;
    float meshBoundsYmin = -1f;
    float meshBoundsZmin = -1f;

    void Start()
    {
        SetDefaultWall();

        // Initialization of the LoggerNotifier.
        loggerNotifier = new LoggerNotifier(persistentEventsHeadersDefaults: new Dictionary<string, string>(){
            {"WallRowCount", "NULL"},
            {"WallColumnCount", "NULL"},
            {"WallSizeX", "NULL"},
            {"WallSizeY", "NULL"},
            {"WallSizeZ", "NULL"},
            {"WallBoundsXMin", "NULL"},
            {"WallBoundsYMin", "NULL"},
            {"WallBoundsZMin", "NULL"},
            {"WallBoundsXMax", "NULL"},
            {"WallBoundsYMax", "NULL"},
            {"WallBoundsZMax", "NULL"},
            {"WallCenterX", "NULL"},
            {"WallCenterY", "NULL"},
            {"WallCenterZ", "NULL"},
            {"WallCurveRatioX", "NULL"},
            {"WallCurveRatioY", "NULL"}
        });

        loggerNotifier.InitPersistentEventParameters(new Dictionary<string, object>(){
            {"WallRowCount", rowCount},
            {"WallColumnCount", columnCount},
            {"WallSizeX", wallSize.x},
            {"WallSizeY", wallSize.y},
            {"WallSizeZ", wallSize.z},
            {"WallBoundsXMin", wallSize.x},
            {"WallBoundsYMin", wallSize.y},
            {"WallBoundsZMin", wallSize.z},
            {"WallBoundsXMax", wallSize.x},
            {"WallBoundsYMax", wallSize.y},
            {"WallBoundsZMax", wallSize.z},
            {"WallCenterX", wallCenter.x},
            {"WallCenterY", wallCenter.y},
            {"WallCenterZ", wallCenter.z},
            {"WallCurveRatioX", xCurveRatio},
            {"WallCurveRatioY", yCurveRatio}
        });

        targetSpawners = new Dictionary<int, TargetSpawner>();
        wallGenerator = gameObject.GetComponent<WallGenerator>();
        wallCenter = new Vector3(wallSize.x / 2f, wallSize.y / 2f, 0);
        isInit = true;
    }

    // Sets an eye patch. Calls WaitForCameraAndUpdate coroutine to set eye patch.
    public void SetWallVisible(bool value)
    {
        if (wallVisible == value) return;
        wallVisible = value;
        if (!wallVisible)
        {
            wallGenerator.SetMeshMaterial(invisibleMaterial);
            //greyBackground.enabled = true; // No longer needed, keep it commented out just in case
        }
        else
        {
            wallGenerator.ResetMeshMaterial();
            greyBackground.enabled = false;
        }
    }

    public void SetDefaultWall()
    {
        rowCount = defaultWall.rowCount;
        columnCount = defaultWall.columnCount;
        heightOffset = defaultWall.heightOffset;
        wallSize = defaultWall.wallSize;
        xCurveRatio = defaultWall.xCurveRatio;
        yCurveRatio = defaultWall.yCurveRatio;
        maxAngle = defaultWall.maxAngle;
        moleScale = defaultWall.moleScale;
    }

    private void UpdateWallLogs()
    {
        MeshRenderer mesh = GetComponent<MeshRenderer>();

        loggerNotifier.InitPersistentEventParameters(new Dictionary<string, object>(){
            {"WallRowCount", rowCount},
            {"WallColumnCount", columnCount},
            {"WallSizeX", wallSize.x},
            {"WallSizeY", wallSize.y},
            {"WallSizeZ", wallSize.z},
            {"WallBoundsXMin", meshBoundsXmin},
            {"WallBoundsYMin", meshBoundsYmin},
            {"WallBoundsZMin", meshBoundsZmin},
            {"WallBoundsXMax", meshBoundsXmax},
            {"WallBoundsYMax", meshBoundsYmax},
            {"WallBoundsZMax", meshBoundsZmax},
            {"WallCenterX", meshCenter.x},
            {"WallCenterY", meshCenter.y},
            {"WallCenterZ", meshCenter.z},
            {"WallCurveRatioX", xCurveRatio},
            {"WallCurveRatioY", yCurveRatio}
        });
    }

    void OnValidate()
    {
        UpdateWall();
    }

    public void Enable()
    {
        active = true;

        if (targetSpawners.Count == 0)
        {
            IEnumerable<Vector3> positions;
            MeshRenderer meshRenderer;

            (positions, meshRenderer) = wallGenerator.GenerateWall(new WallSettings()
            {
                rowCount = rowCount,
                columnCount = columnCount,
                heightOffset = heightOffset,
                wallSize = wallSize,
                xCurveRatio = xCurveRatio,
                yCurveRatio = yCurveRatio,
                maxAngle = maxAngle,
                moleScale = moleScale
            });

            UpdateWallInfoFromGeneration(positions, meshRenderer);
            UpdateWallLogs();
        }
    }

    public void Disable()
    {
        active = false;
        disableMoles();
    }

    public void Clear()
    {
        active = false;
        DestroyTargetSpawners();
        WallInfo wallInfo = CreateWallInfo();
        stateUpdateEvent.Invoke(wallInfo);
    }

    public void UpdateWallInfoFromGeneration(IEnumerable<Vector3> positions, MeshRenderer mesh)
    {
        wallCenter = new Vector3(wallSize.x / 2f, wallSize.y / 2f, 0);

        highestX = positions.Max(p => p.x);
        lowestX = positions.Min(p => p.x);
        highestY = positions.Max(p => p.y);
        lowestY = positions.Min(p => p.y);
        highestZ = positions.Max(p => p.z);
        lowestZ = positions.Min(p => p.z);

        meshCenter = mesh.bounds.center;
        meshBoundsXmax = mesh.bounds.max.x;
        meshBoundsYmax = mesh.bounds.max.y;
        meshBoundsZmax = mesh.bounds.max.z;
        meshBoundsXmin = mesh.bounds.min.x;
        meshBoundsYmin = mesh.bounds.min.y;
        meshBoundsZmin = mesh.bounds.min.z;

        WallInfo wallInfo = CreateWallInfo();
        stateUpdateEvent.Invoke(wallInfo);
    }

    public WallInfo CreateWallInfo()
    {
        WallInfo wallInfo = new WallInfo();
        wallInfo.active = active;
        wallInfo.targetSpawners = targetSpawners;
        wallInfo.wallSize = wallSize;
        wallInfo.wallCenter = wallCenter;
        wallInfo.heightOffset = heightOffset;
        wallInfo.highestX = highestX;
        wallInfo.highestY = highestY;
        wallInfo.lowestX = lowestX;
        wallInfo.lowestY = lowestY;
        wallInfo.lowestZ = lowestZ;
        wallInfo.highestZ = highestZ;
        wallInfo.meshCenter = meshCenter;
        wallInfo.meshBoundsXmax = meshBoundsXmax;
        wallInfo.meshBoundsYmax = meshBoundsYmax;
        wallInfo.meshBoundsZmax = meshBoundsZmax;
        wallInfo.meshBoundsXmin = meshBoundsXmin;
        wallInfo.meshBoundsYmin = meshBoundsYmin;
        wallInfo.meshBoundsZmin = meshBoundsZmin;

        return wallInfo;
    }

    // Activates a random Mole for a given lifeTime and set if is fake or not
    public void ActivateRandomMole(float lifeTime, float moleExpiringDuration, Mole.MoleType type, Mole.MoleOutcome outcome)
    {
        if (!active) return;

        GetRandomFreeSpawner().SpawnMole(type, outcome, lifeTime, moleExpiringDuration, moleCount);
        moleCount++;
    }

    // Activates a specific Mole for a given lifeTime and set if is fake or not
    public Mole CreateMole(int targetSpawnId, float lifeTime, float moleExpiringDuration, Mole.MoleType type, Mole.MoleOutcome outcome)
    {
        if (!active) return null;
        if (!targetSpawners.ContainsKey(targetSpawnId))
        {
            Debug.LogError($"TargetSpawner with ID {targetSpawnId} does not exist.");
            return null;
        }

        targetSpawners[targetSpawnId].SpawnMole(type, outcome, lifeTime, moleExpiringDuration, spawnOrder);
        moleCount++;

        return targetSpawners[targetSpawnId].GetCurrentMole();
    }

    // Pauses/unpauses the moles
    public void SetPauseMole(bool pause)
    {
        foreach (TargetSpawner targetSpawner in targetSpawners.Values)
        {
            targetSpawner.GetCurrentMole()?.SetPause(pause);
        }
    }

    public void SetSpawnOrder(int value)
    {
        spawnOrder = value;
    }

    public void UpdateMoleCount(int newRowCount = -1, int newColumnCount = -1)
    {
        if (newRowCount >= 2) rowCount = newRowCount;
        if (newColumnCount >= 2) columnCount = newColumnCount;
        // UpdateWall();
    }

    public void UpdateWallSize(float newWallSizeX = -1, float newWallSizeY = -1, float newWallSizeZ = -1)
    {
        if (newWallSizeX >= 0) wallSize.x = newWallSizeX;
        if (newWallSizeY >= 0) wallSize.y = newWallSizeY;
        if (newWallSizeZ >= 0) wallSize.z = newWallSizeZ;
        // UpdateWall();
    }

    public void UpdateWallPosition(float? posX = null, float? posY = null, float? posZ = null)
    {
        transform.transform.localPosition = new Vector3(
            posX.HasValue ? posX.Value : transform.localPosition.x,
            posY.HasValue ? posY.Value : transform.localPosition.y,
            posZ.HasValue ? posZ.Value : transform.localPosition.z
        );
    }

    public void UpdateWallCurveRatio(float newCurveRatioX = -1, float newCurveRatioY = -1)
    {
        if (newCurveRatioX >= 0 && newCurveRatioX <= 1) xCurveRatio = newCurveRatioX;
        if (newCurveRatioY >= 0 && newCurveRatioY <= 1) yCurveRatio = newCurveRatioY;
        // UpdateWall();
    }

    public void UpdateWallMaxAngle(float newMaxAngle)
    {
        if (newMaxAngle >= 0 && newMaxAngle <= 90) maxAngle = newMaxAngle;
        // UpdateWall();
    }

    public void UpdateMoleScale(float newMoleScaleX = -1, float newMoleScaleY = -1, float newMoleScaleZ = -1)
    {
        if (newMoleScaleX >= 0) moleScale.x = newMoleScaleX;
        if (newMoleScaleY >= 0) moleScale.y = newMoleScaleY;
        if (newMoleScaleZ >= 0) moleScale.z = newMoleScaleZ;
        // UpdateWall();
    }

    public UnityEvent<WallInfo> GetUpdateEvent()
    {
        return stateUpdateEvent;
    }

    public void SetPerformanceFeedback(bool perf)
    {
        performanceFeedback = perf;

        foreach (TargetSpawner targetSpawner in targetSpawners.Values)
        {
            targetSpawner.UpdateParameters(performanceFeedback: performanceFeedback);
        }
    }
    public bool GetPerformanceFeedback()
    {
        return performanceFeedback;
    }

    // Returns a random, inactive TargetSpawner (isFree == true). Throws if none are free.
    private TargetSpawner GetRandomFreeSpawner()
    {
        List<TargetSpawner> freeSpawners = targetSpawners.Values.Where(ts => ts.isFree()).ToList();
        if (freeSpawners.Count == 0)
        {
            throw new System.InvalidOperationException("No free TargetSpawner available.");
        }
        int randomIndex = Random.Range(0, freeSpawners.Count);
        return freeSpawners[randomIndex];
    }

    private void disableMoles()
    {
        foreach (TargetSpawner targetSpawner in targetSpawners.Values)
        {
            // TODO set targetSpawner visibility to false
            targetSpawner.DespawnMole();
            moleCount = 0;
        }
    }

    private void DestroyTargetSpawners()
    {
        foreach (TargetSpawner targetSpawner in targetSpawners.Values)
        {
            Destroy(targetSpawner.gameObject);
        }
        targetSpawners.Clear();
        moleCount = 0;
    }

    // Updates the wall
    private void UpdateWall()
    {
        if (!(active && isInit)) return;
        StopAllCoroutines();
        StartCoroutine(WallUpdateCooldown());
    }

    private IEnumerator WallUpdateCooldown()
    {
        yield return new WaitForSeconds(updateCooldownDuration);

        if (active)
        {
            Clear();
            Enable();
        }
    }
    public void ResetMoleSpawnOrder()
    {
        moleCount = 0;
    }
}
