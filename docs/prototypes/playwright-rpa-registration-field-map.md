# Visa2026 → migration portal Registration field map

**Status:** draft (doc only — **no production schema/code changes until mapping is agreed**)  
**Portal type:** `#id_type` = **`HA4`** (REGISTRATION)  
**Scope:** one unfinished portal application + one or more citizens  
**Safety:** no Search / Save on live portal without officer job consent  
**Portal UI language:** Türkmençe (tk) — worker sets language before fill

Related:

- [playwright-rpa-portal-css-selectors.md](./playwright-rpa-portal-css-selectors.md)
- [playwright-rpa-portal-field-discovery.md](./playwright-rpa-portal-field-discovery.md)
- [_portal_capture/](./_portal_capture/) (dropdown catalogs)
- [playwright-rpa-visa2026-integration-README.md](./playwright-rpa-visa2026-integration-README.md)
- Worker DTO sketch: `Visa2026.PortalFiling.Worker/FilingJobModels.cs`
- Process diagrams: [playwright-rpa-registration-process.mmd](./playwright-rpa-registration-process.mmd) · [playwright-rpa-registration-sequence.mmd](./playwright-rpa-registration-sequence.mmd)

---

## 0. Architecture rule (locked) — separate portal RPA data

Portal filing (RPA) must **not** overwrite or dual-use production Visa2026 fields that already drive the app.

| Kind | Rule |
|------|------|
| **Read for fill** | OK to *read* existing Visa2026 data as sources (e.g. `Visa.StartDate`, `Passport.PassportNumber`, `Person.Photo`, `AddressOfResidence.FullAddress`). |
| **Portal-only / portal-returned / portal codes** | Store in **dedicated** properties (or a dedicated filing side-object). Prefer names like `Portal*` / `MigrationPortal*`. |
| **Production numbering / dates / enums** | Do **not** dual-use `ResidenceType`, `Position`, etc. as portal-code stores. **Exception (Q2 revised):** after portal create, write portal ID → `ApplicationProfileInstance.FullApplicationNumber` and portal Creation date → `ApplicationDate` (plus mirror on `MigrationPortalFiling`). |
| **Ship rule** | Agree this map first; any new properties are **additive** and nullable/optional so current production behaviour stays unchanged. |

### Preferred storage shape (proposal — not implemented)

**A. Case filing side-object** (**locked**), e.g. `MigrationPortalFiling` linked to `ApplicationProfileInstance`:

| Proposed property | Purpose |
|-------------------|---------|
| `PortalApplicationId` | Portal list **ID** (e.g. `11270` / `#11270`) — **auto-created** when **+ Add application** is clicked (`/panel/applications/create/` → `edit/{id}/`) |
| `PortalCreatedOn` | Portal **Creation date** |
| `PortalStatus` | Last known portal status text (Scheduled / Pending / …) optional |
| `PortalLetterNumber` / `PortalLetterDate` | Values used on `#id_letter_number` / `#id_letter_date` if distinct from id/date |
| `LastJobStatus` / `LastError` | RPA job mirror |

**B. Additive fields on existing BOs** (only where officers edit portal-specific text/codes):

| BO | Proposed additive field(s) | Do **not** reuse |
|----|----------------------------|------------------|
| `EmployeePositionHistory` | e.g. `PortalWorkplace` (`string`) for `#id_work_place` | `Position`, `ActualPosition`, `Department` |
| `AddressOfResidence` (and/or `Lodging`/`Hotel`/…) | e.g. `PortalBodyTypeCode`, `PortalBodyId`, `PortalCityId`, optional `PortalHouse` / `PortalBlock` / `PortalApartment` | `Type`, `City`, `FullAddress` as the portal code store |
| `Lodging` (optional later) | e.g. `PortalBodyId` sticky match to `#id_body` | guessing from `FullAddress` alone in production logic |

Exact property names TBD at implementation; this doc locks the **separation principle**.

---

## Job payload (conceptual)

Queued from **`ApplicationProfileInstance`** (Registration family / `CfgShowRegistrations`) + selected **`People`** M2M.

- Links: **`ApplicationProfileInstancePersonResolvedLink`**
- **Do not use** removed **`ApplicationItem`**
- Job DTO remains portal-shaped (`FilingJobPayload`); hydration reads Visa2026 + `Portal*` fields

| Slice | Source for RPA fill | Where portal-specific value lives |
|-------|---------------------|-----------------------------------|
| Citizen passport / DOB | `Passport.PassportNumber`, `Person.DateOfBirth` | (read-only for search) |
| Citizen photo | `Person.Photo` | (read-only for upload) |
| Registration period | Linked **`Visa.StartDate`** / **`Visa.ExpirationDate`** | (read-only; no new period fields required for v1) |
| Letter id / date on portal | **Auto-created by portal** on **+ Add application** (list ID + Creation date; edit URL `/panel/applications/edit/{id}/`) | Write-back → **`PortalApplicationId`** + **`PortalCreatedOn`** on filing **and** `FullApplicationNumber` + `ApplicationDate` on the case |
| Workplace | **New** `EmployeePositionHistory.PortalWorkplace` (planned) | Separate from visa-report position fields |
| Stay type / place / district | Read `AddressOfResidence` / `InvitationAddress` + catalogs | Portal option **codes** in `PortalBodyTypeCode` / `PortalBodyId` / `PortalCityId` (planned) |
| Street | `FullAddress` / site name (read) | House/block/apt → `PortalHouse`/`PortalBlock`/`PortalApartment` if needed |
| Attachments | Document copies / ``FileData`` for **Required person-related data** on the Application Profile, for each **roster** person on the instance | **Q5b locked**; portal accepts jpg/png only (conversion still open) |
| Note | Optional job note | Filing side-object or job row |

