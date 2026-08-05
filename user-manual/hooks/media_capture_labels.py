"""Inject E2E media capture date/time into video captions and screenshot frames."""

from __future__ import annotations

import re
from datetime import datetime, timezone

_SCREENSHOT_IMAGE_RE = re.compile(
    r"(!\[[^\]]*\]\([^)]*assets/screenshots/[^)]+\.(?:png|jpg|jpeg|webp)\))",
    re.IGNORECASE,
)
_VIDEO_CAPTION_RE = re.compile(
    r'(<p class="visa-manual-video-caption">)(.*?)(</p>)',
    re.DOTALL | re.IGNORECASE,
)
_SCREENSHOT_TIP_RE = re.compile(
    r'(!!! tip "Screenshots"\s*\n)(.*?)(?=\n(?:##|!!!|\Z))',
    re.DOTALL,
)

_LOCALES = {
    "en": {
        "screenshot_tip": (
            "Images below are from the **English** application UI (version **{version}**). "
            "If your office uses Turkish, Turkmen, or Russian, the labels are translated but the steps are the same.\n\n"
            "**E2E capture:** {when_utc} UTC · run `{run_id}`"
        ),
        "video_caption": (
            "Recording from the training environment (test data). "
            "**E2E capture:** {when_utc} UTC · run `{run_id}`. The steps below match the video."
        ),
        "frame_caption": "Screenshot · E2E capture {when_utc} UTC · run `{run_id}`",
    },
    "tr": {
        "screenshot_tip": (
            "Görseller **İngilizce** arayüzden (sürüm **{version}**). "
            "Adımlar aynıdır.\n\n"
            "**E2E kaydı:** {when_utc} UTC · çalıştırma `{run_id}`"
        ),
        "video_caption": (
            "Kayıt eğitim ortamından (test verisi). "
            "**E2E kaydı:** {when_utc} UTC · çalıştırma `{run_id}`. Aşağıdaki adımlar videoyla aynıdır."
        ),
        "frame_caption": "Ekran görüntüsü · E2E kaydı {when_utc} UTC · çalıştırma `{run_id}`",
    },
    "tk": {
        "screenshot_tip": (
            "Suratlar **iňlis** interfeýsinden (wersiýa **{version}**).\n\n"
            "**E2E ýazgy:** {when_utc} UTC · iş `{run_id}`"
        ),
        "video_caption": (
            "Ýazgy okuw gurşawyndan (synag maglumatlary). "
            "**E2E ýazgy:** {when_utc} UTC · iş `{run_id}`. Aşakdaky ädimler wideo bilen gabat gelýär."
        ),
        "frame_caption": "Skrinşot · E2E ýazgy {when_utc} UTC · iş `{run_id}`",
    },
    "ru": {
        "screenshot_tip": (
            "Снимки из **английского** интерфейса (версия **{version}**).\n\n"
            "**Захват E2E:** {when_utc} UTC · запуск `{run_id}`"
        ),
        "video_caption": (
            "Запись из учебной среды (тестовые данные). "
            "**Захват E2E:** {when_utc} UTC · запуск `{run_id}`. Шаги ниже соответствуют видео."
        ),
        "frame_caption": "Снимок экрана · захват E2E {when_utc} UTC · запуск `{run_id}`",
    },
}


def _page_locale(page) -> str:
    src = str(getattr(page.file, "src_path", "") or "").replace("\\", "/")
    parts = src.split("/")
    if len(parts) >= 2 and parts[0] in _LOCALES:
        return parts[0]
    return "en"


def _parse_iso(value: str | None) -> datetime | None:
    if not value or not str(value).strip():
        return None
    text = str(value).strip().strip('"')
    try:
        if text.endswith("Z"):
            text = text[:-1] + "+00:00"
        dt = datetime.fromisoformat(text)
        if dt.tzinfo is None:
            dt = dt.replace(tzinfo=timezone.utc)
        return dt.astimezone(timezone.utc)
    except ValueError:
        return None


def _format_when(dt: datetime) -> str:
    return dt.strftime("%Y-%m-%d %H:%M")


def _strings(locale: str) -> dict[str, str]:
    return _LOCALES.get(locale, _LOCALES["en"])


def _inject_screenshot_tip(markdown: str, locale: str, version: str, when: str, run_id: str) -> str:
    if not run_id or not when:
        return markdown

    template = _strings(locale)["screenshot_tip"]
    body = template.format(version=version or "—", when_utc=when, run_id=run_id)
    replacement = f'!!! tip "Screenshots"\n    {body}\n'
    if _SCREENSHOT_TIP_RE.search(markdown):
        return _SCREENSHOT_TIP_RE.sub(replacement, markdown, count=1)
    return markdown


def _inject_video_caption(markdown: str, locale: str, when: str, run_id: str) -> str:
    if not run_id or not when:
        return markdown

    text = _strings(locale)["video_caption"].format(when_utc=when, run_id=run_id)

    def repl(match: re.Match[str]) -> str:
        return f'{match.group(1)}{text}{match.group(3)}'

    if _VIDEO_CAPTION_RE.search(markdown):
        return _VIDEO_CAPTION_RE.sub(repl, markdown, count=1)
    return markdown


def _inject_frame_captions(markdown: str, locale: str, when: str, run_id: str) -> str:
    if not run_id or not when:
        return markdown

    caption = _strings(locale)["frame_caption"].format(when_utc=when, run_id=run_id)
    frame_html = f'\n\n<p class="visa-manual-screenshot-caption">{caption}</p>'

    parts: list[str] = []
    last = 0
    for match in _SCREENSHOT_IMAGE_RE.finditer(markdown):
        parts.append(markdown[last : match.end()])
        tail = markdown[match.end() : match.end() + 80]
        if "visa-manual-screenshot-caption" not in tail:
            parts.append(frame_html)
        last = match.end()
    parts.append(markdown[last:])
    return "".join(parts)


def on_page_markdown(markdown: str, page, config, **kwargs) -> str:
    meta = getattr(page, "meta", None) or {}
    run_id = str(meta.get("mediaE2eRunId") or "").strip()
    version = str(meta.get("screenshotsVersion") or meta.get("videosVersion") or "").strip()

    shots_dt = _parse_iso(meta.get("screenshotsCapturedAt"))
    video_dt = _parse_iso(meta.get("videoCapturedAt"))

    if not run_id and not shots_dt and not video_dt:
        return markdown

    locale = _page_locale(page)

    if shots_dt and run_id:
        when = _format_when(shots_dt)
        markdown = _inject_screenshot_tip(markdown, locale, version, when, run_id)
        markdown = _inject_frame_captions(markdown, locale, when, run_id)

    if video_dt and run_id:
        when = _format_when(video_dt)
        markdown = _inject_video_caption(markdown, locale, when, run_id)

    return markdown
