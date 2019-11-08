using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestWall : MonoBehaviour
{
    void Start()
    {
        gameObject.GetComponent<WallManager>().Enable();
    }
}
