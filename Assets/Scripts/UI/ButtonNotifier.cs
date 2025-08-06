using UnityEngine;

/*
Implementation of the UiNotifier class for buttons. The argument is defined in the editor since buttons don't have dynamic values.
*/

public class ButtonNotifier : UiNotifier
{
    [SerializeField]
    private string buttonArg;

    // Returns the button argument
    public string GetArg()
    {
        return buttonArg;
    }

    public void OnValueChange()
    {
        NotifyTarget(buttonArg);
    }
}
