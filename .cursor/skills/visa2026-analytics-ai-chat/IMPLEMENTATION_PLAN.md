# AI analytics chat — implementation plan

Canonical: `docs/ANALYTICS_AI_CHAT.md` · Skill: [SKILL.md](./SKILL.md)

## Locked decisions

- Report Dashboard tools only; aggregates only; panel on Report Dashboard; `None` then Azure OpenAI; default off.
- **Multi-view composition** — combine 2+ real view aggregates via allow-listed recipes into structured insight cards (richer chat UI). Numbers only from tools; no free SQL.

## Slices

| Slice | Status | Deliverable |
|-------|--------|-------------|
| **A0** | Partial | Skill folder + AGENTS / report-dashboard cross-links done. Remaining: `docs/ANALYTICS_AI_CHAT.md`, options/DI, access gate, REPORT_DASHBOARD.md link |
| **A1** | Pending | Tool executor (real-only aggregates) + `compose_insight` / composer stub + unit tests |
| **A2** | Pending | Orchestrator (multi-tool rounds) + `None` intent map including **multi-view** canned questions |
| **A3** | Pending | Chat panel with **insight card** renderers (KPI / comparison / bucket strip / deep link) + localization + feature flag |
| **A4** | Pending | Audit BO + permissions (record composition recipe + source view keys) |
| **A5** | Pending | Azure OpenAI adapter + env secrets; keep `None` default |

## Skill bootstrap (2026-08-21)

- Created `.cursor/skills/visa2026-analytics-ai-chat/`.
- Locked multi-view composition + rich insight UI (plan iteration).
- Code and `docs/ANALYTICS_AI_CHAT.md` still part of remaining A0 work.
