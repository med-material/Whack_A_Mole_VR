library(shiny)
library(DT)
library(dplyr)
library(ggplot2)
library(plotly)
library(readr)
library(stringr)
library(purrr)
library(tidyr)

# Prism Shiny app for inspecting completed prism-adaptation sessions.
#
# This file contains both the user interface (what the analyst can click on)
# and the server logic (how CSV files are discovered, loaded, transformed,
# summarized, and finally visualized).
#
# Conceptually the app has four layers:
# 1. Startup / file helpers:
#    find the default logging folder and safely read CSV files.
# 2. Data preparation helpers:
#    convert raw rows into summary values, quality metrics, spatial layouts,
#    and trajectory-ready data frames.
# 3. Plot builders:
#    each tab gets one or more functions that return a ggplot object.
# 4. Shiny reactivity:
#    when the user changes a dropdown, the relevant reactive expressions
#    automatically recompute and the visible plots/tables update.

# Resolve the most likely PrismLogging folder without assuming that getwd()
# necessarily equals the Shiny app directory. Depending on whether the app is
# launched from Overleaf/RStudio buttons, sourced manually, or run from the repo
# root, getwd() can point at different places. We therefore search nearby
# ancestors for both the current canonical location (Assets/PrismLogging) and the
# older root-level fallback (PrismLogging), then pick the richest match.
find_prism_log_dir <- function(start_dir = getwd(), max_depth = 8) {
  start_dir <- normalizePath(start_dir, winslash = "/", mustWork = FALSE)

  ancestors <- character()
  current <- start_dir
  for (i in seq_len(max_depth)) {
    ancestors <- c(ancestors, current)
    parent <- dirname(current)
    if (identical(parent, current)) break
    current <- parent
  }

  candidate_dirs <- unique(c(
    ancestors,
    file.path(ancestors, "Assets", "PrismLogging", "Participants"),
    file.path(ancestors, "Assets", "PrismLogging"),
    file.path(ancestors, "PrismLogging", "Participants"),
    file.path(ancestors, "PrismLogging")
  ))

  existing_dirs <- candidate_dirs[dir.exists(candidate_dirs)]
  if (length(existing_dirs) == 0) {
    return(normalizePath(file.path(start_dir, "Assets", "PrismLogging", "Participants"), winslash = "/", mustWork = FALSE))
  }

  session_counts <- map_int(existing_dirs, function(dir_path) {
    length(list.files(dir_path, pattern = "_Meta\\.csv$", full.names = FALSE))
  })

  preference_bonus <- ifelse(grepl("Assets/PrismLogging/Participants$", existing_dirs), 200000L,
                       ifelse(grepl("Assets/PrismLogging$", existing_dirs), 100000L,
                       ifelse(grepl("PrismLogging/Participants$", existing_dirs), 50000L, 0L)))
  best_index <- which.max(session_counts + preference_bonus)
  normalizePath(existing_dirs[[best_index]], winslash = "/", mustWork = FALSE)
}

# If the user pastes the repo root or Assets folder instead of the actual
# logging directory, this helper resolves that into the real PrismLogging folder.
resolve_log_dir <- function(path) {
  path <- normalizePath(path, winslash = "/", mustWork = FALSE)

  candidates <- unique(c(
    path,
    file.path(path, "Participants"),
    file.path(path, "Assets", "PrismLogging", "Participants"),
    file.path(path, "Assets", "PrismLogging"),
    file.path(path, "PrismLogging", "Participants"),
    file.path(path, "PrismLogging")
  ))

  existing <- candidates[dir.exists(candidates)]
  if (length(existing) == 0) return(path)

  session_counts <- map_int(existing, function(dir_path) {
    length(list.files(dir_path, pattern = "_Meta\\.csv$", full.names = FALSE))
  })

  preference_bonus <- ifelse(grepl("Assets/PrismLogging/Participants$", existing), 200000L,
                       ifelse(grepl("Assets/PrismLogging$", existing), 100000L,
                       ifelse(grepl("PrismLogging/Participants$", existing), 50000L, 0L)))
  best_index <- which.max(session_counts + preference_bonus)
  normalizePath(existing[[best_index]], winslash = "/", mustWork = FALSE)
}

normalize_session_scope <- function(scope) {
  if (is.null(scope) || identical(scope, "")) return("Participant")
  if (startsWith(scope, "Participant")) return("Participant")
  if (startsWith(scope, "Debug")) return("Debug")
  if (startsWith(scope, "All")) return("All")
  "Participant"
}

format_session_timestamp <- function(timestamp_text) {
  if (is.na(timestamp_text) || identical(timestamp_text, "")) return("")
  sub("\\.[0-9]+$", "", timestamp_text)
}

default_log_dir <- find_prism_log_dir()

# ---------- Basic CSV / parsing helpers ----------

# Read one semicolon-separated CSV file and fail safely.
# If a file is malformed or temporarily unavailable, return an empty tibble
# instead of crashing the whole app.
safe_read_csv <- function(path, n_max = Inf, columns = NULL) {
  tryCatch(
    suppressMessages(
      if (is.null(columns)) {
        read_delim(path, delim = ";", show_col_types = FALSE, progress = FALSE, n_max = n_max)
      } else {
        read_delim(
          path,
          delim = ";",
          show_col_types = FALSE,
          progress = FALSE,
          n_max = n_max,
          col_select = any_of(columns)
        )
      }
    ),
    error = function(e) tibble()
  )
}

# Convert the literal string "NULL" into real NA values.
# Many of the Unity-generated CSVs store missing values as the text "NULL",
# which is inconvenient for filtering and plotting in R.
clean_log_df <- function(df) {
  if (nrow(df) == 0) return(df)
  df |>
    mutate(across(where(is.character), ~na_if(.x, "NULL")))
}

# Helper readers for the few metadata values needed while discovering sessions.
# These are intentionally tiny wrappers around safe_read_csv() so session discovery
# can inspect files without loading everything at once.
read_session_id <- function(path) {
  data <- clean_log_df(safe_read_csv(path, n_max = 1, columns = "SessionID"))
  if (!"SessionID" %in% names(data) || nrow(data) == 0) return(NA_character_)
  as.character(data$SessionID[[1]])
}

read_session_timestamp <- function(path) {
  data <- clean_log_df(safe_read_csv(path, n_max = 1, columns = "Timestamp"))
  if (!"Timestamp" %in% names(data) || nrow(data) == 0) return(NA_character_)
  as.character(data$Timestamp[[1]])
}

read_session_state <- function(path) {
  data <- clean_log_df(safe_read_csv(path, n_max = 1, columns = "SessionState"))
  if (!"SessionState" %in% names(data) || nrow(data) == 0) return(NA_character_)
  as.character(data$SessionState[[1]])
}

read_meta_value <- function(path, key) {
  data <- clean_log_df(safe_read_csv(path, n_max = 1, columns = key))
  if (!(key %in% names(data)) || nrow(data) == 0) return(NA_character_)
  as.character(data[[key]][[1]])
}

derive_global_participant_id <- function(experiment_mode, input_mode_label, study_participant_index) {
  experiment_mode <- tolower(trimws(coalesce(experiment_mode, "")))
  input_mode_label <- tolower(trimws(coalesce(input_mode_label, "")))
  participant_index <- suppressWarnings(as.integer(study_participant_index))

  if (experiment_mode != "participant" || is.na(participant_index) || participant_index < 1) {
    return(NA_integer_)
  }

  if (input_mode_label == "controller") {
    return((participant_index * 2L) - 1L)
  }

  if (input_mode_label == "embodied") {
    return(participant_index * 2L)
  }

  NA_integer_
}

as_num <- function(x) suppressWarnings(as.numeric(x))
as_time <- function(x) suppressWarnings(as.POSIXct(x, tz = "UTC"))

coerce_summary_numeric_columns <- function(summary_data) {
  if (nrow(summary_data) == 0) return(summary_data)

  numeric_columns <- c(
    "BaselineValue", "BaselineSd", "PostValue", "PostSd",
    "SignedDelta", "Magnitude", "NormalizedMagnitude", "TrialCount",
    "RawBaselineMean", "RawPostMean", "RawSignedDelta", "RawMagnitude",
    "StudyGlobalParticipantId", "StudyParticipantIndex", "StudySessionIndex",
    "Framecount"
  )

  for (column_name in intersect(numeric_columns, names(summary_data))) {
    summary_data[[column_name]] <- as_num(summary_data[[column_name]])
  }

  summary_data
}

has_cols <- function(data, ...) {
  required <- c(...)
  all(required %in% names(data))
}

safe_num_col <- function(data, key, default = NA_real_) {
  if (key %in% names(data)) {
    return(as_num(data[[key]]))
  }
  rep(default, nrow(data))
}

safe_chr_col <- function(data, key, default = NA_character_) {
  if (key %in% names(data)) {
    return(as.character(data[[key]]))
  }
  rep(default, nrow(data))
}

meta_num <- function(meta_data, key, default = NA_real_) {
  if (!has_cols(meta_data, key) || nrow(meta_data) == 0) return(default)
  value <- as_num(meta_data[[key]][[1]])
  if (is.na(value)) default else value
}

resolve_exposure_radius_m <- function(meta_data, event_data) {
  meta_radius <- meta_num(meta_data, "ExposureHitRadiusMeters", NA_real_)
  if (is.finite(meta_radius)) {
    return(meta_radius)
  }

  event_radius <- event_data |>
    mutate(target_radius_m = safe_num_col(event_data, "TargetRadiusMeters")) |>
    filter(!is.na(target_radius_m), is.finite(target_radius_m), target_radius_m > 0) |>
    summarise(value = dplyr::first(target_radius_m)) |>
    pull(value)

  if (length(event_radius) == 0 || !is.finite(event_radius)) {
    return(NA_real_)
  }

  event_radius
}

stable_cluster_center <- function(x) {
  x <- x[is.finite(x)]
  if (length(x) == 0) return(NA_real_)
  if (length(x) == 1) return(x[[1]])

  raw_center <- median(x, na.rm = TRUE)
  robust_scale <- IQR(x, na.rm = TRUE) / 1.349
  if (!is.finite(robust_scale) || robust_scale < 1e-6) {
    robust_scale <- sd(x, na.rm = TRUE)
  }
  if (!is.finite(robust_scale) || robust_scale < 1e-6) {
    return(mean(x, na.rm = TRUE))
  }

  weights <- 1 / (1 + (abs(x - raw_center) / robust_scale)^2)
  weighted.mean(x, weights, na.rm = TRUE)
}

stable_cluster_spread <- function(x) {
  x <- x[is.finite(x)]
  if (length(x) <= 1) return(NA_real_)

  center <- stable_cluster_center(x)
  robust_scale <- IQR(x, na.rm = TRUE) / 1.349
  if (!is.finite(robust_scale) || robust_scale < 1e-6) {
    robust_scale <- sd(x, na.rm = TRUE)
  }
  if (!is.finite(robust_scale) || robust_scale < 1e-6) {
    return(0)
  }

  weights <- 1 / (1 + (abs(x - median(x, na.rm = TRUE)) / robust_scale)^2)
  sqrt(weighted.mean((x - center)^2, weights, na.rm = TRUE))
}

extract_perceived_center_trials <- function(event_data) {
  if (nrow(event_data) == 0) return(tibble())

  event_data |>
    filter(
      TaskMode %in% c("OpenLoop", "LineBisection"),
      BlockType %in% c("Baseline", "Post"),
      Event == "Trial Accepted"
    ) |>
    mutate(
      TrialIndex = as_num(TrialIndex),
      PerceivedCenterCm = case_when(
        TaskMode == "OpenLoop" ~ as_num(SignedOffsetCm),
        TaskMode == "LineBisection" ~ as_num(ErrorCm),
        TRUE ~ NA_real_
      )
    ) |>
    filter(!is.na(TrialIndex), is.finite(PerceivedCenterCm))
}

build_stable_perceived_center_aftereffects <- function(summary_aftereffect, event_data) {
  trials <- extract_perceived_center_trials(event_data)
  if (nrow(trials) == 0) return(summary_aftereffect)

  block_centers <- trials |>
    group_by(TaskMode, BlockType) |>
    summarise(
      CenterValue = stable_cluster_center(PerceivedCenterCm),
      CenterSpread = stable_cluster_spread(PerceivedCenterCm),
      RawMeanValue = mean(PerceivedCenterCm, na.rm = TRUE),
      RawSdValue = sd(PerceivedCenterCm, na.rm = TRUE),
      TrialCount = n(),
      .groups = "drop"
    )

  robust_wide <- block_centers |>
    select(TaskMode, BlockType, CenterValue, CenterSpread, RawMeanValue, RawSdValue, TrialCount) |>
    pivot_wider(
      names_from = BlockType,
      values_from = c(CenterValue, CenterSpread, RawMeanValue, RawSdValue, TrialCount),
      names_sep = ""
    )

  required_wide_cols <- c("CenterValueBaseline", "CenterValuePost")
  if (!all(required_wide_cols %in% names(robust_wide))) {
    return(summary_aftereffect)
  }

  for (missing_col in setdiff(
    c(
      "CenterSpreadBaseline", "CenterSpreadPost", "RawMeanValueBaseline", "RawMeanValuePost",
      "TrialCountBaseline", "TrialCountPost"
    ),
    names(robust_wide)
  )) {
    robust_wide[[missing_col]] <- NA_real_
  }

  robust_rows <- robust_wide |>
    filter(!is.na(CenterValueBaseline), !is.na(CenterValuePost)) |>
    mutate(
      SummaryType = "Aftereffect",
      BlockType = "BaselineToPost",
      MetricName = "StablePerceivedCenter",
      MetricUnits = "cm",
      BaselineValue = CenterValueBaseline,
      PostValue = CenterValuePost,
      BaselineSd = CenterSpreadBaseline,
      PostSd = CenterSpreadPost,
      SignedDelta = PostValue - BaselineValue,
      Magnitude = abs(SignedDelta),
      NormalizedMagnitude = ifelse(
        is.na(BaselineSd) | BaselineSd < 1e-6,
        NA_real_,
        Magnitude / BaselineSd
      ),
      TrialCount = pmin(TrialCountBaseline, TrialCountPost, na.rm = TRUE),
      Notes = "Stable perceived-center shift; all accepted trials included with far responses down-weighted, not removed.",
      Estimator = "StableClusterCenter",
      RawBaselineMean = RawMeanValueBaseline,
      RawPostMean = RawMeanValuePost,
      RawSignedDelta = RawPostMean - RawBaselineMean,
      RawMagnitude = abs(RawSignedDelta)
    )

  if (nrow(robust_rows) == 0) return(summary_aftereffect)

  metadata_source <- summary_aftereffect |>
    select(any_of(c(
      "SessionID", "Timestamp", "ExperimentMode", "StudyGlobalParticipantId", "StudyInputMode",
      "StudyParticipantIndex", "StudyResolvedEffect", "StudyResolvedTask", "StudySessionIndex",
      "StudyTaskOrder", "ConfiguredEffectMode", "AppliedEffectMode", "ConfirmMitigationMode",
      "ActiveHand", "TrackingMode", "XRBackend"
    ))) |>
    slice(1)

  if (nrow(metadata_source) == 0) {
    return(robust_rows)
  }

  bind_cols(
    metadata_source[rep(1, nrow(robust_rows)), , drop = FALSE],
    robust_rows |> select(-any_of(names(metadata_source)))
  )
}

