# Çakylyklar — InvitationItem state ListView (prototype)

**Status:** visual prototype for implementation. Canonical plan: officer registry of issued invitation person-lines with color tabs, Search, and preview-slot copy.

**Image:** [invitation-cakylyklar-listview-prototype-dark.jpg](./invitation-cakylyklar-listview-prototype-dark.jpg)

## Purpose

Left-nav **Çakylyklar** (next to Via ministry / Direct migration) opens a ListView of **InvitationItem** rows (one person on an issued invitation). Officers filter by lifecycle color tabs and open the invitation letter copy in the right preview slot.

## Tabs

| Tab | State |
|-----|--------|
| **Ähli** | All issued invitation items in scope |
| **Taýýar** (ready to use) | `InvitationLaterLife.InForce` |
| **Ulanyldy** | Used → linked issued visa |
| **Ýatyryldy** | Cancelled → cancellation application |
| **Üýtgedildi** | Changed → change application + renewed invitation |
| **Möhleti gutaran** | Expired unused |
| **Gözleg** | Search matches across all states; visible while searching; count 0 / empty grid when no hits (same as Application Profile Instance Search) |

## Columns (prototype)

Person, Passport, Invitation №, CO, Expiration, state-linked detail, Preview.

## Colors

Aligned with invitation-life banner tones (`site.css` `.invitation-life-banner--*`): ready green, used light green, cancelled coral, changed violet, expired pink.