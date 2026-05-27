library(readr)
library(dplyr)
library(tidyr)
library(ggplot2)
library(stringr)
library(purrr)

args <- commandArgs(trailingOnly = TRUE)
repo_root <- normalizePath(ifelse(length(args) >= 1, args[[1]], getwd()), winslash = "/", mustWork = TRUE)
log_dir <- normalizePath(file.path(repo_root, "Assets", "PrismLogging", "Participants"), winslash = "/", mustWork = TRUE)
out_dir <- normalizePath(file.path(repo_root, "Assets", "PEGFG", "Analysis", "PrismReport", "output"), winslash = "/", mustWork = FALSE)
dir.create(out_dir, recursive = TRUE, showWarnings = FALSE)

as_num <- function(x) suppressWarnings(as.numeric(x))

safe_read_csv <- function(path, columns = NULL) {
  tryCatch(
    suppressMessages(
      if (is.null(columns)) {
        read_delim(path, delim = ";", show_col_types = FALSE, progress = FALSE)
      } else {
        read_delim(path, delim = ";", show_col_types = FALSE, progress = FALSE, col_select = any_of(columns))
      }
    ) |>
      mutate(across(where(is.character), ~na_if(.x, "NULL"))),
    error = function(e) tibble()
  )
}

stable_cluster_center <- function(x) {
  x <- x[is.finite(x)]
  if (length(x) == 0) return(NA_real_)
  if (length(x) == 1) return(x[[1]])

  raw_center <- median(x, na.rm = TRUE)
  robust_scale <- IQR(x, na.rm = TRUE) / 1.349
  if (!is.finite(robust_scale) || robust_scale < 1e-6) robust_scale <- sd(x, na.rm = TRUE)
  if (!is.finite(robust_scale) || robust_scale < 1e-6) return(mean(x, na.rm = TRUE))

  weights <- 1 / (1 + (abs(x - raw_center) / robust_scale)^2)
  weighted.mean(x, weights, na.rm = TRUE)
}

stable_cluster_spread <- function(x) {
  x <- x[is.finite(x)]
  if (length(x) <= 1) return(NA_real_)

  center <- stable_cluster_center(x)
  robust_scale <- IQR(x, na.rm = TRUE) / 1.349
  if (!is.finite(robust_scale) || robust_scale < 1e-6) robust_scale <- sd(x, na.rm = TRUE)
  if (!is.finite(robust_scale) || robust_scale < 1e-6) return(0)

  weights <- 1 / (1 + (abs(x - median(x, na.rm = TRUE)) / robust_scale)^2)
  sqrt(weighted.mean((x - center)^2, weights, na.rm = TRUE))
}

session_files <- function(pattern) {
  list.files(log_dir, pattern = pattern, full.names = TRUE)
}

meta_cols <- c(
  "SessionID", "Timestamp", "ExperimentMode", "SessionState", "InputModeLabel",
  "StudyGlobalParticipantId", "StudyInputMode", "StudyParticipantIndex",
  "StudyResolvedEffect", "StudyResolvedTask", "StudySessionIndex", "StudyTaskOrder",
  "TrackingMode", "XRBackend", "SessionDuration"
)

meta_index <- tibble(path = session_files("_Meta\\.csv$")) |>
  mutate(data = map(path, ~safe_read_csv(.x, meta_cols))) |>
  filter(map_int(data, nrow) > 0) |>
  unnest(data) |>
  mutate(
    StudyGlobalParticipantId = as.integer(as_num(StudyGlobalParticipantId)),
    StudyParticipantIndex = as.integer(as_num(StudyParticipantIndex)),
    StudySessionIndex = as.integer(as_num(StudySessionIndex)),
    SessionDuration = as_num(SessionDuration),
    ExperimentMode = str_trim(coalesce(ExperimentMode, "")),
    SessionState = str_trim(coalesce(SessionState, "")),
    InputModeLabel = str_trim(coalesce(InputModeLabel, StudyInputMode, "")),
    StudyResolvedEffect = str_trim(coalesce(StudyResolvedEffect, "")),
    StudyResolvedTask = str_trim(coalesce(StudyResolvedTask, ""))
  )

