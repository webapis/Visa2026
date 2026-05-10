# Application supporting-document package — implementation plan

> **Filename note:** `APPLICATION_DIPLOMA_PACKAGE_PLAN.md` is kept for continuity; scope includes **diplomas (education)**, **passport copies** (current + previous), **visa copies** (current + previous), **medical record** (**`CurrentMedicalRecord`**), **address of residence** (**`CurrentAddressOfResidence`**), **work permit** scans for **employee** applicants only (**`Person.IsEmployee == true`**, parent **`WorkPermit`** via **`CurrentWorkPermitItem`** / **`PreviousWorkPermitItem`**, with **deduplication** across application lines), **invitation** scans (parent **`Invitation`**, via **`CurrentInvitationItem`** only — same parent-file pattern as work permit, with **`Invitation.ID`** deduplication), and — when the applicant **`Person`** is a **family member** (**`Person.IsEmployee == false`**) — **relationship evidence** via **`PersonDocument`** (**`FileData`**) in one ZIP. **v1 ZIP packing uses `FileData` / `*Document` rows only** (§4.8); **`ImageBase` / `byte[]` / `*Image`** attachments (including **`FamilyMemberImage`**) are **not** read by the worker to limit memory use on **Docker** on the **droplet**. The **`Relationship`** lookup (**`Person.Relationship`**) has **no** attachments; scans for the ZIP are **file-backed** per §4.8.

This document describes **how** we will add **scanned / uploaded copies** to the same official document workflow as application PDFs: **binary attachments** from linked business objects on **`ApplicationItem`**, packaged in the **existing ZIP / background batch** pattern (`Generate PDF` → `PdfGenerationBatch` → worker). It is the agreed design **before** coding.

## 1. Context

### 1.1 Single entry point: same **Generate PDF** as the visa application form

We **reuse the existing XAF action** on **`ApplicationItem`** — **Generate PDF** (`ApplicationItemPdfController` → `PdfGenerationBatch`) — the same flow that produces filled PDFs from **`PdfSettings:TemplatePath`** (typically **`Visa2026.Module/Resources/Visa_Application_TM_QR_08.pdf`**, also available as an embedded fallback in the worker).

- **One button**, **one queued batch**, **one completed `ZipFile`** per job (same **My PDF Jobs** / download experience as today).
- The worker continues to fill **one application PDF per selected `ApplicationItem`** exactly as now; **supporting files** (diplomas, passport, visa, medical record, address of residence, work permit, invitation — per batch flags) are **additional ZIP entries** when options say so (see §5.1).
- **No separate actions** for each document type are required for the baseline; one **Generate PDF** run can include any combination via batch options.

### 1.2 Domain recap

- Users create an **`Application`** with **`ApplicationItem`** rows (one per person on the application).
- They already generate **filled visa/application PDFs** per item and download a **ZIP** via **`PdfGenerationBatch`** (queued from **`ApplicationItemPdfController`**, processed by **`PdfGenerationBatchWorkerService`** in the Blazor host).
- **Education (diplomas):** **`Person`** has many **`Education`** records. The BO may have both **`EducationImage`** (`ImageBase`) and **`EducationDocument`** (`DocumentBase` / **`FileData`**); **v1 ZIP packing reads `EducationDocument` only** (§4.8). **`ApplicationItem.CurrentEducation`** is selected per item for forms; **which educations to pack** is a batch option (§4.1), gated by **§1.3** (**`Person`** must be set; **current-only** scope also requires **`CurrentEducation`** set on the item).
- **Passport:** **`ApplicationItem.CurrentPassport`** and **`ApplicationItem.PreviousPassport`**. Each **`Passport`** may have **`PassportImage`** and **`PassportDocument`** in the app; **v1 packer emits `PassportDocument.File` only** (§4.8). Pack **only** when each **`ApplicationItem`** link is **set** (non-null); see §1.3.
- **Visa:** **`ApplicationItem.CurrentVisa`** and **`ApplicationItem.PreviousVisa`**. **`VisaDocument`** only in v1 ZIP (§4.8). Pack **only** when the corresponding **`ApplicationItem`** property is set (§1.3).
- **Medical record:** **`ApplicationItem.CurrentMedicalRecord`**. **`MedicalRecordDocument`** only in v1 ZIP (§4.8). Pack **only** when **`CurrentMedicalRecord`** is set on the item (§1.3).
- **Address of residence:** **`ApplicationItem.CurrentAddressOfResidence`**. **`AddressOfResidenceDocument`** and, when lodging applies, **`LodgingDocument`** only in v1 ZIP (§4.8). Pack **only** when that property is set on the item (§1.3).
- **Work permit (employees only):** **`ApplicationItem.CurrentWorkPermitItem`** and **`ApplicationItem.PreviousWorkPermitItem`** apply **only** when **`ApplicationItem.Person.IsEmployee == true`** (employee). **`WorkPermitItem`** is an employee-side concept; **`ApplicationItem.ApplyCurrentFieldsFromSelectedPerson`** clears both work permit slots when the selected **`Person`** is **not** an employee. The ZIP worker must **not** emit work permit files for **family member** lines — even if a slot were non-null from legacy data. When **`Person.IsEmployee == true`**, each slot must still be **set** on the item to contribute (§1.3). Each **`WorkPermitItem`** belongs to a parent **`WorkPermit`**. Parent attachments in the model include **`WorkPermitImage`** and **`WorkPermitDocument`**; **v1 packer reads `WorkPermitDocument` only** (§4.8) — **not** on **`WorkPermitItem`**. Multiple **employee** application lines may reference **different** **`WorkPermitItem`** rows that still share the **same** **`WorkPermit`**; the ZIP must include that parent’s files **once per distinct `WorkPermit` in the batch** (§4.5). **Persistence:** EF maps **`PreviousWorkPermitItem`** to the existing SQL column **`SecondWorkPermitItemId`** (see **`Visa2026DbContext`**) so existing databases do not require a column rename.
- **Invitation:** **`ApplicationItem.CurrentInvitationItem`** links to **`InvitationItem`** (there is **no** `PreviousInvitationItem` on **`ApplicationItem`**). Each **`InvitationItem`** belongs to a parent **`Invitation`**. Parent **`InvitationDocument`** only in v1 ZIP (§4.8). Several **`ApplicationItem`** lines may reference **different** **`InvitationItem`** rows that still share the **same** **`Invitation`**; the ZIP must include that parent’s files **once per distinct `Invitation` in the batch** (§4.7).
- **Family member vs employee (`Person.IsEmployee`):** **`Person`** is a single BO; **`IsEmployee == true`** means the record is the **employee** (work permit items, company, etc.); **`IsEmployee == false`** means a **family member** of a sponsoring employee (**`Person.SponsoringEmployee`**) with a required **`Person.Relationship`** lookup (spouse, child, …). **`Relationship`** itself is a **`LookupBase`** row — **no** `Image` / `FileData` on that type. For this ZIP feature, **relationship packaging applies only when `Person.IsEmployee` is false** and packs **`PersonDocument`** (**`FileData`**) only (§4.6, §4.8). The UI may still offer **`FamilyMemberImage`** (`byte[]`) for other workflows; it is **out of scope** for the v1 supporting-doc worker.

### 1.3 Item-level eligibility — **`ApplicationItem`** links must be **set** (non-null)

Supporting ZIP content for **document slots** is driven **only** by what is **assigned on `ApplicationItem`**, not by “what exists somewhere on **`Person`**.” In EF/XAF terms, each relevant navigation / FK on **`ApplicationItem`** must be **non-null** before that branch is **eligible** for file generation under the corresponding include flag. **Do not** infer a previous passport, previous visa, second work permit, alternate address, etc. from **`Person.Passports`**, **`Person.MedicalRecords`**, or other collections when the **`ApplicationItem`** property for that slot is **unset**.

