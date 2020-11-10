using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerMirror : MonoBehaviour
{

    [SerializeField]
    private GameObject controllerToMirror;

    [SerializeField]
    private GameObject laserOriginToMirror;

    [SerializeField]
    private LaserMapper laserMapper;

    [SerializeField]
    private LineRenderer laser;

    [SerializeField]
    private LaserCursor cursor;

    [SerializeField]
    protected float maxLaserLength;

    Vector3 newPos;
    Quaternion newRot;
    Vector3 pos;
    Vector3 mappedPosition;
    Vector3 rayPosition;
    Vector3 origin;
    Vector3 rayDirection;
    RaycastHit hit;
    Mole hoveredMole;

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

        UpdatePointer();
    }

    void UpdatePointer()
    {
        pos = new Vector2(laserOriginToMirror.transform.position.x, laserOriginToMirror.transform.position.y);
        mappedPosition = laserMapper.ConvertMotorSpaceToWallSpace(pos);
        origin = laserOriginToMirror.transform.position;
        rayDirection = (mappedPosition - origin).normalized;
        if (Physics.Raycast(laserOriginToMirror.transform.position, rayDirection, out hit, 100f, Physics.DefaultRaycastLayers))
        {
            Vector3 hitPosition = laserOriginToMirror.transform.InverseTransformPoint(hit.point);
            laser.SetPosition(1, hitPosition);
            cursor.SetPosition(hitPosition);
            hoverMole(hit);
        }
        else
        {
            rayPosition = laserOriginToMirror.transform.InverseTransformDirection(rayDirection) * maxLaserLength; 
            laser.SetPosition(1, rayPosition);
            cursor.SetPosition(rayPosition);
        }
    }

    // Checks if a Mole is hovered and tells it to play the hovered efect.
    private void hoverMole(RaycastHit hit)
    {
        Mole mole;
        if (hit.collider.gameObject.TryGetComponent<Mole>(out mole))
        {
            if (mole != hoveredMole)
            {
                if (hoveredMole)
                {
                    hoveredMole.OnHoverLeave();
                }
                hoveredMole = mole;
                hoveredMole.OnHoverEnter();
            }
        }
        else
        {
            if (hoveredMole)
            {
                hoveredMole.OnHoverLeave();
                hoveredMole = null;
            }
        }
    }

    void OnDrawGizmos() {
        // Draw cube to visualize the latest calculated wall space coordinate.
        Gizmos.DrawCube(pos, new Vector3(0.05f,0.05f,0.05f)); 
        Gizmos.DrawCube(mappedPosition, new Vector3(0.05f,0.05f,0.05f)); 
    }

}
