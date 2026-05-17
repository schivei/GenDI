#!/usr/bin/env python3

from __future__ import annotations

import html
import json
import os
import re
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path


START_MARKER = "<!-- benchmark-ci:start -->"
END_MARKER = "<!-- benchmark-ci:end -->"
README_SALES_START_MARKER = "<!-- benchmark-sales:start -->"
README_SALES_END_MARKER = "<!-- benchmark-sales:end -->"
HOMEPAGE_SALES_START_MARKER = "/* benchmark-sales:start */"
HOMEPAGE_SALES_END_MARKER = "/* benchmark-sales:end */"
MARKETING_THRESHOLD_PERCENT = 5.0


@dataclass(frozen=True)
class BenchmarkRow:
    method: str
    mean: str
    allocated: str


@dataclass(frozen=True)
class BenchmarkSummary:
    manual: BenchmarkRow
    constructor: BenchmarkRow
    property_injection: BenchmarkRow
    reflection: BenchmarkRow
    constructor_delta: float
    property_delta: float
    reflection_slowdown: float
    reflection_alloc_factor: float

    @property
    def best_gendi_variant(self) -> tuple[str, float]:
        improvements = {
            "constructor injection": max(0.0, -self.constructor_delta),
            "property injection": max(0.0, -self.property_delta),
        }
        return max(improvements.items(), key=lambda item: item[1])


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


def _build_summary(rows: list[BenchmarkRow]) -> BenchmarkSummary:
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

    return BenchmarkSummary(
        manual=manual,
        constructor=constructor,
        property_injection=property_injection,
        reflection=reflection,
        constructor_delta=constructor_delta,
        property_delta=property_delta,
        reflection_slowdown=reflection_slowdown,
        reflection_alloc_factor=reflection_alloc_factor,
    )


def _build_ci_section(summary: BenchmarkSummary) -> str:

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
        f"| {summary.manual.method} | {summary.manual.mean} | {summary.manual.allocated} |\n"
        f"| {summary.constructor.method} | {summary.constructor.mean} | {summary.constructor.allocated} |\n"
        f"| {summary.property_injection.method} | {summary.property_injection.mean} | {summary.property_injection.allocated} |\n"
        f"| {summary.reflection.method} | {summary.reflection.mean} | {summary.reflection.allocated} |\n\n"
        "### CI analysis\n\n"
        f"- GenDI constructor injection is **{summary.constructor_delta:+.1f}%** versus manual registration.\n"
        f"- GenDI property injection is **{summary.property_delta:+.1f}%** versus manual registration.\n"
        f"- Reflection scanning remains the outlier at **~{summary.reflection_slowdown:.1f}x slower** "
        f"and **~{summary.reflection_alloc_factor:.1f}x higher allocation** than manual registration.\n"
        "- Compatibility note: this benchmark compares manual and generated registrations against a reflection "
        "scanner baseline; as documented below, reflection scanning is not suitable for trimming/NativeAOT "
        "scenarios, while manual and GenDI-generated registrations remain the supported path.\n"
        f"{END_MARKER}"
    )


def _build_readme_sales_section(summary: BenchmarkSummary) -> str:
    best_variant, best_improvement = summary.best_gendi_variant
    if best_improvement <= MARKETING_THRESHOLD_PERCENT:
        return f"{README_SALES_START_MARKER}\n{README_SALES_END_MARKER}"

    benchmark_link = "./docs/BENCHMARKS.md"
    return (
        f"{README_SALES_START_MARKER}\n"
        "## Why teams adopt GenDI\n\n"
        f"> Latest CI benchmarks show **GenDI {best_variant} is {best_improvement:.1f}% faster than manual registration**.\n\n"
        "- **Move faster**: replace repetitive `AddScoped<>` / `AddSingleton<>` wiring with compile-time generation.\n"
        "- **Start faster**: keep registrations out of reflection scanners and on the fast path for startup.\n"
        "- **Deploy safely**: stay ready for trimming and NativeAOT without giving up readable DI code.\n"
        "- **Scale cleanly**: property injection and generated factories keep large services maintainable.\n\n"
        f"[See the latest benchmark details]({benchmark_link})\n"
        f"{README_SALES_END_MARKER}"
    )