| `ApplicationItem` property | Eligibility for packing that branch |
|-----------------------------|-------------------------------------|
| **`CurrentPassport`** | **`Include passport copies`**: pack **`Current/…`** only if **`CurrentPassport`** is set. |
| **`PreviousPassport`** | Pack **`Previous/…`** only if **`PreviousPassport`** is set. If unset, **no** previous-passport files for that item — even if **`Person.Passports`** contains another passport. |
| **`CurrentVisa`** / **`PreviousVisa`** | Same rule: each side only if the matching property on **`ApplicationItem`** is set. |
| **`CurrentMedicalRecord`** | **`Include medical record copies`**: only if **`CurrentMedicalRecord`** is set. |
| **`CurrentAddressOfResidence`** | **`Include address of residence copies`**: only if **`CurrentAddressOfResidence`** is set. |
| **`CurrentWorkPermitItem`** / **`PreviousWorkPermitItem`** | **`Include work permit copies`**: a parent **`WorkPermit`** is considered for dedupe/packing only when reached through a **set** slot on **`ApplicationItem`** **and** **`ApplicationItem.Person`** is **set** with **`Person.IsEmployee == true`** (null slot → that slot does not contribute; **family member** lines → **no** work permit packing — §4.5). |
| **`CurrentInvitationItem`** | **`Include invitation copies`**: parent **`Invitation`** is in scope only when **`CurrentInvitationItem`** is **set** on **`ApplicationItem`** (§1.3, §4.7). |
| **`CurrentEducation`** | **`Include diploma files`**, scope **current only** (§4.1): pack only when **`CurrentEducation`** is set on **`ApplicationItem`**. |
| **`Person`** | Required for the application line; if null, skip per-item supporting sections that need a person. **Family relationship** (§4.6) additionally requires **`Person.IsEmployee == false`**. |

**“Set” means:** reference assigned (non-null). **Empty `*Document` / `FileData` collections** on the **target** BO (e.g. a passport with no **`PassportDocument`** rows) still mean that branch was **eligible** for ZIP output but produces **no files** — skip silently / log, no failure (see §6). **`*Image` / `byte[]` rows are ignored by the v1 packer** (§4.8) — if only images exist and no **`FileData`**, the branch is effectively **empty** for ZIP purposes (optional **warn** so users upload PDFs/files via **`Document`** collections).

**Diploma scope “all educations” (§4.1 option A):** rows come from **`Person.Educations`**; the **`ApplicationItem.Person`** link must still be set. Do **not** pack educations for a null person or for a different person than the item’s **`Person`**.

## 2. Goals

1. When a user clicks **Generate PDF** (the same control used for **`Visa_Application_TM_QR_08`** / configured template), the resulting ZIP should optionally include, per selected **`ApplicationItem`**:
   - **Diploma / education** file copies from **`EducationDocument`** (`FileData`) per §4.1 / §4.8, and/or
   - **Passport** file copies from **`PassportDocument`** on **`CurrentPassport`** and **`PreviousPassport`**, with paths or filenames that clearly say **`Current`** vs **`Previous`** (§4.4, §5.1), and/or
   - **Visa** file copies from **`VisaDocument`** on **`CurrentVisa`** and **`PreviousVisa`**, likewise labelled **`Current`** vs **`Previous`**, and/or
   - **Medical record** file copies from **`MedicalRecordDocument`** on **`CurrentMedicalRecord`** under **`MedicalRecord/`** per item (§4.4, §5.1), and/or
   - **Address of residence** proofs from **`AddressOfResidenceDocument`** and **`LodgingDocument`** when applicable (§4.3) under **`AddressOfResidence/`** per item, and/or
   - **Work permit** files from **`WorkPermitDocument`** on the parent **`WorkPermit`** reached via **`CurrentWorkPermitItem`** and **`PreviousWorkPermitItem`** (**employee** **`Person`** only — §4.5), **deduplicated** across all items in the same ZIP when they share the same **`WorkPermit`** (§4.5, §5.1), and/or
   - **Invitation** files from **`InvitationDocument`** on the parent **`Invitation`** reached via **`CurrentInvitationItem`**, **deduplicated** by **`Invitation.ID`** (§4.7, §5.1), and/or
   - **Family relationship** file copies for lines where **`ApplicationItem.Person.IsEmployee`** is **false**: **`PersonDocument`** (`FileData`) only (§4.6, §4.8),
   with **no second primary workflow**.
2. Preserve **original fidelity** (no mandatory re-layout through XtraReports for scans).
3. Stay aligned with **Module** domain + **existing worker/controller** boundaries (see §5.4).
4. Support **clear filenames** and predictable folder layout for clerks and external organisations — including an unambiguous distinction between **current** and **previous** passport and visa attachments, **distinct `MedicalRecord/`** and **`AddressOfResidence/`** areas, **no duplicate `WorkPermit` or `Invitation` file trees** when the same parent **`WorkPermit`** or **`Invitation`** is shared by several item rows on the same application batch, and **as short as practical** folder/file names without dropping information needed to tell entries apart (§4.4).

## 3. Non-goals (initial phase)

- Replacing or duplicating the **XFA form PDF** pipeline (`PdfMappingHelper`, `IPdfFormFillerService`).
- A standalone **XAF report** whose primary job is to render diploma / passport / visa / medical / address / work permit / invitation scans (reports may be used later only as an **optional cover sheet** — see §8).
- Changing how **`CurrentEducation`** is chosen for forms unless product explicitly requires it.

## 4. Product decisions (lock before implementation)

### 4.1 Which education rows to include

| Option | Meaning |
|--------|--------|
| **A — All active educations** | Every non-deleted **`Education`** on **`ApplicationItem.Person`**, ordered (e.g. graduation year descending, then institution). **Requires** **`ApplicationItem.Person`** set. **Recommended default** for “all diplomas for this applicant.” |
| **B — Current only** | Only the education row referenced by **`ApplicationItem.CurrentEducation`** when that property is **set** on the item (the line’s chosen education for forms — typically aligned with **`Person.CurrentEducation`** after sync). If **`CurrentEducation`** is **null** on **`ApplicationItem`**, **do not** pack diploma files for that item under this scope (§1.3). |

**Action:** Confirm with stakeholders. The implementation should read this from a **single enum or bool** on the batch (or application type) so we can switch without rewriting the packer. **Attachments (v1):** **`EducationDocument`** (`FileData`) only — not **`EducationImage`** (§4.8).

### 4.2 Output shapes (user-visible)

Implement as **batch options** (stored on **`PdfGenerationBatch`** or passed when enqueueing — see §5.2):

| Flag / option | Behaviour |
|----------------|-----------|
| **Include diploma files** | When true, packer adds diploma outputs under `Diplomas/…` (§5.1). Default per product. |
| **Include passport copies** | When true, packer adds **`PassportDocument`** (`FileData`) **only** for **`ApplicationItem.CurrentPassport`** / **`PreviousPassport`** where each property is **set** (non-null) — **v1: no `PassportImage`** (§4.8). Unset slot → **no** files for that slot (§1.3). ZIP paths or file names **must** include **`Current`** or **`Previous`** (§4.4). |
| **Include visa copies** | When true, same for **`VisaDocument`** on **`CurrentVisa`** / **`PreviousVisa`** — each branch only if set on **`ApplicationItem`** (§1.3), **`Current`** / **`Previous`** labelling, **no `VisaImage`** (§4.8). |
| **Include medical record copies** | When true, packer adds **`MedicalRecordDocument`** only when **`ApplicationItem.CurrentMedicalRecord`** is set; then under `MedicalRecord/…` (§4.3, §5.1). If null on the item, skip entirely (§1.3). |
| **Include address of residence copies** | When true, packer adds **`AddressOfResidenceDocument`** / **`LodgingDocument`** only when **`ApplicationItem.CurrentAddressOfResidence`** is set (§4.3, §5.1). If null on the item, skip (§1.3). |
| **Include work permit copies** | When true, packer adds **`WorkPermitDocument`** from each distinct parent **`WorkPermit`** reached only through **set** **`CurrentWorkPermitItem`** or **`PreviousWorkPermitItem`** on **`ApplicationItem`** rows where **`Person.IsEmployee == true`** (§1.3, §4.5, §4.8), with **deduplication** by **`WorkPermit.ID`**. **No** work permit files for **family member** lines (`IsEmployee == false`), regardless of stale slot values. |
| **Include invitation copies** | When true, packer adds **`InvitationDocument`** from each distinct parent **`Invitation`** reached only through **set** **`CurrentInvitationItem`** on **`ApplicationItem`** rows in the batch (§1.3, §4.7, §4.8), with **deduplication** by **`Invitation.ID`**. |
| **Include family relationship copies** | When true, for each **`ApplicationItem`** whose **`Person`** has **`IsEmployee == false`**, pack all **`PersonDocument`** (**`Person.Documents`**, `FileData`) under **`FamilyRelationship/…`** (§4.6, §4.8, §5.1). For **`IsEmployee == true`**, skip this subtree for that item. |
| **Separate files** | One ZIP entry per source **`FileData`** / document row (extensions / sanitisation in implementation). |
| **Merged PDF (optional)** | Per §4.3 — e.g. merged diplomas only, or separate merged PDFs per category; at least one **separate-files** or **merged** path should exist when any include flag is on. |
| **Both** (where merged exists) | Separate files **and** merged PDF for that category. |