participant_sessions <- meta_index |>
  filter(
    tolower(ExperimentMode) == "participant",
    tolower(SessionState) == "finished",
    !is.na(StudyGlobalParticipantId)
  ) |>
  arrange(StudyGlobalParticipantId, StudySessionIndex, Timestamp)

summary_index <- tibble(path = session_files("_Summary\\.csv$")) |>
  mutate(data = map(path, ~safe_read_csv(.x, c("SessionID", "SummaryType", "TaskMode", "ConfiguredEffectMode")))) |>
  filter(map_int(data, nrow) > 0) |>
  unnest(data) |>
  distinct(SessionID, path)

event_index <- tibble(path = session_files("_Event\\.csv$")) |>
  mutate(data = map(path, ~safe_read_csv(.x, c("SessionID")))) |>
  filter(map_int(data, nrow) > 0) |>
  unnest(data) |>
  distinct(SessionID, path)

sample_index <- tibble(path = session_files("_Sample\\.csv$")) |>
  mutate(data = map(path, ~safe_read_csv(.x, c("SessionID")))) |>
  filter(map_int(data, nrow) > 0) |>
  unnest(data) |>
  distinct(SessionID, path)

analysis_sessions <- participant_sessions |>
  left_join(summary_index, by = "SessionID", suffix = c("", "_summary")) |>
  rename(SummaryPath = path_summary, MetaPath = path) |>
  left_join(event_index, by = "SessionID") |>
  rename(EventPath = path) |>
  left_join(sample_index, by = "SessionID") |>
  rename(SamplePath = path)

event_cols <- c(
  "SessionID", "ExperimentMode", "Event", "TaskMode", "BlockType", "TrialIndex",
  "SignedOffsetCm", "ErrorCm", "HitDistanceMeters", "IsHit", "AttemptIndex",
  "StudyGlobalParticipantId", "StudyResolvedEffect", "StudyResolvedTask",
  "StudySessionIndex", "StudyInputMode", "Timestamp"
)

extract_aftereffect <- function(session_row) {
  if (is.na(session_row$EventPath) || !file.exists(session_row$EventPath)) return(tibble())
  event_data <- safe_read_csv(session_row$EventPath, event_cols)
  if (nrow(event_data) == 0) return(tibble())

  trials <- event_data |>
    filter(
      Event == "Trial Accepted",
      TaskMode %in% c("OpenLoop", "LineBisection"),
      BlockType %in% c("Baseline", "Post")
    ) |>
    mutate(
      PerceivedMiddleCm = case_when(
        TaskMode == "OpenLoop" ~ as_num(SignedOffsetCm),
        TaskMode == "LineBisection" ~ as_num(ErrorCm),
        TRUE ~ NA_real_
      ),
      TrialIndex = as_num(TrialIndex)
    ) |>
    filter(is.finite(PerceivedMiddleCm))

  if (nrow(trials) == 0) return(tibble())

  centers <- trials |>
    group_by(TaskMode, BlockType) |>
    summarise(
      CenterCm = stable_cluster_center(PerceivedMiddleCm),
      SpreadCm = stable_cluster_spread(PerceivedMiddleCm),
      RawMeanCm = mean(PerceivedMiddleCm, na.rm = TRUE),
      RawSdCm = sd(PerceivedMiddleCm, na.rm = TRUE),
      TrialCount = n(),
      .groups = "drop"
    ) |>
    pivot_wider(
      names_from = BlockType,
      values_from = c(CenterCm, SpreadCm, RawMeanCm, RawSdCm, TrialCount),
      names_sep = ""
    )

  if (!all(c("CenterCmBaseline", "CenterCmPost") %in% names(centers))) return(tibble())

  centers |>
    filter(is.finite(CenterCmBaseline), is.finite(CenterCmPost)) |>
    transmute(
      SessionID = session_row$SessionID,
      Timestamp = session_row$Timestamp,
      Participant = session_row$StudyGlobalParticipantId,
      InputMode = tolower(session_row$InputModeLabel),
      Task = TaskMode,
      Effect = session_row$StudyResolvedEffect,
      SessionIndex = session_row$StudySessionIndex,
      TaskOrder = session_row$StudyTaskOrder,
      BaselineCenterCm = CenterCmBaseline,
      PostCenterCm = CenterCmPost,
      SignedShiftCm = PostCenterCm - BaselineCenterCm,
      MagnitudeCm = abs(SignedShiftCm),
      BaselineSpreadCm = SpreadCmBaseline,
      PostSpreadCm = SpreadCmPost,
      RawBaselineMeanCm = RawMeanCmBaseline,
      RawPostMeanCm = RawMeanCmPost,
      RawSignedShiftCm = RawPostMeanCm - RawBaselineMeanCm,
      RawMagnitudeCm = abs(RawSignedShiftCm),
      BaselineTrials = TrialCountBaseline,
      PostTrials = TrialCountPost
    )
}

