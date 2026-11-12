# Migration portal field discovery (Registration RPA)

**Rule:** no Save/Submit/search against live portal without officer consent.  
**Scope now:** Registration applications only (`/panel/applications/`).  
**Deferred:** Business trip forms via `/panel/notifications/habars/`.

Related: [playwright-rpa-registration-field-map.md](./playwright-rpa-registration-field-map.md) (Visa2026→portal map) · [playwright-rpa-portal-css-selectors.md](./playwright-rpa-portal-css-selectors.md) (selector lookup) · [playwright-rpa-visa2026-integration-README.md](./playwright-rpa-visa2026-integration-README.md) · `Visa2026.PortalFiling.Worker`.

## Officer process (Registration) — confirmed 2026-11-10

1. Open `https://rgstr.migration.gov.tm/panel/applications/` (list of prior Registration-family apps).
2. Click **+ Add application** → creates draft at `/panel/applications/edit/{id}/` (status **Unfinished**). Examples seen: `#11270`, `#11272`.
3. On edit page click **+ Add citizen** → `/panel/persons/search/{id}/`.
4. Enter **Passport number** (no spaces) + **Date of birth** (`dd.mm.YYYY`) → **Search**.  
   Warning: *You can only make 5 consecutive incorrect searches!*
5. Portal returns read-only **Personal information** + **Visa information**; officer may **Upload photo**, then **+ Add**.
6. Back on edit page: citizen row appears; fill **Application details** (+ attachments); **Save** (`Ýatda sakla`) / **Delete** (`Pozmak`).

## Screens / URLs

| Step | URL pattern |
|------|-------------|
| List | `/panel/applications/` |
| Draft edit | `/panel/applications/edit/{applicationId}/` |
| Add citizen search | `/panel/persons/search/{applicationId}/` |

## Application details fields (empty draft + filled sample)

