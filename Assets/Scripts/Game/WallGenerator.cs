using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallGenerator : MonoBehaviour
{
    [SerializeField]
    private Material meshMaterial;

    [SerializeField]
    private float wallRecoil; // Distance to push the mesh points outwards based on their rotation.

    private Vector3[,] pointsList;  // List of points to use for the mesh.
    private Quaternion[,] rotationsList;  // Corresponding rotations for the points.
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Material startMaterial;


    void Start()
    {
        // Initializing mesh-related components.
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        startMaterial = meshMaterial;
    }

    // Initialises the arrays.
    public void InitPointsLists(int columnCount, int rowCount)
    {
        pointsList = new Vector3[columnCount + 2, rowCount + 2]; // +2 for padding on each side.
        rotationsList = new Quaternion[columnCount + 2, rowCount + 2];
    }

    // Adds a point to the arrays.
    public void AddPoint(int xIndex, int yIndex, Vector3 position, Quaternion rotation)
    {
        // Offset by 1 for padding.
        pointsList[xIndex + 1, yIndex + 1] = position;
        rotationsList[xIndex + 1, yIndex + 1] = rotation;
    }

    // Update the material of the mesh.
    public void SetMeshMaterial(Material mat)
    {
        meshMaterial = mat;
    }

    // Resets the material of the mesh to its initial state.
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

        // Overflow generation to ensure the wall has padding at its edges.
        for (int x = 0; x < pointsList.GetLength(0); x++)
        {
            for (int y = 0; y < pointsList.GetLength(1); y++)
            {
                // Adjusting edge points to make sure the wall extends beyond the main area.
                // Edges
                if (x == pointsList.GetLength(0) - 1)
                {
                    // Right edge: mirroring the second last column to extend the wall.
                    pointsList[x, y] = pointsList[x - 1, y] - (pointsList[x - 2, y] - pointsList[x - 1, y]);
                    rotationsList[x, y] = rotationsList[x - 1, y];
                }

                if (x == 0)
                {
                    // Left edge: mirroring the second column to extend the wall.
                    pointsList[x, y] = pointsList[x + 1, y] - (pointsList[x + 2, y] - pointsList[x + 1, y]);
                    rotationsList[x, y] = rotationsList[x + 1, y];
                }

                if (y == pointsList.GetLength(1) - 1)
                {
                    // Top edge: mirroring the second last row to extend the wall.
                    pointsList[x, y] = pointsList[x, y - 1] - (pointsList[x, y - 2] - pointsList[x, y - 1]);
                    rotationsList[x, y] = rotationsList[x, y - 1];
                }

                if (y == 0)
                {
                    // Bottom edge: mirroring the second row to extend the wall.
                    pointsList[x, y] = pointsList[x, y + 1] - (pointsList[x, y + 2] - pointsList[x, y + 1]);
                    rotationsList[x, y] = rotationsList[x, y + 1];
                }

                // Adjusting corner points. This might be causing the "cut corners".
                // These calculations are averaging the points diagonally adjacent to the corner, which may lead to a slight indent.
                // Corners
                if (x == pointsList.GetLength(0) - 1 && y == 0)
                {
                    // Bottom right corner.
                    pointsList[x, y] = pointsList[x - 1, y + 1] - (pointsList[x - 2, y + 2] - pointsList[x - 1, y + 1]) / 2;
                    rotationsList[x, y] = rotationsList[x - 1, y + 1];
                }

                if (x == 0 && y == 0)
                {
                    // Bottom left corner.
                    pointsList[x, y] = pointsList[x + 1, y + 1] - (pointsList[x + 2, y + 2] - pointsList[x + 1, y + 1]) / 2;
                    rotationsList[x, y] = rotationsList[x + 1, y + 1];
                }

                if (y == pointsList.GetLength(1) - 1 && x == 0)
                {
                    // Top left corner.
                    pointsList[x, y] = pointsList[x + 1, y - 1] - (pointsList[x + 2, y - 2] - pointsList[x + 1, y - 1]) / 2;
                    rotationsList[x, y] = rotationsList[x + 1, y - 1];
                }

                if (x == pointsList.GetLength(0) - 1 && y == pointsList.GetLength(1) - 1)
                {
                    // Top right corner.
                    pointsList[x, y] = pointsList[x - 1, y - 1] - (pointsList[x - 2, y - 2] - pointsList[x - 1, y - 1]) / 2;
                    rotationsList[x, y] = rotationsList[x - 1, y - 1];
                }
            }
        }

        // Generating the actual mesh based on points and rotations.
        for (int x = 0; x < pointsList.GetLength(0); x++)
        {
            for (int y = 0; y < pointsList.GetLength(1); y++)
            {
                int index = (x * pointsList.GetLength(1)) + y;
                // Push the point outwards based on its rotation and the recoil amount.
                vertices.Add(pointsList[x, y] + ((rotationsList[x, y] * Vector3.forward) * wallRecoil));
                uvs.Add(new Vector2((float)x / (pointsList.GetLength(0) - 1), (float)y / (pointsList.GetLength(1) - 1)));

                // Skip triangles for the first row and column to prevent wrap-around.
                if (x == 0 || y == 0) continue;

                // Define the triangles for the mesh.
                triangles.Add(index - (pointsList.GetLength(1) + 1));
                triangles.Add(index - (pointsList.GetLength(1)));
                triangles.Add(index);

                triangles.Add(index - (pointsList.GetLength(1) + 1));
                triangles.Add(index);
                triangles.Add(index - 1);
            }
        }

        // Assign the vertices, triangles, and UVs to the mesh.
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
        meshRenderer.material = meshMaterial;
    }
}