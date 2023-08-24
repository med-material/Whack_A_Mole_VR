using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{

    [SerializeField]
    private CanvasGroup gradientL;

    [SerializeField]
    private CanvasGroup gradientR;

    [SerializeField]
    private CanvasGroup gradientT;

    [SerializeField]
    private CanvasGroup gradientB;

    private float clock = -1f;

    Side side = Side.None;
    
    private bool active = false;

    private float timer = 0.35f;

    private bool lastEnter = true;
    private Side lastSide;

    private Dictionary<Side,Coroutine> coroutines = new Dictionary<Side,Coroutine>();


    public bool enabled = true;
    // Start is called before the first frame update
    void Start()
    {
        Reset();
    }

    public void OnGameStateChanged(GameDirector.GameState newState) {
        switch(newState)
        {
            case GameDirector.GameState.Stopped:
                active = false;
                break;
            case GameDirector.GameState.Playing:
                active = true;
                if (!lastEnter) { ActivateGradient(lastSide, FadeAction.In); }
                break;
            case GameDirector.GameState.Paused:
                active = false;
                break;
        }
    }

    public void Reset() {
        if (coroutines.ContainsKey(Side.Right)) { StopCoroutine(coroutines[Side.Right]); }
        FadingUtils.FadeRoutine(handler: this, Obj: gradientR.transform.gameObject, fadeTime: timer, fadeDirection: FadeAction.Out);
        if (coroutines.ContainsKey(Side.Top)) { StopCoroutine(coroutines[Side.Top]); }
        FadingUtils.FadeRoutine(handler: this, Obj: gradientT.transform.gameObject, fadeTime: timer, fadeDirection: FadeAction.Out);
        if (coroutines.ContainsKey(Side.Bottom)) { StopCoroutine(coroutines[Side.Bottom]); }
        FadingUtils.FadeRoutine(handler: this, Obj: gradientB.transform.gameObject, fadeTime: timer, fadeDirection: FadeAction.Out);
        if (coroutines.ContainsKey(Side.Left)) { StopCoroutine(coroutines[Side.Left]); }
        FadingUtils.FadeRoutine(handler: this, Obj: gradientL.transform.gameObject, fadeTime: timer, fadeDirection: FadeAction.Out);
    }

    public void OnMotorSpaceEnter(EnterMotorSpaceInfo m) {
        lastEnter = m.enter;
        lastSide = m.side;
        Debug.Log("MotorSpaceEnter " + m.enter + "active " + active);
        if (m.enter && active) {
            Debug.Log("MotorSpaceEnter " + m.side);
            //ActivateGradient(m.side, FadeAction.Out);
            Reset();
        } else if (!m.enter && active) {
            Debug.Log("MotorSpaceExit " + m.side);
            ActivateGradient(m.side, FadeAction.In);
        }
    }  

    public void ActivateGradient(Side s, FadeAction f) {
        if (!enabled) return;

        if (coroutines.ContainsKey(s)) { StopCoroutine(coroutines[s]); }
        if (s == Side.Left) {
            coroutines[s] = FadingUtils.FadeRoutine(handler: this, Obj: gradientL.transform.gameObject, fadeTime: timer, fadeDirection: f, fadeDelay: 0.5f);
        }
        if (s == Side.Right) {
            coroutines[s] = FadingUtils.FadeRoutine(handler: this, Obj: gradientR.transform.gameObject, fadeTime: timer, fadeDirection: f, fadeDelay: 0.5f);
        }
        if (s == Side.Top) {
            coroutines[s] =  FadingUtils.FadeRoutine(handler: this, Obj: gradientT.transform.gameObject, fadeTime: timer, fadeDirection: f, fadeDelay: 0.5f);
        }
        if (s == Side.Bottom) {
            coroutines[s] =  FadingUtils.FadeRoutine(handler: this, Obj: gradientB.transform.gameObject, fadeTime: timer, fadeDirection: f, fadeDelay: 0.5f);
        }
    }
}
