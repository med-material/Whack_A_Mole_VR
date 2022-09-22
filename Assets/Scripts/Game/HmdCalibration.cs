using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using UnityEngine.Events;

public class HmdCalibration : MonoBehaviour
{

    [System.Serializable]
    public class CalibrationEvent : UnityEvent {}
    [SerializeField]
    public CalibrationEvent calibrationUpdate;

    [SerializeField]
    private SteamVR_Input_Sources controller;

    [SerializeField]
    private GameObject controllerGameObject;

    [SerializeField]
    private float ratioSpeed = 3f;

    bool calibrated = false;
    int timeout = 100;
    int time = 0;

    [SerializeField]
    private CanvasGroup canvasGroupToFade; // Used to make disappear all elements linked to a canvas

    // Start is called before the first frame update
    void Awake()
    {
        controllerGameObject.GetComponent<SteamVR_Behaviour_Pose>().onTransformUpdated.AddListener(delegate{PositionUpdated();});
    }

    public void Update() {
        if (!calibrated) {
            if (Input.GetKeyDown(KeyCode.V))
            {
                CloseInstructionPanel();
            }
        }
    }

    // Update is called once per frame
    public void PositionUpdated() {
        // HACK: for some reason we need invoke the calibrationUpdate a couple of times..
        // need to investigate why this is..
        if (time < timeout) {
            if(SteamVR.active)
            {
                if (SteamVR_Input._default.inActions.GrabPinch.GetStateDown(controller))
                {
                    CloseInstructionPanel();
                }
            }
            if (calibrated) {
            time++;
            }
        }
    }

    //generic function if we need to change something when the instruction panel disappeared
    private void CloseInstructionPanel(){
        StartCoroutine(FadeOutCanvasGroup());
        calibrationUpdate.Invoke();
        calibrated = true;
    }

    public IEnumerator FadeOutCanvasGroup()
    {
        while (canvasGroupToFade.alpha > 0)
        {
            canvasGroupToFade.alpha -= Time.deltaTime * ratioSpeed;

            yield return null;
        }
        canvasGroupToFade.transform.gameObject.SetActive(false);
    }
}
