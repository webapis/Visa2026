"""Sync user-manual/assets into docs/assets for MkDocs without live-reload loops."""

from __future__ import annotations

import os
import shutil
from pathlib import Path


def _assets_paths(config) -> tuple[Path, Path]:
    manual_root = Path(config["config_file_path"]).resolve().parent
    return manual_root / "assets", manual_root / "docs" / "assets"


def _should_copy_file(source: Path, target: Path) -> bool:
    if not target.is_file():
        return True

    source_stat = source.stat()
    target_stat = target.stat()
    if source_stat.st_size != target_stat.st_size:
        return True

    # Skip unchanged files so mkdocs serve does not see docs/assets churn.
    return source_stat.st_mtime_ns > target_stat.st_mtime_ns


def _sync_assets(config) -> None:
    source, target = _assets_paths(config)
    if not source.is_dir():
        return

    for src_file in source.rglob("*"):
        if not src_file.is_file():
            continue

        rel = src_file.relative_to(source)
        dst_file = target / rel
        if not _should_copy_file(src_file, dst_file):
            continue

        dst_file.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(src_file, dst_file)


def on_pre_build(config, **kwargs) -> None:
    if os.environ.get("MANUAL_MEDIA_BASE_URL", "").strip():
        return
    _sync_assets(config)
