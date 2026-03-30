using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
// DepthQueGradient automatically colors the child stripes of the DepthQue parent
// so they form a depth cue from "near the participant" to "far toward the wall".
//
// In practical terms:
// each child cube gets a slightly different shade, creating a runway-like gradient
// that helps the participant judge distance toward the task board.
//
// The script is intentionally simple:
// - collect all child objects,
// - optionally sort them from near to far,
// - compute a color for each child,
// - apply that color to the child's renderer.
//
// ExecuteAlways is important here because the user wanted the gradient to be visible
// and editable directly in the Unity editor, not only while the scene is running.
public class DepthQueGradient : MonoBehaviour
{
    [Header("Gradient")]
    [SerializeField] private Color nearColor = new Color(0.92f, 0.92f, 0.92f, 1f);
    [SerializeField] private Color farColor = new Color(0.28f, 0.28f, 0.28f, 1f);
    [SerializeField, Tooltip("If enabled, children are sorted by local Z before coloring. Otherwise sibling order is used.")]
    private bool sortByLocalZ = true;
    [SerializeField, Tooltip("Reverse the gradient direction if your furthest stripe should be lighter instead of darker.")]
    private bool invertGradient = false;
    [SerializeField, Min(0f), Tooltip("Extra gamma-style contrast for the gradient. 1 = linear.")]
    private float gradientExponent = 1f;

    [Header("Material Handling")]
    [SerializeField, Tooltip("Use shared materials so the editor updates persist without instantiating new materials per child.")]
    private bool useSharedMaterial = true;

    // Apply the gradient immediately when the object first becomes active.
    private void Awake()
    {
        ApplyGradient();
    }

    // Re-apply when the component is enabled, for example after toggling it in the inspector.
    private void OnEnable()
    {
        ApplyGradient();
    }

    // Re-apply whenever an inspector value changes.
    // This gives immediate visual feedback while adjusting colors, ordering, or contrast.
    private void OnValidate()
    {
        ApplyGradient();
    }

    [ContextMenu("Apply Depth Gradient")]
    // Core method of the script.
    //
    // This is the part that actually assigns the near-to-far colors.
    // Read it as five steps:
    // 1. gather the child stripes,
    // 2. optionally sort them by depth,
    // 3. compute where each stripe sits in the 0-to-1 gradient,
    // 4. convert that position into a color,
    // 5. write that color into the child's material.
    public void ApplyGradient()
    {
        int childCount = transform.childCount;
        // If the parent has no children, there is nothing to color.
        if (childCount == 0)
            return;

        // Build a local list of all direct children so we can sort and process them.
        Transform[] children = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
            children[i] = transform.GetChild(i);

        if (sortByLocalZ)
        {
            // Sorting by local Z lets the script infer which stripe is nearer/further
            // based on how the user laid them out in the scene.
            System.Array.Sort(children, (a, b) => a.localPosition.z.CompareTo(b.localPosition.z));
        }

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            // Defensive checks: skip missing children or children that do not render anything.
            if (child == null)
                continue;

            Renderer rendererComponent = child.GetComponent<Renderer>();
            if (rendererComponent == null)
                continue;

            // t is the stripe's normalized position inside the gradient:
            // 0 means "use nearColor", 1 means "use farColor".
            // If there is only one child, it is treated as fully far to avoid division by zero.
            float t = children.Length == 1 ? 1f : (float)i / (children.Length - 1);
            if (invertGradient)
                // Sometimes the scene is arranged in the opposite direction.
                // This flips the gradient without requiring the user to reorder the objects.
                t = 1f - t;

            // gradientExponent works like a contrast control for the gradient.
            // 1 = normal linear blend.
            // >1 compresses the lighter end and makes the darker end change more aggressively.
            // <1 does the opposite.
            t = Mathf.Pow(Mathf.Clamp01(t), Mathf.Max(0.0001f, gradientExponent));
            Color targetColor = Color.Lerp(nearColor, farColor, t);

            // In edit mode we use sharedMaterial to avoid Unity creating a unique material
            // instance for every stripe. That is safer for editor use and prevents material leaks.
            // At runtime the user can optionally switch to per-instance materials if needed.
            bool shouldUseSharedMaterial = useSharedMaterial || !Application.isPlaying;

            if (shouldUseSharedMaterial)
            {
                Material material = rendererComponent.sharedMaterial;
                if (material != null)
                    material.color = targetColor;
            }
            else
            {
                Material material = rendererComponent.material;
                if (material != null)
                    material.color = targetColor;
            }
        }
    }
}