---

## 1. Add citizen (search → photo → Add)

| Portal control | Selector | Visa2026 source | Rules |
|----------------|----------|-----------------|-------|
| Passport number | `input[name="document_number"]` | Resolved / current `Passport.PassportNumber` | Trim; no spaces; uppercase |
| Date of birth | `input[name="birth_date"]` | `Person.DateOfBirth` | `dd.MM.yyyy` |
| Search | `form[method="POST"] button.btn-success[type="submit"]` | — | Consent (5-miss limit) |
| Photo | `input#fileInput` | `Person.Photo` | Temp image file; required before Add |
| Add | `form[action*="/panel/persons/create/"] button[type="submit"]` | — | After search hit + photo |

Portal registry fields (name, gender, …) are **not** typed by RPA.

---

## 2. Application details — Registration `HA4`

Fill order: `#id_type` first, then location, dates, address, workplace, letter fields as applicable.

| Portal control | Selector | Source / direction | Notes |
|----------------|----------|--------------------|-------|
| Application type | `#id_type` | Fixed **`HA4`** | |
| Type of location | `#id_body_type` | Prefer **`PortalBodyTypeCode`** if set; else translate from `AddressOfResidence.Type` via **Q6 table** | **Locked (Q6)** |
| Place of stay | `#id_body` | Prefer **`PortalBodyId`**; else fail-closed catalog match | No fuzzy guess |
| Registration start / end | `#id_start_date` / `#id_end_date` | Linked **`Visa.StartDate`** / **`Visa.ExpirationDate`** | **Locked (Q1)** |
| Address district | `#id_city` | Prefer **`PortalCityId`**; else translate `City` | Fail-closed |
| Street | `#id_address` | `FullAddress` / hotel name / private-house text | May auto-fill from portal body LAN API |
| Building / Block / Apt | `#id_house` / `#id_block` / `#id_apartment` | **`PortalHouse` / `PortalBlock` / `PortalApartment`** on address/filing (private house) | **Locked (Q4c=A)** — officers fill; not parsed from `FullAddress` |
| Workplace | `#id_work_place` | **`EmployeePositionHistory.PortalWorkplace`** (planned additive) | **Locked direction (Q3)** — property does not exist yet |
| Letter number / date | `#id_letter_number` / `#id_letter_date` | After create: copy portal list **ID** + **Creation date** into these inputs (same as officers today); persist to **`PortalApplicationId`** / **`PortalCreatedOn`** **and** set case **`FullApplicationNumber`** / **`ApplicationDate`** | **Locked (Q2 revised)** |
| Note | `#id_notes` | Optional | |
| Attachments | `#attachments` | Document copies from profile **Required person-related data** rows for roster people on the instance (e.g. Passport, Visa, Address of residence, Education, Position, … as configured) | **Q5+Q5b locked**; jpg/png conversion still open |
| Save | `button[name="action"][value="save"]` | Officer ConfirmSave only | |

### Letter number flow (clarified)

```text
Visa2026 Queue job
  → Worker creates/opens portal application
  → Portal assigns ID (#11270) + Creation date (list columns)
  → Worker stores PortalApplicationId + PortalCreatedOn on filing side-object
  → Worker sets ApplicationProfileInstance.FullApplicationNumber = portal ID
  → Worker sets ApplicationProfileInstance.ApplicationDate = portal Creation date
  → If edit form letter fields must be filled, use those same portal values
  → Visa2026 FullApplicationNumber / ApplicationDate = portal ID / Creation date (Q2 revised)
```

---

## 3. Dropdown translation

Still required before automation. Catalogs: [_portal_capture/](./_portal_capture/).

### 3a. `ResidenceType` → `#id_body_type` — **locked (Q6)**

Default translation (portal catalog `_portal_capture/location-types.json`). Prefer **`PortalBodyTypeCode`** when set (allows `7`, `9`, `4`, `6`, etc.); else use this table from `AddressOfResidence.Type`:

| Visa2026 `ResidenceType` | Portal `#id_body_type` | Label |
|--------------------------|------------------------|-------|
| `Lodging` | `2` | Dormitory |
| `Hotel` | `1` | Hotel |
| `Hospital` | `3` | Hospital |
| `Other` | `8` | Other |
| `PrivateHouse` | `5` | Private House |

**Do not** change `ResidenceType` enum to match portal. Edge portal types (`7` Private House Registered Address, `9` Residence/Org Premises, `4` Visa and Registration Department, `6` Parking Lot) are selected only via explicit **`PortalBodyTypeCode`**, not by guessing from `ResidenceType`.

