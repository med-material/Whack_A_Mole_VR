using UnityEngine;
using System.Collections.Generic;

public interface IInputProvider
{
    Pose GetPointerPose();
    Ray GetPointerRay();
    bool ConfirmPressedThisFrame();
}

public interface IEffectTransform
{
    Pose TransformPose(Pose rawPose);
    Ray TransformRay(Ray rawRay);
    void ApplyCameraEffect(Camera cam);
    void ResetCameraEffect(Camera cam);
}

public class NoEffect : IEffectTransform
{
    public Pose TransformPose(Pose rawPose) => rawPose;
    public Ray TransformRay(Ray rawRay) => rawRay;
    public void ApplyCameraEffect(Camera cam) { }
    public void ResetCameraEffect(Camera cam) { }
}

[System.Serializable]
public class TranslationEffect : IEffectTransform
{
    public Vector3 offsetWorld = new Vector3(0.12f, 0f, 0f);

    public Pose TransformPose(Pose rawPose)
        => new Pose(rawPose.position + offsetWorld, rawPose.rotation);

    public Ray TransformRay(Ray rawRay)
        => new Ray(rawRay.origin + offsetWorld, rawRay.direction);

    public void ApplyCameraEffect(Camera cam) { }
    public void ResetCameraEffect(Camera cam) { }
}

[System.Serializable]
public class RotationEffect : IEffectTransform
{
    public float rotationDegrees = 20f;
    public Vector3 axisWorld = Vector3.up;

    public Pose TransformPose(Pose rawPose)
    {
        var q = Quaternion.AngleAxis(rotationDegrees, axisWorld.normalized);
        return new Pose(rawPose.position, q * rawPose.rotation);
    }

    public Ray TransformRay(Ray rawRay)
    {
        var q = Quaternion.AngleAxis(rotationDegrees, axisWorld.normalized);
        return new Ray(rawRay.origin, q * rawRay.direction);
    }

    public void ApplyCameraEffect(Camera cam) { }
    public void ResetCameraEffect(Camera cam) { }
}

[System.Serializable]
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

    public Pose TransformPose(Pose rawPose)
    {
        // Shift the visible pose so markers / visible cues can appear shifted.
        Vector3 shift = GetShiftVector();
        return new Pose(rawPose.position + shift, rawPose.rotation);
    }

    public Ray TransformRay(Ray rawRay)
    {
        // Keep scoring / interaction ray real and unchanged.
        return rawRay;
    }

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

    Vector3 GetShiftVector()
    {
        float shiftMeters = ComputeShiftMeters();

        Vector3 axis = worldShiftAxis;
        if (axis.sqrMagnitude < 1e-6f)
            axis = Vector3.right;

        axis.Normalize();
        return axis * shiftMeters;
    }

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
