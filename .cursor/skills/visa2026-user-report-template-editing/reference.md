# User report template in-app editing — reference

**Plan:** [`docs/USER_REPORT_TEMPLATE_IN_APP_EDITING_PLAN.md`](../../../docs/USER_REPORT_TEMPLATE_IN_APP_EDITING_PLAN.md)

---

## Phase 1 implementation checklist

Copy and track in PR / chat:

```text
Phase 1 — TemplateEditor occupant
- [ ] VisaPreviewSlotMode.TemplateEditor + TemplateEditorSlotRequest
- [ ] VisaPreviewSlotOccupantKeys.template-editor:{id}:return:…
- [ ] IVisaPreviewSlotService.OpenTemplateEditorAsync
- [ ] VisaPreviewSlotHost.razor branch → TemplateEditorSlotPanel.razor
- [ ] TemplateEditorWordBody.razor (DxRichEdit, OpenXml)
- [ ] TemplateEditorExcelBody.razor (Download + Replace .xlsx)
- [ ] IUserReportTemplateMaintenanceService (refactor from UserReportTemplateController)
- [ ] ApplicationReportPackageComponent — gear → OpenTemplateEditorAsync (not target="_blank")
- [ ] Footer: Save, Back to reports, Preview report, placeholder help
- [ ] Unsaved confirm on Back / occupant switch
- [ ] docs/PREVIEW_SLOT.md — TemplateEditor row (planned → shipped)
- [ ] USER_TEMPLATE_AUTHOR_GUIDE.md — officer steps when shipping
```

---

## File map (from plan §6)

| Area | Path |
|------|------|
| Slot mode + request | `Visa2026.Module/Services/PreviewSlot/` |
| Occupant keys | `VisaPreviewSlotOccupantKeys.cs` |
| Host service | `Visa2026.Blazor.Server/Services/VisaPreviewSlotService.cs` |
| Host UI | `Visa2026.Blazor.Server/Components/VisaPreviewSlotHost.razor` |
| Editor panel | `TemplateEditorSlotPanel.razor` |
| Word body | `TemplateEditorWordBody.razor` |
| Excel body (Phase 1) | `TemplateEditorExcelBody.razor` |
| Excel Univer (Phase 3a) | `TemplateEditorExcelUniverBody.razor` |
| Excel ONLYOFFICE (Phase 3b) | `TemplateEditorExcelOnlyOfficeBody.razor` |
| Resminamalar entry | `ApplicationReportPackageComponent.razor` |
| BO | `UserReportTemplate.cs` — `TemplateFile` (`FileData`) |
| Today’s extract/validate | `UserReportTemplateController.cs` |
| Today’s edit link | `UserReportTemplateEditLinkService.cs` |

---

## Phase 1b — Univer POC checklist

```text
- [ ] Spike folder: tools/ExcelTemplateSpike/ (optional)
- [ ] Import 433_gurlusyk_ckl.xlsx → edit header/row placeholder cell → export
- [ ] Same for Sanaw_hasaba_alys.xlsx (merged headers, {{#ds.rows}})
- [ ] ClosedXML merge / Resminamalar preview unchanged except intentional edit
- [ ] ExcelTemplateSpike extract finds all placeholders post round-trip
- [ ] Record pass/fail in plan §7.5
```

---

## Phase 3b — ONLYOFFICE sketch

| Item | Notes |
|------|--------|
| Container | `onlyoffice-documentserver` on company Ubuntu compose |
| Serve file | `GET /api/user-report-templates/{id}/document` (auth + Write) |
| Callback | `POST /api/onlyoffice/callback` — status `2` → fetch `url` → `FileData` |
| Slot UI | iframe `DocsAPI.DocEditor`, `documentType: "cell"` |
| Legal | AGPL sign-off before prod embed |

---

## Existing code pointers

- **Permissions:** `UserReportTemplate` Write + `FileData` Write (officer role).
- **Extract/Validate:** `UserReportTemplateController` — refactor to shared service for Blazor + XAF.
- **Edit link today:** `UserReportTemplateEditLinkService` — keep for admin list deep-link only.
