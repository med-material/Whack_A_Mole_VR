using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class LevelFaderScreen : MonoBehaviour
{
    private float _fadeDuration = 2.5f;

    private void Start()
    {
        FadeFromBlack();
    }

    private void FadeToBlack()
    {
        //set start color
        SteamVR_Fade.View(Color.clear, 0f);
        //set and start fade to
        SteamVR_Fade.View(Color.black, _fadeDuration);
    }

    private void FadeFromBlack()
    {
        //set start color
        SteamVR_Fade.View(Color.black, 0f);
        //set and start fade to
        SteamVR_Fade.View(Color.clear, _fadeDuration);
    }
}
