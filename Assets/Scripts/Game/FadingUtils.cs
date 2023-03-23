using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;

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
        //LineRenderer,
        //TrailRenderer
        ERROR
    }

    // Helping functions
    public static float GetAlpha(ComponentToFade toFade, GameObject Obj) 
    {
        float alphaToReturn = 0f;
        switch (toFade)
        {
            case ComponentToFade.CanvasGroup:
                alphaToReturn=Obj.GetComponent<CanvasGroup>().alpha;
                break;
            case ComponentToFade.Image:
                alphaToReturn=Obj.GetComponent<Image>().color.a;
                break;
            case ComponentToFade.SpriteRenderer:
                alphaToReturn=Obj.GetComponent<SpriteRenderer>().color.a;
                break;
            case ComponentToFade.MeshRenderer:
                alphaToReturn=Obj.GetComponent<MeshRenderer>().material.color.a;
                break;
            case ComponentToFade.Text:
                alphaToReturn=Obj.GetComponent<Text>().color.a;
                break;
        }
        return alphaToReturn;
    } // reads alpha value of an object

    public static void SetAlpha(ComponentToFade toFade, GameObject Obj, float targetAlpha) 
    {
        switch (toFade)
        {
            case ComponentToFade.CanvasGroup:
                Obj.GetComponent<CanvasGroup>().alpha=targetAlpha;
                break;
            case ComponentToFade.Image:
                var compColor = Obj.GetComponent<Image>().color;
                Obj.GetComponent<Image>().color=new Color(compColor.r, compColor.g, compColor.b, targetAlpha);
                break;
            case ComponentToFade.SpriteRenderer:
                compColor = Obj.GetComponent<SpriteRenderer>().color;
                Obj.GetComponent<SpriteRenderer>().color = new Color(compColor.r, compColor.g, compColor.b, targetAlpha);
                break;
            case ComponentToFade.MeshRenderer:
                compColor = Obj.GetComponent<MeshRenderer>().material.color;
                Obj.GetComponent<MeshRenderer>().material.color = new Color(compColor.r, compColor.g, compColor.b, targetAlpha);
                break;
            case ComponentToFade.Text:
                compColor = Obj.GetComponent<Text>().color;
                Obj.GetComponent<Text>().color = new Color(compColor.r, compColor.g, compColor.b, targetAlpha);
                break;
        }
    } // sets alpha value of an object

    // FadeUtils principal function call
    public static void FadeRoutine(MonoBehaviour handler, GameObject Obj, float fadeTime, FadeAction fadeDirection, float fadeDelay = 0f,
        SetEnable setEnable = SetEnable.Off) 
    { 
        instance=handler;
        switch (fadeDirection)
        {
            case FadeAction.In:
                instance.StartCoroutine(FadeInColor(Obj, FadeIdentification(Obj), fadeTime, fadeDelay, setEnable));
                break;

            case FadeAction.Out:
                instance.StartCoroutine(FadeOutColor(Obj, FadeIdentification(Obj), fadeTime, fadeDelay, setEnable));
                break;
        }
    }

    // Identification function of the object to fade
    public static ComponentToFade FadeIdentification(GameObject Obj) 
    {
        Component[] listComponents = Obj.GetComponents(typeof(Component)); // create a list of all components located inside the object to fade

        int iterator = 0; // helps getting outside of the loop if no compatible component is found, once all components have been verified

        while (iterator<listComponents.Length) // browse all components of the gameObject
        {
            if (listComponents[iterator] is CanvasGroup)
                return ComponentToFade.CanvasGroup;
            if (listComponents[iterator] is Image)
                return ComponentToFade.Image;
            if (listComponents[iterator] is SpriteRenderer)
                return ComponentToFade.SpriteRenderer;
            if (listComponents[iterator] is MeshRenderer)
                return ComponentToFade.MeshRenderer;
            if (listComponents[iterator] is Text)
                return ComponentToFade.Text;
            iterator++;
        }
        return ComponentToFade.ERROR; // if no compatible component is found
    }

    // Generic fading in function
    public static IEnumerator FadeInColor(GameObject Obj, ComponentToFade componentToUse, float fadeTime, float fadeDelay, SetEnable setEnable) 
    {
        if (fadeDelay!=0) // play delay
        {
            yield return new WaitForSeconds(fadeDelay);
        }

        if (setEnable == SetEnable.On) // enabling
        {
            Obj.SetActive(true);
        }

        SetAlpha(componentToUse, Obj, 0); // setting up alpha to 0 so we can fade in

        for (float i = 0;i<=fadeTime;i+=Time.deltaTime) // play fade effect
        {
            SetAlpha(componentToUse, Obj, FadeInAlpha(i, fadeTime, GetAlpha(componentToUse, Obj)));
            yield return null;
        }
    }

    // Generic fading out function
    public static IEnumerator FadeOutColor(GameObject Obj, ComponentToFade componentToUse, float fadeTime, float fadeDelay, SetEnable setEnable) 
    {
        float baseAlpha = GetAlpha(componentToUse, Obj); // getting "base" alpha for of the object

        if (fadeDelay!=0) // play delay
        {
            yield return new WaitForSeconds(fadeDelay);
        }

        SetAlpha(componentToUse, Obj, baseAlpha); // Setting alpha to base alpha so we can fade out

        for (float i = 0;i<=fadeTime;i+=Time.deltaTime) // play fade effect
        {
            SetAlpha(componentToUse, Obj, FadeOutAlpha(i, fadeTime, GetAlpha(componentToUse, Obj)));
            yield return null;
        }

        if (setEnable == SetEnable.On) // disabling
        {
            Obj.SetActive(false);
        }
    }

    // Does the actual alpha shifting towards 1 (apparent object)
    public static float FadeInAlpha(float iterator, float fadeTime, float alpha) 
    {
        alpha=math.remap(0, fadeTime, 0, 1, iterator);
        return alpha;
    }

    // Does the actual alpha shifting towards 0 (transparent object)
    public static float FadeOutAlpha(float iterator, float fadeTime, float alpha) 
    {
        alpha=1-math.remap(0, fadeTime, 0, 1, iterator);
        return alpha;
    }
}
