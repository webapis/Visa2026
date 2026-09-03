# Learnings (append-only): Global preview slot

Purpose: **shell, layout, occupants, catalog card UX, JS/CSS** — not Resminamalar merge or document-copy scan rules.

**Read before every preview-slot task:** skim **## Entries** (newest first).
**Maturity:** [MATURITY.md](./MATURITY.md).

**After a verified fix:** append one entry. **Do not** edit or delete prior entries.

```markdown
### YYYY-MM-DD — <short title> (<Resminamalar | DocumentCopies | shell | CSS>)

- **Symptom**:
- **Try**:
- **Test**:
- **Root cause**:
- **Fix**:
- **Prevent**:
- **Cross-skill**: preview-slot | resminamalar | document-copies | —
```

---

## Entries

### 2026-09-03 — Catalog on Choose Approval legs opens the occupant list

- **Symptom**: No picker action opened the approval-leg catalog list in `#visa-preview-slot` (only + New / per-card Open).
- **Fix**: **Catalog** link → empty `ApprovalLegCatalogSlotRequest`. Version remount shows the list.
- **Test**: Officer: restart, hard-refresh. Choose Approval legs → Catalog. Slot title Approval leg profiles with search and Open.
- **Prevent**: Do not reuse StartNew for this action.
- **Cross-skill**: visa2026-application-profile

### 2026-09-03 — Approval-leg Active off did not persist

- **Symptom**: Slot Active switch off + Save left the chain active (picker still listed it).
- **Root cause**: Save without `SetModified` skipped the bool; seed sync restored `IsActive` from JSON.
- **Fix**: Module `MarkModified` on Save; seed only sets Active on new seed rows. Occupant toggle uses `onchange`.
- **Test**: `ApplyScalars_writes_inactive`. Officer: restart, Open → Active off → Save; catalog Inactive, picker card gone.
- **Prevent**: Do not `@bind` the Active switch if Save can race the bind. Do not clobber officer Active from catalog JSON.
- **Cross-skill**: visa2026-application-profile

### 2026-09-03 — Approval-leg slot Create failed (SaveFailed)

- **Symptom**: + New from Choose Approval legs → Create showed **Could not save the approval-leg profile.**
- **Root cause**: New-parent ministry legs wrote `ApprovalLegProfileId = Guid.Empty`.
- **Fix**: Module `SyncForeignKeys` copies the parent client id. Occupant Create/Save unchanged.
- **Test**: `SyncForeignKeys_copies_parent_id_while_parent_is_new`. Officer: restart, hard-refresh, + New → Create.
- **Prevent**: Do not treat empty Guid as “FK unset” on this occupant’s persist path.
- **Cross-skill**: visa2026-application-profile

### 2026-09-03 — Approval-leg slot opens from Choose Approval legs

- **Symptom**: Officers needed + New / Open on instance create, not only Configure Identity Edit in Configuration.
- **Fix**: Same `ApprovalLegCatalog` occupant. `ApprovalLegCatalogSlotRequest.StartNew` / `FocusProfileId` open New or Edit. Notifier now carries the changed chain id so the picker selects the new card.
- **Test**: Officer: restart, hard-refresh. Choose Approval legs → + New → Create; slot then picker card selected. Open on a card opens the editor.
- **Prevent**: Do not add a second approval-leg CRUD host. Do not remount the picker to loading while the slot saves.
- **Cross-skill**: visa2026-application-profile


### 2026-09-02 — This profile / Shared tab bar vs utility links (CSS)

- **Need**: Prototype has This profile / Shared as left tabs with a thick teal underline on the gray bar; Recycle Bin and Select all sit on the right as smaller blue links.
- **Fix**: Native tab buttons (not DxButton Link). Active `::after` 3px `#0f766e` on the catalog bottom border. Utilities `margin-left: auto`. Recycle Bin stays an action link, not a third tab.
- **Test**: Ctrl+F5 case Resminamalar This profile and Shared. Tabs left, Recycle Bin + actions right. Active tab teal bar, not the same underline as Select all.
- **Prevent**: Do not put `app-item-doc-copies__action-btn` on content tabs. Do not group Recycle Bin in `role=tablist`.
- **Cross-skill**: preview-slot | resminamalar

### 2026-09-02 — Resminamalar This profile / Shared tab chrome (Resminamalar)

- **Need**: Case catalog tabs plus SHARED chip and green/grey ON/OFF pill matching the signed-off prototypes.
- **Fix**: `.resminamalar-catalog__pane-tabs` + `__utilities` (Recycle Bin stays a utility); `__shared-chip`; `__toggle--on` / `--off`. Shared tab uses `justify-content` so Recycle Bin sits on the right.
- **Test**: Ctrl+F5 catalog This profile and Shared. Download package hidden on Shared. Chip only on included shared rows.
- **Prevent**: Do not restyle Recycle Bin as a third content tab. Toggle CSS lives in `resminamalar-catalog.css`, not `site.css`.
- **Cross-skill**: preview-slot | resminamalar

### 2026-08-27 — Remove Duplicate from approval-leg occupant

- **Need**: Duplicate on in-use catalog rows and the locked editor footer is not wanted.
- **Fix**: Open only on cards; in-use footer is Save. Hint text no longer mentions Duplicate.
- **Test**: Ctrl+F5 catalog used row and Open used chain.
- **Prevent**: Do not re-add Duplicate on this occupant.
- **Cross-skill**: preview-slot | application-profile