| Field (EN) | Control | Sample filled (TK UI / #11272) |
|------------|---------|--------------------------------|
| Application type | dropdown | HASABA ALMAK (Registration) |
| Type of location | dropdown | UMUMY ÝAŞAÝYŞ JAÝY |
| Place of stay | dropdown | ÇALYK ENERJİ UÝJ GIZA |
| Registration date from–to | 2× date | 30.10.2026 – 20.04.2027 |
| Address district | dropdown | TÜRKMENBAŞY ETRABY |
| Address (street) | text | long lodging address text |
| Building / Block / Apartment | 3× text | often empty for dorm |
| Workplace | text | |
| Letter number | text | |
| Letter date | date | |
| Note | textarea | |
| Attachments | file input | pdf/jpg/png, max 10MB |

## Add citizen

| Step | Fields / actions |
|------|------------------|
| Search | Passport number, Date of birth, Search |
| Result (read-only) | Surname, Name, DOB, Citizenship, Passport type, Gender, Passport No; Visa type/No/entries/validity |
| Actions | Upload photo, **+ Add** (or back) |
| After add | Table: Passport, Name, Surname, Gender, Citizenship, DOB; can remove (x) |

## Citizen result fields (from portal registry — not typed by officer)

Surname, Name, DOB, Citizenship, Passport type, Gender, Passport No, Visa type, Visa No, Visa entries, Visa start/end, Photo.

## Consent / safety

- Drafts `#11270` / `#11272` were created during discovery — officer-owned.
- RPA must not Save or fire citizen Search without explicit job confirmation.
- Incorrect passport/DOB burns the 5-search limit.

## Still needed for RPA implementation

See chat / section below — selectors + dropdown catalogs + Visa2026 map + photo/attachment rules.

## CSS / Playwright selectors (Registration)

**Canonical lookup table:** [playwright-rpa-portal-css-selectors.md](./playwright-rpa-portal-css-selectors.md).

**Dropdown catalogs:** [_portal_capture/](./_portal_capture/) (places-of-stay.json, ddress-districts.json, …).

### List page — Add application

Captured from DevTools on `/panel/applications/` (EN UI).

| Item | Value |
|------|--------|
| Tag | `a` |
| href | `/panel/applications/create/` |
| Classes | `btn btn-success` |
| Approx size | ~161 × 44 px |

**Preferred selector (stable):**
```css
a[href="/panel/applications/create/"]
```

**Playwright:**
```csharp
page.Locator("a[href='/panel/applications/create/']")
// or
page.GetByRole(AriaRole.Link, new() { Name = "Add application" })
```

**Scoped alternatives:**
```css
.card-header a.btn.btn-success
a.btn.btn-success
```
Prefer `href` over bare `a.btn-success` (other green buttons may exist).

**Note:** Create entry is `/panel/applications/create/` (then portal assigns `edit/{id}/`).

### Edit page — Add citizen

Captured from DevTools on `/panel/applications/edit/11276/` (EN UI, Unfinished).

| Item | Value |
|------|--------|
| Tag | `a` |
| href | `/panel/persons/search/11276/` (id matches application) |
| Classes | `btn btn-outline-secondary btn-border` |
| Visible text | `Add citizen` (with + icon) |

**Preferred selector (stable pattern):**
```css
a[href^="/panel/persons/search/"]
```

**Playwright:**
```csharp
page.Locator("a[href^='/panel/persons/search/']")
// or
page.GetByRole(AriaRole.Link, new() { Name = "Add citizen" })
```

**Exact for this draft (id-specific):**
```css
a[href="/panel/persons/search/11276/"]
```

**Class-scoped (weaker if other outline buttons exist):**
```css
a.btn.btn-outline-secondary.btn-border
.card-body a.btn-outline-secondary
```

Prefer `href^="/panel/persons/search/"` or accessible name over classes alone.

### Citizen search — Passport number & Date of birth

Captured from DevTools on `/panel/persons/search/11276/` (EN UI). Form: `method="POST"`, hidden `csrfmiddlewaretoken`, hidden `application` = application id.

#### Passport number

| Item | Value |
|------|--------|
| Tag | `input type="text"` |
| name | `document_number` |
| class | `form-control` |
| placeholder | `Passport number (Without whitespaces)` |
| required | yes |
| oninput | strips spaces + `toUpperCase()` |

**Preferred selector:**
```css
input[name="document_number"]
```

**Playwright:**
```csharp
page.Locator("input[name='document_number']")
// fill without spaces; portal uppercases on input
```

#### Date of birth

| Item | Value |
|------|--------|
| Tag | `input type="text"` |
| name | `birth_date` |
| class | `form-control date-field` |
| placeholder | `dd.mm.YYYY` |
| required | yes |

**Preferred selector:**
```css
input[name="birth_date"]
```

**Alternatives:**
```css
input.date-field
input[name="birth_date"].date-field
```

**Playwright:**
```csharp
page.Locator("input[name='birth_date']")
// value format: dd.MM.yyyy
```

### Citizen search — Search button

Captured from DevTools on `/panel/persons/search/11276/` (EN UI).

| Item | Value |
|------|--------|
| Tag | `button type="submit"` |
| Classes | `btn btn-success` |
| onclick | `this.form.submit(); disableBtn(this);` |
| Form | `method="POST"` · `onsubmit="disableBtn(this)"` · hidden `application` |

**Preferred selector:**
```css
form[method="POST"] button.btn-success[type="submit"]
```

**Playwright:**
```csharp
page.Locator("form[method='POST'] button.btn-success[type='submit']")
page.GetByRole(AriaRole.Button, new() { Name = "Search" })
```

Consent: Search burns the 5-incorrect-search limit — job confirmation required.

### Citizen result — Upload photo & Add

Captured from DevTools on `/panel/persons/search/11276/` after a successful search (EN UI).

#### Upload photo

| Item | Value |
|------|--------|
| Visible control | `<button id="uploadBtn" class="btn btn-light w-100">` |
| File input (actual upload) | `<input name="image" type="file" id="fileInput" class="d-none" accept="image/*" required>` |
| Preview | `#preview` / `.person-picture` |

**Preferred selectors:**
```css
button#uploadBtn
input#fileInput
```

**Playwright:** prefer setting files on the hidden input (more reliable than clicking the button):
```csharp
await page.Locator("input#fileInput").SetInputFilesAsync(photoPath);
// optional UI click:
await page.Locator("#uploadBtn").ClickAsync();
```

**Rule from portal:** citizen photo is **required** before Add (see Add button `onclick` alert).

#### + Add (submit citizen to application)

| Item | Value |
|------|--------|
| Tag | `button type="submit"` |
| Classes | `btn btn-primary` |
| Form action | `/panel/persons/create/{applicationId}/` (e.g. `/panel/persons/create/11276/`) |
| onclick | `reportValidity()` then submit; else `alert('Citizen photo is required!')` |

**Preferred selectors:**
```css
form[action*="/panel/persons/create/"] button[type="submit"]
button.btn.btn-primary[type="submit"]
```

**Playwright:**
```csharp
page.Locator("form[action*='/panel/persons/create/'] button[type='submit']")
// or
page.GetByRole(AriaRole.Button, new() { Name = "Add" })
```

Form also posts hidden fields (from registry): `document_number`, `fname`, `lname`, `birth_date`, `visa_type`, CSRF, etc. RPA normally only supplies photo + clicks Add after search; identity comes from the portal.

### Application edit page — full field map

Source: HTML dump of `/panel/applications/edit/11276/` (EN UI). Form `#date-form`, `method="POST"`, `enctype="multipart/form-data"`. Includes CSRF (do not store token values).

#### Navigation / chrome

| Control | Selector |
|---------|----------|
| Applications nav | `a.nav-link[href="/panel/applications/"]` |
| My messages (business trip later) | `a.nav-link[href="/panel/notifications/habars/"]` |
| Language | `select[name="language"]` (`tk` / `ru` / `en`) |

#### Related citizens

| Control | Selector |
|---------|----------|
| Add citizen | `a[href^="/panel/persons/search/"]` |
| Delete citizen | `form[action^="/panel/persons/delete/"] button` |
| Citizen rows | `table.table tbody tr` |

#### Application details — inputs (preferred `#id_*`)

| Label | name | id | Notes |
|-------|------|-----|--------|
| Application type | `type` | `#id_type` | required; unlocks other fields on change |
| Type of location | `body_type` | `#id_body_type` | required after type chosen |
| Place of stay | `body` | `#id_body` | Select2; filtered by `option[data-type]` |
| Registration start | `start_date` | `#id_start_date` | `.date-field`, `dd.mm.YYYY` |
| Registration end | `end_date` | `#id_end_date` | `.date-field` |
| Address district | `city` | `#id_city` | optgroups by welayat |
| Address (street) | `address` | `#id_address` | placeholder Köçe... |
| Building | `house` | `#id_house` | |
| Block | `block` | `#id_block` | |
| Apartment | `apartment` | `#id_apartment` | |
| Workplace | `work_place` | `#id_work_place` | wrapper `#id_work_place_wrapper` |
| Current district | `current_city` | `#id_current_city` | wrapper `#id_current_city_wrapper`; business trip only |
| Letter number | `letter_number` | `#id_letter_number` | |
| Letter date | `letter_date` | `#id_letter_date` | `.date-field` |
| Note | `notes` | `#id_notes` | textarea |
| Attachments | `images` | `#attachments` | `accept="image/*"` multiple; jpg/png; 10MB |

**Playwright fill pattern:** `page.Locator("#id_type")`, etc.

#### Footer actions

| Control | Selector |
|---------|----------|
| Delete | `button[name="action"][value="delete"]` (`.btn-danger`) |
| Save | `button[name="action"][value="save"]` (`.btn-success`) |
| Cancel application | `form[action*="/cancel/"] button[value="warning"]` (`.btn-warning`) |

#### Application type option values (`#id_type`)

| value | Label |
|-------|--------|
| `PA3` | PASSPORT REPLACEMENT |
| `HA4` | REGISTRATION |
| `HA5` | REGISTRATION EXTENSION |
| `SA6` | CHANGE OF ADDRESS |
| `WE8` | MOVING TO ANOTHER REGION |
| `IS9` | CHANGE OF WORKPLACE |
| `IS100` | BUSINESS TRIP |

Pilot Registration RPA: select **`HA4`**.

#### Type of location (`#id_body_type`)

| value | Label |
|-------|--------|
| `8` | Other |
| `9` | Residence / Organization Premises |
| `1` | Hotel |
| `2` | Dormitory |
| `3` | Hospital |
| `4` | Visa and Registration Department |
| `5` | Private House |
| `7` | Private House (Registered Address) |
| `6` | Parking Lot |

Place of stay `#id_body` options use `data-type` matching these values. Changing `#id_body_type` filters `#id_body` options.

#### JS behaviour (important for RPA order)

1. On load, most detail fields are **disabled** until `#id_type` has a value.
2. Selecting type enables location/dates/city/address/letter; if type=`IS100` shows current city and hides workplace; else reverse.
3. `#id_body_type` change: for types `9,1,2,3,4,6` building/block/apt not required; for `5,7,8` they are required.
4. `#id_body` change calls internal API `http://10.0.40.40/applications/body-data/{bodyId}/` and auto-fills city, street, house, block, apartment (may only work on portal LAN).
5. Date fields auto-insert dots while typing.
6. Attachments: client check max 10MB; alert if larger.

#### Attachment requirement text

`#reqs` is filled from selected type’s `data-reqs` (e.g. `1. Hat` for Registration types).