# Scan one logging directory and reconstruct complete sessions from the separate
# Meta / Event / Sample / Summary CSV files.
#
# The Unity pipeline writes one file per collection, so this function groups those
# files back into one "session index" row that the rest of the app can work with.
discover_sessions <- function(log_dir) {
  log_dir <- resolve_log_dir(log_dir)
  files <- list.files(log_dir, pattern = "\\.(csv)$", full.names = TRUE)
  files <- files[!grepl("\\.meta$", files, ignore.case = TRUE)]
  files <- files[grepl("_(Meta|Event|Sample|Summary)\\.csv$", basename(files))]

  if (length(files) == 0) return(tibble())

  tibble(path = files) |>
    mutate(
      file_name = basename(path),
      file_type = str_match(file_name, "_(Meta|Event|Sample|Summary)\\.csv$")[, 2],
      session_id = map_chr(path, read_session_id),
      timestamp = map_chr(path, ~if (grepl("_Meta\\.csv$", .x)) read_session_timestamp(.x) else NA_character_),
      session_state = map_chr(path, ~if (grepl("_Meta\\.csv$", .x)) read_session_state(.x) else NA_character_),
      input_mode_label = map_chr(path, ~if (grepl("_Meta\\.csv$", .x)) read_meta_value(.x, "InputModeLabel") else NA_character_),
      experiment_mode = map_chr(path, ~if (grepl("_Meta\\.csv$", .x)) read_meta_value(.x, "ExperimentMode") else NA_character_),
      study_participant_index = map_chr(path, ~if (grepl("_Meta\\.csv$", .x)) read_meta_value(.x, "StudyParticipantIndex") else NA_character_),
      study_global_participant_id = map_chr(path, ~if (grepl("_Meta\\.csv$", .x)) read_meta_value(.x, "StudyGlobalParticipantId") else NA_character_),
      study_session_index = map_chr(path, ~if (grepl("_Meta\\.csv$", .x)) read_meta_value(.x, "StudySessionIndex") else NA_character_)
    ) |>
    filter(!is.na(session_id), session_id != "") |>
    group_by(session_id) |>
    summarise(
      timestamp = first(na.omit(timestamp)),
      session_state = first(na.omit(session_state)),
      input_mode_label = first(na.omit(input_mode_label)),
      experiment_mode = first(na.omit(experiment_mode)),
      study_participant_index = first(na.omit(study_participant_index)),
      study_global_participant_id = first(na.omit(study_global_participant_id)),
      study_session_index = first(na.omit(study_session_index)),
      Meta = first(path[file_type == "Meta"]),
      Event = first(path[file_type == "Event"]),
      Sample = first(path[file_type == "Sample"]),
      Summary = first(path[file_type == "Summary"]),
      .groups = "drop"
    ) |>
    mutate(
      experiment_mode = ifelse(is.na(experiment_mode), "", trimws(experiment_mode)),
      input_mode_label = ifelse(is.na(input_mode_label), "", trimws(input_mode_label)),
      session_state = ifelse(is.na(session_state), "", trimws(session_state)),
      study_participant_index = suppressWarnings(as.integer(study_participant_index)),
      study_global_participant_id = suppressWarnings(as.integer(study_global_participant_id)),
      study_session_index = suppressWarnings(as.integer(study_session_index)),
      study_global_participant_id = dplyr::coalesce(
        study_global_participant_id,
        vapply(
          seq_len(n()),
          function(i) derive_global_participant_id(experiment_mode[[i]], input_mode_label[[i]], study_participant_index[[i]]),
          integer(1)
        )
      )
    ) |>
    arrange(desc(timestamp))
}

# Given one row from the session index, read all four collections for that session.
# The return value is a named list so downstream code can use current_data()$event,
# current_data()$sample, and so on.
load_session_data <- function(session_row) {
  list(
    meta = clean_log_df(safe_read_csv(session_row$Meta)),
    event = clean_log_df(safe_read_csv(session_row$Event)),
    sample = clean_log_df(safe_read_csv(session_row$Sample)),
    summary = coerce_summary_numeric_columns(clean_log_df(safe_read_csv(session_row$Summary)))
  )
}

# ---------- Summary-tab helpers ----------

# Build the simple bar chart shown on the Summary tab.
# This plot only uses rows of type "Aftereffect", because those rows already condense
# baseline/post comparisons into the final analysis values.
build_summary_plot <- function(summary_data) {
  aftereffect <- summary_data |>
    filter(SummaryType == "Aftereffect") |>
    mutate(Magnitude = as_num(Magnitude)) |>
    filter(!is.na(Magnitude))

  if (nrow(aftereffect) == 0) {
    return(
      ggplot() +
        annotate("text", x = 1, y = 1, label = "No aftereffect rows in Summary.csv") +
        theme_void()
    )
  }

  ggplot(aftereffect, aes(x = TaskMode, y = Magnitude, fill = MetricName)) +
    geom_col(width = 0.6) +
    geom_text(aes(label = sprintf("%.2f", Magnitude)), vjust = -0.4, size = 4) +
    labs(
      title = "Stable Perceived-center Shift",
      x = "Task",
      y = "Magnitude"
    ) +
    theme_minimal(base_size = 13) +
    theme(
      legend.position = "none",
      plot.title = element_text(face = "bold"),
      axis.title.x = element_text(margin = margin(t = 10)),
      plot.margin = margin(12, 24, 12, 12)
    )
}

# Translate one aftereffect summary row into plain-language interpretation strings
# for the metric cards on the Summary tab.
#
# The app deliberately writes these short interpretations so the dashboard is easier
# to read for non-programmers and non-statisticians.
compute_interpretation <- function(aftereffect_row) {
  if (nrow(aftereffect_row) == 0) {
    return(list(
      direction_label = "No post data",
      correction_label = "No error-correction estimate",
      strength_label = "No aftereffect row",
      correction_value = NA_real_,
      correction_display = "N/A",
      signed_shift_label = "No signed shift estimate"
    ))
  }

  baseline <- as_num(aftereffect_row$BaselineValue[[1]])
  post <- as_num(aftereffect_row$PostValue[[1]])
  normalized <- as_num(aftereffect_row$NormalizedMagnitude[[1]])
  signed_delta <- as_num(aftereffect_row$SignedDelta[[1]])
  metric_name <- aftereffect_row$MetricName[[1]]
  task_mode <- aftereffect_row$TaskMode[[1]]

  if (is.na(normalized)) {
    strength_label <- "Magnitude available, normalization unavailable"
  } else if (normalized < 0.2) {
    strength_label <- "Very small change from baseline"
  } else if (normalized < 0.5) {
    strength_label <- "Small change from baseline"
  } else if (normalized < 0.8) {
    strength_label <- "Moderate change from baseline"
  } else {
    strength_label <- "Large change from baseline"
  }

  if (metric_name == "Accuracy") {
    correction <- post - baseline
    correction_display <- ifelse(
      is.na(correction),
      "N/A",
      sprintf("%.2f", abs(correction))
    )
    direction_label <- ifelse(
      is.na(correction),
      "Accuracy change unavailable",
      ifelse(correction > 0, "Post accuracy improved", ifelse(correction < 0, "Post accuracy decreased", "No measurable accuracy change"))
    )
    correction_label <- ifelse(
      is.na(correction),
      "No correction estimate",
      sprintf("Accuracy changed by %.2f", correction)
    )
    signed_shift_label <- ifelse(
      is.na(signed_delta),
      "No signed shift estimate",
      ifelse(signed_delta > 0, sprintf("Post score increased by %.2f", signed_delta), ifelse(signed_delta < 0, sprintf("Post score decreased by %.2f", abs(signed_delta)), "No signed score change")))
  } else {
    baseline_abs <- abs(baseline)
    post_abs <- abs(post)
    correction <- baseline_abs - post_abs
    correction_display <- ifelse(
      is.na(correction),
      "N/A",
      sprintf("%.2f", abs(correction))
    )
    direction_label <- ifelse(
      is.na(correction),
      "Direction unavailable",
      ifelse(correction > 0, "Post responses moved closer to target", ifelse(correction < 0, "Post responses moved farther from target", "No measurable change toward target"))
    )
    correction_label <- ifelse(
      is.na(correction),
      "No error-correction estimate",
      sprintf("Absolute error changed by %.2f", correction)
    )
    signed_shift_label <- ifelse(
      is.na(signed_delta),
      "No signed shift estimate",
      ifelse(
        metric_name %in% c("SignedEndpointError", "BisectionError"),
        ifelse(
          signed_delta > 0,
          sprintf("Post pointing shifted %.2f cm to the right", abs(signed_delta)),
          ifelse(signed_delta < 0, sprintf("Post pointing shifted %.2f cm to the left", abs(signed_delta)), "No signed pointing shift")
        ),
        ifelse(
          signed_delta > 0,
          sprintf("Post pointing shifted %.2f in the positive direction", signed_delta),
          ifelse(signed_delta < 0, sprintf("Post pointing shifted %.2f in the negative direction", abs(signed_delta)), "No signed pointing shift")
        )
      )
    )
  }

  list(
    direction_label = direction_label,
    correction_label = correction_label,
    strength_label = strength_label,
    correction_value = correction,
    correction_display = correction_display,
    signed_shift_label = signed_shift_label
  )
}

# Compare baseline and post variability, typically via standard deviation.
# This powers the "Consistency" card on the Summary tab.
compute_variability <- function(aftereffect_row) {
  if (nrow(aftereffect_row) == 0) {
    return(list(
      baseline_sd = NA_real_,
      post_sd = NA_real_,
      delta_sd = NA_real_,
      variability_label = "No variability estimate"
    ))
  }

  baseline_sd <- as_num(aftereffect_row$BaselineSd[[1]])
  post_sd <- as_num(aftereffect_row$PostSd[[1]])
  delta_sd <- post_sd - baseline_sd

  variability_label <- ifelse(
    is.na(delta_sd),
    "Variability unavailable",
    ifelse(
      abs(delta_sd) < 0.1,
      "Consistency stayed about the same",
      ifelse(delta_sd > 0, "Participant became less consistent", "Participant became more consistent")
    )
  )

  list(
    baseline_sd = baseline_sd,
    post_sd = post_sd,
    delta_sd = delta_sd,
    variability_label = variability_label
  )
}

# Display configured and applied effect modes together.
# This matters because some sessions may request one effect but effectively apply another,
# and the card should not hide that difference.
format_effect_label <- function(aftereffect_row) {
  if (nrow(aftereffect_row) == 0) return("Unknown")

  configured <- aftereffect_row$ConfiguredEffectMode[[1]]
  applied <- aftereffect_row$AppliedEffectMode[[1]]

  configured <- ifelse(is.na(configured) || configured == "", "Unknown", configured)
  applied <- ifelse(is.na(applied) || applied == "", "Unknown", applied)

  if (configured == applied) {
    return(configured)
  }

  paste0(configured, " (applied: ", applied, ")")
}

# ---------- Spatial / trajectory data preparation ----------

# Build the Spatial-tab plot.
#
# This function branches by task because "spatial overview" means different things:
# - OpenLoop: accepted endpoints relative to a single target
# - LineBisection: accepted endpoints relative to a displayed line and its midpoint
# - Exposure: target centers plus hit and miss locations in world space
build_spatial_plot <- function(event_data, selected_task = "Exposure", selected_block = "Exposure") {
  if (nrow(event_data) == 0) {
    return(ggplot() + annotate("text", x = 1, y = 1, label = "No event data loaded") + theme_void())
  }

  event_subset <- event_data |>
    filter(TaskMode == selected_task, BlockType == selected_block)

  if (selected_task == "OpenLoop") {
    trials <- event_subset |>
      filter(Event == "Trial Accepted") |>
      transmute(
        x = as_num(SignedOffsetCm),
        y = 0
      ) |>
      filter(!is.na(x))

    if (nrow(trials) == 0) {
      return(ggplot() + annotate("text", x = 1, y = 1, label = "No OpenLoop response data found for this filter") + theme_void())
    }

    return(
      ggplot() +
        geom_hline(yintercept = 0, color = "#cbd5e1") +
        geom_point(data = trials, aes(x = x, y = y), size = 3, alpha = 0.8, color = "#0ea5a4") +
        geom_point(aes(x = 0, y = 0), shape = 4, size = 5, stroke = 1.6, color = "#1f2937") +
        labs(
          title = paste(selected_task, selected_block, "Spatial Overview"),
          subtitle = "Each point is one accepted response. The cross at 0 cm is the intended target center.",
          x = "Signed deviation from target center (cm)",
          y = ""
        ) +
        theme_minimal(base_size = 13) +
        theme(axis.text.y = element_blank(), axis.title.y = element_blank(), axis.ticks.y = element_blank())
    )
  }

  if (selected_task == "LineBisection") {
    line_half_cm <- event_subset |>
      filter(Event == "Trial Accepted") |>
      mutate(line_half_cm = as_num(LineLengthMeters) * 50) |>
      summarise(value = median(line_half_cm, na.rm = TRUE)) |>
      pull(value)

    if (!is.finite(line_half_cm)) {
      line_half_cm <- 10
    }

    trials <- event_subset |>
      filter(Event == "Trial Accepted") |>
      transmute(
        x = as_num(ErrorCm),
        y = 0
      ) |>
      filter(!is.na(x))

    if (nrow(trials) == 0) {
      return(ggplot() + annotate("text", x = 1, y = 1, label = "No LineBisection response data found for this filter") + theme_void())
    }

    line_df <- tibble(x = c(-line_half_cm, line_half_cm), y = c(0, 0))

    return(
      ggplot() +
        geom_line(data = line_df, aes(x = x, y = y), color = "#64748b", linewidth = 1.2) +
        geom_point(data = trials, aes(x = x, y = y), size = 3, alpha = 0.8, color = "#0ea5a4") +
        geom_point(aes(x = 0, y = 0), shape = 4, size = 5, stroke = 1.6, color = "#1f2937") +
        labs(
          title = paste(selected_task, selected_block, "Spatial Overview"),
          subtitle = "The horizontal line is the displayed bisection line. The cross marks the true midpoint.",
          x = "Signed deviation from true midpoint (cm)",
          y = ""
        ) +
        theme_minimal(base_size = 13) +
        theme(axis.text.y = element_blank(), axis.title.y = element_blank(), axis.ticks.y = element_blank())
    )
  }

  targets_raw <- event_subset |>
    filter(Event == "Mole Spawned") |>
    transmute(
      kind = "Target",
      x = as_num(MolePositionWorldX),
      y = as_num(MolePositionWorldY)
    ) |>
    filter(!is.na(x), !is.na(y)) |>
    mutate(
      x_group = round(x, 2),
      y_group = round(y, 2)
    ) |>
    group_by(x_group, y_group) |>
    summarise(
      kind = "Target",
      x = median(x, na.rm = TRUE),
      y = median(y, na.rm = TRUE),
      .groups = "drop"
    ) |>
    arrange(x)

  targets <- if (nrow(targets_raw) >= 1) {
    # Spawn positions can differ by tiny floating-point amounts across repeated
    # trials. Rather than rely on kmeans, which can become brittle on tiny or odd
    # subsets, keep a deterministic set of logical target anchors ordered by x.
    #
    # If 3 or more grouped positions exist, use:
    # - leftmost as Left
    # - median-x position as Center
    # - rightmost as Right
    #
    # If only 2 positions are present, keep Left and Right.
    # If only 1 is present, treat it as Center.
    ordered_targets <- targets_raw |>
      arrange(x)

    if (nrow(ordered_targets) >= 3) {
      center_index <- ceiling(nrow(ordered_targets) / 2)

      ordered_targets |>
        slice(c(1, center_index, n())) |>
        mutate(label = c("Left", "Center", "Right")) |>
        select(kind, x, y, label)
    } else if (nrow(ordered_targets) == 2) {
      ordered_targets |>
        mutate(label = c("Left", "Right")) |>
        select(kind, x, y, label)
    } else {
      ordered_targets |>
        mutate(label = "Center") |>
        select(kind, x, y, label)
    }
  } else {
    targets_raw |>
      mutate(
        label = case_when(
          x < -0.1 ~ "Left",
          x > 0.1 ~ "Right",
          TRUE ~ "Center"
        )
      ) |>
      select(kind, x, y, label)
  }

  hits <- event_subset |>
    filter(Event == "Mole Hit") |>
    transmute(
      kind = "Hit",
      x = as_num(HitPositionWorldX),
      y = as_num(HitPositionWorldY)
    ) |>
    filter(!is.na(x), !is.na(y))

  misses <- event_subset |>
    filter(Event == "Mole Missed") |>
    transmute(
      kind = "Miss",
      x = as_num(HitPositionWorldX),
      y = as_num(HitPositionWorldY)
    ) |>
    filter(!is.na(x), !is.na(y))

  if (nrow(bind_rows(targets, hits, misses)) == 0) {
    return(ggplot() + annotate("text", x = 1, y = 1, label = "No spatial hit/miss data found for this filter") + theme_void())
  }

  plot_title <- if (selected_task == selected_block) {
    paste(selected_task, "Spatial Overview")
  } else {
    paste(selected_task, selected_block, "Spatial Overview")
  }

  target_radius_m <- resolve_exposure_radius_m(tibble(), event_subset)
  if (!is.finite(target_radius_m)) {
    target_radius_m <- 0.10
  }
  target_circles <- if (nrow(targets) > 0) {
    circle_steps <- seq(0, 2 * pi, length.out = 160)

    targets |>
      select(label, x, y) |>
      tidyr::crossing(theta = circle_steps) |>
      mutate(
        circle_x = x + target_radius_m * cos(theta),
        circle_y = y + target_radius_m * sin(theta)
      )
  } else {
    tibble(label = character(), circle_x = numeric(), circle_y = numeric())
  }

  ggplot() +
    geom_path(
      data = target_circles,
      aes(x = circle_x, y = circle_y, group = label),
      color = "#94a3b8",
      linewidth = 0.9,
      alpha = 0.8
    ) +
    geom_point(data = targets, aes(x = x, y = y), shape = 4, size = 5, stroke = 1.6, color = "#1f2937") +
    geom_text(data = targets, aes(x = x, y = y, label = label), nudge_y = 0.03, size = 4.2, color = "#1f2937") +
    geom_point(data = hits, aes(x = x, y = y), size = 3, alpha = 0.8, color = "#0ea5a4") +
    geom_point(data = misses, aes(x = x, y = y), size = 3, alpha = 0.8, color = "#dc2626") +
    coord_equal() +
    labs(
      title = plot_title,
      subtitle = "Targets, hits, and misses in board/world space",
      x = "World X",
      y = "World Y"
    ) +
    theme_minimal(base_size = 13)
}

