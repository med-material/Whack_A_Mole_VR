using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/*
Class keeping track of the state of the wall, meaning the state of the Mole. Keeps a list of the activated moles to
calculate the closest active mole when a laser is shot and misses. Also tracks active mole IDs ordered by expiration time.
*/

public class WallStateTracker : MonoBehaviour
{
    private List<Mole> activeMoles = new List<Mole>();
    private Dictionary<int, float> activeMoleIDsWithExpiration = new Dictionary<int, float>();

    void Start()
    {
        FindObjectOfType<WallManager>().GetUpdateEvent().AddListener(WallStateUpdate);
    }

    // Returns the distance between the hit point and the closest active Mole.
    public Vector2 GetClosestDistPointToMole(Vector3 point)
    {
        Vector3 closestMoleDist = Vector3.zero;

        foreach (Mole mole in activeMoles)
        {
            if ((closestMoleDist == Vector3.zero) || (closestMoleDist.magnitude > Mathf.Abs(Vector3.Distance(point, mole.transform.position))))
            {
                closestMoleDist = (point - mole.transform.position);
                closestMoleDist = new Vector3(Mathf.Abs(closestMoleDist.x), Mathf.Abs(closestMoleDist.y), Mathf.Abs(closestMoleDist.z));
            }
        }

        return new Vector2(new Vector2(closestMoleDist.x, closestMoleDist.z).magnitude, closestMoleDist.y);
    }

    // Function called through an event when the WallManager initialises the wall (spawns the moles).
    public void WallStateUpdate(WallInfo wallInfo)
    {
        bool isActivating = wallInfo.active;
        Dictionary<int, TargetSpawner> targetSpawners = wallInfo.targetSpawners;
        if (isActivating)
        {
            activeMoleIDsWithExpiration.Clear();
            foreach (TargetSpawner targetSpawner in targetSpawners.Values)
            {
                targetSpawner.GetUpdateEvent().AddListener(MoleStateUpdate);
            }
        }
        else
        {
            activeMoles.Clear();
            activeMoleIDsWithExpiration.Clear();
        }
    }

    // Function called through an event when a Mole's state changes.
    public void MoleStateUpdate(bool isActivating, Mole concernedMole)
    {
        if (isActivating)
        {
            activeMoles.Add(concernedMole);
            float expirationTime = Time.time + concernedMole.GetLifeTime();
            activeMoleIDsWithExpiration[concernedMole.GetMoleOccurrenceID()] = expirationTime;
        }
        else
        {
            activeMoles.Remove(concernedMole);
            activeMoleIDsWithExpiration.Remove(concernedMole.GetMoleOccurrenceID());
        }
    }
}
