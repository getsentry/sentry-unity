#!/usr/bin/env python3
"""Summarizes a screenshot performance harness run.

Usage: python3 scripts/screenshot-perf-report.py <run directory> [<baseline run directory>]

Every configuration is compared against the run's `baseline` configuration (screenshots disabled). Each metric is
computed per round and reported as the median across rounds, followed by the half-range spread across rounds. When
a second run directory is passed, its configurations are listed alongside for comparison, e.g. to compare SDK versions.
"""

import csv
import statistics
import sys
from collections import defaultdict
from pathlib import Path


def percentile(values, p):
    if not values:
        return float("nan")
    ordered = sorted(values)
    index = min(len(ordered) - 1, max(0, round(p / 100 * (len(ordered) - 1))))
    return ordered[index]


SEGMENTS = ("before_processors_ms", "processors_ms", "after_processors_ms")


def median(values):
    return statistics.median(values) if values else float("nan")


def mean(values):
    return statistics.fmean(values) if values else float("nan")


def load(run_dir):
    run_dir = Path(run_dir)
    # config -> round -> phase -> rows
    frames = defaultdict(lambda: defaultdict(lambda: defaultdict(list)))
    with open(run_dir / "frames.csv", newline="") as f:
        for row in csv.DictReader(f):
            frames[row["config"]][row["round"]][row["phase"]].append(
                {k: float(v) for k, v in row.items() if k not in ("config", "round", "phase")})

    # config -> round -> (phase, thread) -> ms, and the same keyed with the segment name appended
    captures = defaultdict(lambda: defaultdict(lambda: defaultdict(list)))
    with open(run_dir / "captures.csv", newline="") as f:
        for row in csv.DictReader(f):
            key = (row["phase"], row["thread"])
            captures[row["config"]][row["round"]][key].append(float(row["ms"]))
            for segment in SEGMENTS:
                value = float(row.get(segment) or -1)
                if value >= 0:
                    captures[row["config"]][row["round"]][key + (segment,)].append(value)

    # config -> round -> (gfx MB, managed heap MB)
    memory = defaultdict(dict)
    with open(run_dir / "memory.csv", newline="") as f:
        for row in csv.DictReader(f):
            memory[row["config"]][row["round"]] = (float(row["gfx_used_mb"]), float(row["gc_used_mb"]))

    # config -> round -> encode ms on the SDK's worker, and sent events as (has screenshot, screenshot bytes)
    encodes = defaultdict(lambda: defaultdict(list))
    sent = defaultdict(lambda: defaultdict(list))
    if (run_dir / "encodes.csv").exists():
        with open(run_dir / "encodes.csv", newline="") as f:
            for row in csv.DictReader(f):
                if row["phase"] == "periodic":
                    encodes[row["config"]][row["round"]].append(float(row["ms"]))
        with open(run_dir / "sent.csv", newline="") as f:
            for row in csv.DictReader(f):
                sent[row["config"]][row["round"]].append((int(row["has_screenshot"]), int(row["screenshot_bytes"])))
    captures["__encodes__"] = encodes
    captures["__sent__"] = sent

    device = {}
    with open(run_dir / "device.csv", newline="") as f:
        for line in f:
            key, _, value = line.strip().partition(",")
            device[key] = value

    return frames, captures, memory, device


def column(rows, name, only_positive=False):
    values = [r[name] for r in rows]
    # Negative values mark frames without timing data
    return [v for v in values if v > 0] if only_positive else [v for v in values if v >= 0]


def summarize_round(frames, captures, memory_mb, encodes=(), sent=()):
    idle = frames["idle"]
    periodic = frames["periodic"]
    burst_main = frames["burst-main"]
    burst_background = frames["burst-background"]

    return {
        "idle_cpu_main_mean": mean(column(idle, "cpu_main_ms")),
        "idle_cpu_main_p99": percentile(column(idle, "cpu_main_ms"), 99),
        "idle_dt_p99": percentile(column(idle, "dt_ms"), 99),
        "idle_gpu_mean": mean(column(idle, "gpu_ms", only_positive=True)),
        "idle_gc_per_frame": mean(column(idle, "gc_bytes")),
        "refresh_mean": mean(column(idle, "refresh_ms", only_positive=True)),
        "refresh_count": len(column(idle, "refresh_ms", only_positive=True)),
        "readback_mean": mean(column(idle, "readback_ms", only_positive=True)),
        "readback_max": max(column(idle, "readback_ms", only_positive=True), default=float("nan")),
        "idle_frames": len(idle),
        "periodic_dt_p99": percentile(column(periodic, "dt_ms"), 99),
        "periodic_dt_max": max(column(periodic, "dt_ms"), default=float("nan")),
        "periodic_cpu_main_max": max(column(periodic, "cpu_main_ms"), default=float("nan")),
        "capture_main_median": statistics.median(captures[("periodic", "main")] or [float("nan")]),
        "capture_main_p95": percentile(captures[("periodic", "main")], 95),
        "capture_background_median": statistics.median(captures[("periodic", "background")] or [float("nan")]),
        **{f"main_{segment}": median(captures[("periodic", "main", segment)]) for segment in SEGMENTS},
        "burst_main_total": sum(captures[("burst-main", "main")]),
        "burst_main_dt_max": max(column(burst_main, "dt_ms"), default=float("nan")),
        "burst_background_dt_max": max(column(burst_background, "dt_ms"), default=float("nan")),
        "worker_encode_median": median(list(encodes)),
        "worker_encode_count": len(encodes) if encodes else float("nan"),
        "sent_events": len(sent) if sent else float("nan"),
        "sent_with_screenshot_pct": 100 * sum(1 for has, size in sent if has and size > 0) / len(sent) if sent else float("nan"),
        "sent_screenshot_kb": median([size / 1024 for has, size in sent if has and size > 0]),
        "gfx_mb": memory_mb[0],
        "managed_mb": memory_mb[1],
    }