# Non-exposure tasks do not show a continuous movement path in this app.
# Instead, they show a trial-by-trial response overview, optionally overlaying
# Baseline and Post so directional bias can be compared directly.
build_non_exposure_trial_plot <- function(event_data, selected_task, selected_block, selected_trial, overlap_blocks = FALSE) {
  signed_col <- if (selected_task == "LineBisection") "ErrorCm" else "SignedOffsetCm"
  blocks_to_show <- if (overlap_blocks) c("Baseline", "Post") else selected_block

  trial_rows <- event_data |>
    filter(TaskMode == selected_task, BlockType %in% blocks_to_show, Event == "Trial Accepted") |>
    mutate(
      TrialIndex = as_num(TrialIndex),
      signed_value = as_num(.data[[signed_col]])
    ) |>
    filter(!is.na(TrialIndex), !is.na(signed_value), !is.na(BlockType))

  if (nrow(trial_rows) == 0) {
    return(
      ggplot() +
        annotate("text", x = 1, y = 1, label = "No accepted trials found for this task/block") +
        theme_void()
    )
  }

  highlighted <- trial_rows |>
    filter(BlockType == selected_block, TrialIndex == selected_trial) |>
    slice(1)

  metric_label <- ifelse(
    selected_task == "LineBisection",
    "Signed deviation from true midpoint (cm)",
    "Signed deviation from target center (cm)"
  )

  subtitle_text <- if (overlap_blocks) {
    "Each dot is one accepted trial. Baseline and Post are overlaid to compare response bias directly. The blue point is the selected trial."
  } else {
    "Each dot is one accepted trial in this block. Dashed line is the ideal center. The blue point is the selected trial."
  }

  ggplot(trial_rows, aes(x = TrialIndex, y = signed_value, color = BlockType, group = BlockType)) +
    geom_hline(yintercept = 0, color = "#111827", linetype = "dashed") +
    geom_line(linewidth = 0.8, alpha = 0.7) +
    geom_point(size = 2.7, alpha = 0.8) +
    geom_point(data = highlighted, aes(x = TrialIndex, y = signed_value), inherit.aes = FALSE, color = "#2563eb", size = 4) +
    scale_color_manual(values = c("Baseline" = "#94a3b8", "Post" = "#0ea5a4", "Exposure" = "#dc2626")) +
    labs(
      title = if (overlap_blocks) paste(selected_task, "Baseline vs Post Trial Overview") else paste(selected_task, selected_block, "Selected Trial Overview"),
      subtitle = subtitle_text,
      x = "Trial",
      y = metric_label,
      color = NULL
    ) +
    theme_minimal(base_size = 13) +
    theme(legend.position = if (overlap_blocks) "top" else "none")
}

# Extract raw controller/hand trajectory samples near one accepted attempt.
# This is a low-level helper kept for experimentation and targeted attempt views.
extract_attempt_trajectory <- function(sample_data, event_data, selected_task, attempt_index, time_window) {
  sample_subset <- sample_data |>
    filter(TaskMode == selected_task) |>
    mutate(
      ts = as_time(Timestamp),
      x = coalesce(as_num(RightControllerLaserPosWorldX), as_num(LeftControllerLaserPosWorldX)),
      y = coalesce(as_num(RightControllerLaserPosWorldY), as_num(LeftControllerLaserPosWorldY)),
      z = coalesce(as_num(RightControllerLaserPosWorldZ), as_num(LeftControllerLaserPosWorldZ))
    ) |>
    filter(!is.na(ts), !is.na(x), !is.na(y), !is.na(z))

  pointer_events <- event_data |>
    filter(TaskMode == selected_task, Event %in% c("Pointer Shoot", "Mole Hit", "Mole Missed")) |>
    mutate(
      ts = as_time(Timestamp),
      AttemptIndex = as_num(AttemptIndex)
    ) |>
    filter(!is.na(ts), !is.na(AttemptIndex))

  if (nrow(pointer_events) == 0 || nrow(sample_subset) == 0) {
    return(list(path = tibble(), event = tibble()))
  }

  chosen_event <- pointer_events |>
    filter(AttemptIndex == attempt_index) |>
    arrange(ts) |>
    slice(1)

  if (nrow(chosen_event) == 0) {
    return(list(path = tibble(), event = tibble()))
  }

  t0 <- chosen_event$ts[[1]]
  path <- sample_subset |>
    filter(ts >= (t0 - time_window), ts <= (t0 + time_window)) |>
    mutate(sample_index = row_number())

  list(path = path, event = chosen_event)
}

# Convert the live pointer ray into projected coordinates on the target plane,
# attempt by attempt. This is the key step behind the Exposure trajectory view:
# it lets the app show where the participant's projected aim moved relative to
# the target center before the final confirmation.
extract_projected_attempts <- function(sample_data, event_data, selected_task, time_window) {
  sample_subset <- sample_data |>
    filter(TaskMode == selected_task) |>
    mutate(
      ts = as_time(Timestamp),
      px = as_num(PointerOriginX),
      py = as_num(PointerOriginY),
      pz = as_num(PointerOriginZ),
      fx = as_num(PointerForwardX),
      fy = as_num(PointerForwardY),
      fz = as_num(PointerForwardZ)
    ) |>
    filter(!is.na(ts), !is.na(px), !is.na(py), !is.na(pz), !is.na(fx), !is.na(fy), !is.na(fz))

  attempt_events <- event_data |>
    filter(TaskMode == selected_task, Event %in% c("Mole Hit", "Mole Missed")) |>
    mutate(
      ts = as_time(Timestamp),
      AttemptIndex = as_num(AttemptIndex),
      target_x = as_num(MolePositionWorldX),
      target_y = as_num(MolePositionWorldY),
      target_z = as_num(MolePositionWorldZ),
      hit_x = as_num(HitPositionWorldX),
      hit_y = as_num(HitPositionWorldY),
      outcome = ifelse(Event == "Mole Hit", "Hit", "Miss")
    ) |>
    filter(!is.na(ts), !is.na(AttemptIndex), !is.na(target_x), !is.na(target_y), !is.na(target_z))

  if (nrow(sample_subset) == 0 || nrow(attempt_events) == 0) {
    return(tibble())
  }

  spawn_events <- event_data |>
    filter(TaskMode == selected_task, Event == "Mole Spawned") |>
    mutate(
      ts = as_time(Timestamp),
      MoleId = as.character(MoleId)
    ) |>
    filter(!is.na(ts))

  purrr::pmap_dfr(
    attempt_events |> select(AttemptIndex, ts, target_x, target_y, target_z, hit_x, hit_y, outcome, MoleId),
    function(AttemptIndex, ts, target_x, target_y, target_z, hit_x, hit_y, outcome, MoleId) {
      event_ts <- ts
      spawn_match <- spawn_events |>
        filter(ts <= event_ts) |>
        filter(is.na(MoleId) | MoleId == "" | MoleId == "NULL" | MoleId == as.character(MoleId)) |>
        arrange(desc(ts)) |>
        slice(1)

      spawn_ts <- if (nrow(spawn_match) == 0) event_ts - time_window else spawn_match$ts[[1]]
      window_start <- max(event_ts - time_window, spawn_ts)

      attempt_samples <- sample_subset |>
        filter(ts >= window_start, ts <= event_ts) |>
        mutate(time_to_confirm = as.numeric(difftime(ts, event_ts, units = "secs")))

      if (nrow(attempt_samples) == 0) return(tibble())

      attempt_samples |>
        mutate(
          ray_t = ifelse(abs(fz) < 1e-4, NA_real_, (target_z - pz) / fz),
          board_x = px + fx * ray_t,
          board_y = py + fy * ray_t,
          relative_x = board_x - target_x,
          relative_y = board_y - target_y,
          AttemptIndex = AttemptIndex,
          outcome = outcome,
          target_x = target_x,
          target_y = target_y,
          hit_relative_x = hit_x - target_x,
          hit_relative_y = hit_y - target_y,
          window_start = window_start
        ) |>
        filter(
          !is.na(ray_t),
          is.finite(ray_t),
          ray_t > 0,
          ray_t < 5,
          !is.na(relative_x),
          !is.na(relative_y),
          is.finite(relative_x),
          is.finite(relative_y),
          abs(relative_x) < 1.5,
          abs(relative_y) < 1.5
        )
    }
  )
}

# Build the main plot on the Trajectories tab.
#
# Exposure gets an actual projected aim path.
# The measurement tasks instead delegate to build_non_exposure_trial_plot(),
# because their most meaningful "trajectory" is the sequence of accepted trials.
build_trajectory_overview_plot <- function(sample_data, event_data, selected_task = "Exposure", selected_block = "Exposure", selected_attempt = NA_real_, time_window = 1.0, overlap_blocks = FALSE) {
  if (selected_task != "Exposure") {
    return(build_non_exposure_trial_plot(event_data, selected_task, selected_block, selected_attempt, overlap_blocks))
  }

  projected <- extract_projected_attempts(sample_data, event_data, selected_task, time_window)

  if (nrow(projected) == 0) {
    return(
      ggplot() +
        annotate("text", x = 1, y = 1, label = "No approach samples found for this task") +
        theme_void()
    )
  }

  selected_path <- projected |>
    filter(AttemptIndex == selected_attempt) |>
    arrange(time_to_confirm)

  if (nrow(selected_path) == 0) {
    return(
      ggplot() +
        annotate("text", x = 1, y = 1, label = "No samples found for the selected attempt") +
        theme_void()
    )
  }

  selected_outcome <- selected_path$outcome[[1]]
  actual_window <- abs(min(selected_path$time_to_confirm, na.rm = TRUE))
  hit_point <- selected_path |>
    slice_tail(n = 1) |>
    transmute(
      x = hit_relative_x,
      y = hit_relative_y,
      outcome = outcome
    )

  projected_endpoint <- selected_path |>
    slice_tail(n = 1) |>
    transmute(
      x = relative_x,
      y = relative_y
    )

  start_point <- selected_path |>
    slice_head(n = 1) |>
    transmute(
      x = relative_x,
      y = relative_y
    )

  plot <- ggplot() +
    geom_path(
      data = selected_path,
      aes(x = relative_x, y = relative_y),
      color = "#2563eb",
      linewidth = 1.4
    ) +
    geom_point(
      data = start_point,
      aes(x = x, y = y),
      color = "#16a34a",
      size = 3
    ) +
    geom_point(
      data = projected_endpoint,
      aes(x = x, y = y),
      color = "#1d4ed8",
      size = 3.5
    ) +
    geom_point(
      data = hit_point,
      aes(x = x, y = y, color = outcome),
      shape = 1,
      size = 4.5,
      stroke = 1.3
    ) +
    geom_point(aes(x = 0, y = 0), color = "#111827", shape = 4, size = 5, stroke = 1.6, inherit.aes = FALSE) +
    scale_color_manual(values = c("Hit" = "#0ea5a4", "Miss" = "#dc2626")) +
    coord_equal() +
    labs(
      title = paste(selected_task, "Selected Attempt Approach"),
      subtitle = sprintf("Attempt %s was a %s. Blue line = projected aim path only during this target's own lifetime, ending at confirm. Visible window: %.2fs. Green = start, blue dot = final projected pointer, hollow circle = registered hit point, cross = target center.", selected_attempt, selected_outcome, actual_window),
      x = "Relative X from target center",
      y = "Relative Y from target center",
      color = NULL
    ) +
    theme_minimal(base_size = 13) +
    theme(legend.position = "top")

  plot
}

