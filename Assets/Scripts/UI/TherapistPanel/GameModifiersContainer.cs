using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
Class updating the modifiers buttons when a modifier's state is changed.
*/

public class GameModifiersContainer : MonoBehaviour
{
    private Dictionary<string, Dictionary<string, Button>> buttonsDict;
    private ButtonsSelect[] buttonsSelects;


    void Awake()
    {
        buttonsDict = new Dictionary<string, Dictionary<string, Button>>();

        buttonsSelects = gameObject.GetComponentsInChildren<ButtonsSelect>();

        foreach (Button button in gameObject.GetComponentsInChildren<Button>())
        {
            ButtonNotifier notifier = button.gameObject.GetComponent<ButtonNotifier>();

            if (!buttonsDict.ContainsKey(notifier.GetIdentifier())) buttonsDict.Add(notifier.GetIdentifier(), new Dictionary<string, Button>());
            buttonsDict[notifier.GetIdentifier()].Add(notifier.GetArg(), button);
        }
    }

    public void UpdateSelectedButton(string identifier, string argument)
    {
        if (buttonsDict.ContainsKey(identifier))
        {
            if (buttonsDict[identifier].ContainsKey(argument))
            {
                foreach (ButtonsSelect select in buttonsSelects)
                {
                    if (select.ContainsButton(buttonsDict[identifier][argument]))
                    {
                        select.ButtonClicked(buttonsDict[identifier][argument]);
                    }
                }
            }
        }
    }
}
