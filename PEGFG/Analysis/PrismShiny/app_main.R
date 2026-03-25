library(shiny)
library(DT)
library(dplyr)
library(ggplot2)
library(plotly)
library(readr)
library(stringr)
library(purrr)
library(tidyr)

app_dir <- if (exists('.prism_app_dir', inherits = TRUE)) get('.prism_app_dir', inherits = TRUE) else normalizePath(getwd(), winslash = '/', mustWork = FALSE)
repo_root_dir <- normalizePath(file.path(app_dir, "..", "..", ".."), winslash = "/", mustWork = FALSE)
default_log_dir <- normalizePath(file.path(repo_root_dir, "Assets", "PrismLogging"), winslash = "/", mustWork = FALSE)
synthetic_log_dir <- normalizePath(file.path(default_log_dir, "SyntheticShowcase"), winslash = "/", mustWork = FALSE)

.csv_cache <- new.env(parent = emptyenv())

safe_read_csv <- function(path, n_max = Inf) {
  if (length(path) == 0 || is.na(path) || path == "" || !file.exists(path)) return(tibble())
  path <- normalizePath(path, winslash = "/", mustWork = FALSE)
  info <- file.info(path)
  if (is.na(info$mtime[[1]])) return(tibble())

  cache_key <- paste(path, info$mtime[[1]], info$size[[1]], n_max, sep = "::")
  if (exists(cache_key, envir = .csv_cache, inherits = FALSE)) {
    return(get(cache_key, envir = .csv_cache, inherits = FALSE))
  }

  data <- tryCatch({
    suppressMessages(read_delim(path, delim = ";", show_col_types = FALSE, progress = FALSE, n_max = n_max))
  }, error = function(e) {
    tibble()
  })
  if (is.finite(n_max)) {
    data <- data |> slice_head(n = n_max)
  }
  assign(cache_key, data, envir = .csv_cache)
  data
}

clean_log_df <- function(df) {
  if (nrow(df) == 0) return(df)
  df |>
    mutate(across(where(is.character), ~na_if(.x, "NULL")))
}

has_cols <- function(df, cols) {
  all(cols %in% names(df))
}

read_session_id <- function(path) {
  data <- clean_log_df(safe_read_csv(path, n_max = 1))
  if (!"SessionID" %in% names(data) || nrow(data) == 0) return(NA_character_)
  as.character(data$SessionID[[1]])
}

read_session_timestamp <- function(path) {
  data <- clean_log_df(safe_read_csv(path, n_max = 1))
  if (!"Timestamp" %in% names(data) || nrow(data) == 0) return(NA_character_)
  as.character(data$Timestamp[[1]])
}

read_session_state <- function(path) {
  data <- clean_log_df(safe_read_csv(path, n_max = 1))
  if (!"SessionState" %in% names(data) || nrow(data) == 0) return(NA_character_)
  as.character(data$SessionState[[1]])
}

read_meta_value <- function(path, key) {
  data <- clean_log_df(safe_read_csv(path, n_max = 1))
  if (!(key %in% names(data)) || nrow(data) == 0) return(NA_character_)
  as.character(data[[key]][[1]])
}

as_num <- function(x) suppressWarnings(as.numeric(x))
as_time <- function(x) suppressWarnings(as.POSIXct(x, tz = "UTC"))

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

discover_sessions <- function(log_dir) {
  csv_files <- list.files(log_dir, pattern = "_(Meta|Event|Sample|Summary)\\.csv$", full.names = TRUE)
  csv_files <- csv_files[!grepl("\\.meta$", csv_files, ignore.case = TRUE)]

  if (length(csv_files) == 0) return(tibble())

  file_index <- tibble(path = csv_files) |>
    mutate(
      file_name = basename(path),
      file_type = str_match(file_name, "_(Meta|Event|Sample|Summary)\\.csv$")[, 2],
      session_id = map_chr(path, read_session_id),
      timestamp = map_chr(path, read_session_timestamp),
      session_state = map_chr(path, read_session_state),
      input_mode_label = map2_chr(path, file_type, ~ if (.y == "Meta") { read_meta_value(.x, "InputModeLabel") } else { NA_character_ })
    ) |>
    filter(!is.na(session_id), session_id != "", !is.na(file_type))

  if (nrow(file_index) == 0) return(tibble())

  file_index |>
    group_by(session_id) |>
    summarise(
      timestamp = {
        vals <- na.omit(timestamp)
        if (length(vals) == 0) NA_character_ else vals[[1]]
      },
      session_state = {
        vals <- na.omit(session_state)
        if (length(vals) == 0) NA_character_ else vals[[1]]
      },
      input_mode_label = {
        vals <- na.omit(input_mode_label)
        if (length(vals) == 0) NA_character_ else vals[[1]]
      },
      Meta = {
        vals <- path[file_type == "Meta"]
        if (length(vals) == 0) NA_character_ else vals[[1]]
      },
      Event = {
        vals <- path[file_type == "Event"]
        if (length(vals) == 0) NA_character_ else vals[[1]]
      },
      Sample = {
        vals <- path[file_type == "Sample"]
        if (length(vals) == 0) NA_character_ else vals[[1]]
      },
      Summary = {
        vals <- path[file_type == "Summary"]
        if (length(vals) == 0) NA_character_ else vals[[1]]
      },
      .groups = "drop"
    ) |>
    filter(!is.na(Meta), Meta != "") |>
    arrange(desc(timestamp))
}

load_session_part <- function(session_row, part_name) {
  if (nrow(session_row) == 0 || !(part_name %in% names(session_row))) return(tibble())
  clean_log_df(safe_read_csv(session_row[[part_name]][[1]]))
}

