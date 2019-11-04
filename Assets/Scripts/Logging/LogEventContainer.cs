using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Class dedicated to contain a log event. Basically contains two dictionaries, one for events, and one for persistent events.
*/
public class LogEventContainer
{
    public Dictionary<string, object> logEventParameters = new Dictionary<string, object>();
    public Dictionary<string, object> persistentLogEventParameters = new Dictionary<string, object>();
    public LogEventContainer(Dictionary<string, object> newEventParameters = null, Dictionary<string, object> newPersistentEventParameters = null)
    {
        if(!(newEventParameters is null)) logEventParameters = newEventParameters;
        if(!(newPersistentEventParameters is null)) persistentLogEventParameters = newPersistentEventParameters;
    }
}
