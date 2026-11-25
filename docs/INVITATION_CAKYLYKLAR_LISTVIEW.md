# Invitations — Employee / Family member ListViews

> **Status:** **Implemented.** Top-level **Invitations** nav (beside Employees / Family Members) with two issued-`InvitationItem` registries split by `PersonRole`. TemporaryVisitor lines are hidden. Shared color tabs, Search, row colors, and preview links.
>
> **Prototype:** [`prototypes/invitation-cakylyklar-listview-prototype-dark.jpg`](prototypes/invitation-cakylyklar-listview-prototype-dark.jpg)
>
> **Related:** [`InvitationLaterLife`](../Visa2026.Module/Services/ApplicationWorkspace/InvitationLaterLife.cs), [`APPLICATION_PROFILE_LISTVIEW_PROGRESS_COLORS.md`](APPLICATION_PROFILE_LISTVIEW_PROGRESS_COLORS.md) §6, [`PREVIEW_SLOT.md`](PREVIEW_SLOT.md).

---

## 1. Navigation

| Nav | ListView | Criteria |
|-----|----------|----------|
| **Invitations → Invitations (Employee)** | `InvitationItem_Cakylyklar_Employees_ListView` | Issued + `PersonRole = Employee` |
| **Invitations → Invitations (Family member)** | `InvitationItem_Cakylyklar_FamilyMembers_ListView` | Issued + `PersonRole = FamilyMember` |

Removed from Application Profile Instances (Via / Direct). Legacy `InvitationItem_Cakylyklar` nav is denied/hidden.

### Captions

| Culture | Group | Employee | Family |
|---------|-------|----------|--------|
| en | Invitations | Invitations (Employee) | Invitations (Family member) |
| tk | Çakylyklar | Çakylyklar (Işgär) | Çakylyklar (Maşgala) |
| tr | Davetiyeler | Davetiyeler (Çalışan) | Davetiyeler (Aile üyesi) |
| ru | Приглашения | Приглашения (Сотрудник) | Приглашения (Член семьи) |

## 2. Tabs / columns / affordances

Same UI for both lists: Ähli / Taýýar / Ulanyldy / … / Gözleg; Person, Passport, Department, Work permit, Visa type / category / period, CO, Linked detail, **Expiration Date**, **Days remaining**, Invitation №, State (last). Taýýar Linked detail also shows expiry · remaining days. Family rows usually have no work permit. Search spans states within that role list.

## 3. Code map

| Layer | Location |
|-------|----------|
| Nav / ListView ids | `InvitationItemCakylyklarNavigation` |
| Nav seed | `CustomNavigationUpdater.EnsureInvitationsGroupNavigation` |
| Permissions | `Updater.cs` (`Items/Invitations/...`) |
| Controllers | Target `TargetViewIds` (both ListViews) |

## 4. Changelog

| Date | Note |
|------|------|
| 2026-11-25 | Initial Çakylyklar under Application Profile Instances |
| 2026-11-25 | Split to top-level Invitations (Employee / Family); TemporaryVisitor hidden; TR Davetiyeler |
| 2026-11-25 | Department column (current position history) |
| 2026-11-25 | Work permit column (WorkPermitItem number; preview copies) |
| 2026-11-25 | Expiration Date + Days remaining for Taýýar / Ready |
| 2026-11-25 | Ulanyldy/Used row tint #bbf7d0 → #4ade80 (less pale on dark) |
