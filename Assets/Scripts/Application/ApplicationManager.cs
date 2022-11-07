using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplicationManager : MonoBehaviour
{
    [SerializeField]
    //event that opens message panel and wait until the end of the save.
	private TherapistUi therapistUi;

    private SaveStatus _saveStatus;

    void Update()
    {
        if (Input.GetKey("escape")) StartCoroutine(QuitApplication());
    }

    //listen the variable of our Event that will impact the behaviour of our Warning Message Panel
    public void OnSaveInfoListener(SaveStateInfo s) {
        _saveStatus = s.status;  
    }

    public void CloseGame()
    {
        StartCoroutine(QuitApplication());
    }

    private IEnumerator QuitApplication()
    {
        if(_saveStatus == SaveStatus.IsSaving)
        {
           therapistUi.OpenWarningMessagePanel();
        }
        yield return new WaitUntil(() => _saveStatus == SaveStatus.Saved || _saveStatus == SaveStatus.ReadyToSave); 
        Application.Quit();
    }
}