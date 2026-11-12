# Migration portal — CSS / Playwright selectors

**Portal:** `https://rgstr.migration.gov.tm`  
**Scope:** Registration flow (`/panel/applications/`). Business trip `/habars/` deferred.  
**Safety:** Do not Save / Search / Submit on live portal without officer consent.  
**Source process notes:** [playwright-rpa-portal-field-discovery.md](./playwright-rpa-portal-field-discovery.md) · **Field map:** [playwright-rpa-registration-field-map.md](./playwright-rpa-registration-field-map.md)  
**Captured:** 2026-11-10 (EN UI unless noted). Prefer `id` / `name` / `href` over classes or visible text.

---

## URL patterns

| Step | Pattern |
|------|---------|
| Applications list | `/panel/applications/` |
| Create draft | `/panel/applications/create/` → redirects to edit |
| Edit draft | `/panel/applications/edit/{applicationId}/` |
| Citizen search | `/panel/persons/search/{applicationId}/` |
| Citizen create (Add) | `/panel/persons/create/{applicationId}/` |
| Citizen delete | `/panel/persons/delete/{applicationId}/` |
| Cancel application | `/panel/applications/{applicationId}/cancel/` |

---

## 1. Applications list

| Control | Preferred CSS | Playwright |
|---------|---------------|------------|
| Add application | `a[href="/panel/applications/create/"]` | `page.Locator("a[href='/panel/applications/create/']")` |

Fallbacks: `.card-header a.btn.btn-success`, `a.btn.btn-success` (weaker).

---

## 2. Application edit — chrome & citizens

| Control | Preferred CSS | Playwright |
|---------|---------------|------------|
| Applications nav | `a.nav-link[href="/panel/applications/"]` | |
| My messages (later) | `a.nav-link[href="/panel/notifications/habars/"]` | |
| Language | `select[name="language"]` | values: `tk`, `ru`, `en` |
| Add citizen | `a[href^="/panel/persons/search/"]` | `page.Locator("a[href^='/panel/persons/search/']")` |
| Delete citizen | `form[action^="/panel/persons/delete/"] button` | |
| Citizen rows | `table.table tbody tr` | |

---

## 3. Application edit — details form

Form: `#date-form` · `method="POST"` · `enctype="multipart/form-data"`  
(CSRF present as `input[name="csrfmiddlewaretoken"]` — do not store token values.)

| Label | name | Preferred CSS |
|-------|------|---------------|
| Application type | `type` | `#id_type` |
| Type of location | `body_type` | `#id_body_type` |
| Place of stay | `body` | `#id_body` (Select2) |
| Registration start | `start_date` | `#id_start_date` |
| Registration end | `end_date` | `#id_end_date` |
| Address district | `city` | `#id_city` |
| Address (street) | `address` | `#id_address` |
| Building | `house` | `#id_house` |
| Block | `block` | `#id_block` |
| Apartment | `apartment` | `#id_apartment` |
| Workplace | `work_place` | `#id_work_place` |
| Workplace wrapper | — | `#id_work_place_wrapper` |
| Current district | `current_city` | `#id_current_city` |
| Current district wrapper | — | `#id_current_city_wrapper` |
| Letter number | `letter_number` | `#id_letter_number` |
| Letter date | `letter_date` | `#id_letter_date` |
| Note | `notes` | `#id_notes` |
| Attachments | `images` | `#attachments` |
| Attachment reqs text | — | `#reqs` |

**Playwright fill:** `page.Locator("#id_type")`, etc.

**Date fields:** also match `.date-field` · format `dd.mm.YYYY`.

### Footer actions

| Control | Preferred CSS |
|---------|---------------|
| Save | `button[name="action"][value="save"]` |
| Delete | `button[name="action"][value="delete"]` |
| Cancel application | `form[action*="/cancel/"] button[value="warning"]` |

---

## 4. Citizen search page

| Control | Preferred CSS | Notes |
|---------|---------------|-------|
| Passport number | `input[name="document_number"]` | no spaces; portal uppercases |
| Date of birth | `input[name="birth_date"]` | `dd.mm.YYYY`; also `.date-field` |
| Search | `form[method="POST"] button.btn-success[type="submit"]` | DevTools 2026-11-10 |

Form: `method="POST"` · `onsubmit="disableBtn(this)"` · hidden `application` · CSRF (do not store).

### Search button

```html
<button type="submit" class="btn btn-success" onclick="this.form.submit(); disableBtn(this);">Search</button>
```

**Preferred CSS:** `form[method="POST"] button.btn-success[type="submit"]`

