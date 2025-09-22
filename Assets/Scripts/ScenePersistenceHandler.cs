using UnityEngine;
using System.Linq;

public class ScenePersistenceHandler : MonoBehaviour
{
    void Start()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("keepBetweenScenes");
        objs.ToList().ForEach(obj => DontDestroyOnLoad(obj));
    }
}
