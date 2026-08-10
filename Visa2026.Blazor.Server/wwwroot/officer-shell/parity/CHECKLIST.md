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



## Gate checks

- [x] Primary flows work without console errors
- [x] List ↔ Grid ↔ Grouped toggle on staged; List ↔ Grid on in-process / templates
- [x] Start process banner when selection blocked
- [x] Wizard `#/templates/wizard/{0-4}`

**H7 deferred:** Person DetailView staging — no PNG in set.