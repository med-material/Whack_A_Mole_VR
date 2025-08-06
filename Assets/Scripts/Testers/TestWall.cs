using UnityEngine;

public class TestWall : MonoBehaviour
{
    void Start()
    {
        gameObject.GetComponent<WallManager>().Enable();
    }
}
