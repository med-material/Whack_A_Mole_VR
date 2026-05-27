# Virtual Reality Prism Effect

Unity-based prototype for testing and comparing different virtual prism-effect implementations in VR. The project was developed as an 8th semester Medialogy project at Aalborg University and builds on the Whack-A-Mole VR rehabilitation platform.

The prototype is intended as a research sandbox rather than a clinical treatment product. It supports comparable baseline, exposure, and post-exposure modules for investigating prism-adaptation-like effects in healthy participants.

## Main Features

- VR tasks for Open-Loop Pointing, Line Bisection, Landmark, and Exposure.
- Prism-effect modes: None, Translation, Rotation, and Skew.
- Input modes: controller-based pointing and embodied hand-tracking pointing.
- Opposite-hand controller confirmation to reduce selection-induced movement error.
- Runtime logging of metadata, events, summaries, and continuous movement samples.
- RShiny analysis dashboard for summary, comparison, participant, quality, spatial, and trajectory views.

## Project Structure

- `Assets/PEGFG/Scripts/` - Unity runtime scripts for task flow, prism effects, input handling, and calibration.
- `Assets/PEGFG/Scripts/Logging/` - custom logging pipeline for experiment data.
- `Assets/PEGFG/Analysis/PrismShiny/` - RShiny dashboard for inspecting participant logs.
- `Assets/PrismLogging/Participants/` - default location for exported participant CSV logs.
- `Documentation/` - report, article, figures, and LaTeX source material.
- `Articles/` - reviewed prism adaptation and VR rehabilitation literature.

## Running the Unity Prototype

1. Open the project in Unity.
2. Load the main prism experiment scene.
3. Ensure XR/OpenXR and Meta/OVR components are configured for the headset.
4. Start Play Mode or build to the target VR device.
5. Use the participant/session settings in the Unity inspector before running a test.

The project was primarily tested with Meta Quest hardware. Controller input requires the OVR/XR rig setup to be present in the scene.

## Running the RShiny Analysis App

In R or RStudio:

```r
install.packages(c("shiny", "DT", "dplyr", "plotly", "ggplot2", "readr", "tidyr"))
shiny::runApp("Assets/PEGFG/Analysis/PrismShiny")
```

If package installation fails on Windows because a package is already loaded, restart the R session and install the missing package again.

Inside the app, point the log folder to:

```text
Assets/PrismLogging/Participants
```

## Data Notes

Each participant module follows a baseline--exposure--post structure:

- Baseline: 10 trials with no effect.
- Exposure: 60 attempts under the active effect condition.
- Post: 10 trials with no effect.

The analysis uses baseline-to-post perceived-centre shift and participant-specific no-effect references to compare perturbation conditions.

## Credits

Developed by Markus Birch Flensborg, Mathias Bisgaard, Rikke Bragh Jensen, and Tze Huo Gucci Ho.

Based on the Whack-A-Mole VR platform: <https://github.com/med-material/Whack_A_Mole_VR>