### 2026-08-27 — Approval-leg slot side gutters

- **Need**: Catalog and editor sat too close to the left and right edges of `#visa-preview-slot`.
- **Fix**: `--approval-leg-pad-x: clamp(1.85rem, 5vw, 3rem)` on header, list, editor scroll, footer, and hints. Zero out shared `.resminamalar-slot-panel__catalog` horizontal padding so it does not fight the occupant gutters.
- **Test**: Ctrl+F5 catalog and unused editor — content inset from both sides.
- **Prevent**: Do not rely on `--resminamalar-slot-inset-x` for this occupant; it is too tight on a wide slot.
- **Cross-skill**: preview-slot | application-profile

### 2026-08-27 — New ministry in approval-leg occupant

- **Need**: Chain editor could only pick existing ministries.
- **Fix**: Inline **+ New ministry** in `ApprovalLegProfileSlotPanel` (not a second occupant).
- **Test**: Ctrl+F5 unused chain editor → New ministry form → Create ministry adds a leg.
- **Prevent**: Do not open XAF lookup UI from this occupant.
- **Cross-skill**: preview-slot | application-profile

### 2026-08-27 — Approval-leg slot catalog padding

- **Need**: Occupant catalog/editor looked tight vs other slot panels.
- **Fix**: `approval-leg-slot.css` — catalog list scroll + hint pinned; editor sections + sticky footer; switch for Active.
- **Test**: Ctrl+F5 on Edit in Configuration catalog and Open editor.
- **Prevent**: Do not put editor actions in the scrolling form body.
- **Cross-skill**: preview-slot | application-profile

### 2026-08-27 — Approval-leg catalog occupant

- **Need**: Wizard **Edit in Configuration** must use `#visa-preview-slot`, not an XAF modal.
- **Fix**: `VisaPreviewSlotMode.ApprovalLegCatalog`, `OpenApprovalLegCatalogAsync`, occupant key `approval-leg-catalog:tenant`, `ApprovalLegProfileSlotPanel` + `approval-leg-slot.css`.
- **Test**: Blazor Debug succeeded. Officer: Ctrl+F5, Configure profile → Edit in Configuration → slot catalog; X closes occupant; leaving the wizard view auto-closes (owner view id).
- **Prevent**: Do not add a File occupant for this — it is catalog CRUD, not a stored document.
- **Cross-skill**: preview-slot | application-profile

### 2026-08-26 — Field cues on all issued BOs from Application Profile Instance

- **Need**: Orange/blue/green was only on visa/WP/border-zone compose. Invitation, rejection, and native XAF New DetailViews for issued BOs had no cues.
- **Fix**: Compose cues on Invitation + Rejection headers too. Native new DetailViews for Invitation, InvitationItem, WorkPermit, WorkPermitItem, BorderZone, BorderZoneItem, Rejection, RejectionItem, Visa use `issued-field-cue.js`.
- **Test**: Blazor Debug succeeded. Officer: Ctrl+F5. New invitation / rejection / visa / WP / border zone in the slot; native New DetailView for those types also shows borders.
- **Prevent**: Do not leave `UseFieldCues` limited to WP/BZ. Nested item tables on compose have no extra property fields — cues are on the header (and WP person cards).
- **Cross-skill**: preview-slot | application-profile

### 2026-08-26 — Compose field border cues (visa / WP / border zone)

- **Need**: Officers could not see which compose fields were empty vs system-default vs already confirmed.
- **Fix**: Orange empty required, blue defaults to review, green after blur/change. Passport and computed expiration use sourced (not an edit target). CSS on `.issue-issued-header-slot__field--*`. Invitation/rejection tables unchanged.
- **Test**: Blazor Debug succeeded. Officer: Ctrl+F5, New issued visa / work permit / border zone — empty fields orange, defaults blue, Tab/blur turns filled fields green.
- **Prevent**: Do not use named C# args (`required: true`) in Razor `class=""` — Razor treats `required` as HTML. Nested `"` in `@onfocusout` breaks attributes; use single-quoted handlers.
- **Cross-skill**: preview-slot | application-profile

### 2026-08-26 — Work permit compose person cards in header slot

- **Need**: Same `#visa-preview-slot` occupant as invitation, but WP people need item fields.
- **Fix**: `IssueIssuedHeaderKind.WorkPermit` renders per-employee cards (visa card CSS). Invitation/rejection/border zone keep tables.
- **Test**: Blazor Debug 0 errors. Officer: Ctrl+F5, New work permit — cards, not checkbox grid.
- **Prevent**: Do not mix WP item fields into invitation people tables.
- **Cross-skill**: preview-slot | application-profile

### 2026-08-26 — Work permit compose person-card prototypes (not implemented)

- **Need**: Same slot as header compose, but WP people need visa-style cards instead of an include table.
- **Shipped (prototype only)**: `issue-work-permit-slot-01` … `04` + README. Implement later in `IssueIssuedHeaderSlotPanel` / compose service when officer accepts.
- **Test**: Officer review. Do not code until accepted.
- **Prevent**: Do not mix WP item fields into invitation/rejection/border-zone tables.
- **Cross-skill**: preview-slot | application-profile

### 2026-08-26 — Issued visa compose for visa-only profiles (roster)

