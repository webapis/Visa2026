# AI analytics chat — Learnings (append-only)

Date format: `YYYY-MM-DD`

Read before starting work; append after every verified change (success or failure).

---

## 2026-08-21 — Skill created

**Ask:** Dedicated Agent skill for AI analytics chat (not folded into report-dashboard).

**Now:** `.cursor/skills/visa2026-analytics-ai-chat/` with SKILL / reference / IMPLEMENTATION_PLAN / prompts / learnings. Product rules locked: RD tools only, aggregates only, panel on RD, None then Azure, default off.

**Next:** Finish A0 — `docs/ANALYTICS_AI_CHAT.md`, options/DI, AGENTS + REPORT_DASHBOARD links; then A1 tools.
## 2026-08-21 — Multi-view composition + rich chat cards

**Ask:** Can AI chat combine results from several predefined SQL views and present more interesting analytical UI than a single dashboard-style dump?

**Now:** Yes — locked in skill/plan. Orchestrator fetches 2+ allow-listed panel aggregates; Module compose_insight / recipe catalog derives KPIs, ratios, comparisons; Blazor renders structured insight cards. Still no free SQL and no PreviewRows/PII to the model. MaxToolRounds raised to 5 in reference config.

**Next:** Document in docs/ANALYTICS_AI_CHAT.md when A0 continues; implement composer in A1 and card UI in A3.

## 2026-08-21 — Prototype images (4)

**Ask:** Create 4 prototype images for analytics-ai-chat.

**Now:** `docs/prototypes/analytics-ai-chat-01-empty-panel.png` … `-04-ai-off.png`. Linked from skill `reference.md`.

**Next:** Include gallery in `docs/ANALYTICS_AI_CHAT.md` when that doc is written.

## 2026-08-21 — Prototype images 05–08

**Ask:** Mock more analytics-ai-chat prototype images.

**Now:** Added loading, mock-refuse, coverage-ratio, error-timeout under `docs/prototypes/analytics-ai-chat-05` … `-08`. Gallery is 01–08 in skill `reference.md`.

**Next:** Optional further scenes; include full gallery when writing `docs/ANALYTICS_AI_CHAT.md`.
