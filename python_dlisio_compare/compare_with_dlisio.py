#!/usr/bin/env python3
"""
Сравнение данных LIS между:
1) нашей реализацией Lis.Core (через C# CLI-дампер),
2) python-библиотекой dlisio.
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Dict, List, Any, Set

from dlisio import lis


@dataclass
class DiffReport:
    errors: List[str] = field(default_factory=list)
    warnings: List[str] = field(default_factory=list)
    infos: List[str] = field(default_factory=list)

    def ok(self) -> bool:
        return len(self.errors) == 0


def load_core_summary(repo_root: Path, lis_path: Path, out_json: Path) -> Dict[str, Any]:
    cmd = [
        "dotnet",
        "run",
        "--project",
        str(repo_root / "src" / "Lis.Compare.Cli" / "Lis.Compare.Cli.csproj"),
        "--",
        str(lis_path),
        str(out_json),
    ]
    subprocess.run(cmd, check=True, cwd=str(repo_root))
    return json.loads(out_json.read_text(encoding="utf-8"))


def load_dlisio_summary(lis_path: Path) -> Dict[str, Any]:
    logical_files: List[Dict[str, Any]] = []

    with lis.load(str(lis_path)) as files:
        for file_index, logical_file in enumerate(files):
            header = logical_file.header()
            trailer = logical_file.trailer()

            text_count = (
                len(logical_file.operator_command_inputs())
                + len(logical_file.operator_response_inputs())
                + len(logical_file.system_outputs_to_operator())
                + len(logical_file.flic_comment())
            )

            dfsr_list = []
            curve_counts: Dict[str, int] = {}
            formatspecs = logical_file.data_format_specs()

            for dfsr_index, dfsr in enumerate(formatspecs):
                sample_rates: Set[int] = set()
                channels = []
                for spec in dfsr.specs:
                    mnem = str(spec.mnemonic).strip()
                    units = str(spec.units).strip()
                    samples = int(spec.samples)
                    reprc = int(spec.reprc)
                    sample_rates.add(samples)
                    channels.append(
                        {
                            "Mnemonic": mnem,
                            "Units": units,
                            "Samples": samples,
                            "RepresentationCode": reprc,
                        }
                    )

                dfsr_list.append(
                    {
                        "Index": dfsr_index,
                        "Subtype": int(dfsr.spec_block_subtype),
                        "SpecCount": len(dfsr.specs),
                        "SampleRates": sorted(sample_rates),
                        "Channels": channels,
                    }
                )

                # Считаем количество сэмплов по кривым для каждого sample_rate.
                for rate in sorted(sample_rates):
                    data = lis.curves(logical_file, dfsr, sample_rate=rate, strict=False)
                    for field_name in data.dtype.names:
                        key = str(field_name).strip()
                        curve_counts[key] = curve_counts.get(key, 0) + int(len(data))

            curves = [
                {"Mnemonic": k, "SampleCount": v}
                for k, v in sorted(curve_counts.items(), key=lambda x: x[0].lower())
            ]

            logical_files.append(
                {
                    "Index": file_index,
                    "FileHeaderName": None if header is None else str(header.file_name).strip(),
                    "FileTrailerName": None if trailer is None else str(trailer.file_name).strip(),
                    "TextRecordCount": text_count,
                    "DfsrCount": len(formatspecs),
                    "FrameCount": 0,  # dlisio curves API не оперирует нашим понятием frame-list
                    "CurveCount": len(curves),
                    "Curves": curves,
                    "Dfsrs": dfsr_list,
                }
            )

    return {
        "LogicalFileCount": len(logical_files),
        "LogicalFiles": logical_files,
    }


def index_channels(dfsr: Dict[str, Any]) -> Dict[str, Dict[str, Any]]:
    result: Dict[str, Dict[str, Any]] = {}
    for ch in dfsr.get("Channels", []):
        key = str(ch.get("Mnemonic", "")).strip().upper()
        if key:
            result[key] = ch
    return result


def compare_summaries(core: Dict[str, Any], py: Dict[str, Any]) -> DiffReport:
    report = DiffReport()

    core_files = core.get("LogicalFiles", [])
    py_files = py.get("LogicalFiles", [])

    if int(core.get("LogicalFileCount", -1)) != int(py.get("LogicalFileCount", -2)):
        report.errors.append(
            f"Количество logical files отличается: core={core.get('LogicalFileCount')} py={py.get('LogicalFileCount')}"
        )

    file_count = min(len(core_files), len(py_files))
    for i in range(file_count):
        cf = core_files[i]
        pf = py_files[i]

        if (cf.get("FileHeaderName") or "") != (pf.get("FileHeaderName") or ""):
            report.warnings.append(
                f"LF[{i}] отличается FileHeaderName: core='{cf.get('FileHeaderName')}' py='{pf.get('FileHeaderName')}'"
            )

        if int(cf.get("DfsrCount", -1)) != int(pf.get("DfsrCount", -2)):
            report.errors.append(
                f"LF[{i}] отличается DfsrCount: core={cf.get('DfsrCount')} py={pf.get('DfsrCount')}"
            )

        core_dfsrs = cf.get("Dfsrs", [])
        py_dfsrs = pf.get("Dfsrs", [])
        dfsr_count = min(len(core_dfsrs), len(py_dfsrs))
        for d in range(dfsr_count):
            cd = core_dfsrs[d]
            pd = py_dfsrs[d]

            core_rates = list(cd.get("SampleRates", []))
            py_rates = list(pd.get("SampleRates", []))
            if core_rates != py_rates:
                report.warnings.append(
                    f"LF[{i}] DFSR[{d}] sample_rates отличаются: core={core_rates} py={py_rates}"
                )

            core_channels = index_channels(cd)
            py_channels = index_channels(pd)
            core_keys = set(core_channels.keys())
            py_keys = set(py_channels.keys())

            missing_in_py = sorted(core_keys - py_keys)
            missing_in_core = sorted(py_keys - core_keys)

            if missing_in_py:
                report.errors.append(
                    f"LF[{i}] DFSR[{d}] каналы отсутствуют в dlisio: {missing_in_py}"
                )
            if missing_in_core:
                report.errors.append(
                    f"LF[{i}] DFSR[{d}] каналы отсутствуют в Lis.Core: {missing_in_core}"
                )

            for key in sorted(core_keys & py_keys):
                cch = core_channels[key]
                pch = py_channels[key]
                if int(cch.get("Samples", -1)) != int(pch.get("Samples", -2)):
                    report.warnings.append(
                        f"LF[{i}] DFSR[{d}] канал {key}: samples core={cch.get('Samples')} py={pch.get('Samples')}"
                    )

                if int(cch.get("RepresentationCode", -1)) != int(pch.get("RepresentationCode", -2)):
                    report.warnings.append(
                        f"LF[{i}] DFSR[{d}] канал {key}: reprc core={cch.get('RepresentationCode')} py={pch.get('RepresentationCode')}"
                    )

        report.infos.append(
            f"LF[{i}] проверен: dfsr={cf.get('DfsrCount')} channels_total(core_curves)={cf.get('CurveCount')}"
        )

    return report


def main() -> int:
    parser = argparse.ArgumentParser(description="Сравнение LIS данных между Lis.Core и python+dlisio")
    parser.add_argument("lis_file", help="Путь к LIS-файлу")
    parser.add_argument(
        "--repo-root",
        default=str(Path(__file__).resolve().parents[1]),
        help="Корень репозитория (по умолчанию автоопределение)",
    )
    parser.add_argument(
        "--core-json",
        default="core_summary.json",
        help="Имя/путь JSON-файла summary от Lis.Core CLI",
    )
    args = parser.parse_args()

    lis_path = Path(args.lis_file).resolve()
    repo_root = Path(args.repo_root).resolve()
    out_json = Path(args.core_json).resolve()

    if not lis_path.exists():
        print(f"[ERROR] LIS файл не найден: {lis_path}", file=sys.stderr)
        return 2

    print("[INFO] Генерирую summary через Lis.Core...")
    core_summary = load_core_summary(repo_root, lis_path, out_json)

    print("[INFO] Генерирую summary через python+dlisio...")
    py_summary = load_dlisio_summary(lis_path)

    print("[INFO] Сравниваю...")
    report = compare_summaries(core_summary, py_summary)

    if report.infos:
        print("\n[INFO]")
        for line in report.infos:
            print(" -", line)

    if report.warnings:
        print("\n[WARN]")
        for line in report.warnings:
            print(" -", line)

    if report.errors:
        print("\n[ERROR]")
        for line in report.errors:
            print(" -", line)
        return 1

    print("\n[OK] Критических расхождений не найдено.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
