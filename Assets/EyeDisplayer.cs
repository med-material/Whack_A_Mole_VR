using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeDisplayer : MonoBehaviour
{
    public MeshRenderer left;
    public MeshRenderer right;

    public EyeTracker tracker;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (tracker.left.validGaze)
        {
            left.enabled = true;
            left.transform.localPosition = tracker.left.pos;
            left.transform.localRotation = tracker.left.rot;
        }
        else
        {
            left.enabled = false;
        }
        if (tracker.right.validGaze)
        {
            right.enabled = true;
            right.transform.localPosition = tracker.right.pos;
            right.transform.localRotation = tracker.right.rot;
        }
        else
        {
            right.enabled = false;
        }
    }
}
