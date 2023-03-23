using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;
using UnityEngine.Analytics;
using static UnityEngine.GraphicsBuffer;

public class BoundaryArrow : MonoBehaviour
{
    private bool cursorIsOut;
    private bool arrowOnScreen;

    private Animation animationPlayer;
    private string playingClip = "";

    private PlayerTarget boundTarget;

    [Range(0, 3)]
    private int ID;

    public float offset = 5.0f;

    public void Awake()
    {

    }

    public void Start()
    {
        animationPlayer=gameObject.GetComponent<Animation>();
    }

    public void Update()
    {
        Quaternion lookAtRotation=Quaternion.LookRotation(boundTarget.transform.position-transform.position);

        if (cursorIsOut==true && arrowOnScreen==false)
        {
            RotateArrowToCursor(gameObject, lookAtRotation);
            PlayAnimation("ArrowAppearing");
            arrowOnScreen=true;
        }

        if (cursorIsOut==true && arrowOnScreen==true)
        {
            RotateArrowToCursor(gameObject, lookAtRotation);
            PlayAnimation("ArrowBouncing");
        }

        if (cursorIsOut==false && arrowOnScreen==true)
        {
            RotateArrowToCursor(gameObject, lookAtRotation);
            PlayAnimation("ArrowDisappearing");
            arrowOnScreen=false;
        }

        if (cursorIsOut==false && arrowOnScreen==false)
        {
            RotateArrowToCursor(gameObject, lookAtRotation);
            PlayAnimation("ArrowDisappearing");
            arrowOnScreen=false;
        }
        arrowinformation();
    }

    // Plays an animation clip.
    private void PlayAnimation(string animationName)
    {
        playingClip=animationName;
        animationPlayer.Play(playingClip);
    }

    private void RotateArrowToCursor(GameObject gamobject, Quaternion lookRotation)
    {
        if (gamobject.transform.rotation!=lookRotation)
        {
            Vector3 tmp=lookRotation.eulerAngles;
            tmp.y-=90;                            // fix the rotation
            tmp.z=(tmp.x*-1)-5.0f;                // uses the x value to aim vertically and iff offset so it aims at the center of the target
            tmp.x=0;                              // stops the arrow from rotating in the wrong axis

            Quaternion newRotation = Quaternion.Euler(tmp);

            gameObject.transform.rotation=Quaternion.RotateTowards(gameObject.transform.rotation, newRotation, 1000);
        }
    }

    public void IsCursorOut(bool isItTrue, int identifier)
    {
        if (ID==identifier)
        {
            cursorIsOut=isItTrue;
            Debug.Log("is it true : "+isItTrue);
        }
    }

    public void OnArrowInvocation(BoundaryArrow arrowToSpawn, GameObject playerTarget)
    {
        arrowToSpawn.ID=arrowToSpawn.boundTarget.GetID();

        if (ID==playerTarget.GetComponent<PlayerTarget>().GetID())
        {
            arrowToSpawn.cursorIsOut=true;
            arrowToSpawn.arrowOnScreen=false;
            arrowToSpawn.boundTarget=playerTarget.GetComponent<PlayerTarget>();
        }
    }

    private void arrowinformation()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            Debug.Log("cursorIsOut ? : "+cursorIsOut);
            Debug.Log("arrowOnScreen ? : "+arrowOnScreen);
            Debug.Log("boundTarget ? : "+boundTarget);
            Debug.Log("ID ? : "+ID);
        }    
    }
}

        
    
