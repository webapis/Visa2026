
### 2026-09-21 — Contract group + CCUR (Zähmet şertnamasy)

- **Symptom**: Yellow `1.667.00 USD` could only map **CSAL**. Contract dates sat under leftover Visa.
- **Fix**: Catalog **CCUR** → `Salary_CurrencyCode`. Officer group **Contract** = CSAL, CCUR, CSDT, CEDT. Packs unchanged (salary / visa).
- **Officer**: Stop F5, rebuild. Placeholder Manual / Review Add → **Contract**.
- **Cross-skill**: visa2026-template-scan

### 2026-09-19 — Excel {{#ds.rows}} does not belong on the ACPOS/ACFNM footer

- **Symptom**: Yellow-marks Excel Generate inserted loop open/close between signatory **ACPOS** and **ACFNM**.
- **Fix**: Signatory tokens are not a roster loop row. Skip `{{/ds.rows}}` when that row already has mapped `{{` cells (close is optional).
- **Officer**: Stop F5, rebuild, Generate. Footer = position + name only.
- **Cross-skill**: visa2026-template-scan

### 2026-09-19 — Placeholder Manual uses the same officer groups as Review Add

- **Symptom**: Placeholder Manual listed every RelatedBo enum and showed joined **EGIY**/**VNAT**. Review Add already hid those and used Application / Visa subgroups.
- **Fix**: `GetGroupedEntries` = `ScanPlaceholderChoiceList.RemainingGroups`. Manual filter options = groups that still have codes.
- **Officer**: Stop F5, rebuild. Open Placeholder Manual — same titles and codes as Review Add (type `EGIY`/`VNAT` for joined leftovers).
- **Cross-skill**: visa2026-template-scan

### 2026-09-19 — Visa picker subgroups (linked active / cancel)

- **Symptom**: Review Add Visa mixed CurrentVisa roster codes with cancel stacks.
- **Decision**: Same pattern as Application subgroups. **Visa — linked active** / **Visa — cancel**. No **Visa — requested** this round (VPER/VCAT/AVPRD/AVCAT stay Application — general).
- **Fix**: Catalog `relatedBo` VisaLinkedActive / VisaCancel. Short codes unchanged. Merge unchanged.
- **Officer**: Stop F5, rebuild. **Visa — linked active** → VNUM/VTYP/VEDT. **Visa — cancel** → CVNB/CVSB/CVEB.
- **Cross-skill**: visa2026-template-scan

### 2026-09-18 — FMRLH + Application picker subgroups (family: AppScalar)

- **Symptom**: Letter needed relationship-only `adamsynyň`; Application list too large for FM header codes.
- **Fix**: **FMRLH** → `FamilyMember_Relationship_NameTm`. Application subgroups: general / family member / cancellation / business trip.
- **Officer**: Stop F5, rebuild. **Application — family member** → **FMRLH**.
- **Cross-skill**: visa2026-template-scan

### 2026-09-18 — FMWZP FM Wezipesi roster (family: ItemRoster)

- **Symptom**: Review Add Family member showed FMEIY/FMESP but not FM_WezipesiTm for sanaw Wezipesi.
- **Cause**: Education-only catalog round deferred Wezipesi.
- **Fix**: Catalog **FMWZP** → `FM_WezipesiTm` (Family member, Row). Sanaw row key + Analyze prefers FMWZP on wezipesi headers.
- **Officer**: Stop F5, rebuild. Family member → **FMWZP**.
- **Cross-skill**: visa2026-template-scan

### 2026-09-18 — FMEIY/FMESP FM roster education (family: ItemRoster)

- **Symptom**: FM sanaw Bilimi / Hünäri need dependent rules (Çaga vs Orta defaults), separate from EGIY/EGSP.
- **Decision**: Catalog **FMEIY** / **FMESP** under Family member. Child = age < 18 or MaritalStatus Çaga → Çaga. Adult = Education BO if set, else `Orta, Orta mekdep` / `Orta bilim`. Employees = real education.
- **Fix**: `FamilyMemberEducationCaption` + `FM_EducationLevelAndInstitutionTm`; sanaw row keys; Analyze prefers FMEIY/FMESP on bilimi/hünäri headers.
- **Prevent**: Do not map FM Bilimi/Hünäri to EGIY/EGSP when adult empty-education fallback is required. Wezipesi still `FM_WezipesiTm` (not catalogued this round).
- **Officer**: Stop F5, rebuild. Filter Family member → **FMEIY** / **FMESP**.
- **Cross-skill**: visa2026-template-scan

### 2026-09-18 — FMSPH header phrase + SPPOS (family: AppScalar / FM letter)

- **Symptom**: Çakylyk Almak FM yellow text `adamsynyň (İzzet Taşdelen-İşe goýberiş…)` needed one header map; multi-dependent same sponsor.
- **Decision**: Phrase shape A (joined relationships + one sponsor). One sponsor per letter. New composite header + Header SPPOS.
- **Fix**: **FMSPH** → `FamilyMember_SponsorPhraseTm` (`{FMREL} ({SPFNM}-{SPPOS})`). **SPPOS** → `SponsoringEmployee_PositionTm` (Header; **PSEP** stays Row). Helper + tests. Header merge dict keys.
- **Prevent**: Do not invent per-dependent sponsor repeats in the header. Do not put spaces around `-` inside the parens.
- **Officer**: Stop F5, rebuild. Library filter `FMSPH` / `SPPOS`. Map yellow block to **FMSPH**.
- **Cross-skill**: visa2026-template-scan
# Learnings (append-only): User report templates (Word / Excel seeds)

Purpose: capture Resminamalar / DocxTemplater / Extract–Validate / **`ItemRows`** pitfalls from user-seeded templates under **`Resources/Templates/`**. Agents **read before** debugging merge or placeholder work on a similar template; **append after** a resolved incident.

Keep **`SKILL.md`** stable; **promote** into `SKILL.md` only when the same lesson has recurred.

## How to use

**Before** `ItemRows` merge errors, invalid Extract counts, or new registration/list seeds: skim **## Entries**.

**After** fix is verified in app (Resminamalar OK + Validate green): append one entry (date, template, symptom, root cause, fix, prevent) using the template below.

```markdown
### YYYY-MM-DD — <Basename>.docx (family: ItemRows | …)

- **Symptom**:
- **Root cause**:
- **Fix**:
- **Prevent**:
```

---

## Entries

### 2026-09-18 — Linked WP item AS / valid-to (family: ItemRoster)

- **Symptom**: Review Add had no current WorkPermitItem AS number or Valid to. Sanaw *Rugsat edilen möhleti* / AS-№ could not be mapped except as cancel CWAB/CWEB.
- **Root cause**: Only WPNM and WPLC were catalogued. Merge line already had AS/start/expiration; Excel/sanaw dicts did not copy them.
- **Fix**: **WPAS** **WPST** **WPED**. Row dict keys + aliases. WPED is current Valid to; CWEB stays cancel stack.
- **Prevent**: Mirror invitation INVN/INVS/INVE for current WP item, not only cancel blocks.
- **Officer**: Stop F5, rebuild. Filter `WPAS` / `valid to`. Preview fills from the linked Work permit item.
- **Cross-skill**: visa2026-template-scan

### 2026-09-18 — AWPLC case Work permit location (family: ItemRoster)

- **Symptom**: Excel sanaw last column *Goşulmaly hereket çäkleri* had no roster token for Case summary Work permit location. WPLC/CWLB are the person’s WorkPermitItem areas.
- **Root cause**: Instance `MovementPermitLocation` had header `MovementPermitLocation_NameTm` only — no Row short code and no sanaw/excel row key.
- **Fix**: Row **AWPLC** → `Application_WorkPermitLocation_NameTm` (print stored catalog text as-is). Same value on every selected person. Header `{{ds.…}}` not added.
- **Prevent**: Do not reuse WPLC/CWLB for case Work permit location. Analyze caption *Goşulmaly hereket çäkleri* → AWPLC.
- **Officer**: Stop F5, rebuild, Analyze. Last column → **AWPLC**. Filter `AWPLC` / `goşulmaly`. Preview repeats the Case summary locations.
- **Cross-skill**: visa2026-template-scan

### 2026-09-16 — BTAD missing Region/City (family: ItemRows)

- **Symptom**: `BusinessTripAddress_FullAddress` printed lodging street only; case To region / To city not in Sanaw.
- **Root cause**: BTAD used lodging `FullAddress` / lodging.City; lodging rows often have street only. ADRS uses Region+City+FullAddress via `AddressOfResidenceReportText.CityAndStreet`.
- **Fix**: BTAD = same `CityAndStreet(ToRegion, ToCity, lodging/hotel/… street)` as ADRS.
- **Prevent**: Do not treat site catalog FullAddress as the full ADRS-shaped merge; always join case ToRegion/ToCity.
- **Officer**: Stop F5, rebuild, Preview Resminamalar. Expect `Balkan welaýatynyň, Türkmenbaşı etraby, <lodging street>`.
### 2026-09-16 — BTFRG empty when FromRegion nav missing (family: AppScalar)

- **Symptom**: Catalog Preview left From Region (#8 / **BTFRG**) blank while From City / To Region / To City filled.
- **Root cause**: `FromRegionName_Genitive` used only `FromRegion` and `FromCity.Region`. Those navigations are often unloaded; City.RegionName was ignored. From city save did not reload Region.
- **Fix**: Fall back to `FromCity.RegionName` (and ObjectSpace reload). Workspace From city assigns FromRegion after ReloadObject.
- **Prevent**: Origin region tokens must not depend on a loaded FromRegion navigation.
- **Officer**: Stop F5, rebuild. Fill From region or From city, then Preview.

### 2026-09-16 — Purpose Add-list name (family: AppScalar)

- **Symptom**: Maksady yellow on the business-trip letter. Officer opened Add in the Application group and could not find the instance **Purpose** property.
- **Root cause**: **BTPRP** was labelled “Business trip purpose” and grouped under Business trip, not the XafDisplayName **Purpose**.
- **Fix**: Catalog label **Purpose**, relatedBo Application. Search **Purpose** / **Maksady** / **BTPRP**. Merge still fills `Purpose` / alias **BTPRP**. Do not use RGEL (Purpose of arrival) or BusinessTripPurpose lookup.
- **Prevent**: Add-list English names must match the case field display name.
- **Officer**: Stop F5, rebuild, Analyze. Filter `Purpose` or `Maksady`. Pick **Purpose — BTPRP** from **Application**.

### 2026-09-16 — From/To Region/City Add-list names (family: AppScalar)

- **Symptom**: Officer searched “From Region” / “From City” / “To Region” / “To City” and did not see BTFRG/BTFCT/BTTRG/BTTCT.
- **Root cause**: Catalog labels used “district” and genitive/ablative wording; group was Business trip.
- **Fix**: Labels match the instance property names. RelatedBo Application. Search expand includes FromCity / ToCity.
- **Prevent**: Keep Add-list English names equal to XafDisplayName on the case fields.
- **Officer**: Stop F5, rebuild. Filter those four names or BTFRG / BTFCT / BTTRG / BTTCT.

### 2026-09-15 — Business trip Gitmek/Gelmek letter + sanaw (family: AppScalar + ItemRoster)

- **Symptom**: Create from yellow marks had no codes for trip dates, `2 (iki) gün`, from/to welaýat/etrap, Maksady, or sanaw *Iş saparynda boljak salgysy*.
- **Root cause**: NotMapped trip fields were uncatalogued. Destination lives on instance Region+City; origin is FromCity (hidden on BT profiles). Duration count shared TPCNT guessing.
- **Fix**: Header **BTSD** **BTED** **BTDCNT** **BTDCTX** **BTFRG** **BTFCT** **BTTRG** **BTTCT** **BTPRP**. Row **BTAD**. Merge header dict + sanaw/excel rows. Duration words. To* prefer Region/City `NameTm`.
- **Prevent**: Purpose is free-text **BTPRP**, not BusinessTripPurpose lookup. `-den`/`-ne` stay in the Word. `iş saparyna gidýändigini` is static.
- **Officer**: Stop F5, rebuild. Analyze Gitmek/Gelmek letter and sanaw. Add **Business trip** group if a mark is empty.

### 2026-09-14 — CINB/CISB/CIEB cancel invitation sanaw stacks (family: ItemRoster)

- **Symptom**: Excel *Çakylygyň belgisi* / resmileşdirilen / möhleti had no cancel-stack codes. Add showed INVN/INVS/INVE only.
- **Root cause**: Invitation merge had current+previous fields but no stacked block short codes (unlike CWAB/CWSB/CWEB).
- **Fix**: **CINB** / **CISB** / **CIEB** join Current then Previous invitation headers; skip duplicate Invitation.ID; `1)` only when two values.
- **Prevent**: INVN is current-only. Invitation has no separate AS field — CINB is InvitationNumber (COO…).
- **Officer**: Stop F5, rebuild, Analyze. Add **Cancel invitation AS numbers — CINB**.

### 2026-09-14 — CICNT / CICTX cancel invitation letter counts (family: AppScalar)

- **Symptom**: Create from yellow marks Add list had Cancel visa / Cancel work permit counts but no invitation count. Letter *çakylygyny ýatyrmak* could not be mapped.
- **Root cause**: Catalog and merge header dict omitted `CancelInvCount`. Count was roster people with a CurrentInvitationItem.
- **Fix**: Header **CICNT** / **CICTX**. Count distinct Invitation headers on linked InvitationItems.
- **Prevent**: Two people on one invitation = CICNT 1. Do not use TPCNT or INVN for the letter count.
- **Officer**: Stop F5, rebuild, Analyze. Add **Cancel invitation count — CICNT** and **(words) — CICTX**.

### 2026-09-14 — One work permit prints without 1) (family: ItemRoster)

- **Symptom**: Cancel-WP sanaw with one linked permit showed `1) COO…` on AS/tassyk/dates.
- **Root cause**: Numbered join always prefixed, including a single line.
- **Fix**: `1)` / `2)` only when the block has two or more values. One valid WP (or one visa) prints the value alone. CWLB stays unnumbered.
- **Prevent**: Do not number a one-document stack.
- **Officer**: Stop F5, rebuild, Preview 9/-1687. Two linked WPs still get `1)` `2)`.

### 2026-09-14 — CWLB locations stay unnumbered (family: ItemRoster)

- **Symptom**: Officer does not want `1)` / `2)` on *Hereket edýän çägi* (**CWLB**).
- **Root cause**: Locations used the same numbered join as AS/tassyk/dates/visa.
- **Fix**: `CancelWorkPermit_LocationsBlock` joins without ordinals. Other cancel blocks keep `1)` `2)`. Duplicate location still prints once.
- **Prevent**: Do not number CWLB / WPLC.
- **Officer**: Stop F5, rebuild, Preview. Location cell is the area name only.

### 2026-09-14 — Cancel stack blocks print 1) 2) order prefixes (family: ItemRoster)

- **Symptom**: Sanaw yellow sample uses `1) COO…` / `2) COO…` on stacked AS, tassyk, dates, visa. Merge printed raw values only.
- **Root cause**: `JoinVisaFieldLines` joined Current + Previous/Next with newlines and no ordinal.
- **Fix**: Each non-empty line is `1) `, `2) `, … (visa CVNB/CVSB/CVEB and WP CWNB/CWAB/CWSB/CWEB/CWLB). One line still gets `1)`. CWLB still skips a duplicate location.
- **Prevent**: Do not put `1)` in the yellow sample as the merge source; numbering is merge.
- **Officer**: Stop F5, rebuild, Preview SANAW. Stacked cells show `1)` then `2)`.

### 2026-09-14 — CWLB Add search missed Hereket / Work Permitted Locations (family: ItemRoster)

- **Symptom**: Filter `hereket` or `Work Permitted Locations` did not list CWLB.
- **Root cause**: Catalog tk-TM/en labels did not include the Excel column caption or the BO display name.
- **Fix**: WPLC/CWLB labels include *Hereket edýän çägi* / Work permitted locations. Add search expands those phrases.
- **Prevent**: Keep officer column captions on catalog labels.
- **Officer**: Analyze maps #9 to CWLB. Filter `CWLB` if a mark is still empty.

### 2026-09-14 — Cancel WP sanaw Hereket edýän çägi missing (family: ItemRoster)

- **Symptom**: Review #9 Hereket edýän çägi had no Work permit locations code. Add list showed CWNB/CWSB/CWEB/WPNM only.
- **Root cause**: `WorkPermit_WorkPermittedLocations` existed on the merge line with no catalog short code.
- **Fix**: **WPLC** (current locations) and **CWLB** / `CancelWorkPermit_LocationsBlock` (Last-N stack; skip duplicate if both WPs share the same area). Excel *Hereket edýän çägi* → CWLB.
- **Prevent**: Do not leave required WorkPermitItem scalars uncatalogued. Search Add: `location`, `WPLC`, `CWLB`, `Work Permitted Locations`.
- **Officer**: Stop F5, rebuild, Analyze. #9 → CWLB (or Add WPLC for current-only).

### 2026-09-14 — Cancel WP sanaw AS-№ stacked block (family: ItemRoster)

- **Symptom**: Daşary ýurt raýatlarynyň sanawy AS-№ column needs Current + Previous `WorkPermitItem.ASNumber` (COO…). Tassyknama belgisi is Work Permit Number (**CWNB**).
- **Root cause**: Catalog had CWNB / CWSB / CWEB but no AS stack. Current-only `WorkPermit_ASNumber` is not the cancel pair.
- **Fix**: **CWAB** / `CancelWorkPermit_ASNumberBlock` (same Join lines as CWNB). Excel header *AS-№* → CWAB, *Tassyknama belgisi* → CWNB. Do not swap with old XtraReports maps (those had AS and tassyk reversed).
- **Prevent**: Do not map AS-№ to WPNM / CWNB. Tassyknama is CWNB.
- **Officer**: Stop F5, rebuild, Analyze the Excel. AS-№ → CWAB. Tassyknama → CWNB.

### 2026-09-12 — Cancel visa+WP CWCNT / stacked WP blocks (family: AppScalar + ItemRoster)

- **Symptom**: Visa+WP cancel letters and sanaw need a work-permit-to-cancel count and stacked WP number/date blocks, same as CVCNT / CVNB.
- **Root cause**: `CancelWPCount` counted roster lines + Previous WP. Catalog had no CWCNT / CWNB. Hydrator already stacks Current + Previous WP.
- **Fix**: Count distinct WorkPermitItem links (`ApplicationProfileInstanceCancelCounts.WorkPermits`). Catalog **CWCNT** / **CWCTX** (Header) and **CWNB** / **CWSB** / **CWEB** (Row, Application + ApplicationItem). Merge header + sanaw/Excel row dicts include the properties.
- **Prevent**: Do not use WPNM (current WP only) for the cancel stack. Do not count persons for CWCNT.
- **Officer**: Stop F5, rebuild, restart. Map *iş rugsatnamasyny ýatyrmak* to CWCNT. Preview fills from linked WPs.

### 2026-09-12 — Cancel-visa sanaw PBPL printed country (family: ItemRoster)

- **Symptom**: SANAW-WIZANY YATYRMAK Preview showed Türkiye for **PBPL** (Doglan ýeri) on case 8/-1307.
- **Root cause**: `Person_BirthPlace` was the raw `Person.BirthPlace` (`Türkiye/Gaziantep` or country-only). **PCBT** already prints the country.
- **Fix**: `PersonBirthPlaceText.CityOnly` — city after `/`, never the country name. Catalog example is Kahramanmaraş.
- **Prevent**: Do not fall back PBPL to CountryOfBirth.

### 2026-09-12 — Cancel-visa sanaw omitted Person_BirthPlace (family: ItemRoster)

- **Symptom**: After mapping **PBPL**, Preview would stay blank — `BuildWizaYatyrylmakSanawRowDictionary` had no `Person_BirthPlace`.
- **Root cause**: Cancel-visa row dict was a short cancel-visa subset.
- **Fix**: Add `Person_BirthPlace` and `Person_CountryOfBirthTm` (`PCBT`).
- **Prevent**: When Review maps a Person row token on this sanaw, put the same key on the cancel-visa dict.

### 2026-09-12 — Cancel-visa sanaw EGIY blank for child dependents (family: ItemRoster)

- **Symptom**: SANAW-WIZANY YATYRMAK Preview left Hünäri we bilimi empty for child dependents (case 8/-1307). Officers map `EGIY` / `Education_LevelAndInstitutionTm`.
- **Root cause**: Children have no Education record. Cancel-visa sanaw row dict omitted `Education_LevelAndInstitutionTm`.
- **Fix**: Child dependents (`!IsEmployee` and Age &lt; 18 or marital status Çaga/Minor) print **Çaga**. Add the key to cancel-visa and sanawy row dicts.
- **Prevent**: Do not leave EGIY out of `BuildWizaYatyrylmakSanawRowDictionary`. Do not use Age &lt; 18 when DateOfBirth is default (Age is 0).

### 2026-09-12 — Cancel-visa sanaw Education_* empty without Education tile (family: ItemRoster)

- **Symptom**: Cancel-visa Review could not offer `EGLV` / `EGIN` / `EGSP`. Even after mapping, Preview would stay blank if Education was not linked.
- **Root cause**: Placeholder pack followed the hidden People & links Education toggle. Hydrator set `CurrentEducation` only from an Education resolved link.
- **Fix**: PersonEducation tokens stay in the profile set. Hydrator falls back to `PersonCurrentItems.GetCurrentEducation`.
- **Prevent**: Do not require an Education pin to fill `Education_*` on cancel-visa sanaw.

### 2026-09-12 — Cancel-visa letter CVCNT stayed 1 with two linked visas (family: AppScalar)

- **Symptom**: Wizany Ýatyrmak 9/-001. Serdar has two valid linked visas (A14886414, A17327411). Review placeholders 6/7 (`CVCNT`/`CVCTX`) and Preview showed `1 (bir)`.
- **Root cause**: `CancelVisaCount` summed CurrentVisa + NextVisa. NextVisa meant a future-start visa. Both booklets have already started, so only CurrentVisa counted. Hydrator also assigned only the first Last-N visa.
- **Fix**: Count distinct People & links Visa pins (`ApplicationProfileInstanceCancelCounts`). Hydrator sets NextVisa from the second linked visa (sanaw stacked fields).
- **Prevent**: Do not treat NextVisa as "second visa to cancel". Last-N pins are the cancel set.
- **Officer**: Stop F5, rebuild, restart. Preview Ýüztutma — `2 (iki)` at the visa-cancel pair. No Re-Approve.
- **Cross-skill**: visa2026-resminamalar | visa2026-template-scan

### 2026-09-11 — Cancel-visa letter CVCNT / CVCTX (family: AppScalar)

- **Symptom**: Wizany Ýatyrmak Ýüztutma needs a visa-count pair next to *wizasy ýatyrmak*, distinct from person count *daşary ýurt raýaty*. Officers had no short codes; yellow scan mapped both `1 (bir)` to TPCNT.
- **Root cause**: `CancelVisaCount` / `CancelVisaCountText` already exist on the instance (CurrentVisa + NextVisa per line) but were not in the catalog or header merge dictionary.
- **Fix**: Catalog **CVCNT** / **CVCTX** (Header, Core, Application). `BuildApplicationHeaderDictionary` includes the properties so Enrich adds the short codes. Scan remaps the visa-cancel pair (see template-scan).
- **Prevent**: Person count stays TPCNT. Do not invent a new NotMapped count. Link CurrentVisa / NextVisa on the roster or the visa count stays 0.
- **Officer**: Stop F5, rebuild, restart. Analyze then Preview the cancel-visa letter. `{{ds.CVCNT}}` / `{{ds.CVCTX}}` fill from the case.
- **Cross-skill**: visa2026-template-scan | visa2026-resminamalar

### 2026-09-11 — Passport-change Excel sanaw two stacked passport tables

- **Symptom**: Wizany KP-i Täze Pasporta Geçirmek (`pasport_change`) DAŞARY ÝURT RAÝATYNYŇ SANAWY Preview filled only the latest passport. Kiçirak (previous booklet) and Täze (new booklet) tables both showed the current row or stayed empty.
- **Root cause**: Catalog/scan used `PPN` for both tables. Excel merge expanded only the first `{{#ds.rows}}` row and header-merged the second table with no roster line. Loop planner emitted one loop on the min yellow row. Deleting `{{/ds.rows}}` on the next row would wipe the Täze title.
- **Fix**: Previous-passport codes `PRPN`/`PRIS`/`PRED`/… . Scan remaps Kiçirak yellows to that family. Merge dict includes `PreviousPassport_*`. Excel expands every loop row plus stacked-table prototype rows (bottom→top), overlays current passport keys from previous under Kiçirak, and does not delete title rows. People & links still hydrates last two passports by issue date.
- **Prevent**: Do not treat one `{{#ds.rows}}` as the whole sheet. Do not delete a close-marker row that also has a section title. Do not remap `RPPN` (wekil). Person must have two linked passports.
- **Officer**: Stop F5, rebuild, restart. Preview DAŞARY ÝURT RAÝATYNYŇ SANAWY on 5/-814. Already-approved `{{.PPN}}` on both tables should fill (Kiçirak = previous, Täze = new). New Analyze should propose `PRPN` on Kiçirak. If Kiçirak stays blank, link the old booklet on People & links.
- **Cross-skill**: visa2026-template-scan | visa2026-resminamalar

### 2026-09-11 — Change-invitation Ýüztutma officer confirmed (family: AppScalar)

- **Symptom**: Officer confirmed the one-invitation letter after rebuild.
- **Root cause**: Same as the entry below — header invitation tokens were missing from the Invitation group.
- **Fix**: Verified in app. `{{ds.INVN}}` / `{{ds.INVS}}` / `{{ds.INVE}}` on one Ýüztutma; several InvitationItems share that header.
- **Prevent**: Keep one invitation per change-invitation application until the officer asks for a table of invitations.
- **Cross-skill**: visa2026-template-scan | visa2026-resminamalar

### 2026-09-11 — Change-invitation Ýüztutma (family: AppScalar)

- **Symptom**: Çakylygy üýtgetmek letter needs invitation number / issued date / expiry in the paragraph. Catalog had only row `INVN`.
- **Root cause**: Invitation fields lived on the roster line only. A change-invitation case may link several InvitationItems (people) but for now one Invitation header per application.
- **Fix**: Catalog Invitation group: `INVN` / `INVS` / `INVE` as Header+Row. Instance `Invitation_*` reads the linked InvitationItem header (then roster, then produced Invitations). Word: `{{ds.INVN}}` `{{ds.INVS}}` `{{ds.INVE}}` in the letter. No `{{#ds.invitations}}` table yet.
- **Prevent**: Do not emit one letter page per InvitationItem. Do not join several invitation numbers into one token unless the officer asks for multiple invitations per case.
- **Cross-skill**: visa2026-template-scan | visa2026-resminamalar

### 2026-09-10 — Yuztutma cover letter (family: AppScalar)

- **Symptom**: Resminamalar Preview of a yellow-marks Word letter left `{{ds.AFNUM}}` / `ADAT` / `MSRV` / `TPCNT` empty; `ACPOS` / `CHFN` filled. File name used a roster person.
- **Root cause**: ApplicationItem-root DocxTemplater model had signatory aliases on the merge line but not FullApplicationNumber, MigrationService_NameTm, or TotalPersonCount. Thin header dictionary also omitted those letter scalars.
- **Fix**: Header dictionary always includes those instance properties (short-code aliases via Enrich). Item-root merge copies the header dict and falls back to the parent instance instead of overwriting with blanks.
- **Prevent**: Cover letters with only `{{ds.*}}` must not rely on roster-line property names. Do not add one-off `FullApplicationNumber` aliases on `ApplicationRosterMergeLine` when the instance already has them.
- **Cross-skill**: visa2026-template-scan | visa2026-resminamalar

### 2026-09-10 — Temporarily disable Resources/Templates seed (officer re-author)

- **Symptom**: Officers will re-add Word/Excel from case Resminamalar (Add existing / Create from yellow marks). Shipped seeds would reappear on F5 (`DEBUG` always re-seeds).
- **Fix**: `UserReportTemplateUpdater.SeedEmbeddedTemplatesEnabled = false`. Seed binaries, `*_map.md`, and scans deleted from `Resources/Templates`. Local F5 `visa2026` nested + shared catalog rows purged.
- **Prevent**: Do not set the flag back to true until seed files are restored and embedded in the Module csproj.
- **Cross-skill**: visa2026-resminamalar | visa2026-template-scan | visa2026-application-profile

### 2026-09-10 — Forma 16 / Hasaba empty TRDT TRCK AVCAT (Emre Akbulut)

- **Symptom**: Resminamalar Preview of officer `_F16.docx` warned Line "Emre Akbulut": `Travel_CheckPointTm`, `Travel_DateText`, `Application_VisaCategory_NameTm` empty. People & links had Travel history Entry 11.08.2025 Aşgabat şäher howa menzili MGSP and a linked visa.
- **Root cause**: Official Forma 16 and yellow-marks Review use registration **TRDT**/**TRCK** and instance **AVCAT**. Hasaba Almak has no registration-line TravelDate/CheckPoint and `RequireVisaCategory` is off, so instance `VisaCategory` is null. Values live on linked `TravelHistory` and `Visa`.
- **Fix**: `Travel_DateText` / `Travel_CheckPointTm` fall back to `CurrentTravelHistory` (date + place/city) when the registration line is empty. `Application_VisaCategory_NameTm` falls back to linked visa, then invitation. Registration/instance values still win when set. Catalog tokens stay distinct (no TRDT=THDT alias).
- **Prevent**: Do not tell officers to remap F16 Giren wagty/ýeri to THDT/THCP just to fill Preview. Do not require Visa category on the Hasaba instance when the person visa already has one.
- **Verified**: Officer confirmed Hasaba F16 Preview filled after rebuild.
- **Cross-skill**: visa2026-application-profile | visa2026-resminamalar | visa2026-template-scan

