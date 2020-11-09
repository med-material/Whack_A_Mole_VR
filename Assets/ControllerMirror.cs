using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerMirror : MonoBehaviour
{

    [SerializeField]
    private GameObject controllerToMirror;

    [SerializeField]
    private Pointer laserToMirror;

    Vector3 newPos;
    Quaternion newRot;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Mirror position
        newPos = controllerToMirror.transform.position;
        newPos = new Vector3 (newPos.x * -1f, newPos.y, newPos.z);

        // Mirror rotation
        newRot = controllerToMirror.transform.localRotation;
        newRot = new Quaternion(newRot.x, newRot.y * -1f, newRot.z * -1f, newRot.w);

        this.transform.position = newPos;
        this.transform.localRotation = newRot;
    }
}
