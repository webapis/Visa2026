# PNG parity checklist — officer shell

**Full analysis:** `[COMPARISON.md](COMPARISON.md)` (22 PNGs vs HTML, gaps + backlog)  
**Viewport:** 1440×900 · **Zoom:** 100%  
**Interactive:** `/officer-shell/` · **Gallery:** `#/mockups`

Sign-off: **Layout** | **Copy** | **Colors/states** | **Date** | **Notes**  
**Status key:** ✅ close · 🟡 partial · 🔴 stub · ⬜ not built

---

## Summary (2026-08-10)


| Status      | Count | Screens                                                    |
| ----------- | ----- | ---------------------------------------------------------- |
| ✅ Close     | 16    | Wizard 5 + workspace 7 tabs + templates 3 + staged grouped |
| 🟡 Partial  | 4     | Shell, staged/in-process flat lists                        |
| 🔴 Stub     | 0     | —                                                          |
| ⬜ Not built | 0     | —                                                          |


---



## Shell & navigation


| PNG                                                  | Route   | L   | C   | Col | Status | Notes                              |
| ---------------------------------------------------- | ------- | --- | --- | --- | ------ | ---------------------------------- |
| `visa2026-custom-left-navigation-shell-mockup.png`   | all     | ☐   | ☐   | ☐   | 🟡     | No collapse; logo differs          |
| `application-profiles-navigation-sidebar-mockup.png` | sidebar | ☐   | ☐   | ☐   | ✅      | Badges 18 / 24; templates sub copy |




## Staged profiles


| PNG                                                | Route                     | L   | C   | Col | Status | Notes                           |
| -------------------------------------------------- | ------------------------- | --- | --- | --- | ------ | ------------------------------- |
| `staged-profiles-listview-table-mockup.png`        | `#/staged` list           | ☐   | ☐   | ☐   | 🟡     | Chips + legend; shared pager    |
| `staged-profiles-grid-cards-mockup.png`            | `#/staged` grid           | ☐   | ☐   | ☐   | 🟡     | Chips, card badges, warn line   |
| `staged-application-profiles-workspace-mockup.png` | `#/staged?group=template` | ☐   | ☐   | ☐   | ✅      | Accordion groups, selection bar |




## In-process profiles


| PNG                                                  | Route               | L   | C   | Col | Status | Notes                          |
| ---------------------------------------------------- | ------------------- | --- | --- | --- | ------ | ------------------------------ |
| `process-started-profiles-listview-table-mockup.png` | `#/in-process` list | ☐   | ☐   | ☐   | 🟡     | Chips + SLA days; shared pager |
| `process-started-profiles-list-cards-mockup.png`     | `#/in-process` grid | ☐   | ☐   | ☐   | 🟡     | Simplified cards               |




## Case workspace (tabs)


| PNG                                                        | Route                    | L   | C   | Col | Status | Notes                                |
| ---------------------------------------------------------- | ------------------------ | --- | --- | --- | ------ | ------------------------------------ |
| `process-started-application-profile-workspace-mockup.png` | `#/case/p1/overview`     | ☐   | ☐   | ☐   | ✅      | Summary tiles, stepper, rail         |
| `process-started-nav-overview.png`                         | overview                 | ☐   | ☐   | ☐   | ✅      | Same overview layout                 |
| `process-started-nav-people-links.png`                     | `#/case/p1/people`       | ☐   | ☐   | ☐   | ✅      | M2M matrix + rail                    |
| `process-started-nav-progress.png`                         | `#/case/p1/progress`     | ☐   | ☐   | ☐   | ✅      | Vertical timeline + rail             |
| `process-started-nav-document-copies.png`                  | `#/case/p1/documents`    | ☐   | ☐   | ☐   | ✅      | Summary bar, accordion, preview pane |
| `process-started-nav-resminamalar.png`                     | `#/case/p1/resminamalar` | ☐   | ☐   | ☐   | ✅      | Catalog + preview                    |
| `process-started-nav-sla-deadlines.png`                    | `#/case/p1/sla`          | ☐   | ☐   | ☐   | ✅      | SLA dashboard                        |




## Profile templates


| PNG                                                    | Route                  | L   | C   | Col | Status | Notes                             |
| ------------------------------------------------------ | ---------------------- | --- | --- | --- | ------ | --------------------------------- |
| `application-profile-templates-listview-mockup.png`    | `#/templates` list     | ☐   | ☐   | ☐   | ✅      | Chips, status pills, shared pager |
| `application-profile-templates-grid-mockup.png`        | `#/templates` grid     | ☐   | ☐   | ☐   | ✅      | Rich cards + stats                |
| `application-profile-template-overview-mockup.png`     | `#/templates/t1`       | ☐   | ☐   | ☐   | ✅      | Rail, numbered cols, usage bar    |
| `application-profile-template-wizard-mockup.png`       | `#/templates/wizard/0` | ☐   | ☐   | ☐   | ✅      | Bootstrap wizard                  |
| `application-profile-template-wizard-step2-mockup.png` | `/wizard/1`            | ☐   | ☐   | ☐   | ✅      |                                   |
| `application-profile-template-wizard-step3-mockup.png` | `/wizard/2`            | ☐   | ☐   | ☐   | ✅      |                                   |
| `application-profile-template-wizard-step4-mockup.png` | `/wizard/3`            | ☐   | ☐   | ☐   | ✅      |                                   |
| `application-profile-template-wizard-step5-mockup.png` | `/wizard/4`            | ☐   | ☐   | ☐   | ✅      |                                   |


