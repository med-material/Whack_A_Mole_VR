using System.Collections;
using UnityEngine;

/*
An implementation of the Mole abstract class. Defines different parameters to modify and
overrides the actions to do on different events (enables, disables, popped...).
*/

public class BallMole : Mole
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

    private Shader opaqueShader;
    private Shader glowShader;
    private Material ballMaterial;
    private AudioSource audioSource;

    public override void Init(TargetSpawner parentSpawner)
    {
        ballMaterial = gameObject.GetComponent<Renderer>().material;
        opaqueShader = Shader.Find("Standard");
        glowShader = Shader.Find("Particles/Standard Unlit");
        audioSource = gameObject.GetComponent<AudioSource>();

        base.Init(parentSpawner);
    }

    /*
    Override of the event functions of the base class.
    */

    protected override IEnumerator PlayEnabling()
    {
        PlaySound(enableSound);
        yield return base.PlayEnabling();
    }

    protected override void PlayEnabled()
    {
        SwitchShader(false);

        if (moleOutcome == MoleOutcome.Valid)
        {
            ChangeColor(enabledColor);
        }
        else
        {
            ChangeColor(fakeEnabledColor);
        }
    }

    protected override IEnumerator PlayDisabling()
    {
        PlaySound(enableSound);
        yield return base.PlayDisabling();
    }

    protected override void PlayDisabled()
    {
        SwitchShader(false);
        ChangeColor(disabledColor);
    }

    protected override void PlayHoverEnter()
    {
        SwitchShader(true);
        if (moleOutcome == MoleOutcome.Valid)
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
        if (moleOutcome == MoleOutcome.Valid)
        {
            ChangeColor(enabledColor);
        }
        else
        {
            ChangeColor(fakeEnabledColor);
        }
    }

    protected override IEnumerator PlayPopping()
    {
        SwitchShader(true);
        if (moleOutcome == MoleOutcome.Valid)
        {
            ChangeColor(enabledColor);
        }
        else
        {
            ChangeColor(fakeEnabledColor);
        }
        PlaySound(popSound);
        yield return new WaitForSeconds(.2f);
        yield return base.PlayPopping();
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
}
