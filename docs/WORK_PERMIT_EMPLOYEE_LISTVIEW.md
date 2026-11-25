# Work Permits — Employee ListView

> **Status:** **Implemented.** Top-level **Work Permits** nav (beside Invitations) with one issued-`WorkPermitItem` registry for **Employee**. No Family list. Color tabs, Search, row colors, and preview links mirror Invitations Çakylyklar (without Used).
>
> **Related:** [`INVITATION_CAKYLYKLAR_LISTVIEW.md`](INVITATION_CAKYLYKLAR_LISTVIEW.md), [`WorkPermitItemLaterLife`](../Visa2026.Module/Services/ApplicationWorkspace/WorkPermitItemLaterLife.cs), [`PREVIEW_SLOT.md`](PREVIEW_SLOT.md).

---

## 1. Navigation

| Nav | ListView | Criteria |
|-----|----------|----------|
| **Work Permits → Work Permits (Employee)** | `WorkPermitItem_Registry_Employees_ListView` | Issued + `PersonRole = Employee` |

Legacy `WorkPermit` / `WorkPermitItem` nav items stay hidden/denied.

### Captions

| Culture | Group | Employee |
|---------|-------|----------|
| en | Work Permits | Work Permits (Employee) |
| tk | Iş rugsatlary | Iş rugsatlary (Işgär) |
| tr | İş İzinleri | İş İzinleri (Çalışan) |
| ru | Разрешения на работу | Разрешения на работу (Сотрудник) |

## 2. Tabs / columns / affordances

Tabs: Ähli / **Güýjünde** (in force) / Ýatyryldy / Üýtgedildi / Möhleti gutaran / Gözleg. No Ulanyldy/Used.

Columns: Person, Passport, Department, **Work permit №** (→ WP copies), Start, Expiration, Days remaining, CO (→ app workspace), Linked detail, State (last).

Linked detail: cancel/change process caption; Güýjünde shows `dd.MM.yyyy · Nd`; Expired shows expiry date.

Lifecycle: cancelled > changed > expired > in force.

## 3. Code map

| Layer | Location |
|-------|----------|
| Nav / ListView ids | `WorkPermitItemRegistryNavigation` |
| Nav seed | `CustomNavigationUpdater.EnsureWorkPermitsGroupNavigation` |
| Permissions | `Updater.cs` (`Items/WorkPermits/...`) |
| Controllers | Target `WorkPermitItem_Registry_Employees_ListView` |

## 4. Changelog

| Date | Note |
|------|------|
| 2026-11-25 | Initial Work Permits (Employee) registry |