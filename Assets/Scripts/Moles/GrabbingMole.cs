using UnityEngine;

public class GrabbingMole : Mole
{
    [SerializeField] private HandGestureState validationGrabbingGesture; // Gesture required to grab the mole
    [SerializeField] private Vector3 targetDestination; // Destination position to validate the mole
    [SerializeField] private float validationRadiusDistance; // Radius distance to validate the mole
    [SerializeField] private GameObject destionationVisualPrefab; // Visual prefab to indicate the destination

    private GameObject handObject; // Set by the VirtualHand
    [SerializeField] private GameObject destionationVisual; // Instance of the visual destination object


    private void Start()
    {
        state = States.Enabled; // TODO: remove me
    }

    public override void Init(TargetSpawner parentSpawner)
    {
        base.Init(parentSpawner);
    }


    public override bool checkShootingValidity(string validationArg)
    {
        return false; // Disable shooting for grabbing moles
    }

    public bool checkGrabbingValidity(HandGestureState currentGesture)
    {
        return currentGesture == validationGrabbingGesture;
    }

    public void grabedBy(GameObject gameObject)
    {
        Debug.Log("!! GrabbingMole grabbed by " + gameObject.name);
        transform.SetParent(gameObject.transform, true);
    }

    protected override void PlayHoverEnter()
    {
        destionationVisual = Instantiate(destionationVisualPrefab, this.transform);
        destionationVisual.transform.position = targetDestination;
        destionationVisual.transform.localScale = Vector3.one * validationRadiusDistance * 2f;

        base.PlayHoverEnter();
    }

    protected override void PlayHoverLeave()
    {
        Destroy(destionationVisual.gameObject);

        // Detach the mole from the handObject
        grabedBy(null);
        base.PlayHoverLeave();

        if (isWithinValidationRadius()) PlayPopping();
    }

    public bool isWithinValidationRadius() => Vector3.Distance(transform.position, targetDestination) <= validationRadiusDistance;
    public void SetHandObject(GameObject hand) => handObject = hand;
}
