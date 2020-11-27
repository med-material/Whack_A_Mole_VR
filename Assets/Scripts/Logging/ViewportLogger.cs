using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewportLogger : MonoBehaviour
{


    [SerializeField]
    private GameObject upperMiddle;
    [SerializeField]
    private GameObject upperRight;
    [SerializeField]
    private GameObject lowerRight;
    [SerializeField]
    private GameObject lowerMiddle;
    [SerializeField]
    private GameObject lowerLeft;
    [SerializeField]
    private GameObject upperLeft;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Dictionary<string, object> GetViewportData() {
        Dictionary<string, object> data = new Dictionary<string, object>() {
            {"ViewportUpperMiddleX", upperMiddle.transform.position.x},
            {"ViewportUpperMiddleY", upperMiddle.transform.position.y},
            {"ViewportUpperMiddleZ", upperMiddle.transform.position.z},
            {"ViewportUpperRightX", upperRight.transform.position.x},
            {"ViewportUpperRightY", upperRight.transform.position.y},
            {"ViewportUpperRightZ", upperRight.transform.position.z},
            {"ViewportLowerRightX", lowerRight.transform.position.x},
            {"ViewportLowerRightY", lowerRight.transform.position.y},
            {"ViewportLowerRightZ", lowerRight.transform.position.z},
            {"ViewportLowerMiddleX", lowerMiddle.transform.position.x},
            {"ViewportLowerMiddleY", lowerMiddle.transform.position.y},
            {"ViewportLowerMiddleZ", lowerMiddle.transform.position.z},
            {"ViewportLowerLeftX", lowerLeft.transform.position.x},
            {"ViewportLowerLeftY", lowerLeft.transform.position.y},
            {"ViewportLowerLeftZ", lowerLeft.transform.position.z},
            {"ViewportUpperLeftX", upperLeft.transform.position.x},
            {"ViewportUpperLeftY", upperLeft.transform.position.y},
            {"ViewportUpperLeftZ", upperLeft.transform.position.z},
        };
        return data;
    }

}