aftereffects <- map_dfr(seq_len(nrow(analysis_sessions)), ~extract_aftereffect(analysis_sessions[.x, ])) |>
  mutate(
    Effect = factor(Effect, levels = c("None", "Translation", "Rotation", "Skew")),
    Task = factor(Task, levels = c("LineBisection", "OpenLoop")),
    InputMode = factor(InputMode, levels = c("controller", "embodied"))
  ) |>
  arrange(Participant, SessionIndex)

extract_exposure_quality <- function(session_row) {
  if (is.na(session_row$EventPath) || !file.exists(session_row$EventPath)) return(tibble())
  event_data <- safe_read_csv(session_row$EventPath, event_cols)
  if (nrow(event_data) == 0) return(tibble())

  exposure_outcomes <- event_data |>
    filter(
      TaskMode == "Exposure",
      Event %in% c("Mole Hit", "Mole Missed"),
      !is.na(AttemptIndex)
    ) |>
    mutate(
      AttemptIndex = as_num(AttemptIndex),
      IsHitNum = as_num(IsHit),
      HitDistanceMeters = as_num(HitDistanceMeters),
      TimestampParsed = suppressWarnings(as.POSIXct(Timestamp, tz = "UTC"))
    ) |>
    arrange(AttemptIndex, TimestampParsed)

  if (nrow(exposure_outcomes) == 0) return(tibble())

  intervals <- as.numeric(diff(exposure_outcomes$TimestampParsed), units = "secs")
  intervals <- intervals[is.finite(intervals) & intervals > 0 & intervals < 30]

  tibble(
    SessionID = session_row$SessionID,
    Participant = session_row$StudyGlobalParticipantId,
    InputMode = tolower(session_row$InputModeLabel),
    Task = session_row$StudyResolvedTask,
    Effect = session_row$StudyResolvedEffect,
    SessionIndex = session_row$StudySessionIndex,
    ExposureAttempts = nrow(exposure_outcomes),
    ExposureHits = sum(exposure_outcomes$IsHitNum == 1, na.rm = TRUE),
    ExposureMisses = sum(exposure_outcomes$IsHitNum == 0, na.rm = TRUE),
    ExposureHitRate = mean(exposure_outcomes$IsHitNum == 1, na.rm = TRUE),
    ExposureMeanDistanceCm = mean(exposure_outcomes$HitDistanceMeters, na.rm = TRUE) * 100,
    ExposureMedianPaceSeconds = median(intervals, na.rm = TRUE),
    ExposureMeanPaceSeconds = mean(intervals, na.rm = TRUE)
  )
}

sample_cols <- c(
  "SessionID", "Timestamp", "TaskMode", "BlockType", "TrialIndex", "AttemptIndex",
  "PointerOriginX", "PointerOriginY", "PointerOriginZ",
  "PointerForwardX", "PointerForwardY", "PointerForwardZ",
  "StudyGlobalParticipantId", "StudyInputMode", "StudySessionIndex"
)

