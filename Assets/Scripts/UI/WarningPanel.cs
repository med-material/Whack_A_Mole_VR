using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarningPanel : MonoBehaviour
{
    [SerializeField]
    private GameObject warningTextContainer;

    public void UpdateWarningDisplay(bool display)
    {
        if (display) warningTextContainer.SetActive(true);
        else warningTextContainer.SetActive(false);
    }
}
