using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
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

    private void Awake()
    {
        ApplyGradient();
    }

    private void OnEnable()
    {
        ApplyGradient();
    }

    private void OnValidate()
    {
        ApplyGradient();
    }

    [ContextMenu("Apply Depth Gradient")]
    public void ApplyGradient()
    {
        int childCount = transform.childCount;
        if (childCount == 0)
            return;

        Transform[] children = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
            children[i] = transform.GetChild(i);

        if (sortByLocalZ)
        {
            System.Array.Sort(children, (a, b) => a.localPosition.z.CompareTo(b.localPosition.z));
        }

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null)
                continue;

            Renderer rendererComponent = child.GetComponent<Renderer>();
            if (rendererComponent == null)
                continue;

            float t = children.Length == 1 ? 1f : (float)i / (children.Length - 1);
            if (invertGradient)
                t = 1f - t;

            t = Mathf.Pow(Mathf.Clamp01(t), Mathf.Max(0.0001f, gradientExponent));
            Color targetColor = Color.Lerp(nearColor, farColor, t);

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
