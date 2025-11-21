using System.Collections;
using UnityEngine;
using UnityEngine.Animations;

// Minimal helper to snap a mole (or any object) to a hand joint anchor using a ParentConstraint.
// Provides a coroutine to wait until snap (blend) completes so callers can delay state progression.
[DisallowMultipleComponent]
public class KeyMoleSnapHelper : MonoBehaviour
{
    [Header("Anchor Lookup")]
    [SerializeField] private Transform explicitAnchor;
    [SerializeField] private string anchorTag = "KeyAnchor";

    [Header("Constraint Settings")]
    [SerializeField] private ParentConstraint parentConstraint; // optional; will be auto-added if missing
    [SerializeField] private bool addConstraintIfMissing = true;

    [Header("Offsets (relative to the anchor)")]
    [SerializeField] private bool zeroOffsets = true;
    [SerializeField] private Vector3 localPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 localRotationOffsetEuler = Vector3.zero;
    [SerializeField] private bool keepConstraintActive = true;

    [Header("Blend")]
    [SerializeField] private float blendInDuration = 0f;
    [SerializeField] private AnimationCurve blendCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private int sourceIndex = -1;
    private Coroutine blendRoutine;
    private bool snappingActive = false; // True while blendRoutine running

    public bool IsSnapping => snappingActive;
    public float GetSnapDuration() => blendInDuration;

    public void SnapNow()
    {
        Transform anchor = ResolveAnchor();
        if (anchor == null)
        {
            Debug.LogWarning("[MoleSnapHelper] No anchor found (explicitAnchor null and no object with tag '" + anchorTag + "').");
            return;
        }

        if (parentConstraint == null)
        {
            parentConstraint = GetComponent<ParentConstraint>();
            if (parentConstraint == null && addConstraintIfMissing)
            {
                parentConstraint = gameObject.AddComponent<ParentConstraint>();
            }
        }

        if (parentConstraint == null)
        {
            Debug.LogWarning("[MoleSnapHelper] No ParentConstraint available and auto-add is disabled.");
            return;
        }

        parentConstraint.constraintActive = false;
        parentConstraint.locked = false;
        parentConstraint.SetSources(new System.Collections.Generic.List<ConstraintSource>()); // clear

        ConstraintSource src = new ConstraintSource { sourceTransform = anchor, weight = 1f };
        sourceIndex = parentConstraint.AddSource(src);

        if (zeroOffsets)
        {
            parentConstraint.SetTranslationOffset(sourceIndex, Vector3.zero);
            parentConstraint.SetRotationOffset(sourceIndex, Vector3.zero);
        }
        else
        {
            parentConstraint.SetTranslationOffset(sourceIndex, localPositionOffset);
            parentConstraint.SetRotationOffset(sourceIndex, localRotationOffsetEuler);
        }

        if (blendRoutine != null)
        {
            StopCoroutine(blendRoutine);
            blendRoutine = null;
        }

        if (blendInDuration > 0f)
        {
            parentConstraint.weight = 0f;
            parentConstraint.constraintActive = true;
            snappingActive = true;
            blendRoutine = StartCoroutine(BlendConstraintWeight(1f, blendInDuration));
        }
        else
        {
            parentConstraint.weight = 1f;
            parentConstraint.constraintActive = true;
            snappingActive = false; // instantaneous
        }

        if (keepConstraintActive)
        {
            parentConstraint.locked = true;
        }
        else
        {
            StartCoroutine(DisableAfterFrame());
        }
    }

    // Allows external callers (InteractiveMole) to wait until snap completes.
    public IEnumerator WaitForSnapCompletion()
    {
        if (!snappingActive) yield break;
        while (snappingActive)
        {
            yield return null;
        }
    }

    public void Release()
    {
        if (parentConstraint != null)
        {
            parentConstraint.constraintActive = false;
            parentConstraint.locked = false;
        }
        if (blendRoutine != null)
        {
            StopCoroutine(blendRoutine);
            blendRoutine = null;
        }
        snappingActive = false;
    }

    private Transform ResolveAnchor()
    {
        if (explicitAnchor != null) return explicitAnchor;
        if (!string.IsNullOrEmpty(anchorTag))
        {
            GameObject go = GameObject.FindWithTag(anchorTag);
            if (go != null) return go.transform;
        }
        return null;
    }

    private IEnumerator BlendConstraintWeight(float target, float duration)
    {
        float start = parentConstraint.weight;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
            float eased = blendCurve != null ? blendCurve.Evaluate(k) : k;
            parentConstraint.weight = Mathf.LerpUnclamped(start, target, eased);
            yield return null;
        }
        parentConstraint.weight = target;
        blendRoutine = null;
        snappingActive = false;
    }

    private IEnumerator DisableAfterFrame()
    {
        yield return null;
        if (parentConstraint != null)
        {
            parentConstraint.constraintActive = false;
            parentConstraint.locked = false;
        }
    }
}