extract_sample_quality <- function(session_row) {
  if (is.na(session_row$SamplePath) || !file.exists(session_row$SamplePath)) return(tibble())
  sample_data <- safe_read_csv(session_row$SamplePath, sample_cols)
  if (nrow(sample_data) < 2) return(tibble())

  samples <- sample_data |>
    mutate(
      TimestampParsed = suppressWarnings(as.POSIXct(Timestamp, tz = "UTC")),
      PointerOriginX = as_num(PointerOriginX),
      PointerOriginY = as_num(PointerOriginY),
      PointerOriginZ = as_num(PointerOriginZ)
    ) |>
    filter(
      is.finite(PointerOriginX), is.finite(PointerOriginY), is.finite(PointerOriginZ),
      !is.na(TimestampParsed)
    ) |>
    arrange(TimestampParsed)

  if (nrow(samples) < 2) return(tibble())

  dx <- diff(samples$PointerOriginX)
  dy <- diff(samples$PointerOriginY)
  dz <- diff(samples$PointerOriginZ)
  dt <- as.numeric(diff(samples$TimestampParsed), units = "secs")
  step_distance <- sqrt(dx^2 + dy^2 + dz^2)
  speed <- step_distance / dt
  speed <- speed[is.finite(speed) & speed >= 0 & speed < 10]

  tibble(
    SessionID = session_row$SessionID,
    Participant = session_row$StudyGlobalParticipantId,
    InputMode = tolower(session_row$InputModeLabel),
    Task = session_row$StudyResolvedTask,
    Effect = session_row$StudyResolvedEffect,
    SessionIndex = session_row$StudySessionIndex,
    SampleRows = nrow(samples),
    SampleDurationSeconds = as.numeric(max(samples$TimestampParsed) - min(samples$TimestampParsed), units = "secs"),
    PointerPathMeters = sum(step_distance[is.finite(step_distance)], na.rm = TRUE),
    PointerMeanSpeedMps = mean(speed, na.rm = TRUE),
    PointerP95SpeedMps = as.numeric(quantile(speed, 0.95, na.rm = TRUE))
  )
}

exposure_quality <- map_dfr(seq_len(nrow(analysis_sessions)), ~extract_exposure_quality(analysis_sessions[.x, ]))
sample_quality <- map_dfr(seq_len(nrow(analysis_sessions)), ~extract_sample_quality(analysis_sessions[.x, ]))

none_reference <- aftereffects |>
  filter(Effect == "None") |>
  group_by(Participant, Task) |>
  summarise(
    NoneMagnitudeCm = mean(MagnitudeCm, na.rm = TRUE),
    NoneSignedShiftCm = mean(SignedShiftCm, na.rm = TRUE),
    .groups = "drop"
  )

normalized <- aftereffects |>
  left_join(none_reference, by = c("Participant", "Task")) |>
  mutate(
    MagnitudeMinusNoneCm = MagnitudeCm - NoneMagnitudeCm,
    SignedShiftMinusNoneCm = SignedShiftCm - NoneSignedShiftCm,
    HasNoneReference = is.finite(NoneMagnitudeCm)
  )

participant_completeness <- aftereffects |>
  distinct(Participant, InputMode, Task, Effect, SessionIndex) |>
  group_by(Participant, InputMode) |>
  summarise(
    Modules = n(),
    Tasks = n_distinct(Task),
    Effects = n_distinct(Effect),
    HasExpectedEightModules = Modules == 8 && Tasks == 2 && Effects == 4,
    .groups = "drop"
  )

condition_summary <- normalized |>
  group_by(Task, InputMode, Effect) |>
  summarise(
    N = n(),
    Participants = n_distinct(Participant),
    MedianMagnitudeCm = median(MagnitudeCm, na.rm = TRUE),
    MeanMagnitudeCm = mean(MagnitudeCm, na.rm = TRUE),
    MedianMagnitudeMinusNoneCm = median(MagnitudeMinusNoneCm, na.rm = TRUE),
    MeanMagnitudeMinusNoneCm = mean(MagnitudeMinusNoneCm, na.rm = TRUE),
    MedianSignedShiftCm = median(SignedShiftCm, na.rm = TRUE),
    .groups = "drop"
  )

exposure_participant_summary <- exposure_quality |>
  group_by(Participant, InputMode) |>
  summarise(
    ExposureAttempts = sum(ExposureAttempts, na.rm = TRUE),
    ExposureMisses = sum(ExposureMisses, na.rm = TRUE),
    ExposureHitRate = mean(ExposureHitRate, na.rm = TRUE),
    ExposureMedianPaceSeconds = median(ExposureMedianPaceSeconds, na.rm = TRUE),
    ExposureMeanDistanceCm = mean(ExposureMeanDistanceCm, na.rm = TRUE),
    .groups = "drop"
  )

