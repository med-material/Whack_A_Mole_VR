using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplicationManager : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKey("escape")) QuitApplication();
    }

    public void CloseGame()
    {
        QuitApplication();
    }

    private void QuitApplication()
    {
        Application.Quit();
    }
}