### 2026-09-09 — TravelHistory placeholders for People & links (THKD/THDT/THCP)

- **Symptom**: After Travel history was required on profiles, Create from yellow marks / Resminamalar still had no tokens for People & links Kind / Date / Check point. Existing **TRDT** / **TRCK** are registration-line `TravelDate` / `CheckPoint`, not `TravelHistory`.
- **Root cause**: `PersonTravelHistory` pack existed with zero catalog rows. Merge hydrator never assigned a linked `TravelHistory`.
- **Fix**: Catalog **THKD** Kind, **THDT** date, **THCP** checkpoint, **THPL** place (checkpoint or city), plus type/country/region/city/notes. Hydrator sets `CurrentTravelHistory` from resolved links. Do not alias TRDT/TRCK to TravelHistory.
- **Prevent**: People & links travel columns map to `TravelHistory_*`, not registration `Travel_*`.
- **Cross-skill**: visa2026-application-profile | visa2026-template-scan



- **Symptom**: Placeholders 12 ACPOS and 13 ACFNM visible on Review below the table; Resminamalar Preview showed only the 11-column people list.
- **Root cause**: Excel header dictionary did not include CompanyHead. Footer cells are merged with no roster line, so missing header keys become blank.
- **Fix**: `BuildApplicationHeaderDictionary` always includes signatory position + name (aliases ACPOS/ACFNM). Excel merge `.` tokens can resolve from header.
- **Prevent**: Footer signatory tokens must live on the application header dict, not only on Extracted Placeholders or the ItemList row dictionary.

