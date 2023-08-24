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

    private List<float> values = new List<float>();

    [SerializeField]
    private float soundDelay = 0.1f;

    [SerializeField]
    private SoundManager soundManager;

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
        values.Add(value);
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
        values = new List<float>();
        foreach(GameObject e in elements) {
            Destroy(e);
        }
    }

    public void Show() {
        progressPanel.SetActive(true);
        StartCoroutine(PlaySound(values, soundDelay));
    }

    private IEnumerator PlaySound(List<float> values, float delay)
    {
        foreach(float value in values) { 
            soundManager.PlaySoundWithPitch(gameObject, SoundManager.Sound.greenMoleHit, value);
            yield return new WaitForSeconds(delay);
        }
        yield return null;
    }

}
