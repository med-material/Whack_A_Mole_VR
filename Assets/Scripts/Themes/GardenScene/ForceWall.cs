using UnityEngine;
using UnityEngine.SceneManagement;

public class ForceWall : MonoBehaviour
{
    [SerializeField] private Material sceneWallMaterial; // assign per-scene in Inspector

    void Start()
    {
        ApplyMaterialToPersistentWall();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyMaterialToPersistentWall();
    }

    private void ApplyMaterialToPersistentWall()
    {
        if (sceneWallMaterial == null)
        {
            Debug.LogWarning("ForceWall: No sceneWallMaterial assigned in inspector.");
            return;
        }

        // Find the (possibly persistent) WallManager in the scene or elsewhere
        WallManager wallManager = FindObjectOfType<WallManager>();
        if (wallManager == null)
        {
            Debug.LogWarning("ForceWall: WallManager not found in scene.");
            return;
        }

        MeshRenderer meshRenderer = wallManager.GetComponentInChildren<MeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogWarning("ForceWall: MeshRenderer not found on WallManager or its children.");
            return;
        }

        meshRenderer.material = sceneWallMaterial;
        Debug.Log($"ForceWall: Applied material '{sceneWallMaterial.name}' to wall.");
    }
}