extract_phase_profile <- function(meta_data, sample_data, event_data, selected_task = "Exposure", selected_block = "Exposure", selected_attempt = NA_real_, time_window = 1.0) {
  sample_task_subset <- sample_data |>
    filter(TaskMode == selected_task)

  sample_subset <- sample_data |>
    filter(TaskMode == selected_task) |>
    mutate(
      ts = as_time(Timestamp),
      active_hand = as.character(ActiveHand),
      px = as_num(PointerOriginX),
      py = as_num(PointerOriginY),
      pz = as_num(PointerOriginZ),
      fx = as_num(PointerForwardX),
      fy = as_num(PointerForwardY),
      fz = as_num(PointerForwardZ),
      right_move_x = coalesce(safe_num_col(sample_task_subset, "RightMovementAnchorPosWorldX"), safe_num_col(sample_task_subset, "RightControllerPosWorldX")),
      right_move_y = coalesce(safe_num_col(sample_task_subset, "RightMovementAnchorPosWorldY"), safe_num_col(sample_task_subset, "RightControllerPosWorldY")),
      right_move_z = coalesce(safe_num_col(sample_task_subset, "RightMovementAnchorPosWorldZ"), safe_num_col(sample_task_subset, "RightControllerPosWorldZ")),
      left_move_x = coalesce(safe_num_col(sample_task_subset, "LeftMovementAnchorPosWorldX"), safe_num_col(sample_task_subset, "LeftControllerPosWorldX")),
      left_move_y = coalesce(safe_num_col(sample_task_subset, "LeftMovementAnchorPosWorldY"), safe_num_col(sample_task_subset, "LeftControllerPosWorldY")),
      left_move_z = coalesce(safe_num_col(sample_task_subset, "LeftMovementAnchorPosWorldZ"), safe_num_col(sample_task_subset, "LeftControllerPosWorldZ")),
      right_move_source = safe_chr_col(sample_task_subset, "RightMovementAnchorSource"),
      left_move_source = safe_chr_col(sample_task_subset, "LeftMovementAnchorSource"),
      move_x = case_when(
        active_hand == "Left" ~ left_move_x,
        active_hand == "Right" ~ right_move_x,
        TRUE ~ coalesce(right_move_x, left_move_x)
      ),
      move_y = case_when(
        active_hand == "Left" ~ left_move_y,
        active_hand == "Right" ~ right_move_y,
        TRUE ~ coalesce(right_move_y, left_move_y)
      ),
      move_z = case_when(
        active_hand == "Left" ~ left_move_z,
        active_hand == "Right" ~ right_move_z,
        TRUE ~ coalesce(right_move_z, left_move_z)
      ),
      move_source = case_when(
        active_hand == "Left" ~ coalesce(left_move_source, "ControllerPosFallback"),
        active_hand == "Right" ~ coalesce(right_move_source, "ControllerPosFallback"),
        TRUE ~ coalesce(right_move_source, left_move_source, "ControllerPosFallback")
      )
    ) |>
    filter(!is.na(ts), !is.na(px), !is.na(py), !is.na(pz), !is.na(fx), !is.na(fy), !is.na(fz))

  if (nrow(sample_subset) == 0) {
    return(list(error = "No sample rows available for phase analysis"))
  }

  if (selected_task == "Exposure") {
    attempt_event <- event_data |>
      filter(TaskMode == selected_task, BlockType == selected_block, Event %in% c("Mole Hit", "Mole Missed")) |>
      mutate(
        AttemptIndex = as_num(AttemptIndex),
        ts = as_time(Timestamp),
        target_x = as_num(MolePositionWorldX),
        target_y = as_num(MolePositionWorldY),
        target_z = as_num(MolePositionWorldZ),
        outcome = ifelse(Event == "Mole Hit", "Hit", "Miss"),
        MoleIdValue = as.character(MoleId)
      ) |>
      filter(!is.na(AttemptIndex), !is.na(ts), !is.na(target_x), !is.na(target_y), !is.na(target_z)) |>
      filter(AttemptIndex == selected_attempt) |>
      arrange(ts) |>
      slice(1)

    if (nrow(attempt_event) == 0) {
      return(list(error = "No exposure attempt was found for the selected index"))
    }

    spawn_events <- event_data |>
      filter(TaskMode == selected_task, BlockType == selected_block, Event == "Mole Spawned") |>
      mutate(
        ts = as_time(Timestamp),
        MoleIdValue = as.character(MoleId)
      ) |>
      filter(!is.na(ts))

    attempt_mole_id <- attempt_event$MoleIdValue[[1]]
    confirm_ts <- attempt_event$ts[[1]]
    spawn_match <- spawn_events |>
      filter(ts <= confirm_ts)
    if (!is.na(attempt_mole_id) && attempt_mole_id != "" && attempt_mole_id != "NULL") {
      spawn_match <- spawn_match |>
        filter(MoleIdValue == attempt_mole_id)
    }
    spawn_match <- spawn_match |>
      arrange(desc(ts)) |>
      slice(1)

    spawn_ts <- if (nrow(spawn_match) == 0) confirm_ts - time_window else spawn_match$ts[[1]]
    window_start <- max(confirm_ts - time_window, spawn_ts)
    target_radius <- resolve_exposure_radius_m(meta_data, event_data)

    path <- sample_subset |>
      filter(ts >= window_start, ts <= confirm_ts) |>
      mutate(
        ray_t = ifelse(abs(fz) < 1e-4, NA_real_, (attempt_event$target_z[[1]] - pz) / fz),
        board_x = px + fx * ray_t,
        board_y = py + fy * ray_t,
        relative_x = board_x - attempt_event$target_x[[1]],
        relative_y = board_y - attempt_event$target_y[[1]]
      ) |>
      filter(
        !is.na(ray_t),
        is.finite(ray_t),
        ray_t > 0,
        ray_t < 5,
        !is.na(relative_x),
        !is.na(relative_y),
        is.finite(relative_x),
        is.finite(relative_y),
        abs(relative_x) < 1.5,
        abs(relative_y) < 1.5
      ) |>
      arrange(ts)

    title_text <- paste(selected_task, "Attempt", selected_attempt, "Phase Breakdown")
    subtitle_text <- sprintf("Window limited to the last %.2fs before confirm, capped at target spawn. Outcome: %s.", abs(as.numeric(difftime(confirm_ts, window_start, units = "secs"))), attempt_event$outcome[[1]])
  } else {
    trial_start_events <- event_data |>
      filter(TaskMode == selected_task, BlockType == selected_block, Event == "Trial Started")

    start_event <- trial_start_events |>
      mutate(
        TrialIndex = as_num(TrialIndex),
        ts = as_time(Timestamp),
        target_x = as_num(TargetWorldX),
        target_y = as_num(TargetWorldY),
        target_z = as_num(TargetWorldZ),
        target_radius = safe_num_col(trial_start_events, "TargetRadiusMeters")
      ) |>
      filter(!is.na(TrialIndex), !is.na(ts), !is.na(target_x), !is.na(target_y), !is.na(target_z)) |>
      filter(TrialIndex == selected_attempt) |>
      arrange(ts) |>
      slice(1)

    accepted_event <- event_data |>
      filter(TaskMode == selected_task, BlockType == selected_block, Event == "Trial Accepted") |>
      mutate(
        TrialIndex = as_num(TrialIndex),
        ts = as_time(Timestamp)
      ) |>
      filter(!is.na(TrialIndex), !is.na(ts)) |>
      filter(TrialIndex == selected_attempt) |>
      arrange(ts) |>
      slice(1)

    if (nrow(start_event) == 0 || nrow(accepted_event) == 0) {
      return(list(error = "Phase timing for this task needs newer sessions with Trial Started logging"))
    }

    start_ts <- start_event$ts[[1]]
    confirm_ts <- accepted_event$ts[[1]]
    target_radius <- ifelse(is.na(start_event$target_radius[[1]]), 0.03, start_event$target_radius[[1]])

    path <- sample_subset |>
      filter(ts >= start_ts, ts <= confirm_ts) |>
      mutate(
        ray_t = ifelse(abs(fz) < 1e-4, NA_real_, (start_event$target_z[[1]] - pz) / fz),
        board_x = px + fx * ray_t,
        board_y = py + fy * ray_t,
        relative_x = board_x - start_event$target_x[[1]],
        relative_y = board_y - start_event$target_y[[1]]
      ) |>
      filter(
        !is.na(ray_t),
        is.finite(ray_t),
        ray_t > 0,
        ray_t < 5,
        !is.na(relative_x),
        !is.na(relative_y),
        is.finite(relative_x),
        is.finite(relative_y),
        abs(relative_x) < 1.5,
        abs(relative_y) < 1.5
      ) |>
      arrange(ts)

    title_text <- paste(selected_task, selected_block, "Trial", selected_attempt, "Phase Breakdown")
    subtitle_text <- "Full trial window from Trial Started to Trial Accepted."
  }

  if (nrow(path) < 3) {
    return(list(error = "Not enough projected samples were available to estimate phases"))
  }

  path <- path |>
    mutate(
      time_sec = as.numeric(difftime(ts, first(ts), units = "secs")),
      dx = c(NA_real_, diff(relative_x)),
      dy = c(NA_real_, diff(relative_y)),
      move_dx = c(NA_real_, diff(move_x)),
      move_dy = c(NA_real_, diff(move_y)),
      move_dz = c(NA_real_, diff(move_z)),
      dt = c(NA_real_, diff(time_sec)),
      projected_speed_mps = sqrt(dx^2 + dy^2) / pmax(dt, 1e-4),
      projected_speed_mps = ifelse(is.finite(projected_speed_mps), projected_speed_mps, NA_real_),
      projected_speed_mps = replace_na(projected_speed_mps, 0),
      movement_speed_mps = sqrt(move_dx^2 + move_dy^2 + move_dz^2) / pmax(dt, 1e-4),
      movement_speed_mps = ifelse(is.finite(movement_speed_mps), movement_speed_mps, NA_real_),
      movement_speed_mps = replace_na(movement_speed_mps, 0),
      distance_to_target = sqrt(relative_x^2 + relative_y^2)
    )

  peak_idx <- which.max(path$movement_speed_mps)
  if (length(peak_idx) == 0 || !is.finite(path$movement_speed_mps[[peak_idx]])) {
    return(list(error = "Peak speed could not be estimated for this response"))
  }

  peak_speed <- path$movement_speed_mps[[peak_idx]]
  speed_threshold <- max(0.02, peak_speed * 0.2)
  near_target <- path$distance_to_target <= target_radius
  slow_enough <- path$movement_speed_mps <= speed_threshold

  hover_start_idx <- NA_integer_
  idx <- nrow(path)
  while (idx >= 1 && isTRUE(near_target[[idx]]) && isTRUE(slow_enough[[idx]])) {
    idx <- idx - 1
  }
  if (idx < nrow(path)) hover_start_idx <- idx + 1L
  if (!is.na(hover_start_idx) && hover_start_idx <= peak_idx) hover_start_idx <- NA_integer_

  total_time_ms <- max(path$time_sec, na.rm = TRUE) * 1000
  peak_time_ms <- path$time_sec[[peak_idx]] * 1000
  hover_start_ms <- if (!is.na(hover_start_idx)) path$time_sec[[hover_start_idx]] * 1000 else NA_real_

  acceleration_ms <- max(0, peak_time_ms)
  if (!is.na(hover_start_ms)) {
    deceleration_ms <- max(0, hover_start_ms - peak_time_ms)
    hover_ms <- max(0, total_time_ms - hover_start_ms)
  } else {
    deceleration_ms <- max(0, total_time_ms - peak_time_ms)
    hover_ms <- 0
  }

  phase_df <- tibble(
    Phase = factor(c("Acceleration", "Deceleration", "Hover"), levels = c("Acceleration", "Deceleration", "Hover")),
    DurationMs = c(acceleration_ms, deceleration_ms, hover_ms)
  ) |>
    filter(DurationMs > 0.5)

  final_phase <- if (hover_ms > 0.5) "Hover" else if (deceleration_ms > 0.5) "Deceleration" else "Acceleration"
  final_distance_m <- path$distance_to_target[[nrow(path)]]
  peak_speed_mps <- max(path$movement_speed_mps, na.rm = TRUE)
  peak_projected_speed_mps <- max(path$projected_speed_mps, na.rm = TRUE)
  movement_anchor_source <- {
    vals <- unique(na.omit(path$move_source))
    if (length(vals) == 0) "ControllerPosFallback" else vals[[1]]
  }

  list(
    error = NULL,
    title = title_text,
    subtitle = paste0(subtitle_text, " Confirm ended during: ", final_phase, "."),
    target_radius_m = target_radius,
    speed_threshold_mps = speed_threshold,
    phase_df = phase_df,
    speed_df = path |>
      transmute(
        TimeMs = time_sec * 1000,
        SpeedMps = movement_speed_mps,
        ProjectedSpeedMps = projected_speed_mps,
        DistanceM = distance_to_target
      ),
    total_time_ms = total_time_ms,
    peak_speed_mps = peak_speed_mps,
    peak_projected_speed_mps = peak_projected_speed_mps,
    movement_anchor_source = movement_anchor_source,
    peak_time_ms = peak_time_ms,
    hover_start_ms = hover_start_ms,
    hover_ms = hover_ms,
    final_distance_m = final_distance_m,
    final_phase = final_phase
  )
}

build_phase_plot <- function(meta_data, sample_data, event_data, selected_task = "Exposure", selected_block = "Exposure", selected_attempt = NA_real_, time_window = 1.0) {
  profile <- extract_phase_profile(meta_data, sample_data, event_data, selected_task, selected_block, selected_attempt, time_window)

  if (!is.null(profile$error)) {
    return(
      plot_ly() |>
        layout(
          annotations = list(
            text = profile$error,
            x = 0.5,
            y = 0.5,
            xref = "paper",
            yref = "paper",
            showarrow = FALSE,
            font = list(size = 18)
          ),
          xaxis = list(visible = FALSE),
          yaxis = list(visible = FALSE)
        )
    )
  }

  phase_colors <- c("Acceleration" = "#2563eb", "Deceleration" = "#0ea5e9", "Hover" = "#16a34a")

  phase_plot_df <- profile$phase_df |>
    mutate(
      PhaseLabel = as.character(Phase),
      hover_text = paste0(PhaseLabel, "<br>Duration: ", round(DurationMs), " ms")
    )

  phase_plot <- plot_ly(
    data = phase_plot_df,
    x = ~DurationMs,
    y = rep("Phases", nrow(phase_plot_df)),
    type = "bar",
    orientation = "h",
    color = ~PhaseLabel,
    colors = phase_colors,
    text = ~hover_text,
    textposition = "inside",
    hoverinfo = "text"
  ) |>
    layout(
      barmode = "stack",
      showlegend = TRUE,
      xaxis = list(title = "Time within visible response window (ms)", zeroline = FALSE),
      yaxis = list(title = "", showticklabels = FALSE)
    )

  speed_plot <- plot_ly(
    data = profile$speed_df,
    x = ~TimeMs,
    y = ~SpeedMps,
    type = "scatter",
    mode = "lines",
    line = list(color = "#475569", width = 3),
    hovertemplate = paste0(
      "Time: %{x:.0f} ms<br>",
      "Movement-anchor speed: %{y:.3f} m/s<extra></extra>"
    ),
    name = "Movement-anchor speed"
  ) |>
    add_trace(
      x = c(min(profile$speed_df$TimeMs, na.rm = TRUE), max(profile$speed_df$TimeMs, na.rm = TRUE)),
      y = c(profile$speed_threshold_mps, profile$speed_threshold_mps),
      type = "scatter",
      mode = "lines",
      line = list(color = "#16a34a", dash = "dash"),
      inherit = FALSE,
      hoverinfo = "skip",
      name = "Hover speed threshold",
      showlegend = FALSE
    ) |>
    add_markers(
      x = profile$peak_time_ms,
      y = max(profile$speed_df$SpeedMps, na.rm = TRUE),
      marker = list(color = "#1d4ed8", size = 9),
      name = "Peak speed",
      hoverinfo = "skip",
      showlegend = FALSE
    ) |>
    layout(
      xaxis = list(title = "Time within visible response window (ms)"),
      yaxis = list(title = "Movement-anchor speed (m/s)", zeroline = FALSE),
      showlegend = FALSE
    )

  if (!is.na(profile$hover_start_ms)) {
    speed_plot <- speed_plot |>
      add_trace(
        x = c(profile$hover_start_ms, profile$hover_start_ms),
        y = c(0, max(profile$speed_df$SpeedMps, na.rm = TRUE)),
        type = "scatter",
        mode = "lines",
        line = list(color = "#16a34a", dash = "dot"),
        inherit = FALSE,
        hoverinfo = "skip",
        name = "Hover starts",
        showlegend = FALSE
      )
  }

  summary_items <- c(
    paste0("Window: ", round(profile$total_time_ms), " ms"),
    paste0("Anchor: ", profile$movement_anchor_source),
    paste0("Peak movement speed: ", sprintf("%.2f", profile$peak_speed_mps), " m/s"),
    paste0("Peak projected speed: ", sprintf("%.2f", profile$peak_projected_speed_mps), " m/s"),
    paste0("Hover: ", round(profile$hover_ms), " ms"),
    paste0("Final distance: ", sprintf("%.1f", profile$final_distance_m * 100), " cm")
  )

  subplot(phase_plot, speed_plot, nrows = 2, heights = c(0.32, 0.68), shareX = FALSE, titleY = TRUE) |>
    layout(
      title = list(
        text = paste0(
          profile$title,
          "<br><sup>",
          profile$subtitle,
          " Hover radius: ",
          sprintf("%.1f", profile$target_radius_m * 100),
          " cm. Dashed green = hover speed threshold. Dotted green = hover start.</sup>",
          "<br><sup>",
          paste(summary_items, collapse = " | "),
          "</sup>"
        )
      ),
      margin = list(t = 110)
    )
}

