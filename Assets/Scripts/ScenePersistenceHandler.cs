using UnityEngine;
using System.Linq;

/*
ScenePersistenceHandler
- Marks GameObjects tagged "keepBetweenScenes" as DontDestroyOnLoad.
- Use this in scenes OTHER than the Setup scene (e.g., MainScene, pattern scenes).
- Since persistent objects are created in Setup scene, this will only run if you directly open a scene in the editor for testing.
Usage:
- Attach to a GameObject in MainScene and other gameplay scenes.
- If you start from Setup scene, persistent objects already exist and this does nothing.
- If you open MainScene directly in the editor, this ensures objects in that scene persist when switching to patterns.
*/
public class ScenePersistenceHandler : MonoBehaviour
{
    void Start()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("keepBetweenScenes");
        objs.ToList().ForEach(obj => DontDestroyOnLoad(obj));
    }
}
