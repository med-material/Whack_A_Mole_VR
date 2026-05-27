# Prism Adaptation Participant Report

Generated: 2026-05-22 12:22:27

## Dataset

- Log folder: `C:/UNI/P8/Prism-effect-adaptation/Assets/PrismLogging/Participants`
- Included sessions: 80 finished participant sessions.
- Aftereffect rows reconstructed from Event logs: 80.
- Participants represented: 10.
- Participants with the expected 8 modules: 10.

The report excludes debug and aborted sessions by requiring `ExperimentMode == Participant` and `SessionState == Finished`.

## Primary Measure

For OpenLoop and LineBisection, baseline and post responses are each summarized as a stable perceived middle. This uses all accepted trials, but far-away responses are down-weighted so the repeated response cluster drives the estimate more than isolated slips. The main aftereffect magnitude is the absolute baseline-to-post shift in that perceived middle, measured in centimeters.

The normalized outcome subtracts each participant's own None run for the same task. Positive values mean the perturbation produced a larger baseline-to-post shift than ordinary no-effect drift/noise for that participant.

## High-level Pattern

- Largest median perturbation-over-None condition: LineBisection / controller / Rotation = 10.67 cm.
- Smallest median perturbation-over-None condition: OpenLoop / embodied / Rotation = -12.96 cm.

Interpretation should focus on whether perturbation conditions exceed participant-specific None, not only on raw magnitude, because several None sessions show non-trivial drift.

## Behavioural and Sample-derived Quality

The report now includes objective exposure behaviour and sample-derived movement summaries. Exposure behaviour is estimated from Event logs using hit rate, misses, final distance, and the median time between exposure attempts. Sample behaviour is estimated from Sample logs using pointer path length and pointer speed summaries. These are not replacements for the aftereffect measures; they are context for deciding how much trust to place in each participant.

- Participants with automatic quality flags: P10 (embodied; hit rate 38%; median pace 0.80s; max None 19.5 cm); P2 (embodied; hit rate 41%; median pace 0.66s; max None 17.8 cm); P4 (embodied; hit rate 45%; median pace 1.34s; max None 22.3 cm); P8 (embodied; hit rate 59%; median pace 1.00s; max None 10.2 cm); P6 (embodied; hit rate 61%; median pace 0.85s; max None 8.6 cm); P9 (controller; hit rate 82%; median pace 1.19s; max None 6.0 cm); P3 (controller; hit rate 86%; median pace 1.40s; max None 44.4 cm); P5 (controller; hit rate 88%; median pace 0.59s; max None 9.0 cm).
- Participant 2 context: exposure hit rate 41%, median exposure pace 0.66s, max no-effect shift 17.79 cm. Examiner observation notes very fast completion and many misses, so this participant should be interpreted cautiously even if not all automatic thresholds trigger.

Manual examiner observations are stored separately in `behavioral_observations.csv` and joined into `participant_summary.csv` / `quality_flags.csv`. They should be reported as qualitative context, not as exclusion rules unless explicitly justified.

## Output Files

- `included_sessions.csv`: session-level inclusion list.
- `session_aftereffects.csv`: one aftereffect row per finished module.
- `effect_minus_none.csv`: main normalized analysis table.
- `exposure_quality_by_session.csv`: hit/miss/pace metrics from exposure Event logs.
- `sample_movement_by_session.csv`: pointer path and speed metrics from Sample logs.
- `condition_summary.csv`: grouped condition summaries.
- `participant_summary.csv`: participant-level summaries.
- `quality_flags.csv`: participants/modules that need caution.
- `behavioral_observations.csv`: examiner notes and interpretive tags.
- `condition_absolute_shift.png`: raw absolute baseline-to-post shifts.
- `effect_minus_none.png`: perturbation effect above None.
- `participant_heatmap_effect_minus_none.png`: participant-by-condition overview.
- `participant_behavior_quality.png`: exposure pace/hit-rate/no-effect drift overview.