- **Need**: Visa without invitation opened XAF modal; prototypes want the same slot as Path A with roster cards under **People on this case**.
- **Fix**: `IssueIssuedVisa` occupant for both families. `CanOpenInSlot` = ProduceVisa. Roster copy/banners in `IssueIssuedVisaSlotPanel`.
- **Test**: Occupant-key + `CanOpenInSlot` / `UsesInvitationSource` unit tests passed. Blazor Debug 0 errors. Officer: stop F5, Ctrl+F5, visa-only **+ Add issued visa**.
- **Prevent**: Do not require ProduceInvitation to open the visa occupant.
- **Cross-skill**: preview-slot | application-profile

### 2026-08-26 — New issued visa compose clipped the third person

- **Symptom**: Invitation 0010 has three people; New issued visa showed Andy and Serdar only. No scrollbar.
- **Root cause**: `.resminamalar-slot-panel__catalog` is `overflow: hidden`. Two tall visa cards filled the slot; Ali’s card was below the fold.
- **Fix**: `.issue-issued-header-slot-panel .resminamalar-slot-panel__catalog { overflow-y: auto }`. Sticky Create/Cancel stay at the bottom of the scroller.
- **Test**: Officer: Ctrl+F5 (CSS cache). New issued visa on 87-007 → scroll to Ali Enes Yetkin.
- **Prevent**: Compose occupants that reuse the Resminamalar catalog shell must override catalog overflow; document-copies scrolls inside `__body`, not the catalog wrapper.
- **Cross-skill**: preview-slot | application-profile

### 2026-08-26 — Visa Delete Clear throws on skip-nav collection

- **Symptom**: Case-list Delete showed ObservableCollection.Clear / NotifyCollectionChanged Reset error; visa stayed.
- **Fix**: `IssueIssuedVisaComposeService.Delete` unlinks input M2M with `Remove`, not `Clear`.
- **Test**: Module Debug build. Officer: rebuild, Ctrl+F5, Delete a0001.
- **Prevent**: Never `Clear()` XAF `ObservableCollection` navigations.
- **Cross-skill**: preview-slot | application-profile

### 2026-08-26 — Delete issued visa from the case list

- **Need**: Officers delete Path A visas created under **Visas issued by this case**, same as invitation Delete.
- **Fix**: **Delete** on each visa row. Confirm, then `IssueIssuedVisaComposeService.Delete` removes copies/images, clears `InvitationItem.IsUsed` / `IssuingInvitationItem`, and deletes the visa. Closes compose or copy preview if that visa is open. Refuses if the visa is skip-nav linked on another application.
- **Test**: `Delete_RejectsMissingArguments` passed. Blazor Debug build succeeded. Officer: stop F5, Ctrl+F5. Delete a0002 → confirm → row gone; invitation line unused again; invitation Delete still blocked until its visas are gone.
- **Prevent**: Do not delete the invitation when deleting a visa. Always clear `IsUsed` or the line stays locked.
- **Cross-skill**: preview-slot | application-profile

### 2026-08-26 — Issued visa row Preview opens copy in slot

- **Need**: Officers preview the visa scan from **Visas issued by this case** without opening compose, same as invitation **Preview**.
- **Fix**: Issued visa rows show **Preview** (not Delete). `HasCopy` from `VisaDocument`. Click opens `HeaderDocumentCopies` family `Visa` with `OpenPreviewOnly`. Number-click still edits in compose.
- **Test**: Unit tests `TryGetDocumentCopiesFamily_MapsIssuedVisaToVisaCopies` and `BuildIssuedTiles_IssuedVisaHasCopy_WhenVisaDocumentHasFile` passed. Blazor Debug build succeeded. Officer: stop F5, Ctrl+F5. a0002 with copy → Preview shows PDF in the slot; a0001 without copy stays disabled; invitation Preview unchanged.
- **Prevent**: Do not gate visa Preview on `canDeleteIssued` (that is Inv/WP/RJ/BZ only). Do not add a new slot mode.
- **Cross-skill**: preview-slot | application-profile | invitation-work-permit-document-copies

### 2026-08-26 — Issued visa row opens compose slot, not XAF

- **Symptom**: Clicking a visa under **Visas issued by this case** opened the native XAF Visa DetailView.
- **Fix**: Same intercept as invitation rows. Path A opens `IssueIssuedVisa` slot in **edit** (`ExistingVisaId`). Save updates the visa. Direct/extension profiles still use the modal.
- **Test**: Occupant-key unit test passed. `dotnet build Visa2026.Blazor.Server -c Debug` succeeded. Officer: stop F5, Ctrl+F5. Click a0002 → slot **Visa a0002** with compose fields + Save, not the XAF modal.
- **Prevent**: Do not call `ApplicationWorkspaceIssuedHeaderOpenHelper.TryOpen` for Path A issued visas.
- **Cross-skill**: preview-slot | application-profile

### 2026-08-26 — Border zone compose uses Visa popup editor

