using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerMirror : MonoBehaviour
{

    [SerializeField]
    private GameObject controllerToMirror;

    [SerializeField]
    private GameObject playerController;

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

    [SerializeField]
    private Color shootColor;

    [SerializeField]
    private Color badShootColor;

    [SerializeField]
    private float laserExtraWidthShootAnimation = .05f;

    private float shootTimeLeft;
    private float totalShootTime;

    private Pointer pointerToMirror;

    Vector3 newPos;
    Quaternion newRot;
    Vector3 pos;
    Vector3 mappedPosition;
    Vector3 rayPosition;
    Vector3 origin;
    Vector3 rayDirection;
    Mole hoveredMole;
    bool didHit = false;
    RaycastHit hit;

    private float laserWidth;
    private Color laserColor;

    void Start()
    {
        pointerToMirror = controllerToMirror.GetComponent<Pointer>();
        pointerToMirror.onPointerShoot.AddListener(onPointerShoot);
        laserWidth = laser.startWidth;
        laserColor = laser.startColor;
        //var lastChild = playerController.gameObject.transform.childCount-1;
        //var controllerGeom = Instantiate(playerController.gameObject.transform.GetChild(lastChild));
        //controllerGeom.transform.SetParent(this.gameObject.transform);
        //controllerGeom.transform.position = new Vector3(0f,0f,0f);
        //controllerGeom.transform.eulerAngles = new Vector3(0f, 0f, 0f);
        //var motorspace = laserMapperToMirror.GetMotorSpace();
        //motorspace.pos = new Vector3(-motorspace.pos.x, motorspace.pos.y, motorspace.pos.z);
        //laserMapper.SetMotorSpace(motorspace);
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
            didHit = true;
            Vector3 hitPosition = laserOriginToMirror.transform.InverseTransformPoint(hit.point);
            laser.SetPosition(1, hitPosition);
            cursor.SetPosition(hitPosition);
            hoverMole(hit);
        }
        else
        {
            didHit = false;
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

    public void onPointerShoot()
    {
        Shoot();
    }

    // Shoots a raycast. If Mole is hit, calls its Pop() function. Depending on the hit result, plays the hit/missed shooting animation
    // and raises a "Mole Missed" event.
    private void Shoot()
    {
        if (didHit == false) return;
        Mole mole;

        if (hit.collider)
        {
            if (hit.collider.gameObject.TryGetComponent<Mole>(out mole))
            {
                Mole.MolePopAnswer moleAnswer = mole.Pop(hit.point);

                //if (moleAnswer == Mole.MolePopAnswer.Disabled) RaiseMoleMissedEvent(hit.point);
                PlayShoot(moleAnswer == Mole.MolePopAnswer.Ok);
                return;
            }
            //RaiseMoleMissedEvent(hit.point);
        }
        PlayShoot(false);
    }

    // Implementation of the behavior of the Pointer on shoot. 
    private void PlayShoot(bool correctHit)
    {
        Color newColor;
        if (correctHit) newColor = shootColor;
        else newColor = badShootColor;

        StartCoroutine(PlayShootAnimation(.5f, newColor));
    }

    // Ease function, Quart ratio.
    private float EaseQuartOut (float k) 
    {
        return 1f - ((k -= 1f)*k*k*k);
    }

    // IEnumerator playing the shooting animation.
    private IEnumerator PlayShootAnimation(float duration, Color transitionColor)
    {
        shootTimeLeft = duration;
        totalShootTime = duration;

        // Generation of a color gradient from the shooting color to the default color (idle).
        Gradient colorGradient = new Gradient();
        GradientColorKey[] colorKey = new GradientColorKey[2]{new GradientColorKey(laser.startColor, 0f), new GradientColorKey(transitionColor, 1f)};
        GradientAlphaKey[] alphaKey = new GradientAlphaKey[2]{new GradientAlphaKey(laser.startColor.a, 0f), new GradientAlphaKey(transitionColor.a, 1f)};
        colorGradient.SetKeys(colorKey, alphaKey);

        // Playing of the animation. The laser and Cursor color and scale are interpolated following the easing curve from the shooting values (increased size, red/green color)
        // to the idle values
        while (shootTimeLeft > 0f)
        {
            float shootRatio = (totalShootTime - shootTimeLeft) / totalShootTime;
            float newLaserWidth = 0f;
            Color newLaserColor = new Color();

            newLaserWidth = laserWidth + ((1 - EaseQuartOut(shootRatio)) * laserExtraWidthShootAnimation);
            newLaserColor = colorGradient.Evaluate(1 - EaseQuartOut(shootRatio));

            laser.startWidth = newLaserWidth;
            laser.endWidth = newLaserWidth;

            laser.startColor = newLaserColor;
            laser.endColor = newLaserColor;
            cursor.SetColor(newLaserColor);
            cursor.SetScaleRatio(newLaserWidth / laserWidth);

            shootTimeLeft -= Time.deltaTime;

            yield return null;
        }

        // When the animation is finished, resets the laser and Cursor to their default values. 
        laser.startWidth = laserWidth;
        laser.endWidth = laserWidth;
        laser.startColor = laserColor;
        laser.endColor = laserColor;
        cursor.SetScaleRatio(1f);
    }

}
