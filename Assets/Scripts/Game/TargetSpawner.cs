using UnityEngine;
using UnityEngine.Events;

public class MoleParameters
{
    public int id;
    public Vector3 localScale;
    public Vector3 normalizedIndex;
    public bool performanceFeedback;
}

public class TargetSpawner : MonoBehaviour
{
    [SerializeField] private Mole molePrefab; // TODO: move "Mole Type" system from Mole.cs to TargetSpawner.cs

    public Vector3 _position
    {
        get => transform.localPosition; set => transform.localPosition = value;
    }
    public Quaternion _rotation
    {
        get => transform.localRotation; set => transform.localRotation = value;
    }

    private int id;
    private bool _lock;
    private Mole currentMole;
    private MoleParameters parameters;
    private class StateUpdateEvent : UnityEvent<bool, Mole> { };
    private StateUpdateEvent stateUpdateEvent = new StateUpdateEvent();

    public static TargetSpawner Instantiate(TargetSpawner prefab, Transform parentTransform, Vector3 position, Quaternion rotation, MoleParameters parameters)
    {
        TargetSpawner targetSpawner = Instantiate(prefab, parentTransform);
        targetSpawner.parameters = parameters;
        targetSpawner._position = position;
        targetSpawner._rotation = rotation;
        targetSpawner.id = parameters.id;

        return targetSpawner;
    }

    public void UpdateParameters(Vector3? localScale = null, bool? performanceFeedback = null)
    {
        if (localScale.HasValue) parameters.localScale = localScale.Value;
        if (performanceFeedback.HasValue) parameters.performanceFeedback = performanceFeedback.Value;
    }

    public Mole SpawnMole(Mole.MoleType type, float lifeTime, float expiringDuration, int spawnOrder)
    {
        if (_lock) return GetCurrentMole();

        currentMole = Instantiate(molePrefab, transform);
        currentMole.Init(this);
        currentMole.SetNormalizedIndex(parameters.normalizedIndex);
        currentMole.SetPerformanceFeedback(parameters.performanceFeedback);
        currentMole.SetId(id); // TODO: add globalSpawnCounter to create unique ID - need to adapt logging index X and Y in mole.cs
        currentMole.transform.localScale = parameters.localScale;
        currentMole.Enable(lifeTime, expiringDuration, type, spawnOrder); // TODO future update, check if enable still needed (of change to init)
        stateUpdateEvent.Invoke(true, currentMole);

        _lock = true;
        return currentMole;
    }

    public void DespawnMole()
    {
        if (currentMole != null)
        {
            stateUpdateEvent.Invoke(false, currentMole);
            Destroy(currentMole.gameObject);
            currentMole = null;
        }

        _lock = false;
    }

    public void OnDestroy() => DespawnMole();
    public bool isFree() => !_lock;
    public Mole GetCurrentMole() => currentMole;
    public int GetId() => id;
    public UnityEvent<bool, Mole> GetUpdateEvent() => stateUpdateEvent;

}