# Build the lower plot on the Trajectories tab.
#
# Again this differs by task:
# - for measurement tasks it shows absolute trial error over time,
# - for exposure it shows final hit distance per attempt.
build_attempt_error_plot <- function(event_data, selected_task = "Exposure", selected_block = "Exposure", overlap_blocks = FALSE) {
  if (selected_task != "Exposure") {
    error_col <- if (selected_task == "LineBisection") "ErrorCm" else "SignedOffsetCm"
    blocks_to_show <- if (overlap_blocks) c("Baseline", "Post") else selected_block

    attempts <- event_data |>
      filter(TaskMode == selected_task, BlockType %in% blocks_to_show, Event == "Trial Accepted") |>
      mutate(
        AttemptIndex = as_num(TrialIndex),
        final_error = abs(as_num(.data[[error_col]]))
      ) |>
      filter(!is.na(AttemptIndex), !is.na(final_error), !is.na(BlockType))

    if (nrow(attempts) == 0) {
      return(
        ggplot() +
          annotate("text", x = 1, y = 1, label = "No trial error data found for this task/block") +
          theme_void()
      )
    }

    metric_label <- ifelse(
      selected_task == "LineBisection",
      "Absolute deviation from midpoint (cm)",
      "Absolute deviation from target center (cm)"
    )

    return(
      ggplot(attempts, aes(x = AttemptIndex, y = final_error, color = BlockType, group = BlockType)) +
        geom_line(linewidth = 0.7, alpha = 0.7) +
        geom_point(size = 3) +
        scale_color_manual(values = c("Baseline" = "#94a3b8", "Post" = "#0ea5a4", "Exposure" = "#dc2626")) +
        labs(
          title = if (overlap_blocks) paste(selected_task, "Baseline vs Post Final Error by Trial") else paste(selected_task, selected_block, "Final Error by Trial"),
          subtitle = if (overlap_blocks) "Each dot is one accepted trial. Lower is better. Compare blocks directly." else "Each dot is one accepted trial in this block. Lower is better.",
          x = "Trial",
          y = metric_label,
          color = NULL
        ) +
        theme_minimal(base_size = 13) +
        theme(legend.position = if (overlap_blocks) "top" else "none")
    )
  }

  attempts <- event_data |>
    filter(TaskMode == selected_task, Event %in% c("Mole Hit", "Mole Missed")) |>
    mutate(
      AttemptIndex = as_num(AttemptIndex),
      hit_x = as_num(HitPositionWorldX),
      hit_y = as_num(HitPositionWorldY),
      target_x = as_num(MolePositionWorldX),
      target_y = as_num(MolePositionWorldY),
      outcome = ifelse(Event == "Mole Hit", "Hit", "Miss"),
      final_error = sqrt((hit_x - target_x)^2 + (hit_y - target_y)^2)
    ) |>
    filter(!is.na(AttemptIndex), !is.na(final_error))

  if (nrow(attempts) == 0) {
    return(
      ggplot() +
        annotate("text", x = 1, y = 1, label = "No attempt error data found for this task") +
        theme_void()
    )
  }

  ggplot(attempts, aes(x = AttemptIndex, y = final_error, color = outcome)) +
    geom_line(color = "#94a3b8", linewidth = 0.7) +
    geom_point(size = 3) +
    scale_color_manual(values = c("Hit" = "#0ea5a4", "Miss" = "#dc2626")) +
    labs(
      title = paste(selected_task, "Final Error by Attempt"),
      subtitle = "Distance between the registered hit point and the target center at confirmation. Lower is better.",
      x = "Attempt",
      y = "Final error on board",
      color = NULL
    ) +
    theme_minimal(base_size = 13) +
    theme(legend.position = "top")
}

# ---------- Quality-tab helpers ----------

# Convert task-specific Event rows into a common quality table with:
# - TrialIndex
# - absolute_error
# - outcome
#
# This lets the Quality tab reuse one plotting/statistics pipeline across different tasks.
extract_quality_trials <- function(event_data, selected_task, selected_block) {
  if (nrow(event_data) == 0) return(tibble())

  if (selected_task == "Exposure") {
    return(
      event_data |>
        filter(TaskMode == selected_task, BlockType == selected_block, Event %in% c("Mole Hit", "Mole Missed")) |>
        mutate(
          TrialIndex = as_num(AttemptIndex),
          absolute_error = as_num(HitDistanceMeters),
          outcome = ifelse(Event == "Mole Hit", "Hit", "Miss")
        ) |>
        filter(!is.na(TrialIndex), !is.na(absolute_error))
    )
  }

  if (selected_task == "OpenLoop") {
    return(
      event_data |>
        filter(TaskMode == selected_task, BlockType == selected_block, Event == "Trial Accepted") |>
        mutate(
          TrialIndex = as_num(TrialIndex),
          signed_error = as_num(SignedOffsetCm),
          absolute_error = abs(signed_error),
          outcome = "Accepted"
        ) |>
        filter(!is.na(TrialIndex), !is.na(absolute_error))
    )
  }

  if (selected_task == "LineBisection") {
    return(
      event_data |>
        filter(TaskMode == selected_task, BlockType == selected_block, Event == "Trial Accepted") |>
        mutate(
          TrialIndex = as_num(TrialIndex),
          signed_error = as_num(ErrorCm),
          absolute_error = abs(signed_error),
          outcome = "Accepted"
        ) |>
        filter(!is.na(TrialIndex), !is.na(absolute_error))
    )
  }

  if (selected_task == "Landmark") {
    return(
      event_data |>
        filter(TaskMode == selected_task, BlockType == selected_block, Event == "Trial Accepted") |>
        mutate(
          TrialIndex = as_num(TrialIndex),
          absolute_error = ifelse(as_num(IsCorrect) == 1, 0, 1),
          outcome = ifelse(as_num(IsCorrect) == 1, "Correct", "Incorrect")
        ) |>
        filter(!is.na(TrialIndex), !is.na(absolute_error))
    )
  }

  tibble()
}

# Compute quality metrics used by both the cards and the plot.
# This includes:
# - mean and median error,
# - a simple outlier threshold,
# - drift over trials (linear slope),
# - and hit rate for exposure.
compute_quality_metrics <- function(quality_trials, selected_task) {
  if (nrow(quality_trials) == 0) {
    return(list(
      mean_abs_error = NA_real_,
      median_abs_error = NA_real_,
      outlier_count = NA_integer_,
      outlier_threshold = NA_real_,
      slope = NA_real_,
      trend_label = "No quality data",
      hit_rate = NA_real_,
      stable_center = NA_real_,
      stable_spread = NA_real_,
      trial_count = 0
    ))
  }

  x <- quality_trials$TrialIndex
  y <- quality_trials$absolute_error

  q1 <- as.numeric(quantile(y, 0.25, na.rm = TRUE))
  q3 <- as.numeric(quantile(y, 0.75, na.rm = TRUE))
  iqr <- q3 - q1
  outlier_threshold <- q3 + 1.5 * iqr
  outlier_count <- sum(y > outlier_threshold, na.rm = TRUE)

  slope <- if (length(unique(x)) > 1) {
    as.numeric(coef(lm(absolute_error ~ TrialIndex, data = quality_trials))[["TrialIndex"]])
  } else {
    NA_real_
  }

  trend_label <- ifelse(
    is.na(slope),
    "No drift estimate",
    ifelse(abs(slope) < 0.001, "Performance stayed stable over trials",
           ifelse(slope > 0, "Error increased over trials", "Error decreased over trials"))
  )

  hit_rate <- if (selected_task == "Exposure") {
    mean(quality_trials$outcome == "Hit", na.rm = TRUE)
  } else {
    NA_real_
  }

  stable_center <- if ("signed_error" %in% names(quality_trials)) {
    stable_cluster_center(quality_trials$signed_error)
  } else {
    NA_real_
  }

  stable_spread <- if ("signed_error" %in% names(quality_trials)) {
    stable_cluster_spread(quality_trials$signed_error)
  } else {
    NA_real_
  }

  list(
    mean_abs_error = mean(y, na.rm = TRUE),
    median_abs_error = median(y, na.rm = TRUE),
    outlier_count = outlier_count,
    outlier_threshold = outlier_threshold,
    slope = slope,
    trend_label = trend_label,
    hit_rate = hit_rate,
    stable_center = stable_center,
    stable_spread = stable_spread,
    trial_count = nrow(quality_trials)
  )
}

# Small label helpers so the quality tab can talk about metres, centimetres,
# or abstract task units without hardcoding those strings in many places.
quality_units_label <- function(selected_task) {
  if (selected_task == "Exposure") {
    return("m")
  }
  if (selected_task %in% c("OpenLoop", "LineBisection")) {
    return("cm")
  }
  if (selected_task == "Landmark") {
    return("task units")
  }
  "units"
}

quality_error_label <- function(selected_task) {
  if (selected_task == "Exposure") {
    return("Distance from target center on the board")
  }
  if (selected_task == "OpenLoop") {
    return("Distance from the true target location")
  }
  if (selected_task == "LineBisection") {
    return("Distance from the true midpoint")
  }
  if (selected_task == "Landmark") {
    return("Trial error")
  }
  "Absolute error"
}

quality_title_label <- function(selected_task, selected_block) {
  if (identical(selected_task, selected_block)) {
    return(paste(selected_task, "Quality Overview"))
  }

  paste(selected_task, selected_block, "Quality Overview")
}

# Build the main Quality-tab plot.
# It combines:
# - per-trial values,
# - a dashed outlier threshold,
# - and a simple linear trend line across the block.
build_quality_plot <- function(quality_trials, selected_task, selected_block) {
  if (nrow(quality_trials) == 0) {
    return(
      ggplot() +
        annotate("text", x = 1, y = 1, label = "No quality data found for this task/block") +
        theme_void()
    )
  }

  metrics <- compute_quality_metrics(quality_trials, selected_task)

  ggplot(quality_trials, aes(x = TrialIndex, y = absolute_error)) +
    geom_line(color = "#94a3b8", linewidth = 0.8) +
    geom_point(
      aes(color = outcome),
      size = 2.8
    ) +
    geom_hline(yintercept = metrics$outlier_threshold, linetype = "dashed", color = "#dc2626") +
    geom_smooth(method = "lm", se = FALSE, color = "#1d4ed8", linewidth = 0.8) +
    labs(
      title = quality_title_label(selected_task, selected_block),
      subtitle = "Per-trial distance from the intended target, with unusually large errors and overall trend",
      x = "Trial / Attempt",
      y = paste(quality_error_label(selected_task), "(", quality_units_label(selected_task), ")"),
      color = NULL
    ) +
    theme_minimal(base_size = 13) +
    theme(legend.position = "top")
}

# Compact summary table used on the Summary tab.
# This strips the raw Summary.csv down to the columns most useful for human inspection.
summary_compact_table <- function(summary_data) {
  summary_data |>
    filter(SummaryType %in% c("Aftereffect", "BlockMetric")) |>
    select(any_of(c(
      "TaskMode", "BlockType", "SummaryType", "MetricName", "Estimator", "MetricUnits",
      "BaselineValue", "BaselineSd", "PostValue", "PostSd", "SignedDelta", "Magnitude",
      "NormalizedMagnitude", "RawBaselineMean", "RawPostMean", "RawSignedDelta",
      "RawMagnitude", "TrialCount", "ConfiguredEffectMode", "Notes"
    )))
}

# ---------- Compare-tab helpers ----------

# Build one cross-session dataset by reading the Aftereffect rows from every available session.
# This is the backbone of the Compare tab.
build_comparison_dataset <- function(session_rows) {
  if (nrow(session_rows) == 0) return(tibble())

  start_time <- Sys.time()

  result <- purrr::map_dfr(seq_len(nrow(session_rows)), function(i) {
    row <- session_rows[i, ]
    summary_data <- coerce_summary_numeric_columns(clean_log_df(safe_read_csv(row$Summary)))
    if (nrow(summary_data) == 0) return(tibble())

    aftereffect_data <- summary_data |>
      filter(SummaryType == "Aftereffect")

    if (nrow(aftereffect_data) == 0) return(tibble())

    event_data <- clean_log_df(safe_read_csv(row$Event))
    aftereffect_data <- coerce_summary_numeric_columns(
      build_stable_perceived_center_aftereffects(aftereffect_data, event_data)
    )

    if (!"StudyGlobalParticipantId" %in% names(aftereffect_data)) {
      aftereffect_data$StudyGlobalParticipantId <- row$study_global_participant_id[[1]]
    }

    if (!"StudySessionIndex" %in% names(aftereffect_data)) {
      aftereffect_data$StudySessionIndex <- row$study_session_index[[1]]
    }

    aftereffect_data |>
      mutate(
        SessionID = row$session_id[[1]],
        TimestampLabel = row$timestamp[[1]],
        TimestampDt = as_time(row$timestamp[[1]]),
        SessionState = row$session_state[[1]],
        InputModeLabel = row$input_mode_label[[1]],
        StudyGlobalParticipantId = dplyr::coalesce(
          suppressWarnings(as.integer(StudyGlobalParticipantId)),
          row$study_global_participant_id[[1]]
        ),
        StudySessionIndex = dplyr::coalesce(
          suppressWarnings(as.integer(StudySessionIndex)),
          row$study_session_index[[1]]
        ),
        Magnitude = as_num(Magnitude),
        SignedDelta = as_num(SignedDelta),
        BaselineValue = as_num(BaselineValue),
        PostValue = as_num(PostValue),
        BaselineSd = as_num(BaselineSd),
        PostSd = as_num(PostSd),
        ConsistencyDelta = PostSd - BaselineSd,
        TowardTargetDelta = ifelse(
          MetricName == "Accuracy",
          PostValue - BaselineValue,
          abs(BaselineValue) - abs(PostValue)
        )
      ) |>
      mutate(
        RunLabel = paste0(
          ifelse(is.na(TimestampLabel) | TimestampLabel == "", "Unknown time", TimestampLabel),
          " | ",
          ifelse(is.na(InputModeLabel) | InputModeLabel == "", "unknown", InputModeLabel),
          " | ",
          substr(SessionID, 1, 8)
        )
      )
  }) |>
    arrange(TimestampDt)

  cat(sprintf(
    "[PrismShiny] built comparison dataset sessions=%d rows=%d in %.1fs\n",
    nrow(session_rows),
    nrow(result),
    as.numeric(difftime(Sys.time(), start_time, units = "secs"))
  ))

  result
}

# ---------- Participant-tab helpers ----------

participant_label <- function(participant_id) {
  ifelse(is.na(participant_id), "Unknown participant", paste0("P", participant_id))
}