### 2026-09-09 — ADRS includes City (Sanaw_hasaba_alys.xlsx)

- Symptom: Hasaba almak Preview *Türkmenistandaky salgysy* showed only the street (`FullAddress`). People & links also has City (`Turkmenbashy etraby`).
- Root cause: `Address_FullAddress` / `ADRS` returned `CurrentAddressOfResidence.FullAddress` only.
- Fix: `AddressOfResidenceReportText.CityAndStreet` prefixes `City.NameTm` when it is not already in the street. Same getter on the roster line and WorkPermitItem.
- Prevent: Do not add a second city token for this column; ADRS is city + street. Do not duplicate City when FullAddress already contains it.

### 2026-09-09 — ADRS residence address (family: ItemList)

- Symptom: Yellow-marks Review attached company ACADR for Türkmenistandaky salgysy because officers could not find a case-linked residence token.
- Root cause: ADRS existed (Address_FullAddress on the roster line from People & links Address) but pack PersonAddressOfResidence hid it; salgy captions always preferred ACADR.
- Fix: Catalog ADRS packKey Core; still relatedBo AddressOfResidence. Merge unchanged (ApplicationRosterMergeLine.Address_FullAddress).
- Prevent: Do not map person residence to Application_Company_Address. Company legal address stays ACADR (yuridiki / kärhana only).

