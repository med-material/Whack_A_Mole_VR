using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
// RoundedRectGraphic is a custom UI graphic used to draw a filled rounded rectangle.
//
// Unity's standard UI Image is easy to use, but this project needed a lightweight
// procedural shape for the dwell/progress bar. Instead of depending on a sprite,
// this class builds the rectangle mesh directly in code.
//
// In plain language:
// - if the corners should be square, draw one normal rectangle,
// - if the corners should be rounded, approximate the rounded outline with small arc segments,
// - then fill the whole shape with triangles.
public class RoundedRectGraphic : MaskableGraphic
{
    [SerializeField] private float cornerRadius = 10f;
    [SerializeField] private int cornerSegments = 6;

    // Public property used by other scripts, such as HandDwellProgressBar,
    // to change how rounded the box should be.
    //
    // SetVerticesDirty() tells Unity:
    // "the geometry has changed, rebuild this UI mesh on the next update."
    public float CornerRadius
    {
        get => cornerRadius;
        set
        {
            cornerRadius = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    // This is the main drawing method for a MaskableGraphic.
    //
    // Unity calls this when the UI mesh needs to be rebuilt.
    // The VertexHelper is the object we write our custom shape into.
    //
    // The method works in three stages:
    // 1. decide whether we need a plain rectangle or a rounded one,
    // 2. create the required vertices,
    // 3. connect those vertices into triangles so the shape can be rendered.
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        // Start from a clean mesh every time.
        vh.Clear();

        // Get the current rectangle this UI element occupies on screen.
        Rect rect = GetPixelAdjustedRect();
        float width = rect.width;
        float height = rect.height;
        // If the UI element has no visible size, there is nothing to draw.
        if (width <= 0f || height <= 0f)
            return;

        // Limit the radius so it can never become larger than half the width/height.
        // Otherwise the arcs would overlap and produce invalid geometry.
        float radius = Mathf.Min(cornerRadius, width * 0.5f, height * 0.5f);
        if (radius <= 0.01f)
        {
            // If the radius is basically zero, drawing a normal quad is simpler and cleaner.
            AddQuad(vh, rect.min, rect.max, color);
            return;
        }

        // cornerSegments controls how smooth the rounded corners look.
        // More segments = smoother corners, but also more vertices.
        int segments = Mathf.Max(1, cornerSegments);
        Vector2 center = rect.center;
        // The center vertex is used as the common anchor for the triangle fan.
        AddVertex(vh, center, color);

        // Build the outline by tracing the four corner arcs in order around the rectangle.
        // The first arc includes its first vertex; the next ones skip their first vertex
        // so we do not duplicate points where two arcs meet.
        AddCornerArc(vh, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f, segments, true);
        AddCornerArc(vh, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f, segments, false);
        AddCornerArc(vh, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f, segments, false);
        AddCornerArc(vh, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f, segments, false);

        // Fill the rounded outline using a triangle fan:
        // every triangle starts from the center and connects to two neighboring edge vertices.
        for (int i = 1; i < vh.currentVertCount - 1; i++)
            vh.AddTriangle(0, i, i + 1);

        // Close the loop by connecting the final edge vertex back to the first one.
        vh.AddTriangle(0, vh.currentVertCount - 1, 1);
    }

    // Small helper that adds one colored vertex to the UI mesh.
    static void AddVertex(VertexHelper vh, Vector2 position, Color32 vertexColor)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = vertexColor;
        vertex.position = position;
        vh.AddVert(vertex);
    }

    // Convenience method for the simple "no rounded corners" case.
    // It writes four corner vertices and two triangles, which together form a rectangle.
    static void AddQuad(VertexHelper vh, Vector2 min, Vector2 max, Color32 vertexColor)
    {
        int startIndex = vh.currentVertCount;
        AddVertex(vh, new Vector2(min.x, min.y), vertexColor);
        AddVertex(vh, new Vector2(min.x, max.y), vertexColor);
        AddVertex(vh, new Vector2(max.x, max.y), vertexColor);
        AddVertex(vh, new Vector2(max.x, min.y), vertexColor);
        vh.AddTriangle(startIndex + 0, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex + 0, startIndex + 2, startIndex + 3);
    }

    // Generates one rounded corner as a short arc.
    //
    // arcCenter is the center of the imaginary circle the arc belongs to.
    // startDegrees/endDegrees define which quarter of that circle should be traced.
    // segments decides how many straight edges are used to approximate the curve.
    //
    // The "includeFirstVertex" flag avoids duplicate vertices where neighboring arcs touch.
    void AddCornerArc(VertexHelper vh, Vector2 arcCenter, float radius, float startDegrees, float endDegrees, int segments, bool includeFirstVertex)
    {
        for (int i = 0; i <= segments; i++)
        {
            if (!includeFirstVertex && i == 0)
                continue;

            // Interpolate between the start and end angle, then convert that angle
            // into a 2D point on the arc using cosine/sine.
            float t = i / (float)segments;
            float angleRadians = Mathf.Lerp(startDegrees, endDegrees, t) * Mathf.Deg2Rad;
            Vector2 point = new Vector2(
                arcCenter.x + Mathf.Cos(angleRadians) * radius,
                arcCenter.y + Mathf.Sin(angleRadians) * radius);
            AddVertex(vh, point, color);
        }
    }
}
