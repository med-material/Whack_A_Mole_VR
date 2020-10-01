using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleDisplay : MonoBehaviour
{
    // The parent we will follow in terms of object position.
    [SerializeField]
    private GameObject parent;

    [SerializeField]
    private bool parentX = true;

    [SerializeField]
    private bool parentY = true;

    [SerializeField]
    private bool parentZ = false;

    [SerializeField]
    private float offsetX = 0f;

    [SerializeField]
    private float offsetY = 0f;

    [SerializeField]
    private float offsetZ = 0f;

    private float newPosX;
    private float newPosY;
    private float newPosZ;

    private Vector3 ownPosition;
    
    // Start is called before the first frame update
    void Awake()
    {
        ownPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        // Update our world position to be equivalent to the parent, for the axis chosen.
        newPosX = parentX ? parent.transform.position.x : ownPosition.x;
        newPosY = parentY ? parent.transform.position.y : ownPosition.y;
        newPosZ = parentZ ? parent.transform.position.z : ownPosition.z;

        this.transform.position = new Vector3(newPosX + offsetX, newPosY + offsetY, newPosZ + offsetZ);
    }
}