def _build_benchmark_sales_section(summary: BenchmarkSummary) -> str:
    best_variant, best_improvement = summary.best_gendi_variant
    if best_improvement <= MARKETING_THRESHOLD_PERCENT:
        return f"{README_SALES_START_MARKER}\n{README_SALES_END_MARKER}"

    return (
        f"{README_SALES_START_MARKER}\n"
        "## Why this benchmark matters\n\n"
        f"> GenDI {best_variant} is currently **{best_improvement:.1f}% faster than manual registration** in the latest CI snapshot.\n\n"
        "- You get compile-time DI registration without paying a startup penalty for reflection scanning.\n"
        "- You remove repetitive manual wiring while keeping generated code explicit and reviewable.\n"
        "- You stay aligned with trimming and NativeAOT-friendly deployment paths.\n"
        f"{README_SALES_END_MARKER}"
    )


def _build_homepage_sales_module(summary: BenchmarkSummary) -> str:
    best_variant, best_improvement = summary.best_gendi_variant
    sales_pitch: dict[str, object] | None
    if best_improvement <= MARKETING_THRESHOLD_PERCENT:
        sales_pitch = None
    else:
        sales_pitch = {
            "eyebrow": "Latest CI benchmark advantage",
            "title": f"GenDI is currently {best_improvement:.1f}% faster than manual registration",
            "description": (
                f"The fastest generated path in CI is GenDI {best_variant}. "
                "That means you can remove DI boilerplate and still improve startup registration performance."
            ),
            "points": [
                "Eliminate repetitive service-registration code from startup files.",
                "Avoid reflection-based scanning costs during cold start.",
                "Keep the generated path friendly to trimming and NativeAOT deployments.",
            ],
            "ctaLabel": "See benchmark details",
            "ctaHref": "/docs/advanced/benchmarks",
        }

    sales_pitch_literal = "null" if sales_pitch is None else json.dumps(sales_pitch, indent=2)
    return (
        f"{HOMEPAGE_SALES_START_MARKER}\n"
        f"const benchmarkSalesPitch = {sales_pitch_literal};\n"
        f"{HOMEPAGE_SALES_END_MARKER}"
    )


def _replace_marker_block(
    document_path: Path,
    replacement_block: str,
    start_marker: str,
    end_marker: str,
) -> None:
    content = document_path.read_text(encoding="utf-8")
    pattern = re.compile(
        re.escape(start_marker) + r".*?" + re.escape(end_marker),
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
    readme_path = repo_root / "README.md"
    homepage_data_path = repo_root / "website" / "src" / "data" / "benchmarkSalesPitch.js"

    rows = _parse_short_run_rows(report_path)
    summary = _build_summary(rows)
    block = _build_ci_section(summary)
    readme_sales_block = _build_readme_sales_section(summary)
    benchmark_sales_block = _build_benchmark_sales_section(summary)
    homepage_sales_block = _build_homepage_sales_module(summary)

    for path in docs_paths:
        _replace_marker_block(path, block, START_MARKER, END_MARKER)
        _replace_marker_block(
            path,
            benchmark_sales_block,
            README_SALES_START_MARKER,
            README_SALES_END_MARKER,
        )

    _replace_marker_block(
        readme_path,
        readme_sales_block,
        README_SALES_START_MARKER,
        README_SALES_END_MARKER,
    )
    _replace_marker_block(
        homepage_data_path,
        homepage_sales_block,
        HOMEPAGE_SALES_START_MARKER,
        HOMEPAGE_SALES_END_MARKER,
    )

    print("Updated benchmark sections and dynamic sales highlights.")


if __name__ == "__main__":
    main()
