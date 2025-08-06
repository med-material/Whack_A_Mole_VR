using System.Collections;
using UnityEngine;

/*
Basic implementation of the pointer abstract class. Simply changes the color of the laser and
the Cursor on shoot.
*/

public class ClickPointer : Pointer
{
    [SerializeField]
    private Color shootColor;

    [SerializeField]
    private Color badShootColor;

    [SerializeField]
    private float laserExtraWidthShootAnimation = .05f;

    private float shootTimeLeft;
    private float totalShootTime;
    private delegate void Del();


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
