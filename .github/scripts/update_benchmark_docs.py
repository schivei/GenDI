#!/usr/bin/env python3

from __future__ import annotations

import html
import os
import re
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path


START_MARKER = "<!-- benchmark-ci:start -->"
END_MARKER = "<!-- benchmark-ci:end -->"


@dataclass(frozen=True)
class BenchmarkRow:
    method: str
    mean: str
    allocated: str


def _parse_numeric(value: str) -> float:
    match = re.search(r"[-+]?[0-9]*\.?[0-9]+", value.replace(",", ""))
    if match is None:
        raise ValueError(f"Could not parse numeric value from: {value}")
    return float(match.group(0))


def _parse_short_run_rows(report_path: Path) -> list[BenchmarkRow]:
    content = report_path.read_text(encoding="utf-8")
    rows: list[BenchmarkRow] = []

    for line in content.splitlines():
        if not line.startswith("|"):
            continue

        columns = [html.unescape(cell.strip()) for cell in line.strip().strip("|").split("|")]
        if len(columns) < 12:
            continue
        if columns[0] in {"Method", "----------------------------------------------------"}:
            continue

        method = columns[0].strip("'")
        job = columns[1]
        if job != "ShortRun":
            continue

        rows.append(BenchmarkRow(method=method, mean=columns[5], allocated=columns[-1]))

    if len(rows) < 4:
        raise ValueError("Expected at least four ShortRun rows in benchmark report.")

    return rows


def _find_row(rows: list[BenchmarkRow], fragment: str) -> BenchmarkRow:
    for row in rows:
        if fragment in row.method:
            return row
    raise ValueError(f"Could not find benchmark row containing: {fragment}")


def _build_ci_section(rows: list[BenchmarkRow]) -> str:
    manual = _find_row(rows, "Manual registration")
    constructor = _find_row(rows, "constructor injection")
    property_injection = _find_row(rows, "property injection")
    reflection = _find_row(rows, "Reflection registration")

    manual_mean = _parse_numeric(manual.mean)
    constructor_mean = _parse_numeric(constructor.mean)
    property_mean = _parse_numeric(property_injection.mean)
    reflection_mean = _parse_numeric(reflection.mean)

    manual_alloc = _parse_numeric(manual.allocated)
    reflection_alloc = _parse_numeric(reflection.allocated)

    constructor_delta = ((constructor_mean / manual_mean) - 1.0) * 100.0
    property_delta = ((property_mean / manual_mean) - 1.0) * 100.0
    reflection_slowdown = reflection_mean / manual_mean
    reflection_alloc_factor = reflection_alloc / manual_alloc

    run_number = os.environ.get("GITHUB_RUN_NUMBER", "local")
    run_url = os.environ.get("BENCHMARK_RUN_URL", "").strip()
    now_utc = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC")

    run_line = (
        f"_Updated by CI run #{run_number} on {now_utc}_"
        if not run_url
        else f"_Updated by [CI run #{run_number}]({run_url}) on {now_utc}_"
    )

    return (
        f"{START_MARKER}\n"
        f"{run_line}\n\n"
        "| Method | Mean | Allocated |\n"
        "|---|---:|---:|\n"
        f"| {manual.method} | {manual.mean} | {manual.allocated} |\n"
        f"| {constructor.method} | {constructor.mean} | {constructor.allocated} |\n"
        f"| {property_injection.method} | {property_injection.mean} | {property_injection.allocated} |\n"
        f"| {reflection.method} | {reflection.mean} | {reflection.allocated} |\n\n"
        "### CI analysis\n\n"
        f"- GenDI constructor injection is **{constructor_delta:+.1f}%** versus manual registration.\n"
        f"- GenDI property injection is **{property_delta:+.1f}%** versus manual registration.\n"
        f"- Reflection scanning remains the outlier at **~{reflection_slowdown:.1f}x slower** "
        f"and **~{reflection_alloc_factor:.1f}x higher allocation** than manual registration.\n"
        "- Compatibility note: this benchmark compares manual and generated registrations against a reflection "
        "scanner baseline; as documented below, reflection scanning is not suitable for trimming/NativeAOT "
        "scenarios, while manual and GenDI-generated registrations remain the supported path.\n"
        f"{END_MARKER}"
    )


def _replace_marker_block(document_path: Path, replacement_block: str) -> None:
    content = document_path.read_text(encoding="utf-8")
    pattern = re.compile(
        re.escape(START_MARKER) + r".*?" + re.escape(END_MARKER),
        flags=re.DOTALL,
    )

    if not pattern.search(content):
        raise ValueError(f"Markers not found in {document_path}")

    updated = pattern.sub(replacement_block, content, count=1)
    document_path.write_text(updated, encoding="utf-8")


def main() -> None:
    repo_root = Path(__file__).resolve().parents[2]
    report_path = repo_root / "BenchmarkDotNet.Artifacts" / "results" / "GenDI.Benchmarks.StartupRegistrationBenchmarks-report-github.md"
    docs_paths = [
        repo_root / "docs" / "BENCHMARKS.md",
        repo_root / "website" / "docs" / "advanced" / "benchmarks.md",
    ]

    rows = _parse_short_run_rows(report_path)
    block = _build_ci_section(rows)

    for path in docs_paths:
        _replace_marker_block(path, block)

    print("Updated benchmark sections in documentation files.")


if __name__ == "__main__":
    main()
