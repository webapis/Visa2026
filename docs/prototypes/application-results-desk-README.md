# Application results desk (2026-11-27, prototype)

Officers enter invitation, work permit, visa, border zone, work location, and rejection from a page of its own. The case workspace **Application result** tab stays the place the result is stored. **Add** on this desk opens the compose already used on that case.

**Status:** First implementation is in the app (2026-11-27). The pictures stay the target. The live page is English for this pass. It has not been opened in the browser yet.

## First implementation

| Choice | What the page does now |
|--------|------------------------|
| Navigation | Root item **Application results**, next to Find employees. Officers who can open a case see it. The visa-office role does not. The count of missing cases is on the page, not on the menu. |
| Search | Exact match on the CO / AS number, the full application number, or the application number. |
| Missing queue | Latest 400 cases that already have a CO / AS number and still miss a required result. The page says when the list is cut off. |
| Valid | A required result whose coverage is complete. **Open** goes to the case. |
| Cancelled / Changed | The cancellation or change case itself, once it has an issued process number. This is not a scan of each document. |
| Seretmezlik | Submitted cases that require a target instance. No CO / AS and no Add. |
| Work permit | List on this page. Save the letter number and issued date first. That creates one line per employee who has a passport and a position. **Open** is the real work-permit detail in a window. |
| Visa | List on this page. **Open** is the real visa detail, filled from the invitation or from the case. The visa number stays empty until the officer types it. |
| Invitation, border zone, rejection, work location | **Add** and **Open** go to the case. The letter forms stay on the case. |

Image labels may be slightly garbled. This file is the source of truth.

## Locked choices

| Decision | Choice |
|----------|--------|
| Navigation | Own item under **Application Profiles**, caption **Application results**. Visible to every officer who can open a case. |
| Badge | Count of cases that still have at least one required result missing. The orange **27** on the pictures is an example. |
| Search | CO / AS process number (`ProcessNumber`, for example `C00138718`) and the application number (for example `2026-0155`). |
| Expected | A required result appears only after the case has a CO / AS number, and only for a type that profile **May produce**. |
| No CO / AS yet | The case stays off the missing queue. Office preparation and ministry steps are not listed here. |
| Row | One row per case per result type. Coverage is people on that case, for example `1/14`. |
| Add | Opens the existing compose for that case. Same people, same required fields. After save, this page shows the new coverage. |
| Open | Opens the case. |
| Rejection | Optional. A zero is not a gap and does not change the badge. The card is dashed. |
| Work location | Shown only when the profile produces an extra work location. |
| Border zone | Shown only when the profile produces a border zone permit. |
| Missing | The result was never entered for every person the case expects. |
| Valid, Cancelled, Changed | States of a document that already exists. This page shows them and opens the case. Cancel and change stay on the document. |
| Seretmezlik | Its own filter, once the case is submitted. These cases have no CO / AS number and no result to add. |
| Prototype language | English, so the rules stay readable. The live page uses the usual tk / en / tr / ru strings. |

## Screens

| File | What it shows |
|------|----------------|
| [application-results-desk-01-landing.png](./application-results-desk-01-landing.png) | Nav item open. Search empty. Queue of cases that still need a required result. **All missing** and **Missing** are the selected chips. |
| [application-results-desk-02-search-short.png](./application-results-desk-02-search-short.png) | CO / AS `C00138718` found. Invitation `14/14` complete. Work permit `1/14` and visa `0/14` missing, with **Add**. Rejection `0` optional. Border zone and work location hidden. |
| [application-results-desk-03-search-complete.png](./application-results-desk-03-search-complete.png) | `C00120441` produces an invitation only, and it is `8/8`. Banner: all required results are entered. No Add, no missing table. |
| [application-results-desk-04-not-found.png](./application-results-desk-04-not-found.png) | `C00999999` matches nothing. The missing queue stays on the page so another case can be picked. |
| [application-results-desk-05-border-zone.png](./application-results-desk-05-border-zone.png) | `C00144100` produces a border zone permit only. Coverage `0/4`. **Add border zone**. Selected chips: **Border zone** and **Missing**. |
| [application-results-desk-06-work-location.png](./application-results-desk-06-work-location.png) | `C00155210` produces an extra work location only, not a new work permit. Coverage `2/6`. **Add work location**. Selected chips: **Work location** and **Missing**. |
| [application-results-desk-07-valid.png](./application-results-desk-07-valid.png) | Issued documents whose state is **Valid**. Every row is **Open**. No Add. |
| [application-results-desk-08-seretmezlik.png](./application-results-desk-08-seretmezlik.png) | Submitted Seretmezlik cases. No CO / AS column. State **Submitted**. **Open** only. |
| [application-results-desk-09-cancelled-changed.png](./application-results-desk-09-cancelled-changed.png) | Issued documents that are **Cancelled** or **Changed**. Section title **Issued results**. **Open** only. |
| [application-results-desk-10-add-invitation.png](./application-results-desk-10-add-invitation.png) | **Add invitation** for `C00127600`, a case that still needs an invitation. Header plus the two people on the case. |
| [application-results-desk-11-add-work-permit.png](./application-results-desk-11-add-work-permit.png) | **Work permits** list for `C00138718`. One row per employee. The highlighted row is Ali Enes Yetkin. Click opens the work permit detail. |
| [application-results-desk-12-add-visa.png](./application-results-desk-12-add-visa.png) | **Visas** list for `C00138718`. One row per person who has an invitation. The highlighted row is Dowran Atayew. Click opens the visa detail. |
| [application-results-desk-13-work-permit-detail.png](./application-results-desk-13-work-permit-detail.png) | Work permit detail opened from the Ali Enes Yetkin row. Passport and AS number are read-only. **Save** returns to the list. |
| [application-results-desk-14-visa-detail.png](./application-results-desk-14-visa-detail.png) | Visa detail opened from the Dowran Atayew row. Passport, process number, and issuing invitation are read-only. **Save** returns to the list. |

## Picture notes

- **05.** Ignore the blank purple button and any chip that does not say **Border zone** or **Missing**. The real action is **Add border zone**.
- **06.** The **Work permit** and **Changed** chips in the picture are wrong. The selected chips are **Work location** and **Missing**.
- **10.** Invitation stays a letter form. **Back to results** returns to the search.
- **11–14.** Work permit and visa are lists. A row click opens that record's detail. **New** opens an empty detail for a person on the case. Passport, the work-permit AS number, and the visa process number are read-only. **Back to work permits** and **Back to visas** return to the list.

## What Add opens

| Add | Screen on this desk |
|-----|---------------------|
| Invitation, rejection, border zone | Letter form. Invitation is screen 10. Field list: [issue-issued-header-slot-README.md](./issue-issued-header-slot-README.md) |
| Work permit | Employee list (screen 11). A row opens the work permit detail (screen 13). |
| Visa | Person list (screen 12). A row opens the visa detail (screen 14). People with no invitation on this case are omitted. |
| Work location | Same list-and-detail action. Its detail is still the case flow for `ProduceWorkLocation`. |