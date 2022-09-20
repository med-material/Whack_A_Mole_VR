using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class LevelFaderScreen : MonoBehaviour
{
    private float _fadeDuration = 2f;

    [SerializeField]
    private GameObject panel;

    private void Start()
    {
        FadeToBlack();
        Invoke("FadeFromBlack", _fadeDuration);
    }
    private void FadeToBlack()
    {
        //set start color
        SteamVR_Fade.View(Color.clear, 0f);
        //set and start fade to
        SteamVR_Fade.View(Color.black, _fadeDuration);
        panel.SetActive(false);
    }
    private void FadeFromBlack()
    {
        //set start color
        SteamVR_Fade.View(Color.black, 0f);
        //set and start fade to
        SteamVR_Fade.View(Color.clear, _fadeDuration);
        panel.SetActive(true);
    }
}
