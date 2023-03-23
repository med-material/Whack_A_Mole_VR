using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Valve.VR.SteamVR_PlayArea;

public enum FeedBackMode
{
    LineTrail,
    ObjectTrail,
    OnlyScale
}

public enum ScaleChanging
{
    On,
    Off
}

public enum PointToDraw
{
    Add,
    Remove
}

public class TargetAssist : MonoBehaviour
{
    [SerializeField]
    private GameObject cursor; // reference to the patient's cursor in the hierarchy

    [SerializeField]
    private FeedBackMode feedBackMode; // enum to specify which feedback mode to use (not implemented)

    [SerializeField]
    private ScaleChanging scaleChanging; // enum to specify which feedback mode to use (not implemented)

    public Dictionary<int, LineToDraw> currentLines; // used to store the differents currently drawn lines 

    private TrailRenderer trailRenderer; // used to draw a trail after the patient's cursor

    private LineToDraw closestLine = null; // specify which is the closest line from the currentLines

    // variable below are used in ChangeObject, ChangeTrail and ChangeScale functions (not working properly)
    /*private float minDistance = 0.001f; // Minimum distance for the object to be at its smallest size
    private float maxDistance = 2f; // Maximum distance for the object to be at its largest size
    private float minScale = 2f; // Minimum scale for the object
    private float maxScale = 4f; // Maximum scale for the object

    float minWidth = 0.125f; // Minimum width for the trail
    float maxWidth = 0.25f; // Maximum width for the trail*/

    // variable used in test function to move the patient's cursor without headset and controller
    private float moveSpeed = 2f;

    private void Awake()
    {
        currentLines = new Dictionary<int, LineToDraw>();
        DiskMole.trajectoryLinesUpdate+=UpdateLines;
        DiskMole.trajectoryReset+=ResetPositions;
    }

    private void Start()
    {
        InitLineTrail();
    }

    private void InitLineTrail()
    {
        trailRenderer=gameObject.GetComponent<TrailRenderer>();
        Color trailColor = Color.blue;
        trailColor.a=0.3f;
        trailRenderer.startColor=trailColor;
        trailColor=Color.blue;
        trailColor.a=0.6f;
        trailRenderer.endColor=trailColor;
        trailRenderer.startWidth=0.1f;
        trailRenderer.endWidth=0.1f;
    }

    public void UpdateLines(PointToDraw action, Vector3 molePos, int moleID) // event function used to add lines to draw or remove them
    {
        if (action==PointToDraw.Add)
        {
            LineToDraw lineToDraw = new LineToDraw(moleID, cursor.transform.position, molePos);
            currentLines.Add(moleID, lineToDraw);
            closestLine=null;
        }
        else if (action==PointToDraw.Remove)
        {
            currentLines.Remove(moleID);
            Destroy(GameObject.Find("Line "+moleID));
            closestLine=null;
        }
    }

    public void ResetPositions()
    {
        foreach (LineToDraw obj in currentLines.Values)
        {
            if (obj.lineGo!=null)
                Destroy(obj.lineGo);
        }
        currentLines= new Dictionary<int, LineToDraw>();
        closestLine=null;
    } // event function used to reset all lines after launching a game / exiting a game

    private void FindClosestLine()
    {
        Vector3 closestPoint = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 globalClosestPoint = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

        foreach (LineToDraw line in currentLines.Values)
        {
            closestPoint=line.linePoints.OrderBy(p => Vector3.Distance(p, cursor.transform.position)).First();
            line.closestPoint=closestPoint;
            if (Vector3.Distance(closestPoint, cursor.transform.position)<Vector3.Distance(globalClosestPoint, cursor.transform.position))
            {
                globalClosestPoint=closestPoint;
                closestLine=line;
            }
        }
    } // find which line is the closest to the cursor of the player

    private void ChangeColor()
    {
        if (currentLines!=null)
        {
            foreach (LineToDraw line in currentLines.Values)
            {
                if (closestLine.lineID==line.lineID)
                {
                    line.line.startColor=Color.green;
                    line.line.endColor=Color.green;
                }
                else
                {
                    line.line.startColor=Color.red;
                    line.line.endColor=Color.red;
                }
            }
        }
    } // changes the color of the closest line to the cursor to green and tjhe others to red

    /*
    private void ChangeScale()
    {
        Vector3 max = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        if (currentLines.Count >= 1)
        {
            if (closestLine.closestPoint!=max)
            {
                float distance = Vector3.Distance(cursor.transform.position, closestLine.closestPoint);
                float scale = Mathf.Lerp(maxScale, minScale, (distance-minDistance)/(maxDistance-minDistance)); 
                Debug.Log("scale : "+scale);
                cursor.transform.localScale=new Vector3(scale, scale, scale);
            }
        }
    }*/

    /*
    private void ChangeTrail()
    {
        Vector3 max = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        if (currentLines.Count>=1)
        {
            if (closestLine.closestPoint!=max)
            {
                float distance = Vector3.Distance(cursor.transform.position, closestLine.closestPoint);
                float width = Mathf.Lerp(maxWidth, minWidth, (distance-minDistance)/(maxDistance-minDistance));
                trailRenderer.startWidth=width;
                trailRenderer.endWidth=width;
            }
        }
    }*/

    /*private void ChangeObject()
    {
        Vector3 max = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        if (currentLines.Count>=1)
        {
            if (closestLine.closestPoint!=max)
            {
                GameObject spawnedObject = Instantiate(cursor, cursor.transform.position, Quaternion.identity);
                spawnedObject.transform.localScale=new Vector3(cursor.transform.localScale.x, cursor.transform.localScale.y, cursor.transform.localScale.z);
                Destroy(spawnedObject, 1.25f);
            }
        }
    }*/

    public void Update()
    {
        FindClosestLine();
        ChangeColor();

        if (feedBackMode == FeedBackMode.LineTrail)
        {
            if (scaleChanging==ScaleChanging.On)
            {
                //ChangeScale();
                //ChangeTrail();
            }
            else if (scaleChanging==ScaleChanging.Off)
            {
                //ChangeTrail();
            }
        }
        else if (feedBackMode == FeedBackMode.ObjectTrail)
        {
            if (scaleChanging==ScaleChanging.On)
            {
                //ChangeScale();
                //ChangeObject();
            }
            else if (scaleChanging==ScaleChanging.Off)
            {
                //ChangeObject();
            }
        }
        else if (feedBackMode == FeedBackMode.OnlyScale)
        {
            //ChangeScale();
        }

        test();
    }

    public void test() // test function
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("clostest line :"+ closestLine.lineID);
            Debug.Log("closest point :"+ closestLine.closestPoint);
        }
        if (Input.GetKey(KeyCode.Z))
        {
            cursor.transform.Translate(Vector3.up*moveSpeed*Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.S))
        {
            cursor.transform.Translate(Vector3.down*moveSpeed*Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.Q))
        {
            cursor.transform.Translate(Vector3.left*moveSpeed*Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.D))
        {
            cursor.transform.Translate(Vector3.right*moveSpeed*Time.deltaTime);
        }
    }
}