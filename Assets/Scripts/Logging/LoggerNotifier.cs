using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class UpdateLogEvent : UnityEvent<LogEventContainer>
{
}

/*
Class dedicated to notifier the EventLogger of a new event.
*/

public class LoggerNotifier: UnityEngine.Object
{
    public delegate LogEventContainer UpdateGeneralValues();
    private UpdateGeneralValues updateGeneralValues;
    private Dictionary<string, string> eventsHeadersDefaults;
    private Dictionary<string, string> persistentEventsHeadersDefaults;
    private UpdateLogEvent updateEvent = new UpdateLogEvent();

    // The class needs specific initialization. To initialize it, it is necessary to specify the Event and PersistentEvent parameters' names that will be passed through the events, as well as their default
    // values (that will be used if, when raising the event, no value is given for the parameter). It is also possible to pass a function that will be called whan raising an event to update parameters
    // that would be passed every time (see Mole LoggerNotifier's implementation for an example). All of these parameters are optionnal, since a LoggerNotifier may be created to only raise PersistentEvent,
    // only raise Event, raise both...
    // Once the initialisation is done, automatically detects the EventLogger and calls it so it update its headers and default values lists to take into account the headers and default values that will be 
    // used by this LoggerNotifier.
    public LoggerNotifier(UpdateGeneralValues updateGeneralValues = null, Dictionary<string, string> eventsHeadersDefaults = null, Dictionary<string, string> persistentEventsHeadersDefaults = null)
    {
        if(eventsHeadersDefaults is null) this.eventsHeadersDefaults = new Dictionary<string, string>();
        else this.eventsHeadersDefaults = eventsHeadersDefaults;
        
        if(persistentEventsHeadersDefaults is null) this.persistentEventsHeadersDefaults = new Dictionary<string, string>();
        else this.persistentEventsHeadersDefaults = persistentEventsHeadersDefaults;
        
        this.updateGeneralValues = updateGeneralValues;

        EventLogger eventLogger = FindObjectOfType<EventLogger>();
        updateEvent.AddListener(eventLogger.EventNotification);
        eventLogger.UpdateHeadersAndDefaults(GetHeadersAndDefaults());
    }

    // Function called by the class instantiating this LoggerNotifier to notify the EventLogger of a new event.
    // If an eventName is given, an Event will be raised. Otherwise only a PersistentEvent will be raised (if it has parameters).
    public void NotifyLogger(string eventName = "", Dictionary<string, object> overrideEventParameters = null)
    {
        updateEvent.Invoke(GenerateLogEvent(eventName, overrideEventParameters));
    }

    // Function called by the class instantiating this LoggerNotifier to initialize certain parameters with a desired value.
    // Has to be called right after the LoggerNotifier's initialization to fill its purpose (see ModifiersManager implementation for an example).
    public void InitPersistentEventParameters(Dictionary<string, object> initParameters)
    {
        updateEvent.Invoke(new LogEventContainer(newPersistentEventParameters: initParameters));
    }

    // Generates a new event, passed through a LogEventContainer containing a Event and/or a PersistentEvent.
    // If no value is given for a certain parameter, uses the parameter's value given by the updateGeneralValue function
    // passed by the parent class on initialization. If no function has been passed or the function doesn't return a value for
    // the given parameter, the value will be set to its default by the EventLogger.
    private LogEventContainer GenerateLogEvent(string eventName, Dictionary<string, object> overrideEventParameters)
    {
        Dictionary<string, object> resultEventParameters = new Dictionary<string, object>();
        Dictionary<string, object> resultPersistentEventParameters = new Dictionary<string, object>();
        object value;
        LogEventContainer generalDatas = new LogEventContainer();
        
        if(!(updateGeneralValues is null))
        {
            generalDatas = updateGeneralValues();
        }

        if(eventName != "")
        {
            resultEventParameters.Add("Event", eventName);
            foreach(KeyValuePair<string, string> headerDefault in eventsHeadersDefaults)
            {
                if(!(overrideEventParameters is null))
                {
                    if(overrideEventParameters.TryGetValue(headerDefault.Key, out value))
                    {
                        resultEventParameters.Add(headerDefault.Key, value);
                        continue;
                    }
                }
                
                if (generalDatas.logEventParameters.TryGetValue(headerDefault.Key, out value))
                {
                    resultEventParameters.Add(headerDefault.Key, value);
                }
            }
        }

        foreach(KeyValuePair<string, string> headerDefault in persistentEventsHeadersDefaults)
        {
            if(!(overrideEventParameters is null))
            {
                if(overrideEventParameters.TryGetValue(headerDefault.Key, out value))
                {
                    resultPersistentEventParameters.Add(headerDefault.Key, value);
                    continue;
                }
            }

            if (generalDatas.persistentLogEventParameters.TryGetValue(headerDefault.Key, out value))
            {
                resultPersistentEventParameters.Add(headerDefault.Key, value);
            }
        }

        return new LogEventContainer(resultEventParameters, resultPersistentEventParameters);
    }

    // Generates and returns the headers (parameters names) and corresponding defaults values.
    private Dictionary<string, string> GetHeadersAndDefaults()
    {
        Dictionary<string, string> resultDict = new Dictionary<string, string>();

        foreach(KeyValuePair<string, string> pair in eventsHeadersDefaults) resultDict.Add(pair.Key, pair.Value);
        foreach(KeyValuePair<string, string> pair in persistentEventsHeadersDefaults) resultDict.Add(pair.Key, pair.Value); 

        return resultDict;
    }
}