build_summary_plot <- function(summary_data) {
  if (!has_cols(summary_data, c("SummaryType", "Magnitude", "TaskMode", "MetricName"))) {
    return(
      ggplot() +
        annotate("text", x = 1, y = 1, label = "No summary rows available for plotting") +
        theme_void()
    )
  }

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
      title = "Aftereffect Magnitude",
      x = "Task",
      y = "Size of change from baseline to post"
    ) +
    theme_minimal(base_size = 13) +
    theme(
      legend.position = "none",
      plot.title = element_text(face = "bold"),
      axis.title.x = element_text(margin = margin(t = 10)),
      plot.margin = margin(12, 24, 12, 12)
    )
}

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
      ifelse(
        correction > 0,
        sprintf("Average distance from target decreased by %.2f", abs(correction)),
        ifelse(
          correction < 0,
          sprintf("Average distance from target increased by %.2f", abs(correction)),
          "Average distance from target stayed about the same"
        )
      )
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

  spawn_events <- event_subset |>
    filter(Event == "Mole Spawned")

  targets <- spawn_events |>
    transmute(
      kind = "Target",
      x = as_num(MolePositionWorldX),
      y = as_num(MolePositionWorldY),
      radius_m = safe_num_col(spawn_events, "TargetRadiusMeters"),
      label = case_when(
        as_num(MolePositionWorldX) < -0.1 ~ "Left",
        as_num(MolePositionWorldX) > 0.1 ~ "Right",
        TRUE ~ "Center"
      ),
      hover_text = paste0(
        "Target: ", case_when(
          as_num(MolePositionWorldX) < -0.1 ~ "Left",
          as_num(MolePositionWorldX) > 0.1 ~ "Right",
          TRUE ~ "Center"
        ),
        "<br>Target radius: ", sprintf("%.1f", dplyr::coalesce(safe_num_col(spawn_events, "TargetRadiusMeters"), 0.112) * 100), " cm"
      )
    ) |>
    filter(!is.na(x), !is.na(y)) |>
    distinct(x, y, label, .keep_all = TRUE)

  hit_events <- event_subset |>
    filter(Event == "Mole Hit")

  hits <- hit_events |>
    filter(Event == "Mole Hit") |>
    transmute(
      kind = "Hit",
      x = as_num(HitPositionWorldX),
      y = as_num(HitPositionWorldY),
      distance_m = as_num(HitDistanceMeters),
      hover_text = paste0(
        "Outcome: Hit",
        "<br>Distance from target center: ", sprintf("%.1f", as_num(HitDistanceMeters) * 100), " cm"
      )
    ) |>
    filter(!is.na(x), !is.na(y))

  miss_events <- event_subset |>
    filter(Event == "Mole Missed")

  miss_threshold_m <- dplyr::coalesce(safe_num_col(miss_events, "TargetRadiusMeters"), 0.112)

  misses <- miss_events |>
    transmute(
      kind = "Miss",
      x = as_num(HitPositionWorldX),
      y = as_num(HitPositionWorldY),
      distance_m = as_num(HitDistanceMeters),
      threshold_m = miss_threshold_m,
      hover_text = paste0(
        "Outcome: Miss",
        "<br>Distance from target center: ", sprintf("%.1f", as_num(HitDistanceMeters) * 100), " cm",
        "<br>Outside hit radius of ", sprintf("%.1f", miss_threshold_m * 100), " cm"
      )
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

  target_circles <- purrr::pmap_dfr(
    list(targets$x, targets$y, dplyr::coalesce(targets$radius_m, 0.112), targets$label),
    function(cx, cy, radius, label) {
      theta <- seq(0, 2 * pi, length.out = 80)
      tibble(
        x = cx + radius * cos(theta),
        y = cy + radius * sin(theta),
        label = label
      )
    }
  )

  ggplot() +
    geom_path(data = target_circles, aes(x = x, y = y, group = label), color = "#94a3b8", linewidth = 0.8, alpha = 0.9) +
    geom_point(data = targets, aes(x = x, y = y, text = hover_text), shape = 4, size = 5, stroke = 1.6, color = "#1f2937") +
    geom_text(data = targets, aes(x = x, y = y, label = label), nudge_y = 0.03, size = 4.2, color = "#1f2937") +
    geom_point(data = hits, aes(x = x, y = y, text = hover_text), size = 3, alpha = 0.8, color = "#0ea5a4") +
    geom_point(data = misses, aes(x = x, y = y, text = hover_text), size = 3, alpha = 0.8, color = "#dc2626") +
    coord_equal() +
    labs(
      title = plot_title,
      subtitle = "Target outlines, target centers, hits, and misses in board/world space",
      x = "World X",
      y = "World Y"
    ) +
    theme_minimal(base_size = 13)
}

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

  trial_rows <- trial_rows |>
    mutate(
      x_plot = if (overlap_blocks) {
        TrialIndex + case_when(
          BlockType == "Baseline" ~ -0.12,
          BlockType == "Post" ~ 0.12,
          TRUE ~ 0
        )
      } else {
        TrialIndex
      }
    )

  highlighted <- trial_rows |>
    filter(BlockType == selected_block, TrialIndex == selected_trial) |>
    slice(1)

  metric_label <- ifelse(
    selected_task == "LineBisection",
    "Signed deviation from true midpoint (cm)",
    "Signed deviation from target center (cm)"
  )

  subtitle_text <- if (overlap_blocks) {
    "Each dot is one accepted trial. Baseline dots are shifted slightly left and Post dots slightly right so the same trial index does not stack visually. The blue point is the selected trial."
  } else {
    "Each dot is one accepted trial in this block. Dashed line is the ideal center. The blue point is the selected trial."
  }

  plot_title <- if (overlap_blocks) {
    paste(selected_task, "Signed Response by Trial")
  } else {
    paste(selected_task, selected_block, "Signed Response by Trial")
  }
  legend_position <- if (overlap_blocks) "top" else "none"

  ggplot(trial_rows, aes(x = x_plot, y = signed_value, color = BlockType, group = BlockType)) +
    geom_hline(yintercept = 0, color = "#111827", linetype = "dashed") +
    geom_line(linewidth = 0.8, alpha = 0.7) +
    geom_point(size = 2.7, alpha = 0.8) +
    geom_point(data = highlighted, aes(x = x_plot, y = signed_value), inherit.aes = FALSE, color = "#2563eb", size = 4) +
    scale_color_manual(values = c("Baseline" = "#94a3b8", "Post" = "#0ea5a4", "Exposure" = "#dc2626")) +
    scale_x_continuous(
      breaks = sort(unique(trial_rows$TrialIndex)),
      labels = sort(unique(trial_rows$TrialIndex))
    ) +
    labs(
      title = plot_title,
      subtitle = subtitle_text,
      x = "Trial index",
      y = metric_label,
      color = NULL
    ) +
    theme_minimal(base_size = 13) +
    theme(legend.position = legend_position)
}

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

    attempts <- attempts |>
      mutate(
        x_plot = if (overlap_blocks) {
          AttemptIndex + case_when(
            BlockType == "Baseline" ~ -0.12,
            BlockType == "Post" ~ 0.12,
            TRUE ~ 0
          )
        } else {
          AttemptIndex
        }
      )

    metric_label <- ifelse(
      selected_task == "LineBisection",
      "Absolute deviation from midpoint (cm)",
      "Absolute deviation from target center (cm)"
    )

    plot_title <- if (overlap_blocks) {
      paste(selected_task, "Absolute Error by Trial")
    } else {
      paste(selected_task, selected_block, "Absolute Error by Trial")
    }
    plot_subtitle <- if (overlap_blocks) {
      "Each dot is one accepted trial. Baseline and Post are shifted slightly apart at each trial index so they can be compared directly. Lower is better."
    } else {
      "Each dot is one accepted trial in this block. Lower is better."
    }
    legend_position <- if (overlap_blocks) {
      "top"
    } else {
      "none"
    }

    return(
      ggplot(attempts, aes(x = x_plot, y = final_error, color = BlockType, group = BlockType)) +
        geom_line(linewidth = 0.7, alpha = 0.7) +
        geom_point(size = 3) +
        scale_color_manual(values = c("Baseline" = "#94a3b8", "Post" = "#0ea5a4", "Exposure" = "#dc2626")) +
        scale_x_continuous(
          breaks = sort(unique(attempts$AttemptIndex)),
          labels = sort(unique(attempts$AttemptIndex))
        ) +
        labs(
          title = plot_title,
          subtitle = plot_subtitle,
          x = "Trial index",
          y = metric_label,
          color = NULL
        ) +
        theme_minimal(base_size = 13) +
        theme(legend.position = legend_position)
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
    target_radius <- meta_num(meta_data, "ExposureHitRadiusMeters", 0.112)

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
    hover_start_ms = hover_start_ms
    ,
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

  phase_plot <- plot_ly(
    data = profile$phase_df,
    x = profile$phase_df$DurationMs,
    y = rep("Phases", nrow(profile$phase_df)),
    type = "bar",
    orientation = "h",
    color = profile$phase_df$Phase,
    colors = phase_colors,
    text = paste0(profile$phase_df$Phase, "<br>", round(profile$phase_df$DurationMs), " ms"),
    textposition = "inside",
    hovertemplate = "%{text}<br>Duration: %{x:.0f} ms<extra></extra>"
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

  list(
    mean_abs_error = mean(y, na.rm = TRUE),
    median_abs_error = median(y, na.rm = TRUE),
    outlier_count = outlier_count,
    outlier_threshold = outlier_threshold,
    slope = slope,
    trend_label = trend_label,
    hit_rate = hit_rate,
    trial_count = nrow(quality_trials)
  )
}

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

build_quality_plot <- function(quality_trials, selected_task, selected_block) {
  if (nrow(quality_trials) == 0) {
    return(
      ggplot() +
        annotate("text", x = 1, y = 1, label = "No quality data found for this task/block") +
        theme_void()
    )
  }

  metrics <- compute_quality_metrics(quality_trials, selected_task)
  units_label <- quality_units_label(selected_task)
  error_label <- quality_error_label(selected_task)
  x_label <- if (selected_task == "Exposure") "Attempt number" else "Trial number"
  plot_title <- if (selected_task == selected_block) paste(selected_task, "Quality Overview") else paste(selected_task, selected_block, "Quality Overview")
  outcome_text <- if ("outcome" %in% names(quality_trials)) paste0("<br>Outcome: ", quality_trials$outcome) else rep("", nrow(quality_trials))

  quality_trials <- quality_trials |>
    mutate(
      hover_text = paste0(
        x_label, ": ", TrialIndex,
        "<br>", error_label, ": ", sprintf("%.3f", absolute_error), " ", units_label,
        outcome_text
      )
    )

  ggplot(quality_trials, aes(x = TrialIndex, y = absolute_error)) +
    geom_line(color = "#94a3b8", linewidth = 0.8) +
    geom_point(aes(color = outcome, text = hover_text), size = 2.8) +
    geom_hline(yintercept = metrics$outlier_threshold, linetype = "dashed", color = "#dc2626") +
    geom_smooth(method = "lm", formula = y ~ x, se = FALSE, color = "#1d4ed8", linewidth = 0.8) +
    annotate(
      "text",
      x = max(quality_trials$TrialIndex, na.rm = TRUE),
      y = metrics$outlier_threshold,
      label = paste0("Large-error guide: ", sprintf("%.3f %s", metrics$outlier_threshold, units_label)),
      hjust = 1,
      vjust = -0.5,
      color = "#dc2626",
      size = 3.6
    ) +
    labs(
      title = plot_title,
      subtitle = "Points show each trial. Red dashed line marks the large-error guide for this block. Blue line shows the overall trend across the block.",
      x = x_label,
      y = paste(error_label, "(", units_label, ")"),
      color = NULL
    ) +
    theme_minimal(base_size = 13) +
    theme(legend.position = "top")
}

summary_compact_table <- function(summary_data) {
  required_cols <- c("SummaryType", "TaskMode", "BlockType", "MetricName", "MetricUnits", "BaselineValue", "BaselineSd", "PostValue", "PostSd", "SignedDelta", "Magnitude", "NormalizedMagnitude", "TrialCount", "ConfiguredEffectMode")
  if (!has_cols(summary_data, required_cols)) {
    return(tibble(Message = "Summary.csv is missing columns needed for the summary table"))
  }

  summary_data |>
    filter(SummaryType %in% c("Aftereffect", "BlockMetric")) |>
    select(TaskMode, BlockType, SummaryType, MetricName, MetricUnits, BaselineValue, BaselineSd, PostValue, PostSd, SignedDelta, Magnitude, NormalizedMagnitude, TrialCount, ConfiguredEffectMode)
}

build_comparison_dataset <- function(session_rows) {
  if (nrow(session_rows) == 0) return(tibble())

  compare_rows <- purrr::map_dfr(seq_len(nrow(session_rows)), function(i) {
    row <- session_rows[i, ]
    summary_data <- clean_log_df(safe_read_csv(row$Summary))
    if (nrow(summary_data) == 0 || !has_cols(summary_data, c("SummaryType", "Magnitude", "SignedDelta", "BaselineValue", "PostValue", "BaselineSd", "PostSd", "MetricName", "TaskMode", "ConfiguredEffectMode"))) {
      return(tibble())
    }

    summary_data |>
      filter(SummaryType == "Aftereffect") |>
      mutate(
        SessionID = row$session_id[[1]],
        TimestampLabel = row$timestamp[[1]],
        TimestampDt = as_time(row$timestamp[[1]]),
        SessionState = row$session_state[[1]],
        InputModeLabel = row$input_mode_label[[1]],
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
  })

  if (!("TimestampDt" %in% names(compare_rows)) || nrow(compare_rows) == 0) {
    return(compare_rows)
  }

  compare_rows |>
    arrange(TimestampDt)
}

build_comparison_plot <- function(compare_data) {
  if (nrow(compare_data) == 0) {
    return(
      ggplot() +
        annotate("text", x = 1, y = 1, label = "No aftereffect sessions match the current filters") +
        theme_void()
    )
  }

  grouped <- compare_data |>
    mutate(
      EffectGroup = ifelse(is.na(ConfiguredEffectMode) | ConfiguredEffectMode == "", "Unknown", ConfiguredEffectMode)
    ) |>
    group_by(TaskMode, EffectGroup, InputModeLabel) |>
    mutate(GroupCount = n()) |>
    ungroup()

  grouped <- grouped |>
    mutate(
      hover_text = paste0(
        "Task: ", TaskMode,
        "<br>Effect: ", EffectGroup,
        "<br>Input mode: ", InputModeLabel,
        "<br>Aftereffect magnitude: ", sprintf("%.3f", Magnitude),
        "<br>Session: ", RunLabel
      )
    )

  ggplot(grouped, aes(x = EffectGroup, y = Magnitude, color = InputModeLabel, fill = InputModeLabel)) +
    geom_boxplot(
      aes(group = interaction(EffectGroup, InputModeLabel)),
      position = position_dodge2(width = 0.65, preserve = "single"),
      width = 0.55,
      alpha = 0.18,
      outlier.shape = NA,
      linewidth = 0.8
    ) +
    geom_jitter(
      aes(
        group = InputModeLabel,
        text = hover_text
      ),
      size = 2.6,
      alpha = 0.85,
      position = position_jitterdodge(jitter.width = 0.08, dodge.width = 0.65)
    ) +
    facet_wrap(~TaskMode, scales = "free_y") +
    labs(
      title = "Main Comparison: Aftereffect Magnitude by Condition",
      subtitle = "Primary comparison view. Compare controller vs embodied within each effect, separated by task. Higher values mean larger changes from baseline to post.",
      x = "Configured effect",
      y = "Size of change from baseline to post",
      color = "Input mode",
      fill = "Input mode"
    ) +
    theme_minimal(base_size = 13) +
    theme(
      legend.position = "top",
      strip.text = element_text(size = 16, face = "bold")
    )
}

build_grouped_comparison_plot <- function(compare_data) {
  if (nrow(compare_data) == 0) {
    return(
      ggplot() +
        annotate("text", x = 1, y = 1, label = "No grouped comparison data available") +
        theme_void()
    )
  }

  grouped <- compare_data |>
    mutate(EffectGroup = ifelse(is.na(ConfiguredEffectMode) | ConfiguredEffectMode == "", "Unknown", ConfiguredEffectMode)) |>
    group_by(TaskMode, EffectGroup, InputModeLabel) |>
    summarise(
      MeanMagnitude = mean(Magnitude, na.rm = TRUE),
      MedianMagnitude = median(Magnitude, na.rm = TRUE),
      SessionCount = n(),
      .groups = "drop"
    )

  grouped <- grouped |>
    mutate(
      hover_text = paste0(
        "Task: ", TaskMode,
        "<br>Effect: ", EffectGroup,
        "<br>Input mode: ", InputModeLabel,
        "<br>Average magnitude: ", sprintf("%.3f", MeanMagnitude),
        "<br>Median magnitude: ", sprintf("%.3f", MedianMagnitude),
        "<br>Sessions: ", SessionCount
      )
    )

  ggplot(grouped, aes(x = EffectGroup, y = MeanMagnitude, fill = InputModeLabel)) +
    geom_col(
      aes(text = hover_text),
      position = position_dodge(width = 0.7), width = 0.62, alpha = 0.9
    ) +
    geom_text(
      aes(label = sprintf("n=%d", SessionCount)),
      position = position_dodge(width = 0.7),
      vjust = -0.5,
      size = 3.6
    ) +
    facet_wrap(~TaskMode, scales = "free_y") +
    labs(
      title = "Average Aftereffect by Task, Effect, and Input Mode",
      subtitle = "Simple average view. Higher bars mean larger average changes from baseline to post.",
      x = "Configured effect",
      y = "Average size of change from baseline to post",
      fill = "Input mode"
    ) +
    theme_minimal(base_size = 13)
}

build_comparison_stats_table <- function(compare_data) {
  if (nrow(compare_data) == 0) {
    return(tibble(Message = "No comparison stats available for the current filters"))
  }

  compare_data |>
    mutate(EffectGroup = ifelse(is.na(ConfiguredEffectMode) | ConfiguredEffectMode == "", "Unknown", ConfiguredEffectMode)) |>
    group_by(TaskMode, EffectGroup, InputModeLabel) |>
    summarise(
      Sessions = n(),
      Mean = mean(Magnitude, na.rm = TRUE),
      Median = median(Magnitude, na.rm = TRUE),
      Q1 = as.numeric(quantile(Magnitude, 0.25, na.rm = TRUE)),
      Q3 = as.numeric(quantile(Magnitude, 0.75, na.rm = TRUE)),
      Min = min(Magnitude, na.rm = TRUE),
      Max = max(Magnitude, na.rm = TRUE),
      .groups = "drop"
    ) |>
    rename(
      Task = TaskMode,
      Effect = EffectGroup,
      `Input Mode` = InputModeLabel,
      n = Sessions
    ) |>
    mutate(
      Mean = round(Mean, 3),
      Median = round(Median, 3),
      Q1 = round(Q1, 3),
      Q3 = round(Q3, 3),
      Min = round(Min, 3),
      Max = round(Max, 3)
    )
}

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
    "))
  ),
  sidebarLayout(
    sidebarPanel(
      radioButtons(
        "data_source",
        "Data Source",
        choices = c("Real" = "real", "Synthetic" = "synthetic"),
        selected = "real",
        inline = TRUE
      ),
      textInput("log_dir", "Log Folder", value = default_log_dir),
      actionButton("refresh", "Refresh Sessions"),
      br(), br(),
      uiOutput("session_picker"),
      br(),
      uiOutput("session_status")
    ),
    mainPanel(
      tabsetPanel(
        tabPanel(
          "Summary",
          fluidRow(
            column(4, uiOutput("summary_cards")),
            column(8, plotlyOutput("summary_plot", height = "360px"))
          ),
          h3("Session Summary Table"),
          DTOutput("summary_table")
        ),
        tabPanel(
          "Compare",
          fluidRow(
            column(3, selectInput("compare_task", "Task", choices = c("All"))),
            column(3, selectInput("compare_input_mode", "Input Mode", choices = c("All"))),
            column(3, selectInput("compare_effect", "Effect", choices = c("All"))),
            column(3, checkboxInput("compare_finished_only", "Finished sessions only", value = TRUE))
          ),
          fluidRow(
            column(
              12,
              radioButtons(
                "compare_view",
                "Compare View",
                choices = c("Distribution" = "distribution", "Average" = "average"),
                selected = "distribution",
                inline = TRUE
              )
            )
          ),
          fluidRow(
            uiOutput("compare_cards")
          ),
          plotlyOutput("compare_plot", height = "430px"),
          h3("Condition Statistics"),
          DTOutput("compare_stats_table"),
          h3("Comparison Table"),
          DTOutput("compare_table")
        ),
        tabPanel(
          "Quality",
          fluidRow(
            column(3, selectInput("quality_task", "Task", choices = c("Exposure", "OpenLoop"))),
            column(3, selectInput("quality_block", "Block", choices = c("Exposure", "Baseline", "Post")))
          ),
          fluidRow(
            column(4, uiOutput("quality_cards")),
            column(8, plotlyOutput("quality_plot", height = "420px"))
          ),
          DTOutput("quality_table")
        ),
        tabPanel(
          "Spatial",
          fluidRow(
            column(3, selectInput("spatial_task", "Task", choices = c("Exposure", "OpenLoop"))),
            column(3, selectInput("spatial_block", "Block", choices = c("Exposure", "Baseline", "Post")))
          ),
          plotlyOutput("spatial_plot", height = "620px")
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
          plotlyOutput("trajectory_plot", height = "520px"),
          plotlyOutput("trajectory_phase_plot", height = "560px"),
          plotlyOutput("trajectory_error_plot", height = "320px")
        )
      )
    )
  )
)

