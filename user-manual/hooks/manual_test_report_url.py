"""Append home-page footer link to the separate manual test results report."""

from __future__ import annotations

import os
import re

_HOME_INDEX_RE = re.compile(r"^(?:docs/)?(en|tr|tk|ru)/index\.md$", re.IGNORECASE)
_MARKER = "<!-- visa-test-report-footer -->"

_URL_PLACEHOLDER = "@@TEST_REPORT_URL@@"

_FOOTERS = {
    "en": (
        "\n\n---\n\n"
        f"{_MARKER}\n\n"
        f"**Supervisors / QA:** [Automated test results]({_URL_PLACEHOLDER}) "
        "(pass, fail, or not run for guides in this manual)."
    ),
    "tr": (
        "\n\n---\n\n"
        f"{_MARKER}\n\n"
        f"**Amirler / QA:** [Otomatik test sonuclari]({_URL_PLACEHOLDER}) "
        "(bu kilavuzdaki adimlar icin gecti, kaldi veya calistirilmadi)."
    ),
    "tk": (
        "\n\n---\n\n"
        f"{_MARKER}\n\n"
        f"**Yolbascylar / QA:** [Awtomatiki synag netijeleri]({_URL_PLACEHOLDER}) "
        "(bu gollanmadaky gollanmalar ucin gecdi, yeyan yada isledilmedi)."
    ),
    "ru": (
        "\n\n---\n\n"
        f"{_MARKER}\n\n"
        f"**Руководители / QA:** [Результаты автоматических тестов]({_URL_PLACEHOLDER}) "
        "(пройдено, ошибка или не запускалось для руководств в этом руководстве)."
    ),
}


def _resolve_url() -> str:
    return os.environ.get(
        "MANUAL_TEST_REPORT_URL",
        "/manual-test-reports/latest/summary.html",
    ).strip()


def _home_locale(page) -> str | None:
    src = str(getattr(page.file, "src_path", "") or "").replace("\\", "/")
    match = _HOME_INDEX_RE.match(src)
    if not match:
        return None
    return match.group(1).lower()


def on_config(config, **kwargs) -> None:
    url = _resolve_url()
    if url:
        print(f"[manual-test-report] Home footer link: {url}")


def on_page_markdown(markdown: str, page, config, **kwargs) -> str:
    locale = _home_locale(page)
    if not locale:
        return markdown

    if _MARKER in markdown:
        return markdown

    template = _FOOTERS.get(locale, _FOOTERS["en"])
    url = _resolve_url()
    return markdown.rstrip() + template.replace(_URL_PLACEHOLDER, url)
