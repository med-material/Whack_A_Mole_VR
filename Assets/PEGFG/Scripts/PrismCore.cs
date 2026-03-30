using UnityEngine;
using System.Collections.Generic;

// Generic input interface.
//
// The idea is that the rest of the experiment should not need to care exactly
// where the pointing input comes from. A controller, a tracked hand, or another
// future device can all implement the same three basic capabilities:
// pose, ray, and confirm action.
public interface IInputProvider
{
    Pose GetPointerPose();
    Ray GetPointerRay();
    bool ConfirmPressedThisFrame();
}

// Common interface for all perturbation/effect implementations.
//
// Each effect answers the same four questions:
// - how should a raw pose be transformed?
// - how should a raw ray be transformed?
// - what ongoing visual change should be applied to the scene/camera?
// - how should that visual change be undone again?
//
// This is what makes it easy for SandboxRunner to switch between different effect types.
public interface IEffectTransform
{
    Pose TransformPose(Pose rawPose);
    Ray TransformRay(Ray rawRay);
    void ApplyCameraEffect(Camera cam);
    void ResetCameraEffect(Camera cam);
}

// The neutral effect.
// It exists so the system can still use the same effect interface even when
// no perturbation is meant to happen.
public class NoEffect : IEffectTransform
{
    public Pose TransformPose(Pose rawPose) => rawPose;
    public Ray TransformRay(Ray rawRay) => rawRay;
    public void ApplyCameraEffect(Camera cam) { }
    public void ResetCameraEffect(Camera cam) { }
}

[System.Serializable]
// TranslationEffect applies a fixed world-space offset.
//
// In plain language:
// everything appears shifted sideways/up/down/forward by a constant amount.
// The pointing direction itself is not rotated; only the position is displaced.
public class TranslationEffect : IEffectTransform
{
    public Vector3 offsetWorld = new Vector3(0.12f, 0f, 0f);

    // The input pose is translated in world space, but its rotation stays the same.
    public Pose TransformPose(Pose rawPose)
        => new Pose(rawPose.position + offsetWorld, rawPose.rotation);

    // The input ray origin is translated as well, while the ray direction stays unchanged.
    public Ray TransformRay(Ray rawRay)
        => new Ray(rawRay.origin + offsetWorld, rawRay.direction);

    // Translation is implemented directly in the transformed pose/ray,
    // so no extra camera/world visual shift is needed here.
    public void ApplyCameraEffect(Camera cam) { }
    public void ResetCameraEffect(Camera cam) { }
}

[System.Serializable]
// RotationEffect rotates the pointing direction around a chosen world-space axis.
//
// Unlike TranslationEffect, this does not move the pointer origin.
// Instead, it changes the direction/orientation of the input.
public class RotationEffect : IEffectTransform
{
    public float rotationDegrees = 20f;
    public Vector3 axisWorld = Vector3.up;

    // The pose keeps the same position but its rotation is turned by the chosen angle.
    public Pose TransformPose(Pose rawPose)
    {
        var q = Quaternion.AngleAxis(rotationDegrees, axisWorld.normalized);
        return new Pose(rawPose.position, q * rawPose.rotation);
    }

    // The ray origin stays the same, but its direction is rotated.
    public Ray TransformRay(Ray rawRay)
    {
        var q = Quaternion.AngleAxis(rotationDegrees, axisWorld.normalized);
        return new Ray(rawRay.origin, q * rawRay.direction);
    }

    // As with TranslationEffect, the perturbation is encoded in the transformed input itself.
    public void ApplyCameraEffect(Camera cam) { }
    public void ResetCameraEffect(Camera cam) { }
}

[System.Serializable]
// SkewEffect is the most visually oriented perturbation in this project.
//
// Important conceptual difference:
// - TransformPose shifts the *visible* pose so markers/cues can appear displaced.
// - TransformRay leaves the scoring ray unchanged.
//
// In other words, Skew is mainly a visual prism-style shift rather than a change
// to the actual interaction ray used for hit detection.
public class SkewEffect : IEffectTransform
{
    public enum SkewUnits { Meters, Degrees, PrismDiopters }

    [Header("Visual Prism Shift")]
    public SkewUnits units = SkewUnits.Meters;

    [Tooltip("Meters: direct visual shift.\nDegrees: converted at reference distance.\nPrismDiopters: converted at reference distance.")]
    public float value = 0.10f;

    [Tooltip("Reference distance to target plane (e.g. board) in metres.")]
    public float referenceDistanceMeters = 2.0f;

    [Tooltip("Visible world root to shift.")]
    [System.NonSerialized] public Transform visualWorldRoot;

    [Tooltip("Visible controller/pointer visual to shift. This should be the visual model child, not the tracked controller root.")]
    [System.NonSerialized] public Transform visualPointerRoot;
    [System.NonSerialized] public List<Transform> visualAuxRoots = new List<Transform>();

    [Tooltip("Stable prism shift direction in WORLD space.")]
    public Vector3 worldShiftAxis = Vector3.right;

