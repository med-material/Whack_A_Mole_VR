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
    private Color shootColor;
    [SerializeField]
    private Color badShootColor;
    [SerializeField]
    private float laserExtraWidthShootAnimation = .05f;

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

    private float shootTimeLeft;
    private float totalShootTime;

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
            MyoEMGLogging.CurrentGestures = mole.GetMoleType().ToString();
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

    public void ResetMaxEMG()
    {
        maxEMG = 0.0f;
        recordMaximumEMG = true;
    }

    // Implementation of the behavior of the Pointer on shoot. 
    protected override void PlayShoot(bool correctHit) // TODO: probably need to be adapted
    {
        Color newColor;

        if (correctHit) newColor = shootColor;
        else newColor = badShootColor;
        if (!performancefeedback)
        {
            // don't show badShootColor if performance feedback is disabled.
            newColor = shootColor;
        }
        StartCoroutine(PlayShootAnimation(.5f, newColor));
    }
    // Ease function, Quart ratio.
    private float EaseQuartOut(float k)
    {
        return 1f - ((k -= 1f) * k * k * k);
    }
    // IEnumerator playing the shooting animation.
    private IEnumerator PlayShootAnimation(float duration, Color transitionColor)
    {
        shootTimeLeft = duration;
        totalShootTime = duration;
        // Generation of a color gradient from the shooting color to the default color (idle).
        Gradient colorGradient = new Gradient();
        GradientColorKey[] colorKey = new GradientColorKey[2] { new GradientColorKey(laser.startColor, 0f), new GradientColorKey(transitionColor, 1f) };
        GradientAlphaKey[] alphaKey = new GradientAlphaKey[2] { new GradientAlphaKey(laser.startColor.a, 0f), new GradientAlphaKey(transitionColor.a, 1f) };
        colorGradient.SetKeys(colorKey, alphaKey);
        // Playing of the animation. The laser and Cursor color and scale are interpolated following the easing curve from the shooting values (increased size, red/green color)
        // to the idle values
        while (shootTimeLeft > 0f)
        {
            float shootRatio = (totalShootTime - shootTimeLeft) / totalShootTime;
            float newLaserWidth = 0f;
            Color newLaserColor = new Color();
            newLaserWidth = laserWidth + ((1 - EaseQuartOut(shootRatio)) * laserExtraWidthShootAnimation);
            newLaserColor = colorGradient.Evaluate(1 - EaseQuartOut(shootRatio));
            laser.startWidth = newLaserWidth;
            laser.endWidth = newLaserWidth;
            laser.startColor = newLaserColor;
            laser.endColor = newLaserColor;
            cursor.SetColor(newLaserColor);
            cursor.SetScaleRatio(newLaserWidth / laserWidth);
            shootTimeLeft -= Time.deltaTime;
            yield return null;
        }
        // When the animation is finished, resets the laser and Cursor to their default values. 
        laser.startWidth = laserWidth;
        laser.endWidth = laserWidth;
        laser.startColor = startLaserColor;
        laser.endColor = EndLaserColor;
        cursor.SetColor(EndLaserColor);
        cursor.SetScaleRatio(1f);
    }
}
