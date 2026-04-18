# Report Map — App_Business_Trip_Departure_BusinessTrip.jpg

**Status:** 📋 Draft

---

## Report Identity

| Field | Value |
|---|---|
| Class Name | `AppBusinessTripDepartureSanawReport` |
| Base Class | `AppBusinessTripSanawBaseReport` (shared base, layout identical to Arrival Sanawy) |
| Registered Name | `"App Business Trip Departure Sanaw Report"` |
| Display Name (Tm) | `"Iş Sapary — Gidiş Sanawy"` |
| Reference Image | `App_Business_Trip_Departure_BusinessTrip.jpg` |

---

## Data

| Field | Value |
|---|---|
| Data Type | `BusinessTrip` |
| Registration Target | `typeof(BusinessTrip)` — inplace report |
| Visibility Criteria | `[Application.ApplicationType.Name] = 'App_Business_Trip_Departure'` |
| Shared vs Per-Type | Per-type |
| Background Rule | None — clean white page, no letterhead |

---

## Layout

**Identical to `App_Business_Trip_Arrival_BusinessTrip_map.md` in every detail.**

- Same page setup (Landscape A4, 100F margins, 969F printable width)
- Same 11 columns with identical header text, widths, and data expressions
- Same ReportHeader title: `"Daşary ýurt raýatlarynyň sanawy"`
- Same ReportFooter signatory block
- Only difference: visibility criterion above

The `AppBusinessTripSanawBaseReport` base class implements the entire layout.
`AppBusinessTripArrivalSanawReport` and `AppBusinessTripDepartureSanawReport` are empty thin wrappers — their only purpose is to hold the separate registered names for XAF's visibility rule system.

See [App_Business_Trip_Arrival_BusinessTrip_map.md](App_Business_Trip_Arrival_BusinessTrip_map.md) for all band, column, and control details.

---

## Required BO Properties

Same as Arrival map — all properties are on `BusinessTrip` and shared between both reports.
