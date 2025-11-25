using System;
using UnityEngine;

public class GrabbingMole : Mole
{
    [SerializeField] private HandGestureState validationGrabbingGesture; // Gesture required to grab the mole
    [SerializeField] private Vector3 targetDestination; // Destination position to validate the mole
    [SerializeField] private float validationRadiusDistance; // Radius distance to validate the mole
    [SerializeField] private GameObject destionationVisualPrefab; // Visual prefab to indicate the destination
    [SerializeField] private GameObject destionationVisual; // Instance of the visual destination object

    private EMGPointer refEMGPointer; // Reference to the EMGPointer grabbing the mole

    private void Start() // TODO remove, use TargetSpawner instead <<--------------------
    {
        state = States.Enabled;
        validationArg = validationGrabbingGesture.ToString();
    }

    private void Update()
    {
        if (refEMGPointer != null) // If null, the mole is not being grabbed
        {
            bool thresholdKO = refEMGPointer.getThresholdState() == "below"; // Check if the EMG signal is below the threshold to release the mole
            bool gestureKO = !checkGrabbingValidity(refEMGPointer.GetCurrentGesture()); // Check if the current gesture is not valid for grabbing

            Debug.Log("!! Update: thresholdKO=" + thresholdKO + ", gestureKO=" + gestureKO);

            if (thresholdKO || gestureKO)
            {
                PlayHoverLeave();
            }
        }
    }

    public override bool checkShootingValidity(string validationArg) => false; // Disable shooting for grabbing moles

    public bool checkGrabbingValidity(HandGestureState currentGesture)
    {
        return currentGesture == validationGrabbingGesture;
    }

    public void grabedBy(GameObject handGameObject, EMGPointer emgPointer)
    {
        if (handGameObject == null)
        {
            Debug.Log("[GrebbingMole] Mole released!");
            transform.SetParent(parentTargetSpawner?.transform, true);
            return;
        }
        Debug.Log("[GrebbingMole] Mole grabbed by " + handGameObject.name);
        transform.SetParent(handGameObject.transform, true);
        refEMGPointer = emgPointer;
    }

    protected override void PlayHoverEnter()
    {
        if (destionationVisual != null)
        {
            Destroy(destionationVisual.gameObject);
            destionationVisual = null;
        }

        destionationVisual = Instantiate(destionationVisualPrefab, targetDestination, Quaternion.identity);
        //destionationVisual.transform.localScale = Vector3.one * validationRadiusDistance * 2f; // Adjust visual scale to the validation zone?

        base.PlayHoverEnter();
    }

    protected override void PlayHoverLeave()
    {
        if (destionationVisual != null)
        {
            Destroy(destionationVisual.gameObject);
            destionationVisual = null;
        }

        // Detach the mole from the handObject
        grabedBy(null, null);
        base.PlayHoverLeave();

        if (isWithinValidationRadius()) Debug.Log("!! pop");
        if (isWithinValidationRadius()) PlayPopping();
    }

    public bool isWithinValidationRadius() => Vector3.Distance(transform.position, targetDestination) <= validationRadiusDistance;
}
