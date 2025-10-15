using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class PatternWallVisualizerWindow : EditorWindow
{
    //Usage: Place script in Assets/Editor folder.
    //Ensure test patterns are located in Application.persistentDataPath + "/TestPatterns"
    //To use the functionality use Tools -> Pattern Wall Visualizer.

    private string patternsPath => Path.Combine(Application.persistentDataPath, "TestPatterns");
    private string[] files = new string[0];
    private int selectedIndex = 0;
    private Vector3 previewOffset = Vector3.zero;
    private Transform referenceTransform;
    private bool showPerimeter = true;

    // parsed wall params
    private int rowCount = 9;
    private int columnCount = 9;
    private Vector3 wallSize = new Vector3(4f, 2.5f, 1f);
    private float xCurveRatio = 0.6f;
    private float yCurveRatio = 0.4f;
    private float maxAngle = 80f;
    private List<Vector3> perimeterPoints = new List<Vector3>();

    // number of samples per edge when drawing a smooth outline
    private int edgeSampleCount = 24;

    // mimic wall mesh padding/shaved corners used by WallGenerator
    private bool mimicMeshEdges = true;
    private float meshRecoil = 0f; // if >0 apply the forward recoil offset like WallGenerator

    [MenuItem("Tools/Pattern Wall Visualizer")]
    public static void ShowWindow() => GetWindow<PatternWallVisualizerWindow>("Wall Visualizer");

    private void OnEnable()
    {
        RefreshFileList();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void RefreshFileList()
    {
        if (!Directory.Exists(patternsPath))
        {
            files = new string[0];
            return;
        }
        files = Directory.GetFiles(patternsPath, "*.wampat").Select(Path.GetFileName).ToArray();
        if (files.Length == 0) files = new string[0];
        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, files.Length - 1));
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Pattern files path:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(patternsPath, EditorStyles.wordWrappedLabel);

        if (GUILayout.Button("Refresh list")) RefreshFileList();

        EditorGUILayout.Space();

        if (files.Length == 0)
        {
            EditorGUILayout.HelpBox("No .wampat files found in the TestPatterns folder.", MessageType.Info);
        }
        else
        {
            selectedIndex = EditorGUILayout.Popup("Select pattern", selectedIndex, files);
            if (GUILayout.Button("Load & Visualize"))
            {
                LoadSelectedAndCompute();
            }
        }

        EditorGUILayout.Space();
        referenceTransform = (Transform)EditorGUILayout.ObjectField("Reference Transform", referenceTransform, typeof(Transform), true);
        previewOffset = EditorGUILayout.Vector3Field("Local Offset", previewOffset);
        showPerimeter = EditorGUILayout.Toggle("Show Perimeter", showPerimeter);

        // allow tuning of edge sampling for smoother outline
        edgeSampleCount = EditorGUILayout.IntField("Edge samples", edgeSampleCount);
        if (edgeSampleCount < 2) edgeSampleCount = 2;

        // mesh-style outline options
        mimicMeshEdges = EditorGUILayout.Toggle("Mimic mesh shaved edges", mimicMeshEdges);
        meshRecoil = EditorGUILayout.FloatField("Mesh recoil (apply forward offset)", meshRecoil);

        if (GUILayout.Button("Clear visualization"))
        {
            perimeterPoints.Clear();
            SceneView.RepaintAll();
        }
    }

    private void LoadSelectedAndCompute()
    {
        if (files.Length == 0) return;
        string path = Path.Combine(patternsPath, files[selectedIndex]);
        string[] lines = File.ReadAllLines(path);

        // find WALL line
        string wallLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith("WALL:"));
        if (wallLine == null)
        {
            EditorUtility.DisplayDialog("No WALL", "No WALL line found in pattern.", "OK");
            return;
        }

        // extract inside parentheses
        var m = Regex.Match(wallLine, @"WALL\s*:\s*\((.*)\)");
        if (!m.Success)
        {
            EditorUtility.DisplayDialog("Parse error", "Cannot parse WALL line.", "OK");
            return;
        }

        string inside = m.Groups[1].Value;
        var entries = inside.Split(',').Select(s => s.Trim()).Where(s => s.Contains("=")).ToDictionary(
            s => s.Split('=')[0].Trim().ToUpper(),
            s => s.Split('=')[1].Trim()
        );

        // parse common params (use fallbacks if missing)
        int.TryParse(entries.GetValueOrDefault("ROW", "9"), out rowCount);
        int.TryParse(entries.GetValueOrDefault("COL", "9"), out columnCount);
        float.TryParse(entries.GetValueOrDefault("SIZEX", "4"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float sx);
        float.TryParse(entries.GetValueOrDefault("SIZEY", "2.5"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float sy);
        float.TryParse(entries.GetValueOrDefault("SIZEZ", "1"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float sz);
        float.TryParse(entries.GetValueOrDefault("CURVEX", "1.0"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out xCurveRatio);
        float.TryParse(entries.GetValueOrDefault("CURVEY", "1.0"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out yCurveRatio);
        float.TryParse(entries.GetValueOrDefault("MAXANGLE", "80"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out maxAngle);

        wallSize = new Vector3(sx, sy, sz);

        ComputePerimeterPoints();
        SceneView.RepaintAll();
    }

    private void ComputePerimeterPoints()
    {
        perimeterPoints.Clear();
        if (columnCount < 2 || rowCount < 2) return;

        if (mimicMeshEdges)
        {
            // Recreate WallGenerator's padded pointsList and rotationsList logic so the outline matches generated mesh (with shaved corners)
            int w = columnCount + 2;
            int h = rowCount + 2;
            Vector3[,] pointsList = new Vector3[w, h];
            Quaternion[,] rotationsList = new Quaternion[w, h];

            // Fill inner points (these correspond to AddPoint(x,y) -> stored at x+1,y+1 in WallGenerator)
            for (int x = 0; x < columnCount; x++)
            {
                for (int y = 0; y < rowCount; y++)
                {
                    pointsList[x + 1, y + 1] = DefineMolePos(x, y);
                    rotationsList[x + 1, y + 1] = DefineMoleRotation(x, y);
                }
            }

            // Now apply the same edge extrapolation rules as WallGenerator.GenerateWallMesh
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    // Edges
                    if (x == w - 1)
                    {
                        pointsList[x, y] = pointsList[x - 1, y] - (pointsList[x - 2, y] - pointsList[x - 1, y]);
                        rotationsList[x, y] = rotationsList[x - 1, y];
                    }

                    if (x == 0)
                    {
                        pointsList[x, y] = pointsList[x + 1, y] - (pointsList[x + 2, y] - pointsList[x + 1, y]);
                        rotationsList[x, y] = rotationsList[x + 1, y];
                    }

                    if (y == h - 1)
                    {
                        pointsList[x, y] = pointsList[x, y - 1] - (pointsList[x, y - 2] - pointsList[x, y - 1]);
                        rotationsList[x, y] = rotationsList[x, y - 1];
                    }

                    if (y == 0)
                    {
                        pointsList[x, y] = pointsList[x, y + 1] - (pointsList[x, y + 2] - pointsList[x, y + 1]);
                        rotationsList[x, y] = rotationsList[x, y + 1];
                    }

                    // Corners (match WallGenerator corner handling)
                    if (x == w - 1 && y == 0)
                    {
                        pointsList[x, y] = pointsList[x - 1, y + 1] - (pointsList[x - 2, y + 2] - pointsList[x - 1, y + 1]) / 2f;
                        rotationsList[x, y] = rotationsList[x - 1, y + 1];
                    }

                    if (x == 0 && y == 0)
                    {
                        pointsList[x, y] = pointsList[x + 1, y + 1] - (pointsList[x + 2, y + 2] - pointsList[x + 1, y + 1]) / 2f;
                        rotationsList[x, y] = rotationsList[x + 1, y + 1];
                    }

                    if (y == h - 1 && x == 0)
                    {
                        pointsList[x, y] = pointsList[x + 1, y - 1] - (pointsList[x + 2, y - 2] - pointsList[x + 1, y - 1]) / 2f;
                        rotationsList[x, y] = rotationsList[x + 1, y - 1];
                    }

                    if (x == w - 1 && y == h - 1)
                    {
                        pointsList[x, y] = pointsList[x - 1, y - 1] - (pointsList[x - 2, y - 2] - pointsList[x - 1, y - 1]) / 2f;
                        rotationsList[x, y] = rotationsList[x - 1, y - 1];
                    }
                }
            }

            // Build perimeter using same winding as WallGenerator.CreateOrUpdateOutline
            List<Vector3> perimeter = new List<Vector3>();
            // Top row: x=0..w-1 at y=h-1
            for (int x = 0; x < w; x++) perimeter.Add(pointsList[x, h - 1] + ((rotationsList[x, h - 1] * Vector3.forward) * meshRecoil));
            // Right column: y=h-2..0
            for (int y = h - 2; y >= 0; y--) perimeter.Add(pointsList[w - 1, y] + ((rotationsList[w - 1, y] * Vector3.forward) * meshRecoil));
            // Bottom row: x=w-2..0 at y=0
            for (int x = w - 2; x >= 0; x--) perimeter.Add(pointsList[x, 0] + ((rotationsList[x, 0] * Vector3.forward) * meshRecoil));
            // Left column: y=1..h-2
            for (int y = 1; y <= h - 2; y++) perimeter.Add(pointsList[0, y] + ((rotationsList[0, y] * Vector3.forward) * meshRecoil));

            // apply local preview offset
            for (int i = 0; i < perimeter.Count; i++) perimeter[i] += previewOffset;

            perimeterPoints = perimeter;
            return;
        }

        // Fallback: sample each edge from DefineMolePos fractional positions (previous behavior)
        int samples = Mathf.Max(2, edgeSampleCount);
        float maxX = (float)(columnCount - 1);
        float maxY = (float)(rowCount - 1);

        // Top edge: x from 0 -> maxX at y = maxY
        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            float x = t * maxX;
            perimeterPoints.Add(DefineMolePos(x, maxY));
        }

        // Right edge: y from maxY -> 0 (exclude top-right corner to avoid duplicate)
        for (int i = 1; i <= samples; i++)
        {
            float t = i / (float)samples;
            float y = maxY - t * maxY;
            perimeterPoints.Add(DefineMolePos(maxX, y));
        }

        // Bottom edge: x from maxX -> 0 (exclude bottom-right corner)
        for (int i = 1; i <= samples; i++)
        {
            float t = i / (float)samples;
            float x = maxX - t * maxX;
            perimeterPoints.Add(DefineMolePos(x, 0f));
        }

        // Left edge: y from 0 -> maxY (exclude bottom-left and top-left corners)
        for (int i = 1; i < samples; i++)
        {
            float t = i / (float)samples;
            float y = t * maxY;
            perimeterPoints.Add(DefineMolePos(0f, y));
        }

        // apply local offset
        for (int i = 0; i < perimeterPoints.Count; i++) perimeterPoints[i] += previewOffset;
    }

    // Copy of WallGenerator.DefineMolePos math (approx). Accepts fractional indices so edges can be sampled smoothly.
    private Vector3 DefineMolePos(float xIndex, float yIndex)
    {
        float angleX = ((((float)xIndex / (columnCount - 1)) * 2) - 1) * ((Mathf.PI * xCurveRatio) / 2);
        float angleY = ((((float)yIndex / (rowCount - 1)) * 2) - 1) * ((Mathf.PI * yCurveRatio) / 2);

        float x = Mathf.Sin(angleX) * (wallSize.x / (2f * xCurveRatio));
        float y = Mathf.Sin(angleY) * (wallSize.y / (2f * yCurveRatio));
        float z = ((Mathf.Cos(angleY) * (wallSize.z)) + (Mathf.Cos(angleX) * (wallSize.z)));
        return new Vector3(x, y, z);
    }

    // Copy of WallGenerator.DefineMoleRotation math (approx)
    private Quaternion DefineMoleRotation(int xIndex, int yIndex)
    {
        Quaternion lookAngle = new Quaternion();
        lookAngle.eulerAngles = new Vector3(-((((float)yIndex / (rowCount - 1)) * 2) - 1) * (maxAngle * yCurveRatio), ((((float)xIndex / (columnCount - 1)) * 2) - 1) * (maxAngle * xCurveRatio), 0f);
        return lookAngle;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!showPerimeter || perimeterPoints == null || perimeterPoints.Count == 0) return;

        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        Handles.color = Color.cyan;
        // transform points to world with referenceTransform if provided
        Vector3[] worldPts = perimeterPoints.Select(p => referenceTransform != null ? referenceTransform.TransformPoint(p) : p).ToArray();

        // draw closed polygon
        Handles.DrawAAPolyLine(4f, worldPts.Concat(new[] { worldPts[0] }).ToArray());

        // small spheres on vertices
        for (int i = 0; i < worldPts.Length; i++)
        {
            Handles.DotHandleCap(0, worldPts[i], Quaternion.identity, HandleUtility.GetHandleSize(worldPts[i]) * 0.05f, EventType.Repaint);
            if (sceneView.in2DMode == false)
                Handles.Label(worldPts[i] + Vector3.up * 0.02f, i.ToString());
        }
    }
}
