#!/usr/bin/env python3
from __future__ import annotations

import csv
import glob
import math
import os
import random
import shutil
import uuid
from dataclasses import dataclass
from datetime import datetime, timedelta
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
LOG_DIR = ROOT / "Assets" / "PrismLogging"
OUTPUT_DIR = LOG_DIR / "SyntheticShowcase"


def fmt_float(value: float | None) -> str:
    if value is None:
        return "NULL"
    return f"{value:.4f}"


def parse_float(value: str) -> float | None:
    if value in ("", "NULL", None):
        return None
    return float(value)


def read_csv(path: Path) -> tuple[list[str], list[dict[str, str]]]:
    with path.open(newline="", encoding="utf-8") as handle:
        reader = csv.DictReader(handle, delimiter=";")
        rows = list(reader)
        return list(reader.fieldnames or []), rows


def write_csv(path: Path, fieldnames: list[str], rows: list[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames, delimiter=";")
        writer.writeheader()
        writer.writerows(rows)


@dataclass
class SessionBundle:
    prefix: str
    session_id: str
    mode_label: str
    tracking_mode: str
    meta_fields: list[str]
    meta_rows: list[dict[str, str]]
    event_fields: list[str]
    event_rows: list[dict[str, str]]
    sample_fields: list[str]
    sample_rows: list[dict[str, str]]
    summary_fields: list[str]
    summary_rows: list[dict[str, str]]


def find_finished_seed(target_task: str, mode_label: str) -> SessionBundle:
    meta_by_session: dict[str, tuple[Path, dict[str, str], list[str]]] = {}
    event_by_session: dict[str, tuple[Path, list[str], list[dict[str, str]]]] = {}
    sample_by_session: dict[str, tuple[Path, list[str], list[dict[str, str]]]] = {}
    summary_by_session: dict[str, tuple[Path, list[str], list[dict[str, str]]]] = {}

    for meta_path in LOG_DIR.glob("*_Meta.csv"):
        fields, rows = read_csv(meta_path)
        if not rows:
            continue
        row = rows[0]
        if row.get("SessionState") != "Finished":
            continue
        meta_by_session[row["SessionID"]] = (meta_path, row, fields)

    for event_path in LOG_DIR.glob("*_Event.csv"):
        fields, rows = read_csv(event_path)
        if rows:
            event_by_session[rows[0]["SessionID"]] = (event_path, fields, rows)

    for sample_path in LOG_DIR.glob("*_Sample.csv"):
        fields, rows = read_csv(sample_path)
        if rows:
            sample_by_session[rows[0]["SessionID"]] = (sample_path, fields, rows)

    for summary_path in LOG_DIR.glob("*_Summary.csv"):
        s_fields, s_rows = read_csv(summary_path)
        if not s_rows:
            continue
        session_id = s_rows[0]["SessionID"]
        summary_by_session[session_id] = (summary_path, s_fields, s_rows)

    for session_id, summary_info in summary_by_session.items():
        summary_path, s_fields, s_rows = summary_info
        meta_info = meta_by_session.get(session_id)
        if meta_info is None:
            continue
        event_info = event_by_session.get(session_id)
        sample_info = sample_by_session.get(session_id)
        if event_info is None or sample_info is None:
            continue

        meta_path, meta_row, m_fields = meta_info
        task_modes = {row["TaskMode"] for row in s_rows}
        if target_task not in task_modes:
            continue
        if meta_row.get("InputModeLabel") != mode_label:
            continue

        event_path, e_fields, e_rows = event_info
        sample_path, smp_fields, smp_rows = sample_info
        return SessionBundle(
            prefix=summary_path.name.replace("_Summary.csv", ""),
            session_id=session_id,
            mode_label=mode_label,
            tracking_mode=meta_row.get("TrackingMode", "Hands" if mode_label == "embodied" else "Controllers"),
            meta_fields=m_fields,
            meta_rows=[dict(r) for r in read_csv(meta_path)[1]],
            event_fields=e_fields,
            event_rows=e_rows,
            sample_fields=smp_fields,
            sample_rows=smp_rows,
            summary_fields=s_fields,
            summary_rows=s_rows,
        )

    raise RuntimeError(f"No finished seed found for task={target_task} mode={mode_label}")


def generate_measurement_profile(task: str, mode_label: str, configured_effect: str) -> tuple[int, str, str, float, float, list[float], list[float]]:
    metric_name = "SignedEndpointError" if task == "OpenLoop" else "BisectionError"
    metric_units = "cm"
    trial_count = 30 if task == "OpenLoop" else 20

    effect_delta_scale = {
        "None": 0.35,
        "Translation": 1.00,
        "Rotation": 0.80,
        "Skew": 0.90,
    }.get(configured_effect, 1.0)

    if task == "OpenLoop" and mode_label == "controller":
        baseline_mean = random.uniform(-1.0, 1.0)
        baseline_sd = random.uniform(1.0, 2.4)
        delta = random.choice([-1, 1]) * random.uniform(2.0, 6.0) * effect_delta_scale
        post_sd = baseline_sd * random.uniform(0.8, 1.2)
    elif task == "OpenLoop":
        baseline_mean = random.uniform(-1.5, 1.5)
        baseline_sd = random.uniform(1.8, 4.2)
        delta = random.choice([-1, 1]) * random.uniform(0.5, 3.5) * effect_delta_scale
        post_sd = baseline_sd * random.uniform(0.9, 1.35)
    elif task == "LineBisection" and mode_label == "controller":
        baseline_mean = random.uniform(-1.0, 1.5)
        baseline_sd = random.uniform(0.8, 1.8)
        delta = random.choice([-1, 1]) * random.uniform(0.4, 2.0) * effect_delta_scale
        post_sd = baseline_sd * random.uniform(0.85, 1.2)
    else:
        baseline_mean = random.uniform(-1.5, 1.5)
        baseline_sd = random.uniform(1.2, 2.6)
        delta = random.choice([-1, 1]) * random.uniform(0.3, 1.6) * effect_delta_scale
        post_sd = baseline_sd * random.uniform(0.95, 1.35)

    post_mean = baseline_mean + delta

    def sample_block(mean: float, sd: float, count: int) -> list[float]:
        values = []
        for _ in range(count):
            v = random.gauss(mean, sd)
            if random.random() < 0.06:
                v += random.choice([-1, 1]) * random.uniform(sd * 1.6, sd * 2.8)
            values.append(v)
        return values

    return (
        trial_count,
        metric_name,
        metric_units,
        baseline_mean,
        post_mean,
        sample_block(baseline_mean, baseline_sd, trial_count),
        sample_block(post_mean, post_sd, trial_count),
    )


def mean_and_sd(values: list[float]) -> tuple[float, float]:
    mean = sum(values) / max(len(values), 1)
    if len(values) <= 1:
        return mean, 0.0
    variance = sum((v - mean) ** 2 for v in values) / (len(values) - 1)
    return mean, math.sqrt(max(0.0, variance))


def rebuild_summary_rows(
    seed_fields: list[str],
    session_id: str,
    timestamp: datetime,
    task: str,
    mode_label: str,
    configured_effect: str,
    active_hand: str,
    tracking_mode: str,
    trial_count: int,
    metric_name: str,
    metric_units: str,
    baseline_values: list[float],
    post_values: list[float],
) -> list[dict[str, str]]:
    baseline_mean, baseline_sd = mean_and_sd(baseline_values)
    post_mean, post_sd = mean_and_sd(post_values)
    signed_delta = post_mean - baseline_mean
    magnitude = abs(signed_delta)
    normalized = magnitude / baseline_sd if baseline_sd > 0.0001 else None

    common = {
        "ActiveHand": active_hand,
        "AppliedEffectMode": "None",
        "ConfiguredEffectMode": configured_effect,
        "Email": "anonymous",
        "TrackingMode": tracking_mode,
        "XRBackend": "OpenXR",
        "SessionID": session_id,
        "TaskMode": task,
        "MetricName": metric_name,
        "MetricUnits": metric_units,
        "TrialCount": str(trial_count),
    }

    def make_row(block_type: str, summary_type: str, notes: str) -> dict[str, str]:
        row = {field: "NULL" for field in seed_fields}
        row.update(common)
        row["BlockType"] = block_type
        row["SummaryType"] = summary_type
        row["Timestamp"] = timestamp.strftime("%Y-%m-%d %H:%M:%S.%f")[:-2]
        row["Framecount"] = str(random.randint(5000, 50000))
        row["Notes"] = notes
        return row

    baseline_row = make_row("Baseline", "BlockMetric", f"Mean signed {task} error")
    baseline_row["PostValue"] = fmt_float(baseline_mean)
    baseline_row["PostSd"] = fmt_float(baseline_sd)

    post_row = make_row("Post", "BlockMetric", f"Mean signed {task} error")
    post_row["PostValue"] = fmt_float(post_mean)
    post_row["PostSd"] = fmt_float(post_sd)

    after_row = make_row("Post", "Aftereffect", f"Post minus baseline {task} error")
    after_row["BaselineValue"] = fmt_float(baseline_mean)
    after_row["BaselineSd"] = fmt_float(baseline_sd)
    after_row["PostValue"] = fmt_float(post_mean)
    after_row["PostSd"] = fmt_float(post_sd)
    after_row["SignedDelta"] = fmt_float(signed_delta)
    after_row["Magnitude"] = fmt_float(magnitude)
    after_row["NormalizedMagnitude"] = fmt_float(normalized)

    return [baseline_row, post_row, after_row]


def jitter_exposure_rows(rows: list[dict[str, str]]) -> None:
    for row in rows:
        event = row.get("Event", "")
        if event not in {"Mole Spawned", "Pointer Shoot", "Mole Hit", "Mole Missed"}:
            continue

        target_x = parse_float(row.get("TargetWorldX"))
        target_y = parse_float(row.get("TargetWorldY"))
        target_z = parse_float(row.get("TargetWorldZ"))
        if target_x is None or target_y is None or target_z is None:
            continue

        is_hit = event == "Mole Hit"
        radius = random.uniform(0.015, 0.085) if is_hit else random.uniform(0.12, 0.22)
        angle = random.uniform(-math.pi, math.pi)
        hit_x = target_x + math.cos(angle) * radius
        hit_y = target_y + math.sin(angle) * radius * 0.35
        hit_z = target_z

        for key, value in {
            "HitWorldX": hit_x,
            "HitWorldY": hit_y,
            "HitWorldZ": hit_z,
            "HitPositionWorldX": hit_x,
            "HitPositionWorldY": hit_y,
            "HitPositionWorldZ": hit_z,
            "HitDistanceMeters": radius,
        }.items():
            row[key] = fmt_float(value)

        row["IsHit"] = "1" if is_hit else "0"


def rebuild_event_rows(
    seed_rows: list[dict[str, str]],
    session_id: str,
    task: str,
    mode_label: str,
    tracking_mode: str,
    configured_effect: str,
    baseline_values: list[float],
    post_values: list[float],
    base_timestamp: datetime,
) -> list[dict[str, str]]:
    rows = [dict(row) for row in seed_rows]
    game_id = str(uuid.uuid4())
    active_hand = "Right"

    baseline_index = 0
    post_index = 0
    baseline_template = None
    post_template = None

    def append_missing_trials(block_type: str, template: dict[str, str] | None, values: list[float], start_index: int) -> None:
        nonlocal elapsed
        if template is None:
            return

        for idx in range(start_index, len(values)):
            row = dict(template)
            elapsed += 0.05
            row["SessionID"] = session_id
            row["GameId"] = game_id
            row["TrackingMode"] = tracking_mode
            row["XRBackend"] = "OpenXR"
            row["ActiveHand"] = active_hand
            row["Timestamp"] = (base_timestamp + timedelta(seconds=elapsed)).strftime("%Y-%m-%d %H:%M:%S.%f")[:-2]
            row["TaskMode"] = task
            row["BlockType"] = block_type
            row["TimeSinceLastEvent"] = "0.0500"
            row["TrialIndex"] = str(idx + 1)
            row["TrialsPerBlock"] = str(len(values))
            row["EffectMode"] = "None"

            value = values[idx]
            if task == "OpenLoop":
                row["SignedOffsetCm"] = fmt_float(value)
                row["ErrorCm"] = "NULL"
                row["HitWorldX"] = fmt_float(value / 100.0)
                row["HitWorldY"] = "0.0000"
                row["HitWorldZ"] = "2.0000"
                row["HitPositionWorldX"] = row["HitWorldX"]
                row["HitPositionWorldY"] = row["HitWorldY"]
                row["HitPositionWorldZ"] = row["HitWorldZ"]
                row["LineZMeters"] = "NULL"
                row["LineLengthMeters"] = "NULL"
            else:
                line_z = random.uniform(-0.10, 0.10)
                line_len = random.uniform(1.0, 1.25)
                row["SignedOffsetCm"] = "NULL"
                row["ErrorCm"] = fmt_float(value)
                row["LineZMeters"] = fmt_float(line_z)
                row["LineLengthMeters"] = fmt_float(line_len)
                row["HitWorldX"] = fmt_float(value / 100.0)
                row["HitWorldY"] = fmt_float(line_z)
                row["HitWorldZ"] = "2.0000"
                row["HitPositionWorldX"] = row["HitWorldX"]
                row["HitPositionWorldY"] = row["HitWorldY"]
                row["HitPositionWorldZ"] = row["HitWorldZ"]

            new_rows.append(row)

    elapsed = 0.0
    new_rows: list[dict[str, str]] = []
    for row in rows:
        row = dict(row)
        elapsed += max(parse_float(row.get("TimeSinceLastEvent")) or 0.0, 0.02)
        row["SessionID"] = session_id
        row["GameId"] = game_id
        row["TrackingMode"] = tracking_mode
        row["XRBackend"] = "OpenXR"
        row["ActiveHand"] = active_hand
        row["Timestamp"] = (base_timestamp + timedelta(seconds=elapsed)).strftime("%Y-%m-%d %H:%M:%S.%f")[:-2]
        row["EffectMode"] = configured_effect if row.get("TaskMode") == "Exposure" else "None"

        if row.get("TaskMode") != "Exposure":
            row["TaskMode"] = task
        if row.get("SourceTaskMode") not in ("", "NULL", None):
            row["SourceTaskMode"] = task

        if row.get("Event") == "Trial Accepted":
            block = row.get("BlockType")
            if block == "Baseline":
                if baseline_template is None:
                    baseline_template = dict(row)
                if baseline_index >= len(baseline_values):
                    continue
                baseline_index += 1
                idx, value = baseline_index, baseline_values[baseline_index - 1]
            else:
                if post_template is None:
                    post_template = dict(row)
                if post_index >= len(post_values):
                    continue
                post_index += 1
                idx, value = post_index, post_values[post_index - 1]

            row["TrialIndex"] = str(idx)
            row["TrialsPerBlock"] = str(len(baseline_values if block == "Baseline" else post_values))

            if task == "OpenLoop":
                row["SignedOffsetCm"] = fmt_float(value)
                row["ErrorCm"] = "NULL"
                row["HitWorldX"] = fmt_float(value / 100.0)
                row["HitWorldY"] = "0.0000"
                row["HitWorldZ"] = "2.0000"
                row["HitPositionWorldX"] = row["HitWorldX"]
                row["HitPositionWorldY"] = row["HitWorldY"]
                row["HitPositionWorldZ"] = row["HitWorldZ"]
                row["LineZMeters"] = "NULL"
                row["LineLengthMeters"] = "NULL"
            else:
                line_z = random.uniform(-0.10, 0.10)
                line_len = random.uniform(1.0, 1.25)
                row["SignedOffsetCm"] = "NULL"
                row["ErrorCm"] = fmt_float(value)
                row["LineZMeters"] = fmt_float(line_z)
                row["LineLengthMeters"] = fmt_float(line_len)
                row["HitWorldX"] = fmt_float(value / 100.0)
                row["HitWorldY"] = fmt_float(line_z)
                row["HitWorldZ"] = "2.0000"
                row["HitPositionWorldX"] = row["HitWorldX"]
                row["HitPositionWorldY"] = row["HitWorldY"]
                row["HitPositionWorldZ"] = row["HitWorldZ"]

        elif row.get("Event") == "Block Started" and row.get("TaskMode") == task:
            row["TrialsPerBlock"] = str(len(baseline_values if row.get("BlockType") == "Baseline" else post_values))
        elif row.get("Event") == "Block Completed" and row.get("TaskMode") == task:
            block = row.get("BlockType")
            if block == "Baseline" and baseline_index < len(baseline_values):
                append_missing_trials("Baseline", baseline_template, baseline_values, baseline_index)
                baseline_index = len(baseline_values)
            if block == "Post" and post_index < len(post_values):
                append_missing_trials("Post", post_template, post_values, post_index)
                post_index = len(post_values)
            values = baseline_values if block == "Baseline" else post_values
            mean, sd = mean_and_sd(values)
            row["MeanCm"] = fmt_float(mean)
            row["SdCm"] = fmt_float(sd)
            row["TrialsPerBlock"] = str(len(values))
            if block == "Post":
                row["AfterEffectCm"] = fmt_float(mean_and_sd(post_values)[0] - mean_and_sd(baseline_values)[0])

        new_rows.append(row)

    jitter_exposure_rows(new_rows)
    return new_rows


def rebuild_sample_rows(
    seed_rows: list[dict[str, str]],
    session_id: str,
    task: str,
    mode_label: str,
    tracking_mode: str,
    configured_effect: str,
    base_timestamp: datetime,
) -> list[dict[str, str]]:
    rows = [dict(row) for row in seed_rows]
    elapsed = 0.0
    for index, row in enumerate(rows):
        elapsed += 0.02
        row["SessionID"] = session_id
        row["TrackingMode"] = tracking_mode
        row["XRBackend"] = "OpenXR"
        row["ActiveHand"] = "Right"
        if row.get("TaskMode") != "Exposure":
            row["TaskMode"] = task
        row["EffectMode"] = configured_effect if row.get("TaskMode") == "Exposure" else "None"
        row["Timestamp"] = (base_timestamp + timedelta(seconds=elapsed)).strftime("%Y-%m-%d %H:%M:%S.%f")[:-2]

        # Light position jitter so sessions are not exact clones.
        for key in [
            "PointerOriginX",
            "PointerOriginY",
            "PointerOriginZ",
            "RightControllerPosWorldX",
            "RightControllerPosWorldY",
            "RightControllerPosWorldZ",
            "LeftControllerPosWorldX",
            "LeftControllerPosWorldY",
            "LeftControllerPosWorldZ",
        ]:
            value = parse_float(row.get(key))
            if value is not None:
                row[key] = fmt_float(value + random.gauss(0.0, 0.003))

        row["Framecount"] = str(index)
    return rows


def rebuild_meta_row(seed_row: dict[str, str], session_id: str, mode_label: str, tracking_mode: str, base_timestamp: datetime, summary_rows: list[dict[str, str]]) -> dict[str, str]:
    row = dict(seed_row)
    row["SessionID"] = session_id
    row["InputModeLabel"] = mode_label
    row["TrackingMode"] = tracking_mode
    row["SessionState"] = "Finished"
    row["Timestamp"] = base_timestamp.strftime("%Y-%m-%d %H:%M:%S.%f")[:-2]
    row["Framecount"] = str(random.randint(12000, 65000))
    row["SessionDuration"] = fmt_float(random.uniform(55.0, 240.0))
    row["MainController"] = "Right Controller"
    row["RightControllerMain"] = "TRUE"

    exposure_radius = row.get("ExposureHitRadiusMeters", "")
    if not exposure_radius or exposure_radius == "NULL":
        row["ExposureHitRadiusMeters"] = "0.1120"
        row["ExposureHitRadiusCm"] = "11.2000"

    return row


def generate_session(mode_label: str, task: str, configured_effect: str, index: int, base_bundle: SessionBundle) -> None:
    tracking_mode = "Hands" if mode_label == "embodied" else "Controllers"
    session_id = str(uuid.uuid4())
    stamp = datetime.now() + timedelta(seconds=index)
    prefix = (
        f"synthetic_{mode_label}_{task.lower()}_{configured_effect.lower()}_"
        f"{index:02d}_{stamp.strftime('%Y_%m_%d_%H_%M_%S_%f')[:22]}"
    )

    trial_count, metric_name, metric_units, _, _, baseline_values, post_values = generate_measurement_profile(task, mode_label, configured_effect)

    event_rows = rebuild_event_rows(
        base_bundle.event_rows,
        session_id,
        task,
        mode_label,
        tracking_mode,
        configured_effect,
        baseline_values,
        post_values,
        stamp,
    )

    sample_rows = rebuild_sample_rows(
        base_bundle.sample_rows,
        session_id,
        task,
        mode_label,
        tracking_mode,
        configured_effect,
        stamp,
    )

    summary_rows = rebuild_summary_rows(
        base_bundle.summary_fields,
        session_id,
        stamp,
        task,
        mode_label,
        configured_effect,
        "Right",
        tracking_mode,
        trial_count,
        metric_name,
        metric_units,
        baseline_values,
        post_values,
    )

    meta_row = rebuild_meta_row(base_bundle.meta_rows[0], session_id, mode_label, tracking_mode, stamp, summary_rows)

    write_csv(OUTPUT_DIR / f"{prefix}_Meta.csv", base_bundle.meta_fields, [meta_row])
    write_csv(OUTPUT_DIR / f"{prefix}_Event.csv", base_bundle.event_fields, event_rows)
    write_csv(OUTPUT_DIR / f"{prefix}_Sample.csv", base_bundle.sample_fields, sample_rows)
    write_csv(OUTPUT_DIR / f"{prefix}_Summary.csv", base_bundle.summary_fields, summary_rows)


def main() -> None:
    random.seed(23)
    if OUTPUT_DIR.exists():
        shutil.rmtree(OUTPUT_DIR)
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    embodied_base = find_finished_seed("OpenLoop", "embodied")
    controller_base = find_finished_seed("LineBisection", "controller")

    count_per_subgroup = 5
    combinations = [
        ("embodied", "OpenLoop", embodied_base),
        ("controller", "OpenLoop", controller_base),
        ("embodied", "LineBisection", embodied_base),
        ("controller", "LineBisection", controller_base),
    ]
    configured_effects = ["None", "Translation", "Rotation", "Skew"]

    session_counter = 0
    for mode_label, task, base in combinations:
        for configured_effect in configured_effects:
            for index in range(1, count_per_subgroup + 1):
                session_counter += 1
                generate_session(mode_label, task, configured_effect, session_counter, base)

    print(f"Generated synthetic sessions in {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
