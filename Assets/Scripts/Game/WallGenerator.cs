using System.Collections.Generic;
using UnityEngine;

public class WallGenerator : MonoBehaviour
{
    [SerializeField]
    private Material meshMaterial;

    [SerializeField]
    private float wallRecoil;

    private Vector3[,] pointsList;
    private Quaternion[,] rotationsList;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Material startMaterial;


    void Start()
    {
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        startMaterial = meshMaterial;
    }

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

    // Generates the wall mesh.
    public void GenerateWall()
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
}
