"""Rewrite guide media links when MANUAL_MEDIA_BASE_URL points at remote static storage."""

from __future__ import annotations

import os
import re

_ASSET_PATH_RE = re.compile(r"(?:\.\./)+assets/")


def _resolve_base_url() -> str:
    return os.environ.get("MANUAL_MEDIA_BASE_URL", "").strip().rstrip("/")


def on_config(config, **kwargs) -> None:
    base_url = _resolve_base_url()
    if base_url:
        print(f"[manual-media] Using remote base URL: {base_url}")


def on_page_markdown(markdown: str, page, config, **kwargs) -> str:
    base_url = _resolve_base_url()
    if not base_url:
        return markdown

    return _ASSET_PATH_RE.sub(f"{base_url}/", markdown)
