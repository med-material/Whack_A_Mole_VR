using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
An implementation of the Mole abstract class. Defines different parameters to modify and
overrides the actions to do on different events (enables, disables, popped...).
*/

public class DiskMole : Mole
{
    [SerializeField]
    private Color disabledColor;

    [SerializeField]
    private Color enabledColor;

    [SerializeField]
    private Color fakeEnabledColor;

    [SerializeField]
    private Color hoverColor;

    [SerializeField]
    private Color fakeHoverColor;

    [SerializeField]
    private AudioClip enableSound;

    [SerializeField]
    private AudioClip disableSound;

    [SerializeField]
    private AudioClip popSound;

    private delegate void Del();
    private Shader opaqueShader;
    private Shader glowShader;
    private Material ballMaterial;
    private AudioSource audioSource;

    protected override void Start()
    {
        ballMaterial = gameObject.GetComponent<Renderer>().material;
        opaqueShader = Shader.Find("Standard");
        glowShader = Shader.Find("Particles/Standard Unlit");
        audioSource = gameObject.GetComponent<AudioSource>();
        base.Start();
    }

    /*
    Override of the event functions of the base class.
    */

    protected override void PlayEnabling()
    {
        PlaySound(enableSound);
        base.PlayEnabling();
    }

    protected override void PlayEnable() 
    {
        SwitchShader(false);

        if (!fake)
        {
            ChangeColor(enabledColor);
        }
        else
        {
            ChangeColor(fakeEnabledColor);
        }
    }

    protected override void PlayDisabling()
    {
        PlaySound(enableSound);
        base.PlayDisabling();
    }

    protected override void PlayDisable()
    {
        SwitchShader(false);
        ChangeColor(disabledColor);
    }

    protected override void PlayHoverEnter() 
    {
        SwitchShader(true);
        if (!fake)
        {
            ChangeColor(hoverColor);
        }
        else
        {
            ChangeColor(fakeHoverColor);
        }
    }

    protected override void PlayHoverLeave() 
    {
        SwitchShader(false);
        if (!fake)
        {
            ChangeColor(enabledColor);
        }
        else
        {
            ChangeColor(fakeEnabledColor);
        }
    }

    protected override void PlayPop() 
    {
        SwitchShader(true);
        if (!fake)
        {
            ChangeColor(enabledColor);
        }
        else
        {
            ChangeColor(fakeEnabledColor);
        }
        PlaySound(popSound);
        StartCoroutine(Wait(.2f, new Del(base.PlayPop)));
    }

    private void PlaySound(AudioClip audioClip)
    {
        if (!audioSource)
        {
            return;
        }
        audioSource.clip = audioClip;
        audioSource.Play();
    }

    private void ChangeColor(Color color)
    {
        ballMaterial.color = color;
    }

    private void SwitchShader(bool glow = false)
    {
        if (glow)
        {
            if (ballMaterial.shader.name == glowShader.name) return;
            ballMaterial.shader = glowShader;
        }
        else
        {
            if (ballMaterial.shader.name == opaqueShader.name) return;
            ballMaterial.shader = opaqueShader;
        }
    }

    private IEnumerator Wait(float duration, Del method)
    {
        yield return new WaitForSeconds(duration);
        method();
    }
}