- **Symptom**: Invitation and Path A visa compose showed an inline checkbox wrap of zone names, not the Visa DetailView control (summary + … → “Border zones” popup).
- **Fix**: Replaced the grids with `BorderZoneLocationField` (`CommaSeparatedMultiSelectComponent`) on `IssueIssuedHeaderSlotPanel` and `IssueIssuedVisaSlotPanel`.
- **Test**: `dotnet build Visa2026.slnx -c Debug` succeeded. Officer: stop F5, rebuild, Ctrl+F5. Invitation Header Border zone looks like Visa (Ýok + …). Same control on each visa person card; OK persists; Create stamps `Visa.BorderZoneLocation`.
- **Prevent**: Do not draw catalog checkboxes in compose — reuse the Visa editor wrapper.
- **Cross-skill**: preview-slot | application-profile

### 2026-08-26 — Border zone on invitation and issued-visa compose

- **Need**: Officers set border zone on the invitation letter and see it defaulted on Path A visa cards.
- **Shipped**: Invitation compose Header **Border zone** checkboxes; visa person cards same catalog. Prefill invitation then case.
- **Test**: Stop F5, rebuild, Ctrl+F5. New invitation → pick zones → Save. + Add issued visa → cards show those zones; Create stamps `Visa.BorderZoneLocation`.
- **Cross-skill**: preview-slot | application-profile

### 2026-08-26 — Path A issued-visa compose occupant

- **Need**: Officer **+ Add issued visa** in `#visa-preview-slot` (prototypes 01–04), not an XAF modal.
- **Shipped**: `VisaPreviewSlotMode.IssueIssuedVisa` + `IssueIssuedVisaSlotPanel` + `IssueIssuedVisaComposeService`. Occupant key `issue-issued-visa:{appId}`. Open from workspace / property editors when ProduceVisa and ProduceInvitation. Direct/extension visa stays modal.
- **Test**: `dotnet build Visa2026.slnx -c Debug`; `IssueIssuedVisaComposeServiceTests.CanOpenInSlot_*` passed. Officer: stop F5, rebuild, Ctrl+F5. Invitation+WP case → + Add issued visa → cards under invitation letters → Create visas.
- **Prevent**: Do not put Issue visa back on invitation compose. Do not use input M2M InvitationItems as visa source.
- **Cross-skill**: preview-slot | application-profile

### 2026-08-26 — Issued-visa compose occupant prototypes (not implemented)

- **Need**: Path A **New issued visa** in `#visa-preview-slot` (same host as invitation compose).
- **Shipped (prototype only)**: `docs/prototypes/issue-issued-visa-slot-*.png`. New occupant planned; invitation compose stays without Issue visa shortcut.
- **Test**: Review PNGs before adding a slot mode.
- **Prevent**: Do not open XAF visa modal from **+ Add issued visa** once this occupant ships.
- **Cross-skill**: preview-slot | application-profile

### 2026-08-25 — Issued invitation row Preview opens copy in slot

- **Need**: Officers preview the uploaded invitation scan from Issued records without opening compose.
- **Fix**: Each Inv/WP/RJ/BZ issued row has **Preview**. Opens `HeaderDocumentCopies` with `OpenPreviewOnly` (same viewer as header copies). Disabled until a copy exists. Upload/remove on compose refreshes `HasCopy`.
- **Test**: Stop F5, rebuild, Ctrl+F5. Upload copy on 30 → list **Preview** enabled → click shows PDF in right slot. 31 without copy stays disabled.
- **Prevent**: Do not open compose on Preview; keep number-click for edit. Do not add a new slot mode.
- **Cross-skill**: preview-slot | application-profile | invitation-work-permit-document-copies

### 2026-08-25 — Upload invitation copy from compose slot

- **Need**: Officers attach a scan/PDF of the invitation from the compose panel, not only XAF Documents tab.
- **Fix**: `Invitation copy` card with Upload / Remove. Stores `InvitationDocument` + `FileData` (same as header document copies). Edit saves immediately; New invitation attaches on Create. PDF/PNG/JPEG/TIFF/GIF/BMP up to 5 MB.
- **Test**: Stop F5, rebuild, Ctrl+F5. Edit 30 → Upload copy → file listed. Remove works. New invitation: pick file then Create.
- **Prevent**: Do not invent a second file store — use `Invitation.Documents`.
- **Cross-skill**: preview-slot | application-profile | invitation-work-permit-document-copies
### 2026-08-25 — Available-on-case card: no dashed border

- **Symptom**: Officer disliked the dashed border around **Available on this case**.
- **Fix**: Removed `border-style: dashed`; section uses the same solid card border as Header / People on letter.
- **Test**: Ctrl+F5 on invitation edit — Available card is solid, not dashed.
- **Cross-skill**: preview-slot
### 2026-08-25 — Split assigned vs available invitation people

- **Symptom**: Unassigned people in the same table as the letter made the invitation look incomplete.
- **Fix**: Invitation compose uses two cards — **People on letter** (assigned, Remove) then dashed **Available on this case** (Add). Matches stacked Header → letter → add → actions layout. Occupied-on-other stay hidden.
- **Test**: Stop F5, rebuild, Ctrl+F5. Open 30 → Andy in letter; Ali in Available with Add. Add moves them up; Save persists.
- **Prevent**: Do not mix Include=false rows into the letter table.
- **Cross-skill**: preview-slot | application-profile
### 2026-08-25 — Unassigned people stay visible on invitation edit

- **Symptom**: Third roster person (not on 30 or 31) was hidden on edit; officers could not add them to either letter.
- **Fix**: Edit lists people on this letter **plus** people not yet on any invitation. People already on another letter stay hidden.
- **Test**: Stop F5, rebuild, Ctrl+F5. Open 30 or 31 → unassigned person appears unchecked; check + Save adds them.
- **Prevent**: Do not filter edit to ExistingLineId-only.
- **Cross-skill**: preview-slot | application-profile
### 2026-08-25 — Invitation compose hides people on another letter