### 2026-09-09 — Sanaw_hasaba_alys.xlsx (family: ItemList)

- **Symptom**: Uploaded / synced wide Excel templates kept portrait PageSetup; Resminamalar Preview squeezed the sanaw.
- **Root cause**: Preview PDF orientation came from DevExpress Spreadsheet default, not from used-range width. Upload did not write Landscape into the stored file.
- **Fix**: `ExcelPreviewPageLayout.StampFromContent` on nested save, master `WriteMasterFile`, and staging/HTTP upload. Preview still infers landscape even if the stored file stays portrait.
- **Prevent**: Do not add an officer page-layout picker. Do not rebuild xlsx with ZipFile. Preview owner is resminamalar (`ApplicationWordReportOfficePreviewPdfConverter`), not XtraReports.

### 2026-09-02 — Signatory passport expiration (`CHPE`)

- **Symptom**: Create from yellow marks Review had no Authorized signatory expiration token for `19.02.2034ý.` (only CHPD issue date).
- **Root cause**: `AuthorizedSignatory` had no expiration property; catalog had no `CompanyHead_PassportExpirationDateText`.
- **Fix**: Persistent `PassportExpirationDate`, tenant seed, catalog **CHPE**, merge getters on instance and roster line.
- **Prevent**: Signatory passport dates need both issue (`CHPD`) and expiration (`CHPE`), same as Person `PPID` / `PPED`.

