using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
SpawnCircleVFX
- Pooled circle expansion effect using LineRenderer with echo waves.
- Can be triggered via UnityEvent with Play() or Play(color, startRadius, endRadius, duration, echoes, echoDelay, width).
Usage:
- Attach to a GameObject (usually a child of the mole).
- Wire Play() to InteractiveMole's onMoleSpawnEvent or onMoleSpawnWithValidationEvent in the Inspector.
- Customize default values in the Inspector or pass explicit parameters.
*/
[DisallowMultipleComponent]
public class SpawnCircleVFX : MonoBehaviour
{
    [Header("Line settings")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private int segments = 64;
    [SerializeField] private float defaultWidth = 0.02f;

    [Header("Defaults (used when Play is called without explicit values)")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private float defaultStartRadius = 0.05f;
    [SerializeField] private float defaultEndRadius = 0.6f;
    [SerializeField] private float defaultDuration = 0.8f;
    [SerializeField] private int defaultEchoes = 3;
    [SerializeField] private float defaultEchoDelay = 0.12f;

    private readonly List<LineRenderer> pool = new List<LineRenderer>();

    // Parameterless overload for easy Inspector wiring
    public void Play() => Play(null, null, null, null, null, null, null);

    public void Play(
        Color? color = null,
        float? startRadius = null,
        float? endRadius = null,
        float? duration = null,
        int? echoes = null,
        float? echoDelay = null,
        float? width = null
    )
    {
        Color col = color ?? defaultColor;
        float sR = startRadius ?? defaultStartRadius;
        float eR = endRadius ?? defaultEndRadius;
        float dur = duration ?? defaultDuration;
        int echos = echoes ?? defaultEchoes;
        float eDelay = echoDelay ?? defaultEchoDelay;
        float w = width ?? defaultWidth;

        StartCoroutine(PlaySequenceRoutine(col, sR, eR, dur, echos, eDelay, w));
    }

    public void StopAll()
    {
        StopAllCoroutines();
        foreach (LineRenderer lr in pool)
        {
            if (lr != null) lr.enabled = false;
        }
    }

    private IEnumerator PlaySequenceRoutine(Color color, float startR, float endR, float duration, int echoes, float echoDelay, float width)
    {
        List<LineRenderer> active = new List<LineRenderer>();

        for (int i = 0; i < echoes; i++)
        {
            LineRenderer lr = GetLineRendererFromPool();
            lr.loop = true;
            lr.useWorldSpace = false;
            lr.positionCount = segments + 1; 
            lr.startWidth = width;
            lr.endWidth = width;
            lr.startColor = color;
            lr.endColor = new Color(color.r, color.g, color.b, 0f);
            lr.enabled = true;

            active.Add(lr);

            StartCoroutine(AnimateCircleRoutine(lr, startR, endR, duration, color));

            yield return new WaitForSeconds(echoDelay);
        }

        yield return new WaitForSeconds(duration + 0.01f);

        // cleanup
        foreach (LineRenderer lr in active)
        {
            if (lr != null) lr.enabled = false;
        }
    }

    private IEnumerator AnimateCircleRoutine(LineRenderer lr, float startRadius, float endRadius, float duration, Color color)
    {
        float timer = 0f;
        while (timer < duration)
        {
            float t = Mathf.Clamp01(timer / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f); // ease out cubic
            float radius = Mathf.Lerp(startRadius, endRadius, eased);
            float alpha = Mathf.Lerp(1f, 0f, eased);

            SetCirclePositions(lr, radius);

            Color startCol = new Color(color.r, color.g, color.b, alpha);
            Color endCol = new Color(color.r, color.g, color.b, 0f);
#if UNITY_2018_1_OR_NEWER
            lr.startColor = startCol;
            lr.endColor = endCol;
#else
            lr.SetColors(startCol, endCol);
#endif

            timer += Time.deltaTime;
            yield return null;
        }

        SetCirclePositions(lr, endRadius);
#if UNITY_2018_1_OR_NEWER
        lr.startColor = new Color(color.r, color.g, color.b, 0f);
        lr.endColor = new Color(color.r, color.g, color.b, 0f);
#else
        lr.SetColors(new Color(color.r, color.g, color.b, 0f), new Color(color.r, color.g, color.b, 0f));
#endif
        lr.enabled = false;
    }

    private void SetCirclePositions(LineRenderer lr, float radius)
    {
        float delta = 2f * Mathf.PI / segments;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * delta;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

    private LineRenderer GetLineRendererFromPool()
    {
        foreach (LineRenderer lr in pool)
        {
            if (lr != null && !lr.enabled) return lr;
        }

        GameObject go = new GameObject("SpawnCircleVFX_Line");
        go.transform.SetParent(transform, false);
        LineRenderer lrNew = go.AddComponent<LineRenderer>();
        lrNew.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        lrNew.useWorldSpace = false;
        lrNew.loop = true;
        lrNew.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lrNew.receiveShadows = false;
        lrNew.numCapVertices = 4;
        lrNew.enabled = false;
        pool.Add(lrNew);
        return lrNew;
    }

    private void OnDestroy()
    {
        StopAll();
        foreach (LineRenderer lr in pool)
        {
            if (lr != null) Destroy(lr.gameObject);
        }
        pool.Clear();
    }
}