build_participant_signed_plot <- function(participant_data) {
  if (nrow(participant_data) == 0) {
    return(ggplot() + annotate("text", x = 1, y = 1, label = "No participant selected") + theme_void())
  }

  plot_data <- participant_data |>
    mutate(
      EffectGroup = factor(
        ConfiguredEffectMode,
        levels = c("None", "Translation", "Rotation", "Skew")
      ),
      hover_text = paste0(
        "Participant: P", StudyGlobalParticipantId,
        "<br>Run: ", StudySessionIndex,
        "<br>Task: ", TaskMode,
        "<br>Effect: ", ConfiguredEffectMode,
        "<br>Signed delta: ", sprintf("%.2f", SignedDelta), " ", MetricUnits,
        "<br>Magnitude: ", sprintf("%.2f", Magnitude), " ", MetricUnits,
        "<br>Baseline SD: ", sprintf("%.2f", BaselineSd),
        "<br>Post SD: ", sprintf("%.2f", PostSd)
      )
    )

  ggplot(plot_data, aes(x = EffectGroup, y = SignedDelta, color = TaskMode, group = TaskMode)) +
    geom_hline(yintercept = 0, color = "#94a3b8", linetype = "dashed") +
    geom_line(alpha = 0.45) +
    geom_point(size = 3) +
    facet_wrap(~TaskMode, scales = "free_y") +
    labs(
      title = "Participant Direction of Shift",
      subtitle = "Signed change from baseline to post in the participant's stable perceived middle. Units: cm.",
      x = "Configured effect",
      y = "Signed baseline-to-post shift (cm)",
      color = "Task"
    ) +
    theme_minimal(base_size = 13)
}

build_participant_magnitude_plot <- function(participant_data) {
  if (nrow(participant_data) == 0) {
    return(ggplot() + annotate("text", x = 1, y = 1, label = "No participant selected") + theme_void())
  }

  plot_data <- participant_data |>
    mutate(
      EffectGroup = factor(
        ConfiguredEffectMode,
        levels = c("None", "Translation", "Rotation", "Skew")
      ),
      hover_text = paste0(
        "Run: ", StudySessionIndex,
        "<br>Task: ", TaskMode,
        "<br>Effect: ", ConfiguredEffectMode,
        "<br>Magnitude: ", sprintf("%.2f", Magnitude), " ", MetricUnits,
        "<br>Baseline: ", sprintf("%.2f", BaselineValue),
        "<br>Post: ", sprintf("%.2f", PostValue)
      )
    )

  ggplot(plot_data, aes(x = EffectGroup, y = Magnitude, fill = TaskMode)) +
    geom_col(position = position_dodge(width = 0.7), width = 0.65, alpha = 0.82) +
    labs(
      title = "Participant Absolute Shift",
      subtitle = "Absolute baseline-to-post change in perceived middle. Large None values suggest drift/noise before interpreting perturbations. Units: cm.",
      x = "Configured effect",
      y = "Absolute shift (cm)",
      fill = "Task"
    ) +
    theme_minimal(base_size = 13)
}

build_effect_minus_none_dataset <- function(compare_data) {
  if (nrow(compare_data) == 0) return(tibble())

  normalized_source <- compare_data |>
    mutate(
      StudyGlobalParticipantId = suppressWarnings(as.integer(StudyGlobalParticipantId)),
      Magnitude = as_num(Magnitude),
      SignedDelta = as_num(SignedDelta),
      BaselineSd = as_num(BaselineSd),
      PostSd = as_num(PostSd)
    ) |>
    filter(!is.na(StudyGlobalParticipantId), !is.na(TaskMode), !is.na(ConfiguredEffectMode))

  none_reference <- normalized_source |>
    filter(ConfiguredEffectMode == "None") |>
    group_by(StudyGlobalParticipantId, TaskMode) |>
    summarise(
      NoneMagnitude = mean(Magnitude, na.rm = TRUE),
      NoneSignedDelta = mean(SignedDelta, na.rm = TRUE),
      NoneBaselineSd = mean(BaselineSd, na.rm = TRUE),
      .groups = "drop"
    )

  normalized_source |>
    left_join(none_reference, by = c("StudyGlobalParticipantId", "TaskMode")) |>
    mutate(
      MagnitudeMinusNone = Magnitude - NoneMagnitude,
      SignedDeltaMinusNone = SignedDelta - NoneSignedDelta,
      EffectCategory = ifelse(ConfiguredEffectMode == "None", "Control", "Perturbation"),
      IsNormalizedReferenceAvailable = !is.na(NoneMagnitude)
    )
}

build_participant_normalized_plot <- function(participant_data) {
  normalized_source <- build_effect_minus_none_dataset(participant_data)
  if (!"ConfiguredEffectMode" %in% names(normalized_source)) {
    return(ggplot() + annotate("text", x = 1, y = 1, label = "No normalized participant data available") + theme_void())
  }

  normalized <- normalized_source |>
    filter(ConfiguredEffectMode != "None")

  if (nrow(normalized) == 0) {
    return(ggplot() + annotate("text", x = 1, y = 1, label = "No normalized participant data available") + theme_void())
  }

  plot_data <- normalized |>
    mutate(
      EffectGroup = factor(ConfiguredEffectMode, levels = c("Translation", "Rotation", "Skew")),
      hover_text = paste0(
        "Participant: P", StudyGlobalParticipantId,
        "<br>Task: ", TaskMode,
        "<br>Effect: ", ConfiguredEffectMode,
        "<br>Effect magnitude: ", sprintf("%.2f", Magnitude),
        "<br>None magnitude: ", sprintf("%.2f", NoneMagnitude),
        "<br>Effect - None: ", sprintf("%.2f", MagnitudeMinusNone)
      )
    )

  ggplot(plot_data, aes(x = EffectGroup, y = MagnitudeMinusNone, fill = TaskMode)) +
    geom_hline(yintercept = 0, color = "#475569", linetype = "dashed") +
    geom_col(position = position_dodge(width = 0.7), width = 0.65, alpha = 0.84) +
    labs(
      title = "Perturbation Shift Above This Participant's No-effect Run",
      subtitle = "Positive values mean this effect shifted the perceived middle more than the participant's own None run for the same task. Units: cm.",
      x = "Configured effect",
      y = "Extra shift beyond None (cm)",
      fill = "Task"
    ) +
    theme_minimal(base_size = 13)
}

build_normalized_group_plot <- function(normalized_data) {
  plot_data <- normalized_data |>
    filter(ConfiguredEffectMode != "None", IsNormalizedReferenceAvailable)

  if (nrow(plot_data) == 0) {
    return(ggplot() + annotate("text", x = 1, y = 1, label = "No normalized comparison data available") + theme_void())
  }

  plot_data <- plot_data |>
    mutate(
      EffectGroup = factor(ConfiguredEffectMode, levels = c("Translation", "Rotation", "Skew")),
      hover_text = paste0(
        "P", StudyGlobalParticipantId,
        "<br>Input: ", InputModeLabel,
        "<br>Task: ", TaskMode,
        "<br>Effect: ", ConfiguredEffectMode,
        "<br>Effect magnitude: ", sprintf("%.2f", Magnitude),
        "<br>None magnitude: ", sprintf("%.2f", NoneMagnitude),
        "<br>Effect - None: ", sprintf("%.2f", MagnitudeMinusNone)
      )
    )

  ggplot(plot_data, aes(x = EffectGroup, y = MagnitudeMinusNone, color = InputModeLabel)) +
    geom_hline(yintercept = 0, color = "#475569", linetype = "dashed") +
    geom_boxplot(outlier.shape = NA, alpha = 0.22, position = position_dodge(width = 0.6)) +
    geom_rug(
      aes(color = InputModeLabel),
      sides = "l",
      alpha = 0.7,
      linewidth = 0.55
    ) +
    facet_wrap(~TaskMode, scales = "free_y") +
    labs(
      title = "Perturbation Shift Above Participant-specific None",
      subtitle = "Boxplots show the distribution after subtracting each participant's no-effect shift. Side ticks show individual modules. Values above zero suggest effect beyond ordinary drift/noise. Units: cm.",
      x = "Configured effect",
      y = "Extra shift beyond None (cm)",
      color = "Input mode"
    ) +
    theme_minimal(base_size = 13)
}

# Comparison plot #1:
# grouped distributions by configured effect and input mode.
build_comparison_plot <- function(compare_data) {
  if (nrow(compare_data) == 0) {
    return(
      ggplot() +
        annotate("text", x = 1, y = 1, label = "No aftereffect sessions match the current filters") +
        theme_void()
    )
  }

  grouped <- compare_data |>
    mutate(EffectGroup = factor(ConfiguredEffectMode, levels = c("None", "Translation", "Rotation", "Skew")))

  ggplot(grouped, aes(x = EffectGroup, y = Magnitude, color = InputModeLabel)) +
    geom_boxplot(outlier.shape = NA, alpha = 0.25, position = position_dodge(width = 0.6)) +
    geom_rug(
      aes(color = InputModeLabel),
      sides = "l",
      alpha = 0.7,
      linewidth = 0.55
    ) +
    facet_wrap(~TaskMode, scales = "free_y") +
    labs(
      title = "Participant-level Baseline-to-post Shifts",
      subtitle = "Boxplots summarize the distribution. Side ticks show individual modules without using dot markers. Units: cm.",
      x = "Configured effect",
      y = "Absolute shift from baseline to post (cm)",
      color = "Input mode"
    ) +
    theme_minimal(base_size = 13)
}

# ---------- User interface ----------