### 2026-09-02 — Wrapped `{{IMAGE:Person_Photo}}` after Word download

- **Symptom**: Resminamalar Preview showed literal `{{IMAGE:Person_Photo}}` in the şahsy photo box after download + Add existing template. It had worked on the scan-generated row.
- **Root cause**: The token is longer than the photo cell. Word inserts a line break / second paragraph. Injector required one contiguous `[\w]+` match. Catalog **PPH** is short enough to stay on one line.
- **Fix**: Injector joins table-cell paragraphs and strips wrap whitespace; Extract stores `IMAGE:Person_Photo`. Scan `BuildWordToken` emits `{{IMAGE:PPH}}` again (alias still fills `Person.Photo`). Do not edit seed `.docx`.
- **Prevent**: Prefer `{{IMAGE:PPH}}` in officer-created Word photo boxes. Keep seed `{{IMAGE:Person_Photo}}` as authored.

### 2026-09-02 — Image tokens are `{{IMAGE:Person_Photo}}` not `{{IMAGE:PPH}}`

- **Symptom**: Catalog short code **PPH** wrote `{{IMAGE:PPH}}`. Preview cleared the token instead of injecting `Person.Photo`.
- **Root cause**: `WordUserReportImageInjector` looks up photos by `Person_Photo`. `[\w]+` captures `PPH` as a different key.
- **Fix**: `BuildWordToken` for `IsImage` uses `CanonicalPath`. `TemplateTokenSyntax.TryGetShortCode` maps `Person_Photo` → **PPH**. Injector falls back `PPH` → `Person_Photo`. Create from yellow marks writes the canonical token when a sample portrait is in the Word body.
- **Prevent**: Do not emit `{{IMAGE:PPH}}` on new Word templates. Do not edit seed `.docx` layout. Excel still rejects image tokens.

