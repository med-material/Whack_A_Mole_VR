using NetMQ;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking.Types;
using UnityScript.Steps;

public class LineToDraw : MonoBehaviour
{
    public LineRenderer line; // used to draw a line
    public Vector3 startPosition; // starting pos of the drawn line
    public Vector3 endPosition; // ending pos of the drawn line
    public int lineID; // used to indentify lines
    public GameObject lineGo; // gameObject that will hold the lineRenderer
    public List<Vector3> linePoints = new List<Vector3>(); // list of points that construct the drawn line
    public Vector3 closestPoint = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue); // will be the closest point from the patient's cursor (and chose from "linePoints"

    public LineToDraw(int id, Vector3 start, Vector3 end) // constructor that initialize and create a gameOjbect holdind a line renderer and taking the name "Line [ID]"
    {
        this.lineID=id;
        startPosition=start;
        endPosition=end;

        GameObject lineGo = new GameObject("Line "+id);
        line=lineGo.AddComponent<LineRenderer>();
        line.material=new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
        line.positionCount=2;
        line.startWidth=0.15f;
        line.endWidth=0.1f;
        line.startColor=Color.green;
        line.endColor=Color.green;
        line.SetPosition(0, start);
        line.SetPosition(1, end);

        int numberOfPoints =20;

        for (int i = 0;i<numberOfPoints;i++) // divide the line in a certain amount of points so "linePoints" list can be created
        {
            float t = i/(numberOfPoints-1f);
            Vector3 point = Vector3.Lerp(startPosition, endPosition, t);
            linePoints.Add(point);
        }
    }
}
