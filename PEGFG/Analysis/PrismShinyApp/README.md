# Prism Shiny App

This is the first analysis app for the PEGFG/Prism project.

## What it does

- Loads `Meta`, `Event`, `Sample`, and `Summary` CSV files from `Assets/PrismLogging`
- Lets you choose a session by `SessionID`
- Shows:
  - task/aftereffect summary
  - exposure targets, hits, and misses
  - controller/pointer trajectory

## Packages

In R or RStudio:

```r
install.packages(c("shiny", "tidyverse", "plotly", "DT"))
```

If Windows warns about `Rtools`, that is only required for source builds. If the packages install successfully, you can continue.

## Run

Open `app.R` in RStudio and run:

```r
shiny::runApp()
```

Or from the R console:

```r
setwd("PEGFG/Analysis/PrismShiny")
shiny::runApp()
```

## Notes

- The app lives outside `Assets` on purpose so RStudio does not trigger Unity asset refreshes during play mode
- The app defaults to `Assets/PrismLogging`
- You can change the log folder in the UI and refresh sessions
- This is the first scaffold, not the final analysis workflow