**Default recommendation:** **Separate files = on** for all included categories; **merged PDF = off** unless a recipient requires it. **Include passport / visa / medical / address / work permit / invitation / family relationship** default **on** with diplomas for a “full application package” is a reasonable v1 if stakeholders agree; otherwise default off for stricter minimal ZIPs.

### 4.3 Passport, visa, medical record, and address of residence (which object, which files)

**Prerequisite (§1.3):** Each row below applies **only** if the **`ApplicationItem`** navigation in the first column is **set** (non-null). Unset → that branch is **ineligible**; do **not** substitute from **`Person`** collections.

| Source on `ApplicationItem` | BO | Attachments to pack |
|------------------------------|-----|---------------------|
| **`CurrentPassport`** | `Passport` | All **`PassportDocument`** rows (`FileData`) on that passport — **v1: no `PassportImage`** (§4.8). ZIP layout / names **must** include **`Current`** (§4.4). |
| **`PreviousPassport`** | `Passport` | Same for the previous passport row (**`PassportDocument`** only). Names **must** include **`Previous`**. Skip entire branch if null. |
| **`CurrentVisa`** | `Visa` | All **`VisaDocument`** (`FileData`) — **v1: no `VisaImage`** (§4.8). Names **must** include **`Current`**. |
| **`PreviousVisa`** | `Visa` | Same for the previous visa row (**`VisaDocument`** only). Names **must** include **`Previous`**. Skip if null. |
| **`CurrentMedicalRecord`** | `MedicalRecord` | All **`MedicalRecordDocument`** (`FileData`) on that record only — **v1: no `MedicalRecordImage`** (§4.8). Use a **`MedicalRecord/`** root per item (§4.4, §5.1). **No** `PreviousMedicalRecord` on **`ApplicationItem`** — do not pull other **`Person.MedicalRecords`**. |
| **`CurrentAddressOfResidence`** | `AddressOfResidence` | (1) All **`AddressOfResidenceDocument`** on that address row (`FileData`). (2) If **`Type == Lodging`** (**`ResidenceType.Lodging`**) and **`Lodging`** is not null, also pack all **`LodgingDocument`** on **`Lodging`**. **v1: no `*Image` rows** (§4.8). Use **`AddressOfResidence/`** root per item; place lodging files in **`Lodging/`** under that item path (§4.4, §5.1). **No** `PreviousAddressOfResidence` on **`ApplicationItem`** — do not pull other **`Person.AddressesOfResidence`**. |
| **`CurrentInvitationItem`** | `InvitationItem` → **`Invitation`** (parent) | All **`InvitationDocument`** (`FileData`) on that **`Invitation`** only — **v1: no `InvitationImage`** (§4.8). ZIP layout uses a **shared** tree per distinct **`Invitation.ID`** (§4.7, §5.1). |

**Same entity twice:** If **`CurrentPassport`** and **`PreviousPassport`** reference the same **`Passport`** ID (data error), pack **once** under **`Current`** and do not duplicate under **`Previous`** (log warning). Same rule for **`CurrentVisa`** / **`PreviousVisa`**.

**Merged-PDF option (§4.2):** v1 can scope merged PDF to **diplomas only** (merge **`EducationDocument`** PDFs / single-page PDFs only — **no image-to-PDF conversion** in worker) or define separate merged files per category (`_AllDiplomas_merged.pdf`, `_PassportVisa_merged.pdf`, etc.) — pick one rule before coding. If a merged passport/visa PDF is ever added, split **`Current`** vs **`Previous`** into **separate merged files** or **clearly ordered sections** with bookmark/page labels — avoid a single undifferentiated stack.

### 4.4 Naming (including **Current** vs **Previous**, **MedicalRecord**, **AddressOfResidence**, and **brevity**)

- ZIP outer layout stays consistent with today: inner root folder + per-item form PDFs (see worker).
- **Requirement:** Any packed passport or visa file must be identifiable as **current** or **previous** without opening the file.
- **Brevity vs differentiation:** Generate **as short as possible** folder segments and file stems while **keeping** whatever users need to (a) match a ZIP entry to the **application line** (index / compact person slug), (b) see **category** (passport, visa, diploma, …), (c) tell **current** from **previous** where the product exposes both, (d) separate **collisions** (duplicate names in one folder — short numeric or OID suffix), and (e) distinguish **shared** **`WorkPermit`** / **`Invitation`** folders from per-line trees. **Do not** drop those signals to save characters; **do** drop redundancy (e.g. avoid repeating the full person slug in every leaf filename when the parent folder already encodes the line). Prefer **abbreviations only when still obvious in context** (e.g. `Cur` / `Prev` are **not** recommended unless stakeholders explicitly accept them — `Current` / `Previous` stay clearer for little extra length). For optional traceability tokens (**passport / visa / medical numbers**, address snippets, **`WorkPermitNumber`**, **`InvitationNumber`**), use **minimal length that still disambiguates** in normal batches (e.g. last few digits + sanitised short slug), not full raw values. Prefer a **shallower path** when the same information can live in **filename prefixes** instead of extra folders (see bullet below). Validate **full path length** on Windows (see §9); if close to limits, shorten category folder labels only after confirming they remain intuitive (e.g. `FamilyRelationship` → `FamilyRel` if product agrees).
- **Medical record:** Use a top-level folder per item, e.g. **`MedicalRecord/01_Last_First/…`**, so medical scans are clearly separated from **`Passport/`** and **`Visa/`**. Optional: include sanitised **`DocumentNumber`** from **`MedicalRecord`** in filenames for traceability.
- **Address of residence:** Use **`AddressOfResidence/01_Last_First/…`** for files tied directly to the **`AddressOfResidence`** row; use **`AddressOfResidence/01_Last_First/Lodging/…`** for files from the linked **`Lodging`** when packing lodging proofs. Optional: sanitised snippet of **`FullAddress`** in filenames (short) for traceability.
- **Recommended layout** (paths include literal segments `Current` and `Previous`):

  - `Passport/01_Last_First/Current/…` — scans for **`CurrentPassport`**
  - `Passport/01_Last_First/Previous/…` — scans for **`PreviousPassport`**
  - `Visa/01_Last_First/Current/…` — scans for **`CurrentVisa`**
  - `Visa/01_Last_First/Previous/…` — scans for **`PreviousVisa`**

  Alternative acceptable if path depth is a concern: single folder per item with **filename prefix** `Current_` / `Previous_` on every entry (e.g. `Passport/01_Last_First/Current_doc01.pdf`). Pick one scheme in code and stay consistent.

- Use the same **item index + person slug** as form PDFs (`01_FirstName_LastName`) for correlation; **shorten the slug** in implementation if needed (initials + last name, max length cap) **as long as** items in the batch remain uniquely identifiable (document the rule in code). Document the final scheme in code comments to avoid path length issues on Windows.

### 4.5 Work permit items and parent **`WorkPermit`** (shared documents, deduplication — **employees only**)

**Who qualifies:** **`ApplicationItem.Person`** must be **set** and **`Person.IsEmployee == true`**. Work permit packaging **does not run** for **family members** (`IsEmployee == false`). This matches **`ApplicationItem.ApplyCurrentFieldsFromSelectedPerson`**, which clears **`CurrentWorkPermitItem`** and **`PreviousWorkPermitItem`** when the person is not an employee; the worker must still **guard** on **`IsEmployee`** so mis-linked historical rows never emit work permit files for dependents.

**Properties on `ApplicationItem`:** **`CurrentWorkPermitItem`** (primary slot) and **`PreviousWorkPermitItem`** (second slot, same naming pattern as **`PreviousPassport`** / **`PreviousVisa`**). Not-mapped report helpers: **`PreviousWorkPermit_Number`**, **`PreviousWorkPermit_ExpirationDateText`**.

