using UnityEngine;

/*
AndroidWelcomeSetup
Summary:
- Android only: once the scene is loaded, removes the Velkommen calibration screen and shows the
  welcome screen in its place.
- Nothing to set up: Unity calls it on its own and it finds both screens in the scene, so there is
  no object to add and no field to fill.
- Follows the build target: on an Android build, and in the editor while the platform is Android
  (to check it without building). On a PC build, or in the editor on the PC platform, the #if
  compiles it out entirely: the PC flow is untouched.
- Touches no game logic: GameDirector still starts the game on its own on the headset.
Usage:
- Keep the WelcomePanel prefab in the scene, left inactive (the prefab already is). That is all.
- The Console says what happened: "[AndroidWelcomeSetup] ..." on each Play.
*/
public static class AndroidWelcomeSetup
{
#if UNITY_ANDROID
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SwapVelkommenForWelcomeScreen()
    {
        // The Velkommen screen is the Canvas carrying HmdCalibration, under Calibration Panel.
        HmdCalibration velkommen = Object.FindObjectOfType<HmdCalibration>(true);
        WelcomeScreen welcome = Object.FindObjectOfType<WelcomeScreen>(true);

        if (welcome == null)
        {
            // Without the card, keep Velkommen rather than leave the player with no screen at all.
            Debug.LogWarning("[AndroidWelcomeSetup] No WelcomePanel in the scene: Velkommen kept.");
            return;
        }

        if (velkommen != null) velkommen.gameObject.SetActive(false);
        welcome.gameObject.SetActive(true);
        Debug.Log("[AndroidWelcomeSetup] Velkommen " + (velkommen != null ? "off" : "not found")
                  + ", welcome screen on (" + welcome.name + ").");
    }
#endif
}
