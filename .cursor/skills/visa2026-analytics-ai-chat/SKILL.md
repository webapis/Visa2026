---
name: visa2026-analytics-ai-chat
description: >-
  Officer-facing AI analytics chat on the Report Dashboard: allow-listed tools over
  IReportDashboardQueryService aggregates (counts/status buckets only), multi-view
  composition into insight cards, pluggable providers (None then Azure OpenAI), chat
  panel UX, AnalyticsChatTurn audit, and AnalyticsAi config. Use when adding tools,
  composers, providers, privacy/PII rules, chat UI, or refusing free SQL. Not for
  vw_rd_* views (visa2026-report-dashboard), Template Convert AI, or Officer Task Chat.
---

# Visa2026 AI analytics chat

Canonical doc: `docs/ANALYTICS_AI_CHAT.md` (create/update with feature slices).
Experience log: [learnings.md](./learnings.md) — **read before starting**; **append after every verified change**.
File map, tools, options: [reference.md](./reference.md).
Slice tracker: [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md).
Chat openers: [prompts.md](./prompts.md).
Prototypes: [reference.md § Prototypes](./reference.md) (`docs/prototypes/analytics-ai-chat-01` … `-08`).

**Depends on data from:** [visa2026-report-dashboard](../visa2026-report-dashboard/SKILL.md) (`LoadPanel`, catalog, `RealSubReports`). If a question needs a **mock** sub-report, promote the view via that skill first, then wire the tool here.

---

## Locked product rules (v1)

1. **Data surface** — Report Dashboard only. Allow-listed tools over `LoadPanel` / `PersonRoleCounts` / catalog. **No free SQL**, no schema dump to the model, no custom EF beyond dashboard views.
2. **Answer shape** — **Aggregates only** as inputs (`TotalCount`, `StatusBuckets`, person-role totals). Never send `PreviewRows` (names, passport numbers) to the provider or return named person lists in chat. Answers may **compose** several allow-listed view aggregates into derived insight cards (ratios, comparisons, side-by-side KPIs) — all numbers must come from tool results, never invented by the model.
3. **UI** — Collapsible panel on Report Dashboard (`ReportDashboardComponent`), not a global header shell. Render **structured insight cards**, not only plain text dumps of one panel.
4. **Ship order** — `None` provider first (deterministic intent → tools); then Azure OpenAI. Feature default **`AnalyticsAi:Enabled = false`**.
5. **Providers** — Separate from Template Convert (`ITemplateConvertAiProvider`). Use `IAnalyticsChatAiProvider`.
6. **Audit** — Persist turns in dedicated BOs (e.g. `AnalyticsChatTurn`). Do **not** put transcripts in `ApplicationRuntimeLog`.
7. **Security** — Every tool call uses the officer `IObjectSpace` + XAF grants (same as Report Dashboard). Logic in **Module**; Blazor is thin chrome.
8. **Multi-view composition** — Prefer fetching **2+** real panel aggregates in one turn and composing them server-side into structured insight cards for a richer chat UI.

---

## When to use this skill

| Trigger | Do |
|---------|-----|
| Analytics chat / AI ask the dashboard | Read learnings → follow slices in IMPLEMENTATION_PLAN |
| New allow-listed tool | Add to executor + reference allowlist; keep aggregate-only payload |
| Multi-view insight / comparison | Composer recipes + multi-tool turn; keep aggregate-only inputs |
| Provider / Azure / AI off | Options + adapter; secrets via env only |
| Chat panel UX / localization | Blazor panel + insight cards + `ReportDashboard.AnalyticsChat.*` messages |
| Wrong count vs dashboard | Fix tool mapping or promote `RealSubReports` via report-dashboard skill |

## When not to use

| Need | Use instead |
|------|-------------|
| New `vw_rd_*`, category, ListView parity | [visa2026-report-dashboard](../visa2026-report-dashboard/SKILL.md) |
| Word/Excel template AI convert | [visa2026-application-profile](../visa2026-application-profile/SKILL.md) + Template Convert specs |
| Human officer messaging | `docs/OFFICER_TASK_CHAT_IMPLEMENTATION_PLAN.md` |
| Dev runtime log triage | [visa2026-runtime-error-tracking](../visa2026-runtime-error-tracking/SKILL.md) |

---

## Agent workflow

1. Read [learnings.md](./learnings.md) and [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md).
2. Confirm the change stays within locked rules (no free SQL, no PII rows to the model).
3. Implement in `Visa2026.Module/Services/AnalyticsChat/` (or planned paths in reference); Blazor only for the panel.
4. Add/adjust unit tests for tools / composer / None provider / orchestrator caps.
5. Append a learning after a verified build or behaviour check.
6. Update IMPLEMENTATION_PLAN slice status when a slice ships.

---

## Chat openers

- `@visa2026-analytics-ai-chat` — continue analytics chat slices or fix tool/provider bugs.
- See [prompts.md](./prompts.md).