**Playwright:**
```csharp
page.Locator("form[method='POST'] button.btn-success[type='submit']")
page.GetByRole(AriaRole.Button, new() { Name = "Search" })
```

Fallbacks: `button.btn-success[type="submit"]`, `.card-body form button[type="submit"]`  
Consent: Search counts toward the 5-incorrect-search limit.

---

## 5. Citizen result — photo & Add

| Control | Preferred CSS | Notes |
|---------|---------------|-------|
| Upload photo button | `button#uploadBtn` | UI only |
| Photo file input | `input#fileInput` | `name="image"`; set files here |
| Photo preview | `#preview` / `.person-picture` | |
| Add (submit) | `form[action*="/panel/persons/create/"] button[type="submit"]` | photo required |

**Playwright photo:**
```csharp
await page.Locator("input#fileInput").SetInputFilesAsync(photoPath);
```

**Playwright Add:**
```csharp
page.Locator("form[action*='/panel/persons/create/'] button[type='submit']")
```

---

## 6. Catalog values (for Select)

### `#id_type` — Application type

| value | Label |
|-------|--------|
| `PA3` | PASSPORT REPLACEMENT |
| `HA4` | REGISTRATION |
| `HA5` | REGISTRATION EXTENSION |
| `SA6` | CHANGE OF ADDRESS |
| `WE8` | MOVING TO ANOTHER REGION |
| `IS9` | CHANGE OF WORKPLACE |
| `IS100` | BUSINESS TRIP |

**Registration pilot:** `HA4`.

### `#id_body_type` — Type of location

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

`#id_body` options carry `data-type` matching these values; filter place-of-stay after setting body type.

---

## 7. RPA fill order (edit page)

1. Set `#id_type` first — most detail fields start **disabled**.
2. Then `#id_body_type` → `#id_body` (Select2; filter by `data-type`).
3. Dates `#id_start_date` / `#id_end_date`, then `#id_city` / `#id_address` / house fields.
4. If type `IS100`: show `#id_current_city`, hide workplace; else fill `#id_work_place`.
5. Letter `#id_letter_number` / `#id_letter_date`, optional `#id_notes`, `#attachments`.
6. **Do not click Save** without officer consent.

### Body / address rules

- Body types `9,1,2,3,4,6`: building/block/apt often not required.
- Types `5,7,8`: building/block/apt required.
- Changing `#id_body` may call LAN API `http://10.0.40.40/applications/body-data/{bodyId}/` and auto-fill city/street/house (LAN-only).

### Attachments

- `#attachments` · jpg/png · max **10MB** per file · `#reqs` shows type `data-reqs` (e.g. `1. Hat`).

---

## Dropdown catalogs (data files)

Synced from **live** Chrome DevTools Console capture on edit/11276 (2026-11-10). Counts: 7 types, 9 location types, 1507 places, 99 districts.

Folder: [`_portal_capture/`](./_portal_capture/)

| File | Select |
|------|--------|
| `application-types.json` | `#id_type` (7) |
| `location-types.json` | `#id_body_type` (9) |
| `places-of-stay.json` | `#id_body` (1507, with `dataType`) |
| `address-districts.json` | `#id_city` / `#id_current_city` (99, with welayat `group`) |
| `places-of-stay-calik-subset.md` | 19 Çalık-related places |
| `edit-page-dropdown-catalog.json` | combined |

Registration pilot type value: **`HA4`**. Place-of-stay filter: set `#id_body_type` then keep `#id_body option[data-type=…]`.

## Still missing

| Item | Status |
|------|--------|
| Search button on citizen search page | Done — see section 4 |
| Full `#id_body` / `#id_city` catalogs as data files | Huge; extract Çalık subset when known |
| After-Save URL / success selectors | Not captured |
| Business trip `/habars/` | Deferred |

---

## Quick copy — preferred locator strings

```text
a[href="/panel/applications/create/"]
a[href^="/panel/persons/search/"]
input[name="document_number"]
input[name="birth_date"]
form[method="POST"] button.btn-success[type="submit"]
input#fileInput
button#uploadBtn
form[action*="/panel/persons/create/"] button[type="submit"]
#id_type
#id_body_type
#id_body
#id_start_date
#id_end_date
#id_city
#id_address
#id_house
#id_block
#id_apartment
#id_work_place
#id_current_city
#id_letter_number
#id_letter_date
#id_notes
#attachments
button[name="action"][value="save"]
button[name="action"][value="delete"]
form[action*="/cancel/"] button[value="warning"]
```