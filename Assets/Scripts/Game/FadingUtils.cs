using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public enum FadeAction
{
    In,
    Out
}

public enum SetEnable
{
    On,
    Off
}

public enum AutoPlay
{
    On,
    Off
}

public static class FadingUtils
{
    // Is mandatory if we want to fire coroutines from a static class
    public static MonoBehaviour instance;

    // Object Identifier
    public enum ComponentToFade
    {
        CanvasGroup,
        Image,
        SpriteRenderer,
        MeshRenderer,
        Text,
        ERROR
    }

    // Helping functions
    public static float GetAlpha(Component toFade)
    {
        float alphaToReturn = 0f;
        if (toFade is CanvasGroup)
        {
            CanvasGroup o = (CanvasGroup)toFade;
            alphaToReturn = o.alpha;
        }
        else if (toFade is Image)
        {
            Image o = (Image)toFade;
            alphaToReturn = o.color.a;
        }
        else if (toFade is SpriteRenderer)
        {
            SpriteRenderer o = (SpriteRenderer)toFade;
            alphaToReturn = o.color.a;
        }
        else if (toFade is MeshRenderer)
        {
            MeshRenderer o = (MeshRenderer)toFade;
            alphaToReturn = o.material.color.a;
        }
        else if (toFade is Text)
        {
            Text o = (Text)toFade;
            alphaToReturn = o.color.a;
        }
        return alphaToReturn;
    } // reads alpha value of an object

    public static void SetAlpha(Component toFade, float targetAlpha)
    {
        if (toFade is CanvasGroup)
        {
            CanvasGroup o = (CanvasGroup)toFade;
            o.alpha = targetAlpha;
        }
        else if (toFade is Image)
        {
            Image o = (Image)toFade;
            var compColor = o.color;
            o.color = new Color(compColor.r, compColor.g, compColor.b, targetAlpha);
        }
        else if (toFade is SpriteRenderer)
        {
            SpriteRenderer o = (SpriteRenderer)toFade;
            var compColor = o.color;
            o.color = new Color(compColor.r, compColor.g, compColor.b, targetAlpha);
        }
        else if (toFade is MeshRenderer)
        {
            MeshRenderer o = (MeshRenderer)toFade;
            var compColor = o.material.color;
            o.material.color = new Color(compColor.r, compColor.g, compColor.b, targetAlpha);
        }
        else if (toFade is Text)
        {
            Text o = (Text)toFade;
            var compColor = o.color;
            o.color = new Color(compColor.r, compColor.g, compColor.b, targetAlpha);
        }
    } // sets alpha value of an object

    // FadeUtils principal function call
    public static Coroutine FadeRoutine(MonoBehaviour handler, GameObject Obj, float fadeTime, FadeAction fadeDirection, float fadeDelay = 0f,
        SetEnable setEnable = SetEnable.Off)
    {
        instance = handler;

        switch (fadeDirection)
        {
            case FadeAction.In:
                return instance.StartCoroutine(FadeInColor(Obj, fadeTime, fadeDelay, setEnable));
                break;

            case FadeAction.Out:
                return instance.StartCoroutine(FadeOutColor(Obj, fadeTime, fadeDelay, setEnable));
                break;
        }

        return null;
    }

    // Identification function of the object to fade
    public static Component ComponentIdentification(GameObject Obj)
    {
        Component[] listComponents = Obj.GetComponents(typeof(Component)); // create a list of all components located inside the object to fade

        int iterator = 0; // helps getting outside of the loop if no compatible component is found, once all components have been verified

        while (iterator < listComponents.Length) // browse all components of the gameObject
        {
            if (listComponents[iterator] is CanvasGroup)
                return listComponents[iterator];
            if (listComponents[iterator] is Image)
                return listComponents[iterator];
            if (listComponents[iterator] is SpriteRenderer)
                return listComponents[iterator];
            if (listComponents[iterator] is MeshRenderer)
                return listComponents[iterator];
            if (listComponents[iterator] is Text)
                return listComponents[iterator];
            iterator++;
        }
        return null; // if no compatible component is found
    }

    // Generic fading in function
    public static IEnumerator FadeInColor(GameObject Obj, float fadeTime, float fadeDelay, SetEnable setEnable)
    {
        if (fadeDelay != 0) // play delay
        {
            yield return new WaitForSeconds(fadeDelay);
        }

        Component componentToUse = ComponentIdentification(Obj);
        float baseAlpha = GetAlpha(componentToUse); // getting "base" alpha for of the object

        if (baseAlpha == 1f) { yield break; } // already opaque, exit early.

        float alpha;

        if (setEnable == SetEnable.On) // enabling
        {
            Obj.SetActive(true);
            SetAlpha(componentToUse, 0); // setting up alpha to 0 so we can fade in
        }

        for (float i = 0; i < fadeTime; i += Time.deltaTime) // play fade effect
        {
            alpha = math.remap(0, fadeTime, baseAlpha, 1f, i);
            SetAlpha(componentToUse, alpha);
            yield return null;
        }

        SetAlpha(componentToUse, 1f);
    }

    // Generic fading out function
    public static IEnumerator FadeOutColor(GameObject Obj, float fadeTime, float fadeDelay, SetEnable setEnable)
    {
        if (fadeDelay != 0) // play delay
        {
            yield return new WaitForSeconds(fadeDelay);
        }

        Component componentToUse = ComponentIdentification(Obj);
        float baseAlpha = GetAlpha(componentToUse); // getting "base" alpha for of the object
        float alpha;

        if (baseAlpha == 0f) { yield break; } // already transparent, exit early.

        for (float i = 0; i < fadeTime; i += Time.deltaTime) // play fade effect
        {
            alpha = 1 - math.remap(0, fadeTime, 0f, baseAlpha, i);
            SetAlpha(componentToUse, alpha); //FadeOutAlpha(i, fadeTime, GetAlpha(componentToUse, Obj)));
            yield return null;
        }

        SetAlpha(componentToUse, 0f);

        if (setEnable == SetEnable.On) // disabling
        {
            Obj.SetActive(false);
        }
    }
}
