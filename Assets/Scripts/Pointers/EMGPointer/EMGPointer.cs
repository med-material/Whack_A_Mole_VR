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
    
    [Header("Gesture-Specific MaxEMG Tracking")]
    [SerializeField]
    [Tooltip("Tracks the maximum EMG value reached for each gesture during calibration/training")]
    private Dictionary<string, float> gestureMaxEMG = new Dictionary<string, float>();
    
    [SerializeField]
	[Tooltip("Global maxEMG (highest across all gestures) - for reference")]
	private float globalMaxEMG = 0.0f;
	
    [Header("Wrist Dwell Spinner Settings")]
    [SerializeField]
    [Tooltip("Optional: Reference to the WristDwellSpinner component. If null, will be auto-detected in virtual hand.")]
    private WristDwellSpinner wristSpinner;

    [SerializeField]
    [Tooltip("Enable/disable the wrist spinner display")]
    private bool useWristSpinner = true;

    // Track which arm is currently being used to detect switches
    private Thalmic.Myo.Arm currentArm = Thalmic.Myo.Arm.Unknown;
    private bool hasTrackedArm = false;

    private AIServerInterface aiServerInterface;
    private EMGClassifiedGestureManager emgClassifiedGestureManager;
    private const string DEFAULT_GESTURE = "Neutral"; // Default gesture when no mole is hovered over. 
    private string moleHoveringGesture = DEFAULT_GESTURE; // Current gesture of the mole being hovered over (only in Training mode).
    private string gestureConfidence = "Uncertain";
    private string thresholdState = "below";
    private Mole currentHoveredMole = null; // Track the currently hovered mole
    
    // Dwell timer state tracking
    private float accumulatedDwellTime = 0f; // Total time spent doing correct gesture
    private float lastValidGestureTime = 0f; // When did we last have a valid gesture
    private bool wasValidLastFrame = false; // Track if previous frame was valid

    void Update()
    {
        // Check for arm changes and reset maxEMG if hand switched
        CheckArmChange();
        
        float currentEMG = (float)emgDataProcessor.GetSmoothedAbsAverage();
        
        // Update global maxEMG if recording is enabled
        if (recordMaximumEMG)
        {
            globalMaxEMG = Mathf.Max(globalMaxEMG, currentEMG);
            maxEMG = globalMaxEMG; // Keep maxEMG in sync for backward compatibility
            
            // Update gesture-specific maxEMG when hovering over a gesture mole
            if (!string.IsNullOrEmpty(moleHoveringGesture) && moleHoveringGesture != DEFAULT_GESTURE)
            {
                if (!gestureMaxEMG.ContainsKey(moleHoveringGesture))
                {
                    gestureMaxEMG[moleHoveringGesture] = 0.0f;
                }
                gestureMaxEMG[moleHoveringGesture] = Mathf.Max(gestureMaxEMG[moleHoveringGesture], currentEMG);
            }
        }
        
        thresholdState = IsAboveThreshold(currentEMG) ? "above" : "below";

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
            // Use GetComponentInChildren to search in child objects as well
            LogTracker virtualHandTracker = virtualHand.GetComponentInChildren<LogTracker>();
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
            else
            {
                Debug.LogWarning("EMGPointer: LogTracker not found on virtual hand or its children. Virtual hand position will not be logged.");
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
            // Use GetComponentInChildren to search in child objects as well
            LogTracker virtualHandTracker = virtualHand.GetComponentInChildren<LogTracker>();
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
        
        // Reset dwell tracking
        accumulatedDwellTime = 0f;
        lastValidGestureTime = 0f;
        wasValidLastFrame = false;

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
        string currentGesture = GetCurrentGesture().ToString();
        
        // In Training mode, if gesture is Neutral (below threshold), always reset regardless of mole validation
        // This ensures consistent behavior: below threshold = no progress, even for non-gesture moles
        if (emgPointerBehavior == EMGPointerBehavior.Training && currentGesture == "Neutral")
        {
            // TRAINING MODE: Neutral means below threshold - RESET immediately
            // This ensures instant visual feedback when EMG drops below calibration threshold
            // Debug.Log($"[EMGPointer] [Training] Neutral (below threshold) - RESETTING from {accumulatedDwellTime:F2}s to 0");
            accumulatedDwellTime = 0f;
            wasValidLastFrame = false;
            
            mole.SetLoadingValue(0f);
            if (useWristSpinner && wristSpinner != null && currentHoveredMole != null)
            {
                wristSpinner.UpdateProgress(0f);
                wristSpinner.Show(true); // Green but reset - waiting for gesture
            }
            return; // Exit early - don't process validity check
        }
        
        // Now check validity (for LivePrediction mode or Training mode with active gesture)
        bool isValid = mole.checkShootingValidity(currentGesture);
        
        // Calculate progress based on accumulated time
        float dwellProgress = accumulatedDwellTime / dwellTime;
        
        // Debug.Log($"[EMGPointer] OnHoverStay - Gesture: '{currentGesture}', Valid: {isValid}, Accumulated: {accumulatedDwellTime:F2}s, Progress: {dwellProgress:F2}");
        
        if (isValid)
        {
            // Valid gesture - accumulate dwell time
            if (wasValidLastFrame)
            {
                // Continue accumulating from last frame
                float deltaTime = Time.time - lastValidGestureTime;
                accumulatedDwellTime += deltaTime;
                // Debug.Log($"[EMGPointer] ✓ Valid gesture - ACCUMULATING {deltaTime:F3}s (total: {accumulatedDwellTime:F2}s)");
            }
            else
            {
                // Just started valid gesture
                // Debug.Log($"[EMGPointer] ✓ Valid gesture - STARTING accumulation");
            }
            
            lastValidGestureTime = Time.time;
            wasValidLastFrame = true;
            
            // Recalculate progress
            dwellProgress = accumulatedDwellTime / dwellTime;
            
            // Update displays
            mole.SetLoadingValue(dwellProgress);
            if (useWristSpinner && wristSpinner != null)
            {
                wristSpinner.UpdateProgress(dwellProgress);
                wristSpinner.Show(true); // Green - valid
            }

            // Check if dwell complete
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
        else if (currentGesture == "Neutral")
        {
            // LIVEPREDICTION MODE: Neutral means rest state - PAUSE and keep progress
            // This allows resuming where you left off when you return to active gesture
            // (Training mode Neutral is handled at the top of this function)
            // Debug.Log($"[EMGPointer] [LivePrediction] Neutral - PAUSED at {dwellProgress:F2} ({accumulatedDwellTime:F2}s accumulated)");
            wasValidLastFrame = false;
            
            // Update displays with frozen progress
            mole.SetLoadingValue(dwellProgress);
            if (useWristSpinner && wristSpinner != null && currentHoveredMole != null)
            {
                wristSpinner.UpdateProgress(dwellProgress);
                wristSpinner.Show(true); // Green - resting/waiting
            }
        }
        else if (currentGesture == "Unknown")
        {
            // Unknown - pause, show red (classifier uncertain)
            // Debug.Log($"[EMGPointer] ? Unknown - PAUSED at {dwellProgress:F2} (classifier uncertain)");
            wasValidLastFrame = false;
            
            // Update displays with frozen progress
            mole.SetLoadingValue(dwellProgress);
            if (useWristSpinner && wristSpinner != null && currentHoveredMole != null)
            {
                wristSpinner.UpdateProgress(dwellProgress);
                wristSpinner.Show(false); // Red - uncertain/warning
            }
        }
        else
        {
            // Wrong gesture - reset
            // Debug.Log($"[EMGPointer] ✗ WRONG gesture '{currentGesture}' - RESETTING from {accumulatedDwellTime:F2}s to 0");
            accumulatedDwellTime = 0f;
            wasValidLastFrame = false;
            
            mole.SetLoadingValue(0f);
            if (useWristSpinner && wristSpinner != null && currentHoveredMole != null)
            {
                wristSpinner.UpdateProgress(0f);
                wristSpinner.Show(false); // Red - wrong gesture
            }
        }
    }

    public void ResetMaxEMG() // Call by CALIBRATION keyworkd in Calibration Event (Game Director)
    {
        globalMaxEMG = 0.0f;
        maxEMG = 0.0f;
        gestureMaxEMG.Clear(); // Clear gesture-specific maxEMG values
        recordMaximumEMG = true;
        Debug.Log("[EMGPointer] MaxEMG reset: Global and gesture-specific values cleared");
    }
    
    /// <summary>
    /// Check if the Myo armband has switched arms (left/right) and automatically reset calibration
    /// This ensures that maxEMG thresholds are recalibrated when switching hands
    /// </summary>
    private void CheckArmChange()
    {
        if (emgDataProcessor == null || emgDataProcessor.thalmicMyo == null)
            return;
        
        Thalmic.Myo.Arm detectedArm = emgDataProcessor.thalmicMyo.arm;
        
        // Skip if arm is unknown (armband not synced yet)
        if (detectedArm == Thalmic.Myo.Arm.Unknown)
            return;
        
        // First time detecting an arm - just store it
        if (!hasTrackedArm)
        {
            currentArm = detectedArm;
            hasTrackedArm = true;
            Debug.Log($"[EMGPointer] Arm detected: {detectedArm}. Calibration tracking started.");
            return;
        }
        
        // Check if arm has changed (switched from left to right or vice versa)
        if (detectedArm != currentArm)
        {
            Debug.Log($"[EMGPointer] Arm changed: {currentArm} -> {detectedArm}. Automatically resetting maxEMG calibration.");
            currentArm = detectedArm;
            
            // Automatically reset calibration when switching hands
            ResetMaxEMG();
        }
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
    
    /// <summary>
    /// Gets the current target gesture (from hovered mole) for logging purposes
    /// Returns "None" if not hovering over a gesture mole
    /// </summary>
    public string GetTargetGesture()
    {
        return moleHoveringGesture ?? DEFAULT_GESTURE;
    }
    
    /// <summary>
    /// Gets the gesture-specific maxEMG for the current target gesture
    /// Returns 0 if gesture hasn't been calibrated yet
    /// </summary>
    public float GetTargetGestureMaxEMG()
    {
        if (string.IsNullOrEmpty(moleHoveringGesture) || moleHoveringGesture == DEFAULT_GESTURE)
        {
            return 0.0f;
        }
        
        if (gestureMaxEMG.ContainsKey(moleHoveringGesture))
        {
            return gestureMaxEMG[moleHoveringGesture];
        }
        
        return 0.0f;
    }
    
    /// <summary>
    /// Gets the global maxEMG (highest across all gestures)
    /// </summary>
    public float GetGlobalMaxEMG()
    {
        return globalMaxEMG;
    }
    
    /// <summary>
    /// Gets all gesture-specific maxEMG values (for debugging/visualization)
    /// </summary>
    public Dictionary<string, float> GetAllGestureMaxEMG()
    {
        return new Dictionary<string, float>(gestureMaxEMG); // Return a copy
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
                // In LivePrediction mode, send all EMG data to server without filtering
                // The classifier will handle rest/noise detection
                currentGestureString = aiServerInterface.GetCurrentGesture();
                gestureConfidence = aiServerInterface.GetCurrentGestureProb();
                break;

            case EMGPointerBehavior.Training:
                // In Training mode, use percentage-based threshold to filter noise and ensure quality training data
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
https://delsyseurope.com/libemg-an-open-source-python-toolbox-for-myolectric-control/
*/
