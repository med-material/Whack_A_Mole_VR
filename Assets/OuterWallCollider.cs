using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OuterWallCollider : MonoBehaviour
{

    private BoxCollider boxCollider;

    public float colliderSize = 5f; // extend 5 meters beyond regular size
    public float zOffset = 0.1f; // z offset relative to wall position.

    // Start is called before the first frame update
    void Start()
    {
        boxCollider = this.GetComponent<BoxCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnWallInfoUpdated(WallInfo wallInfo)
    {
        // set size of boxCollider
        float x = (wallInfo.highestX - wallInfo.lowestX) + colliderSize;
        float y = (wallInfo.highestY - wallInfo.lowestY) + colliderSize;
        boxCollider.size = new Vector2(x, y);

        // set position of boxCollider
        this.transform.position = new Vector3(wallInfo.meshCenter.x, wallInfo.meshCenter.y, wallInfo.meshCenter.z + zOffset);
    }
}
