# AI analytics chat — reference

## Planned / target file map

| Area | Path |
|------|------|
| Module services | `Visa2026.Module/Services/AnalyticsChat/` |
| Options | `AnalyticsAiOptions` — section `AnalyticsAi` |
| Provider | `IAnalyticsChatAiProvider`, `NoneAnalyticsChatAiProvider`, `AzureOpenAIAnalyticsChatAiProvider` |
| Tools | `IAnalyticsChatToolExecutor` |
| Composer | `IAnalyticsInsightComposer` — multi-view derived metrics + insight card DTOs |
| Orchestrator | `AnalyticsChatOrchestrator` |
| Access | `AnalyticsChatAccess` (Enabled + Report Dashboard read rights) |
| Audit BO | `AnalyticsChatTurn` (name may settle at implement time) |
| Blazor panel | `Visa2026.Blazor.Server/Editors/AnalyticsChatPanel.razor` (embedded in `ReportDashboardComponent.razor`) |
| Message UI | Structured parts: narrative + `AnalyticsInsightCard` renderers (KPI, comparison, bucket strip, deep links) |
| Dashboard queries | `Visa2026.Module/Services/ReportDashboard/` |
| Real sub-report gate | Lift/share `RealSubReports` from `ReportDashboardHybridQueryService` |
| Permissions | Extend pattern in `ReportDashboardOfficerPermissions` / `Updater` |
| Canonical doc | `docs/ANALYTICS_AI_CHAT.md` |

Status: skill and plan exist; Module/Blazor code not started until slices A0+.

## Allow-listed tools (v1)

| Tool | Behaviour |
|------|-----------|
| `get_person_role_counts` | `LoadSnapshot` → `PersonRoleCounts` only |
| `get_panel_aggregates` | `LoadPanel` → `{ Title, TotalCount, StatusBuckets }` only; **strip PreviewRows** |
| `list_catalog` | Category + sub-report keys/labels from `ReportDashboardCatalog` |
| `resolve_list_view_hint` | ListView id + criteria hint for open this report; no row data |
| `compose_insight` | Server-side: given 2+ aggregate payloads (or view keys fetched in-tool), return structured insight cards + derived metrics. **No new SQL.** |

**Refuse** tool execution when hybrid would return **mock** data for that `(category, subReport)`. Tell the officer the report is not available for AI yet.

**Out of v1:** person search tool (PII), free SQL, named row lists, LLM-invented numbers.

## Multi-view composition

Goal: chat answers feel analytical, not a copy of one dashboard panel.

```
get_panel_aggregates(view A) ─┐
get_panel_aggregates(view B) ─┼─→ compose_insight → InsightCards + narrative facts
get_person_role_counts()     ─┘
```

### Derived metrics (examples — computed in Module only)

| Recipe (illustrative) | Inputs | Outputs |
|-----------------------|--------|---------|
| Active visa coverage | Active visa total + employee role count | KPI: active visas; KPI: employees; ratio % |
| Expiring pressure | Visa by-days-remaining buckets + work-permit days buckets | Side-by-side bucket strips; urgent count sum |
| Process vs stock | Via-ministry on-process totals + active visa total | Comparison card |
| Invitation funnel | Invitation ready / in-process / used aggregates | Funnel-style KPI row |

Recipe catalog lives in Module (`AnalyticsInsightRecipe` allow-list). Provider may **select** a recipe + view keys; it may **not** invent arithmetic outside documented ops (sum, ratio, subtract, pick bucket by label).

### Chat message shape (UI)

```
AnalyticsChatReply
  NarrativeText          // short officer-facing prose
  Cards[]                // structured, renderable
    Kind: Kpi | Comparison | BucketStrip | DeepLink
    Title, Subtitle
    Metrics[] / Buckets[]  // numbers from composer only
    OpenReportHints[]      // optional ListView deep links
```

Blazor renders cards; do not rely on markdown tables alone for the interesting UI.

## Example mappings

**Single view:** How many employees currently have an active visa?

→ `get_panel_aggregates(VisaExtension, active-by-project, Employees)` → KPI card + short text.

**Multi-view:** How does active visa stock compare to visas on extension?

→ two `get_panel_aggregates` + `compose_insight(comparison)` → comparison card + narrative.

## Config

```json
"AnalyticsAi": {
  "Enabled": false,
  "Provider": "None",
  "RequestTimeoutSeconds": 45,
  "MaxToolRounds": 5
}
```

Secrets (Azure): env / IIS slot only — `AnalyticsAi__ApiKey`, endpoint, deployment name. Never commit keys.

## Privacy checklist

- [ ] Tool DTO to provider has no `PreviewRows` / passport / personal number
- [ ] Mock sub-reports refused
- [ ] Feature hidden when `Enabled = false`
- [ ] Composer only uses allow-listed recipes + aggregate inputs
- [ ] Audit stores question + tool names/params + aggregate/composition summary — not full row dumps
- [ ] Provider failures may use `ILogger` / ApplicationRuntimeLog **errors only** — not chat text as the primary audit

## Prototypes (v1)

| # | File | Scene |
|---|------|-------|
| 01 | [`docs/prototypes/analytics-ai-chat-01-empty-panel.png`](../../../docs/prototypes/analytics-ai-chat-01-empty-panel.png) | Report Dashboard + empty Ask analytics drawer + suggestion chips |
| 02 | [`docs/prototypes/analytics-ai-chat-02-single-kpi.png`](../../../docs/prototypes/analytics-ai-chat-02-single-kpi.png) | Single-view answer with KPI insight card + Open in Report Dashboard |
| 03 | [`docs/prototypes/analytics-ai-chat-03-multi-compose.png`](../../../docs/prototypes/analytics-ai-chat-03-multi-compose.png) | Multi-view composition: comparison + KPIs + bucket strip + deep links |
| 04 | [`docs/prototypes/analytics-ai-chat-04-ai-off.png`](../../../docs/prototypes/analytics-ai-chat-04-ai-off.png) | AI off / None provider banner; deterministic demo still available |
| 05 | [`docs/prototypes/analytics-ai-chat-05-loading.png`](../../../docs/prototypes/analytics-ai-chat-05-loading.png) | Loading: skeleton cards while fetching aggregates |
| 06 | [`docs/prototypes/analytics-ai-chat-06-mock-refuse.png`](../../../docs/prototypes/analytics-ai-chat-06-mock-refuse.png) | Refuse mock / not-yet-real sub-report with open-category hint |
| 07 | [`docs/prototypes/analytics-ai-chat-07-coverage-ratio.png`](../../../docs/prototypes/analytics-ai-chat-07-coverage-ratio.png) | Coverage recipe: % + active visas + employees + deep links |
| 08 | [`docs/prototypes/analytics-ai-chat-08-error-timeout.png`](../../../docs/prototypes/analytics-ai-chat-08-error-timeout.png) | Timeout / error card with Try again |