- **Symptom**: Edit invitation 31 still listed Andy as "On invitation 30" — full roster on every letter was confusing.
- **Fix**: `LoadDraft` omits people already on another invitation. `LoadExistingDraft` lists only people on that letter. New invitation shows remaining (unassigned) people only.
- **Test**: Stop F5, rebuild, Ctrl+F5. Open 31 → only that letter's people. Open New invitation → only people not yet on a letter.
- **Prevent**: Do not show locked "On invitation N" rows; hide occupied people instead.
- **Cross-skill**: preview-slot | application-profile
### 2026-08-25 — Delete issued invitation from Application Profile Instance

- **Symptom**: Officers could create/edit invitations on the case but not delete them from Issued records.
- **Fix**: List-row **Delete** + slot **Delete** (edit mode). `IssueIssuedHeaderComposeService.Delete` removes unused invitation (items, documents, images). Refuses if any line has `IsUsed` or `IssuedVisa`. Workspace refresh; closes slot if that header is open. Same list action for WP/RJ/BZ. Issued visa still has no delete.
- **Test**: Stop F5, rebuild, Ctrl+F5. Overview → Invitation → Delete → confirm → row gone. Invitation with a visa → error. Slot edit Delete same.
- **Prevent**: Do not fall back to XAF modal delete for compose kinds; keep the visa-consumption guard.
- **Cross-skill**: preview-slot | application-profile
### 2026-08-25 — Invitation 005 list click still opened XAF modal

- **Symptom**: After compose wiring, screenshot still showed Invitation DetailView modal for **005**.
- **Root cause**: List-row **Open** used `TryOpen` → modal. New used EventCallback/JS that could no-op when hostRef null but return success. User often clicks existing **005**, not only New.
- **Fix**: Case workspace opens Inv/WP/RJ/BZ via `OpenIssuedHeaderInSlotAsync` (JS + DI) for **New and Open**; `TryOpen` refuses those kinds; `ExistingHeaderId` loads success view in slot.
- **Test**: Stop F5, rebuild, Ctrl+F5. Click **New invitation** OR list **005** → right slot, never center modal.
- **Cross-skill**: preview-slot | application-profile


### 2026-08-25 — New invitation opened XAF modal instead of IssueIssuedHeader slot

- **Symptom**: Case workspace **New invitation** still showed Invitation DetailView modal.
- **Root cause**: (1) Compose open via App-circuit `IVisaPreviewSlotService` may not reach `#visa-preview-slot` host; (2) failed/missed compose fell back to `TryCreate` modal.
- **Fix**: Open via `visaPreviewDrawer.openIssueIssuedHeader` → host `OpenIssueIssuedHeaderFromJsAsync`; PropertyEditors use `IssueIssuedHeaderPreviewSlotOpenHelper`; `TryCreate` redirects Inv/WP/RJ/BZ to compose (no modal). Issued visa stays modal.
- **Test**: Stop F5, rebuild, F5, Ctrl+F5 → New invitation → right slot compose (not center modal).
- **Prevent**: Do not fall back to modal DetailView for issued-header compose kinds.
- **Cross-skill**: preview-slot | application-profile


### 2026-08-25 — IssueIssuedHeader occupant (compose New invitation/WP/RJ/BZ)

- **Symptom**: Issued-header create used modal DetailView; prototypes required `#visa-preview-slot` compose.
- **Fix**: New mode `IssueIssuedHeader`, `IssueIssuedHeaderSlotPanel`, CSS `issue-issued-header-slot.css`, `OpenIssueIssuedHeaderAsync` / `ForIssueIssuedHeader` key. Host branch + Version remount.
- **Wiring**: OfficerShell / ApplicationWorkspace New buttons → `TryOpenCompose` (fallback modal for issued visa).
- **Prevent**: Do not add four separate slot modes for Inv/WP/RJ/BZ — one parameterized occupant.
- **Cross-skill**: preview-slot | application-profile


### 2026-08-19 — Wizard template Preview must use visaPreviewDrawer.open JS

- **Symptom**: Configure profile Templates showed Preview links, but `#visa-preview-slot` never opened (wizard stayed full width).
- **Root cause**: File occupant lives on the `VisaPreviewSlotHost` root in `_Host.cshtml`. Grid file links already call `window.visaPreviewDrawer.open` → host `OpenFileFromJsAsync`. Wizard called C# `IVisaPreviewSlotService.OpenFileAsync` on the XAF `App` circuit, so the host never received File mode.
- **Fix**: Wizard Preview invokes `visaPreviewDrawer.open(sourceType, objectId, null, ownerViewId)`. Host `OpenFileFromJsAsync` accepts ownerViewId. File drawer load waits until `@ref` exists (OnAfterRender).
- **Test**: Stop F5, rebuild, F5, Ctrl+F5. Templates & person → Preview Borcnama → right slot PDF with placeholders.
- **Prevent**: Do not open the File occupant only via `@inject IVisaPreviewSlotService` from an XAF property editor.
- **Cross-skill**: preview-slot | application-profile

### 2026-08-19 — Application Profile wizard template Preview uses File occupant

