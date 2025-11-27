using System;
using System.Collections.Generic;
using UnityEngine;

/*
Implementation of the pointer abstract class to handle the EMG Pointer.
Used to control the Pointer with EMG data from the armband.
*/
public class EMGPointer : Pointer
{
    [SerializeField]
    private GameObject virtualHandPrefab;
    private GameObject virtualHand;

    [SerializeField]
    [Tooltip("Distance in front of the camera where the virtual hand will be placed.")]
    private float handOffsetDistance = -0.35f; // Distance in front of the camera where the virtual hand will be placed.

    [SerializeField] private GameObject SteamVRVisualHand;
    [SerializeField] private EMGDataProcessor emgDataProcessor;
    [SerializeField] private EMGPointerBehavior emgPointerBehavior;
    [SerializeField] private bool recordMaximumEMG = true; // If true, records the maximum EMG value reached during the session.
    [SerializeField] private float maxEMG = 0.0f;
    [SerializeField][Range(0f, 1f)] private float emgThreshold = 0.3f; // Threshold above which the EMG signal is considered as a muscle activation (0-1).

    [Header("Wrist Dwell Spinner Settings")]
    [SerializeField]
    [Tooltip("Optional: Reference to the WristDwellSpinner component. If null, will be auto-detected in virtual hand.")]
    private WristDwellSpinner wristSpinner;

    [SerializeField]
    [Tooltip("Enable/disable the wrist spinner display")]
    private bool useWristSpinner = true;

    private AIServerInterface aiServerInterface;
    private EMGClassifiedGestureManager emgClassifiedGestureManager;
    private const string DEFAULT_GESTURE = "Neutral"; // Default gesture when no mole is hovered over. 
    private string moleHoveringGesture = DEFAULT_GESTURE; // Current gesture of the mole being hovered over (only in Training mode).
    private string gestureConfidence = "Uncertain";
    private string thresholdState = "below";
    private Mole currentHoveredMole = null; // Track the currently hovered mole

    void Update()
    {
        // Update max EMG if recording is enabled
        if (recordMaximumEMG) maxEMG = Mathf.Max(maxEMG, (float)emgDataProcessor.GetSmoothedAbsAverage());
        thresholdState = IsAboveThreshold(emgDataProcessor.GetSmoothedAbsAverage()) ? "above" : "below";

        // Disable default visual hand when EMG pointer enabled
        if (SteamVRVisualHand != null && SteamVRVisualHand.activeSelf) SteamVRVisualHand.SetActive(false);

        // Update Gesture Visual based on current behavior (only if gesture manager is ready)
        if (emgClassifiedGestureManager != null && emgClassifiedGestureManager.IsInitialized)
        {
            HandGestureState currentGesture = GetCurrentGesture();
            emgClassifiedGestureManager.SetPose(currentGesture);
        }
    }

    public override void Enable()
    {
        if (active) return;
        if (laserMapper != null) laserMapper.GetComponentInChildren<Canvas>().enabled = false; //Disable visual components of laserMapper only for EMG pointer
        if (virtualHand != null) Destroy(virtualHand);
        if (virtualHandPrefab != null)
        {
            virtualHand = Instantiate(virtualHandPrefab, transform);
            virtualHand.transform.localPosition = new Vector3(0f, handOffsetDistance, 0f); // adjust as needed
            emgClassifiedGestureManager = virtualHand.GetComponent<EMGClassifiedGestureManager>();

            // Auto-detect wrist spinner if not assigned
            if (wristSpinner == null && useWristSpinner)
            {
                wristSpinner = virtualHand.GetComponentInChildren<WristDwellSpinner>();
                if (wristSpinner == null)
                {
                    Debug.LogWarning("EMGPointer: WristDwellSpinner not found in virtual hand. Wrist spinner display will be disabled.");
                    useWristSpinner = false;
                }
            }

            Transform visualStick = virtualHand.transform.Find("VisualStick");
            if (visualStick != null)
            {
                Renderer renderer = visualStick.GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    renderer.material = laserMaterial;
                    renderer.material.color = startLaserColor;
                }
            }
            else Debug.LogError("No 'VisualStick' found in the virtual hand prefab.");

            virtualHand.GetComponent<VirtualHandTrigger>().TriggerOnMoleEntered += OnHoverEnter;
            virtualHand.GetComponent<VirtualHandTrigger>().TriggerOnMoleExited += OnHoverExit;
            virtualHand.GetComponent<VirtualHandTrigger>().TriggerOnMoleStay += OnHoverStay;

            // Register the virtual hand's LogTracker with the TrackerHub
            LogTracker virtualHandTracker = virtualHand.GetComponent<LogTracker>();
            if (virtualHandTracker != null)
            {
                TrackerHub trackerHub = FindObjectOfType<TrackerHub>();
                if (trackerHub != null)
                {
                    trackerHub.RegisterTracker(virtualHandTracker);
                }
                else
                {
                    Debug.LogWarning("EMGPointer: TrackerHub not found. Virtual hand position will not be logged.");
                }
            }
        }
        else Debug.LogError("No virtual hand prefab assigned to the EMG Pointer.");

