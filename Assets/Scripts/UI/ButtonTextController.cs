using UnityEngine;
using UnityEngine.UI;

/*
Class linked to the Property Edit Button prefab. Allows its behavior, meaning that when the user hovers the object, an "edit" text appears.
On click, allows an input in an input field, then on exit, checks if the input value is correct or not.
The prefab is a button made of three main components, an "info text" displaying the information when idle ("participant 3", "4 minutes"...), an "edit" text
which appears when the mouse hovers the button, and an input field allowing for edition of the property.
Extends the UiNotifier class to notify any listener when its value has changed.
*/

public class ButtonTextController : UiNotifier
{
    [SerializeField]
    private GameObject editText;

    [SerializeField]
    private GameObject infoText;

    [SerializeField]
    private GameObject inputField;

    [SerializeField]
    private int defaultValue = -1;

    [SerializeField]
    private int minValue = 0;

    [SerializeField]
    private bool swapInfoTextOrder = false;

    private int inputValue = -1;
    private string defaultinfoText = "";
    private bool edit = false;
    private bool activated = true;


    // On start, sets the default value if it fits inside the limits and constraints.
    void Start()
    {
        defaultinfoText = infoText.GetComponent<Text>().text;

        if (defaultValue != -1 && defaultValue <= 999)
        {
            if (defaultValue < minValue)
            {
                defaultValue = minValue;
            }
            inputField.GetComponent<InputField>().text = defaultValue.ToString();
            UpdateinputValue();
        }
        FocusInfo();
    }

    // Disables the button, preventing any interaction and setting the text gray to notify the user.
    public void Disable()
    {
        if (!activated || edit) return;
        activated = false;
        gameObject.GetComponent<Button>().interactable = false;
        infoText.GetComponent<Text>().color = Color.gray;
    }

    // Enables the button.
    public void Enable()
    {
        if (activated) return;
        activated = true;
        gameObject.GetComponent<Button>().interactable = true;
        infoText.GetComponent<Text>().color = Color.black;
    }

    // On click, activates the input field and focuses the UI on it.
    public void OnMouseClick()
    {
        GetComponent<Button>().interactable = false;
        FocusInput();
        inputField.GetComponent<InputField>().ActivateInputField();
    }

    // On hover enter, activates the "edit" text.
    public void OnMouseEnter()
    {
        if (!activated) return;
        FocusEdit();
    }

    // On hover leave, if the input field is not being edited, activates the information text.
    public void OnMouseLeave()
    {
        if (!activated) return;
        if (!edit)
        {
            FocusInfo();
        }
    }

    // On end of the edition of the input field, checks the input value, activates the information text and notifies any listener that its value has changed.
    public void OnInputEndEdit()
    {
        edit = false;
        UpdateinputValue();
        FocusInfo();
        GetComponent<Button>().interactable = true;
        NotifyTarget(inputValue.ToString());
    }

    // Updates the input value. Checks if the vaule fits inside the limits and constraints set in the editor and updates the information text accordingly.
    private void UpdateinputValue()
    {
        string inputText = inputField.GetComponent<InputField>().text;
        if (inputText != "")
        {
            int tempValue = int.Parse(inputField.GetComponent<InputField>().text);

            if (tempValue < 0 || tempValue < minValue)
            {
                return;
            }

            inputValue = tempValue;

            if (!swapInfoTextOrder)
            {
                infoText.GetComponent<Text>().text = defaultinfoText + inputValue.ToString();
            }
            else
            {
                infoText.GetComponent<Text>().text = inputValue.ToString() + defaultinfoText;
            }
        }
    }

    // Displays the "edit" text.
    private void FocusEdit()
    {
        inputField.SetActive(false);
        editText.SetActive(true);
        infoText.SetActive(false);
    }

    // Displays the "info" text.
    private void FocusInfo()
    {
        inputField.SetActive(false);
        editText.SetActive(false);
        infoText.SetActive(true);
    }

    // Displays the input field.
    private void FocusInput()
    {
        edit = true;
        inputField.SetActive(true);
        editText.SetActive(false);
        infoText.SetActive(false);
    }
}