- **Symptom**: Officers needed to see the actual Word/Excel layout from Configure profile Templates & person, not a SAMPLE sketch.
- **Try**: Reuse `#visa-preview-slot` File occupant (`OpenFileAsync`). Convert stored .docx/.xlsx to PDF with `ApplicationWordReportOfficePreviewPdfConverter`. Do not open Resminamalar (no live application to merge).
- **Test**: `dotnet build Visa2026.slnx -c Debug`. Manual: stop F5, rebuild, F5. App_Inv_And_WP → Templates & person → Preview on Shared (e.g. Borcnama) and a Profile-specific file → slot shows layout PDF; Close X; leaving Configure closes the slot.
- **Root cause**: Wizard is configuration, not merge.
- **Fix**: `user-report-template` and `application-profile-template` `IFilePreviewSource`s; Preview links on both lists and Edit modal. Wizard ObjectSpace first so unsaved uploads still preview.
- **Prevent**: Do not add a new `VisaPreviewSlotMode` for template look. Do not iframe .docx.
- **Cross-skill**: preview-slot | application-profile | resminamalar

### 2026-08-17 — Ministry letter Preview never iframes the PDF

- **Symptom**: Workspace Progress **View letter** on a filled Application form (`ApplicationForm_…pdf`) showed Chrome’s PDF toolbar and the XFA “Please wait / Adobe Reader” sheet plus Spire evaluation text.
- **Root cause**: `ContainsXfa` looked for uncompressed `/XFA`. Spire-filled letters often keep that key in a Flate stream, so preview used an iframe. PDFium cannot paint XFA.
- **Fix**: `ProgressLettersInlinePreview` always uses pdf.js (`visaXfaPreview`) for non-image letters (XFA layer, canvas fallback for scans). `PdfXfaDocument.ContainsXfa` also scans Flate streams and `xdp:xdp`.
- Test: `PdfXfaDocumentTests`. Stop F5, rebuild, F5, Ctrl+F5. On **B/-010** Progress → View letter → Şahsy kagyzy in the slot, not Please wait. Download unchanged.
- Prevent: Do not iframe ministry-letter PDFs. Do not Spire-flatten for Chrome.
- Cross-skill: visa2026-application-progress | visa2026-document-copies

### 2026-08-17 — Approval letter Preview uses pdf.js when the file is XFA

- Workspace Progress letter click iframed the uploaded PDF. Chrome showed the XFA “Please wait / Adobe Reader” sheet (often a filled Application form saved as the ministry letter, with a Spire evaluation banner). Download still opened in Foxit.
- **Fix**: `PdfXfaDocument.ContainsXfa` (`/XFA` marker). `ProgressLettersInlinePreview` renders those bytes with `visaXfaPreview` (same host as Document copies Application form). Scans and images stay iframe/img. Download is unchanged.
- Test: `PdfXfaDocumentTests`. Stop F5, rebuild, F5, Ctrl+F5. On **B/-008** Progress → click the Energetika letter → Şahsy kagyzy in the slot, not Please wait.
- Prevent: Do not iframe XFA ministry letters. Do not Spire-flatten for Chrome.
- Cross-skill: visa2026-application-progress | visa2026-document-copies | visa2026-pdf-form-mapping

### 2026-08-17 — Application form occupant uses pdf.js, not the slot iframe

- Document copies Application form Preview stays **OpenPreviewOnly** in `#visa-preview-slot`. Chrome cannot iframe XFA; the occupant renders pdf.js XFA HTML (`visaXfaPreview`) inside a host div. Slot theme sync still styles the chrome only — black paper was SVG `rect` fill, not `dxbl-theme`.
- Prevent: Do not iframe filled application forms. Do not assume slot dark-theme CSS is the paper color.
- Cross-skill: visa2026-document-copies

### 2026-08-15 — Document copies By type Preview still uses OpenPreviewOnly

- Workspace **By type** section Preview opens the existing Document copies occupant as **viewer only** (`FocusSlotKey` = `Family:{family}`, chip-selected person ids). `DocumentCopiesInlinePreview` parses the family key and calls `TryGetMergedFamilyPdf`.
- Prevent: Do not add a second catalog in `#visa-preview-slot` for type Preview. Do not invent a new occupant.
- Cross-skill: visa2026-document-copies

### 2026-08-15 — Document copies Preview needs the case id

- Workspace Preview is still `OpenPreviewOnly` in `#visa-preview-slot`. Merge must receive `ApplicationProfileInstanceId` from the occupant request; without it a person on several imported cases fails to load the PDF.
- Prevent: Do not resolve the roster for workspace Preview with `applicationId: Guid.Empty`.
- Cross-skill: visa2026-document-copies

### 2026-08-15 — Document copies person Preview still uses OpenPreviewOnly

- Workspace Document copies is now person-grouped. Row Preview still opens the existing Document copies occupant as **viewer only** (`FocusSlotKey` + `OpenPreviewOnly`). The request may carry one person id so the merge is not the whole roster.
- Prevent: Do not add a new slot occupant or show the catalog again in `#visa-preview-slot` from the case tab.
- Cross-skill: visa2026-document-copies | visa2026-application-profile

### 2026-08-14 — Done ministry steps expose letter preview links

