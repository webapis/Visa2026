# Application result — counts on tabs + XAF-like toolbar (VISA2014 feel)

Overview hybrid **level-2 issued tabs** show coverage on the tab (`3/10`). Optional types show **count only**. **Result overview cards are removed**. **Add/New, Edit, Delete, Open** live in an XAF ListView-style toolbar under the tabs (not the purple Actions card). Left-nav Application result page is **out of scope**.

**Status:** implemented (Overview hybrid level-2 only).

## Locked decisions

| Decision | Choice |
|----------|--------|
| Look/feel | **VISA2014** |
| Tab caption (required) | Label + **issued/expected** (`0/5`) |
| Tab caption (optional) | Label + **count only** (`0`) |
| Result overview cards | **Removed** (info lives on tabs) |
| Actions | XAF-like toolbar: **New/Add only**; row click opens (same as Application result) |
| Scope | Overview hybrid level-2 issued tabs **only** |
| Left-nav Application result | Unchanged for this slice |

## Screen

| File | Notes |
|------|--------|
| [case-workspace-application-result-2014-tabs-toolbar-prototype.jpg](./case-workspace-application-result-2014-tabs-toolbar-prototype.jpg) | Tabs with ratios; Rejection `0`; XAF toolbar New/Edit/Delete/Open; no Result overview cards |