using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using static PlayerTarget;

public class PlayerTarget : MonoBehaviour
{
    private WallInfo wallInfo;

    private bool inVerticalBoundaries = true;
    private bool inHorizontalBoundaries = true;
    private bool activeArrow = false;

    private BoundaryArrow spawnedArrowReference;

    [SerializeField]
    private BoundaryArrow boundaryArrowPrefab;

    [SerializeField]
    [Range(0, 3)]
    private int ID;

    [SerializeField]
    private bool linkedToCamera = false;

    [System.Serializable]
    public class BoundariesUpdate : UnityEvent<bool,int> { }
    public BoundariesUpdate boundariesUpdate;

    [System.Serializable]
    public class ArrowInvocationUpdate : UnityEvent<BoundaryArrow, GameObject> { }
    public ArrowInvocationUpdate arrowInvocationUpdate;

    [System.Serializable]
    public class ArrowDestructionUpdate : UnityEvent<int> { }
    public ArrowDestructionUpdate arrowDestructionUpdate;

    Vector3 spawnVectorL, spawnVectorR, spawnToUse;

    void Awake()
    {
        if (linkedToCamera == false)
        {
            spawnVectorL=new Vector3(-3.89f, 2.154f, 4);
            spawnVectorR=new Vector3(3.89f, 2.154f, 4);
        }
        if (linkedToCamera == true)
        {
            spawnVectorL=new Vector3(-3.89f, 2.154f, 4); // to modify
            spawnVectorR=new Vector3(3.89f, 2.154f, 4); // to modify
        }
        
    }

    void Start()
    {
        if (ID == 0 || ID == 2)
        {
            spawnToUse = spawnVectorR;
        }
        else if (ID == 1 || ID == 3)
        {
            spawnToUse = spawnVectorL;
        }
    }

    void Update()
    {
        cursorMovement();
        var cursorPos = GetCursorPosition(gameObject);
        
        if (cursorPos.x < wallInfo.lowestX + wallInfo.lowestX/2 && inHorizontalBoundaries==true) // Left boundary
        {
            if (activeArrow == false)
            {
                spawnedArrowReference=Instantiate(boundaryArrowPrefab, spawnToUse, Quaternion.identity);
                arrowInvocationUpdate.Invoke(spawnedArrowReference, gameObject);
                activeArrow=true;
            }

            boundariesUpdate.Invoke(true,ID);
            inHorizontalBoundaries=false;
            Debug.Log("outside left");
        }

        if (cursorPos.x > wallInfo.highestX + wallInfo.highestX/2 && inHorizontalBoundaries==true) // Right boundary
        {
            if (activeArrow==false)
            {
                spawnedArrowReference=Instantiate(boundaryArrowPrefab, spawnToUse, Quaternion.identity);
                arrowInvocationUpdate.Invoke(spawnedArrowReference, gameObject);
                activeArrow=true;
            }

            boundariesUpdate.Invoke(true,ID);
            inHorizontalBoundaries=false;
            Debug.Log("outside right");
        }

        if (cursorPos.y < wallInfo.lowestY - wallInfo.highestX/2 && inVerticalBoundaries==true) // Bottom boundary
        {
            if (activeArrow==false)
            {
                spawnedArrowReference=Instantiate(boundaryArrowPrefab, spawnToUse, Quaternion.identity);
                arrowInvocationUpdate.Invoke(spawnedArrowReference, gameObject);
                activeArrow=true;
            }
            boundariesUpdate.Invoke(true,ID);
            inVerticalBoundaries=false;
            Debug.Log("outside bottom");
        }

        if (cursorPos.y > wallInfo.highestY + wallInfo.highestX/2 && inVerticalBoundaries==true) // Top boundary
        {

            if (activeArrow==false)
            {
                spawnedArrowReference=Instantiate(boundaryArrowPrefab, spawnToUse, Quaternion.identity);
                arrowInvocationUpdate.Invoke(spawnedArrowReference, gameObject);
                activeArrow=true;
            }
            boundariesUpdate.Invoke(true,ID);
            inVerticalBoundaries=false;
            Debug.Log("outside top");
        }

        if ((wallInfo.lowestX + wallInfo.lowestX/2) < cursorPos.x && cursorPos.x < (wallInfo.highestX + wallInfo.highestX/2) && inHorizontalBoundaries==false) // Inside Horizontal boundaries
        {
            boundariesUpdate.Invoke(false,ID);
            Destroy(spawnedArrowReference.gameObject,1f);
            Debug.Log("Inside Horizontal boundaries");
            inHorizontalBoundaries=true;
            activeArrow=false;
        }

        if (((wallInfo.highestY + wallInfo.highestX/2) > cursorPos.y && cursorPos.y > (wallInfo.lowestY - wallInfo.highestX/2)) && inVerticalBoundaries==false) // inside vertical boundaries
        {
            boundariesUpdate.Invoke(false,ID);
            Destroy(spawnedArrowReference.gameObject,1f);
            Debug.Log("inside vertical boundaries");
            inVerticalBoundaries=true;
            activeArrow=false;
        }

        resetCursorPos();
        wallinformation();
    }

    public void OnWallInfoCreated(WallInfo wallInformations)
    {
        wallInfo = wallInformations;
    }

    public int GetID()
    {
        return this.ID;
    }

    private Vector3 GetCursorPosition(GameObject gameobj)
    {
        return gameobj.transform.position;
    }

    ////////////////////////////////////////////////////////test functions///////////////////////////////////////////////////////////////////////////////////////////// 



    float speed = 5.0f;
    private void cursorMovement()
    {
        var move = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0);
        gameObject.transform.position += move * speed * Time.deltaTime;
    }

    private void resetCursorPos()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            gameObject.transform.position = new Vector3(wallInfo.lowestX, wallInfo.lowestY, wallInfo.highestZ-1);
        }
    }

    private void wallinformation()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log("HX"+wallInfo.highestX);
            Debug.Log("HY"+wallInfo.highestY);
            Debug.Log("LX"+wallInfo.lowestX);
            Debug.Log("LY"+wallInfo.lowestY);
            Debug.Log("HZ"+wallInfo.highestZ);
            Debug.Log(""+wallInfo);
        }
    }

    // For now the update function gets cursor position every frame, 
    // And arrows appear when you get out of the boundaries of the game
    // cursor position : (x,y) / if inside rectangle created by points A B C D nothing, else : boundary arrow shown

    // private float pointA; // A : Top left corner : (lowestX + lowestX/2, highestY + highestX/2)
    // private float pointB; // B : Top right corner : (highestX + highestX/2, highestY + highestX/2)
    // private float pointC; // C : Bottom right corner : (highestX + highestX/2, lowestY - highestX/2)
    // private float pointD; // D : Bottom left corner : (lowestX + lowestX/2, lowestY - highestX/2)

    // ==> (lowestX + lowestX/2) < cursoPos(x) < (highestX + highestX/2)
    // ==> (highestY + highestX/2) < cursoPos(y) < (lowestY - highestX/2
}