- Workspace Progress/Overview hid approval files once the step was no longer current. `ProgressLetters` preview still works; the timeline now keeps `MinistryLetterFileName` on done legs and the UI shows the filename on Progress + Overview.
- Prevent: Do not clear letter metadata on `BuildFilledStep` when `slotState != current`.
- Cross-skill: visa2026-application-profile | visa2026-application-progress

### 2026-08-14 — Progress tab ministry letter opens side preview (OpenPreviewOnly)

- **Symptom**: Case workspace Progress showed the uploaded approval PDF as an `<a target="_blank">` download. Officers could not preview it in `#visa-preview-slot`.
- **Try**: Reuse existing `ProgressLetters` occupant (same as Document copies / Resminamalar workspace Preview).
- **Test**: `dotnet build Visa2026.slnx -c Debug` (0 errors). Manual: stop F5, rebuild, F5. Open 8/-005 Progress → click the ministry letter filename → slot shows inline PDF; Close preview closes the slot (not a new browser tab).
- **Root cause**: Workspace Progress never called `IVisaPreviewSlotService.OpenProgressLettersAsync`. `ProgressLettersSlotRequest` had no `OpenPreviewOnly`, so even a catalog open would not match the locked “tab owns list, slot is viewer” rule.
- **Fix**: `OpenPreviewOnly` + `FocusDisplayName` on `ProgressLettersSlotRequest`; `ProgressLettersSlotPanel` skips catalog and opens `ProgressLettersInlinePreview`; Progress filename is a button that opens the slot with `FocusProgressId`. Owner view id from Application workspace / Officer shell hosts.
- **Prevent**: Do not add a new occupant or `<a href="/api/.../ministry-letter">` for workspace preview. Do not use `.app-progress-letter-link` on the workspace button (that class is captured by `_Host.cshtml` for the XAF grid catalog path).
- **Cross-skill**: preview-slot | visa2026-application-profile | visa2026-application-progress

### 2026-07-31 — Templates brand mark for report package slot

- **Ask**: Dedicated Resminamalar/Templates brand; officer name Templates.
- **Fix**: Slot title `.templates-brand-title`; CSS linked in `_Host.cshtml`; contract in `PREVIEW_SLOT.md` § Templates brand mark.
- **Prevent**: Do not reuse DocumentCopies brand classes for Templates.
- **Cross-skill**: resminamalar | preview-slot

### 2026-07-31 — Resminamalar catalog chrome (flat cards, shared tokens)

- **Ask**: Update Resminamalar slot catalog look to match Document Copies cards.
- **Fix**: Added `resminamalar-catalog.css` (reuses `--dcc-*` tokens); contract in `docs/PREVIEW_SLOT.md` § Resminamalar catalog chrome; linked from `_Host.cshtml`.
- **Prevent**: Catalog card CSS stays out of preview mode; do not force Document Copies section Open onto Resminamalar.
- **Cross-skill**: resminamalar | preview-slot

### 2026-07-30 - New surface must live in main area, not as an occupant; OwnerViewId from a PropertyEditor

- **Symptom (design-time)**: A person dossier page that also opens document copies would fight the slot: `Open*` is last-wins with **one occupant at a time**, so hosting the dossier as an occupant means opening copies evicts the dossier the officer is reading.
- **Fix**: Dossier renders in the **main content area** (`#visa-app-shell` grid `1fr` + slot width), so data and scans are visible side by side. Slot stays reserved for files.
- **Second trap**: `VisaPreviewSlotCloseController` closes the slot when the **owning View** deactivates. Navigating search -> dossier would therefore close a slot opened from the previous view.
- **Fix**: Pass the dossier view id explicitly. A `BlazorPropertyEditorBase` has no `View`, so `VisaPreviewSlotViewHelper.ResolveOwnerViewId(view)` is unavailable - added the constant `PersonDossierViewIds.DetailView` (`"PersonDossierHost_DetailView"`) and passed it as `ownerViewId`.
- **Prevent**: When opening the slot from a property editor / component rather than a `ViewController`, use a view-id constant; do not pass `null` (that makes the slot owner-less and closes unpredictably).
- **Not verified in a running app session**: build only.
- **Cross-skill**: person-document-copies

### 2026-06-06 — Catalog card UX polish (Resminamalar + Document copies)

- **Symptom**: Inline catalog cramped, duplicate report names, centered card floating in empty space; preview viewer incorrectly narrowed when catalog CSS applied globally.
- **Try**: Open Resminamalar slot → long template names → Preview Excel → Close preview.
- **Test**: Single display name per row; top-aligned full-width card; list grows with slot height; preview uses full slot width after hard-refresh.
- **Root cause**: Separate `slot-entry` markup; `OutputFileName` shown under `DisplayName`; fixed `max-height` on list; catalog `max-width` leaked into `--preview` mode.
- **Fix**: Unified `group-head` rows; hide `OutputFileName` when `UseInlinePreview`; shared `.resminamalar-slot-panel` card CSS for `app-report-package--inline-slot` and `app-item-doc-copies--inline-slot`; explicit preview full-width rules under `--preview`.
- **Prevent**: Scope catalog layout to catalog selectors only; feature skills own content, **preview-slot** skill owns shell CSS split.
- **Cross-skill**: resminamalar, document-copies

### 2026-06-06 — Occupant switch left stale inline PDF (historical)

