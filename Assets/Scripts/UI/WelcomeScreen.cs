using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using XRDeviceCharacteristics = UnityEngine.XR.InputDeviceCharacteristics;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRInputDevices = UnityEngine.XR.InputDevices;

/*
WelcomeScreen
Summary:
- Welcome card shown on Android standalone in place of the Velkommen screen ("Welcome to
  Whack-A-Mole VR / Click the back button to start").
- Presentation only, it runs no game logic. AndroidWelcomeSetup finds it and switches it on on
  Android; a PC build never activates it, and IsSupportedPlatform() turns it back off should it be
  left active in the scene.
- Dismissed by the back button (B/Y), A/X, the menu button or either trigger, a click on the
  card, or Space/Enter in the editor: it fades out, then raises onStart (nothing wired to it).
Usage:
1. Drop the WelcomePanel prefab in the scene and leave it inactive (the prefab already is).
2. With the platform set to Android, Play in the editor shows it as the headset will. Ticking it
   active by hand also works; forgetting to untick it is harmless, a PC build turns it off.
*/
public class WelcomeScreen : MonoBehaviour
{
    [System.Serializable]
    public class StartEvent : UnityEvent { }

    [Tooltip("Raised once the player dismissed the card, after it finished fading out.")]
    [SerializeField]
    public StartEvent onStart;

    [SerializeField]
    private Canvas canvas;

    [SerializeField]
    private CanvasGroup canvasGroup;

    [Tooltip("Controllers whose activate action (trigger) dismisses the card. Found at runtime if left empty.")]
    [SerializeField]
    private ActionBasedController[] controllers = new ActionBasedController[0];

    [SerializeField]
    private float fadeDuration = 0.35f;

    private bool started = false;

    private readonly List<XRInputDevice> xrControllers = new List<XRInputDevice>();

    // Starts as held so that a button already down when the card shows has to be released first.
    private bool controllerButtonHeld = true;

    private void Awake()
    {
        if (!IsSupportedPlatform())
        {
            gameObject.SetActive(false);
            return;
        }

        if (canvas == null) canvas = GetComponent<Canvas>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        // A Screen Space - Camera canvas draws nothing in the headset while its camera is unassigned.
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
        {
            canvas.worldCamera = Camera.main;
        }

        if (controllers == null || controllers.Length == 0)
        {
            controllers = FindObjectsOfType<ActionBasedController>();
        }
    }

    private void Update()
    {
        if (started) return;
        if (StartActionPressed()) OnStartPressed();
    }

    // Called by the card's Button, and usable from any other pointer setup able to click the panel.
    public void OnStartPressed()
    {
        if (started) return;
        started = true;

        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        StartCoroutine(FadeOutAndStart());
    }

    private bool StartActionPressed()
    {
        // Space/Enter for the editor; escapeKey is where an Android back key lands, when there is one.
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame
                                 || keyboard.spaceKey.wasPressedThisFrame
                                 || keyboard.enterKey.wasPressedThisFrame))
        {
            return true;
        }

        foreach (ActionBasedController controller in controllers)
        {
            if (controller == null) continue;

            InputAction activateAction = controller.activateAction.action;
            if (activateAction != null && activateAction.WasPerformedThisFrame()) return true;
        }

        return ControllerButtonPressed();
    }

    // The ActionBasedControllers only bind the right trigger and a headset has no Escape key, so
    // the back button (B/Y), A/X, the menu button and either trigger are read straight from the XR
    // devices, whatever the controller profile. Reports the press, not the hold.
    private bool ControllerButtonPressed()
    {
        bool held = false;
        XRInputDevices.GetDevicesWithCharacteristics(XRDeviceCharacteristics.Controller, xrControllers);
        foreach (XRInputDevice device in xrControllers)
        {
            held |= IsDown(device, XRCommonUsages.secondaryButton)
                 || IsDown(device, XRCommonUsages.primaryButton)
                 || IsDown(device, XRCommonUsages.menuButton)
                 || IsDown(device, XRCommonUsages.triggerButton);
        }

        bool pressed = held && !controllerButtonHeld;
        controllerButtonHeld = held;
        return pressed;
    }

    private static bool IsDown(XRInputDevice device, UnityEngine.XR.InputFeatureUsage<bool> usage)
    {
        return device.TryGetFeatureValue(usage, out bool down) && down;
    }

    private IEnumerator FadeOutAndStart()
    {
        if (canvasGroup != null && fadeDuration > 0f)
        {
            // Unscaled, so the fade never depends on the game's time scale.
            while (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha -= Time.unscaledDeltaTime / fadeDuration;
                yield return null;
            }
        }

        onStart?.Invoke();
        gameObject.SetActive(false);
    }

    // Safety net only: AndroidWelcomeSetup is what activates the panel, on Android. This keeps it
    // off a desktop build even if the object was left active in the scene.
    private static bool IsSupportedPlatform()
    {
#if UNITY_ANDROID || UNITY_EDITOR
        return true;
#else
        return false;
#endif
    }
}
