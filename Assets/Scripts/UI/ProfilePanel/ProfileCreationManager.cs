using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

/*
Class managing the Profile creation part of the profile panel. Checks if the name and mail are correct, if they are,
asks the PanelManager for profile creation.
*/

public class ProfileCreationManager : MonoBehaviour
{
    [SerializeField]
    private InputField nameInputField;

    [SerializeField]
    private InputField mailInputField;

    [SerializeField]
    private Text feedbackText;

    [SerializeField]
    private Color feedbackTextBadColor;

    [SerializeField]
    private Color feedbackTextGoodColor;

    [SerializeField]
    private Color feedbackTextHighlightMask;

    [SerializeField]
    private float feedbackTextHighlightDuration = 0.5f;

    private ProfilePanelManager panelManager;
    private float durationLeft;

    // Email match pattern taken from https://www.codeproject.com/Articles/22777/Email-Address-Validation-Using-Regular-Expression
    private static string matchEmailPattern = 
	    @"^(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
        + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?
		[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
        + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?
		[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
        + @"([a-zA-Z0-9]+[\w-]+\.)+[a-zA-Z]{1}[a-zA-Z0-9-]{1,23})$";

    void Awake()
    {
        panelManager = FindObjectOfType<ProfilePanelManager>();
    }

    // Specific profile properties to be passed on profile creation. For now we pass an empty Dictionary.
    public void CreateProfile()
    {
        if (nameInputField.text.Length == 0)
        {
            UpdateFeedBackText("Missing name !", false);
            return;
        }
        if (mailInputField.text.Length == 0)
        {
            UpdateFeedBackText("Missing e-mail address !", false);
            return;
        } 
        if (!Regex.IsMatch(mailInputField.text, matchEmailPattern))
        {
            UpdateFeedBackText("Bad e-mail address !", false);
            return;
        }

        if(panelManager.ProfileCreated(nameInputField.text, mailInputField.text, new Dictionary<string, string>()))
        {
            nameInputField.text = null;
            mailInputField.text = null;
            UpdateFeedBackText("Profile created !", true);
        }
        else
        {
            UpdateFeedBackText("Profile could not be saved !", false);
        }
    }

    // Disables the profile's creation button.
    public void DisableInputs(bool disable)
    {
        gameObject.GetComponentInChildren<Button>().interactable = !disable;
    }

    // Clears the feedback text.
    public void ClearFeedbackText()
    {
        feedbackText.text = "";
    }

    // Updates the feedback text
    private void UpdateFeedBackText(string text, bool good)
    {
        StopAllCoroutines();
        feedbackText.text = text;
        if (good) StartCoroutine(PlayHighlight(feedbackTextGoodColor));
        else StartCoroutine(PlayHighlight(feedbackTextBadColor));
    }

    // Quart function
    private float EaseQuartOut (float k) 
    {
        return 1f - ((k -= 1f)*k*k*k);
    }

    // Plays the feedback text highlight effect. Played when the feedback text is changed.
    private IEnumerator PlayHighlight(Color finalColor)
    {
        float durationLeft = feedbackTextHighlightDuration;

        // Generation of a color gradient from the current color plus mask to the end color.
        Gradient colorGradient = new Gradient();
        GradientColorKey[] colorKey = new GradientColorKey[2]{new GradientColorKey(feedbackTextHighlightMask, 0f), new GradientColorKey(finalColor, 1f)};
        GradientAlphaKey[] alphaKey = new GradientAlphaKey[2]{new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f)};
        colorGradient.SetKeys(colorKey, alphaKey);

        // Playing of the animation. The text color is interpolated following the easing curve
        while (durationLeft > 0f)
        {
            float timeRatio = (feedbackTextHighlightDuration - durationLeft) / feedbackTextHighlightDuration;

            feedbackText.color = colorGradient.Evaluate(EaseQuartOut(timeRatio));
            durationLeft -= Time.deltaTime;

            yield return null;
        }

        // When the animation is finished, resets the color to its final value.
        feedbackText.color = finalColor;
    }
}