sample_participant_summary <- sample_quality |>
  group_by(Participant, InputMode) |>
  summarise(
    SampleDurationMinutes = sum(SampleDurationSeconds, na.rm = TRUE) / 60,
    PointerPathMeters = sum(PointerPathMeters, na.rm = TRUE),
    PointerMeanSpeedMps = mean(PointerMeanSpeedMps, na.rm = TRUE),
    PointerP95SpeedMps = mean(PointerP95SpeedMps, na.rm = TRUE),
    .groups = "drop"
  )

behavioral_observations <- tribble(
  ~Participant, ~ObserverNote, ~InterpretiveTags,
  1L, "Meget erfaring i VR, langsom og præcis.", "VR-erfaren; langsom; præcis",
  2L, "Ekstremt hurtig gennemgang, missede mange gange, dårlig test.", "meget hurtig; mange misses; lav datakvalitet",
  3L, "Stabil, ikke meget at notere.", "stabil",
  4L, "Ikke meget erfaring, missede mange, headset blev løst.", "lav VR-erfaring; mange misses; headset løst",
  5L, "Meget erfaring, ekstremt præcis.", "VR-erfaren; meget præcis",
  6L, "Tracking i embodiment var finicky i nogle tilfælde. Adaptation virkede til at transfer til efterfølgende tasks, specielt run 2 post til run 3 baseline. Håndposition gjorde små justeringer til store afstandsforskelle. Open loop var mere præcis end line bisection med no effect.", "tracking-problemer; mulig carryover; embodiment; opgaveforskel",
  7L, "Lærte hurtigt og ramte godt med fin hastighed. Havde svært ved skew i open loop og justerede modsat forventet exposure-retning. Translation mellem task 2-3 syntes at korrigere skew. Inconsistent men god form; moderat VR-erfaring.", "hurtig læring; skew-problem; mulig carryover; moderat VR-erfaring",
  8L, "Blev hurtigt træt og var ikke specielt fokuseret. Kalibrerede mens de kiggede en anden vej, så første post hits i open loop skew blev outliers. Forvirret over hvor der skulle klikkes, hvilket ledte til outliers.", "fatigue; lav fokus; kalibreringsfejl; outliers",
  9L, "Rolig og præcis gennemgang. Fatigue byggede op. Meget præcis og resistent over for adaptation, dog skew-effekt. Brugte overkrop til at sigte selv efter kommentar, hvilket kan gøre resultatet mere præcist og resistent mod perturbation.", "rolig; præcis; fatigue; kompensationsstrategi; adaptation-resistent",
  10L, "Embodiment test hvor deltageren lyttede og forstod opgaven godt. Adaptation bløder ind i næste opgave ved task 3 baseline. Pegede meget opad selv med kommentar. Blev hurtigere mod starten og dermed mere upræcis. Generelt upræcis.", "embodiment; mulig carryover; peger opad; stigende hastighed; upræcis"
)

participant_summary <- normalized |>
  group_by(Participant, InputMode) |>
  summarise(
    Modules = n(),
    MeanMagnitudeCm = mean(MagnitudeCm, na.rm = TRUE),
    MedianMagnitudeCm = median(MagnitudeCm, na.rm = TRUE),
    MaxNoneMagnitudeCm = max(MagnitudeCm[Effect == "None"], na.rm = TRUE),
    MeanPerturbationMinusNoneCm = mean(MagnitudeMinusNoneCm[Effect != "None"], na.rm = TRUE),
    Complete = first(participant_completeness$HasExpectedEightModules[match(paste(Participant, InputMode), paste(participant_completeness$Participant, participant_completeness$InputMode))]),
    .groups = "drop"
  ) |>
  mutate(InputMode = as.character(InputMode)) |>
  left_join(exposure_participant_summary |> mutate(InputMode = as.character(InputMode)), by = c("Participant", "InputMode")) |>
  left_join(sample_participant_summary |> mutate(InputMode = as.character(InputMode)), by = c("Participant", "InputMode")) |>
  left_join(behavioral_observations, by = "Participant") |>
  mutate(
    HighNoneDriftFlag = is.finite(MaxNoneMagnitudeCm) & MaxNoneMagnitudeCm >= 15,
    LowExposureHitRateFlag = is.finite(ExposureHitRate) & ExposureHitRate < 0.50,
    FastExposurePaceFlag = is.finite(ExposureMedianPaceSeconds) & ExposureMedianPaceSeconds < 0.75,
    HighExposureDistanceFlag = is.finite(ExposureMeanDistanceCm) & ExposureMeanDistanceCm > 12,
    HighPointerSpeedFlag = is.finite(PointerP95SpeedMps) & PointerP95SpeedMps > 1.90,
    ObserverRiskFlag = str_detect(
      coalesce(InterpretiveTags, ""),
      regex("lav datakvalitet|mange misses|tracking|kalibreringsfejl|outliers|fatigue|upræcis|headset", ignore_case = TRUE)
    ),
    AnyQualityFlag = HighNoneDriftFlag | LowExposureHitRateFlag | FastExposurePaceFlag |
      HighExposureDistanceFlag | HighPointerSpeedFlag | ObserverRiskFlag
  )