### 2026-09-01 — `{{ds.PVFM}}` is invalid on ApplicationProfileInstance

- **Symptom**: Create from yellow marks Preview blocked Approve: `Person_VisaApplicationFamilyMembersText` (and other Person row fields) not found on ApplicationProfileInstance.
- **Root cause**: Row-only catalog codes written as `{{ds.CODE}}` bind on the instance. They belong on `ApplicationRosterMergeLine` as `{{.CODE}}` (letters without `{{#ds.rows}}` still fill via first-roster promotion).
- **Fix**: `BuildWordToken` honors catalog Row/Header; scan rewriter turns leftover `{{ds.PVFM}}` into `{{.PVFM}}`.
- **Prevent**: Do not add Person_* getters to ApplicationProfileInstance to silence scan validation.

### 2026-09-01 — `Person_VisaApplicationFamilyMembersText` / **PVFM**

- **Symptom**: Officers needed the employee visa family block in Create from yellow marks / the placeholder picker. Only **SKFM** (`SahsyKagyz_FamilyStatusText`) existed, which is the formatted Maşgala ýagdaýy line.
- **Root cause**: `Person.VisaApplicationFamilyMembersText` had no catalog short code or roster merge getter.
- **Fix**: Catalog **PVFM** → `Person_VisaApplicationFamilyMembersText`. Merge line reads the employee (or sponsor for family-member rows). Sanawy + Şahsy row dicts include the key. Excel header `wiza üçin maşgala` → PVFM. Labels are the officer editor caption, not **Maşgala ýagdaýy**.
- **Prevent**: Do not reuse **SKFM** for the raw stored lines. Do not edit şahsy_kagyz.docx; officers type `{{ds.rows.Person_VisaApplicationFamilyMembersText}}` or `{{.PVFM}}` where they need the raw block.

