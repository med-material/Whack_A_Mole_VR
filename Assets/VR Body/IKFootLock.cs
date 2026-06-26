using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKFootLock : MonoBehaviour
{
    [SerializeField]
    private Vector3 lockedPosition;

    private Transform self;

    void Awake()
    {
        self = transform;
    }

    // Update the position of the foot to always stay at the locked position
    void Update()
    {
        self.position = lockedPosition;
    }
}
