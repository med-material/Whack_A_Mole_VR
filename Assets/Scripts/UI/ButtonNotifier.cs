using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
Implementation of the UiNotifier class for buttons. The argument is defined in the editor since buttons don't have dynamic values.
*/

public class ButtonNotifier : UiNotifier
{
    [SerializeField]
    private string buttonArg;

    public void OnValueChange()
    {
        NotifyTarget(buttonArg);
    }
}
