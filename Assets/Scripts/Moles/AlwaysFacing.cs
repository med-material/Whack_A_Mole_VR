using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlwaysFacing : MonoBehaviour
{
    void Start()
    {
        // To face the object's forward direction towards the origin, rotate 180 degrees after LookAt
        transform.LookAt(new Vector3(0, .5f, 0));
        transform.Rotate(0, 180, 0);
    }
}
