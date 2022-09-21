using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class LevelFaderPatientScreen : MonoBehaviour
{
    // Setting fade effect duration
    private float _fadeDuration = 2f;
    // Setting grey color from Unity starting logo
    private Color32 _color = new Color32(77, 77, 77, 255);

    private void Start()
    {
        FadeFromGreyToBlack();
        Invoke("FadeFromBlackToNormal", _fadeDuration);
    }

    private void FadeFromGreyToBlack()
    {
        //set start color (grey)
        SteamVR_Fade.View(_color, 0f);
        //set and start fade to black
        SteamVR_Fade.View(Color.black, _fadeDuration);
    }

    private void FadeFromBlackToNormal()
    {
        //set start color (black)
        SteamVR_Fade.View(Color.black, 0f);
        //set and start fade to normal
        SteamVR_Fade.View(Color.clear, _fadeDuration);
    }
}
