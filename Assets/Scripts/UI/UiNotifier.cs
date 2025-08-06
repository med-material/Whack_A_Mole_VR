using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/*
Base Ui abstract class used to notify a component through UnityEvents of a Ui interaction.
Uses a normalized Dictionary<identifier, argument> to identify itself and pass arguments if needed.
While the identifier is fixed, the argument can be set at runtime.
*/

[System.Serializable]
public class UiNotifyTarget : UnityEvent<Dictionary<string, object>>
{
}

public abstract class UiNotifier : MonoBehaviour
{
    [SerializeField]
    public UiNotifyTarget UiNotifyTarget;

    [SerializeField]
    private string identifier;

    // Returns the identifier
    public string GetIdentifier()
    {
        return identifier;
    }

    // Raises an Event to notify any listener
    protected void NotifyTarget(object arg)
    {
        UiNotifyTarget.Invoke(new Dictionary<string, object> { { identifier, arg } });
    }
}