### 2026-09-01 — Person catalog tokens PCBT / PMNM / PMST / PNTM / PSEF / PSEP

- **Symptom**: Sanaw uses `Person_CountryOfBirthTm`; Forma 16 uses sponsoring-employee name/position; middle name, marital status, and nationality name existed on the merge line but not in the picker.
- **Root cause**: Catalog had codes (`PCBC`, `PNAT`) and first/last name only.
- **Fix**: **PCBT**, **PMNM**, **PMST**, **PNTM**, **PSEF**, **PSEP**. Sanawy/Forma 16 row dicts include the keys. Excel birth column prefers PCBT (name) not PCBC (code). **PSEF** is roster sponsor, not header **SPFNM**. **PNTM** tk-TM is **Raýatlyk ady** (not **Raýatlygy**) so Sanaw nationality **code** column stays **PNAT**.
- **Prevent**: Do not add HireDate / Email / Age until a merge getter exists. Do not reuse **SPFNM** for the family-member sponsor row.

### 2026-09-01 — Education institution / country / graduation year tokens

- **Symptom**: Şahsy kagyz and Sanaw use `Education_InstitutionName` and `Education_CountryCode`, but Create from yellow marks / placeholder picker only had `EGLV` / `EGIY` / `EGSP`.
- **Root cause**: Merge line already exposed institution, country code, and graduation year; catalog never listed them.
- **Fix**: Short codes **EGIN**, **EGCC**, **EGYR** (`PersonEducation` / Education). Sanawy + Şahsy row dicts include the keys. Excel header `okan ýeri` → EGIN.
- **Prevent**: When a map §6 token exists on the roster merge line, add the catalog short code in the same change.

### 2026-09-01 — `Person_PreviousWorkplacesInTurkmenistan` / `PWTM` (Şahsy kagyz F09)

- **Symptom**: Şahsy kagyz **Türkmenistanda öňki işlän ýerleri** had no merge token; map F09 was a static blank underline.
- **Root cause**: Person field existed but catalog, roster merge line, and `BuildSahsyKagyzRowDictionary` had no key.
- **Fix**: Catalog **PWTM** → `Person_PreviousWorkplacesInTurkmenistan`; merge line + sahsy/sanawy row dicts; map **1.0.5**. Word seed still needs the officer to type `{{ds.rows.Person_PreviousWorkplacesInTurkmenistan}}` on the underline (do not edit `.docx` in repo).
- **Prevent**: New Person merge fields need catalog + `ApplicationRosterMergeLine` + the row dictionary that template actually uses (`BuildSahsyKagyzRowDictionary` here).

### 2026-09-01 — Passport type / issued country / authority tokens + related-BO groups

