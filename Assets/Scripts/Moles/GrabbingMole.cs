using System;
using System.Collections;
using UnityEngine;

public class GrabbingMole : Mole
{
    [SerializeField] private HandGestureState validationGrabbingGesture; // Gesture required to grab the mole
    [SerializeField] private Vector3 targetDestination; // Destination position to validate the mole
    [SerializeField] private float validationRadiusDistance; // Radius distance to validate the mole
    [SerializeField] private GameObject destinationVisualPrefab; // Visual prefab to indicate the destination

    [SerializeField] private GameObject hoverInfoContainer;
    [SerializeField] private HoverInfo[] hoverInfos;

    [Serializable]
    public class HoverInfo
    {
        public string key;
        public GameObject value;
    }

    private GameObject destinationVisual; // Instance of the visual destination object
    private EMGPointer refEMGPointer; // Reference to the EMGPointer grabbing the mole

    public override void Init(TargetSpawner parentSpawner)
    {
        base.Init(parentSpawner);
        updateHoverInfo();
        showHoverInfo(false);
    }

    private void Update()
    {
        if (refEMGPointer != null) // If null, the mole is not being grabbed
        {
            bool thresholdKO = refEMGPointer.getThresholdState() == "below"; // Check if the EMG signal is below the threshold to release the mole
            bool gestureKO = !checkGrabbingValidity(refEMGPointer.GetCurrentGesture()); // Check if the current gesture is not valid for grabbing

            if (thresholdKO || gestureKO)
            {
                PlayHoverLeave();
            }
        }
    }

    public override void SetMovement(float ?X, float ?Y)
    {
        targetDestination = new Vector3(
            this.transform.position.x + (X ?? 0f),
            this.transform.position.y + (Y ?? 0f),
            this.transform.position.z
        );
    }

    public override bool checkShootingValidity(string validationArg) => false; // Disable shooting for grabbing moles

    private void showHoverInfo(bool status) => hoverInfoContainer.SetActive(status);
    private void updateHoverInfo()
    {
        foreach (HoverInfo hoverInfo in hoverInfos)
        {
            hoverInfo.value.SetActive(hoverInfo.key == validationArg);
        }
    }

    public bool checkGrabbingValidity(HandGestureState currentGesture)
    {
        return currentGesture == validationGrabbingGesture;
    }

    public void grabedBy(GameObject handGameObject, EMGPointer emgPointer)
    {
        if (handGameObject == null)
        {
            Debug.Log("[GrabbingMole] Mole released!");
            transform.SetParent(parentTargetSpawner?.transform, true);
            refEMGPointer = null;
        }
        else
        {
            Debug.Log("[GrabbingMole] Mole grabbed by " + handGameObject.name);
            transform.SetParent(handGameObject.transform, true);
            refEMGPointer = emgPointer;
        }
    }

    protected override void PlayHoverEnter()
    {
        if (destinationVisual != null)
        {
            Destroy(destinationVisual.gameObject);
            destinationVisual = null;
        }

        destinationVisual = Instantiate(destinationVisualPrefab, targetDestination, Quaternion.identity);
        showHoverInfo(true);

        base.PlayHoverEnter();
    }

    protected override void PlayHoverLeave()
    {
        if (destinationVisual != null)
        {
            Destroy(destinationVisual.gameObject);
            destinationVisual = null;
        }

        // Detach the mole from the handObject
        grabedBy(null, null);
        showHoverInfo(false);
        base.PlayHoverLeave();

        if (isWithinValidationRadius()) StartCoroutine(PlayPopping());
    }

    protected override IEnumerator PlayPopping()
    {
        showHoverInfo(false);
        yield return base.PlayPopping();
    }

    public override void SetValidationArg(string arg)
    {
        base.SetValidationArg(arg);
        if (Enum.TryParse(arg, out HandGestureState gestureState))
        {
            validationGrabbingGesture = gestureState;
        }
        else
        {
            Debug.LogError($"[GrabbingMole] Invalid HandGestureState in validationArg: {arg}");
        }
    }

    public bool isWithinValidationRadius() => Vector3.Distance(transform.position, targetDestination) <= validationRadiusDistance;
}