### 3b–3c. Place of stay / city

Match catalogs by explicit `PortalBodyId` / `PortalCityId` first; label match only as assisted mapping tool for officers, not silent RPA guess.

---

## 4. Multi-citizen

Fill citizens (search → photo → Add) for each selected person; application details once per portal application.

---

## 5. Queue validation (before Ready)

1. Passport number + DOB + `Person.Photo`  
2. Linked visa with start + end dates  
3. Stay resolvable (address + portal codes or agreed translation)  
4. `PortalWorkplace` present on current position history (once property exists)  
5. Attachments: document copies for Required person-related data kinds on the profile, for roster people (missing copies may gap-flag; jpg/png rules TBD)
6. All selected people share the same AddressOfResidence stay (else refuse)  

Letter id/date: **not** required before queue (portal generates them).

---

## 6. Non-goals

- Unrelated renumbering outside portal RPA write-back
- Business trip / other portal types in this map  
- Implementing schema until map is approved  
- Fuzzy place-of-stay guessing  

---

## 7. Decision log

| ID | Decision | Status |
|----|----------|--------|
| A0 | Portal RPA data in **separate** properties / filing object; do not break production dual-use | **Locked** |
| Q1 | Registration period = linked **Visa** start / end | **Locked** |
| Q2 | Portal list ID + Creation date = letter id/date on portal **and** Visa2026 `FullApplicationNumber` + `ApplicationDate`; also mirror on MigrationPortalFiling Portal* | **Locked (revised)** |
| Q2b | Filing storage = dedicated **MigrationPortalFiling** side-object on case | **Locked (A)** |
| Q3 | Workplace from new **`EmployeePositionHistory.PortalWorkplace`** (additive later) | **Locked** |
| Q3b | `PortalWorkplace` content = short workplace/site name as typed on portal today | **Locked (B)** |
| Q4 | Address source = **`AddressOfResidence`** (+ site FKs); portal codes / house-block-apt as Portal\* on filing/address | **Locked principle** |
| Q4b | Stay/address source for Registration RPA = person **`AddressOfResidence`** (current/linked), not case `InvitationAddress` | **Locked (B)** |
| Q4c | Private house in RPA v1 = supported; officers fill `PortalHouse` / `PortalBlock` / `PortalApartment` | **Locked (A)** |
| Q4d | Granularity = one portal application per Visa2026 case; all selected people as citizens; stay/details shared | **Locked (A)** |
| Q4e | Same-address cases by process; RPA refuses queue if selected AddressOfResidence stays differ | **Locked** |
| Q5 | Attachments source = document copies / FileData on person-related BOs linked via the instance | **Locked** |
| Q5b | **Which kinds** = whatever is marked **Required person-related data** on the Application Profile for that instance, for each **roster** (linked) person — not a hard-coded global list | **Locked** |
| Q6 | ResidenceType → `#id_body_type` default table (Lodging=2, Hotel=1, Hospital=3, Other=8, PrivateHouse=5); override via PortalBodyTypeCode | **Locked** |
| Q7 | Portal UI language for worker = Türkmençe (tk) | **Locked (B)** |

7. ~~UI language~~ — **Locked B:** Türkmençe (`select[name="language"]=tk`).

---

## 8. Remaining clarification questions

1. ~~Letter fields on edit form~~ — **Locked A:** officers manually copy list ID + Creation date into `#id_letter_number` / `#id_letter_date`; RPA must do the same.  
2. ~~Filing storage~~ — **Locked A:** dedicated **`MigrationPortalFiling`** side-object linked to the case.  
3. ~~Workplace text~~ — **Locked B:** short workplace/site name as usually typed on the portal.
4. ~~Stay source~~ — **Locked B:** person **`AddressOfResidence`** (current/linked).
6. ~~Portal app granularity~~ — **Locked A:** one portal application per Visa2026 case; selected people as citizens; shared stay/details.
6b. ~~Shared stay when people differ~~ — **Locked:** cases are same-address by process; RPA refuses queue if stays differ.
7. ~~UI language~~ — **Locked B:** Türkmençe (`select[name="language"]=tk`).
5. ~~Private house v1~~ — **Locked A:** supported; officers fill PortalHouse / PortalBlock / PortalApartment.
5b. ~~Attachments source (Q5)~~ — **Locked:** document copies / FileData on person-related BOs via the instance.
8. ~~Attachments kinds (Q5b)~~ — **Locked:** kinds = Application Profile **Required person-related data** for that instance × roster people. Portal still accepts **jpg/png only**, max 10MB each (conversion handling open).

## Portal login (worker) — noted for Step 7

Portal auth = **cellphone** + **temporary SMS code** (~10 days) as password — not a fixed portal password.

- **Preferred:** Playwright uses a **persistent browser profile** so cookies/session usually skip login.
- **Fallback:** worker-local config `PortalLogin` = cellphone + current SMS code (filling PC only; not Visa2026 DB).
- If neither works → job Failed until officer updates code or completes one login in that profile.