quality_flags <- participant_summary |>
  select(
    Participant, InputMode, Complete, Modules, MaxNoneMagnitudeCm, MeanPerturbationMinusNoneCm,
    ExposureHitRate, ExposureMedianPaceSeconds, ExposureMeanDistanceCm, PointerP95SpeedMps,
    HighNoneDriftFlag, LowExposureHitRateFlag, FastExposurePaceFlag, HighExposureDistanceFlag,
    HighPointerSpeedFlag, ObserverRiskFlag, AnyQualityFlag, ObserverNote, InterpretiveTags
  ) |>
  arrange(desc(AnyQualityFlag), desc(ObserverRiskFlag), ExposureHitRate, ExposureMedianPaceSeconds)

write_csv(analysis_sessions, file.path(out_dir, "included_sessions.csv"))
write_csv(aftereffects, file.path(out_dir, "session_aftereffects.csv"))
write_csv(normalized, file.path(out_dir, "effect_minus_none.csv"))
write_csv(exposure_quality, file.path(out_dir, "exposure_quality_by_session.csv"))
write_csv(sample_quality, file.path(out_dir, "sample_movement_by_session.csv"))
write_csv(condition_summary, file.path(out_dir, "condition_summary.csv"))
write_csv(participant_summary, file.path(out_dir, "participant_summary.csv"))
write_csv(quality_flags, file.path(out_dir, "quality_flags.csv"))
write_csv(behavioral_observations, file.path(out_dir, "behavioral_observations.csv"))

theme_report <- function() {
  theme_minimal(base_size = 12) +
    theme(
      legend.position = "top",
      plot.title = element_text(face = "bold"),
      panel.grid.minor = element_blank()
    )
}

ggsave(
  file.path(out_dir, "condition_absolute_shift.png"),
  normalized |>
    ggplot(aes(x = Effect, y = MagnitudeCm, color = InputMode)) +
    geom_boxplot(outlier.shape = NA, position = position_dodge(width = 0.65)) +
    geom_rug(sides = "l", alpha = 0.6) +
    facet_wrap(~Task, scales = "free_y") +
    labs(
      title = "Baseline-to-post shift by condition",
      subtitle = "Absolute shift in perceived middle. Side ticks show individual modules.",
      x = "Effect",
      y = "Absolute shift (cm)",
      color = "Input mode"
    ) +
    theme_report(),
  width = 10,
  height = 6,
  dpi = 180
)

ggsave(
  file.path(out_dir, "effect_minus_none.png"),
  normalized |>
    filter(Effect != "None", HasNoneReference) |>
    ggplot(aes(x = Effect, y = MagnitudeMinusNoneCm, color = InputMode)) +
    geom_hline(yintercept = 0, linetype = "dashed", color = "grey40") +
    geom_boxplot(outlier.shape = NA, position = position_dodge(width = 0.65)) +
    geom_rug(sides = "l", alpha = 0.6) +
    facet_wrap(~Task, scales = "free_y") +
    labs(
      title = "Perturbation shift above participant-specific no-effect drift",
      subtitle = "Positive values indicate a larger baseline-to-post shift than the same participant's None run.",
      x = "Effect",
      y = "Extra shift beyond None (cm)",
      color = "Input mode"
    ) +
    theme_report(),
  width = 10,
  height = 6,
  dpi = 180
)