---



## Template AI convert (slice E7a)

PNGs live in [`docs/prototypes/`](../../../../docs/prototypes/), not in `assets/png/`. Convert opens as a **modal**, so the route below only decides the page behind it.

| PNG                                       | Route                            | L   | C   | Col | Status | Notes                                                        |
| ----------------------------------------- | -------------------------------- | --- | --- | --- | ------ | ------------------------------------------------------------ |
| `template-ai-convert-01-upload.png`       | `#/templates?convert=upload`     | ☐   | ☐   | ☐   | ✅      | PNG draws a 2-step stepper; built with the spec §4 five stages |
| `template-ai-convert-02-candidate-check.png` | `#/templates?convert=candidate` | ☐   | ☐   | ☐   | ✅      | Suitability, chips, criteria, legend; no zoom toolbar yet     |
| `template-ai-convert-05-converting.png`   | `#/templates?convert=converting` | ☐   | ☐   | ☐   | ✅      | 4 steps, auto-advance ~550 ms                                  |
| `template-ai-convert-03-preview-chat.png` | `#/templates?convert=preview`    | ☐   | ☐   | ☐   | ✅      | Filled / Placeholders / Highlights tabs + mapping chat         |
| `template-ai-convert-04-done.png`         | `#/templates?convert=done`       | ☐   | ☐   | ☐   | ✅      | Summary table + manual-add note                                |
| `template-ai-convert-13-excel-roster.png` | `#/templates?convert=roster`     | ☐   | ☐   | ☐   | 🟡     | Sheet grid + row loop chips; no column headers styling pass    |
| `template-ai-convert-12-needs-help-gaps.png` | `#/templates?convert=help`    | ☐   | ☐   | ☐   | 🟡     | Gap list + Back; packet export is an alert stub                |
| `template-ai-convert-14-shared-confirm.png` | `#/templates?convert=confirm`  | ☐   | ☐   | ☐   | ✅      | One dialog covers Shared **and** remaining gaps                |
| `template-ai-convert-06-candidate-fail.png` | `#/templates?convert=fail`     | ☐   | ☐   | ☐   | ✅      | HR-memo fixture; Convert disabled with reason hint             |
| `template-ai-convert-07-candidate-warn.png` | `#/templates?convert=warn`     | ☐   | ☐   | ☐   | ✅      | Hand-tokenized draft; **Continue with warnings** gates Convert |
| `template-ai-convert-10-validate-fail.png` | `#/templates?convert=validate-fail` | ☐ | ☐ | ☐ | ✅      | Error rail replaces chat; broken tokens marked inline          |
| `template-ai-convert-11-config-locked.png` | `#/templates?convert=locked`    | ☐   | ☐   | ☐   | ✅      | Badge + banner; `?locked=1` locks any stage                    |
| `template-ai-convert-16-fill-preview-fallback.png` | `#/templates?convert=fill-error` | ☐ | ☐ | ☐ | ✅   | Tab marked *(error)*, token fallback, Approve still allowed    |
| `template-ai-convert-08-manual-add-ai-off.png` | `#/templates?convert=manual` · `?ai=off` | ☐ | ☐ | ☐ | ✅ | L12 manual mode; AI-off disables Convert with an `AI off` badge |
| `template-ai-convert-09-chat-reject-rewrite.png` | `#/templates?convert=preview` | ☐ | ☐ | ☐ | ✅   | Behavior inside V4: ask for a font change, get the refusal bubble |
| `template-ai-convert-15-wizard-instance-picker.png` | `#/templates?convert=upload` | ☐ | ☐ | ☐ | ✅  | Behavior inside V1: instance select when `source = catalog`    |

Flow reference: [`docs/TEMPLATE_AI_CONVERT_UI_FLOW.md`](../../../../docs/TEMPLATE_AI_CONVERT_UI_FLOW.md) (views V0–V11 and every transition).

Guard regression for the edge states: `node parity/smoke-edge.mjs` from the `officer-shell` folder — it renders V8–V11 headlessly and asserts each disabled button and acknowledge gate.

## Gate checks

- [x] Primary flows work without console errors
- [x] List ↔ Grid ↔ Grouped toggle on staged; List ↔ Grid on in-process / templates
- [x] Start process banner when selection blocked
- [x] Wizard `#/templates/wizard/{0-4}`
- [x] Convert modal reachable from the templates catalog, and from a case only while the topbar **Template convert editor** switch is on (L13)

**H7 deferred:** Person DetailView staging — no PNG in set.