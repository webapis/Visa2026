# Registration RPA process — agreed wording (step by step)

Working alignment of flowchart language vs officer understanding. Source diagrams: `playwright-rpa-registration-process.mmd`, `playwright-rpa-registration-sequence.mmd`.

**Status: Steps 1–14 locked** (process wording agreed). Field-map process wording complete for Steps 1–14; Q5b and Q6 locked. Open tech note: portal attachment jpg/png conversion.

| Step | My wording (diagram) | Agreed meaning |
|------|----------------------|----------------|
| 1 | Officer opens a Registration case (`ApplicationProfileInstance` in Registration family) | User creates or opens an Application Profile instance related to registration |
| 2 | Portal filing RPA enabled? No = hide queue / stop; Yes = continue | Feature/permission gate for migration-portal queue |
| 3 | (was: select people) → All people **linked** to the Registration ApplicationProfileInstance are included in the portal filing; no separate choose step | Linked = included |
| 4 | Queue validation before Ready; fail = refuse + gaps; OK = continue | Pre-flight checks on linked people |
| 5 | Officer clicks **Queue for migration portal** on ApplicationProfileInstance DetailView → validation → create/update MigrationPortalFiling + job Ready | Explicit separate manual action on DetailView; not on create/link |
| 6 | PortalFiling Worker polls / claims Ready job | Attended worker on filling PC picks up Ready filing; no officer click for pickup |
| 7 | Ensure portal login (SMS): prefer persistent browser session; else worker-local PortalLogin (cellphone + SMS code ~10 days); fail → Failed | Auth is cellphone + temporary SMS code; secrets stay on worker PC, not Visa2026 DB |
| 8 | Set language tk · click **+ Add application** | Worker sets Türkmençe, then clicks green **+ Add application** (`/panel/applications/create/`) |
| 9 | Portal auto-creates ID + Creation date → store on MigrationPortalFiling **and** set case FullApplicationNumber + ApplicationDate | Values from portal create; reused as letter number/date; Visa2026 case number/date = portal ID/date |
| 10 | Add citizens loop (each linked Person): passport + DOB search → upload Person.Photo → Add citizen | Repeat until all linked people are on the portal application |
| 11 | Fill application details once (shared for the case) | After all citizens: type HA4; stay from AddressOfResidence + portal codes; dates from linked Visa; workplace from PortalWorkplace; letter = stored portal ID + Creation date; attachments = document copies for Required person-related data on the profile × roster people |
| 12 | Screenshot draft → job status DraftOnScreen | Worker does **not** click portal Save yet; takes screenshot, sets filing/job to DraftOnScreen for officer review |
| 13 | Officer reviews draft (portal and/or Visa2026) | Reject/fix → Failed or Ready again; ConfirmSave → job Confirmed |
| 14 | Worker clicks Save on portal → MigrationPortalFiling Saved · done | Only after ConfirmSave; case FullApplicationNumber + ApplicationDate = portal ID + Creation date |

## Step 7 detail (locked)

Portal login is **not** username/password. Real flow:

1. User enters **cellphone number** on `rgstr.migration.gov.tm`.
2. Migration service sends a **temporary SMS code** (valid about **10 days**); that code is entered as the password.
3. After login, the browser normally **keeps the session** (cookies / profile).

| Priority | Path |
|----------|------|
| 1 (preferred) | Reuse **persistent browser profile** — skip login if session still valid |
| 2 (fallback) | Worker-local **PortalLogin** config (cellphone + current SMS code) if login page appears |
| Fail | Neither → job Failed until code refreshed or one manual login in that profile |

Cellphone + SMS code stay on the **worker machine** only — not on `MigrationPortalFiling` / OData.

## Step 9 detail (locked)

**How portal ID and Creation date appear:**

1. On Applications list (`/panel/applications/`), worker clicks **+ Add application** (`a[href="/panel/applications/create/"]`).
2. The **portal** auto-creates a new draft — assigns **ID** (e.g. `#11327`) and **Creation date** (`dd.mm.yyyy`). Officer/worker does **not** type these.
3. Browser lands on `/panel/applications/edit/{id}/` (header **Application #{id}**, status **Unfinished**). Application type is still empty until filled later (HA4 / Registration).
4. Worker captures **PortalApplicationId** (URL/header) and **PortalCreatedOn** (list Creation date / UI) → stores on **MigrationPortalFiling**.
5. Worker sets **`ApplicationProfileInstance.FullApplicationNumber`** = portal ID and **`ApplicationDate`** = portal Creation date (**Q2 revised**).
6. Later, worker types the same values into portal **Letter number** / **Letter date**.