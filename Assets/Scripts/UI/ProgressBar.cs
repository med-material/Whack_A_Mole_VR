using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{

    [SerializeField]
    private GameObject element;

    [SerializeField]
    private Color highlightColor;

    [SerializeField]
    private Color baseColor;

    [SerializeField]
    private Color wrongColor;

    [SerializeField]
    private Sprite spriteCorrect;

    [SerializeField]
    private Sprite spriteWrong; 

    private List<GameObject> elements = new List<GameObject>();

    private GameObject progressPanel;

    // Start is called before the first frame update
    void Start()
    {
        element.SetActive(false);
        progressPanel = this.transform.GetChild(0).gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddElement(float value, bool correct = true) {
        GameObject newElement = Instantiate(element, progressPanel.transform);
        elements.Add(newElement);
        Image eImage = newElement.transform.GetChild(0).GetComponent<Image>();
        if (correct) {
            eImage.color = Vector4.Lerp(baseColor, highlightColor, value);
            eImage.sprite = spriteCorrect;
        } else {
            Image bg = newElement.GetComponent<Image>();
            bg.color = wrongColor;
            eImage.color = highlightColor;
        }
        newElement.SetActive(true);
    }

    public void Reset() {
        progressPanel.SetActive(false);
        foreach(GameObject e in elements) {
            Destroy(e);
        }
    }

    public void Show() {
        progressPanel.SetActive(true);
    }

}