- **Symptom**: Application Resminamalar open; ApplicationItem Resminamalar left previous PDF visible.
- **Root cause**: Panel reused without remount; `_previewActive` stuck; close on any view deactivate.
- **Fix**: `OccupantKey`, `Version`, `@key`, owner-aware close — see [resminamalar/learnings.md](../visa2026-resminamalar/learnings.md) same date.
- **Prevent**: Any new occupant must bump `Version` and reset local preview flags.
- **Cross-skill**: resminamalar

## 2026-07-31 - Document-copies catalog chrome shared

**Ask:** Person / AppItem / Header document copies should look the same (dossier-adjacent sectioned tables).

**Fix:** Extracted `.doc-copies-catalog*` to `wwwroot/css/document-copies-catalog.css`; Person/Header/AppItem retargeted. Shell remains `resminamalar-slot-panel`. Contract documented in `docs/PREVIEW_SLOT.md`.

**Prevent:** Do not reintroduce `app-item-doc-copies__group` cards for copies catalogs.

### 2026-07-31 — Prototype A nav across all document-copies catalogs

- **Ask**: Apply Foxit-style vertical nav layout across all document copies in the project.
- **Fix**: Shared `DocumentCopiesCatalogNavIcons`; Person/Header/ApplicationItem section heads use `__section-head--nav` + Open/Close; exclusive expand (AppItem/Person multi-section); Header single Documents card collapsed by default.
- **Prevent**: Do not keep flat always-open section heads on Header/AppItem while Person has nav cards.
- **Cross-skill**: person-document-copies | document-copies | invitation-work-permit-document-copies | preview-slot

### 2026-07-31 — Dedicated Document copies brand mark (smiling paperclip)

- **Ask**: Use a dedicated icon/label for Document copies across the project (pill + smiling paperclip).
- **Fix**: `DocumentCopies.svg` (XAF ImageName), `document-copies-clip.svg` + `document-copies-brand.css`, `DocumentCopiesBrandMark`; wired toolbar actions (Person/Header/AppItem), ListView Copies pills, dossier button, slot titles.
- **Prevent**: Do not reuse `BO_FileAttachment` for document-copies entry points; use `DocumentCopies` / `.doc-copies-brand*`.
- **Cross-skill**: person-document-copies | document-copies | invitation-work-permit-document-copies | preview-slot | person-dossier
## 2026-08-25 — Invitation per person (compose)

- New invitation compose defaults **CreateSeparatePerPerson**; people already on another invitation from the case are unchecked with status `On invitation N`.
- When the toggle is on and 2+ people are selected, `CreateSeparateInvitations` creates one header each (numeric base increments: 006→006,007; else suffix `-1`,`-2`), single commit.
- Officers can still turn the toggle off to put multiple people on one letter.
## 2026-08-25 — Shared vs separate invitation (both modes)

- Default letter mode is **single invitation for all selected**; separate-per-person is opt-in radio.
- First invitation on a case pre-checks all ready people; later New invitations pre-check only people not yet on a letter, but **Select all ready** / re-check includes them on a shared letter.
## 2026-08-25 — One invitation per person per application

- Domain rule: a Person may have at most one issued invitation letter within the same `ApplicationProfileInstance` (shared multi-person letter OR separate letters — not both for the same person).
- Compose locks people already on a letter; Create/Update reject duplicates via `FindPeopleAlreadyOnInvitation`.

## 2026-08-25 — No letter-mode radios

- Removed Single/Separate invitation radios. Create always puts checked people on one letter; issue another invitation later for remaining unlocked people (one person per application still enforced).
## 2026-08-25 — Issued-header list switch left stale preview

- Symptom: open invitation 005 then 006 — slot kept 005.
- Cause: `openIssueIssuedHeader` returned true before `invokeMethodAsync` finished; CaseWorkspace skipped DI fallback. Panel could keep prior draft.
- Fix: await JS promise; always DI `OpenIssueIssuedHeaderAsync` then JS; Host `@key` includes OccupantKey; panel reloads when ExistingHeaderId changes; capture row id in list click.

## 2026-08-25 — People on letter stayed on prior invitation

- Symptom: switching 005→006 updated header fields but People checkboxes/status stayed on 005.
- Fix: exclude current invitation from occupancy map when loading edit draft; rebuild person line objects; `@key` people table/rows/checkboxes on OccupantKey + include/status.

## 2026-08-25 — Expiry save pulled person from another invitation

- Symptom: changing invitation 010 expiry then Save added a person who belonged on 009.
- Cause: Update always synced InvitationItems from Include flags (stale after switch); occupancy/dup checks used lazy navigations.
- Fix: UpdateInvitation skips people sync when selected ids match persisted ids; occupancy + dup use `GetObjectsQuery<InvitationItem>`.

## 2026-08-25 — SyncPeopleOnSave for header-only invitation edits

- Comparing selected vs persisted people still failed in practice (stale Include).
- Panel now sets `draft.SyncPeopleOnSave` only when the people fingerprint changes; `UpdateInvitation` skips InvitationItems entirely when false (expiry/date-only Save).

## 2026-08-25 — Expiry save added people: Invitation.OnSaving

- Root cause: `Invitation.OnSaving` called `EnsureRosterInvitationItems`, which added every roster person not already on THAT letter. Saving 010 expiry therefore attached the person from 009.
- Fix: removed OnSaving auto-fill; helper used only at create and skips people already on another invitation of the same application.