ggsave(
  file.path(out_dir, "participant_heatmap_effect_minus_none.png"),
  normalized |>
    filter(Effect != "None", HasNoneReference) |>
    mutate(Module = paste(Task, Effect, sep = " / ")) |>
    ggplot(aes(x = Module, y = factor(Participant), fill = MagnitudeMinusNoneCm)) +
    geom_tile(color = "white") +
    facet_wrap(~InputMode, scales = "free_y") +
    scale_fill_gradient2(low = "#2563eb", mid = "white", high = "#dc2626", midpoint = 0) +
    labs(
      title = "Participant-level perturbation effect above None",
      x = "Task / effect",
      y = "Participant",
      fill = "cm"
    ) +
    theme_report() +
    theme(axis.text.x = element_text(angle = 35, hjust = 1)),
  width = 11,
  height = 6,
  dpi = 180
)

ggsave(
  file.path(out_dir, "participant_behavior_quality.png"),
  participant_summary |>
    ggplot(aes(
      x = ExposureMedianPaceSeconds,
      y = ExposureHitRate,
      size = MaxNoneMagnitudeCm,
      color = InputMode,
      label = Participant
    )) +
    geom_point(alpha = 0.8) +
    geom_text(nudge_y = 0.025, size = 3, show.legend = FALSE) +
    scale_y_continuous(labels = function(x) paste0(round(x * 100), "%")) +
    labs(
      title = "Participant exposure behavior and no-effect drift",
      subtitle = "Fast pace, low hit rate, and large no-effect drift indicate sessions requiring cautious interpretation.",
      x = "Median exposure attempt pace (seconds)",
      y = "Exposure hit rate",
      size = "Max None shift (cm)",
      color = "Input mode"
    ) +
    theme_report(),
  width = 9,
  height = 5.5,
  dpi = 180
)

n_sessions <- nrow(analysis_sessions)
n_aftereffects <- nrow(aftereffects)
n_participants <- n_distinct(aftereffects$Participant)
n_complete <- sum(participant_completeness$HasExpectedEightModules, na.rm = TRUE)

strongest <- condition_summary |>
  filter(Effect != "None", is.finite(MedianMagnitudeMinusNoneCm)) |>
  arrange(desc(MedianMagnitudeMinusNoneCm)) |>
  slice(1)

weakest <- condition_summary |>
  filter(Effect != "None", is.finite(MedianMagnitudeMinusNoneCm)) |>
  arrange(MedianMagnitudeMinusNoneCm) |>
  slice(1)

flagged_participants <- quality_flags |>
  filter(AnyQualityFlag) |>
  mutate(label = paste0(
    "P", Participant,
    " (", InputMode,
    "; hit rate ", ifelse(is.finite(ExposureHitRate), sprintf("%.0f%%", ExposureHitRate * 100), "NA"),
    "; median pace ", ifelse(is.finite(ExposureMedianPaceSeconds), sprintf("%.2fs", ExposureMedianPaceSeconds), "NA"),
    "; max None ", ifelse(is.finite(MaxNoneMagnitudeCm), sprintf("%.1f cm", MaxNoneMagnitudeCm), "NA"),
    ")"
  )) |>
  pull(label)

participant_two <- participant_summary |>
  filter(Participant == 2) |>
  slice(1)