def summarize(frames, captures, memory, config):
    """Median and half-range spread of every metric across rounds."""
    encodes = captures["__encodes__"][config]
    sent = captures["__sent__"][config]
    rounds = [summarize_round(frames[config][r], captures[config][r], memory[config].get(r, (float("nan"), float("nan"))),
                              encodes.get(r, []), sent.get(r, []))
              for r in sorted(frames[config].keys())]
    result = {}
    for key in rounds[0]:
        values = [r[key] for r in rounds if r[key] == r[key]]
        result[key] = (statistics.median(values), (max(values) - min(values)) / 2) if values else (float("nan"), 0.0)
    result["rounds"] = (len(rounds), 0.0)
    return result


ROWS = [
    ("Ongoing (idle, no errors)", None, None),
    ("CPU main thread (busy), mean", "idle_cpu_main_mean", "ms"),
    ("CPU main thread (busy), p99", "idle_cpu_main_p99", "ms"),
    ("Frame time p99", "idle_dt_p99", "ms"),
    ("GPU / frame, mean", "idle_gpu_mean", "ms"),
    ("GC alloc / frame, mean", "idle_gc_per_frame", "B"),
    ("Refresh, mean per call", "refresh_mean", "ms"),
    ("Refresh calls", "refresh_count", ""),
    ("Readback copy, mean", "readback_mean", "ms"),
    ("Readback copy, max", "readback_max", "ms"),
    ("Gfx memory", "gfx_mb", "MB"),
    ("Managed heap", "managed_mb", "MB"),
    ("Per error (1/s)", None, None),
    ("Capture on main thread, median", "capture_main_median", "ms"),
    ("Capture on main thread, p95", "capture_main_p95", "ms"),
    ("Capture on background, median", "capture_background_median", "ms"),
    ("  main: before processors, median", "main_before_processors_ms", "ms"),
    # Covers the screenshot encode. Per-frame marker values are not used for it, since the profiler only partially
    # attributes work on background threads to frames.
    ("  main: processors, median", "main_processors_ms", "ms"),
    ("  main: after processors, median", "main_after_processors_ms", "ms"),
    ("Frame time p99", "periodic_dt_p99", "ms"),
    ("Frame time max", "periodic_dt_max", "ms"),
    ("CPU main thread (busy), max", "periodic_cpu_main_max", "ms"),
    ("Worker (deferred encode)", None, None),
    ("Encode on worker, median", "worker_encode_median", "ms"),
    ("Encodes during periodic phase", "worker_encode_count", ""),
    ("Events sent", "sent_events", ""),
    ("Sent events with screenshot", "sent_with_screenshot_pct", "%"),
    ("Screenshot size, median", "sent_screenshot_kb", "KB"),
    ("Burst (50 errors)", None, None),
    ("Main thread: capture total", "burst_main_total", "ms"),
    ("Main thread: worst frame", "burst_main_dt_max", "ms"),
    ("Background: worst frame", "burst_background_dt_max", "ms"),
]


def fmt(value, unit):
    if value != value:  # NaN
        return "-"
    if unit == "B":
        return f"{value:,.0f}"
    if unit == "":
        return f"{value:.0f}"
    return f"{value:.2f}"


def fmt_spread(spread, unit):
    return f" ±{fmt(spread, unit)}" if spread > 0 and unit not in ("",) else ""


def fmt_delta(value, spread, baseline, unit):
    if value != value or baseline != baseline:
        return fmt(value, unit) + fmt_spread(spread, unit)
    delta = value - baseline
    return f"{fmt(value, unit)} ({'+' if delta >= 0 else ''}{fmt(delta, unit)}){fmt_spread(spread, unit)}"


def print_report(run_dir, other_dir=None):
    frames, captures, memory, device = load(run_dir)
    configs = [c for c in frames.keys()]
    summaries = {c: summarize(frames, captures, memory, c) for c in configs}
    columns = [(c, summaries[c]) for c in configs]

    if other_dir:
        o_frames, o_captures, o_memory, _ = load(other_dir)
        columns += [(f"{c} [cmp]", summarize(o_frames, o_captures, o_memory, c)) for c in o_frames.keys() if c != "baseline"]

    baseline = summaries.get("baseline")

    print(f"Device: {device.get('device_model')} | {device.get('os')} | {device.get('graphics_api')} | "
          f"screen {device.get('screen')} | async readback: {device.get('async_gpu_readback')} | "
          f"Unity {device.get('unity')} | development build: {device.get('development_build')}")
    print(f"Median of {summaries[configs[0]]['rounds'][0]} rounds. Values in parentheses are deltas against `baseline` "
          "(screenshots disabled), ± is the half-range across rounds.\n")

    header = "| Metric | " + " | ".join(name for name, _ in columns) + " |"
    print(header)
    print("|" + "---|" * (len(columns) + 1))
    for label, key, unit in ROWS:
        if key is None:
            print(f"| **{label}** |" + " |" * len(columns))
            continue
        cells = []
        for name, summary in columns:
            value, spread = summary[key]
            if baseline and name != "baseline":
                cells.append(fmt_delta(value, spread, baseline[key][0], unit))
            else:
                cells.append(fmt(value, unit) + fmt_spread(spread, unit))
        print(f"| {label} ({unit or 'n'}) | " + " | ".join(cells) + " |")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)
    print_report(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else None)
