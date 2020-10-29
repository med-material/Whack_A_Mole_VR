using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Abstract class of the Pointer's cursor. The cursor is an object placed at the hit point of the laser. This class allows for
deep customisation of this cursor.
*/
public abstract class LaserCursor : MonoBehaviour
{
    [SerializeField]
    protected Vector3 positionOffset = Vector3.zero;
    protected Renderer cursorRenderer;
    protected bool isEnabled = false;
    protected Vector3 startScale;

    void Awake()
    {
        cursorRenderer = gameObject.GetComponent<Renderer>();
        startScale = transform.localScale;
    }

    // Enables the cursor.
    public void Enable()
    {
        if (isEnabled) return;
        isEnabled = true;
        if (cursorRenderer == null) cursorRenderer = gameObject.GetComponent<Renderer>();
        cursorRenderer.enabled = true;
        ExtraEnable();
    }

    // Disables the cursor.
    public void Disable()
    {
        if (!isEnabled) return;
        isEnabled = false;
        cursorRenderer.enabled = false;
        ExtraDisable();
    }

    public bool IsEnabled()
    {
        return isEnabled;
    }

    // Sets the cursor color.
    public void SetColor(Color newColor)
    {
        cursorRenderer.material.color = newColor;
        ExtraSetColor(newColor);
    }

    // Sets the cursor's position
    public void SetPosition(Vector3 newPosition)
    {
        transform.localPosition = newPosition + positionOffset;
    }

    // Sets the cursor scale
    public void SetScaleRatio(float ratio)
    {
        transform.localScale = startScale * ratio;
    }

    // Virtual functions to be called in the class implementation if it is desired to add extra behaviors.
    protected virtual void ExtraSetColor(Color newColor) {}

    protected virtual void ExtraEnable() {}

    protected virtual void ExtraDisable() {}
}