**Where files live:** **`WorkPermitItem`** → **`WorkPermit`** (`WorkPermitItem.WorkPermit`). Attachments are on the **parent** only: **`WorkPermit.Documents`** (`WorkPermitDocument` → **`FileData`**). Do **not** look for per-item collections on **`WorkPermitItem`**. **v1:** **`WorkPermit.Images`** / **`WorkPermitImage`** are **not** read (§4.8).

**Why dedupe:** One **`WorkPermit`** aggregates many **`WorkPermitItem`** rows (`WorkPermit.WorkPermitItems`). Several **`ApplicationItem`** lines on the **same** **`Application`** may point at **different** **`WorkPermitItem`** rows that still reference the **same** **`WorkPermit`**. Packing naïvely “once per application item” would **duplicate identical files** in the ZIP.

**Rule (within one `PdfGenerationBatch` / one ZIP):**

1. After resolving all **`ApplicationItem`** keys in the batch, consider **only** items with **`Person`** set and **`Person.IsEmployee == true`**. From those, collect every **set** (non-null) **`CurrentWorkPermitItem`** and **`PreviousWorkPermitItem`**, then each one’s **`WorkPermit`** (skip if null).
2. Build the set of **distinct** **`WorkPermit.ID`** values.
3. For each distinct **`WorkPermit`**, write **`WorkPermitDocument`** streams **exactly once** under a **shared** path such as **`WorkPermit/_Shared/WP_{sanitizedWorkPermitNumber}_{shortKey}/`** (see §5.1). **`shortKey`** should be the **shortest** stable disambiguator (e.g. compact GUID prefix) that avoids collisions across the batch (§4.4 brevity). Maintain a **`HashSet<Guid>`** (or equivalent) while emitting; if a **`WorkPermit.ID`** was already written, **skip** (optional debug log).
4. If **`CurrentWorkPermitItem`** and **`PreviousWorkPermitItem`** on the **same** **`ApplicationItem`** resolve to the **same** **`WorkPermit.ID`**, emit **once** (same dedupe set).
5. **Cross-application batches:** If a batch ever includes items from **more than one** **`Application`**, dedupe by **`WorkPermit.ID`** still applies globally inside that ZIP (same physical document should not appear twice). If two applications incorrectly shared one **`WorkPermit`** (data anomaly), one folder still avoids duplication; document if product prefers **application-scoped** subfolders — default is **ID-based global dedupe within the ZIP**.

**Optional v2:** A small manifest file listing which **`ApplicationItem`** indices / person names reference which **`WorkPermit.ID`** — only if clerks ask for traceability without opening the main app.

### 4.6 Family members — relationship evidence (not the `Relationship` lookup row)

**Trigger:** **`ApplicationItem.Person`** is **set** (non-null) and **`Person.IsEmployee == false`** (family member; see **`Person.cs`** — **`SponsoringEmployee`**, **`Relationship`**). If **`Person`** is null on the item, skip (§1.3).

**What to pack** (when **Include family relationship copies** is on — §4.2):

| Source on `Person` | BO | Attachments |
|--------------------|-----|-------------|
| **`Documents`** | **`PersonDocument`** | Each row: **`File`** (`FileData`), optional **`Description`**. **v1 only** — **`Images` / `FamilyMemberImage` / `byte[]`** are **not** packed (§4.8). |

**What *not* to treat as a file source:** **`Person.Relationship`** navigates to a **`Relationship`** lookup — labels only (**`LookupBase`**); there is nothing to stream from that entity for the ZIP.

**Optional metadata in ZIP:** Filenames or a small sidecar text line may include sanitised **`Relationship.Name`** (or localized caption) for clerk context — **optional**, not a substitute for packing **`PersonDocument`**.

**Employees:** When **`Person.IsEmployee == true`**, do **not** add the **`FamilyRelationship/`** folder for that application line via this flag (work permits and other item-linked objects remain governed by their own flags). If product later wants **`PersonDocument`** on employees in the same ZIP, that would be a **separate** batch flag and scope decision.

### 4.7 Invitation item and parent **`Invitation`** (shared documents, deduplication)

**Property on `ApplicationItem`:** **`CurrentInvitationItem`** only (no second slot). **`InvitationItem`** is shown when **`ApplicationType.ShowCurrentInvitationItem`** applies (see **`ApplicationItem`** appearances).

**Where files live:** **`InvitationItem`** → **`Invitation`** (`InvitationItem.Invitation`). Attachments are on the **parent** only: **`Invitation.Documents`** (`InvitationDocument` → **`FileData`**). Do **not** look for per-item collections on **`InvitationItem`** — same pattern as **`WorkPermitItem`** → **`WorkPermit`**. **v1:** **`Invitation.Images`** / **`InvitationImage`** are **not** read (§4.8).

**Why dedupe:** One **`Invitation`** aggregates many **`InvitationItem`** rows (`Invitation.InvitationItems`). Several **`ApplicationItem`** lines on the **same** **`Application`** may point at **different** **`InvitationItem`** rows that still reference the **same** **`Invitation`**. Packing once per application line would **duplicate identical files** in the ZIP.

**Rule (within one `PdfGenerationBatch` / one ZIP):**

1. After resolving all **`ApplicationItem`** keys in the batch, collect every **set** (non-null) **`CurrentInvitationItem`** on those items only, then each one’s **`Invitation`** (skip if null).
2. Build the set of **distinct** **`Invitation.ID`** values.
3. For each distinct **`Invitation`**, write **`InvitationDocument`** streams **exactly once** under a **shared** path such as **`Invitation/_Shared/INV_{sanitizedInvitationNumber}_{shortKey}/`** (see §5.1). **`shortKey`**: same brevity rule as **`WorkPermit`** (§4.4, §4.5). Maintain a **`HashSet<Guid>`** while emitting; if an **`Invitation.ID`** was already written, **skip** (optional debug log).
4. **Do not** pack invitation files from **`Person.InvitationItems`** or from **`Invitation`** rows that are **not** reached via **set** **`ApplicationItem.CurrentInvitationItem`** (§1.3).

**Optional v2:** Manifest mapping **`ApplicationItem`** → **`Invitation.ID`** — only if clerks request it.

### 4.8 v1 packer: **`FileData` / `*Document` only** (no **`ImageBase` / `byte[]` / `*Image`**)

**Scope:** The supporting-doc ZIP worker (**v1**) streams **only** attachment rows backed by **`DocumentBase`** (**`File`** → **`FileData`**) — e.g. **`PassportDocument`**, **`EducationDocument`**, **`PersonDocument`**, **`WorkPermitDocument`**, **`InvitationDocument`**, **`VisaDocument`**, **`MedicalRecordDocument`**, **`AddressOfResidenceDocument`**, **`LodgingDocument`**. It **does not** enumerate or load **`ImageBase`** / **`byte[]`** / **`*Image`** collections (**`PassportImage`**, **`EducationImage`**, **`FamilyMemberImage`**, **`WorkPermitImage`**, **`InvitationImage`**, …).

**Rationale:** Large **`byte[]`** / image columns inflate EF materialization and memory on the server. The **Blazor host running in Docker on the DigitalOcean droplet** should prefer **file-backed** reads (**`FileData`**) and smaller object graphs for batch ZIP work.

**Family (§4.6):** Under **`FamilyRelationship/`**, pack **`PersonDocument`** only — **no** **`Images/`** subtree from **`Person.Images`** in v1.

**UX:** Users who attach scans only as **images** will see an **empty** branch for ZIP until they add **`FileData`** on the **`Document`** side (upload PDF or other file on the relevant **`*Document`** row). Optional **warn** in UI or logs when a flagged branch has **`*Image`** rows but **no** **`*Document`** rows (§1.3).

**v2 (optional):** Server-side image→PDF conversion or packing **`byte[]`** could be revisited with a dedicated pipeline — **out of scope** for v1 droplet worker.

## 5. Technical design

### 5.1 ZIP layout (conceptual)

Existing structure (simplified):

```text
{PDF_Form}/                    # inner folder (worker already uses a stem like PDF_Form)
  01_FirstName_LastName.pdf   # filled application PDF per ApplicationItem
  ...
```

**Extended structure** (when supporting documents are included — any combination):

