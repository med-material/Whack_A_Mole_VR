using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class SliderValue : MonoBehaviour
{

    [System.Serializable]
    public class OnValueChanged : UnityEvent<string> { }
    public OnValueChanged onValueChanged;

    private Slider slider;

    // Start is called before the first frame update
    void Awake()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(delegate {OnSliderValueChange ();});
        onValueChanged.Invoke(slider.value.ToString("0"));
    }


    public void OnSliderValueChange()
    {
        onValueChanged.Invoke(slider.value.ToString("0"));
    }
}