md <- c(
  "# Prism Adaptation Participant Report",
  "",
  paste0("Generated: ", format(Sys.time(), "%Y-%m-%d %H:%M:%S")),
  "",
  "## Dataset",
  "",
  paste0("- Log folder: `", log_dir, "`"),
  paste0("- Included sessions: ", n_sessions, " finished participant sessions."),
  paste0("- Aftereffect rows reconstructed from Event logs: ", n_aftereffects, "."),
  paste0("- Participants represented: ", n_participants, "."),
  paste0("- Participants with the expected 8 modules: ", n_complete, "."),
  "",
  "The report excludes debug and aborted sessions by requiring `ExperimentMode == Participant` and `SessionState == Finished`.",
  "",
  "## Primary Measure",
  "",
  "For OpenLoop and LineBisection, baseline and post responses are each summarized as a stable perceived middle. This uses all accepted trials, but far-away responses are down-weighted so the repeated response cluster drives the estimate more than isolated slips. The main aftereffect magnitude is the absolute baseline-to-post shift in that perceived middle, measured in centimeters.",
  "",
  "The normalized outcome subtracts each participant's own None run for the same task. Positive values mean the perturbation produced a larger baseline-to-post shift than ordinary no-effect drift/noise for that participant.",
  "",
  "## High-level Pattern",
  "",
  if (nrow(strongest) > 0) paste0("- Largest median perturbation-over-None condition: ", strongest$Task, " / ", strongest$InputMode, " / ", strongest$Effect, " = ", sprintf("%.2f", strongest$MedianMagnitudeMinusNoneCm), " cm.") else "- No perturbation-over-None condition could be estimated.",
  if (nrow(weakest) > 0) paste0("- Smallest median perturbation-over-None condition: ", weakest$Task, " / ", weakest$InputMode, " / ", weakest$Effect, " = ", sprintf("%.2f", weakest$MedianMagnitudeMinusNoneCm), " cm.") else "- No perturbation-over-None condition could be estimated.",
  "",
  "Interpretation should focus on whether perturbation conditions exceed participant-specific None, not only on raw magnitude, because several None sessions show non-trivial drift.",
  "",
  "## Behavioural and Sample-derived Quality",
  "",
  "The report now includes objective exposure behaviour and sample-derived movement summaries. Exposure behaviour is estimated from Event logs using hit rate, misses, final distance, and the median time between exposure attempts. Sample behaviour is estimated from Sample logs using pointer path length and pointer speed summaries. These are not replacements for the aftereffect measures; they are context for deciding how much trust to place in each participant.",
  "",
  if (length(flagged_participants) > 0) paste0("- Participants with automatic quality flags: ", paste(flagged_participants, collapse = "; "), ".") else "- No participants triggered automatic quality flags.",
  if (nrow(participant_two) > 0) paste0("- Participant 2 context: exposure hit rate ", sprintf("%.0f%%", participant_two$ExposureHitRate * 100), ", median exposure pace ", sprintf("%.2fs", participant_two$ExposureMedianPaceSeconds), ", max no-effect shift ", sprintf("%.2f cm", participant_two$MaxNoneMagnitudeCm), ". Examiner observation notes very fast completion and many misses, so this participant should be interpreted cautiously even if not all automatic thresholds trigger.") else "- Participant 2 was not found in the included participant summary.",
  "",
  "Manual examiner observations are stored separately in `behavioral_observations.csv` and joined into `participant_summary.csv` / `quality_flags.csv`. They should be reported as qualitative context, not as exclusion rules unless explicitly justified.",
  "",
  "## Output Files",
  "",
  "- `included_sessions.csv`: session-level inclusion list.",
  "- `session_aftereffects.csv`: one aftereffect row per finished module.",
  "- `effect_minus_none.csv`: main normalized analysis table.",
  "- `exposure_quality_by_session.csv`: hit/miss/pace metrics from exposure Event logs.",
  "- `sample_movement_by_session.csv`: pointer path and speed metrics from Sample logs.",
  "- `condition_summary.csv`: grouped condition summaries.",
  "- `participant_summary.csv`: participant-level summaries.",
  "- `quality_flags.csv`: participants/modules that need caution.",
  "- `behavioral_observations.csv`: examiner notes and interpretive tags.",
  "- `condition_absolute_shift.png`: raw absolute baseline-to-post shifts.",
  "- `effect_minus_none.png`: perturbation effect above None.",
  "- `participant_heatmap_effect_minus_none.png`: participant-by-condition overview.",
  "- `participant_behavior_quality.png`: exposure pace/hit-rate/no-effect drift overview."
)

writeLines(md, file.path(out_dir, "report.md"))

cat(sprintf("Included finished participant sessions: %d\n", n_sessions))
cat(sprintf("Participants: %d, complete participants: %d\n", n_participants, n_complete))
cat(sprintf("Wrote report outputs to: %s\n", out_dir))