```text
{PDF_Form}/
  01_FirstName_LastName.pdf
  ...
  Diplomas/                              # if Include diplomas (v1: EducationDocument / FileData only — §4.8)
    01_FirstName_LastName/
      E01_{EducationShortLabel}_{GraduationYear}_doc01.pdf
      ...
      _AllDiplomas_merged.pdf              # optional; merge PDF inputs only — §4.3
  Passport/                              # if Include passport copies (PassportDocument only — §4.8)
    01_FirstName_LastName/
      Current/                             # CurrentPassport → PassportDocument
        doc01.pdf
      Previous/                            # PreviousPassport (omitted if null)
        doc01.pdf
        ...
  Visa/                                  # if Include visa copies (VisaDocument only)
    01_FirstName_LastName/
      Current/
        doc01.pdf
      Previous/
        ...
  MedicalRecord/                         # if Include medical record copies (MedicalRecordDocument only)
    01_FirstName_LastName/
      doc01.pdf                            # from MedicalRecordDocument.File
      ...
  AddressOfResidence/                    # if Include address of residence copies (*Document only)
    01_FirstName_LastName/
      doc01.pdf                            # AddressOfResidenceDocument
      Lodging/                             # only when Type == Lodging and Lodging set
        doc01.pdf                        # LodgingDocument
  WorkPermit/                            # if Include work permit copies (employee Person only; deduped by WorkPermit.ID; WorkPermitDocument only)
    _Shared/
      WP_{WorkPermitNumber}_{shortId}/    # one folder per distinct WorkPermit in the batch
        doc01.pdf                        # WorkPermitDocument
  Invitation/                          # if Include invitation copies (deduped by Invitation.ID; InvitationDocument only)
    _Shared/
      INV_{InvitationNumber}_{shortId}/   # one folder per distinct Invitation in the batch
        doc01.pdf                        # InvitationDocument
  FamilyRelationship/                  # if Include family relationship copies AND Person.IsEmployee == false (PersonDocument only — §4.8)
    01_FirstName_LastName/
      doc01.pdf                          # PersonDocument (Person.Documents); no Images/ subtree in v1
      ...
```

#### 5.1.1 Folder and file structure — grouping diagram

The ZIP is organized into **three logical groups** (see also §4.4–§4.7):

1. **Form PDFs** — one filled application PDF per **`ApplicationItem`** (same ordering / slug as today’s worker).
2. **Per-line supporting files** — each category has its own top-level folder; under it, **one subfolder per application line** (`01_FirstName_LastName` …) so clerks can match scans to a person. **Passport** and **Visa** add **`Current/`** vs **`Previous/`** under that line. **`FamilyRelationship/`** appears only for **family member** lines when that include flag is on (**`PersonDocument`** files only in v1 — §4.8).
3. **Batch-shared parent documents** — **`WorkPermit/`** and **`Invitation/`** use **`_Shared/`** plus **one folder per distinct parent entity in the batch** (not repeated under each line’s slug), because several lines can point at the same **`WorkPermit`** or **`Invitation`** (§4.5, §4.7). **Work permit** files in this group are **employee lines only**.

```mermaid
flowchart TB
  subgraph ZIP["ZIP inner root (worker stem, e.g. PDF_Form)"]
    direction TB

    subgraph G1["Group 1 — Application form PDFs (per ApplicationItem)"]
      F1["01_FirstName_LastName.pdf"]
      F2["02_OtherPerson.pdf"]
    end

    subgraph G2["Group 2 — Per-line supporting scans (item slug under each category)"]
      direction TB
      D["Diplomas / itemSlug / education files + optional merged PDF"]
      P["Passport / itemSlug / Current | Previous / scans"]
      V["Visa / itemSlug / Current | Previous / scans"]
      M["MedicalRecord / itemSlug / scans"]
      A["AddressOfResidence / itemSlug / (+ Lodging subfolder if applicable)"]
      R["FamilyRelationship / itemSlug / PersonDocument files (family members only)"]
    end

    subgraph G3["Group 3 — Batch-shared parent scans (dedupe; not under itemSlug)"]
      direction TB
      W["WorkPermit / _Shared / WP_number_shortId / files (employees only)"]
      I["Invitation / _Shared / INV_number_shortId / files"]
    end
  end
```

**Legend**

| Token / folder | Meaning |
|----------------|--------|
| **`itemSlug`** | Same per-line folder name as form PDFs (e.g. `01_FirstName_LastName`); correlates ZIP entries to one **`ApplicationItem`**. |
| **`Current` / `Previous`** | Under **`Passport/`** and **`Visa/`** only; omitted when the matching **`ApplicationItem`** link is unset (§1.3). |
| **`_Shared`** | **`WorkPermit`** and **`Invitation`** parent files are **not** duplicated under each **`itemSlug`**; one tree per distinct **`WorkPermit.ID`** / **`Invitation.ID`** in the batch. |
| **`WP_*` / `INV_*`** | Sanitised **`WorkPermitNumber`** / **`InvitationNumber`** plus short disambiguator per §4.4–§4.5 / §4.7. |

If **merged diplomas PDF** only (simplest v1), keep merged file under `Diplomas/…` as today’s sketch; passport/visa/medical/address/work permit/invitation/family relationship stay **separate files** unless product adds merged bundles later.

**Naming tokens:** Prefer **short** composed names (§4.4): education index / graduation / institution (abbreviate institution only if still recognisable); for passport/visa include **`Current`** or **`Previous`** in the path (or filename prefix); optionally embed **short** sanitised fragments of **`PassportNumber`** / **`VisaNumber`** / medical **`DocumentNumber`** / address / **`WorkPermitNumber`** / **`InvitationNumber`** / relationship label (**`Relationship`**) — **truncate** to the minimum needed to differentiate files in a typical batch; preserve **`FileData`** extension when available (original client filename may be long — sanitise and shorten per §4.4).

### 5.2 Extending `PdfGenerationBatch`

Today: `ItemKeyType`, `ItemKeysJson`, `ZipFile`, status counters.

**Planned additions** (minimal set — adjust names in implementation):

- `IncludeDiplomas` (`bool`).
- `DiplomaScope` (`enum`: `AllEducations` | `CurrentEducationOnly`) — when `IncludeDiplomas`.
- `IncludePassportCopies` (`bool`) — when true, pack **`CurrentPassport`** / **`PreviousPassport`** only where set on **`ApplicationItem`** (§1.3), with **Current** / **Previous** naming per §4.4.
- `IncludeVisaCopies` (`bool`) — when true, pack **`CurrentVisa`** / **`PreviousVisa`** only where set on **`ApplicationItem`** (§1.3), with **Current** / **Previous** naming per §4.4.
- `IncludeMedicalRecordCopies` (`bool`) — when true, pack **`CurrentMedicalRecord`** attachments only when that property is set on **`ApplicationItem`** (§1.3).
- `IncludeAddressOfResidenceCopies` (`bool`) — when true, pack **`CurrentAddressOfResidence`** (and linked **`Lodging`** when applicable) per §4.3 only when **`CurrentAddressOfResidence`** is set on **`ApplicationItem`** (§1.3).
- `IncludeWorkPermitCopies` (`bool`) — when true, pack parent **`WorkPermit`** attachments per §4.5 only through **set** item slots on **employee** lines (**`Person.IsEmployee == true`**, §1.3), with **`WorkPermit.ID`** deduplication across the batch.
- `IncludeInvitationCopies` (`bool`) — when true, pack parent **`Invitation`** attachments per §4.7 only when **`CurrentInvitationItem`** is set on **`ApplicationItem`** (§1.3), with **`Invitation.ID`** deduplication across the batch.
- `IncludeFamilyRelationshipCopies` (`bool`) — when true, for each item whose **`Person.IsEmployee`** is **false**, pack **`PersonDocument`** per §4.6 / §4.8 (omit **`FamilyRelationship/`** for employees).
- `DiplomaOutputMode` (`enum` flags or separate bools): `SeparateFiles`, `MergedPdf` (validation: when `IncludeDiplomas`, at least one mode applies; same pattern if merged PDF is later added for passport/visa/medical/address/work permit).

**Migration:** EF migration in **`Visa2026.Module`** + update **`Visa2026DbContext`** configuration if needed.

