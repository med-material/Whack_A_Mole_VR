using System.Collections.Generic;
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
    [SerializeField] public PrefabMatchingTable molePrefabs;
    private static int globalMoleIncrement = 1;

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
        prefab.molePrefabs.CheckMatchingTableIntegrity();

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

    public Mole SpawnMole(Mole.MoleType type, Mole.MoleOutcome outcome, float lifeTime, float expiringDuration, int spawnOrder, string validationArg = "")
    {
        if (_lock)
        {
            Debug.LogWarning($"Mistake: TargetSpawn {id} already has a mole. Returning the current one instead of creating a new one.");
            return GetCurrentMole();
        }

        currentMole = Instantiate(molePrefabs.GetPrefab(type), transform).GetComponent<Mole>();

        if (currentMole == null)
        {
            string errorMessage = $"Prefab for type {type} does not have a Mole component.";
            Debug.LogError(errorMessage);
            throw new System.Exception(errorMessage);
        }

        currentMole.Init(this);
        currentMole.SetNormalizedIndex(parameters.normalizedIndex);
        currentMole.SetValidationArg(validationArg);
        currentMole.SetPerformanceFeedback(parameters.performanceFeedback);
        currentMole.SetId(globalMoleIncrement++ + id.ToString()); // Formatted as ZZZXXYY (ZZZ is mole rank, XX is the X index, YY is the Y index)
        currentMole.transform.localScale = parameters.localScale;
        currentMole.Enable(lifeTime, expiringDuration, type, outcome, spawnOrder); // TODO future update, check if enable still needed (or change to init)
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


// ========== PrefabTypeTuple and PrefabMatchingTable classes ==========


[System.Serializable]
public class PrefabTypeTuple // Note: Can't use Dictionary here due to Unity serialization limitations
{
    public Mole.MoleType moleType;
    public GameObject prefab;
}

[System.Serializable]
public class PrefabMatchingTable
{
    [SerializeField] private PrefabTypeTuple[] items;

    public GameObject GetPrefab(Mole.MoleType type)
    {
        foreach (PrefabTypeTuple item in items)
        {
            if (item.moleType == type)
            {
                return item.prefab;
            }
        }

        string errorMessage = $"Prefab for type {type} not found in PrefabMatchingTable.";
        Debug.LogError(errorMessage);
        throw new System.Exception(errorMessage);
    }

    public bool CheckMatchingTableIntegrity() // Check if all mole types are present and unique and explicitly log errors - to avoid runtime errors
    {
        Mole.MoleType[] allMoleTypes = (Mole.MoleType[])System.Enum.GetValues(typeof(Mole.MoleType));
        List<Mole.MoleType> missingTypes = new List<Mole.MoleType>();
        List<Mole.MoleType> checkedTypes = new List<Mole.MoleType>();


        // Ensure no duplicates in the matching table
        foreach (PrefabTypeTuple item in items)
        {
            if (checkedTypes.Contains(item.moleType))
            {
                Debug.LogError($"Duplicate mole type [{item.moleType}] found in PrefabMatchingTable. " +
                    $"Please ensure each mole type is unique in the matching table of the prefab of TargetSpawner.");
                return false;
            }
            checkedTypes.Add(item.moleType);
        }


        // Ensure every mole type is present and has a prefab
        int missingCount = 0;
        foreach (Mole.MoleType type in allMoleTypes)
        {
            if (!Contains(type) || GetPrefab(type) == null)
            {
                missingCount++;
                missingTypes.Add(type);
            }
        }

        if (missingCount > 0)
        {
            Debug.LogError($"PrefabMatchingTable is missing the [{missingCount}] following mole types: " +
                $"{string.Join(", ", missingTypes)}. " +
                $"Please update the matching table in the prefab of TargetSpawner.");
            return false;
        }

        return true;
    }

    public bool Contains(Mole.MoleType type)
    {
        foreach (PrefabTypeTuple item in items)
        {
            if (item.moleType == type)
            {
                return true;
            }
        }
        return false;
    }
}
