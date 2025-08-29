using UnityEngine;

public class VirtualHandTrigger : MonoBehaviour
{
    public event System.Action<Mole> TriggerOnMoleEntered;
    public event System.Action<Mole> TriggerOnMoleExited;
    public event System.Action<Mole> TriggerOnMoleStay;

    [SerializeField] private string layerName = "Target";

    private void OnTriggerEnter(Collider other)
    {
        TriggerOnMole(TriggerOnMoleEntered, other);
    }

    private void OnTriggerExit(Collider other)
    {
        TriggerOnMole(TriggerOnMoleExited, other);
    }

    private void OnTriggerStay(Collider other)
    {
        TriggerOnMole(TriggerOnMoleStay, other);
    }

    private void TriggerOnMole(System.Action<Mole> action, Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(layerName)) // Only interact with objects in the specified layer
        {
            Mole mole;
            if (other.TryGetComponent<Mole>(out mole)) // Only interact with objects that have a Mole component
            {
                action?.Invoke(mole);
            }
        }
    }

    private void OnDestroy()
    {
        TriggerOnMoleEntered = null;
        TriggerOnMoleExited = null;
        TriggerOnMoleStay = null;
    }
}
