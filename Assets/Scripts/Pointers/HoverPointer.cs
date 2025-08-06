using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

/*
Basic implementation of the pointer abstract class. Simply changes the color of the laser and 
the Cursor on shoot.
*/

public class HoverPointer : Pointer
{
    [SerializeField]
    private Color shootColor;

    [SerializeField]
    private Color badShootColor;

    [SerializeField]
    private float laserExtraWidthShootAnimation = .05f;

    private float shootTimeLeft;
    private float totalShootTime;
    private float dwellTime = 3.5f;
    private float dwellTimer = 0f;
    private delegate void Del();

    // Function called on VR update, since it can be faster/not synchronous to Update() function. Makes the Pointer slightly more reactive.
    public override void PositionUpdated()
    {
        if (!active) return;

        Vector2 pos = new Vector2(laserOrigin.transform.position.x, laserOrigin.transform.position.y);
        Vector3 mappedPosition = laserMapper.ConvertMotorSpaceToWallSpace(pos);
        Vector3 origin = laserOrigin.transform.position;
        Vector3 rayDirection = (mappedPosition - origin).normalized;

        RaycastHit hit;
        if (Physics.Raycast(laserOrigin.transform.position + laserOffset, rayDirection, out hit, 100f, Physics.DefaultRaycastLayers))
        {
            //UpdateLaser(true, hitPosition: laserOrigin.transform.InverseTransformPoint(hit.point), rayDirection: laserOrigin.transform.InverseTransformDirection(rayDirection));
            Vector3 hitPosition = laserOrigin.transform.InverseTransformPoint(hit.point);
            laser.SetPosition(1, hitPosition);
            cursor.SetPosition(hitPosition);
            hoverMole(hit);
        }
        else
        {
            Vector3 rayPosition = laserOrigin.transform.InverseTransformDirection(rayDirection) * maxLaserLength;
            laser.SetPosition(1, rayPosition);
            cursor.SetPosition(rayPosition);
            //UpdateLaser(false, rayDirection: laserOrigin.transform.InverseTransformDirection(rayDirection) * maxLaserLength);
        }
        if (SteamVR.active)
        {
            if (hit.collider)
            {
                Mole mole;
                if (hit.collider.gameObject.TryGetComponent<Mole>(out mole))
                {
                    Mole.States moleAnswer = mole.GetState();
                    if (moleAnswer == Mole.States.Enabled)
                    {
                        dwellTimer = dwellTimer + 0.1f;
                        if (dwellTimer > dwellTime)
                        {
                            pointerShootOrder++;
                            loggerNotifier.NotifyLogger(overrideEventParameters: new Dictionary<string, object>(){
                                {"ControllerSmoothed", directionSmoothed},
                                {"ControllerAimAssistState", System.Enum.GetName(typeof(Pointer.AimAssistStates), aimAssistState)},
                                {"LastShotControllerRawPointingDirectionX", transform.forward.x},
                                {"LastShotControllerRawPointingDirectionY", transform.forward.y},
                                {"LastShotControllerRawPointingDirectionZ", transform.forward.z},
                                {"LastShotBubbleRawPointingDirectionX", laserOrigin.transform.forward.x},
                                {"LastShotBubbleRawPointingDirectionY", laserOrigin.transform.forward.y},
                                {"LastShotBubbleRawPointingDirectionZ", laserOrigin.transform.forward.z},
                                {"LastShotBubbleFilteredPointingDirectionX", rayDirection.x},
                                {"LastShotBubbleFilteredPointingDirectionY", rayDirection.y},
                                {"LastShotBubbleFilteredPointingDirectionZ", rayDirection.z},
                            });

                            loggerNotifier.NotifyLogger("Pointer Shoot", EventLogger.EventType.PointerEvent, new Dictionary<string, object>()
                            {
                                {"PointerShootOrder", pointerShootOrder}
                            });
                            Shoot(hit);
                        }
                    }
                    else
                    {
                        if (dwellTimer > 0f)
                        {
                            dwellTimer = dwellTimer - 0.1f;
                        }
                    }
                }
                else
                {
                    if (dwellTimer > 0f)
                    {
                        dwellTimer = dwellTimer - 0.1f;
                    }
                }
            }
            else
            {
                if (dwellTimer > 0f)
                {
                    dwellTimer = dwellTimer - 0.1f;
                }
            }
        }
    }

    // Implementation of the behavior of the Pointer on shoot. 
    protected override void PlayShoot(bool correctHit)
    {
        Color newColor;
        if (correctHit) newColor = shootColor;
        else newColor = badShootColor;

        StartCoroutine(PlayShootAnimation(.5f, newColor));
    }

    // Ease function, Quart ratio.
    private float EaseQuartOut(float k)
    {
        return 1f - ((k -= 1f) * k * k * k);
    }

    // IEnumerator playing the shooting animation.
    private IEnumerator PlayShootAnimation(float duration, Color transitionColor)
    {
        shootTimeLeft = duration;
        totalShootTime = duration;

        // Generation of a color gradient from the shooting color to the default color (idle).
        Gradient colorGradient = new Gradient();
        GradientColorKey[] colorKey = new GradientColorKey[2] { new GradientColorKey(laser.startColor, 0f), new GradientColorKey(transitionColor, 1f) };
        GradientAlphaKey[] alphaKey = new GradientAlphaKey[2] { new GradientAlphaKey(laser.startColor.a, 0f), new GradientAlphaKey(transitionColor.a, 1f) };
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
        laser.startColor = startLaserColor;
        laser.endColor = EndLaserColor;
        cursor.SetColor(EndLaserColor);
        cursor.SetScaleRatio(1f);
    }
}