- **Symptom**: Yellow marks for passport type, issued country, and issuing authority had no library tokens. Officers and AI saw a flat placeholder list mixed across Person, company, wekil, and passport.
- **Root cause**: Catalog had `PPN`/`PPIS`/`PPED` only. `Passport_Authority` / `Passport_CountryCode` / `Passport_CountryTm` existed on the roster merge line but were not catalogued. `PassportType` had no merge property. Manual and Azure payload were a single A–Z list.
- **Fix**: `Passport_TypeTm` + short codes `PPTP`, `PPAT`, `PPCC`, `PPCT`. Catalog `relatedBo` groups the officer Placeholder manual, Review Add-placeholder optgroups, and Azure `allowedTokensByBo`.
- **Prevent**: New tokens need `packKey` (profile gate) and `relatedBo` (manual/AI group). Do not put roster passport fields on wekil `RPPA` / signatory `CHPA`.

### 2026-09-01 — Company registration date placeholder `ACRDT`

- **Symptom**: Yellow `02.02.2009ý.` on borçnama mapped to `ApplicationDateText` because no company registration date existed.
- **Root cause**: `CompanyProfile` had no registration date; catalog had no token; scan date regex always chose `ADAT`.
- **Fix**: `CompanyProfile.RegistrationDate` + `Application_Company_RegistrationDateText` / `{{ds.ACRDT}}`. Tenant JSON `2009-02-02`.
- **Prevent**: Isolated dates next to hasaba alyş / şahamça are company registration, not application date.

### 2026-09-01 — `6aylık-BORÇNAMA_02.docx` (family: letter / loose `{{.X}}`)

- **Symptom**: Catalog Preview empty after yellow-mark Approve; wizard outline had 8 placeholders including `{{.PFN}}` and no `{{#ds.rows}}`.
- **Root cause**: DocxTemplater resolves `{{.PFN}}` on `ds` when there is no row loop; those keys were not on the root bind model.
- **Fix**: `UserReportMergeDataHelper.PromoteLooseRowTokensOntoRoot` copies first-roster values onto `ds` when extracted tokens include `.X` and no `#ds.rows`.
- **Prevent**: Scan Word letters may emit row short codes; merge must flatten first person or the template needs a loop.

### 2026-05-28 — `sahsy_kagyz.docx` (family: **ItemRows**, root **`ApplicationItem`**)

- **Symptom**: Resminamalar failed (0/9): `'{{ds.rows.Person_FullName}}' could not be replaced` with context `Familiýasy, ady, atasynyň ady >> {{ds.rows.Person_FullName}} << Doglan senesi…`.
- **Root cause**: Template had **`{{ds.rows.*}}`** tokens but **no** `{{#ds.rows}}` / `{{/ds.rows}}` loop (and no `{{:s:}}{{:PageBreak}}`). DocxTemplater cannot bind `ds.rows.Property` outside a row loop. Some tokens were split across Word runs (spell-check); extractor still finds them via `InnerText`.
- **Fix** (Word): Insert `{{#ds.rows}}` before the form, `{{:s:}}{{:PageBreak}}` + `{{/ds.rows}}` before `sectPr` (own paragraphs). Rebuild embedded template in repo.
- **Fix** (code): **`BuildSahsyKagyzStyleRows`** + **`EnsureSahsyKagyzRowsWhenNeeded`** (same pattern as Forma 16).
- **Prevent**: After placing yellow placeholders, always add §7 loop tokens before Extract/Validate; confirm **`#ds.rows` count > 0** in docx XML or Extract output.

---

### 2026-05-20 — `Forma_16.docx` (family: **ItemRows**, root **`ApplicationItem`**)

- **Symptom**: Resminamalar failed: `'{{ds.rows.Person_NationalityCode}}' could not be replaced` (§2 Raýatlygy). Earlier: **65 of 93** placeholders invalid after Extract; after Word cleanup **66/66** valid and merge succeeded (TUR, photo, full form).
- **Root cause** (merge): **`{{ds.rows.*}}`** requires **`List<Dictionary<string, object>>`** (or **`List<IDictionary<string, object>>`** before `BindModel("ds", …)`). A typed POCO row type (**`RegistrationForm16MergeRow`**) did **not** bind `{{ds.rows.Property}}` like **`Contract_Inv.docx`**. If the wrong row builder runs (**`BuildLaborContractRowDictionary`**), fields above §1 that exist on labor rows still merge; **`Person_NationalityCode`** is **not** on labor rows — looks like a “new placeholder” bug but is **wrong row set**.
- **Root cause** (validation): Word **split** placeholders across runs → Extract invents fragments (e.g. `.Person_*`, partial `ds.rows`) → high invalid count until user retypes each token **in one run**.
- **Fix** (code): Revert Forma 16 rows to **`UserReportMergeDataHelper.BuildRegistrationForm16RowDictionary`**; keep **`EnsureForma16RowsWhenNeeded`** + **`IsForma16UserReportTemplate`**; cast rows to **`IDictionary<string, object>`** in **`UserReportGenerator.RenderTemplateAsync`**. Do **not** reintroduce typed row classes for **`{{ds.rows.*}}`** without proving DocxTemplater binding.
- **Fix** (Word): Retype tokens per approved **`Forma_16_map.md`** §6; **Extract → Validate** until all placeholders valid; prefer **`{{ds.rows.X}}`** or **`{{.X}}`** inside **`{{#ds.rows}}`** (both OK with dict rows).
- **Prevent**:
  - **`Person_NationalityCode`** is **not** a special BO binding — same **`ApplicationItem`** `[NotMapped]` as Xtra **`RegistrationForm16Report`** and **`BuildSanawyRowDictionary`**; add keys only in **`BuildRegistrationForm16RowDictionary`** (or sanawy/excel builders), not a one-off merge path.
  - For registration **`ItemRows`**, confirm runtime uses **`BuildRegistrationForm16StyleRows`**, not labor/sanawy unless template detection matches (see **`UserReportMergeDataHelper.IsForma16UserReportTemplate`**).
  - High invalid count after Extract → fix Word tokens first; do not add C# properties for fragments that are not real map §6 tokens.
  - Map note: **`Forma_16_map.md`** §6 — type each placeholder in a single Word run.

---