# The UI defines the visible structure of the app:
# - one sidebar for log-folder/session controls,
# - and one main tabset for Summary / Compare / Quality / Spatial / Trajectories.
ui <- fluidPage(
  titlePanel("Prism Analysis"),
  tags$head(
    tags$style(HTML("
      .status-box {padding: 12px 16px; border-radius: 8px; margin-bottom: 12px; font-weight: 600;}
      .status-finished {background: #e8f7ee; color: #166534;}
      .status-aborted {background: #fef2f2; color: #991b1b;}
      .metric-card {background: #f8fafc; border: 1px solid #e5e7eb; border-radius: 8px; padding: 12px 14px; margin-bottom: 12px;}
      .metric-title {font-size: 12px; text-transform: uppercase; color: #6b7280;}
      .metric-value {font-size: 28px; font-weight: 700; color: #111827;}
      .metric-sub {font-size: 13px; color: #4b5563;}
      .analysis-note {background: #fff7ed; border: 1px solid #fed7aa; color: #7c2d12; border-radius: 8px; padding: 12px 14px; margin-bottom: 14px; line-height: 1.35;}
    "))
  ),
  sidebarLayout(
    sidebarPanel(
      textInput("log_dir", "Log Folder", value = default_log_dir),
      selectInput("session_scope", "Show Sessions", choices = c("Participant", "Debug", "All"), selected = "Participant"),
      actionButton("refresh", "Refresh Sessions"),
      br(), br(),
      uiOutput("session_picker"),
      br(),
      uiOutput("session_status")
    ),
    mainPanel(
      tabsetPanel(
        id = "main_tabs",
        tabPanel(
          "Summary",
          fluidRow(
            column(4, uiOutput("summary_cards")),
            column(8, plotOutput("summary_plot", height = "360px"))
          ),
          h3("Session Summary Table"),
          DTOutput("summary_table")
        ),
        tabPanel(
          "Compare",
          div(class = "analysis-note",
              strong("How to read these plots: "),
              "Baseline and post responses are each reduced to the participant's most stable perceived middle. ",
              "This keeps all accepted trials, but random far-away responses have less influence than the cluster where the participant repeatedly responded. ",
              "Magnitude is the absolute baseline-to-post shift in cm. The normalized plot subtracts that participant's own no-effect shift for the same task, so values above zero are the perturbation effect above ordinary drift/noise."
          ),
          fluidRow(
            column(3, selectInput("compare_task", "Task", choices = c("All"))),
            column(3, selectInput("compare_input_mode", "Input Mode", choices = c("All"))),
            column(3, selectInput("compare_effect", "Effect", choices = c("All"))),
            column(3, checkboxInput("compare_finished_only", "Finished sessions only", value = TRUE))
          ),
          fluidRow(
            column(4, uiOutput("compare_cards")),
            column(8, plotOutput("compare_plot", height = "360px"))
          ),
          plotOutput("compare_normalized_plot", height = "380px"),
          h3("Comparison Table"),
          DTOutput("compare_table")
        ),
        tabPanel(
          "Participant",
          div(class = "analysis-note",
              strong("Participant view: "),
              "This groups all modules for one participant. The signed plot shows direction of baseline-to-post shift; the magnitude plot ignores direction; the normalized plot asks whether each perturbation shifted responses more than that participant's own no-effect run."
          ),
          fluidRow(
            column(4, selectInput("participant_id", "Participant", choices = c("No participants loaded"))),
            column(8, uiOutput("participant_cards"))
          ),
          fluidRow(
            column(6, plotOutput("participant_signed_plot", height = "380px")),
            column(6, plotOutput("participant_magnitude_plot", height = "380px"))
          ),
          plotOutput("participant_normalized_plot", height = "360px"),
          h3("Participant Module Table"),
          DTOutput("participant_table")
        ),
        tabPanel(
          "Quality",
          fluidRow(
            column(3, selectInput("quality_task", "Task", choices = c("Exposure", "OpenLoop"))),
            column(3, selectInput("quality_block", "Block", choices = c("Exposure", "Baseline", "Post")))
          ),
          fluidRow(
            column(4, uiOutput("quality_cards")),
            column(8, plotOutput("quality_plot", height = "420px"))
          ),
          DTOutput("quality_table")
        ),
        tabPanel(
          "Spatial",
          fluidRow(
            column(3, selectInput("spatial_task", "Task", choices = c("Exposure", "OpenLoop"))),
            column(3, selectInput("spatial_block", "Block", choices = c("Exposure", "Baseline", "Post")))
          ),
          plotOutput("spatial_plot", height = "620px")
        ),
        tabPanel(
          "Trajectories",
          fluidRow(
            column(3, selectInput("trajectory_task", "Task", choices = c("Exposure", "OpenLoop"))),
            column(3, selectInput("trajectory_block", "Block", choices = c("Exposure", "Baseline", "Post"))),
            column(3, uiOutput("attempt_picker")),
            column(3, uiOutput("trajectory_window_ui"))
          ),
          checkboxInput("trajectory_overlap", "Overlay Baseline and Post for non-exposure tasks", value = FALSE),
          uiOutput("trajectory_help"),
          plotOutput("trajectory_plot", height = "520px"),
          plotlyOutput("trajectory_phase_plot", height = "640px"),
          plotOutput("trajectory_error_plot", height = "320px")
        )
      )
    )
  )
)

# ---------- Server / reactivity ----------
#
# In Shiny, the server function is where inputs, reactive expressions, and outputs are connected.
# A useful way to read this section is:
# 1. session discovery and loading,
# 2. per-tab helper reactives,
# 3. UI outputs,
# 4. plot outputs,
# 5. observers that keep dropdowns in sync.
server <- function(input, output, session) {
  # session_index stores the discovered sessions for the currently selected folder.
  session_index <- reactiveVal(discover_sessions(default_log_dir))

  filtered_session_index <- reactive({
    sessions <- session_index()
    if (nrow(sessions) == 0) {
      return(sessions)
    }

    session_state_norm <- tolower(trimws(coalesce(sessions$session_state, "")))
    sessions <- sessions[session_state_norm == "" | session_state_norm == "finished", , drop = FALSE]

    if (nrow(sessions) == 0) {
      return(sessions)
    }

    scope <- normalize_session_scope(input$session_scope)
    experiment_mode <- tolower(trimws(coalesce(sessions$experiment_mode, "")))
    resolved_dir <- resolve_log_dir(if (is.null(input$log_dir) || identical(input$log_dir, "")) default_log_dir else input$log_dir)
    folder_is_participants <- grepl("(^|/)Participants$", resolved_dir)

    if (scope == "Participant") {
      if (!folder_is_participants) {
        sessions <- sessions |> filter(experiment_mode == "participant")
      }
    } else if (scope == "Debug") {
      sessions <- sessions |> filter(experiment_mode == "debug" | experiment_mode == "")
    }

    sessions
  })

  observe({
    sessions <- session_index()
    if (nrow(sessions) == 0) {
      participant_count <- 0
      debug_count <- 0
    } else {
      session_state_norm <- tolower(trimws(coalesce(sessions$session_state, "")))
      sessions <- sessions[session_state_norm == "" | session_state_norm == "finished", , drop = FALSE]
      experiment_mode <- tolower(trimws(coalesce(sessions$experiment_mode, "")))
      participant_count <- sum(experiment_mode == "participant", na.rm = TRUE)
      debug_count <- sum(experiment_mode == "debug" | experiment_mode == "", na.rm = TRUE)
    }

    current_scope <- normalize_session_scope(input$session_scope)
    scope_choices <- setNames(
      c("Participant", "Debug", "All"),
      c(
        paste0("Participant (", participant_count, ")"),
        paste0("Debug (", debug_count, ")"),
        paste0("All (", participant_count + debug_count, ")")
      )
    )
    updateSelectInput(
      session,
      "session_scope",
      choices = scope_choices,
      selected = current_scope
    )
  })

  # Refresh the session list from disk.
  # If the folder does not exist, clear the index rather than erroring.
  refresh_sessions <- function(log_dir_override = NULL) {
    raw_log_dir <- if (!is.null(log_dir_override) && !identical(log_dir_override, "")) {
      normalizePath(log_dir_override, winslash = "/", mustWork = FALSE)
    } else if (!is.null(input$log_dir) && !identical(input$log_dir, "")) {
      normalizePath(input$log_dir, winslash = "/", mustWork = FALSE)
    } else {
      normalizePath(default_log_dir, winslash = "/", mustWork = FALSE)
    }
    log_dir <- resolve_log_dir(raw_log_dir)
    if (!dir.exists(log_dir)) {
      session_index(tibble())
      return()
    }
    if (!identical(raw_log_dir, log_dir)) {
      updateTextInput(session, "log_dir", value = log_dir)
    }
    discovered_sessions <- discover_sessions(log_dir)
    cat(sprintf("[PrismShiny] refresh_sessions raw='%s' resolved='%s' found=%d\n", raw_log_dir, log_dir, nrow(discovered_sessions)))
    session_index(discovered_sessions)
  }

  # Initial load and explicit manual refresh button.
  observeEvent(TRUE, refresh_sessions(default_log_dir), once = TRUE)
  observeEvent(input$refresh, refresh_sessions())
  session$onFlushed(function() {
    updateTextInput(session, "log_dir", value = default_log_dir)
    refresh_sessions(default_log_dir)
  }, once = TRUE)

  # Sidebar session picker generated from the current session index.
  output$session_picker <- renderUI({
    sessions <- filtered_session_index()
    if (nrow(sessions) == 0) {
      return(tags$p("No sessions found in the selected folder for the current filter."))
    }

    labels <- ifelse(
      is.na(sessions$timestamp) | sessions$timestamp == "",
      sessions$session_id,
      paste0(
        ifelse(
          !is.na(sessions$study_global_participant_id),
          paste0("P", sessions$study_global_participant_id, " | "),
          ""
        ),
        tools::toTitleCase(ifelse(is.na(sessions$experiment_mode) | sessions$experiment_mode == "", "debug", sessions$experiment_mode)),
        " | ",
        ifelse(is.na(sessions$input_mode_label) | sessions$input_mode_label == "", "unknown", sessions$input_mode_label),
        ifelse(
          !is.na(sessions$study_session_index),
          paste0(" | S", sessions$study_session_index),
          ""
        ),
        " | ",
        vapply(sessions$timestamp, format_session_timestamp, character(1))
      )
    )

    selectInput("session_id", "Session", choices = setNames(sessions$session_id, labels))
  })

  # Resolve the currently selected session row from the session index.
  current_session <- reactive({
    sessions <- filtered_session_index()
    req(nrow(sessions) > 0, input$session_id)
    sessions |> filter(session_id == input$session_id) |> slice(1)
  })

  # Load the four CSV collections for the selected session.
  current_data <- reactive({
    row <- current_session()
    load_session_data(row)
  })

  current_summary_data <- reactive({
    data <- current_data()
    summary <- coerce_summary_numeric_columns(data$summary)
    if (nrow(summary) == 0) return(summary)

    aftereffect <- summary |> filter(SummaryType == "Aftereffect")
    non_aftereffect <- summary |> filter(SummaryType != "Aftereffect")
    robust_aftereffect <- coerce_summary_numeric_columns(
      build_stable_perceived_center_aftereffects(aftereffect, data$event)
    )

    bind_rows(
      coerce_summary_numeric_columns(non_aftereffect),
      robust_aftereffect
    )
  })

  # Build the all-sessions comparison dataset once from the session index.
  comparison_data_all <- reactive({
    build_comparison_dataset(filtered_session_index())
  })

  # Session-state status box in the sidebar.
  output$session_status <- renderUI({
    row <- current_session()
    state <- ifelse(is.na(row$session_state) || row$session_state == "", "Unknown", row$session_state)
    class_name <- ifelse(tolower(state) == "finished", "status-finished", "status-aborted")
    div(class = paste("status-box", class_name), paste("Session State:", state))
  })

  # Summary cards for one selected session.
  output$summary_cards <- renderUI({
    summary <- current_summary_data()
    aftereffect <- summary |>
      filter(SummaryType == "Aftereffect") |>
      mutate(
        Magnitude = as_num(Magnitude),
        SignedDelta = as_num(SignedDelta),
        BaselineValue = as_num(BaselineValue),
        PostValue = as_num(PostValue),
        BaselineSd = as_num(BaselineSd),
        PostSd = as_num(PostSd)
      ) |>
      slice(1)

    if (nrow(aftereffect) == 0) {
      return(tagList(
        div(class = "metric-card",
            div(class = "metric-title", "Session"),
            div(class = "metric-value", "Incomplete"),
            div(class = "metric-sub", "No aftereffect row yet. This session likely did not reach Post completion.")
        )
      ))
    }

    interpretation <- compute_interpretation(aftereffect)
    variability <- compute_variability(aftereffect)
    is_exposure_task <- identical(aftereffect$TaskMode[[1]], "Exposure")

    cards <- list(
      div(class = "metric-card",
          div(class = "metric-title", "Task"),
          div(class = "metric-value", aftereffect$TaskMode[[1]]),
          div(class = "metric-sub", paste("Metric:", aftereffect$MetricName[[1]])),
          div(class = "metric-sub", paste("Effect:", format_effect_label(aftereffect)))
      ),
      div(class = "metric-card",
          div(class = "metric-title", "Baseline to Post"),
          div(class = "metric-value", sprintf("%.2f", aftereffect$SignedDelta[[1]])),
          div(class = "metric-sub", paste("Signed change in perceived middle", aftereffect$MetricUnits[[1]])),
          div(class = "metric-sub", interpretation$signed_shift_label)
      ),
      div(class = "metric-card",
          div(class = "metric-title", "Magnitude"),
          div(class = "metric-value", sprintf("%.2f", aftereffect$Magnitude[[1]])),
          div(class = "metric-sub", paste("Absolute perceived-middle shift from baseline to post", aftereffect$MetricUnits[[1]])),
          div(class = "metric-sub", interpretation$strength_label)
      ),
      div(class = "metric-card",
          div(class = "metric-title", "Consistency"),
          div(class = "metric-value", ifelse(
            is.na(variability$baseline_sd) || is.na(variability$post_sd),
            "N/A",
            sprintf("%.2f -> %.2f", variability$baseline_sd, variability$post_sd)
          )),
          div(class = "metric-sub", paste("Baseline SD to post SD", aftereffect$MetricUnits[[1]])),
          div(class = "metric-sub", variability$variability_label)
      ),
      div(class = "metric-card",
          div(class = "metric-title", "Baseline / Post"),
          div(class = "metric-value", sprintf("%.2f -> %.2f", aftereffect$BaselineValue[[1]], aftereffect$PostValue[[1]])),
          div(class = "metric-sub", "Estimated perceived middle for baseline and post")
      )
    )

    if (is_exposure_task) {
      cards <- append(cards, list(
        div(class = "metric-card",
            div(class = "metric-title", "Error Correction"),
            div(class = "metric-value", interpretation$correction_display),
            div(class = "metric-sub", interpretation$direction_label),
            div(class = "metric-sub", "Only meaningful for exposure, where the hitmarker provides online feedback."),
            div(class = "metric-sub", "Positive means the post block ended closer to the target than baseline."),
            div(class = "metric-sub", interpretation$correction_label)
        )
      ), after = 4)
    } else {
      cards <- append(cards, list(
        div(class = "metric-card",
            div(class = "metric-title", "Change Toward Target"),
            div(class = "metric-value", interpretation$correction_display),
            div(class = "metric-sub", interpretation$direction_label),
            div(class = "metric-sub", "Compares absolute baseline error with absolute post error."),
            div(class = "metric-sub", interpretation$correction_label)
        )
      ), after = 4)
    }

    do.call(tagList, cards)
  })

  # Raw summary table from Summary.csv, trimmed to the most useful columns.
  output$summary_table <- renderDT({
    summary <- current_summary_data()
    if (nrow(summary) == 0) return(datatable(tibble(Message = "No Summary.csv for this session")))

    datatable(
      summary_compact_table(summary),
      options = list(pageLength = 10, scrollX = TRUE),
      rownames = FALSE
    )
  })

  # Summary-tab plot.
  output$summary_plot <- renderPlot({
    build_summary_plot(current_summary_data())
  })

  # When a new session is selected, update the task choices for the tab dropdowns
  # based on the tasks actually present in that session's Event.csv.
  observe({
    event_data <- current_data()$event
    if (nrow(event_data) == 0 || !"TaskMode" %in% names(event_data)) {
      updateSelectInput(session, "quality_task", choices = c("Exposure"), selected = "Exposure")
      updateSelectInput(session, "spatial_task", choices = c("Exposure"), selected = "Exposure")
      updateSelectInput(session, "trajectory_task", choices = c("Exposure"), selected = "Exposure")
      return()
    }

    tasks <- sort(unique(na.omit(event_data$TaskMode)))
    if (length(tasks) == 0) tasks <- c("Exposure")
    updateSelectInput(session, "quality_task", choices = tasks, selected = if ("Exposure" %in% tasks) "Exposure" else tasks[[1]])
    updateSelectInput(session, "spatial_task", choices = tasks, selected = if ("Exposure" %in% tasks) "Exposure" else tasks[[1]])
    updateSelectInput(session, "trajectory_task", choices = tasks, selected = if ("Exposure" %in% tasks) "Exposure" else tasks[[1]])
  })

  # Populate Compare-tab filter dropdowns from the full cross-session dataset.
  observe({
    req(input$main_tabs == "Compare")

    compare_data <- comparison_data_all()
    if (nrow(compare_data) == 0 || !all(c("TaskMode", "InputModeLabel", "ConfiguredEffectMode") %in% names(compare_data))) {
      updateSelectInput(session, "compare_task", choices = c("All"), selected = "All")
      updateSelectInput(session, "compare_input_mode", choices = c("All"), selected = "All")
      updateSelectInput(session, "compare_effect", choices = c("All"), selected = "All")
      return()
    }

    tasks <- sort(unique(na.omit(compare_data$TaskMode)))
    input_modes <- sort(unique(na.omit(compare_data$InputModeLabel)))
    effects <- sort(unique(na.omit(compare_data$ConfiguredEffectMode)))

    updateSelectInput(session, "compare_task", choices = c("All", tasks), selected = "All")
    updateSelectInput(session, "compare_input_mode", choices = c("All", input_modes), selected = "All")
    updateSelectInput(session, "compare_effect", choices = c("All", effects), selected = "All")
  })

  # Apply the Compare-tab filters reactively.
  comparison_data_filtered <- reactive({
    compare_data <- comparison_data_all()
    if (nrow(compare_data) == 0) return(compare_data)

    if (isTRUE(input$compare_finished_only)) {
      compare_data <- compare_data |>
        filter(tolower(SessionState) == "finished")
    }

    if (!is.null(input$compare_task) && input$compare_task != "All") {
      compare_data <- compare_data |>
        filter(TaskMode == input$compare_task)
    }

    if (!is.null(input$compare_input_mode) && input$compare_input_mode != "All") {
      compare_data <- compare_data |>
        filter(InputModeLabel == input$compare_input_mode)
    }

    if (!is.null(input$compare_effect) && input$compare_effect != "All") {
      compare_data <- compare_data |>
        filter(ConfiguredEffectMode == input$compare_effect)
    }

    compare_data
  })

  comparison_normalized_filtered <- reactive({
    build_effect_minus_none_dataset(comparison_data_filtered())
  })

  observe({
    req(input$main_tabs == "Participant")

    compare_data <- comparison_data_all()
    if (nrow(compare_data) == 0 || !"StudyGlobalParticipantId" %in% names(compare_data)) {
      updateSelectInput(session, "participant_id", choices = c("No participants loaded"), selected = "No participants loaded")
      return()
    }

    participant_ids <- compare_data |>
      mutate(StudyGlobalParticipantId = suppressWarnings(as.integer(StudyGlobalParticipantId))) |>
      filter(!is.na(StudyGlobalParticipantId)) |>
      pull(StudyGlobalParticipantId) |>
      unique() |>
      sort()

    if (length(participant_ids) == 0) {
      updateSelectInput(session, "participant_id", choices = c("No participants loaded"), selected = "No participants loaded")
      return()
    }

    choices <- setNames(as.character(participant_ids), paste0("P", participant_ids))
    selected <- if (!is.null(input$participant_id) && input$participant_id %in% choices) input$participant_id else as.character(participant_ids[[1]])
    updateSelectInput(session, "participant_id", choices = choices, selected = selected)
  })

  participant_data <- reactive({
    compare_data <- comparison_data_all()
    if (nrow(compare_data) == 0 || is.null(input$participant_id)) return(tibble())

    selected_id <- suppressWarnings(as.integer(input$participant_id))
    if (is.na(selected_id)) return(tibble())

    compare_data |>
      mutate(
        StudyGlobalParticipantId = suppressWarnings(as.integer(StudyGlobalParticipantId)),
        StudySessionIndex = suppressWarnings(as.integer(StudySessionIndex)),
        Magnitude = as_num(Magnitude),
        SignedDelta = as_num(SignedDelta),
        BaselineValue = as_num(BaselineValue),
        PostValue = as_num(PostValue),
        BaselineSd = as_num(BaselineSd),
        PostSd = as_num(PostSd)
      ) |>
      filter(StudyGlobalParticipantId == selected_id) |>
      arrange(StudySessionIndex, TimestampDt)
  })

  output$participant_cards <- renderUI({
    pdata <- participant_data()

    if (nrow(pdata) == 0) {
      return(tagList(
        div(class = "metric-card",
            div(class = "metric-title", "Participant"),
            div(class = "metric-value", "No Data"),
            div(class = "metric-sub", "No finished aftereffect rows for this participant.")
        )
      ))
    }

    participant_id <- pdata$StudyGlobalParticipantId[[1]]
    input_mode <- paste(sort(unique(na.omit(pdata$InputModeLabel))), collapse = ", ")
    run_count <- n_distinct(pdata$SessionID)
    expected_runs <- 8L
    no_effect <- pdata |> filter(ConfiguredEffectMode == "None")
    no_effect_max <- if (nrow(no_effect) == 0) NA_real_ else max(no_effect$Magnitude, na.rm = TRUE)
    strongest <- pdata |> arrange(desc(Magnitude)) |> slice(1)

    tagList(
      div(class = "metric-card",
          div(class = "metric-title", "Participant"),
          div(class = "metric-value", paste0("P", participant_id)),
          div(class = "metric-sub", paste("Input mode:", input_mode))
      ),
      div(class = "metric-card",
          div(class = "metric-title", "Completed Modules"),
          div(class = "metric-value", paste0(run_count, "/", expected_runs)),
          div(class = "metric-sub", "Expected design is 4 effects x 2 tasks")
      ),
      div(class = "metric-card",
          div(class = "metric-title", "No-effect magnitude"),
          div(class = "metric-value", ifelse(is.finite(no_effect_max), sprintf("%.2f", no_effect_max), "N/A")),
          div(class = "metric-sub", "Large values here suggest baseline noisiness, strategy differences, or poor task compliance.")
      ),
      div(class = "metric-card",
          div(class = "metric-title", "Strongest Module"),
          div(class = "metric-value", sprintf("%.2f", strongest$Magnitude[[1]])),
          div(class = "metric-sub", paste(strongest$TaskMode[[1]], "/", strongest$ConfiguredEffectMode[[1]]))
      )
    )
  })

  output$participant_signed_plot <- renderPlot({
    build_participant_signed_plot(participant_data())
  })

  output$participant_magnitude_plot <- renderPlot({
    build_participant_magnitude_plot(participant_data())
  })

  output$participant_normalized_plot <- renderPlot({
    build_participant_normalized_plot(participant_data())
  })

  output$participant_table <- renderDT({
    pdata <- build_effect_minus_none_dataset(participant_data())

    if (nrow(pdata) == 0) {
      return(datatable(tibble(Message = "No participant rows available")))
    }

    datatable(
      pdata |>
        transmute(
          Participant = paste0("P", StudyGlobalParticipantId),
          Run = StudySessionIndex,
          Timestamp = TimestampLabel,
          InputMode = InputModeLabel,
          Task = TaskMode,
          Effect = ConfiguredEffectMode,
          Metric = MetricName,
          BaselineMean = BaselineValue,
          BaselineSd = BaselineSd,
          PostMean = PostValue,
          PostSd = PostSd,
          SignedDelta = SignedDelta,
          Magnitude = Magnitude,
          NoneMagnitude = NoneMagnitude,
          MagnitudeMinusNone = MagnitudeMinusNone,
          TowardTarget = TowardTargetDelta,
          Session = substr(SessionID, 1, 8)
        ),
      options = list(pageLength = 8, scrollX = TRUE),
      rownames = FALSE
    )
  })

  # Compare-tab metric cards.
  output$compare_cards <- renderUI({
    compare_data <- comparison_data_filtered()

    if (nrow(compare_data) == 0) {
      return(tagList(
        div(class = "metric-card",
            div(class = "metric-title", "Comparison"),
            div(class = "metric-value", "No Data"),
            div(class = "metric-sub", "No sessions match the current comparison filters.")
        )
      ))
    }

    mean_mag <- mean(compare_data$Magnitude, na.rm = TRUE)
    median_mag <- median(compare_data$Magnitude, na.rm = TRUE)
    mean_toward <- mean(compare_data$TowardTargetDelta, na.rm = TRUE)
    mode_summary <- paste(sort(unique(compare_data$InputModeLabel)), collapse = ", ")

    tagList(
      div(class = "metric-card",
          div(class = "metric-title", "Sessions"),
          div(class = "metric-value", nrow(compare_data)),
          div(class = "metric-sub", "Finished sessions included in the current comparison")
      ),
      div(class = "metric-card",
          div(class = "metric-title", "Mean Magnitude"),
          div(class = "metric-value", sprintf("%.2f", mean_mag)),
          div(class = "metric-sub", "Average baseline-to-post shift in perceived middle across filtered sessions (cm)")
      ),
      div(class = "metric-card",
          div(class = "metric-title", "Median Magnitude"),
          div(class = "metric-value", sprintf("%.2f", median_mag)),
          div(class = "metric-sub", "Median baseline-to-post shift in perceived middle across filtered sessions (cm)")
      ),
      div(class = "metric-card",
          div(class = "metric-title", "Toward Target"),
          div(class = "metric-value", sprintf("%.2f", mean_toward)),
          div(class = "metric-sub", "Average change toward target across filtered sessions"),
          div(class = "metric-sub", paste("Input modes:", mode_summary))
      )
    )
  })

  # Compare-tab plots and table.
  output$compare_plot <- renderPlot({
    build_comparison_plot(comparison_data_filtered())
  })

  output$compare_normalized_plot <- renderPlot({
    build_normalized_group_plot(comparison_normalized_filtered())
  })

  output$compare_table <- renderDT({
    compare_data <- comparison_data_filtered()

    if (nrow(compare_data) == 0) {
      return(datatable(tibble(Message = "No comparison rows for the current filters")))
    }

    datatable(
      build_effect_minus_none_dataset(compare_data) |>
        transmute(
          Timestamp = TimestampLabel,
          Participant = ifelse(is.na(StudyGlobalParticipantId), "", paste0("P", StudyGlobalParticipantId)),
          Session = substr(SessionID, 1, 8),
          Task = TaskMode,
          Effect = ConfiguredEffectMode,
          InputMode = InputModeLabel,
          State = SessionState,
          Metric = MetricName,
          Baseline = BaselineValue,
          Post = PostValue,
          SignedDelta = SignedDelta,
          Magnitude = Magnitude,
          NoneMagnitude = NoneMagnitude,
          MagnitudeMinusNone = MagnitudeMinusNone,
          ConsistencyChange = ConsistencyDelta,
          TowardTarget = TowardTargetDelta
        ),
      options = list(pageLength = 10, scrollX = TRUE),
      rownames = FALSE
    )
  })

  # Keep block dropdowns synced with the selected task for each tab.
  observeEvent(input$quality_task, {
    event_data <- current_data()$event
    blocks <- event_data |>
      filter(TaskMode == input$quality_task) |>
      pull(BlockType) |>
      na.omit() |>
      unique() |>
      sort()
    if (length(blocks) == 0) blocks <- c("Exposure")
    updateSelectInput(session, "quality_block", choices = blocks, selected = if ("Exposure" %in% blocks) "Exposure" else blocks[[1]])
  }, ignoreNULL = FALSE)

  observeEvent(input$spatial_task, {
    event_data <- current_data()$event
    blocks <- event_data |>
      filter(TaskMode == input$spatial_task) |>
      pull(BlockType) |>
      na.omit() |>
      unique() |>
      sort()
    if (length(blocks) == 0) blocks <- c("Exposure")
    updateSelectInput(session, "spatial_block", choices = blocks, selected = if ("Exposure" %in% blocks) "Exposure" else blocks[[1]])
  }, ignoreNULL = FALSE)

  observeEvent(input$trajectory_task, {
    event_data <- current_data()$event
    blocks <- event_data |>
      filter(TaskMode == input$trajectory_task) |>
      pull(BlockType) |>
      na.omit() |>
      unique() |>
      sort()
    if (length(blocks) == 0) blocks <- c("Exposure")
    updateSelectInput(session, "trajectory_block", choices = blocks, selected = if ("Exposure" %in% blocks) "Exposure" else blocks[[1]])
  }, ignoreNULL = FALSE)

  # The Trajectories tab uses "attempt" terminology for exposure and "trial" for measurement tasks.
  # This UI output switches label and choices accordingly.
  output$attempt_picker <- renderUI({
    event_data <- current_data()$event
    req(input$trajectory_task, input$trajectory_block)
    attempts <- if (input$trajectory_task == "Exposure") {
      event_data |>
        filter(TaskMode == input$trajectory_task, BlockType == input$trajectory_block, Event %in% c("Pointer Shoot", "Mole Hit", "Mole Missed")) |>
        mutate(AttemptIndex = as_num(AttemptIndex)) |>
        filter(!is.na(AttemptIndex)) |>
        pull(AttemptIndex) |>
        unique() |>
        sort()
    } else {
      event_data |>
        filter(TaskMode == input$trajectory_task, BlockType == input$trajectory_block, Event == "Trial Accepted") |>
        mutate(TrialIndex = as_num(TrialIndex)) |>
        filter(!is.na(TrialIndex)) |>
        pull(TrialIndex) |>
        unique() |>
        sort()
    }

    if (length(attempts) == 0) {
      return(selectInput("trajectory_attempt", if (input$trajectory_task == "Exposure") "Highlighted Attempt" else "Highlighted Trial", choices = c(1), selected = 1))
    }

    selectInput("trajectory_attempt", if (input$trajectory_task == "Exposure") "Highlighted Attempt" else "Highlighted Trial", choices = attempts, selected = attempts[[1]])
  })

  # Exposure gets a time-window slider because its trajectory view is time-based.
  # Non-exposure tasks instead show a small explanatory note.
  output$trajectory_window_ui <- renderUI({
    if (input$trajectory_task == "Exposure") {
      return(sliderInput("trajectory_window", "Seconds Before Confirm", min = 0.25, max = 3, value = 1, step = 0.25))
    }

    tags$div(
      class = "metric-card",
      div(class = "metric-title", "View Mode"),
      div(class = "metric-sub", "For non-exposure tasks this tab shows trial-by-trial response values, not physical movement trajectories.")
    )
  })

  # Context-sensitive help text for the Trajectories tab.
  output$trajectory_help <- renderUI({
    if (input$trajectory_task == "Exposure") {
      return(tags$p("This tab focuses on approach behavior rather than raw controller wandering. It shows how the pointer converged on the target plane before each confirmation."))
    }

    tags$p("For OpenLoop and LineBisection, this tab is a trial-by-trial response overview. Each dot is one accepted trial, and the highlighted point is the selected trial. It is not a movement path through space.")
  })

  # Quality-tab cards.
  output$quality_cards <- renderUI({
    req(input$quality_task, input$quality_block)
    quality_trials <- extract_quality_trials(current_data()$event, input$quality_task, input$quality_block)
    metrics <- compute_quality_metrics(quality_trials, input$quality_task)
    units_label <- quality_units_label(input$quality_task)
    error_label <- quality_error_label(input$quality_task)

    trend_value <- ifelse(
      is.na(metrics$slope),
      "N/A",
      ifelse(
        identical(units_label, "m"),
        ifelse(
          abs(metrics$slope * 100) < 0.01,
          sprintf("%.2f mm per trial", metrics$slope * 1000),
          sprintf("%.3f cm per trial", metrics$slope * 100)
        ),
        sprintf("%.3f %s per trial", metrics$slope, units_label)
      )
    )

    if (nrow(quality_trials) == 0) {
      return(tagList(
        div(class = "metric-card",
            div(class = "metric-title", "Quality"),
            div(class = "metric-value", "No Data"),
            div(class = "metric-sub", "No valid trials found for this task/block.")
        )
      ))
    }

    cards <- list(
      div(class = "metric-card",
          div(class = "metric-title", "Trials"),
          div(class = "metric-value", metrics$trial_count),
          div(class = "metric-sub", "Trials included in this quality overview")
      ),
      div(class = "metric-card",
          div(class = "metric-title", "Absolute Error"),
          div(class = "metric-value", sprintf("%.3f", metrics$mean_abs_error)),
          div(class = "metric-sub", paste("Mean absolute", tolower(error_label), "across trials")),
          div(class = "metric-sub", paste("Units:", units_label))
      ),
      div(class = "metric-card",
          div(class = "metric-title", "Unusually Large Errors"),
          div(class = "metric-value", metrics$outlier_count),
          div(class = "metric-sub", "Trials that were much farther from the target than the rest of this block"),
          div(class = "metric-sub", paste("Flagged above", sprintf("%.3f %s", metrics$outlier_threshold, units_label)))
      ),
      div(class = "metric-card",
          div(class = "metric-title", "Change Over Trials"),
          div(class = "metric-value", trend_value),
          div(class = "metric-sub", "Shows whether errors got better or worse as the block went on"),
          div(class = "metric-sub", metrics$trend_label)
      )
    )

    if (input$quality_task == "Exposure" && !is.na(metrics$hit_rate)) {
      cards <- append(cards, list(
        div(class = "metric-card",
            div(class = "metric-title", "Hit Rate"),
            div(class = "metric-value", sprintf("%.1f%%", metrics$hit_rate * 100)),
            div(class = "metric-sub", "Confirmed hits divided by all exposure attempts")
        )
      ))
    }

    if (input$quality_task %in% c("OpenLoop", "LineBisection") && !is.na(metrics$stable_center)) {
      cards <- append(cards, list(
        div(class = "metric-card",
            div(class = "metric-title", "Stable Perceived Center"),
            div(class = "metric-value", sprintf("%.2f cm", metrics$stable_center)),
            div(class = "metric-sub", "Cluster center of accepted signed responses"),
            div(class = "metric-sub", sprintf("Weighted spread: %.2f cm", metrics$stable_spread))
        )
      ))
    }

    do.call(tagList, cards)
  })

  # Quality-tab plot and table.
  output$quality_plot <- renderPlot({
    req(input$quality_task, input$quality_block)
    quality_trials <- extract_quality_trials(current_data()$event, input$quality_task, input$quality_block)
    build_quality_plot(quality_trials, input$quality_task, input$quality_block)
  })

  output$quality_table <- renderDT({
    req(input$quality_task, input$quality_block)
    quality_trials <- extract_quality_trials(current_data()$event, input$quality_task, input$quality_block)

    if (nrow(quality_trials) == 0) {
      return(datatable(tibble(Message = "No quality rows for this task/block")))
    }

    datatable(
      quality_trials |>
        rename(
          `Trial / Attempt` = TrialIndex,
          `Distance From Target` = absolute_error,
          Outcome = outcome
        ) |>
        select(`Trial / Attempt`, `Distance From Target`, Outcome),
      options = list(pageLength = 10, scrollX = TRUE),
      rownames = FALSE
    )
  })

  # Spatial plot for the selected task/block.
  output$spatial_plot <- renderPlot({
    build_spatial_plot(current_data()$event, input$spatial_task, input$spatial_block)
  })

  # Main trajectories plot.
  output$trajectory_plot <- renderPlot({
    req(input$trajectory_attempt)
    build_trajectory_overview_plot(
      current_data()$sample,
      current_data()$event,
      selected_task = input$trajectory_task,
      selected_block = input$trajectory_block,
      selected_attempt = as_num(input$trajectory_attempt),
      time_window = input$trajectory_window,
      overlap_blocks = isTRUE(input$trajectory_overlap)
    )
  })

  # Phase plot that breaks a highlighted attempt into acceleration,
  # deceleration, and hover so the user can inspect how the movement unfolded.
  output$trajectory_phase_plot <- renderPlotly({
    req(input$trajectory_attempt)
    phase_time_window <- if (is.null(input$trajectory_window)) 1 else input$trajectory_window

    build_phase_plot(
      current_data()$meta,
      current_data()$sample,
      current_data()$event,
      selected_task = input$trajectory_task,
      selected_block = input$trajectory_block,
      selected_attempt = as_num(input$trajectory_attempt),
      time_window = phase_time_window
    )
  })

  # Lower trajectories plot showing final error progression.
  output$trajectory_error_plot <- renderPlot({
    build_attempt_error_plot(
      current_data()$event,
      selected_task = input$trajectory_task,
      selected_block = input$trajectory_block,
      overlap_blocks = isTRUE(input$trajectory_overlap)
    )
  })
}

# Launch the app by combining the UI definition and the server logic.
shinyApp(ui, server)
