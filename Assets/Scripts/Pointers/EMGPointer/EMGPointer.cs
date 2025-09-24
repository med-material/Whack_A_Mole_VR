using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

/*
Implementation of the pointer abstract class to handle the EMG Pointer.
Used to control the Pointer with EMG data from the armband.
*/
public class EMGPointer : Pointer
{
    [SerializeField]
    private EMGPointerBehavior emgPointerBehavior;

    [SerializeField]
    private GameObject virtualHandPrefab;
    private GameObject virtualHand;

    [SerializeField]
    [Tooltip("Distance in front of the camera where the virtual hand will be placed.")]
    private float handOffsetDistance = 0.35f; // Distance in front of the camera where the virtual hand will be placed.

    [SerializeField]
    private GameObject SteamVRVisualHand;

    [SerializeField] private EMGDataProcessor emgDataProcessor;
    [SerializeField] private bool recordMaximumEMG = true; // If true, records the maximum EMG value reached during the session.
    [SerializeField] private float maxEMG = 0.0f;
    [SerializeField][Range(0f, 1f)] private float emgThreshold = 0.3f; // Threshold above which the EMG signal is considered as a muscle activation (0-1).

    private AIServerInterface aiServerInterface;
    private string MoleHoveringGesture = "NULL"; // Current gesture of the mole being hovered over (only in Training mode).

    void Update()
    {
        if (recordMaximumEMG) maxEMG = Mathf.Max(maxEMG, (float)emgDataProcessor.GetSmoothedAbsAverage());
        MyoEMGLogging.Threshold = (emgDataProcessor.GetSmoothedAbsAverage() >= (emgThreshold * maxEMG)) ? "above" : "below";

        if (SteamVRVisualHand != null && SteamVRVisualHand.activeSelf)
        {
            SteamVRVisualHand.SetActive(false); // Disable default visual hand when EMG pointer enabled
        }
    }

    public override void Enable()
    {
        if (active) return;
        if (virtualHand != null) Destroy(virtualHand);
        if (virtualHandPrefab != null)
        {
            virtualHand = Instantiate(virtualHandPrefab, transform);
            virtualHand.transform.localPosition = new Vector3(0f, handOffsetDistance, 0f); // adjust as needed
            virtualHand.GetComponent<VirtualHandTrigger>().TriggerOnMoleEntered += OnHoverEnter;
            virtualHand.GetComponent<VirtualHandTrigger>().TriggerOnMoleExited += OnHoverExit;
            virtualHand.GetComponent<VirtualHandTrigger>().TriggerOnMoleStay += OnHoverStay;
        }
        else Debug.LogError("No virtual hand prefab assigned to the EMG Pointer.");

        if (aiServerInterface == null) aiServerInterface = new AIServerInterface(emgDataProcessor.thalmicMyo);
        ChangeBehavior(emgPointerBehavior);

        base.Enable();
    }

    public override void Disable()
    {
        if (!active) return;

        if (virtualHand != null)
        {
            Destroy(virtualHand);
            virtualHand = null;
        }

        CancelInvoke(nameof(StartPredictionRequestCoroutine));

        base.Disable();

        if (SteamVRVisualHand != null)
        {
            SteamVRVisualHand.SetActive(true); // Re-enable default visual hand when EMG pointer disabled
        }
    }

    private void OnHoverEnter(Mole mole)
    {
        mole.OnHoverEnter();
        dwellStartTimer = Time.time;
        if (mole.GetState() == Mole.States.Enabled)
        {
            MoleHoveringGesture = mole.GetMoleType().ToString();
            MyoEMGLogging.CurrentGestures = MoleHoveringGesture;

            loggerNotifier.NotifyLogger("Pointer Hover Begin", EventLogger.EventType.PointerEvent, new Dictionary<string, object>()
            {
                {"ControllerHover", mole.GetId().ToString()},
                {"ControllerName", gameObject.name}
            });
        }
    }

    private void OnHoverExit(Mole mole)
    {
        mole.SetLoadingValue(0);
        mole.OnHoverLeave();

        MoleHoveringGesture = "NULL";
        MyoEMGLogging.CurrentGestures = "NULL";

        loggerNotifier.NotifyLogger("Pointer Hover End", EventLogger.EventType.PointerEvent, new Dictionary<string, object>()
        {
            {"ControllerHover", mole.name},
            {"ControllerName", gameObject.name}
        });
    }

    private void OnHoverStay(Mole mole)
    {
        if (mole.GetState() == Mole.States.Enabled)
        {
            // If the EMG signal is below the threshold, reset the dwell timer.
            if (emgDataProcessor.GetSmoothedAbsAverage() < (emgThreshold * maxEMG)) dwellStartTimer = Time.time;

            mole.SetLoadingValue((Time.time - dwellStartTimer) / dwellTime);
            if ((Time.time - dwellStartTimer) > dwellTime)
            {
                pointerShootOrder++;
                loggerNotifier.NotifyLogger(overrideEventParameters: new Dictionary<string, object>(){
                                {"ControllerSmoothed", directionSmoothed},
                                {"ControllerAimAssistState", System.Enum.GetName(typeof(Pointer.AimAssistStates), aimAssistState)},
                                {"LastShotControllerRawPointingDirectionX", transform.forward.x},
                                {"LastShotControllerRawPointingDirectionY", transform.forward.y},
                                {"LastShotControllerRawPointingDirectionZ", transform.forward.z}
                            });

                loggerNotifier.NotifyLogger("Pointer Shoot", EventLogger.EventType.PointerEvent, new Dictionary<string, object>()
                            {
                                {"PointerShootOrder", pointerShootOrder},
                                {"ControllerName", gameObject.name}
                            });
                MyoEMGLogging.CurrentGestures = "NULL";
                Shoot(mole);
            }
        }
    }

    public void ResetMaxEMG() // Call by CALIBRATION keyworkd in Calibration Event (Game Director)
    {
        maxEMG = 0.0f;
        recordMaximumEMG = true;
    }

    public void ChangeBehavior(EMGPointerBehavior newBehavior)
    {
        // Always cancel the previous behavior
        CancelInvoke(nameof(StartPredictionRequestCoroutine));

        switch (newBehavior)
        {
            case EMGPointerBehavior.LivePrediction:
                InvokeRepeating(nameof(StartPredictionRequestCoroutine), 0f, 0.004f);
                break;
            case EMGPointerBehavior.Training:
                break;
            default:
                Debug.LogError("Unknown EMG Pointer behavior: " + newBehavior);
                break;
        }

        emgPointerBehavior = newBehavior;
    }

    private void StartPredictionRequestCoroutine() => aiServerInterface.StartPredictionRequestCoroutine();

    public string GetCurrentGesture()
    {
        switch(emgPointerBehavior)
        {
            case EMGPointerBehavior.LivePrediction:
                return aiServerInterface.GetCurrentGesture();
            case EMGPointerBehavior.Training:
                return MoleHoveringGesture;
            default:
                Debug.LogError("Unknown EMG Pointer behavior: " + emgPointerBehavior);
                return "Unknown";
        }
    }
}

public enum EMGPointerBehavior
{
    LivePrediction,
    Training
}
