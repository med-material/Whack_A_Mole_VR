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
    // 10° equivalent at a 2 m board distance is tan(10°) * 2 m ≈ 0.35 m.
    public Vector3 offsetWorld = new Vector3(0.35f, 0f, 0f);

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
    // Match the common 10° prism-equivalent magnitude used in several VR/AR PA studies.
    public float rotationDegrees = 10f;
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
// - TransformPose shifts the apparent effector pose.
// - TransformRay shifts the effective aim origin by the same amount when enabled.
//
// In other words, Skew represents the hand/controller as being in a displaced
// location, and the aim ray follows that displaced location.
public class SkewEffect : IEffectTransform
{
    public enum SkewUnits { Meters, Degrees, PrismDiopters }

    [Header("Visual Prism Shift")]
    public SkewUnits units = SkewUnits.Meters;

    [Tooltip("Meters: direct visual shift.\nDegrees: converted at reference distance.\nPrismDiopters: converted at reference distance.")]
    // Default to a 10° prism-equivalent visual shift.
    public float value = 10f;

    [Tooltip("Reference distance to target plane (e.g. board) in metres.")]
    public float referenceDistanceMeters = 2.0f;

    [Tooltip("Visible controller/pointer visual to shift. This should be the visual model child, not the tracked controller root.")]
    [System.NonSerialized] public Transform visualPointerRoot;
    [System.NonSerialized] public List<Transform> visualAuxRoots = new List<Transform>();
    [System.NonSerialized] public bool shiftInputPoseAndRay = true;

    [Tooltip("Stable prism shift direction in WORLD space.")]
    public Vector3 worldShiftAxis = Vector3.right;

    Vector3 _originalPointerLocalPosition;
    readonly List<Vector3> _originalAuxLocalPositions = new List<Vector3>();
    bool _hasOriginalPointerLocalPosition = false;

    // Visible cues and visible pointer markers can be shifted so the participant
    // perceives a displaced visual scene.
    public Pose TransformPose(Pose rawPose)
    {
        if (!shiftInputPoseAndRay)
            return rawPose;

        // Shift the visible pose so markers / visible cues can appear shifted.
        Vector3 shift = GetShiftVector();
        return new Pose(rawPose.position + shift, rawPose.rotation);
    }

    // The skew effect offsets both the visible effector and the effective aiming ray.
    // The direction is unchanged; only the apparent hand/controller position moves.
    public Ray TransformRay(Ray rawRay)
    {
        if (!shiftInputPoseAndRay)
            return rawRay;

        Vector3 shift = GetShiftVector();
        return new Ray(rawRay.origin + shift, rawRay.direction);
    }

    // Applies the visual prism shift to scene elements.
    //
    // Two things can move visually:
    // 1. the visible pointer/controller model,
    // 2. extra auxiliary controller visuals such as poke/direct-interactor objects.
    //
    // The original positions are cached the first time so ResetCameraEffect can put them back.
    public void ApplyCameraEffect(Camera cam)
    {
        Vector3 shift = GetShiftVector();

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

                // If an auxiliary visual already lives under the main shifted pointer root,
                // moving the parent is enough. Applying the same shift again here would
                // double-offset child visuals such as attached beams or direct-interactor cues.
                if (visualPointerRoot != null && aux.IsChildOf(visualPointerRoot))
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
        if (visualPointerRoot != null && _hasOriginalPointerLocalPosition)
            visualPointerRoot.localPosition = _originalPointerLocalPosition;

        if (visualAuxRoots != null && _originalAuxLocalPositions.Count > 0)
        {
            for (int i = 0; i < visualAuxRoots.Count && i < _originalAuxLocalPositions.Count; i++)
            {
                if (visualAuxRoots[i] != null)
                {
                    if (visualPointerRoot != null && visualAuxRoots[i].IsChildOf(visualPointerRoot))
                        continue;

                    visualAuxRoots[i].localPosition = _originalAuxLocalPositions[i];
                }
            }
        }

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

    // Expose the currently configured visual shift vector so other systems can reason
    // about the pointer-only visual offset without duplicating the skew conversion math.
    public Vector3 GetConfiguredShiftVector()
    {
        return GetShiftVector();
    }

    public bool TryGetVisualPointerWorldDelta(out Vector3 delta)
    {
        delta = Vector3.zero;

        if (visualPointerRoot == null || !_hasOriginalPointerLocalPosition)
            return false;

        Vector3 originalWorldPosition = visualPointerRoot.parent != null
            ? visualPointerRoot.parent.TransformPoint(_originalPointerLocalPosition)
            : _originalPointerLocalPosition;

        delta = visualPointerRoot.position - originalWorldPosition;
        return true;
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