        if (aiServerInterface == null) aiServerInterface = new AIServerInterface(emgDataProcessor.thalmicMyo);
        ChangeBehavior(emgPointerBehavior);

        base.Enable();
    }

    public override void Disable()
    {
        if (!active) return;

        // Unregister the virtual hand's LogTracker before destroying it
        if (virtualHand != null)
        {
            LogTracker virtualHandTracker = virtualHand.GetComponent<LogTracker>();
            if (virtualHandTracker != null)
            {
                TrackerHub trackerHub = FindObjectOfType<TrackerHub>();
                if (trackerHub != null)
                {
                    trackerHub.UnregisterTracker(virtualHandTracker);
                }
            }

            Destroy(virtualHand);
            virtualHand = null;
        }

        // Hide wrist spinner when disabling
        if (wristSpinner != null)
        {
            wristSpinner.Hide();
        }

        currentHoveredMole = null;

        CancelInvoke(nameof(StartPredictionRequestCoroutine));

        base.Disable();

        if (SteamVRVisualHand != null)
        {
            SteamVRVisualHand.SetActive(true); // Re-enable default visual hand when EMG pointer disabled
        }
    }

    private void OnHoverEnter(Mole mole)
    {
        currentHoveredMole = mole;
        mole.OnHoverEnter();
        dwellStartTimer = Time.time;

        // Show wrist spinner when hovering over a mole
        if (useWristSpinner && wristSpinner != null)
        {
            bool isValidTarget = mole.IsValid();
            wristSpinner.Show(isValidTarget);
        }

        if (mole.GetState() == Mole.States.Enabled)
        {
            moleHoveringGesture = mole.GetValidationArg();

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

        // Hide wrist spinner when leaving a mole
        if (useWristSpinner && wristSpinner != null)
        {
            wristSpinner.Hide();
        }

        currentHoveredMole = null;
        moleHoveringGesture = DEFAULT_GESTURE;

        loggerNotifier.NotifyLogger("Pointer Hover End", EventLogger.EventType.PointerEvent, new Dictionary<string, object>()
        {
            {"ControllerHover", mole.name},
            {"ControllerName", gameObject.name}
        });
    }

    private void OnHoverStay(Mole mole)
    {
        if (mole.checkShootingValidity(GetCurrentGesture().ToString()))
        {
            // If the EMG signal is below the threshold, reset the dwell timer.
            if (emgDataProcessor.GetSmoothedAbsAverage() < (emgThreshold * maxEMG)) 
            {
                dwellStartTimer = Time.time;
                
                // Reset wrist spinner progress
                if (useWristSpinner && wristSpinner != null)
                {
                    wristSpinner.UpdateProgress(0f);
                }
            }

            // Calculate dwell progress
            float dwellProgress = (Time.time - dwellStartTimer) / dwellTime;
            
            // Update both mole and wrist spinner
            mole.SetLoadingValue(dwellProgress);
            if (useWristSpinner && wristSpinner != null)
            {
                wristSpinner.UpdateProgress(dwellProgress);
            }

            if (dwellProgress >= 1f)
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
                OnHoverExit(mole);
                Shoot(mole);
            }
        }
        else
        {
            // Invalid gesture - reset dwell timer and show in wrist spinner
            // Only update if we're still hovering over a mole (not after OnHoverExit has been called)
            if (useWristSpinner && wristSpinner != null && currentHoveredMole != null)
            {
                wristSpinner.UpdateProgress(0f);
                wristSpinner.Show(false); // Show as invalid
            }
        }
    }

    public void ResetMaxEMG() // Call by CALIBRATION keyworkd in Calibration Event (Game Director)
    {
        maxEMG = 0.0f;
        recordMaximumEMG = true;
    }

    private bool IsAboveThreshold(float emgIntensity) => (emgIntensity >= (emgThreshold * maxEMG));
    
    public string getThresholdState()
    {
        if (!active)
        {
            return "inactive";
        }
        return thresholdState;
    }

    public void ChangeBehavior(EMGPointerBehavior newBehavior)
    {
        // Always cancel the previous behavior
        CancelInvoke(nameof(StartPredictionRequestCoroutine));
        Debug.Log("EMG Pointer behavior changed to: " + newBehavior);

        switch (newBehavior)
        {
            case EMGPointerBehavior.LivePrediction:
                InvokeRepeating(nameof(StartPredictionRequestCoroutine), 0f, 0.004f);
                break;

            case EMGPointerBehavior.Training:
                gestureConfidence = "Training"; // No confidence in training mode
                break;

            default:
                Debug.LogError("Unknown EMG Pointer behavior: " + newBehavior);
                break;
        }

        emgPointerBehavior = newBehavior;
    }

    private void StartPredictionRequestCoroutine() => aiServerInterface.StartPredictionRequestCoroutine();

    public HandGestureState GetCurrentGesture()
    {
        // Return Unknown if not fully initialized
        if (!active || aiServerInterface == null)
        {
            return HandGestureState.Unknown;
        }

        string currentGestureString;

        switch (emgPointerBehavior)
        {
            case EMGPointerBehavior.LivePrediction:
                currentGestureString = IsAboveThreshold(emgDataProcessor.GetSmoothedAbsAverage()) ? aiServerInterface.GetCurrentGesture() : DEFAULT_GESTURE;
                gestureConfidence = aiServerInterface.GetCurrentGestureProb();
                break;

            case EMGPointerBehavior.Training:
                currentGestureString = IsAboveThreshold(emgDataProcessor.GetSmoothedAbsAverage()) ? moleHoveringGesture : DEFAULT_GESTURE;
                gestureConfidence = "Training";
                break;

            default:
                Debug.LogError("Unknown EMG Pointer behavior: " + emgPointerBehavior);
                gestureConfidence = "Uncertain";
                return HandGestureState.Unknown;
        }

        // Try to get gesture enum from string, ignore case
        if (!Enum.TryParse(currentGestureString, true, out HandGestureState handGestureState))
        {
            Debug.LogWarning("/!\\ Unrecognized gesture: " + currentGestureString);
            gestureConfidence = "Uncertain";
            return HandGestureState.Unknown; // Return Unknown if parsing fails
        }
        else return handGestureState; // Return parsed gesture
    }

    public string GetCurrentGestureConfidence()
    {
        if (!active || aiServerInterface == null)
        {
            return "Uninitialized";
        }
        return gestureConfidence;
    }

    /// <summary>
    /// Allows external control of wrist spinner visibility
    /// </summary>
    public void SetWristSpinnerEnabled(bool enabled)
    {
        useWristSpinner = enabled;
        if (!enabled && wristSpinner != null)
        {
            wristSpinner.Hide();
        }
    }

    /// <summary>
    /// Gets reference to the wrist spinner (if available)
    /// </summary>
    public WristDwellSpinner GetWristSpinner()
    {
        return wristSpinner;
    }
}

public enum EMGPointerBehavior
{
    LivePrediction,
    Training
}

/*
AIServerInterface
Handles communication with external AI server for EMG gesture classification.

Note: For future consideration - LibEMG is an alternative open-source Python toolbox 
for myoelectric control that could be evaluated:
https://delsyseurope.com/libemg-an-open-source-python-toolbox-for-myoelectric-control/
*/
