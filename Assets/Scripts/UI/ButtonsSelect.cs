using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
Simple class used to create a select from multiple buttons. Any button children of the GameObject having this script will be taken into account.
This class ensures that only one button is selected as once (like radio buttons).
*/

public class ButtonsSelect : MonoBehaviour
{
    private List<Button> buttons = new List<Button>();

    void Start()
    {
        foreach(Button button in gameObject.GetComponentsInChildren<Button>())
        {
            buttons.Add(button);
            button.onClick.AddListener(delegate{ButtonClicked(button);});
        }
    }

    public void ButtonClicked(Button clickedButton)
    {
        foreach(Button button in buttons)
        {
            if (button == clickedButton)
            {
                button.interactable = false;
            }
            else
            {
                button.interactable = true;
            }
        }
    }
}
