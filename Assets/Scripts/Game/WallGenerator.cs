using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WallGenerator : MonoBehaviour
{
    [SerializeField]
    private Material meshMaterial;

    [SerializeField]
    private float wallRecoil;

    [SerializeField]
    private TargetSpawner targetSpawnerPrefab;

    private Vector3[,] pointsList;
    private Quaternion[,] rotationsList;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Material startMaterial;
    private WallManager wallManager;
    private WallSettings wallSettings = new WallSettings();

    void Start()
    {
        wallManager = gameObject.GetComponent<WallManager>();
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        startMaterial = meshMaterial;
    }


    // ---------------- Generation Methods ----------------

    public (IEnumerable<Vector3> positions, MeshRenderer meshRenderer) GenerateWall(WallSettings newWallSettings)
    {
        // Set global wall settings
        wallSettings = newWallSettings;

        InitPointsLists(wallSettings.columnCount, wallSettings.rowCount);
        GenerateTargetSpawner();
        GenerateWallMesh();

        IEnumerable<Vector3> positions = wallManager.targetSpawners.Values.Select(m => m.transform.position);

        return (positions, meshRenderer);
    }

    // Generates the targets on the wall.
    public void GenerateTargetSpawner()
    {
        // For each row and column:
        for (int x = 0; x < wallSettings.columnCount; x++)
        {
            for (int y = 0; y < wallSettings.rowCount; y++)
            {
                // Parameters
                Vector3 tempPos = DefineMolePos(x, y);
                Quaternion tempRotation = DefineMoleRotation(x, y);

                AddPoint(x, y, tempPos, tempRotation);

                // If corner: no TargetSpawner is generated
                if ((x == 0 || x == wallSettings.columnCount - 1) && (y == wallSettings.rowCount - 1 || y == 0)) { continue; }

                // TargetSpawner
                MoleParameters parameters = new MoleParameters()
                {
                    id = GenerateIdByIndex(x, y),
                    localScale = wallSettings.moleScale,
                    performanceFeedback = wallManager.GetPerformanceFeedback(),
                    normalizedIndex = GetnormalizedIndex(x, y)
                };

                TargetSpawner newTargetSpawner = TargetSpawner.Instantiate(targetSpawnerPrefab, transform, tempPos, tempRotation, parameters);
                wallManager.targetSpawners.Add(newTargetSpawner.GetId(), newTargetSpawner);

                wallManager.loggingManager.Log("Event", new Dictionary<string, object>()
                {
                    {"Event", "Target Spawner Created"},
                    {"EventType", "MoleEvent"},
                    {"TargetPositionWorldX", newTargetSpawner.transform.position.x},
                    {"TargetPositionWorldY", newTargetSpawner.transform.position.y},
                    {"TargetPositionWorldZ", newTargetSpawner.transform.position.z},
                    {"TargetIndexX", (int)Mathf.Floor(newTargetSpawner.GetId()/100)},
                    {"TargetIndexY", newTargetSpawner.GetId() % 100},
                });
            }
        }
    }

    // Generates the wall mesh.
    public void GenerateWallMesh()
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        // Generates points for the wall overflow (so there is a padding between the wall and the moles at the edges).
        for (int x = 0; x < pointsList.GetLength(0); x++)
        {
            for (int y = 0; y < pointsList.GetLength(1); y++)
            {
                // Far to be clean, but didn't find any better solution.

                // Edges
                if (x == pointsList.GetLength(0) - 1)
                {
                    pointsList[x, y] = pointsList[x - 1, y] - (pointsList[x - 2, y] - pointsList[x - 1, y]);
                    rotationsList[x, y] = rotationsList[x - 1, y];
                }

                if (x == 0)
                {
                    pointsList[x, y] = pointsList[x + 1, y] - (pointsList[x + 2, y] - pointsList[x + 1, y]);
                    rotationsList[x, y] = rotationsList[x + 1, y];
                }

                if (y == pointsList.GetLength(1) - 1)
                {
                    pointsList[x, y] = pointsList[x, y - 1] - (pointsList[x, y - 2] - pointsList[x, y - 1]);
                    rotationsList[x, y] = rotationsList[x, y - 1];
                }

                if (y == 0)
                {
                    pointsList[x, y] = pointsList[x, y + 1] - (pointsList[x, y + 2] - pointsList[x, y + 1]);
                    rotationsList[x, y] = rotationsList[x, y + 1];
                }

                // Corners
                if (x == pointsList.GetLength(0) - 1 && y == 0)
                {
                    pointsList[x, y] = pointsList[x - 1, y + 1] - (pointsList[x - 2, y + 2] - pointsList[x - 1, y + 1]) / 2;
                    rotationsList[x, y] = rotationsList[x - 1, y + 1];
                }

                if (x == 0 && y == 0)
                {
                    pointsList[x, y] = pointsList[x + 1, y + 1] - (pointsList[x + 2, y + 2] - pointsList[x + 1, y + 1]) / 2;
                    rotationsList[x, y] = rotationsList[x + 1, y + 1];
                }

                if (y == pointsList.GetLength(1) - 1 && x == 0)
                {
                    pointsList[x, y] = pointsList[x + 1, y - 1] - (pointsList[x + 2, y - 2] - pointsList[x + 1, y - 1]) / 2;
                    rotationsList[x, y] = rotationsList[x + 1, y - 1];
                }

                if (x == pointsList.GetLength(0) - 1 && y == pointsList.GetLength(1) - 1)
                {
                    pointsList[x, y] = pointsList[x - 1, y - 1] - (pointsList[x - 2, y - 2] - pointsList[x - 1, y - 1]) / 2;
                    rotationsList[x, y] = rotationsList[x - 1, y - 1];
                }
            }
        }

        // Generates the vertices, triangles and UVs, then applies them to the mesh
        for (int x = 0; x < pointsList.GetLength(0); x++)
        {
            for (int y = 0; y < pointsList.GetLength(1); y++)
            {
                int index = (x * pointsList.GetLength(1)) + y;
                vertices.Add(pointsList[x, y] + ((rotationsList[x, y] * Vector3.forward) * wallRecoil));
                uvs.Add(new Vector2((float)x / (pointsList.GetLength(0) - 1), (float)y / (pointsList.GetLength(1) - 1)));

                if (x == 0 || y == 0) continue;

                triangles.Add(index - (pointsList.GetLength(1) + 1));
                triangles.Add(index - (pointsList.GetLength(1)));
                triangles.Add(index);

                triangles.Add(index - (pointsList.GetLength(1) + 1));
                triangles.Add(index);
                triangles.Add(index - 1);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
        meshRenderer.material = meshMaterial;
    }


    // ------------------ Helper Methods ------------------

    // Initialises the arrays
    public void InitPointsLists(int columnCount, int rowCount)
    {
        pointsList = new Vector3[columnCount + 2, rowCount + 2];
        rotationsList = new Quaternion[columnCount + 2, rowCount + 2];
    }

    // Adds a point to the arrays.
    public void AddPoint(int xIndex, int yIndex, Vector3 position, Quaternion rotation)
    {
        pointsList[xIndex + 1, yIndex + 1] = position;
        rotationsList[xIndex + 1, yIndex + 1] = rotation;
    }

    public void SetMeshMaterial(Material mat)
    {
        meshMaterial = mat;
    }

    public void ResetMeshMaterial()
    {
        meshMaterial = startMaterial;
    }

    // Gets the Mole rotation so it is always looking away from the wall, depending on its X local position and the wall's curvature (curveCoeff)
    private Quaternion DefineMoleRotation(int xIndex, int yIndex)
    {
        Quaternion lookAngle = new Quaternion();
        lookAngle.eulerAngles = new Vector3(-((((float)yIndex / (wallSettings.rowCount - 1)) * 2) - 1) * (wallSettings.maxAngle * wallSettings.yCurveRatio), ((((float)xIndex / (wallSettings.columnCount - 1)) * 2) - 1) * (wallSettings.maxAngle * wallSettings.xCurveRatio), 0f);
        return lookAngle;
    }

    // Gets the Mole position depending on its index, the wall size (x and y axes of the vector3), and also on the curve coefficient (for the z axis).
    private Vector3 DefineMolePos(int xIndex, int yIndex)
    {
        float angleX = ((((float)xIndex / (wallSettings.columnCount - 1)) * 2) - 1) * ((Mathf.PI * wallSettings.xCurveRatio) / 2);
        float angleY = ((((float)yIndex / (wallSettings.rowCount - 1)) * 2) - 1) * ((Mathf.PI * wallSettings.yCurveRatio) / 2);

        return new Vector3(Mathf.Sin(angleX) * (wallSettings.wallSize.x / (2 * wallSettings.xCurveRatio)), Mathf.Sin(angleY) * (wallSettings.wallSize.y / (2 * wallSettings.yCurveRatio)), ((Mathf.Cos(angleY) * (wallSettings.wallSize.z)) + (Mathf.Cos(angleX) * (wallSettings.wallSize.z))));
    }

    private int GenerateIdByIndex(int xIndex, int yIndex)
    {
        return ((xIndex + 1) * 100) + (yIndex + 1);
    }

    private Vector2 GetnormalizedIndex(int xIndex, int yIndex)
    {
        return (new Vector2((float)xIndex / (wallSettings.columnCount - 1), (float)yIndex / (wallSettings.rowCount - 1)));
    }
}