    Vector3 _originalWorldPosition;
    Vector3 _originalPointerLocalPosition;
    readonly List<Vector3> _originalAuxLocalPositions = new List<Vector3>();
    bool _hasOriginalWorldPosition = false;
    bool _hasOriginalPointerLocalPosition = false;

    // Visible cues and visible pointer markers can be shifted so the participant
    // perceives a displaced visual scene.
    public Pose TransformPose(Pose rawPose)
    {
        // Shift the visible pose so markers / visible cues can appear shifted.
        Vector3 shift = GetShiftVector();
        return new Pose(rawPose.position + shift, rawPose.rotation);
    }

    // The interaction/scoring ray is intentionally kept real.
    // This lets the project separate what is *seen* from what is *physically scored*.
    public Ray TransformRay(Ray rawRay)
    {
        // Keep scoring / interaction ray real and unchanged.
        return rawRay;
    }

    // Applies the visual prism shift to scene elements.
    //
    // Three things can move visually:
    // 1. the visible world root,
    // 2. the visible pointer/controller model,
    // 3. extra auxiliary controller visuals such as poke/direct-interactor objects.
    //
    // The original positions are cached the first time so ResetCameraEffect can put them back.
    public void ApplyCameraEffect(Camera cam)
    {
        Vector3 shift = GetShiftVector();

        // Shift the visible world in world space.
        if (visualWorldRoot != null)
        {
            if (!_hasOriginalWorldPosition)
            {
                _originalWorldPosition = visualWorldRoot.position;
                _hasOriginalWorldPosition = true;
            }

            visualWorldRoot.position = _originalWorldPosition + shift;
        }

        // Shift the visible controller MODEL relative to its tracked parent.
        if (visualPointerRoot != null)
        {
            if (!_hasOriginalPointerLocalPosition)
            {
                _originalPointerLocalPosition = visualPointerRoot.localPosition;
                _hasOriginalPointerLocalPosition = true;
            }

            Vector3 localShift = visualPointerRoot.parent != null
                ? visualPointerRoot.parent.InverseTransformVector(shift)
                : shift;

            visualPointerRoot.localPosition = _originalPointerLocalPosition + localShift;
        }

        if (visualAuxRoots != null && visualAuxRoots.Count > 0)
        {
            if (_originalAuxLocalPositions.Count != visualAuxRoots.Count)
            {
                _originalAuxLocalPositions.Clear();
                for (int i = 0; i < visualAuxRoots.Count; i++)
                {
                    Transform aux = visualAuxRoots[i];
                    _originalAuxLocalPositions.Add(aux != null ? aux.localPosition : Vector3.zero);
                }
            }

            for (int i = 0; i < visualAuxRoots.Count; i++)
            {
                Transform aux = visualAuxRoots[i];
                if (aux == null)
                    continue;

                Vector3 auxLocalShift = aux.parent != null
                    ? aux.parent.InverseTransformVector(shift)
                    : shift;

                aux.localPosition = _originalAuxLocalPositions[i] + auxLocalShift;
            }
        }
    }

    // Restores any visual objects moved by ApplyCameraEffect() to their original positions.
    // This is critical because otherwise old effect offsets would leak into later phases.
    public void ResetCameraEffect(Camera cam)
    {
        if (visualWorldRoot != null && _hasOriginalWorldPosition)
            visualWorldRoot.position = _originalWorldPosition;

        if (visualPointerRoot != null && _hasOriginalPointerLocalPosition)
            visualPointerRoot.localPosition = _originalPointerLocalPosition;

        if (visualAuxRoots != null && _originalAuxLocalPositions.Count > 0)
        {
            for (int i = 0; i < visualAuxRoots.Count && i < _originalAuxLocalPositions.Count; i++)
            {
                if (visualAuxRoots[i] != null)
                    visualAuxRoots[i].localPosition = _originalAuxLocalPositions[i];
            }
        }

        _hasOriginalWorldPosition = false;
        _hasOriginalPointerLocalPosition = false;
        _originalAuxLocalPositions.Clear();
    }

    // Converts the current skew setting into a concrete world-space shift vector.
    // First compute the amount in meters, then apply it along the chosen world axis.
    Vector3 GetShiftVector()
    {
        float shiftMeters = ComputeShiftMeters();

        Vector3 axis = worldShiftAxis;
        if (axis.sqrMagnitude < 1e-6f)
            axis = Vector3.right;

        axis.Normalize();
        return axis * shiftMeters;
    }

    // Converts the user-facing skew value into meters.
    //
    // Supported interpretations:
    // - Meters: already a direct translation value.
    // - Degrees: convert angular deviation into linear displacement at a chosen distance.
    // - PrismDiopters: convert prism diopters into displacement using the standard
    //   approximation displacement = distance * (diopters / 100).
    float ComputeShiftMeters()
    {
        switch (units)
        {
            case SkewUnits.Meters:
                return value;

            case SkewUnits.Degrees:
                return referenceDistanceMeters * Mathf.Tan(value * Mathf.Deg2Rad);

            case SkewUnits.PrismDiopters:
                return referenceDistanceMeters * (value / 100f);

            default:
                return value;
        }
    }
}