server <- function(input, output, session) {
  session_index <- reactiveVal(tibble())
  active_log_dir <- reactiveVal(default_log_dir)

  observeEvent(input$data_source, {
    selected_path <- switch(
      input$data_source,
      real = default_log_dir,
      synthetic = synthetic_log_dir,
      default_log_dir
    )

    active_log_dir(selected_path)
    session_index(tibble())
    updateTextInput(session, "log_dir", value = selected_path)
    if (dir.exists(selected_path)) {
      session_index(discover_sessions(selected_path))
    } else {
      session_index(tibble())
    }
  }, ignoreInit = TRUE)

  refresh_sessions <- function() {
    raw_log_dir <- if (!is.null(input$log_dir) && nzchar(input$log_dir)) input$log_dir else active_log_dir()
    log_dir <- normalizePath(raw_log_dir, winslash = "/", mustWork = FALSE)
    active_log_dir(log_dir)
    if (!dir.exists(log_dir)) {
      session_index(tibble())
      return()
    }
    session_index(discover_sessions(log_dir))
  }

  observeEvent(TRUE, refresh_sessions(), once = TRUE)
  observeEvent(input$refresh, refresh_sessions())

  output$session_picker <- renderUI({
    sessions <- session_index()
    if (nrow(sessions) == 0) {
      return(tags$p("No sessions found in the selected folder."))
    }

    labels <- ifelse(
      is.na(sessions$timestamp) | sessions$timestamp == "",
      sessions$session_id,
      paste0(
        sessions$timestamp,
        " | ",
        ifelse(is.na(sessions$input_mode_label) | sessions$input_mode_label == "", "unknown", sessions$input_mode_label),
        " | ",
        substr(sessions$session_id, 1, 8)
      )
    )

    selected_session <- isolate(input$session_id)
    if (is.null(selected_session) || !(selected_session %in% sessions$session_id)) {
      selected_session <- sessions$session_id[[1]]
    }

    selectInput("session_id", "Session", choices = setNames(sessions$session_id, labels), selected = selected_session)
  })

  current_session <- reactive({
    sessions <- session_index()
    req(nrow(sessions) > 0)
    if (is.null(input$session_id) || !(input$session_id %in% sessions$session_id)) {
      return(sessions |> slice(1))
    }
    sessions |> filter(session_id == input$session_id) |> slice(1)
  })

  current_meta <- reactive({
    row <- current_session()
    load_session_part(row, "Meta")
  })

  current_event <- reactive({
    row <- current_session()
    load_session_part(row, "Event")
  })

  current_summary <- reactive({
    row <- current_session()
    load_session_part(row, "Summary")
  })

  current_sample <- reactive({
    row <- current_session()
    load_session_part(row, "Sample")
  })

  comparison_data_all <- reactiveVal(tibble())

  observeEvent(session_index(), {
    comparison_data_all(build_comparison_dataset(session_index()))
  }, ignoreInit = FALSE)

  output$session_status <- renderUI({
    row <- current_session()
    state <- ifelse(is.na(row$session_state) || row$session_state == "", "Unknown", row$session_state)
    class_name <- ifelse(tolower(state) == "finished", "status-finished", "status-aborted")
    div(class = paste("status-box", class_name), paste("Session State:", state))
  })

  output$summary_cards <- renderUI({
    summary <- current_summary()
    meta <- current_meta()
    session_row <- current_session()

    input_mode_label <- NA_character_
    if (nrow(session_row) > 0 && "input_mode_label" %in% names(session_row)) {
      input_mode_label <- session_row$input_mode_label[[1]]
    }
    if ((is.na(input_mode_label) || input_mode_label == "") && has_cols(meta, "InputModeLabel") && nrow(meta) > 0) {
      input_mode_label <- as.character(meta$InputModeLabel[[1]])
    }
    if ((is.na(input_mode_label) || input_mode_label == "") && has_cols(meta, "TrackingMode") && nrow(meta) > 0) {
      tracking_mode <- as.character(meta$TrackingMode[[1]])
      input_mode_label <- if (identical(tracking_mode, "Hands")) "embodied" else if (identical(tracking_mode, "Controllers")) "controller" else tracking_mode
    }
    if (is.na(input_mode_label) || input_mode_label == "") {
      input_mode_label <- "unknown"
    }

    if (!has_cols(summary, c("SummaryType", "Magnitude", "SignedDelta", "BaselineValue", "PostValue", "BaselineSd", "PostSd", "TaskMode", "MetricName", "MetricUnits"))) {
      return(tagList(
        div(class = "metric-card",
            div(class = "metric-title", "Session"),
            div(class = "metric-value", "No Data"),
            div(class = "metric-sub", "Summary.csv is missing required columns or has no usable rows.")
        )
      ))
    }

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
          div(class = "metric-sub", paste("Input mode:", input_mode_label)),
          div(class = "metric-sub", paste("Measured value:", aftereffect$MetricName[[1]])),
          div(class = "metric-sub", paste("Configured effect:", format_effect_label(aftereffect)))
      ),
      div(class = "metric-card",
          div(class = "metric-title", "Baseline to Post"),
          div(class = "metric-value", sprintf("%.2f", aftereffect$SignedDelta[[1]])),
          div(class = "metric-sub", paste("Average left/right shift from baseline to post (", aftereffect$MetricUnits[[1]], ")", sep = "")),
          div(class = "metric-sub", interpretation$signed_shift_label)
      ),
      div(class = "metric-card",
          div(class = "metric-title", "Magnitude"),
          div(class = "metric-value", sprintf("%.2f", aftereffect$Magnitude[[1]])),
          div(class = "metric-sub", paste("How much the average response changed, ignoring direction (", aftereffect$MetricUnits[[1]], ")", sep = "")),
          div(class = "metric-sub", interpretation$strength_label)
      ),
      div(class = "metric-card",
          div(class = "metric-title", "Consistency"),
          div(class = "metric-value", ifelse(
            is.na(variability$baseline_sd) || is.na(variability$post_sd),
            "N/A",
            sprintf("%.2f -> %.2f", variability$baseline_sd, variability$post_sd)
          )),
          div(class = "metric-sub", paste("Spread of responses: baseline to post (", aftereffect$MetricUnits[[1]], ")", sep = "")),
          div(class = "metric-sub", variability$variability_label)
      ),
      div(class = "metric-card",
          div(class = "metric-title", "Baseline / Post"),
          div(class = "metric-value", sprintf("%.2f -> %.2f", aftereffect$BaselineValue[[1]], aftereffect$PostValue[[1]])),
          div(
            class = "metric-sub",
            if (aftereffect$MetricName[[1]] %in% c("SignedEndpointError", "BisectionError")) {
              "Average signed response: baseline to post. Positive = right, negative = left."
            } else {
              "Average task response: baseline to post."
            }
          )
      )
    )

    if (is_exposure_task) {
      cards <- append(cards, list(
        div(class = "metric-card",
            div(class = "metric-title", "Error Correction"),
            div(class = "metric-value", interpretation$correction_display),
            div(class = "metric-sub", interpretation$direction_label),
            div(class = "metric-sub", "Only shown for exposure, where the hitmarker gives live feedback."),
            div(class = "metric-sub", "Positive means the participant ended closer to the target than in baseline."),
            div(class = "metric-sub", interpretation$correction_label)
        )
      ), after = 4)
    } else {
      cards <- append(cards, list(
        div(class = "metric-card",
            div(class = "metric-title", "Distance From Target"),
            div(class = "metric-value", interpretation$correction_display),
            div(class = "metric-sub", interpretation$direction_label),
            div(class = "metric-sub", paste("Average distance from the target center: baseline vs post (", aftereffect$MetricUnits[[1]], ")", sep = "")),
            div(class = "metric-sub", interpretation$correction_label)
        )
      ), after = 4)
    }

    do.call(tagList, cards)
  })

  output$summary_table <- renderDT({
    summary <- current_summary()
    if (nrow(summary) == 0) return(datatable(tibble(Message = "No Summary.csv for this session")))

    datatable(
      summary_compact_table(summary),
      options = list(pageLength = 10, scrollX = TRUE),
      rownames = FALSE
    )
  })

  output$summary_plot <- renderPlotly({
    ggplotly(build_summary_plot(current_summary()))
  })

  observe({
    event_data <- current_event()
    if (!has_cols(event_data, "TaskMode")) {
      tasks <- c("Exposure")
      updateSelectInput(session, "quality_task", choices = tasks, selected = tasks[[1]])
      updateSelectInput(session, "spatial_task", choices = tasks, selected = tasks[[1]])
      updateSelectInput(session, "trajectory_task", choices = tasks, selected = tasks[[1]])
      return()
    }

    tasks <- sort(unique(na.omit(event_data$TaskMode)))
    if (length(tasks) == 0) tasks <- c("Exposure")
    default_task <- if ("Exposure" %in% tasks) "Exposure" else tasks[[1]]
    updateSelectInput(session, "quality_task", choices = tasks, selected = default_task)
    updateSelectInput(session, "spatial_task", choices = tasks, selected = default_task)
    updateSelectInput(session, "trajectory_task", choices = tasks, selected = default_task)
  })

  observe({
    compare_data <- comparison_data_all()
    if (!has_cols(compare_data, c("TaskMode", "InputModeLabel", "ConfiguredEffectMode"))) {
      updateSelectInput(session, "compare_task", choices = c("All"), selected = "All")
      updateSelectInput(session, "compare_input_mode", choices = c("All"), selected = "All")
      updateSelectInput(session, "compare_effect", choices = c("All"), selected = "All")
      return()
    }

    tasks <- sort(unique(na.omit(compare_data$TaskMode)))
    input_modes <- sort(unique(na.omit(compare_data$InputModeLabel)))
    effects <- sort(unique(na.omit(compare_data$ConfiguredEffectMode)))

    preferred_task <- if ("OpenLoop" %in% tasks) "OpenLoop" else tasks[[1]]
    selected_task <- if (!is.null(input$compare_task) && input$compare_task %in% c("All", tasks)) input$compare_task else preferred_task
    selected_mode <- if (!is.null(input$compare_input_mode) && input$compare_input_mode %in% c("All", input_modes)) input$compare_input_mode else "All"
    selected_effect <- if (!is.null(input$compare_effect) && input$compare_effect %in% c("All", effects)) input$compare_effect else "All"

    updateSelectInput(session, "compare_task", choices = c("All", tasks), selected = selected_task)
    updateSelectInput(session, "compare_input_mode", choices = c("All", input_modes), selected = selected_mode)
    updateSelectInput(session, "compare_effect", choices = c("All", effects), selected = selected_effect)
  })

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

    top_task <- compare_data |>
      group_by(TaskMode) |>
      summarise(MeanMagnitude = mean(Magnitude, na.rm = TRUE), .groups = "drop") |>
      arrange(desc(MeanMagnitude)) |>
      slice(1)

    top_condition <- compare_data |>
      mutate(EffectGroup = ifelse(is.na(ConfiguredEffectMode) | ConfiguredEffectMode == "", "Unknown", ConfiguredEffectMode)) |>
      group_by(TaskMode, EffectGroup, InputModeLabel) |>
      summarise(MeanMagnitude = mean(Magnitude, na.rm = TRUE), Sessions = n(), .groups = "drop") |>
      arrange(desc(MeanMagnitude)) |>
      slice(1)

    mode_means <- compare_data |>
      filter(!is.na(InputModeLabel), InputModeLabel %in% c("controller", "embodied")) |>
      group_by(InputModeLabel) |>
      summarise(MeanMagnitude = mean(Magnitude, na.rm = TRUE), .groups = "drop")

    mode_delta_text <- "Not enough data to compare controller and embodied directly"
    if (nrow(mode_means) == 2) {
      controller_mean <- mode_means$MeanMagnitude[mode_means$InputModeLabel == "controller"]
      embodied_mean <- mode_means$MeanMagnitude[mode_means$InputModeLabel == "embodied"]
      if (length(controller_mean) == 1 && length(embodied_mean) == 1) {
        diff_value <- embodied_mean - controller_mean
        mode_delta_text <- if (abs(diff_value) < 0.05) {
          "Embodied and controller looked very similar"
        } else if (diff_value > 0) {
          sprintf("Embodied averaged %.2f higher magnitude than controller", abs(diff_value))
        } else {
          sprintf("Controller averaged %.2f higher magnitude than embodied", abs(diff_value))
        }
      }
    }

    fluidRow(
      column(
        3,
        div(class = "metric-card",
            div(class = "metric-title", "Sessions"),
            div(class = "metric-value", nrow(compare_data)),
            div(class = "metric-sub", "Finished sessions currently included in this comparison")
        )
      ),
      column(
        3,
        div(class = "metric-card",
            div(class = "metric-title", "Strongest Task"),
            div(class = "metric-value", top_task$TaskMode[[1]]),
            div(class = "metric-sub", sprintf("Largest average shift: %.2f", top_task$MeanMagnitude[[1]])),
            div(class = "metric-sub", "Quick answer to which task produced the biggest aftereffect")
        )
      ),
      column(
        3,
        div(class = "metric-card",
            div(class = "metric-title", "Strongest Condition"),
            div(class = "metric-value", paste(top_condition$TaskMode[[1]], "/", top_condition$EffectGroup[[1]], "/", top_condition$InputModeLabel[[1]])),
            div(class = "metric-sub", sprintf("Largest average shift: %.2f across %d session(s)", top_condition$MeanMagnitude[[1]], top_condition$Sessions[[1]])),
            div(class = "metric-sub", "Quick answer to which exact condition produced the biggest aftereffect")
        )
      ),
      column(
        3,
        div(class = "metric-card",
            div(class = "metric-title", "Average Magnitude"),
            div(class = "metric-value", sprintf("%.2f", mean_mag)),
            div(class = "metric-sub", sprintf("Typical shift across sessions: median %.2f", median_mag)),
            div(class = "metric-sub", "Higher means a larger change from baseline to post"),
            div(class = "metric-sub", mode_delta_text)
        )
      )
    )
  })

  output$compare_plot <- renderPlotly({
    compare_data <- comparison_data_filtered()
    suppressWarnings(suppressMessages({
      if (identical(input$compare_view, "average")) {
        plot_obj <- build_grouped_comparison_plot(compare_data)
      } else {
        plot_obj <- build_comparison_plot(compare_data)
      }
      ggplotly(plot_obj, tooltip = "text")
    }))
  })

  output$compare_stats_table <- renderDT({
    compare_data <- comparison_data_filtered()

    datatable(
      build_comparison_stats_table(compare_data),
      options = list(pageLength = 10, scrollX = TRUE, dom = "tip"),
      rownames = FALSE
    )
  })

  output$compare_table <- renderDT({
    compare_data <- comparison_data_filtered()

    if (nrow(compare_data) == 0) {
      return(datatable(tibble(Message = "No comparison rows for the current filters")))
    }

    datatable(
      compare_data |>
        transmute(
          Timestamp = TimestampLabel,
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
          ConsistencyChange = ConsistencyDelta,
          TowardTarget = TowardTargetDelta
        ),
      options = list(pageLength = 10, scrollX = TRUE),
      rownames = FALSE
    )
  })

  blocks_for_task <- function(event_data, task_name) {
    if (!has_cols(event_data, c("TaskMode", "BlockType"))) {
      return(c("Exposure"))
    }

    blocks <- event_data |>
      filter(TaskMode == task_name) |>
      pull(BlockType) |>
      na.omit() |>
      unique() |>
      sort()

    if (length(blocks) == 0) blocks <- c("Exposure")
    blocks
  }

  observe({
    event_data <- current_event()
    req(nrow(event_data) >= 0)

    quality_task_value <- if (!is.null(input$quality_task) && nzchar(input$quality_task)) input$quality_task else "Exposure"
    quality_blocks <- blocks_for_task(event_data, quality_task_value)
    quality_selected <- if (!is.null(input$quality_block) && input$quality_block %in% quality_blocks) input$quality_block else if ("Exposure" %in% quality_blocks) "Exposure" else quality_blocks[[1]]
    updateSelectInput(session, "quality_block", choices = quality_blocks, selected = quality_selected)

    spatial_task_value <- if (!is.null(input$spatial_task) && nzchar(input$spatial_task)) input$spatial_task else "Exposure"
    spatial_blocks <- blocks_for_task(event_data, spatial_task_value)
    spatial_selected <- if (!is.null(input$spatial_block) && input$spatial_block %in% spatial_blocks) input$spatial_block else if ("Exposure" %in% spatial_blocks) "Exposure" else spatial_blocks[[1]]
    updateSelectInput(session, "spatial_block", choices = spatial_blocks, selected = spatial_selected)

    trajectory_task_value <- if (!is.null(input$trajectory_task) && nzchar(input$trajectory_task)) input$trajectory_task else "Exposure"
    trajectory_blocks <- blocks_for_task(event_data, trajectory_task_value)
    trajectory_selected <- if (!is.null(input$trajectory_block) && input$trajectory_block %in% trajectory_blocks) input$trajectory_block else if ("Exposure" %in% trajectory_blocks) "Exposure" else trajectory_blocks[[1]]
    updateSelectInput(session, "trajectory_block", choices = trajectory_blocks, selected = trajectory_selected)
  })

  output$attempt_picker <- renderUI({
    event_data <- current_event()
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

    attempt_label <- if (input$trajectory_task == "Exposure") "Highlighted Attempt" else "Highlighted Trial"

    if (length(attempts) == 0) {
      return(selectInput("trajectory_attempt", attempt_label, choices = c(1), selected = 1))
    }

    selectInput("trajectory_attempt", attempt_label, choices = attempts, selected = attempts[[1]])
  })

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

  output$trajectory_help <- renderUI({
    if (input$trajectory_task == "Exposure") {
      return(tags$p("This tab focuses on approach behavior rather than raw controller wandering. The first plot shows how the projected aim point converged on the target plane before confirmation. The second plot breaks that same response window into acceleration, deceleration, and hover around the target."))
    }

    tags$p("For OpenLoop and LineBisection, this tab combines two views. The top and bottom plots are trial-by-trial response trends, not physical movement trajectories. The added phase plot uses the logged trial window to estimate acceleration, deceleration, and hover for newer sessions that include Trial Started events.")
  })

  output$quality_cards <- renderUI({
    req(input$quality_task, input$quality_block)
    quality_trials <- extract_quality_trials(current_event(), input$quality_task, input$quality_block)
    metrics <- compute_quality_metrics(quality_trials, input$quality_task)
    units_label <- quality_units_label(input$quality_task)
    error_label <- quality_error_label(input$quality_task)

    trend_value <- ifelse(
      is.na(metrics$slope),
      "N/A",
      sprintf("%.4f %s per trial", metrics$slope, units_label)
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
          div(class = "metric-sub", paste("Average", tolower(error_label), "across trials")),
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

    do.call(tagList, cards)
  })

  output$quality_plot <- renderPlotly({
    req(input$quality_task, input$quality_block)
    quality_trials <- extract_quality_trials(current_event(), input$quality_task, input$quality_block)
    suppressWarnings(suppressMessages({
      plot_obj <- build_quality_plot(quality_trials, input$quality_task, input$quality_block)
      ggplotly(plot_obj, tooltip = "text")
    }))
  })

  output$quality_table <- renderDT({
    req(input$quality_task, input$quality_block)
    quality_trials <- extract_quality_trials(current_event(), input$quality_task, input$quality_block)

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

  output$spatial_plot <- renderPlotly({
    suppressWarnings(suppressMessages({
      plot_obj <- build_spatial_plot(current_event(), input$spatial_task, input$spatial_block)
      ggplotly(plot_obj, tooltip = "text")
    }))
  })

  output$trajectory_plot <- renderPlotly({
    req(input$trajectory_attempt)
    suppressWarnings(suppressMessages(
      ggplotly(build_trajectory_overview_plot(
        current_sample(),
        current_event(),
        selected_task = input$trajectory_task,
        selected_block = input$trajectory_block,
        selected_attempt = as_num(input$trajectory_attempt),
        time_window = input$trajectory_window,
        overlap_blocks = isTRUE(input$trajectory_overlap)
      ))
    ))
  })

  output$trajectory_phase_plot <- renderPlotly({
    req(input$trajectory_attempt)
    phase_time_window <- if (is.null(input$trajectory_window)) {
      1
    } else {
      input$trajectory_window
    }

    suppressWarnings(suppressMessages(
      build_phase_plot(
        current_meta(),
        current_sample(),
        current_event(),
        selected_task = input$trajectory_task,
        selected_block = input$trajectory_block,
        selected_attempt = as_num(input$trajectory_attempt),
        time_window = phase_time_window
      )
    ))
  })

  output$trajectory_error_plot <- renderPlotly({
    suppressWarnings(suppressMessages(
      ggplotly(build_attempt_error_plot(
        current_event(),
        selected_task = input$trajectory_task,
        selected_block = input$trajectory_block,
        overlap_blocks = isTRUE(input$trajectory_overlap)
      ))
    ))
  })
}
