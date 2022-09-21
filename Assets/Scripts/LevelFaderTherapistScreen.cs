using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class LevelFaderTherapistScreen : MonoBehaviour
{
    // Setting fade effect duration
    private float _fadeDuration = 2f;
    // Setting grey color from Unity starting logo
    private Color32 _color = new Color32(50,50,50, 255);

    // Variable used to stock the therapist panel
    [SerializeField]
    public GameObject therapistPanel;

    private void Start()
    {
        //disables therapist panel so it doesn't show on launch
        therapistPanel.SetActive(false);
        //fade in then fade out effect
        FadeFromGreyToBlack();
        Invoke("FadeFromBlackToNormal", _fadeDuration);
    }

    private void ActivateTherapistScreen()
    {
        therapistPanel.SetActive(true);
    }

    private void FadeFromGreyToBlack()
    {
        //set start color (grey)
        SteamVR_Fade.Start(_color, 0f);
        //set and start fade to black
        SteamVR_Fade.Start(Color.black, _fadeDuration);
    }

    private void FadeFromBlackToNormal()
    {
        //set start color (black)
        SteamVR_Fade.Start(Color.black, 0f);
        //set and start fade to normal
        SteamVR_Fade.Start(Color.clear, _fadeDuration);
        //enables the therapist after fade out effect
        Invoke("ActivateTherapistScreen", _fadeDuration);
    }
}