**Enqueue:** **`ApplicationItemPdfController`** — when the user runs **Generate PDF**, create the batch with **all** supporting-document flags set there (e.g. **parameter dialog** before commit, or **defaults** on `PdfGenerationBatch` for v1). This is the **only** enqueue path for this feature in the baseline design. Defaults from **`ApplicationType`** or global settings can be a **later** enhancement.

### 5.3 Worker logic (`PdfGenerationBatchWorkerService`)

Same service and **same ZIP archive** as today’s **Generate PDF** job: for each `ApplicationItem` key, write the filled **visa application** PDF entry first (unchanged), then append supporting-document entries per batch flags (`IncludeDiplomas`, `IncludePassportCopies`, `IncludeVisaCopies`, `IncludeMedicalRecordCopies`, `IncludeAddressOfResidenceCopies`, `IncludeWorkPermitCopies`, `IncludeInvitationCopies`, `IncludeFamilyRelationshipCopies`).

**Naming:** Every passport- and visa-related ZIP entry must sit under or be prefixed with **`Current`** vs **`Previous`** as specified in §4.4 — **never** mix both into one undistinguished folder. Apply §4.4 **brevity** rules to every path segment and file stem. **Medical record** files use **`MedicalRecord/{itemFolder}/`** only (no **Current**/**Previous** subfolders — only **`CurrentMedicalRecord`** exists on the item). **Address of residence** files use **`AddressOfResidence/{itemFolder}/`**, with **`Lodging/`** subfolder for lodging-sourced files when applicable (§4.4). **Work permit** parent files use **`WorkPermit/_Shared/…`** (§4.5) — **not** under each person’s item folder, so shared **`WorkPermit`** documents are not duplicated; **only** from **`ApplicationItem`** rows whose **`Person.IsEmployee == true`**. **Invitation** parent files use **`Invitation/_Shared/…`** (§4.7) — same dedupe rationale. **Family relationship** files use **`FamilyRelationship/{itemFolder}/…`** (**`PersonDocument`** only — §4.6, §4.8) when the person is a family member.

After generating each item’s **form PDF** entry, for that same item:

**A — Diplomas (unchanged from prior plan)**

1. Resolve **`ApplicationItem`** → **`Person`** (skip supporting docs if **`Person`** unset or item deleted; form PDF handling stays as today). For diplomas, apply **§4.1** + §1.3 (**current only** requires **`CurrentEducation`** set on the item; **all educations** requires **`Person`** set).
2. If **`IncludeDiplomas`**: load **`Education`** rows per **§4.1**; enumerate **`EducationDocument`** only (§4.8); write ZIP entries and optional merged PDF per **`DiplomaOutputMode`** (merged step: **PDF inputs only** — §4.3).

**B — Passport**

3. If **`IncludePassportCopies`**:
   - If **`ApplicationItem.CurrentPassport`** is **set**: enumerate **`PassportDocument`**; write under `Passport/{itemFolder}/Current/…` (or equivalent prefix scheme per §4.4). If unset, **no** `Current` passport files for that item (§1.3).
   - If **`ApplicationItem.PreviousPassport`** is **set**: enumerate **`PassportDocument`**; write under `Passport/{itemFolder}/Previous/…`. If unset, **no** `Previous` files — **do not** pick another passport from **`Person`** (§1.3). If **`CurrentPassport`** and **`PreviousPassport`** share the same **`Passport`** ID, pack **only** under **`Current`** and log (§4.3).

**C — Visa**

4. If **`IncludeVisaCopies`**:
   - If **`ApplicationItem.CurrentVisa`** is **set**: enumerate **`VisaDocument`**; write under `Visa/{itemFolder}/Current/…`. If unset, skip **`Current`** branch (§1.3).
   - If **`ApplicationItem.PreviousVisa`** is **set**: write under `Visa/{itemFolder}/Previous/…`. If unset, skip **`Previous`** branch (§1.3). Deduplicate if current and previous reference the same **`Visa`** ID (§4.3).

**D — Medical record**

5. If **`IncludeMedicalRecordCopies`** and **`ApplicationItem.CurrentMedicalRecord`** is **set** (§1.3): enumerate **`MedicalRecordDocument`**; write under `MedicalRecord/{itemFolder}/…` (§4.4, §5.1). If **`CurrentMedicalRecord`** is unset on the item, skip.

**E — Address of residence**

6. If **`IncludeAddressOfResidenceCopies`** and **`ApplicationItem.CurrentAddressOfResidence`** is **set** (§1.3):
   - Enumerate **`AddressOfResidenceDocument`**; write under `AddressOfResidence/{itemFolder}/…`.
   - If residence **`Type`** is **Lodging** and **`Lodging`** is not null: enumerate **`LodgingDocument`**; write under `AddressOfResidence/{itemFolder}/Lodging/…`.
   If **`CurrentAddressOfResidence`** is unset on the item, skip.

**F — Work permit (batch-level dedupe)**

7. If **`IncludeWorkPermitCopies`**: either **(a)** after the per-item loop, compute distinct **`WorkPermit`** IDs from **`ApplicationItem`** rows in **`batch.ItemKeysJson`** where **`Person`** is set and **`Person.IsEmployee == true`**, using only **set** **`CurrentWorkPermitItem`** / **`PreviousWorkPermitItem`** on those rows, and emit each parent’s **`WorkPermitDocument`** streams once under `WorkPermit/_Shared/…` (§4.5), **or (b)** during the per-item loop, for **employee** lines only, consult a **`HashSet<Guid>`** of already-packed **`WorkPermit.ID`** and only enqueue/write on first sighting. Both approaches satisfy dedupe; pick one for clarity in code.

**H — Invitation (batch-level dedupe)**

8. If **`IncludeInvitationCopies`**: same pattern as step 7 — distinct **`Invitation`** IDs from **set** **`CurrentInvitationItem`** on each **`ApplicationItem`** in the batch; emit **`InvitationDocument`** once per **`Invitation.ID`** under `Invitation/_Shared/…` (§4.7).

**G — Family relationship (per item, family members only)**

9. If **`IncludeFamilyRelationshipCopies`** and **`Person`** is not null and **`Person.IsEmployee`** is **false**: enumerate **`PersonDocument`** on **`Person.Documents`**; write under `FamilyRelationship/{itemFolder}/…` (§4.6, §5.1). If **`Person.IsEmployee`** is **true**, skip this step for that item.

**Shared:**

10. **Merged PDF** (when enabled for diplomas): use **`IPdfFormFillerService.MergePdfs`** for **PDF** **`FileData`** sources only (§4.3). **v1:** no worker step that converts **`byte[]`** images to PDF pages.
11. **Progress / errors:** increment `ProcessedItems` as today; missing optional sections (no passport files, no visa, no diplomas, no medical files, no address files, no work permit files, no invitation files, no family relationship files) — skip + log (recommend).

**Performance:** Stream each **`FileData`** to the ZIP **one document at a time** where practical to avoid holding the whole dossier in memory (especially on the droplet Docker host).

### 5.4 New or moved code (where logic lives)

| Concern | Location |
|---------|----------|
| Enumerating **Education** / **Passport** / **Visa** / **MedicalRecord** / **AddressOfResidence** / **Lodging** / **WorkPermit** / **Invitation** / **`PersonDocument`** (when **`Person.IsEmployee == false`**) **`*Document`** / **`FileData`** rows, sanitising paths, shared “write stream to ZIP entry” helpers, **`WorkPermit`** and **`Invitation`** dedupe registries | **`Visa2026.Module`** — e.g. `Services/ApplicationSupportingDocumentsHelper.cs` (name TBD) or split by concern; consumed by host |
| ZIP writing orchestration | **`PdfGenerationBatchWorkerService`** (Blazor.Server) — **calls** Module helper with `IObjectSpace` / keys |
| Domain types | Already in Module: `Education` + **`EducationDocument`** (v1 ZIP: document side only; **`EducationImage`** exists but not packed — §4.8); `Passport` + **`PassportDocument`**; `Visa` + **`VisaDocument`**; `MedicalRecord` + **`MedicalRecordDocument`**; `AddressOfResidence` + **`AddressOfResidenceDocument`**; `Lodging` + **`LodgingDocument`**; `WorkPermit` + **`WorkPermitDocument`**; `WorkPermitItem`; `Invitation` + **`InvitationDocument`**; `InvitationItem`; `Application` + `ApplicationItems`; `ApplicationItem` (`CurrentWorkPermitItem`, `PreviousWorkPermitItem`, `CurrentInvitationItem`, `CurrentPassport`, `PreviousPassport`, `CurrentVisa`, `PreviousVisa`, `CurrentMedicalRecord`, `CurrentAddressOfResidence`, `CurrentEducation` / `Person`); `Person` (`IsEmployee`, `Images` → `FamilyMemberImage` **not v1 ZIP**, `Documents` → `PersonDocument`, `Relationship` lookup — no blobs); `Relationship` (`LookupBase`) |

Keep the Blazor worker as a **thin orchestrator** that already has configuration, PDF services, and non-secured `ObjectSpace`.

### 5.5 Security

Batch processing already uses **`INonSecuredObjectSpaceFactory`**. **`FileData`** / **`*Document`** streams must be read only for **objects reachable from the batch’s `ApplicationItem` keys** (`CurrentPassport`, `PreviousPassport`, `CurrentVisa`, `PreviousVisa`, `CurrentMedicalRecord`, `CurrentAddressOfResidence` / nested **`Lodging`**, `CurrentWorkPermitItem` / `PreviousWorkPermitItem` / nested **`WorkPermit`** **only on employee lines** (`Person.IsEmployee == true`), `CurrentInvitationItem` / nested **`Invitation`**, `Person` / `Education` per scope, and when the family flag is on: **`Person.Documents`** for that same **`Person`** only when **`IsEmployee == false`**) — same trust model as current PDF fill (worker runs server-side). **v1:** do **not** traverse **`Person.Images`** or other **`*Image`** navigations for ZIP (§4.8). No change to client-side security model unless new UI surfaces are added.

### 5.6 UI

- **`ApplicationItemPdfController`**: extend **only** the existing **Generate PDF** handler. Dialog or v1 defaults must cover **diplomas + passport + visa + medical record + address of residence + work permit + invitation + family relationship** include flags (and diploma scope / output mode).
- **`PdfGenerationBatch`** Detail/List: show new columns so users see what they queued.

### 5.7 API / download

Existing **`PdfBatchesController`** exposes download when batch completes. No change required if the same **`ZipFile`** grows; verify **timeout / max size** for very large attachment sets (many education rows + passport + visa + medical + address/lodging + work permit + invitation **`FileData`** rows + family **`PersonDocument`** — document in ops if limits exist).

## 6. Edge cases

| Case | Handling |
|------|----------|
| Person has educations but **no** **`EducationDocument`** rows | No diploma file entries for that section; form PDFs still generated. |
| **Diploma scope “current only”, `ApplicationItem.CurrentEducation` null** | No diploma files for that item even if **`Person.CurrentEducation`** exists — item link must be set (§1.3, §4.1). |
| **`CurrentPassport`** is null | Skip only `…/Current/` under passport for that item; still pack **`PreviousPassport`** if set. If both null, omit passport subtree entirely. |
| **`PreviousPassport`** is null on **`ApplicationItem`** | Skip only `…/Previous/`; still pack **`Current`** if **`CurrentPassport`** is set (§1.3). |
| **`PreviousPassport`** unset but **`Person.Passports`** has multiple rows | **No** `Previous/` subtree — do **not** infer a previous passport from **`Person`** (§1.3). |
| **`CurrentVisa`** is null on **`ApplicationItem`** | Skip only `…/Current/` under visa; still pack **`PreviousVisa`** if set. |
| **`PreviousVisa`** is null on **`ApplicationItem`** | Skip only `…/Previous/`; still pack **`CurrentVisa`** if set. |
| **`PreviousVisa`** unset but other visas exist on **`Person`** | **No** `Previous/` subtree from **`Person`** — only **`ApplicationItem.PreviousVisa`** when set (§1.3). |
| **`CurrentVisa`** and **`PreviousVisa`** both null | Omit visa subtree (common when **`ApplicationType`** does not use visa on the item). |
| **`CurrentPassport`** / **`PreviousPassport`** both null | Omit passport subtree. |
| **Same `Passport` ID** on **`CurrentPassport`** and **`PreviousPassport`** | Pack once under **`Current`** only; log warning (§4.3). |
| **Same `Visa` ID** on **`CurrentVisa`** and **`PreviousVisa`** | Pack once under **`Current`** only; log warning (§4.3). |
| **`CurrentMedicalRecord`** is null | Omit **`MedicalRecord/`** subtree for that item (common when **`ApplicationType.ShowCurrentMedicalRecord`** is false or data not filled). |
| **`CurrentAddressOfResidence`** is null | Omit **`AddressOfResidence/`** subtree for that item (common when **`ApplicationType.ShowCurrentAddressOfResidence`** is false or data not filled). |
| Address row exists but **no** **`AddressOfResidenceDocument`** / **`LodgingDocument`** rows to pack | Omit content under **`AddressOfResidence/`**; no failure. |
| **`Type == Lodging`** but **`Lodging`** is null | Pack only direct **`AddressOfResidenceDocument`** rows (if any); **`Lodging/`** subfolder empty or omitted. |
| Medical record exists but **no** **`MedicalRecordDocument`** rows | Skip **`MedicalRecord/`** content; no failure. |
| **`Person.IsEmployee == false`** (family member) with **set** **`CurrentWorkPermitItem`** / **`PreviousWorkPermitItem`** (data anomaly) | **No** work permit ZIP content from that line — ignore slots unless **`IsEmployee == true`** (§4.5); optional debug log. |
| **Same `WorkPermit` ID** referenced from multiple **`ApplicationItem`** / **`WorkPermitItem`** lines | **One** shared folder in **`WorkPermit/_Shared/…`**; do not re-emit files (§4.5). **Only** contributions from **employee** items count toward the dedupe set. |
| **`CurrentWorkPermitItem`** and **`PreviousWorkPermitItem`** both null | No work permit files for that item; dedupe set may still be filled from other rows. |
| **`WorkPermit`** has no **`WorkPermitDocument`** rows | Skip that **`WorkPermit.ID`**; no failure. |
| Passport/visa exist but **no** **`PassportDocument`** / **`VisaDocument`** rows | Skip that section; no failure. |
| **Very long** institution or file names | Truncate/slug per **§4.4 brevity** (keep distinguishing tail + extension); reuse sanitisation patterns from `BuildZipName` / `SanitizeFileNamePart`. |
| Duplicate filenames in same folder | Append `_2`, `_3` or include image/document **OID** suffix. |
| Corrupt or unreadable `FileData` | Log, skip file, continue; mark batch **Failed** only if **critical** failure (policy: prefer **Completed** with partial output vs **Failed** — align with PDF batch philosophy). |
| **Same person** on two application items (if ever allowed) | Each item gets its own subtrees (`Diplomas/`, `Passport/`, `Visa/`, `MedicalRecord/`, `AddressOfResidence/`, `FamilyRelationship/`) under that item’s index to avoid collisions; **`WorkPermit`** and **`Invitation`** files remain in **`WorkPermit/_Shared/…`** and **`Invitation/_Shared/…`** deduped by parent ID, not duplicated per item. |
| **`IncludeFamilyRelationshipCopies`** on, **`Person.IsEmployee == true`** | No **`FamilyRelationship/`** subtree for that line (employee). |
| **`IncludeFamilyRelationshipCopies`** on, **`Person.IsEmployee == false`**, empty **`Documents`** (no **`PersonDocument`**) | Omit **`FamilyRelationship/`** content (or empty folder); no failure. |
| Family member line, **`Relationship`** set but **no** **`PersonDocument`** rows | Same as above — optional filename hint from relationship name only if implemented (§4.6). |
| **`CurrentInvitationItem`** unset on **`ApplicationItem`** but **`Person.InvitationItems`** has rows | **No** invitation files from **`Person`** — only via **set** **`CurrentInvitationItem`** (§1.3, §4.7). |
| **Same `Invitation` ID** from multiple **`ApplicationItem`** / **`InvitationItem`** lines | **One** shared folder in **`Invitation/_Shared/…`**; do not re-emit files (§4.7). |
| **`Invitation`** has no **`InvitationDocument`** rows | Skip that **`Invitation.ID`**; no failure. |

## 7. Dependencies

- **Spire.PDF** (already used) for merge of **PDF** **`FileData`** inputs (§4.3, §5.3).
- **System.IO.Compression** (already used for ZIP).

## 8. Optional later: XAF cover sheet

If a branded **cover page** is required, add a **small** XtraReport that prints metadata only, export to PDF, **prepend** to a merged PDF (e.g. diplomas bundle). **Not** part of the first milestone unless requested.

## 9. Verification checklist (after implementation)

- [ ] **§1.3:** No supporting branch reads from **`Person`** collections when the matching **`ApplicationItem`** navigation is unset (e.g. **`PreviousPassport`** null → no **`Previous/`** files, even if **`Person.Passports`** has extra rows).
- [ ] **§4.8 / §1.3:** Worker never loads **`*Image`** / **`byte[]`** for ZIP; only **`FileData`** / **`*Document`** rows.
- [ ] **Naming / §4.4:** Paths and stems are **as short as practical** without losing line, category, and **Current**/**Previous** clarity; no redundant long tokens; optional business-key fragments are truncated.
- [ ] **Passport** enabled: **`Current`** and **`Previous`** subtrees (or prefixes) are both present when the corresponding `ApplicationItem` properties are non-null; never ambiguous.
- [ ] **Passport** enabled: attachments come **only** from **`PassportDocument`** on **`CurrentPassport`** / **`PreviousPassport`** on the item — not from other **`Person.Passports`**.
- [ ] **Visa** enabled: **`Current`** / **`Previous`** layout; sources only **`VisaDocument`** on **`CurrentVisa`** / **`PreviousVisa`** on the item.
- [ ] **Duplicate current/previous:** same passport (or visa) ID on current and previous → **single** packed copy under **`Current`**, warning logged.
- [ ] **Medical record** enabled: `MedicalRecord/…` populated **only** from **`MedicalRecordDocument`** on **`CurrentMedicalRecord`** on the item — not from other **`Person.MedicalRecords`**.
- [ ] **Address of residence** enabled: `AddressOfResidence/…` from **`AddressOfResidenceDocument`** on **`CurrentAddressOfResidence`** only; **`Lodging/`** subfolder populated when type is lodging and **`Lodging`** is set; not from other **`Person.AddressesOfResidence`**.
- [ ] **Work permit** enabled: parent **`WorkPermit`** **`WorkPermitDocument`** files appear **once** per distinct **`WorkPermit.ID`** when several **employee** items share the same work permit; **`CurrentWorkPermitItem`** / **`PreviousWorkPermitItem`** considered **only** when **`Person.IsEmployee == true`**; **no** work permit files for family member lines.
- [ ] **Invitation** enabled: parent **`Invitation`** **`InvitationDocument`** files appear **once** per distinct **`Invitation.ID`** when several items share the same invitation; only **`CurrentInvitationItem`** on the item (§1.3, §4.7).
- [ ] **Family relationship** enabled: for **`Person.IsEmployee == false`**, `FamilyRelationship/…` contains only that person’s **`PersonDocument`** rows; **no** subtree for employees; **no** files streamed from **`Relationship`** lookup; **no** **`FamilyMemberImage`** in v1 ZIP.
- [ ] **Generate PDF** with diplomas enabled: form PDFs + `Diplomas/` (and merged PDF when enabled, **PDF sources only**).
- [ ] Batch with **all educations** includes every non-deleted education’s **`EducationDocument`** attachments when that scope is selected.
- [ ] **Current only** respects **`ApplicationItem.CurrentEducation`** when set; if null on the item, **no** diplomas for that line (§1.3, §4.1).
- [ ] ZIP opens on Windows; paths under **260 chars** in typical cases (or use safe short names per §4.4).
- [ ] Merged PDF (if implemented) opens in Adobe and a common non-Adobe viewer.
- [ ] Large file / many items: worker completes without OOM (smoke test).

## 10. References (code)

- `Visa2026.Module/BusinessObjects/ApplicationItem.cs` — `CurrentWorkPermitItem`, `PreviousWorkPermitItem`, `CurrentInvitationItem`, `CurrentPassport`, `PreviousPassport`, `CurrentVisa`, `PreviousVisa`, `CurrentMedicalRecord`, `CurrentAddressOfResidence`, `CurrentEducation`, `Person`, `Application`; **`ApplyCurrentFieldsFromSelectedPerson`** clears **`CurrentWorkPermitItem`** / **`PreviousWorkPermitItem`** when **`Person.IsEmployee`** is false
- `Visa2026.Module/BusinessObjects/Person.cs` — **`IsEmployee`** (work permit packaging only when **true**); `SponsoringEmployee`, `Relationship`, `Images` (`FamilyMemberImage`), `Documents` (`PersonDocument`)
- `Visa2026.Module/BusinessObjects/FamilyMemberImage.cs`, `PersonDocument.cs`
- `Visa2026.Module/BusinessObjects/LookupBusinessObjects.cs` — `Relationship` (lookup; no attachments)
- `Visa2026.Module/BusinessObjects/Education.cs`, `EducationImage.cs`, `EducationDocument.cs`
- `Visa2026.Module/BusinessObjects/Passport.cs`, `PassportImage.cs`, `PassportDocument.cs`
- `Visa2026.Module/BusinessObjects/Visa.cs`, `VisaImage.cs`, `VisaDocument.cs`
- `Visa2026.Module/BusinessObjects/MedicalRecord.cs`, `MedicalRecordImage.cs`, `MedicalRecordDocument.cs`
- `Visa2026.Module/BusinessObjects/AddressOfResidence.cs`, `AddressOfResidenceImage.cs`, `AddressOfResidenceDocument.cs`
- `Visa2026.Module/BusinessObjects/Lodging.cs`, `LodgingImage.cs`, `LodgingDocument.cs`
- `Visa2026.Module/BusinessObjects/WorkPermit.cs`, `WorkPermitImage.cs`, `WorkPermitDocument.cs`
- `Visa2026.Module/BusinessObjects/WorkPermitItem.cs` — `WorkPermit` parent navigation
- `Visa2026.Module/BusinessObjects/Invitation.cs`, `InvitationImage.cs`, `InvitationDocument.cs`
- `Visa2026.Module/BusinessObjects/InvitationItem.cs` — `Invitation` parent navigation
- `Visa2026.Module/BusinessObjects/PdfGenerationBatch.cs`
- `Visa2026.Module/Controllers/ApplicationItemPdfController.cs` — **Generate PDF** → enqueue batch (`GenerateApplicationPdfBatch`)
- `Visa2026.Blazor.Server/Services/PdfGenerationBatchWorkerService.cs` — ZIP build; template from `PdfSettings:TemplatePath` or embedded `Visa2026.Module.Resources.Visa_Application_TM_QR_08.pdf`
- `Visa2026.Module/Resources/Visa_Application_TM_QR_08.pdf` — default visa application form asset
- `Visa2026.Module/Services/PdfFormFillerService.cs` — `MergePdfs` (page import)

---

**Document status:** planning — **not** yet implemented. Update this file when §4 decisions are locked and when behaviour diverges during implementation. **Scope:** item-scoped supporting files are **eligible** only when the corresponding **`ApplicationItem`** property is **set** (§1.3); then diplomas (**`EducationDocument`** per §4.1 / §4.8), passport and visa files (**`PassportDocument`**, **`VisaDocument`** on **`CurrentPassport`** / **`PreviousPassport`**, **`CurrentVisa`** / **`PreviousVisa`**) with mandatory **Current** vs **Previous** naming, **`MedicalRecordDocument`** on **`CurrentMedicalRecord`** under **`MedicalRecord/`**, **`AddressOfResidenceDocument`** / **`LodgingDocument`** on **`CurrentAddressOfResidence`** under **`AddressOfResidence/`**, parent **`WorkPermitDocument`** via **set** **`CurrentWorkPermitItem`** / **`PreviousWorkPermitItem`** on **`Person.IsEmployee == true`** lines only (§4.5), parent **`InvitationDocument`** via **set** **`CurrentInvitationItem`**, each with **parent `ID`** deduplication within the ZIP (**§4.5**, **§4.7**), and for **`Person.IsEmployee == false`** **`PersonDocument`** under **`FamilyRelationship/`**; **ZIP paths and filenames** stay **as short as practical** while preserving §4.4 differentiation — same **Generate PDF** batch. **v1:** **no `*Image` / `byte[]`** in worker (§4.8).
