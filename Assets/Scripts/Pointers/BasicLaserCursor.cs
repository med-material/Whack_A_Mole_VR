using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Basic implementation of the LaserCursor abstract class. Is to be used with the BasicPointer
*/

public class BasicLaserCursor : LaserCursor
{
    [SerializeField]
    private float lightRadius = 0.5f;

    [SerializeField]
    private float lightIntensity = 0.75f;

    private GameObject lightObject;
    private Light pointLight;

    // On enable (right at the beginning, before Start()), creates a Light object.
    void Start()
    {
        lightObject = new GameObject("Light");
        lightObject.transform.parent = this.transform;
        lightObject.transform.localScale = Vector3.one;
        lightObject.transform.localPosition = new Vector3(0f, 0f, 0f);
        pointLight = lightObject.AddComponent<Light>();
        pointLight.color = cursorRenderer.material.color;
        pointLight.range = lightRadius;
    }

    // Extra behavior to do when the Cursor is enabled.
    protected override void ExtraEnable()
    {
        if (!pointLight) return;
        pointLight.enabled = true;
    }

    // Extra behavior to do when the Cursor is disabled.
    protected override void ExtraDisable()
    {
        pointLight.enabled = false;
    }

    // Extra behavior to do when the the color of the Cursor is set.
    protected override void ExtraSetColor(Color newColor)
    {
        pointLight.color = newColor;
    }
}
