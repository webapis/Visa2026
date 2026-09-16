
### 2026-09-16 - check_in_internal To Region/City from NewRegistrationLocation

- **Phase**: mapping + correction
- **Why**: Hasaba Almak (Welaýatdan gelmegi sebäpli) Overview showed empty To Region / To City (e.g. 5/-001). Transform only joined `BusinessTripDestination`; registration geo lives on `NewRegistrationLocation` / `PreviousRegistrationLocation` (field-map had them deferred).
- **Fix**: coalesce `BusinessTripDestination` then `NewRegistrationLocation` → ToCity/ToRegion; `PreviousRegistrationLocation` → FromCity/FromRegion. OData + case-summary PATCH write From*/To*. Patch default types include `App_Reg_Check_In_Internal` / `App_Reg_Check_Out_Internal`.
- **Verify**: patch Planned **490** / Patched **490** / Failed **0** / no id-map **2**. Local PG `check_in_internal`: **490/490** with ToCity+ToRegion (and FromCity). Refresh case Overview.
- **Prevent**: do not invent From* when Previous* null; keep To* required for check_in_internal.
### 2026-09-15 - Dummy Purpose for Iş Saparyna Gitmek imports

- **Phase**: correction (local PG)
- **Why**: Live VISA2015 has no Purpose for E:13; Calik profile `RequirePurpose=true` left Overview red (e.g. 3/-2412).
- **Fix**: Set instance `Purpose` + profile `DefaultPurpose` to `İs maksatly` for `business_trip_departure` (594 rows). Import/patch resolve empty Purpose via `Visa2014ApplicationTransform.BusinessTripDepartureDummyPurpose`. Tenant JSON `DefaultPurpose` on Iş Saparyna Gitmek; manifest **46**.
- **Verify**: `missing_purpose=0` / `dummy_purpose=594` on local PG.
- **Prevent**: Do not invent Purpose from AnketaMaksat (always empty on Calik). Keep the locked dummy string for Gitmek only (not Gelmek).

### 2026-09-15 - BTA destination Excel review merged into Calik site catalogs

- **Phase**: lookup + correction (local PG)
- **Source**: live `.15` / `VISA2015` `AddressOnBusinessTrip` DISTINCT (Excel preview approved)
- **Catalog**: merged `add_to_catalog` into tenant JSON — Lodging **59→62**, Hotel **46→51**, OtherSite **25→65**, Hospital unchanged **4**; manifest **44→45**; copied to embedded `*.json` via `SiteLookup-CalikEnergi.ps1`
- **DB sync**: `--updateDatabase --forceUpdate` blocked by PG `42P07` truncated index renames. Seeded missing rows with Npgsql UTF-8 seeder (`artifacts/SeedBtaCatalogs`); set `SystemSettings.LookupCatalogManifestVersion=45`
- **Import**: unmatched policy OtherSite; resolve uses AoR cleaners (`NormalizeHotelCatalogName` / `NormalizeLodgingCatalogAddress`) before FK match; clear sibling FKs on re-patch
- **Re-patch**: `--patch-visa2014-application-business-trip-case-summary` Planned **583** / Patched **583** / Failed **0** / no id-map **4**
- **PG result**: Lodging **405**, Hotel **103**, OtherSite **55**, PrivateHouse free-text **8** (down from **148**)
- **Prevent**: pass `--application-id-map` to source `id-maps/calik-energi-local-pg/ApplicationProfileInstance.json` (bin ContentRoot misses id-maps); do not use PS 5.1 `ConvertFrom-Json` to rewrite Turkmen tenant JSON

### 2026-09-15 — Business-trip case summary resolves Lodging/Hotel/Hospital/OtherSite

- **Need**: Stop inventing `BusinessTripAddress` catalog rows for Gitmek destination; reuse residence site tenant catalogs.
- **Fix**: `Visa2014ApplicationODataImporter.TryAddBusinessTripDestinationFields` classifies address line and resolves Lodging/Hotel/Hospital/OtherSite; PrivateHouse free text if no match. No CreateAsync on BusinessTripAddress.
- **Prevent**: Do not seed a separate business-trip-address.json unless free-text leftovers demand it. Prefer existing lodging/hotel/hospital/other-site Calik JSON.
- **Cross-skill**: visa2026-application-profile


### 2026-09-15 - 8/-1601 still empty after case-summary patch (orphan local)

- **Phase**: correction / local PG
- **Why**: Officer still on **8/-1601**. Live VISA2015 has **no** `8/-1601`. Patch filled **583** id-mapped E:13 rows; **11** local departure rows (incl. 8/-1601) had `ToCity` only — not in live extract / no id-map.
- **Fix**: SQL backfill `CityId`/`RegionId` from `ToCityID` + city→region map; also filled `Cities.RegionID` for those 5 catalog cities.
- **Verify**: 8/-1601 now City=Aşgabat şäheri, Region=Aşgabat şäheri. **Business trip address** and **Purpose** still empty (no legacy source for this local/demo case).
- **Next**: refresh Overview on 8/-1601. Enter Purpose (and address if needed) manually. Prefer imported Gitmek cases for address fill check.

### 2026-09-15 - Iş Saparyna Gitmek case summary Region/City/address not imported
### 2026-09-15 - Iş Saparyna Gitmek case summary Region/City/address not imported

- **Phase**: mapping + correction
- **Why**: Calik `business_trip_departure` shows Region, City, Business trip address, Purpose (`RequireRegion`/`RequireCity`/`RequireBusinessTripAddress`/`RequirePurpose`). Header import only posted hidden `ToCity` plus trip dates. `AddressOnBusinessTrip.AddressOnTrip` is an **Address FK**, not text.
- **Live VISA2015 E:13**: destination **587/587**; PIA AddressLine **574/587**; AnketaMaksat / PurposeOfTrave **0** (Purpose has nothing to copy).
- **Screenshot 8/-1601**: Demo Işberler, not in VISA2015. Same empty tiles as imported cases. Patch uses ApplicationProfileInstance id-map (imported rows only).
- **Fix**: transform City/Region from destination; BusinessTripAddress from Address.AddressLine; `--patch-visa2014-application-business-trip-case-summary`.
- **Next**: stop F5, rebuild DataImporter, run patch `--legacy-source calik-energi-local-pg --application-type App_Business_Trip_Departure --inprocess --no-wait`. Refresh an imported Iş Saparyna Gitmek case (not 8/-1601).

### 2026-09-14 - App_Visa_and_WP_Ext must pin WorkPermitItem being extended

- **Phase**: lookup + import strategy
- **Why**: extend_visa_wp People & links hid Work permit (5/-1636) because RequirePersonWorkPermitItem was off. Next import must still pin PIA CurrentWorkPermit after the flag is on.
- **Locked**: import-strategy.yaml `calikExtendVisaWpWorkPermitItemLock`; application-type-import-order.yaml App_Visa_and_WP_Ext notes; IMPORT_PLAN_AND_STRATEGY.md.
- **Next**: restart for catalog sync; `--correct-visa2014-application-person-document-links` if existing local cases still have Work permit 0.

### 2026-09-11 - EPA backfill after Address/Position required on all profiles

- **Phase**: correction / roster ResolvedLinks
- **Mode**: `--correct-visa2014-application-person-document-links --epa-roster-backfill-only --legacy-source calik-energi-local-pg --inprocess --no-wait`
- **Outcome**: success (exit **0**)
- **Why**: Çakylygy üýtgetmek (App_Change_Inv / 3/-352) showed Position 0 / Address 0. First EPA backfill ran while those `RequirePerson*` flags were false on invitation-change (and other) profiles. Tiles appeared after catalog sync; links were never pinned.
- **Environment**: local PostgreSQL `visa2026`
- **Counts**: Education **0** / Address **771** / Position **1367** (employees only).
- **Log**: `artifacts/headless-import/epa-roster-backfill-change-inv-20260911.log`
- **Next**: refresh 3/-352. Address/Position 0 only if that person has no imported child row (or FM Position uses sponsor caption). Halt.
- **Strategy (2026-09-11):** this case was **not** in the import gate until now. Locked in `import-strategy.yaml` (`calikAddressPositionEpaLock`), `order.yaml` `epa-roster-backfill`, `application-type-import-order.yaml` (document-links + App_Change_Inv), `IMPORT_PLAN_AND_STRATEGY.md` wave 4. Next Demo/Prod/full import must run `--epa-roster-backfill-only` after document-links and re-run EPA if `RequirePerson*` flags changed.
### 2026-09-09 - TravelHistory officer verified local PG

- **Phase**: person-domain / TravelHistory
- **Outcome**: officer confirmed fixed (Registration case chips after SQL pin)
- **Environment**: local PostgreSQL `visa2026`
- **Next**: Halt. Demo/Prod TravelHistory not run.
### 2026-09-09 - TravelHistory pin: Registration SQL backfill, not all PIA

- **Phase**: correction / roster ResolvedLinks
- **Mode**: --correct-visa2014-application-person-document-links --travel-roster-backfill-only --legacy-source calik-energi-local-pg --inprocess --no-wait
- **Outcome**: success (exit **0**)
- **Why**: PIA checkpoint / RegistrationDate / travel FKs exist only when the Application is Registration. Walking all **22845** PIA rows for travel is wrong and slow. Stopped the full PIA pin (~7k/22845). Pin missing TravelHistory links where profile `ActionFamily = Registration` (2) and `RequirePersonTravelHistory`.
- **Environment**: local PostgreSQL `visa2026`
- **Counts**: SQL missing Registration pairs **7222** / links changed **7222**. No PIA loop.
- **Log**: `artifacts/headless-import/travel-roster-backfill-20260909.log`
- **Next**: refresh a Registration case. Chips stay 0 only when that person has no imported TravelHistory. Halt.
### 2026-09-09 - TravelHistory from PersonInApplication .15 -> local PG

- **Phase**: person-domain / TravelHistory
- **Mode**: `--import-visa2014 --entity TravelHistory --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **Why**: Case Travel history chips were 0 because travel was never imported. Grain is **PersonInApplication** (checkpoint, PurposeOfTrave, RegistrationDate, Employee/FamilyMemberEntryDate FKs), joined to `dbo.TravelInformation` for TravelDate. Not TI-only. Registration types only (App_Reg_Check_In/_Out/_Internal). Live registration TravelHistory sync stays retired.
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy SQL **22845** / Prepared **3512** / Posted **3512** / Failed **0** / no Person map **0** / already **0** / skipped **19333** (non-registration or no date) / dedupe **240**.
- **Id-map**: `id-maps/calik-energi-local-pg/TravelHistory.json` (copied bin -> source). Key = PIA.Oid; TI.Oid alias when unique.
- **Log**: `artifacts/headless-import/travel-history-20260909.log`
- **Next**: `--correct-visa2014-application-person-document-links` to pin TravelHistory when `RequirePersonTravelHistory`. Halt until proceed. Last-N expected 0 when locked and the person has no travel rows.
### 2026-09-09 - EPA roster backfill (Education/Position/Address) local PG

- **Phase**: correction / roster ResolvedLinks
- **Mode**: `--correct-visa2014-application-person-document-links --epa-roster-backfill-only --legacy-source calik-energi-local-pg --inprocess --no-wait` (no VISA2015 round-trip)
- **Outcome**: success (exit **0**)
- **Why**: Case workspace chips are `ApplicationProfileInstancePersonResolvedLink`, not Person children. Per-PIA `GetObjectsQuery` + `e.Person != null` missed co-applicants (e.g. `2/-311` Turan Birincioglu had Education/Position/Address on Person, chips 0). Slow PIA loop never finished.
- **Fix**: SQL DISTINCT ON missing pairs where profile `RequirePerson*` and the person has a live child row; `ReplaceKind` in batches of 50. Same backfill runs at end of roster import and full document-link correction.
- **Counts**: Education **4299** / Address **4729** / Position **3959**. Remaining required+has-child gaps **0**. `2/-311` both people now have LinkKind 0–4.
- **Log**: `artifacts/headless-import/epa-roster-backfill-20260909.log`
- **Next**: refresh Case workspace `2/-311`. Chips 0 remain only when that person has no Education/Position/Address row in Visa2026.
### 2026-09-08 - FamilyProofDocument file wave .15 -> local PG (attachmentsSequence)

- **Phase**: file import
- **Mode**: `--import-visa2014-files --entity Person --property FamilyProofDocument --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50` `--id-map` full `Person.json`
- **Outcome**: success (exit **0**, Failed **0**) — last wired attachmentsSequence step
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Person id-map **3409** / Legacy family-proof rows **450** / Posted PersonDocument **9** + PersonFamilyRelationDocument **437** (**446**) / Failed **0** / No person map **0** / No blob **0** / Oversize (>5MB) **1** / Duplicate blob **3** / already **0**.
- **Id-map**: `id-maps/calik-energi-local-pg/FamilyProofDocument.json` (copied bin -> source).
- **Log**: `artifacts/document-copies-import/FamilyProofDocument-20260908.log`
- **Next**: attachmentsSequence complete for local PG this wipe. Unwired (do not invent): RejectionDocument, BorderZoneDocument, MinistryLetterFile.
### 2026-09-08 - InvitationDocument file wave .15 -> local PG (attachmentsSequence)

- **Phase**: file import
- **Mode**: `--import-visa2014-files --entity Invitation --property InvitationDocument --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Invitation id-map **2941** / Legacy copy rows **3256** / Posted **3046** / Failed **0** / No parent map **205** / No blob **5** / Oversize **0** / already **0**.
- **Id-map**: `id-maps/calik-energi-local-pg/InvitationDocument.json` (copied bin -> source).
- **Note**: skipped rebuilding Blazor (`bind-Date` RZ9991 in IssueIssued* panels). `dotnet build DataImporter /p:BuildProjectReferences=false` then full DLL path.
- **Log**: `artifacts/document-copies-import/InvitationDocument-20260908.log`
- **Next**: FamilyProofDocument. Halt until proceed.
### 2026-09-08 - WorkPermitDocument file wave .15 -> local PG (attachmentsSequence)

- **Phase**: file import
- **Mode**: `--import-visa2014-files --entity WorkPermit --property WorkPermitDocument --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: WorkPermit id-map **361** / Legacy copy rows **1024** / Posted **915** / Failed **0** / No parent map **107** / No blob **2** / Oversize **0** / already **0**.
- **Id-map**: `id-maps/calik-energi-local-pg/WorkPermitDocument.json` (copied bin -> source).
- **Log**: `artifacts/document-copies-import/WorkPermitDocument-20260908.log`
- **Next**: InvitationDocument. Halt until proceed.
### 2026-09-08 - MedicalRecordDocument file wave .15 -> local PG (attachmentsSequence)

- **Phase**: file import
- **Mode**: `--import-visa2014-files --entity MedicalRecord --property MedicalRecordDocument --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50` `--id-map` full `Person.json`
- **Outcome**: success (exit **0**, Failed **0**) — Calik no-op as expected
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Person id-map **3409** / Spid link rows **2** / Importable (Copy+FileData) **0** / Posted **0** / Failed **0** / Orphan copy link **2**.
- **Note**: DataImporter bin emptied again (VS Insiders F5 `Visa2026.Blazor.Server` locked Module.dll). Stopped PID then rebuild. Do not F5 the Blazor host during remaining file waves.
- **Log**: `artifacts/document-copies-import/MedicalRecordDocument-20260908.log`
- **Next**: WorkPermitDocument. Halt until proceed.
### 2026-09-08 - EducationDocument file wave .15 -> local PG (attachmentsSequence)

- **Phase**: file import
- **Mode**: `--import-visa2014-files --entity Education --property EducationDocument --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Education id-map **3281** / Legacy diploma copy rows **4500** / Posted **4409** / Failed **0** / No education map **0** / No blob **34** / Oversize (>5MB) **40** / already **0** / Duplicate blob **17**.
- **Id-map**: `id-maps/calik-energi-local-pg/EducationDocument.json` (copied bin -> source).
- **Note**: first CLI failed (bin emptied to 9 files, `dotnet Visa2026.DataImporter.dll` not found). Rebuild Debug then `dotnet` full DLL path. Wall-clock ~10 min after host listen.
- **Log**: `artifacts/document-copies-import/EducationDocument-20260908.log`
- **Next**: MedicalRecordDocument. Halt until proceed.
### 2026-09-08 - EducationDocument CLI fail (bin wiped)

- **Phase**: file import
- **Mode**: `dotnet Visa2026.DataImporter.dll --import-visa2014-files --entity Education --property EducationDocument`
- **Outcome**: fail (exit **1**, no import)
- **Cause**: `Visa2026.DataImporter\bin\Debug\net8.0` had **9** files; DLL and `runtimeconfig.json` missing. `dotnet` treated the name as a subcommand.
- **Fix**: `dotnet build Visa2026.DataImporter -c Debug`, copy id-maps, run with full DLL path.### 2026-09-08 - VisaDocument file wave .15 -> local PG (attachmentsSequence)

- **Phase**: file import
- **Mode**: `--import-visa2014-files --entity Visa --property VisaDocument --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Visa id-map **6328** / Rows with blob **6282** / Posted **6128** / Failed **0** / No visa map **67** / No blob **46** / Oversize (>5MB) **154** / already **0**.
- **Id-map**: `id-maps/calik-energi-local-pg/VisaDocument.json` (copied bin -> source).
- **Note**: wall-clock ~17 min after host listen (more rows than PassportDocument).
- **Log**: `artifacts/document-copies-import/VisaDocument-20260908.log`
- **Next**: EducationDocument. Halt until proceed.### 2026-09-08 - PassportDocument file wave .15 -> local PG (attachmentsSequence)

- **Phase**: file import
- **Mode**: `--import-visa2014-files --entity Passport --property PassportDocument --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Passport id-map **3765** / Legacy copy rows **3800** / Posted **3749** / Failed **0** / No passport map **13** / No blob **1** / Oversize (>5MB) **37** / already **0**.
- **Id-map**: `id-maps/calik-energi-local-pg/PassportCopy.json` (copied bin -> source).
- **Note**: rebuild Debug first (`runtimeconfig.json` / `deps.json` missing after Photo run). Wall-clock ~6 min after host listen.
- **Log**: `artifacts/document-copies-import/PassportDocument-20260908.log`
- **Next**: VisaDocument. Halt until proceed.### 2026-09-08 - Person.Photo file wave .15 -> local PG (attachmentsSequence)

- **Phase**: file import
- **Mode**: `--import-visa2014-files --entity Person --property Photo --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Id-map **3409** / Processed **3409** / Patched **3338** / No blob **71** / Failed **0**.
- **Note**: rebuild Debug first (bin was missing `runtimeconfig.json`). Photo wall-clock ~4 min after host listen; blobs from `.15` then XAF PATCH FileData. Not a hang.
- **Log**: `artifacts/document-copies-import/Person-Photo-20260908.log`
- **Next**: PassportDocument. Halt until proceed.### 2026-09-08 - PIA document-link pin .15 -> local PG (after accept)

- **Phase**: correction
- **Mode**: `--correct-visa2014-application-person-document-links --legacy-source calik-energi-local-pg --application-id-map` full path `ApplicationProfileInstance.json`. `--target-connection` local PG.
- **Outcome**: success (exit **0**, errors **0**)
- **Counts**: Legacy PIA **22825**. Passport changed **0**. Visa changed **2**. WorkPermitItem changed **2**. Already correct **22262**. Missing parent id-map **258**. No mapped snapshot **188**.
- **Logs**: `artifacts/headless-import/correct-pia-document-links-20260908.log`
- **Next**: attachmentsSequence (file waves). postAllTypeSlices scalar complete.
### 2026-09-08 - Visa remainder .15 -> local PG (postAllTypeSlices, after proceed)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Visa --visa-remainder --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`. `--application-id-map` full path `ApplicationProfileInstance.json`. No `--application-type`.
- **Outcome**: success (exit **0**, Failed **0**)
- **Counts**: Legacy **6378** / Prepared **6353** / skip **19** / dedupe **6**. Posted **0** / already **6328** / no Passport map **25** / remainder without issuing **0** / issuing patched **0**.
- **PG**: `Visas` **6328**. Type-slice Visa waves already posted every remainder candidate except 25 without Passport id-map.
- **Id-maps**: Visa **6328**.
- **Logs**: `artifacts/headless-import/Visa-remainder-20260908.log`
- **Halt**: wait for accept before `--correct-visa2014-application-person-document-links` (pins PIA Passport/PreviousPassport/Visa/WorkPermit).
### 2026-09-08 - BorderZone + BorderZoneItem remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity BorderZone` then `BorderZoneItem --border-zone-id-map` full path `BorderZone.json`. `--application-id-map` full path `ApplicationProfileInstance.json`. `--legacy-source calik-energi-local-pg --inprocess`. No BorderZoneDocument.
- **Outcome**: success (exit **0**, Failed **0** both)
- **Header**: Posted **108** / already **2** / prepared **110** / transform skip **1**. PG `BorderZones` **110**. Instance FK null **0**. ValidityDuration FK null **0**.
- **Items**: Posted **207** / already **2** / prepared **212** / transform skip **6** / missing required id-map **3** (Passport, same as 2026-09-07). PG `BorderZoneItems` **209**. Person/Passport/BorderZone FK null **0**.
- **Id-maps**: BorderZone **110**, BorderZoneItem **209**.
- **Logs**: `artifacts/headless-import/BorderZone-header-remainder-20260908.log`, `BorderZoneItem-remainder-20260908.log`
- **Next**: `--visa-remainder` (no `--application-type`), then `--correct-visa2014-application-person-document-links`.
### 2026-09-08 - BorderZone + BorderZoneItem sample 2 .15 -> local PG (postAllTypeSlices)

- **Phase**: import (sample)
- **Mode**: `--entity BorderZone --application-id-map` full path `ApplicationProfileInstance.json`. First `--max-rows 2` Posted **1** (1 junk skip in TOP). Then `--max-rows 3` Posted **1** more. Then `--entity BorderZoneItem --border-zone-id-map` full path `BorderZone.sample2.json`. No BorderZoneDocument.
- **Outcome**: headers Posted **2** Failed **0**. Items Posted **2** Failed **0** (missing parent map 210 of 212 prepared; transform skip 6).
- **Headers**: `AS468709` start 2014-08-26 app `8/-3585` Fatih Akgollu U 00368984. `AS473641` start 2014-09-26 app `9/-3771` Levent Ozgur TOPAL U 06197890. Profile get_border_zone. PeriodDays 182 → ValidityDuration 180 (6 ay) closest Match. Employees.
- **Halt**: wait for accept before BorderZone remainder, then BorderZoneItem remainder. Remainder may miss 3 Passport id-maps (same as 2026-09-07).
- **Id-maps**: BorderZone **2**. Sample `BorderZone.sample2.json`.
- **Logs**: `artifacts/headless-import/BorderZone-header-sample2-20260908.log`, `BorderZone-header-sample2b-20260908.log`, `BorderZoneItem-sample2-20260908.log`
### 2026-09-08 - Rejection + RejectionItem remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Rejection` then `RejectionItem --rejection-id-map` full path `Rejection.json`. `--legacy-source calik-energi-local-pg --inprocess`. `--application-id-map` full path `ApplicationProfileInstance.json`. No RejectionDocument.
- **Outcome**: success (exit **0**, Failed **0** both)
- **Header**: Posted **205** / already **2** / prepared **207**. PG `Rejections` **207**. Instance FK null **0**.
- **Items**: Posted **252** / already **2** / prepared **254**. PG `RejectionItems` **254**. Person/Passport/Rejection FK null **0**.
- **Id-maps**: Rejection **207**, RejectionItem **254**.
- **Logs**: `artifacts/headless-import/Rejection-header-remainder-20260908.log`, `RejectionItem-remainder-20260908.log`
- **Next**: BorderZone documents sample (header then items). Do not invent BorderZoneDocument. Then `--visa-remainder`, then document-link pin.
### 2026-09-08 - Rejection + RejectionItem sample 2 .15 -> local PG (postAllTypeSlices)

- **Phase**: import (sample)
- **Mode**: `--entity Rejection --max-rows 2 --application-id-map` full path `ApplicationProfileInstance.json`, then `--entity RejectionItem --rejection-id-map` full path `Rejection.sample2.json`. `--legacy-source calik-energi-local-pg --inprocess`. No RejectionDocument.
- **Outcome**: header Posted **2** Failed **0**. Items Posted **2** Failed **0** (missing parent map 252 of 254).
- **Headers**: `101/2-819` date 2014-03-24 app `3/-2435` profile get_invitation_wp. `AS447001` date 2014-04-07 app `3/-2493` profile get_invitation. Employees Hakan YORUK U 08805860 / Ramazan MAMAK U 08743877. Number, date, app number, person, passport Match.
- **Halt**: wait for accept before Rejection remainder, then RejectionItem remainder.
- **Id-maps**: Rejection **2**. Sample `Rejection.sample2.json`.
- **Logs**: `artifacts/headless-import/Rejection-header-sample2-20260908.log`, `RejectionItem-sample2-20260908.log`
### 2026-09-08 - App_Business_Trip_Departure remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance|Person|Progress --application-type App_Business_Trip_Departure --legacy-source calik-energi-local-pg --inprocess`. `--application-id-map` full path `ApplicationProfileInstance.json`. Progress `--batch-size 1`. Skip Invitation, WorkPermit, Visa. No `--visa-remainder`.
- **Outcome**: success (exit **0**, Failed **0** all three)
- **Header**: Posted **592** / already **2** / type **594**. PG `business_trip_departure` headers **594**.
- **Roster**: Posted **727** / already **21838** / missing parent **52**.
- **Progress**: Posted **1162** / already **37701** / no instance map **70**.
- **Id-maps**: instance **12575**, person **22589**, progress **38868**.
- **Logs**: `artifacts/headless-import/AppBizTripDep-header-remainder-20260908.log`, roster/progress `AppBizTripDep-*-remainder-20260908.log`
- **Next**: skip empty App_Business_Trip_Arrival. Then postAllTypeSlices: Rejection, RejectionItem, BorderZone documents, `--visa-remainder`, `--correct-visa2014-application-person-document-links`. Do not invent RejectionDocument/BorderZoneDocument.
### 2026-09-08 - App_Business_Trip_Departure header+roster+progress sample 2 .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Business_Trip_Departure --max-rows 2` then roster+progress with `--application-id-map` full path `ApplicationProfileInstance.sample2-btdep.json`. Progress `--batch-size 1`. Skip Invitation, WorkPermit, Visa. No `--visa-remainder`.
- **Outcome**: header Posted **2** Failed **0** (594 type matches). Roster Posted **4**. Progress Posted **4**.
- **Apps**: `2/-221` Bora Yolcu U31669065 visa A1548869 (employee, 2025-02-24). `4/-12929` three employees Burak BILGIN / Peter Anthony TERRIO / Todor Ivanov MIRKOV with matching passports and visas A1338445 / A1338267 / A1338268 (2019-04-15). Profile business_trip_departure. Cancelled=0. Roster people Match (Employee, not FamilyMember).
- **Halt**: wait for accept before header remainder, then roster, progress.
- **Id-maps**: instance **11983**. Sample `ApplicationProfileInstance.sample2-btdep.json`.
- **Logs**: `artifacts/headless-import/AppBizTripDep-header-sample2-20260908.log`, roster/progress `AppBizTripDep-*-sample2-20260908.log`
### 2026-09-08 - App_Reg_Info_Change_Passport remainder .15 -> local PG (after proceed)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance|Person|Progress --application-type App_Reg_Info_Change_Passport --legacy-source calik-energi-local-pg --inprocess`. `--application-id-map` full path `ApplicationProfileInstance.json`. Progress `--batch-size 1`. Skip Invitation, WorkPermit, Visa. No `--visa-remainder`.
- **Outcome**: success (exit **0**, Failed **0** all three). First header attempt failed (missing `DevExpress.Office.v25.2.Core`); rebuilt DataImporter Debug, recopied id-maps, retried.
- **Header**: Posted **99** / already **2** / type **101**.
- **Roster**: Posted **114** / already **21720** / missing parent **782**.
- **Progress**: Posted **198** / already **37499** / no instance map **1232**.
- **Id-maps**: instance **11981**, person **21858**, progress **37702**.
- **Logs**: `artifacts/headless-import/AppRegInfoPp-header-remainder-20260908.log`, roster/progress `AppRegInfoPp-*-remainder-20260908.log`
- **Next**: skip empty App_Reg_Info_Change_Visa and App_Reg_Check_Out_Internal. Then App_Business_Trip_Departure sample. Skip Invitation, WorkPermit, Visa.
### 2026-09-08 - App_Reg_Info_Change_Passport header+roster+progress sample 2 .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Reg_Info_Change_Passport --max-rows 2` then roster+progress with `--application-id-map` full path `ApplicationProfileInstance.sample2-regpp.json`. Progress `--batch-size 1`. Skip Invitation, WorkPermit, Visa. No `--visa-remainder`.
- **Outcome**: header Posted **2** Failed **0** (101 type matches). Roster Posted **2**. Progress Posted **4**.
- **Apps**: `1/-20` Mumin Abbaz U35307900 visa A1458728 (employee, 2023-01-17). `9/-3969` Anil Yilmaz GUNESLI U 09663605 visa A0893738 (2014-09-24). PreviousPassport empty both PIAs. Profile reg_info_change_passport. Cancelled=0. Passports+visas Match.
- **Halt**: wait for accept before header remainder, then roster, progress.
- **Id-maps**: instance **11882**. Sample `ApplicationProfileInstance.sample2-regpp.json`.
- **Logs**: `artifacts/headless-import/AppRegInfoPp-header-sample2-20260908.log`, roster/progress `AppRegInfoPp-*-sample2-20260908.log`
### 2026-09-08 - App_Reg_Info_Change_Address remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance|Person|Progress --application-type App_Reg_Info_Change_Address --legacy-source calik-energi-local-pg --inprocess`. `--application-id-map` full path `ApplicationProfileInstance.json`. Progress `--batch-size 1`. Skip Invitation, WorkPermit, Visa. No `--visa-remainder`.
- **Outcome**: success (exit **0**, Failed **0** all three)
- **Header**: Posted **645** / already **2** / type **647**. PG `reg_info_change_address` headers **647**.
- **Roster**: Posted **828** / already **20890** / missing parent **898**.
- **Progress**: Posted **1290** / already **36205** / no instance map **1434**.
- **Id-maps**: instance **11880**, person **21742**, progress **37500**.
- **Logs**: `artifacts/headless-import/AppRegInfoAddr-header-remainder-20260908.log`, roster/progress `AppRegInfoAddr-*-remainder-20260908.log`
- **Next**: App_Reg_Info_Change_Passport sample. Skip Invitation, WorkPermit, Visa.
### 2026-09-08 - App_Reg_Info_Change_Address header+roster+progress sample 2 .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Reg_Info_Change_Address --max-rows 2` then roster+progress with `--application-id-map` full path `ApplicationProfileInstance.sample2-regaddr.json`. Progress `--batch-size 1`. Skip Invitation, WorkPermit, Visa. No `--visa-remainder`.
- **Outcome**: header Posted **2** Failed **0** (647 type matches). Roster Posted **2**. Progress Posted **4**.
- **Apps**: `1/-7127` Metin AKYOL U 02169790 visa A0981650 (employee, 2016-01-19). `5/-7997` Ersoy KETENE U 06171393 visa A1046630 (2016-06-08). Profile reg_info_change_address. Cancelled=0. Passports+visas Match.
- **Halt**: wait for accept before header remainder, then roster, progress.
- **Id-maps**: instance **11235**. Sample `ApplicationProfileInstance.sample2-regaddr.json`.
- **Logs**: `artifacts/headless-import/AppRegInfoAddr-header-sample2-20260908.log`, roster/progress `AppRegInfoAddr-*-sample2-20260908.log`
### 2026-09-08 - App_Reg_Check_Out remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance|Person|Progress --application-type App_Reg_Check_Out --legacy-source calik-energi-local-pg --inprocess`. `--application-id-map` full path `ApplicationProfileInstance.json`. Progress `--batch-size 1`. Skip Invitation, WorkPermit, Visa. No `--visa-remainder`.
- **Outcome**: success (exit **0**, Failed **0** all three)
- **Header**: Posted **2205** / already **2** / type **2207**. PG `check_out` headers **2207**.
- **Roster**: Posted **3430** / already **17458** / missing parent **1728**.
- **Progress**: Posted **4326** / already **31875** / no instance map **2728**.
- **Id-maps**: instance **11233**, person **20912**, progress **36206**.
- **Logs**: `artifacts/headless-import/AppRegCheckOut-header-remainder-20260908.log`, roster/progress `AppRegCheckOut-*-remainder-20260908.log`
- **Next**: App_Reg_Info_Change_Address sample. Skip Invitation, WorkPermit, Visa.
### 2026-09-08 - App_Reg_Check_Out header+roster+progress sample 2 .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Reg_Check_Out --max-rows 2` then roster+progress with `--application-id-map` full path `ApplicationProfileInstance.sample2-regcheckout.json`. Progress `--batch-size 1`. Skip Invitation, WorkPermit, Visa. No `--visa-remainder`.
- **Outcome**: header Posted **2** Failed **0** (2207 type matches). Roster Posted **2**. Progress Posted **4**.
- **Apps**: `3/-201` Fuat Kelesoglu U34387202 visa A1601731 (employee, 2025-03-19). `9/-8752` Sulek UYSAL U 11110738 visa A1089789 (2016-09-30). Profile check_out. Cancelled=0. Passports+visas Match.
- **Halt**: wait for accept before header remainder, then roster, progress.
- **Id-maps**: instance **9028**. Sample `ApplicationProfileInstance.sample2-regcheckout.json`.
- **Logs**: `artifacts/headless-import/AppRegCheckOut-header-sample2-20260908.log`, roster/progress `AppRegCheckOut-*-sample2-20260908.log`
### 2026-09-08 - App_Reg_ext remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance|Person|Progress --application-type App_Reg_ext --legacy-source calik-energi-local-pg --inprocess`. `--application-id-map` full path `ApplicationProfileInstance.json`. Progress `--batch-size 1`. Skip Invitation, WorkPermit, Visa. No `--visa-remainder`.
- **Outcome**: success (exit **0**, Failed **0** all three)
- **Header**: Posted **1241** / already **2** / type **1243**. PG `reg_extension` headers **1243**.
- **Roster**: Posted **2653** / already **14803** / missing parent **5160**.
- **Progress**: Posted **2478** / already **29393** / no instance map **7058**.
- **Id-maps**: instance **9026**, person **17480**, progress **31876**.
- **Logs**: `artifacts/headless-import/AppRegExt-header-remainder-20260908.log`, roster/progress `AppRegExt-*-remainder-20260908.log`
- **Next**: App_Reg_Check_Out sample. Skip Invitation, WorkPermit, Visa.
### 2026-09-08 - App_Reg_ext header+roster+progress sample 2 .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Reg_ext --max-rows 2` then roster+progress with `--application-id-map` full path `ApplicationProfileInstance.sample2-regext.json`. Progress `--batch-size 1`. Skip Invitation, WorkPermit, Visa. No `--visa-remainder`.
- **Outcome**: header Posted **2** Failed **0** (1243 type matches). Roster Posted **3**. Progress Posted **4**.
- **Apps**: `3/-376` Zekai Akurek U26213934 visa A1675436 + Abdul Ahad Raufi PO5427481 visa A1675435 (employee, 2026-03-04). `7/-1057` Selcuk Keles U24888382 visa A1634423 (2025-07-26). Profile reg_extension. Cancelled=0. Passports+visas Match.
- **Halt**: wait for accept before header remainder, then roster, progress.
- **Id-maps**: instance **7785**. Sample `ApplicationProfileInstance.sample2-regext.json`.
- **Logs**: `artifacts/headless-import/AppRegExt-header-sample2-20260908.log`, roster/progress `AppRegExt-*-sample2-20260908.log`
### 2026-09-08 - App_Reg_Check_In_Internal remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance|Person|Progress --application-type App_Reg_Check_In_Internal --legacy-source calik-energi-local-pg --inprocess`. `--application-id-map` full path `ApplicationProfileInstance.json`. Progress `--batch-size 1`. Skip Invitation, WorkPermit, Visa. No `--visa-remainder`.
- **Outcome**: success (exit **0**, Failed **0** all three)
- **Header**: Posted **488** / already **2** / type **490**. PG `check_in_internal` headers **490**.
- **Roster**: Posted **609** / already **14191** / missing parent **7816**.
- **Progress**: Posted **976** / already **28413** / no instance map **9540**.
- **Id-maps**: instance **7783**, person **14824**, progress **29394**.
- **Logs**: `artifacts/headless-import/AppRegCheckInInt-header-remainder-20260908.log`, roster/progress `AppRegCheckInInt-*-remainder-20260908.log`
- **Next**: App_Reg_ext sample. Skip Invitation, WorkPermit, Visa.
### 2026-09-08 - App_Reg_Check_In_Internal header+roster+progress sample 2 .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Reg_Check_In_Internal --max-rows 2` then roster+progress with `--application-id-map` full path `ApplicationProfileInstance.sample2-reginternal.json`. Progress `--batch-size 1`. Skip Invitation, WorkPermit, Visa. No `--visa-remainder`.
- **Outcome**: header Posted **2** Failed **0** (490 type matches). Roster Posted **2**. Progress Posted **4**.
- **Apps**: `1/-9339` family 2017-01-18: PIA Employee Ahmet Murat Karaalp + FamilyMember Tetiana Karaalp FA917373 visa A1147551; roster person Tetiana (ForFamilyMember). `4/-5586` Omer BINARBASI U 07720860 visa A0929144 (employee, 2015-04-30). Passports+visas Match. Profile check_in_internal. Cancelled=0.
- **Halt**: wait for accept before header remainder, then roster, progress.
- **Id-maps**: instance **7295**. Sample `ApplicationProfileInstance.sample2-reginternal.json`.
- **Logs**: `artifacts/headless-import/AppRegCheckInInt-header-sample2-20260908.log`, roster/progress `AppRegCheckInInt-*-sample2-20260908.log`
### 2026-09-08 - App_Reg_Check_In remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance|Person|Progress --application-type App_Reg_Check_In --legacy-source calik-energi-local-pg --inprocess`. `--application-id-map` full path `ApplicationProfileInstance.json`. Progress `--batch-size 1`. Skip Invitation, WorkPermit, Visa. No `--visa-remainder`.
- **Outcome**: success (exit **0**, Failed **0** all three)
- **Header**: Posted **1814** / already **2** / type **1816**. PG `check_in_from_abroad` headers **1816**.
- **Roster**: Posted **3135** / already **11054** / missing parent **8427**.
- **Progress**: Posted **3608** / already **24801** / no instance map **10520**.
- **Id-maps**: instance **7293**, person **14213**, progress **28414**.
- **Logs**: `artifacts/headless-import/AppRegCheckIn-header-remainder-20260908.log`, roster/progress `AppRegCheckIn-*-remainder-20260908.log`
- **Next**: App_Reg_Check_In_Internal sample. Skip Invitation, WorkPermit, Visa.
### 2026-09-08 - App_Reg_Check_In header+roster+progress sample 2 .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Reg_Check_In --max-rows 2` then roster+progress with `--application-id-map` full path `ApplicationProfileInstance.sample2-regcheckin.json`. Progress `--batch-size 1`. Skip Invitation, WorkPermit, Visa. No `--visa-remainder`.
- **Outcome**: header Posted **2** Failed **0** (1816 type matches). Roster Posted **2**. Progress Posted **4**.
- **Apps**: `4/-547` Gulladi Ramachandra Raghavendra --- Z4675820 visa A1704067 (employee, 2026-04-03, check_in_from_abroad). `2/-2348` Baris CAMCI U 04947836 visa A0820054 (2014-02-26). Passports+visas Match. Profile Match. Cancelled=0. ProcessNumber empty both sides.
- **Halt**: wait for accept before header remainder, then roster, progress. Skip Invitation, WorkPermit, Visa.
- **Id-maps**: instance **5479**. Sample `ApplicationProfileInstance.sample2-regcheckin.json`.
- **Logs**: `artifacts/headless-import/AppRegCheckIn-header-sample2-20260908.log`, roster/progress `AppRegCheckIn-*-sample2-20260908.log`
### 2026-09-08 - App_Sevice_Passport remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance|Person|Progress|Invitation|InvitationItem|Visa --application-type App_Sevice_Passport --legacy-source calik-energi-local-pg --inprocess`. `--application-id-map` full path `ApplicationProfileInstance.json`. Progress `--batch-size 1`. Skip WorkPermit. No `--visa-remainder`.
- **Outcome**: success (exit **0**, Failed **0** all six)
- **Header**: Posted **42** / already **2** / type **44**. PG `get_invitation_service_passport` headers **44**.
- **Roster**: Posted **50** / already **11002** / missing parent **11564**.
- **Progress**: Posted **200** / already **24597** / no instance map **14132**.
- **Invitation**: Posted **32** / already **2909** / instance not in map **2**.
- **InvitationItem**: Posted **39** / already **5236** / missing id-map **30**.
- **Visa**: Posted **0** / already **6328** / no passport **25** / issuing FK patched **9**.
- **Id-maps**: instance **5477**, person **11076**, progress **24802**, Invitation **2941**, InvitationItem **5275**.
- **Logs**: `artifacts/headless-import/AppSevicePp-header-remainder-20260908.log`, roster/progress/inv/invitem/visa `AppSevicePp-*-remainder-20260908.log`
- **Next**: App_Reg_Check_In sample (skip Invitation, WorkPermit, Visa). This wipe has not run registration.
### 2026-09-08 - App_Sevice_Passport header+roster+progress+Invitation sample 2 .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Sevice_Passport --max-rows 2` then roster+progress+Invitation+InvitationItem+Visa with `--application-id-map` full path `ApplicationProfileInstance.sample2-sevice.json`. Progress `--batch-size 1`. Skip WorkPermit. No `--visa-remainder`.
- **Outcome**: header Posted **2** Failed **0** (44 type matches). Roster Posted **2**. Progress Posted **9**. Invitation Posted **2**. InvitationItem Posted **2**. Visa Posted **0** / already **6328** (PIA Visa empty both apps).
- **Apps**: `11/-13247` Hikmet SEZER S 03046413 invitation AS0053302 2019-11-21..2020-02-21 (employee, get_invitation_service_passport). `8/-1528` Burak Yuksel S36243438 invitation CO0230857 2026-08-31..2026-11-27. ProcessNumber matches invitation Number. Cancelled=0. No previous passport.
- **Halt**: wait for accept before header remainder, then roster, progress, Invitation, InvitationItem, Visa. Skip WorkPermit. Do not use `--visa-remainder` on this type.
- **Id-maps**: instance **5435**. Sample `ApplicationProfileInstance.sample2-sevice.json`.
- **Logs**: `artifacts/headless-import/AppSevicePp-header-sample2-20260908.log`, roster/progress/inv/invitem/visa `AppSevicePp-*-sample2-20260908.log`
### 2026-09-08 - App_Cancell_WP remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance|Person|Progress --application-type App_Cancell_WP --legacy-source calik-energi-local-pg --inprocess`. `--application-id-map` full path to `ApplicationProfileInstance.json`. Progress `--batch-size 1`. Skip Invitation, WorkPermit, Visa. No `--visa-remainder`.
- **Outcome**: success (exit **0**, Failed **0** all three)
- **Header**: Posted **87** / already **2** / type **89**. PG `cancel_workpermit` headers **89**.
- **Roster**: Posted **194** / already **10806** / missing parent **11615**.
- **Progress**: Posted **174** / already **24414** / no instance map **14341**.
- **Id-maps**: instance **5433**, person **11024**, progress **24593**.
- **Logs**: `artifacts/headless-import/AppCancellWp-header-remainder-20260908.log`, roster/progress `AppCancellWp-*-remainder-20260908.log`
- **Next**: App_Cancel_BZ already skip. This wipe has not run band 5+; next live type **App_Sevice_Passport** (invitation+visa; skip WorkPermit). Wait for lock/sample.
### 2026-09-08 - Strategy: imported applications are past (not today)

- **Phase**: mapping / strategy
- **Why**: App_Cancell_WP sample 4/-9661 linked WorkPermitItem 1757/29 (today's current) instead of PIA 5014/1. All VISA2014 data is historical.
- **Locked**: `import-strategy.yaml` `historicalApplicationSnapshot` (past_not_today). Roster ResolvedLinks = PersonInApplication snapshot (Passport/PreviousPassport/Visa/WorkPermitItem). Skip latest-N / PersonCurrentItems during IsDataImport. Pin at roster; `--correct-visa2014-application-person-document-links` also pins WorkPermitItem. Officer Relink may still use today. Gap only when source Oid not in id-map.
- **Code**: CollectMissingAutoLinks empty during import; PinDocumentSnapshot WP; ExistingItemLinkCorrection ReplaceKind; tombstone restore on unique index.
- **Correction**: exit 0. WorkPermitItem links changed **523**. 4/-9661 should now be 5014/1.
- **Halt**: App_Cancell_WP remainder still waiting for accept.
### 2026-09-08 - App_Cancell_WP header+roster+progress sample 2 .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Cancell_WP --max-rows 2` then roster+progress with `--application-id-map` full path `ApplicationProfileInstance.sample2-cancelwp.json`. Progress `--batch-size 1`. Skip Invitation, WorkPermit, Visa. No `--visa-remainder`.
- **Outcome**: header Posted **2** Failed **0** (88 type matches). Roster Posted **3**. Progress Posted **4**.
- **Apps**: `3/-5341` Turgut MAHMUTOGLU U 04083490 WP 326/12 + Omer Akif KOPUZ U 01182745 WP 326/11 (employee, 2015-03-24). Passports+WP Match. `4/-9661` Graham Buchanan FULTON 801314039: PIA WP **5014/1** vs ResolvedLink WorkPermitItem **1757/29** Mismatch (PinDocumentSnapshot is Passport/Visa only; WP is latest-N auto-link). PROCESS_ISSUED Description **5014/1** Match vs PIA. Profile cancel_workpermit. Cancelled=0. Visa empty both sides.
- **Halt**: wait for accept before header remainder, then roster, progress.
- **Id-maps**: instance **5346**. Sample `ApplicationProfileInstance.sample2-cancelwp.json`.
- **Logs**: `artifacts/headless-import/AppCancellWp-header-sample2-20260908-110739.log`, roster/progress `AppCancellWp-*-sample2-20260908.log`
### 2026-09-08 - Family-app roster compare: use FamilyMember, not COALESCE(Employee)

- **Phase**: mapping (sample verify)
- **Why**: App_Cancel_Visa 8/-1609 looked like Person Mismatch (Hüseyin vs Elzem/Aysel). False alarm.
- **Fact**: ForFamilyMember=1. Each PIA has Employee=Hüseyin (sponsor) and FamilyMember=Elzem Ayza / Aysel. Importer ResolvePersonOid uses FamilyMember when ForFamilyMember. COALESCE(Employee, FamilyMember) prefers the sponsor and hides the actual roster people.
- **Passports/visas**: already matched; names Match when FamilyMember is used.
### 2026-09-08 - App_Cancel_Visa remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance|Person|Progress --application-type App_Cancel_Visa --legacy-source calik-energi-local-pg --inprocess`. Progress `--batch-size 1`. Skip Invitation, WorkPermit, Visa. No `--visa-remainder`.
- **Outcome**: success (exit **0**, Failed **0** all three)
- **Header**: Posted **54** / already **2** / type **56**. PG headers **56**. Id-map instance **5344**, person **10827**, progress **24415**.
- **Roster**: Posted **115** / already **10688** / missing parent **11811**.
- **Progress**: Posted **108** / already **24302** / no instance map **14517**.
- **Logs**: `artifacts/headless-import/AppCancelVisa-header-remainder-20260908-110000.log`, roster/progress `AppCancelVisa-*-remainder-20260908.log`
- **Next**: App_Cancell_WP (`cancel_workpermit`) sample 2. Skip Invitation, WorkPermit, Visa.
### 2026-09-08 - App_Cancel_Visa header+roster+progress sample 2 .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Cancel_Visa --max-rows 2` then roster+progress with `--application-id-map` full path `ApplicationProfileInstance.sample2-cancelvisa.json`. Progress `--batch-size 1`. Skip Invitation, WorkPermit, Visa. No `--visa-remainder`.
- **Outcome**: header Posted **2** Failed **0** (56 type matches). Roster Posted **3**. Progress Posted **4**.
- **Apps**: `8/-1609` family 2026-08-31 `cancel_visa` / Wizany Yatyrmak, Cancelled=0. Legacy PIA both Person Hüseyin Kelebek (same Oid) passports U33420085 / U38044059 visas A1733700 / A1733699. PG roster resolved those passports to Elzem Ayza Kelebek and Aysel Kelebek (Person names Mismatch vs PIA; passport+visa numbers Match). `4/-9675` Alexandre DE TROIA passport 14DI07449 visa A1151045 (2017-01-09, AS578955) Match.
- **Halt**: wait for accept before header remainder, then roster, progress.
- **Id-maps**: instance **5290**. Sample `ApplicationProfileInstance.sample2-cancelvisa.json`.
- **Logs**: `artifacts/headless-import/AppCancelVisa-header-sample2.log` (import `20260908-105535`), roster/progress `AppCancelVisa-*-sample2.log`
### 2026-09-08 - App_Cancel_Visa_and_WP remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance|Person|Progress --application-type App_Cancel_Visa_and_WP --legacy-source calik-energi-local-pg --inprocess`. Progress `--batch-size 1`. Skip Invitation, WorkPermit, Visa. No `--visa-remainder`. Rebuilt DataImporter.exe first.
- **Outcome**: success (exit **0**, Failed **0** all three)
- **Header**: Posted **326** / already **2** / type **328**. PG headers **328**. Id-map **5288**.
- **Roster**: Posted **620** / already **10068** / missing parent **11926**.
- **Progress**: Posted **652** / already **23650** / no instance map **14625**.
- **Logs**: `artifacts/headless-import/AppCancelVisaWp-header-remainder-20260908-105132.log`, roster/progress `AppCancelVisaWp-*-remainder-20260908.log`
- **Next**: skip App_Cancel_Inv_WP, App_Cancel_App, App_Cancel_Visa_Ext, App_Cancel_Visa_and_WP_Ext, App_Cancel_Inv. Then App_Cancel_Visa sample.
### 2026-09-08 - App_Cancel_Visa_and_WP header+roster+progress sample 2 .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Cancel_Visa_and_WP --max-rows 2` then roster+progress with `--application-id-map ApplicationProfileInstance.sample2-cancelvisawp.json`. Progress `--batch-size 1`. Skip Invitation, WorkPermit, Visa. No `--visa-remainder`.
- **Outcome**: header Posted **2** Failed **0** (328 type matches). Roster Posted **2**. Progress Posted **4**.
- **Apps**: `6/-453` Mehmet Sahan Eken U32484504 visa A1511832 WP 277/3 (CO0123158). `12/-1122` Marko Lisicar 087394531 visa A1567709 WP 764/5 (CO0135802). Profile cancel_visa_wp. Application.Cancelled=0; PROCESS_ISSUED notes match WP AppruvalNumber.
- **Halt**: wait for accept before header remainder, then roster, progress.
- **Id-maps**: instance **4962**. Sample `ApplicationProfileInstance.sample2-cancelvisawp.json`.
- **Logs**: `artifacts/headless-import/AppCancelVisaWp-header-sample2-20260908-103748.log`, roster/progress `AppCancelVisaWp-*-sample2-20260908.log`
### 2026-09-08 - App_Change_Passport remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance|Person|Progress|Visa --application-type App_Change_Passport --legacy-source calik-energi-local-pg --inprocess`. Progress `--batch-size 1`. Skip Invitation and WorkPermit. No `--visa-remainder`. Rebuilt DataImporter.exe first (bin cleaned).
- **Outcome**: success (exit **0**, Failed **0** all four)
- **Header**: Posted **120** / already **2** / type **122**. PG headers **122**. Id-map **4960**.
- **Roster**: Posted **144** / already **9922** / missing parent **12548**.
- **Progress**: Posted **240** / already **23406** / no instance map **15281**.
- **Visa**: Posted **0** / already **6328** / no passport **25** / issuing FK patched **132**.
- **Logs**: `artifacts/headless-import/AppChangePp-header-remainder-20260908-102711.log`, roster/progress/visa `AppChangePp-*-remainder-20260908*.log`
- **Next**: band 4 App_Cancel_Visa_and_WP sample (skip Invitation, WorkPermit, Visa).
### 2026-09-08 - App_Change_Passport sample comparison: include previous + new passport

- **Phase**: mapping (sample verify)
- **Why**: Reviewer could not see previous vs new passport on the Field | Legacy | Imported tables.
- **Source**: PersonInApplication.PreviousPassport vs Passport. Imported as two ResolvedLinks LinkKind=Passport (previous first, then current).
- **2/-291**: previous U 14404315 (2017-04-10..2027-04-10) / new U39222006 (2026-01-13..2036-01-12) Match.
- **3/-11580**: previous U 13336055 (2016-10-25..2018-10-25) / new U 15980186 (2018-03-05..2028-03-04) Match.
- **Halt**: still waiting for accept before App_Change_Passport remainder.
### 2026-09-08 - App_Change_Passport header+roster+progress+Visa sample 2 .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Change_Passport --max-rows 2` then roster+progress+Visa with `--application-id-map ApplicationProfileInstance.sample2-changepp.json`. Progress `--batch-size 1`. Skip Invitation and WorkPermit. No `--visa-remainder`. Rebuilt DataImporter.exe (bin cleaned).
- **Outcome**: header Posted **2** Failed **0** (122 type matches). Roster Posted **2**. Progress Posted **4**. Visa Posted **0** / already **6328** / issuing FK patched **2**.
- **Apps**: `2/-291` Ismet Danis U39222006 (2026-02-19, pasport_change, AS0205257, visa A1675261 2026-02-20). `3/-11580` Fatih DANIS U 15980186 (2018-03-29, AS631968, visa A1247287 2018-04-02). Issuing via Visa.ASNumber = Application.ProcessNumber overlay.
- **Halt**: wait for accept before App_Change_Passport header remainder, then roster, progress, Visa.
- **Id-maps**: instance **4840**. Sample `ApplicationProfileInstance.sample2-changepp.json`.
- **Logs**: `artifacts/headless-import/AppChangePp-header-sample2-20260908-101512.log`, roster/progress/visa `AppChangePp-*-sample2-20260908.log`
### 2026-09-08 - App_Change_Visa_Category skip probe (this wipe)

- **Phase**: import (probe)
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Change_Visa_Category --max-rows 2`
- **Outcome**: **0** prepared matches, Posted **0** Failed **0**. Confirms yaml skip (no enum-8 rows).
### 2026-09-08 - App_Change_Inv remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance|Person|Progress|Invitation|InvitationItem|Visa --application-type App_Change_Inv --legacy-source calik-energi-local-pg --inprocess`. Progress `--batch-size 1`. Skip WorkPermit. No `--visa-remainder`.
- **Outcome**: success (exit **0**, Failed **0** all six)
- **Header**: Posted **55** / already **2** / type **57**. PG headers **57**. Id-map **4838**.
- **Roster**: Posted **55** / already **9870** / missing parent **12689**. Id-map **9944**.
- **Progress**: Posted **110** / already **23297** / no instance map **15525**. Id-map **23407**.
- **Invitation**: Posted **53** / already **2854** / instance not in map **36**.
- **InvitationItem**: Posted **53** / already **5181** / missing id-map **71** (includes sample `10/-6548` passport not in Passport map).
- **Visa**: Posted **3** / already **6325** / no passport **25** / issuing FK patched **47**.
- **Logs**: `artifacts/headless-import/AppChangeInv-header-remainder-20260908-100237.log`, roster/progress/inv/invitem/visa `AppChangeInv-*-remainder-20260908*.log`
- **Next**: skip App_Change_Visa_Category (0 enum-8). Then App_Change_Passport sample.
### 2026-09-08 - App_Change_Inv header+roster+progress+Invitation sample 2 .15 -> local PG

- **Phase**: import (sample)
- **Why**: After Additional WP remainder, yaml empty skips then band 3 (this wipe had 0 change_invitation / pasport_change / cancel headers). Not cancel first.
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Change_Inv --max-rows 2` then roster+progress+Invitation+InvitationItem+Visa with `--application-id-map ApplicationProfileInstance.sample2-changeinv.json` (full path). Progress `--batch-size 1`. Skip WorkPermit. No `--visa-remainder`.
- **Outcome**: header Posted **2** Failed **0** (57 type matches). Roster Posted **2**. Progress Posted **4**. Invitation Posted **2**. InvitationItem Posted **1**. Visa Posted **0** / already **6325** / issuing not in sample map **3**.
- **Apps**: `8/-1088` Sevki Gurhan KARS U88407662 (2025-08-01, change_invitation, invitation ASGH281608 2025-08-01..2025-10-28, item Match). `10/-6548` Devrim Bursal S 01841368 (2015-10-03, invitation ASGH367329 2015-09-30..2015-12-30; InvitationItem skipped -- PII.Passport `56894743-...` not in Passport id-map).
- **Halt**: wait for accept before App_Change_Inv header remainder, then roster, progress, Invitation, InvitationItem, Visa. Do not use `--visa-remainder` on this type.
- **Id-maps**: instance **4783**; sample `ApplicationProfileInstance.sample2-changeinv.json`.
- **Logs**: `artifacts/headless-import/AppChangeInv-header-sample2-20260908-095457.log`, roster/progress/inv/invitem/visa `AppChangeInv-*-sample2-20260908.log`
### 2026-09-08 - Empty skip probes after Additional WP (this wipe)

- **Phase**: import (probe)
- **Mode**: `--entity ApplicationProfileInstance --max-rows 2` for App_Visa_Ext, App_Visa_Ext_According_to_WP, App_WP_Ext, App_Exit_Visa, App_Visa_For_New_Born_FM.
- **Outcome**: each **0** prepared matches, Posted **0** Failed **0**. Confirms yaml skip empty on Calik.
- **Next**: band 3 App_Change_Inv (not cancel; those headers also 0 this wipe until imported).
### 2026-09-08 - App_Additional_WP_location remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance|Person|Progress|WorkPermit|WorkPermitItem --application-type App_Additional_WP_location --legacy-source calik-energi-local-pg --inprocess`. Progress `--batch-size 1`. Skip Invitation and Visa. No `--visa-remainder`.
- **Outcome**: success (exit **0**, Failed **0** all five)
- **Header**: Posted **279** / already **2** / type **281**. PG headers **281**. MovementPermitLocation backfill already=280 noFallback=**1**.
- **noFallback**: `12/-6943` (2015-12-08). Header Goşmaça FK empty; PIA WorkPermit present but WorkPermitLocation **null**. Left MovementPermitLocation empty. Do not skip.
- **Roster**: Posted **428** / already **9440** / missing parent **12746**. Id-map **9887**.
- **Progress**: Posted **1391** / already **21902** / no instance map **15639**. Id-map **23293**.
- **WorkPermit**: Posted **0** / already **361** / instance not in map **49**.
- **WorkPermitItem**: Posted **0** / already **3798** / missing id-map **2699**.
- **Instance id-map**: **4781**.
- **Logs**: `artifacts/headless-import/AppAddWpLoc-header-remainder-20260908-094701.log`, roster `...-094747`, progress `...-094904`, wp `...-094950`, wpitem `...-095020`.
- **Next**: empty skips (App_Visa_Ext, App_Visa_Ext_According_to_WP, App_WP_Ext, App_Exit_Visa, App_Visa_For_New_Born_FM). Then band 4 cancel types starting App_Cancel_Visa_and_WP (this wipe has not reimported cancel yet).
### 2026-09-08 - App_Additional_WP_location header+roster+progress+WP sample 2 .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Additional_WP_location --max-rows 2` then roster+progress+WorkPermit with `--application-id-map ApplicationProfileInstance.sample2-addwp.json`. `--legacy-source calik-energi-local-pg --inprocess`. Progress `--batch-size 1`. Skip Invitation and Visa. No `--visa-remainder`. WorkPermitItem skipped on sample (letters already imported; item importer is not type-scoped).
- **Outcome**: header Posted **2** Failed **0** (281 type matches). Roster Posted **2** Failed **0**. Progress Posted **10** Failed **0**. WorkPermit Posted **0** / already **361** / instance not in map **49**.
- **Apps**: `5/-9807` Sevgi YAVAŞ U 12402046 (2017-05-05, change_workpermit, MP Mary şaheri + Türkmenbaşy şaheri from WP bits; header GoşmaçaIşlemägeRugsatÝeri FK empty; AS577518 / 4061/41). `7/-3395` Faruk YILDIZ U 02820662 (2014-07-08, MP Aşgabat/Mary/Akbugdaý/Serdarabat from WP bits; AS453223 / 1347/8).
- **MovementPermitLocation**: header FK null; fallback majority PIA.WorkPermit.WorkPermitLocation bits Match PG. Backfill updated=0 already=2 noFallback=0.
- **ProjectContract**: legacy Application.Contract null; PG defaulted to **14306 Mary** (AssignDefaultsIfEmpty / ShowProjectContract).
- **Id-maps**: instance **4502**; person **9459**; progress **21902**. Sample `ApplicationProfileInstance.sample2-addwp.json`.
- **Halt**: wait for accept before App_Additional_WP_location header remainder, then roster, progress, WorkPermit (+ items if needed). Do not import Invitation or Visa on this type.
- **Logs**: `artifacts/headless-import/AppAddWpLoc-header-sample2-20260908-093519.log`, roster/progress/wp `AppAddWpLoc-*-sample2-20260908.log`
### 2026-09-08 - App_Border_Zone_Permission remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance|Person|Progress --application-type App_Border_Zone_Permission --legacy-source calik-energi-local-pg --inprocess`. Progress `--batch-size 1`. No Invitation/WorkPermit/Visa. No `--visa-remainder`.
- **Outcome**: success (exit **0**, Failed **0** all three)
- **Header**: Posted **107** / already **4** / type **111**. PG headers **111**, BorderZoneLocation Ýok **2** (7/-134, 7/-135). Id-map **4500**.
- **Roster**: Posted **208** / already **9230** / missing parent **13170**. Id-map **9457**.
- **Progress**: Posted **432** / already **21460** / no instance map **16201**. Id-map **21892**.
- **Logs**: `artifacts/headless-import/AppBorderZone-header-remainder-20260908-093139.log`, roster `...-093209.log`, progress `...-093304.log`
- **Next**: App_Additional_WP_location (header sample). Skip Invitation and Visa; WorkPermit if type generates.
### 2026-09-08 - App_Border_Zone_Permission extra sample 2 with BorderZoneForVisa .15 -> local PG

- **Phase**: import (sample)
- **Why**: Reviewer asked to see samples where the requested zone exists (not 7/-134 Ýok).
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Border_Zone_Permission --max-rows 4` (first 2 already imported; Posted **2**). Roster+progress with `ApplicationProfileInstance.sample2-bz-haszone.json`.
- **Outcome**: header Posted **2** Failed **0**. Roster Posted **7** Failed **0**. Progress Posted **10** Failed **0**.
- **Apps**: `12/-6970` Hakan HACIPAŞAOĞLU, Farap etrap, 14080 Watan, AS536268; `6/-8091` six employees, Farap etrap, 14080 Watan, ProcessNumber `Imza atılmadı Energ,minstr`. Both `ChooseBorderZoneType=1` / Farap bit.
- **Id-map**: instance **4393**. Sample `ApplicationProfileInstance.sample2-bz-haszone.json`.
- **Halt**: wait for accept before header remainder. Ýok keep on 7/-134 and 7/-135 unchanged.
- **Logs**: `artifacts/headless-import/AppBorderZone-roster-sample-haszone-20260908.log`, progress `AppBorderZone-progress-sample-haszone-20260908.log`
### 2026-09-08 - App_Border_Zone_Permission keep Ýok on 2 incomplete headers

- **Phase**: mapping
- **Lock**: Keep `7/-134` and `7/-135` as BorderZoneLocation **Ýok** (legacy incomplete). Do not skip. Do not invent a zone from PIA.Visa (those visas have no BorderZone).
- **Source**: 109/111 E:11 have `BorderZoneForVisa` (`ChooseBorderZoneType=1`). The 2 empties have `ChooseBorderZoneType=0`, FK null, `Bellik` null, process numbers set.
- **Artifacts**: `field-maps/Application.yaml` BorderZoneLocation notes; `application-type-import-order.yaml` App_Border_Zone_Permission notes.
- **Halt**: wait for accept before App_Border_Zone_Permission header remainder (sample 2 stays `7/-134` Ýok + `12/-12709` Farap etrap).
### 2026-09-08 - App_Border_Zone_Permission header+roster+progress sample 2 .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Border_Zone_Permission --max-rows 2` then roster+progress with `--application-id-map ApplicationProfileInstance.sample2-bz.json`. `--legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation`. Progress `--batch-size 1`.
- **Outcome**: header Posted **2** Failed **0** (111 type matches). Roster Posted **3** Failed **0**. Progress Posted **8** Failed **0**.
- **RunId**: header `20260908-091156`
- **Apps**: `7/-134` (Ahmet Faik Albayrak + Yusuf Ayabakan, 2020, profile get_border_zone, BZ Ýok, contract 1235-SERVIS MERKEZI); `12/-12709` (Özgür Purlu, 2018, Farap etrap, TAP). Skip Invitation, WorkPermit, Visa. Issued BorderZone documents wait in postAllTypeSlices.
- **Id-maps**: instance **4391**; person **9242**; progress **21450**. Sample map `ApplicationProfileInstance.sample2-bz.json`.
- **Halt**: wait for accept before App_Border_Zone_Permission header remainder, then roster, progress. Do not import Visa or `--visa-remainder` on this type.
- **Logs**: `artifacts/headless-import/AppBorderZone-header-sample2-20260908-091156.log`, roster `AppBorderZone-roster-sample2-20260908-091241.log`, progress `AppBorderZone-progress-sample2-20260908-091308.log`
### 2026-09-08 - Locked visa remainder + PIA pin in import strategy order

- **Phase**: strategy
- **Why**: Apply the agreed remainder/pin order so type slices do not use --visa-remainder and PIA.Visa mismatches are pinned after orphans exist in the Visa id-map.
- **Locked**: never --visa-remainder on --application-type; per type header → roster → progress → issued → Visa; after all types Rejection → BorderZone documents → --entity Visa --visa-remainder → --correct-visa2014-application-person-document-links. PIA.Visa vs latest-N is expected until pin. Do not run remainder before later types' headers if first POST must set IssuingApplicationProfileInstance.
- **Artifacts**: import-strategy.yaml pplicationTypeSlices; pplication-type-import-order.yaml postAllTypeSlices; order.yaml; SKILL.md; import-practices.md; IMPORT_PLAN_AND_STRATEGY.md; VISA2014_MIGRATION.md; ApplicationPerson.yaml.
- **Next**: still halted before App_Border_Zone_Permission.
### 2026-09-08 - Pin PIA Passport/Visa ResolvedLinks after importing no-issuing source visas

- **Phase**: correction
- **Why**: Ext FM sample ÖZKAN PIA.Visa A0835655/56/57 were not in Visa id-map (issuing app `4/-2612` AS447583 is GCRecord-deleted; PIA rows deleted). Roster Pin left latest-N visas. `--visa-remainder` then `--correct-visa2014-application-person-document-links`.
- **Mode**: `--import-visa2014 --entity Visa --visa-remainder --legacy-source calik-energi-local-pg --inprocess` (full 4389 instance map; no type filter). Then `--correct-visa2014-application-person-document-links --legacy-source calik-energi-local-pg`.
- **Outcome**: Visa remainder Posted **574** Failed **0** (already **5751**, no passport **25**). Visa id-map **6325**. Correction exit **0**: Visa links changed **3714**, Passport **0**, already correct **7127**, missing parent **13596**, no snapshot **65**.
- **Sample after pin**: `3/-5371` ResolvedLinks Visa A0835655/57/56 Match PIA; issued stickers A0950024/25/26 still on instance. `9/-3876` A0877078 still Match.
- **Note**: remainder also posted later-type issued visas without IssuingApplicationProfileInstance; later type Visa waves should backfill issuing.
- **Logs**: `artifacts/headless-import/visa-remainder-no-issuing-20260908-090434.log`, `correct-pia-document-links-20260908-090513.log`
- **Next**: still halted before App_Border_Zone_Permission.
### 2026-09-08 - App_Visa_Ext_FM Visa remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Visa --application-type App_Visa_Ext_FM --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--visa-remainder`; prior type visas kept)
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260908-090057`
- **Counts**: Legacy SQL **6375** / Prepared **6350** / skip **19** / dedupe **6** / Posted **574** / already **5177** / Failed **0** / no passport **25** / issuing not in instance map **574**. Id-map **5751**.
- **Log**: `artifacts/headless-import/AppVisaExtFm-visa-remainder-20260908-090057.log`
- **Next**: App_Visa_Ext_FM inner sequence complete. Next type from `application-type-import-order.yaml`: App_Border_Zone_Permission.
### 2026-09-08 - App_Visa_Ext_FM Visa sample 2 alongside header apps .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--import-visa2014 --entity Visa --application-type App_Visa_Ext_FM --application-id-map ApplicationProfileInstance.sample2-visaextfm.json --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--visa-remainder`; prior type visas kept)
- **Outcome**: success (exit **0**, Posted **3**, Failed **0**). Prepared **6350** / already **5174** / no passport **25** / issuing not in sample map **1148**. Id-map **5177**.
- **RunId**: `20260908-085803`
- **Apps**: `3/-5371` issued AS overlay A0950024 Yeşim / A0950025 Doruk / A0950026 Kıvanç ÖZKAN (WP:11 → FM-Maşgala). Employee Özgür visas A0949803 / A1073536 on same PIA process path not Issued (not on family roster). `9/-3876` no issued visa (PIA.Visa source A0877078 already in map). PIA.Visa pin still latest-N for ÖZKAN (A0835655/56/57 still not in Visa id-map).
- **Halt**: wait for accept before App_Visa_Ext_FM Visa remainder (full 4389 instance map). Do not use `--visa-remainder` on this type slice.
- **Log**: `artifacts/headless-import/AppVisaExtFm-visa-sample2-20260908-085803.log`
### 2026-09-08 - App_Visa_Ext_FM progress remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstanceProgress --application-type App_Visa_Ext_FM --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 1`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260908-085629`
- **Counts**: Posted **1623** / already **19819** / Failed **0** / no instance map **16650**. Id-map **21442**.
- **Log**: `artifacts/headless-import/AppVisaExtFm-progress-remainder-20260908-085629.log`
- **Next**: App_Visa_Ext_FM Visa sample (header apps `3/-5371` / `9/-3876`; no `--visa-remainder`), then remainder. Skip Invitation and WorkPermit.
### 2026-09-08 - App_Visa_Ext_FM roster remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstancePerson --application-type App_Visa_Ext_FM --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260908-085544`
- **Counts**: Prepared **22627** / Posted **676** / already **8563** / Failed **0** / missing parent **13388**. Id-map **9239**.
- **Log**: `artifacts/headless-import/AppVisaExtFm-roster-remainder-20260908-085544.log`
- **Next**: App_Visa_Ext_FM progress remainder.
### 2026-09-08 - App_Visa_Ext_FM header remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Visa_Ext_FM --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260908-085514`
- **Counts**: Type filter **333** / Posted **331** / already **2** / Failed **0**. Instance id-map **4389** (4058 prior + 331).
- **Log**: `artifacts/headless-import/AppVisaExtFm-header-remainder-20260908-085514.log`
- **Next**: App_Visa_Ext_FM roster remainder, then progress remainder.
### 2026-09-08 - App_Visa_Ext_FM sample: PIA document links vs ResolvedLinks

- **Phase**: mapping check (no import)
- **Apps**: `3/-5371` ÖZKAN ×3; `9/-3876` Ayşe Semiha HACIPAŞAOĞLU
- **Passport**: PIA.Passport numbers Match ResolvedLinks Passport on all 4 people. PreviousPassport NULL; `ShowPreviousPassport=false`.
- **Visa**: `ShowCurrentVisa=true`. Ayşe PIA.Visa A0877078 Match (already in Visa id-map). ÖZKAN PIA.Visa A0835657/A0835656/A0835655 **not** in Visa id-map or PG; ResolvedLinks still hold latest-N visas A1468263/A1468262/A1609186. Pin cannot replace until those source visas are imported (this type Visa step).
- **Halt**: still waiting for accept on header/roster/progress sample before remainders.
### 2026-09-08 - App_Visa_Ext_FM header+roster+progress sample 2 .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Visa_Ext_FM --max-rows 2` then roster+progress with `--application-id-map ApplicationProfileInstance.sample2-visaextfm.json`. `--legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation`
- **Outcome**: header Posted **2** Failed **0** (333 type matches). Roster Posted **4** Failed **0**. Progress Posted **9** Failed **0**.
- **RunId**: header `20260908-084754`
- **Apps**: `3/-5371` (Yeşim / Doruk / Kıvanç ÖZKAN, 2015, 5 progress steps, PROCESS_ISSUED A1073536); `9/-3876` (Ayşe Semiha HACIPAŞAOĞLU, 2014, 4 steps, no PROCESS_ISSUED). Profile `visa_ext_fm` / Wiza Möhletini Uzaltmak FM. Composite `F:7`. Roster slots FamilyMember. Skip Invitation and WorkPermit.
- **Id-maps**: instance **4058**; person **8563**; progress **19819**. Sample map `ApplicationProfileInstance.sample2-visaextfm.json`.
- **Halt**: wait for accept before App_Visa_Ext_FM header remainder, then roster, progress, Visa (skip Invitation and WorkPermit).
- **Logs**: `artifacts/headless-import/AppVisaExtFm-header-sample2-20260908-084754.log`, roster `AppVisaExtFm-roster-sample2-20260908-084823.log`, progress `AppVisaExtFm-progress-sample2-20260908-084848.log`
### 2026-09-08 - App_Visa_and_WP_Ext Visa remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Visa --application-type App_Visa_and_WP_Ext --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--visa-remainder`; prior type visas kept)
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260908-084437`
- **Counts**: Legacy SQL **6375** / Prepared **6350** / skip **19** / dedupe **6** / Posted **2013** / already **3161** / Failed **0** / no passport **25** / issuing not in instance map **1151**. Id-map **5174**.
- **Log**: `artifacts/headless-import/AppVisaWpExt-visa-remainder-20260908-084437.log`
- **Next**: App_Visa_and_WP_Ext inner sequence complete. Next type from `application-type-import-order.yaml`: App_Visa_Ext_FM (skip Invitation and WorkPermit).
### 2026-09-08 - App_Visa_and_WP_Ext Visa sample 2 alongside header apps .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--import-visa2014 --entity Visa --application-type App_Visa_and_WP_Ext --application-id-map ApplicationProfileInstance.sample2-visaextwp.json --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--visa-remainder`; prior type visas kept)
- **Outcome**: success (exit **0**, Posted **2**, Failed **0**). Prepared **6350** / already **3159** / no passport **25** / issuing not in sample map **3164**. Id-map **3161**.
- **RunId**: `20260908-084139`
- **Apps**: `12/-4793` A0939432 Turgut KURUN (AS overlay AS487962; PIA.Visa source A0833039 not Issued). `3/-9562` A1184573 Fatih GÜZELBİLEN (AS overlay AS593378; PIA.Visa source A1137996 not Issued). VisaType WP:11 → `WP-Işçi Wiza`.
- **Halt**: wait for accept before App_Visa_and_WP_Ext Visa remainder (full 4056 instance map). Do not use `--visa-remainder` on this type slice.
- **Log**: `artifacts/headless-import/AppVisaWpExt-visa-sample2-20260908-084139.log`
### 2026-09-07 - App_Visa_and_WP_Ext WorkPermitItem remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity WorkPermitItem --application-type App_Visa_and_WP_Ext --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-180208`
- **Counts**: Legacy SQL **6497** / Prepared **6497** / Posted **1678** / already **2120** / Failed **0** / missing required id-map **2699** / position fallback **9**. Id-map **3798**.
- **Log**: `artifacts/headless-import/AppVisaWpExt-workpermititem-remainder-20260907-180208.log`
- **Next**: App_Visa_and_WP_Ext Visa sample (header apps `12/-4793` / `3/-9562`; no `--visa-remainder`), then remainder.
### 2026-09-07 - App_Visa_and_WP_Ext WorkPermit remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity WorkPermit --application-type App_Visa_and_WP_Ext --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-180141`
- **Counts**: Legacy SQL **410** / Prepared **410** / Posted **132** / already **229** / Failed **0** / instance not in id-map **49**. Id-map **361**.
- **Log**: `artifacts/headless-import/AppVisaWpExt-workpermit-remainder-20260907-180141.log`
- **Next**: WorkPermitItem remainder.
### 2026-09-07 - App_Visa_and_WP_Ext WorkPermit+Item sample 2 majority-letter apps .15 -> local PG

- **Phase**: import (sample)
- **Mode**: WorkPermit then WorkPermitItem with `--application-id-map ApplicationProfileInstance.sample2-visaextwp-wp.json`. `--legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation`
- **Outcome**: WorkPermit Posted **2** Failed **0** (already Inv+WP **227**, other letters **181**). WorkPermitItem Posted **5** Failed **0** (already **2115**, missing required id-map **4377**, position fallback **1**).
- **RunId**: WorkPermit `20260907-175703`; WorkPermitItem `20260907-175738`
- **Header sample apps** `12/-4793` / `3/-9562` do **not** own majority WorkPermitLetter (PIA.WorkPermit is existing letter only). WP sample uses majority-owner Ext apps `1/-2161` letter **382** (10 legacy items, 4 posted); `2/-2357` letter **717** (5 legacy items, 1 posted). Unposted items: Person/Passport/EPH not in id-map.
- **Halt**: wait for accept before WorkPermit remainder (full 4056 instance map) then WorkPermitItem remainder, then Visa sample on header apps.
- **Logs**: `artifacts/headless-import/AppVisaWpExt-workpermit-sample2-20260907-175703.log`, `AppVisaWpExt-workpermititem-sample2-20260907-175738.log`
### 2026-09-07 - App_Visa_and_WP_Ext progress remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstanceProgress --application-type App_Visa_and_WP_Ext --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 1`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-174920`
- **Counts**: Posted **3750** / already **16060** / Failed **0** / no instance map **17312**. Id-map **19810**.
- **Log**: `artifacts/headless-import/AppVisaWpExt-progress-remainder-20260907-174920.log`
- **Next**: App_Visa_and_WP_Ext WorkPermit + WorkPermitItem sample (same 2 apps if they own WP letters; skip Invitation), then remainder, then Visa.
### 2026-09-07 - App_Visa_and_WP_Ext roster remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstancePerson --application-type App_Visa_and_WP_Ext --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-174527`
- **Counts**: Prepared **22627** / Posted **2525** / already **6034** / Failed **0** / missing parent **14068**. Id-map **8559**.
- **Log**: `artifacts/headless-import/AppVisaWpExt-roster-remainder-20260907-174527.log`
- **Next**: App_Visa_and_WP_Ext progress remainder.
### 2026-09-07 - App_Visa_and_WP_Ext header remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Visa_and_WP_Ext --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-174457`
- **Counts**: Type filter **771** / Posted **769** / already **2** / Failed **0**. Instance id-map **4056** (3285 prior + 771).
- **Log**: `artifacts/headless-import/AppVisaWpExt-header-remainder-20260907-174457.log`
- **Next**: App_Visa_and_WP_Ext roster remainder, then progress remainder.
### 2026-09-07 - App_Visa_and_WP_Ext header+roster+progress sample 2 .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Visa_and_WP_Ext --max-rows 2` then roster+progress with `--application-id-map ApplicationProfileInstance.sample2-visaextwp.json`. `--legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation`
- **Outcome**: header Posted **2** Failed **0** (771 type matches). Roster Posted **2** Failed **0**. Progress Posted **10** Failed **0** (5 steps each, 1 ministry pair 1_REVIEW + 2_REVIEW).
- **RunId**: header `20260907-173506`
- **Apps**: `12/-4793` (Turgut KURUN, 2014); `3/-9562` (Fatih GÜZELBİLEN, 2017). Profile `extend_visa_wp` / Wiza we Iş Rugsatnamasyny Uzaltmak. Composite `E:7` + WizaAndWorkPermitRequired **1**.
- **Id-maps**: instance **3287**; person **6034**; progress **16060**. Sample map `ApplicationProfileInstance.sample2-visaextwp.json`.
- **Halt**: wait for accept before App_Visa_and_WP_Ext header remainder, then roster, progress, WorkPermit, WorkPermitItem, Visa (skip Invitation).
- **Logs**: `artifacts/headless-import/AppVisaWpExt-header-sample2-20260907-173506.log`, roster `AppVisaWpExt-roster-sample2-20260907-173545.log`, progress `AppVisaWpExt-progress-sample2-20260907-173617.log`
### 2026-09-07 - App_Inv_According_to_WP Visa remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Visa --application-type App_Inv_According_to_WP --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--visa-remainder`; prior type visas kept)
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-173309`
- **Counts**: Legacy SQL **6375** / Prepared **6350** / skip **19** / dedupe **6** / Posted **38** / already **3121** / Failed **0** / no passport **25** / issuing not in instance map **3166**. Id-map **3159**.
- **Log**: `artifacts/headless-import/AppInvAccWp-visa-remainder-20260907-173309.log`
- **Next**: App_Inv_According_to_WP inner sequence complete. Next type slice from `application-type-import-order.yaml` after band-1 invitation types.
### 2026-09-07 - App_Inv_According_to_WP Visa sample 2 alongside header apps .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--import-visa2014 --entity Visa --application-type App_Inv_According_to_WP --application-id-map ApplicationProfileInstance.sample2-invaccwp.json --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--visa-remainder`; prior visas kept)
- **Outcome**: success (exit **0**, Posted **1**, Failed **0**). Prepared **6350** / already **3120** / no passport **25** / issuing not in sample map **3204**.
- **RunId**: `20260907-172624`
- **Apps**: `9/-10592` A1230666 Tevfik Bora ERGÜN (earliest of 5 PIA visas; later stickers not Issued on this app). `12/-4660` Tuğrul ÇANAKÇI — **no Visa row** on passport U 05616056 (invitation only). VisaType WP:11 → `WP-Işçi Wiza`. AS overlay AS614913 = Application.ProcessNumber.
- **Halt**: wait for accept before App_Inv_According_to_WP Visa remainder (full 3285 instance map). Do not use `--visa-remainder` on this type slice.
- **Log**: `artifacts/headless-import/AppInvAccWp-visa-sample2-20260907-172624.log`
### 2026-09-07 - App_Inv_According_to_WP InvitationItem remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity InvitationItem --application-type App_Inv_According_to_WP --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-172512`
- **Counts**: Legacy SQL **5305** / Prepared **5305** / Posted **67** / already **5113** / Failed **0** / missing invitation map **125**. Id-map **5180**.
- **Log**: `artifacts/headless-import/AppInvAccWp-invitationitem-remainder-20260907-172512.log`
- **Next**: App_Inv_According_to_WP Visa sample (issuing instance in type map; no `--visa-remainder`), then remainder.
### 2026-09-07 - App_Inv_According_to_WP Invitation remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Invitation --application-type App_Inv_According_to_WP --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-172443`
- **Counts**: Legacy SQL **2943** / Prepared **2943** / Posted **47** / already **2805** / Failed **0** / instance not in id-map **91**. Id-map **2852**.
- **Log**: `artifacts/headless-import/AppInvAccWp-invitation-remainder-20260907-172443.log`
- **Next**: InvitationItem remainder.
### 2026-09-07 - App_Inv_According_to_WP Invitation+Item sample 2 alongside header apps .15 -> local PG

- **Phase**: import (sample)
- **Mode**: Invitation `--application-id-map ApplicationProfileInstance.sample2-invaccwp.json` then InvitationItem. `--legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation`
- **Outcome**: Invitation Posted **2** Failed **0** (already **2803**, skip other **138**). InvitationItem Posted **2** Failed **0** (already **5111**, missing invitation **192**).
- **RunId**: Invitation `20260907-172139`; InvitationItem `20260907-172204`
- **Apps**: same as header sample — `12/-4660` ASGH334302 (Tuğrul ÇANAKÇI); `9/-10592` ASGH450891 (Tevfik Bora ERGÜN). Numbers match PROCESS_ISSUED descriptions.
- **Halt**: wait for accept before Invitation remainder (full 3285 instance map) then InvitationItem remainder, then Visa.
- **Logs**: `artifacts/headless-import/AppInvAccWp-invitation-sample2-20260907-172139.log`, `AppInvAccWp-invitationitem-sample2-20260907-172204.log`
### 2026-09-07 - App_Inv_According_to_WP progress remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstanceProgress --application-type App_Inv_According_to_WP --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 1`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-171932`
- **Counts**: Prepared **37187** / Posted **279** / already **15771** / Failed **0** / no instance map **21137**. Id-map **16050**.
- **Log**: `artifacts/headless-import/AppInvAccWp-progress-remainder-20260907-171932.log`
- **Next**: App_Inv_According_to_WP Invitation + InvitationItem sample (same 2 apps), then remainder, then Visa (skip WorkPermit).
### 2026-09-07 - App_Inv_According_to_WP roster remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstancePerson --application-type App_Inv_According_to_WP --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-171859`
- **Counts**: Prepared **22627** / Posted **82** / already **5950** / Failed **0** / missing parent **16595**. Id-map **6032**.
- **Log**: `artifacts/headless-import/AppInvAccWp-roster-remainder-20260907-171859.log`
- **Next**: App_Inv_According_to_WP progress remainder.
### 2026-09-07 - App_Inv_According_to_WP header remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Inv_According_to_WP --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-171822`
- **Counts**: Type filter **61** / Posted **59** / already **2** / Failed **0**. Instance id-map **3285** (prior 3224 + this type 61).
- **Log**: `artifacts/headless-import/AppInvAccWp-header-remainder-20260907-171822.log`
- **Next**: App_Inv_According_to_WP roster remainder, then progress remainder.
### 2026-09-07 - App_Inv_According_to_WP header+roster+progress sample 2 .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Inv_According_to_WP --max-rows 2` then roster+progress with `--application-id-map ApplicationProfileInstance.sample2-invaccwp.json`. `--legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation`
- **Outcome**: header Posted **2** Failed **0** (61 type matches). Roster Posted **2** Failed **0**. Progress Posted **10** Failed **0** (5 steps each, 1 ministry pair 1_REVIEW + 2_REVIEW).
- **RunId**: header `20260907-171131`
- **Apps**: `12/-4660` (Tuğrul ÇANAKÇI, 2014); `9/-10592` (Tevfik Bora ERGÜN, 2017). Profile `get_invitation_according_to_wp` / İş Rugsatnama görä Çakylyk Almak. Composite `E:20:na:na:na`. Roster is **employee**, not family.
- **Id-maps**: instance **3226**; person **5950**; progress **15771**. Sample map `ApplicationProfileInstance.sample2-invaccwp.json`.
- **Halt**: wait for accept before App_Inv_According_to_WP header remainder, then roster, progress, Invitation, InvitationItem, Visa (skip WorkPermit).
- **Logs**: `artifacts/headless-import/AppInvAccWp-header-sample2-20260907-171131.log`, roster `AppInvAccWp-roster-sample2-20260907-171211.log`, progress `AppInvAccWp-progress-sample2-20260907-171239.log`
### 2026-09-07 - App_Inv_FM Visa remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Visa --application-type App_Inv_FM --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--visa-remainder`; prior type visas kept)
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-170938`
- **Counts**: Legacy SQL **6375** / Prepared **6350** / skip **19** / dedupe **6** / Posted **324** / already **2796** / Failed **0** / no passport **25** / issuing not in instance map **3205**. Id-map **3120**.
- **Log**: `artifacts/headless-import/AppInvFm-visa-remainder-20260907-170938.log`
- **Next**: App_Inv_FM inner sequence complete. Next type slice **App_Inv_According_to_WP** (header → roster → progress → Invitation → InvitationItem → Visa; skip WorkPermit).
### 2026-09-07 - App_Inv_FM Visa sample 2 alongside header apps .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--import-visa2014 --entity Visa --application-type App_Inv_FM --application-id-map ApplicationProfileInstance.sample2-invfm.json --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--visa-remainder`; prior visas kept)
- **Outcome**: success (exit **0**, Posted **2**, Failed **0**). Prepared **6350** / already **2794** / no passport **25** / issuing not in sample map **3529**.
- **RunId**: `20260907-170636`
- **Apps**: same header sample — `10/-4188` A0908046 Ayşe GÜNDOĞDU (earliest of 2 PIA visas; later A0929148 not posted — one visa per PIA); `5/-816` A1711615 Sema Bolat (earliest). Family person override: legacy WP:11 → `FM-Maşgala`.
- **Halt**: wait for accept before App_Inv_FM Visa remainder (full 3224 instance map). Do not use `--visa-remainder` on this type slice.
- **Log**: `artifacts/headless-import/AppInvFm-visa-sample2-20260907-170636.log`
### 2026-09-07 - App_Inv_FM InvitationItem remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity InvitationItem --application-type App_Inv_FM --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-170449`
- **Counts**: Legacy SQL **5305** / Prepared **5305** / Posted **350** / already **4761** / Failed **0** / missing invitation map **194**. Id-map **5111**.
- **Log**: `artifacts/headless-import/AppInvFm-invitationitem-remainder-20260907-170449.log`
- **Next**: App_Inv_FM Visa sample (issuing instance in type map; no `--visa-remainder`), then remainder.
### 2026-09-07 - App_Inv_FM Invitation remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Invitation --application-type App_Inv_FM --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-170420`
- **Counts**: Legacy SQL **2943** / Prepared **2943** / Posted **193** / already **2610** / Failed **0** / instance not in id-map **140**. Id-map **2803**.
- **Log**: `artifacts/headless-import/AppInvFm-invitation-remainder-20260907-170420.log`
- **Next**: InvitationItem remainder.
### 2026-09-07 - App_Inv_FM Invitation+Item sample 2 alongside header apps .15 -> local PG

- **Phase**: import (sample)
- **Mode**: Invitation `--application-id-map ApplicationProfileInstance.sample2-invfm.json` then InvitationItem. `--legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation`
- **Outcome**: Invitation Posted **2** Failed **0** (already **2608**, skip other **333**). InvitationItem Posted **2** Failed **0** (already **4759**, missing invitation **544**).
- **RunId**: Invitation `20260907-170129`; InvitationItem `20260907-170205`
- **Apps**: same as header sample — `10/-4188` ASGH325851 (Ayşe GÜNDOĞDU); `5/-816` CO317058 (Sema Bolat). Numbers match PROCESS_ISSUED descriptions.
- **Halt**: wait for accept before Invitation remainder (full 3224 instance map) then InvitationItem remainder, then Visa.
- **Logs**: `artifacts/headless-import/AppInvFm-invitation-sample2-20260907-170129.log`, `AppInvFm-invitationitem-sample2-20260907-170205.log`
### 2026-09-07 - App_Inv_FM progress remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstanceProgress --application-type App_Inv_FM --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 1`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-165621`
- **Counts**: Prepared **37199** / Posted **1110** / already **14651** / Failed **0** / no instance map **21438**. Id-map **15761**.
- **Log**: `artifacts/headless-import/AppInvFm-progress-remainder-20260907-165621.log`
- **Next**: App_Inv_FM Invitation + InvitationItem sample, then remainder, then Visa (skip WorkPermit).
### 2026-09-07 - App_Inv_FM roster remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstancePerson --application-type App_Inv_FM --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-165548`
- **Counts**: Prepared **22627** / Posted **396** / already **5552** / Failed **0** / missing parent **16679**. Id-map **5948**.
- **Log**: `artifacts/headless-import/AppInvFm-roster-remainder-20260907-165548.log`
- **Next**: App_Inv_FM progress remainder.
### 2026-09-07 - App_Inv_FM header remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Inv_FM --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-165523`
- **Counts**: Type filter **228** / Posted **226** / already **2** / Failed **0**. Instance id-map **3224** (App_Inv 1818 + App_Inv_And_WP 1178 + App_Inv_FM 228).
- **Log**: `artifacts/headless-import/AppInvFm-header-remainder-20260907-165523.log`
- **Next**: App_Inv_FM roster remainder, then progress remainder.
### 2026-09-07 - App_Inv_FM header+roster+progress sample 2 .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Inv_FM --max-rows 2` then roster+progress with `--application-id-map ApplicationProfileInstance.sample2-invfm.json`. `--legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation`
- **Outcome**: header Posted **2** Failed **0** (228 type matches). Roster Posted **2** Failed **0**. Progress Posted **10** Failed **0** (5 steps each, 1 ministry pair 1_REVIEW + 2_REVIEW).
- **RunId**: header `20260907-164632`
- **Apps**: `10/-4188` (Ayşe GÜNDOĞDU, family of Sevgi YAVAŞ, 2014); `5/-816` (Sema Bolat, family of Hasan Bolat, 2026). Profile `get_invitation_fm` / Çakylyk Almak FM. Composite `F:0:na:na:na`.
- **Halt**: wait for accept before App_Inv_FM header remainder, then roster, progress, Invitation, InvitationItem, Visa (skip WorkPermit).
- **Logs**: `artifacts/headless-import/AppInvFm-header-sample2-20260907-164632.log`, roster `AppInvFm-roster-sample2-20260907-164709.log`, progress `AppInvFm-progress-sample2-20260907-164737.log`
### 2026-09-07 - App_Inv_And_WP Visa remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Visa --application-type App_Inv_And_WP --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--visa-remainder`; App_Inv visas kept)
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-164410`
- **Counts**: Legacy SQL **6375** / Prepared **6350** / skip transform **19** / dedupe **6** / Posted **1104** / already **1690** / Failed **0** / no passport **25** / issuing not in instance map **3531**. Id-map **2794**.
- **Log**: `artifacts/headless-import/AppInvAndWp-visa-remainder-20260907-164410.log`
- **Next**: App_Inv_And_WP inner sequence complete. Next type slice **App_Inv_FM** (header → roster → progress → Invitation → InvitationItem → Visa; skip WorkPermit unless type generates WP).
### 2026-09-07 - Domain: PersonInApplication.Visa is existing (source) visa, not newly issued

- **Phase**: discovery (officer confirmation)
- **Fact**: `PersonInApplication.Visa` is the **existing** visa used when **preparing** the application (input / predecessor). It is **not** the visa newly issued as a result of that application.
- **Import alignment**: Newly issued visa → application uses `Visa.ProcessNumber` → PIA (output lineage) and `Visa.ASNumber` overlay onto unique `Application.ProcessNumber`. Extension matching uses `PIA.Visa` only as **predecessor** (subtype 7 sibling), never as the issued visa Oid. Roster `ResolvedLinks` copies `PIA.Visa` as the case’s source visa document.
- **Sample implication**: `3/-184` A1541088 was linked by AS overlay `CO0126981`, not by `PIA.Visa`.
### 2026-09-07 - App_Inv_And_WP Visa sample 2 alongside header apps .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--import-visa2014 --entity Visa --application-type App_Inv_And_WP --application-id-map ApplicationProfileInstance.sample2-invwp.json --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--visa-remainder`; did not wipe App_Inv visas)
- **Outcome**: success (exit **0**, Posted **2**, Failed **0**). Prepared **6350** / skip **19** / dedupe **6** / already App_Inv **1688** / no passport **25** / issuing not in sample map **4635**.
- **RunId**: `20260907-162925`
- **Apps**: same header sample — `12/-9185` A1159060 Selman CAN (PIA+AS overlay AS581953); `3/-184` A1541088 Yakup Yılmaz (AS overlay CO0126981; invitation was CO235327). VisaType WP:11 → `WP-Işçi Wiza`.
- **Halt**: wait for accept before App_Inv_And_WP Visa remainder (full 2996 instance map). Do not use `--visa-remainder` on this type slice.
- **Log**: `artifacts/headless-import/AppInvAndWp-visa-sample2-20260907-162925.log`
### 2026-09-07 - App_Inv_And_WP WorkPermitItem remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity WorkPermitItem --application-type App_Inv_And_WP --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-162613`
- **Counts**: Legacy SQL **6497** / Prepared **6497** / Posted **2113** / already **2** / Failed **0** / missing WP map **4382** / position fallback **33**. Id-map **2115**.
- **Log**: `artifacts/headless-import/AppInvAndWp-workpermititem-remainder-20260907-162613.log`
- **Next**: App_Inv_And_WP Visa sample (issuing instance in type map; no `--visa-remainder`), then remainder.
### 2026-09-07 - App_Inv_And_WP WorkPermit remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity WorkPermit --application-type App_Inv_And_WP --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-162543`
- **Counts**: Legacy SQL **410** / Prepared **410** / Posted **225** / already **2** / Failed **0** / instance not in id-map **183**. Id-map **227**. Majority app may be App_Inv or App_Inv_And_WP (both in the 2996 instance map). **183** letters wait for later type slices.
- **Log**: `artifacts/headless-import/AppInvAndWp-workpermit-remainder-20260907-162543.log`
- **Next**: WorkPermitItem remainder.
### 2026-09-07 - App_Inv_And_WP WorkPermit+Item sample 2 (majority-letter apps) .15 -> local PG

- **Phase**: import (sample)
- **Mode**: WorkPermit `--application-id-map ApplicationProfileInstance.sample2-invwp-wp.json` then WorkPermitItem. `--legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation`
- **Outcome**: WorkPermit Posted **2** Failed **0** (skip not in map **408**). WorkPermitItem Posted **2** Failed **0** (missing WP map **6495**). Position fallback **0**.
- **RunId**: WorkPermit `20260907-161338`; WorkPermitItem `20260907-161404`
- **Apps**: `10/-8843` letter **3024** (Sinan TAŞ); `11/-10982` letter **2222** (Hasan ÖZMEN). Header FK = majority Application per WorkPermitLetter (not every PIA.ProcessNumber app).
- **Header sample apps `12/-9185` / `3/-184`**: `3/-184` has no WorkPermit. `12/-9185` has 5 items on shared letters whose majority apps are other instances — would post **0** headers on those two FKs. Remainder uses full instance map (2996); letters attach to majority app when that app is already imported.
- **Halt**: wait for accept before WorkPermit remainder (full instance map) then WorkPermitItem remainder, then Visa for App_Inv_And_WP.
- **Logs**: `artifacts/headless-import/AppInvAndWp-workpermit-sample2-20260907-161338.log`, `AppInvAndWp-workpermititem-sample2-20260907-161404.log`
### 2026-09-07 - App_Inv_And_WP InvitationItem remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity InvitationItem --application-type App_Inv_And_WP --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-160157`
- **Counts**: Legacy SQL **5305** / Prepared **5305** / Posted **1724** / already **3035** / Failed **0** / missing invitation map **546**. Id-map **4759**.
- **Log**: `artifacts/headless-import/AppInvAndWp-invitationitem-remainder-20260907-160157.log`
- **Next**: App_Inv_And_WP WorkPermit + WorkPermitItem sample, then remainder, then Visa.
### 2026-09-07 - App_Inv_And_WP Invitation remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Invitation --application-type App_Inv_And_WP --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-160121`
- **Counts**: Legacy SQL **2943** / Prepared **2943** / Posted **937** / already **1671** / Failed **0** / instance not in id-map **335**. Id-map **2608**.
- **Log**: `artifacts/headless-import/AppInvAndWp-invitation-remainder-20260907-160121.log`
- **Next**: InvitationItem remainder.
### 2026-09-07 - App_Inv_And_WP Invitation+Item sample 2 alongside header apps .15 -> local PG

- **Phase**: import (sample)
- **Mode**: Invitation `--application-id-map ApplicationProfileInstance.sample2-invwp.json` then InvitationItem. `--legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation`
- **Outcome**: Invitation Posted **2** Failed **0** (already App_Inv **1669**, other types **1272**). InvitationItem Posted **2** Failed **0** (already **3033**, missing invitation **2270**). Rebuilt DataImporter.exe first (F5 had deleted it).
- **RunId**: Invitation `20260907-155529`; InvitationItem `20260907-155614`
- **Apps**: same as header sample — `12/-9185` ASGH419907 (1 of 3 roster people); `3/-184` CO235327 (Yakup Yılmaz). Numbers match PROCESS_ISSUED descriptions.
- **Halt**: wait for accept before Invitation remainder (full 2996 instance map) then InvitationItem remainder, then WorkPermit.
- **Logs**: `artifacts/headless-import/AppInvAndWp-invitation-sample2-20260907-155529.log`, `AppInvAndWp-invitationitem-sample2-20260907-155614.log`
### 2026-09-07 - App_Inv_And_WP progress remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstanceProgress --application-type App_Inv_And_WP --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 1`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-155137`
- **Counts**: Prepared **36526** / Posted **5790** / already **8851** / Failed **0** / no instance map **21885**. Id-map **14641**.
- **Log**: `artifacts/headless-import/AppInvAndWp-progress-remainder-20260907-1551.log`
- **Next**: App_Inv_And_WP Invitation + InvitationItem, then WorkPermit + WorkPermitItem, then Visa.
### 2026-09-07 - App_Inv_And_WP roster remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstancePerson --application-type App_Inv_And_WP --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-154804`
- **Counts**: Prepared **22624** / Posted **2250** / already **3300** (App_Inv + sample) / Failed **0** / missing parent **17074**. Id-map **5550**.
- **Log**: `artifacts/headless-import/AppInvAndWp-roster-remainder-20260907-1548.log`
- **Next**: App_Inv_And_WP progress remainder.
### 2026-09-07 - App_Inv_And_WP header remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Inv_And_WP --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-154718`
- **Counts**: Type filter **1178** / Posted **1176** / already **2** / Failed **0**. Instance id-map now includes App_Inv **1818** + App_Inv_And_WP **1178**.
- **Log**: `artifacts/headless-import/AppInvAndWp-header-remainder-20260907-154718.log`
- **Next**: App_Inv_And_WP roster remainder, then progress remainder.
### 2026-09-07 - App_Inv_And_WP header+roster+progress sample 2 .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--entity ApplicationProfileInstance --application-type App_Inv_And_WP --max-rows 2` then roster+progress with `--application-id-map ApplicationProfileInstance.sample2-invwp.json`. `--legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation`
- **Outcome**: header Posted **2** Failed **0** (1178 type matches). Roster Posted **4** Failed **0**. Progress Posted **10** Failed **0** (2 ministry legs each).
- **RunId**: header `20260907-154121`
- **Apps**: `12/-9185` (3 people, 2016); `3/-184` (Yakup Yılmaz, 2024). Profile `get_invitation_wp` / Çakylyk we Iş Rugsatnamasyny Almak. Composite `E:0:1:na:na`.
- **Halt**: wait for accept before App_Inv_And_WP header remainder, then roster, progress, Invitation, WorkPermit, Visa.
- **Logs**: `artifacts/headless-import/AppInvAndWp-header-sample2-20260907-154121.log`, roster/progress `AppInvAndWp-*-sample2-20260907-1542.log`
### 2026-09-07 - App_Inv Visa remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Visa --application-type App_Inv --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--visa-remainder`)
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-153929`
- **Counts**: Legacy SQL **6375** / Prepared **6350** / transform skip **19** / dedupe **6** / Posted **1684** / already **4** / Failed **0** / no passport **25** / issuing not in App_Inv instance map **4637**. Id-map **1688**.
- **Log**: `artifacts/headless-import/AppInv-visa-remainder-20260907-153929.log`
- **Next**: App_Inv inner sequence complete. Next type slice App_Inv_And_WP (header → roster → progress → Invitation → InvitationItem → WorkPermit → WorkPermitItem → Visa).
### 2026-09-07 - App_Inv Visa sample 2 issuing apps .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--import-visa2014 --entity Visa --application-type App_Inv --application-id-map ApplicationProfileInstance.sample2-visa.json --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Posted **4**, Failed **0**). Prepared **6350** / skip transform **19** / no passport **25** / issuing not in sample map **6321**.
- **RunId**: `20260907-153646`
- **Apps**: `6/-982` (Muhammed Atalay A1742338); `8/-1598` (3 visas A1745953–A1745955). Earlier roster sample apps `7/-1319` / `8/-1538` have invitations but no Visa.ProcessNumber PIA yet.
- **Halt**: wait for accept before App_Inv Visa remainder (full 1818 instance id-map). Do not use `--visa-remainder` on this type slice.
- **Log**: `artifacts/headless-import/AppInv-visa-sample2-20260907-153646.log`
### 2026-09-07 - App_Inv InvitationItem remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity InvitationItem --application-type App_Inv --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-153250`
- **Counts**: Legacy SQL **5305** / Prepared **5305** / Posted **3027** / already **6** / Failed **0** / missing invitation map **2272**. Id-map **3033**.
- **Log**: `artifacts/headless-import/AppInv-invitationitem-remainder-20260907-153250.log`
- **Next**: App_Inv Visa. Skip WorkPermit on App_Inv.
### 2026-09-07 - App_Inv Invitation remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Invitation --application-type App_Inv --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-153140`
- **Counts**: Legacy SQL **2943** / Prepared **2943** / Posted **1667** / already **2** / Failed **0** / instance not in id-map **1274**. Id-map **1669**.
- **Log**: `artifacts/headless-import/AppInv-invitation-remainder-20260907-153140.log`
- **Next**: InvitationItem remainder.
### 2026-09-07 - App_Inv Invitation+Item sample 2 alongside roster apps .15 -> local PG

- **Phase**: import (sample)
- **Mode**: Invitation `--application-id-map ApplicationProfileInstance.sample2-roster.json` then InvitationItem (invitation id-map 2 keys). `--legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation`
- **Outcome**: Invitation Posted **2** Failed **0** (skipped other types **2941**). InvitationItem Posted **6** Failed **0** (missing invitation map **5299**).
- **RunId**: Invitation `20260907-152557`; InvitationItem `20260907-152648`
- **Apps**: same as header/roster/progress sample — `7/-1319` CO326555 (1 item); `8/-1538` CO331502 (5 items).
- **Notes**: VisaPeriod from letter issued→expire span (`3 (üç) aý`). VisaCategory default `köp gezeklik` (not copied from application). Border zone `Ýok`.
- **Halt**: wait for accept before Invitation remainder (full instance id-map) then InvitationItem remainder.
- **Logs**: `artifacts/headless-import/AppInv-invitation-sample2-20260907-152557.log`, `AppInv-invitationitem-sample2-20260907-152648.log`
### 2026-09-07 - App_Inv progress remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstanceProgress --application-type App_Inv --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 1`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-151721`
- **Counts**: Legacy apps **12708** / Prepared **36574** / parent-skip **176** / Posted **8833** / already **8** / Failed **0** / no instance map (other types) **27733**. Id-map **8841**.
- **Log**: `artifacts/headless-import/AppInv-progress-remainder-20260907-1518.log`
- **Next**: App_Inv Invitation + InvitationItem, then Visa. Skip WorkPermit on App_Inv.
### 2026-09-07 - App_Inv roster remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstancePerson --application-type App_Inv --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-150341`
- **Counts**: Legacy SQL **22832** / Prepared **22624** / transform skip **208** / Posted **3294** / already **2** / Failed **0** / missing parent (other types) **19328**. Id-map **3296**.
- **Log**: `artifacts/headless-import/AppInv-roster-remainder-20260907-150341.log`
- **Next**: App_Inv progress remainder (full ApplicationProfileInstance id-map).
### 2026-09-07 - App_Inv progress sample 2 alongside roster apps .15 -> local PG

- **Phase**: import (sample)
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstanceProgress --application-type App_Inv --application-id-map ApplicationProfileInstance.sample2-roster.json --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 1`
- **Outcome**: success (exit **0**, Posted **8**, Failed **0**). Prepared 36768 / skipped missing map 36760 (full extract; id-map limited to the two roster sample apps).
- **RunId**: `20260907-145743`
- **Apps**: same as roster sample — `7/-1319` (Yunus Emre Yıldız), `8/-1538` (Lucas Ota). 4 steps each: `1_REVIEW_STARTED` / `1_REVIEW_APPROVED` / `PROCESS_STARTED` / `PROCESS_ISSUED`.
- **Halt**: wait for accept of combined header+roster+progress tables before App_Inv roster remainder + progress remainder.
- **Log**: `artifacts/headless-import/AppInv-progress-sample2-20260907-145743.log`
### 2026-09-07 - App_Inv roster sample 2 retry .15 -> local PG (after ResolvedLinks unique-index fix)

- **Phase**: import (sample)
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstancePerson --application-type App_Inv --max-rows 2 --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 1`
- **Outcome**: success (exit **0**, Posted **2**, Failed **0**)
- **RunId**: `20260907-145002`
- **Fix**: `Visa2014ApplicationPersonDocumentLinks.ReplaceKind` now unions unsaved `PersonResolvedLinks` with `LoadLinks` (GetObjectsQuery misses LinkPerson/RefreshResolvedLinks rows). Unique `IX_ApplicationProfileInstancePersonResolvedLinks_Instance_Perso` no longer fires on CommitChanges.
- **Samples**: PIA `d9b6153f-17a7-4aff-b5ab-000155bfd71f` -> Person `b71d3b0c-f41f-4d08-ad5f-64aa8b1a963d` (Yunus Emre Yıldız, 7/-1319, passport U25834233); PIA `7dd8c289-1c15-4f24-84ed-00092e951da3` -> Person `12978cc3-263e-41b6-aab1-4b6eba7bc5cb` (Lucas Ota, 8/-1538, passport GB889706). Visa links empty (Visa wave later). Log `ResolvedLinks created (sum): 0` is AutoLinkedCount after Pin (passport already present from RefreshResolvedLinks).
- **Halt**: wait for accept before App_Inv roster remainder.
- **Log**: `artifacts/headless-import/AppInv-roster-sample2-20260907-145002.log`
### 2026-09-07 - App_Inv roster sample 2 .15 -> local PG FAILED unique ResolvedLinks

- **Phase**: import (sample)
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstancePerson --application-type App_Inv --max-rows 2 --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: failed (exit **1**). Progress posted=2 failed=0 then CommitChanges `23505` unique index `IX_ApplicationProfileInstancePersonResolvedLinks_Instance_Perso`. PG people/links rolled back to 0.
- **RunId**: `20260907-144240`
- **Cause**: PinDocumentSnapshot `ReplaceKind` loaded DB-only links, treated unsaved Passport rows as missing, created duplicates of the same Instance+Person+Kind+Object.
- **Log**: `artifacts/headless-import/AppInv-roster-sample2.log` (if present)
### 2026-09-07 - App_Inv header remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Inv --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-143753`
- **Counts**: Filter **1818** / Posted **1816** / already **2** / Failed **0** / transform skip (other types in extract) **140** among legacy **12708**. Id-map **1818**. PG App_Inv instances match live count below.
- **Log**: `artifacts/headless-import/AppInv-header-remainder-20260907-143753.log`
- **Next**: App_Inv roster (`ApplicationProfileInstancePerson`). Then progress, Invitation, Visa.
### 2026-09-07 - App_Inv header sample 2 .15 -> local PG (dev mapping check)

- **Phase**: import (sample)
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Inv --max-rows 2 --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Posted **2**, Failed **0**)
- **RunId**: `20260907-142745`
- **Samples**: `b4b4de0e-8446-4235-9f81-00018b6f0fa6` -> `86a5001f-6609-42fc-ad5f-bfbff7f39c9c` (10/-519); `4ec872f8-e623-42d8-947c-002330c9646e` -> `5515d5da-f2ff-41cf-a3da-fab0e58da1a8` (3/-2493)
- **Notes**: Person-domain complete; MedicalRecord file wave waits until after all type slices. Filter matched **1818** App_Inv rows; posting 2.
- **Halt**: wait for accept before App_Inv header remainder (then roster/progress/Invitation/Visa in inner sequence).
- **Log**: `artifacts/headless-import/AppInv-header-sample2-20260907-142745.log`
### 2026-09-07 - EmployeeSalary .15 -> local PG (proceed, no sample)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity EmployeeSalary --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-142415`
- **Counts**: Legacy SQL **3116** / Prepared **3053** / transform skip **63** / Posted **3053** / Failed **0**. Id-map **3053**. PG `"EmployeeSalaries"` matches live count below.
- **Log**: `artifacts/headless-import/EmployeeSalary-20260907-142415.log`
- **Next**: MedicalRecord (optional `--max-rows` mapping check). Do not import Visa in person-domain.
### 2026-09-07 - AddressOfResidence remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity AddressOfResidence --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-142153`
- **Counts**: Legacy SQL **4162** / Prepared **4159** / transform skip **3** / Posted **4157** / already **2** / Failed **0** / PIA-inferred skipped **1133** (already posted in sample). Id-map expanded. PG `"AddressesOfResidence"` matches live count below.
- **Log**: `artifacts/headless-import/AOR-remainder-20260907-142153.log`
- **Next**: EmployeeSalary (optional `--max-rows` mapping check). Do not import Visa in person-domain.
### 2026-09-07 - AddressOfResidence sample 2 .15 -> local PG (dev mapping check)

- **Phase**: import (sample)
- **Mode**: `--import-visa2014 --entity AddressOfResidence --max-rows 2 --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-141034`
- **Samples**: `c9eea490-041e-4b78-a869-0002be960915` -> `2eb4e60f-852d-40ce-8426-5be420192e20` (Seyhun AYMELEK, Patent/UÝJ→Lodging); `15e2ec8a-80b6-49cc-a298-00176be95572` -> `49ba66f9-4f1b-4d34-b6a9-01dcf7d6d96b` (Ertugrul Yıldız, Lojman+myhmanhana→Hotel)
- **Also**: PIA inference sub-pass posted **1133** (people with no legacy AOR child). Id-map **1253** (aliases). Remainder will skip these.
- **Halt**: wait for accept before AddressOfResidence remainder.
- **Log**: `artifacts/headless-import/AOR-sample2-20260907-141034.log`
### 2026-09-07 - EmployeePositionHistory remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity EmployeePositionHistory --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-140724`
- **Counts**: Legacy SQL **3167** / Prepared **3167** / Posted **3165** / already **2** / Failed **0** / ActualPositions created **4**. Id-map **3167**. PG `"EmployeePositionHistories"` matches live count below.
- **Log**: `artifacts/headless-import/EPH-remainder-20260907-140724.log`
- **Next**: EmployeeSalary or AddressOfResidence per person-domain order (optional `--max-rows` mapping check). Do not import Visa in person-domain.
### 2026-09-07 - EmployeePositionHistory sample 2 .15 -> local PG (dev mapping check)

- **Phase**: import (sample)
- **Mode**: `--import-visa2014 --entity EmployeePositionHistory --max-rows 2 --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Posted **2**, Failed **0**, ActualPositions created **0**)
- **RunId**: `20260907-135323`
- **Samples**: `0d2358c4-a2cf-497b-911c-00135e82172e` -> `b0ab1e6e-7baf-4322-a5fa-512624125878` (Yousrri Abid); `1f5d30c3-c165-4737-91da-003f5852c63b` -> `10bd806d-fcd5-4e6e-8cc4-b126cf94d6e4` (İldar Musin)
- **Halt**: wait for accept before EmployeePositionHistory remainder.
- **Log**: `artifacts/headless-import/EPH-sample2-20260907-135323.log`
### 2026-09-07 - Education remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Education --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Fail then fix**: RunId `20260907-130002` Posted **3277** / Failed **2** / already **2**. Gaps: institution `Bursa ş.orta mekdep` (catalog had `…mekdebi`); institution `Nacional Experimental De Los Planos Centrales Romulo Gallegos unıwersiteti`; specialty `Jemgyýetde hasapçylyk`. Seeded PG `EducationInstitutions`/`Specialties` + appended tenant JSON (calik + embedded).
- **Retry**: RunId `20260907-130350` Posted **2** / Failed **0** / already **3279**.
- **Outcome**: success (exit **0**, Failed **0**)
- **Counts**: Legacy SQL (extract joins) **3281** / Prepared **3281** / Posted **3277+2** / already sample **2** / Failed **0**. Id-map **3281**. PG `"Educations"` matches live count below.
- **Logs**: `artifacts/headless-import/Education-remainder-20260907-130002.log`, `Education-remainder-retry-20260907-130350.log`
- **Next**: EmployeePositionHistory (optional `--max-rows` mapping check). Do not import Visa in person-domain.
### 2026-09-07 - Education sample 2 .15 -> local PG (dev mapping check)

- **Phase**: import (sample)
- **Mode**: `--import-visa2014 --entity Education --max-rows 2 --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Posted **2**, Failed **0**)
- **RunId**: `20260907-124222`
- **Samples**: `53120792-5287-43bf-a4a6-000c307a11a6` -> `1590429f-2bab-46ab-b04d-4f1d2ea33cc0` (Hamit Helli); `d5364c84-0225-409a-acbb-0039b12da6eb` -> `00281532-9bf5-4c3f-8304-0d0e7073b5ee` (Adil Hürriyet ÖCAL)
- **Halt**: wait for accept before Education remainder.
- **Log**: `artifacts/headless-import/Education-sample2-20260907-124222.log`
### 2026-09-07 - Passport remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Passport --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **RunId**: `20260907-123337`
- **Counts**: Legacy SQL **3845** / Prepared **3764** / Posted **3762** / already **2** / Failed **0** / transform skip **79** / Dedupe merged **2**. Id-map expanded **3765**. PG `"Passports"` matches live count below.
- **Log**: `artifacts/headless-import/Passport-remainder-20260907-123337.log`
- **Next**: Education (optional `--max-rows` mapping check).
### 2026-09-07 - Passport sample 2 .15 -> local PG (dev mapping check)

- **Phase**: import (sample)
- **Mode**: `--import-visa2014 --entity Passport --max-rows 2 --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Posted **2**, Failed **0**)
- **RunId**: `20260907-121900`
- **Samples**: `b227435c-6748-4223-9851-00064c367939` -> `335c0543-062a-455e-9c8c-c3780a094e1b` (U22415835); `e98ad9e3-c0ed-40cc-8132-000c2a1581a4` -> `a85a4898-849b-4f5a-97c8-94cd127867f0` (U 08659668)
- **Halt**: wait for accept before Passport remainder.
- **Log**: `artifacts/headless-import/Passport-sample2-20260907-121900.log`
### 2026-09-07 - Person remainder .15 -> local PG (after accept)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Person --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **Fail then fix**: first remainder RunId `20260907-120642` hit `23505 IX_People_PersonalNumber` — main Person import did not skip id-map / did not persist map until the end, so the sample + ~1050 posted rows were inserted again. Killed PID; People sat at **1051**.
- **Fix**: skip existing Person id-map always; `TryFindPersonByIdentityAsync` relink by PN (or name+DOB for sentinel 0); flush id-map every 250.
- **RunId**: `20260907-121607`
- **Counts**: Legacy SQL **3409** / Prepared **3409** / Posted **2358** / Relinked **1050** / already **1** / Failed **0** / Dedupe suffixed **21**. PG `"People"` **3409**; id-map **3409**.
- **Log**: `artifacts/headless-import/Person-remainder-20260907-121607.log`
- **Next**: Passport (one BO at a time; optional `--max-rows 1` mapping check).
### 2026-09-07 - Person sample 1 .15 -> local PG (dev mapping check)

- **Phase**: import (sample)
- **Mode**: `--import-visa2014 --entity Person --max-rows 1 --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, Posted **1**, Failed **0**) after host fix
- **Fix**: `ApplicationProfileSeedGate` skipped on `VISA2026_HEADLESS_IMPORT`; moved after `UseXaf()` (template OnSaving / ValueManager). First attempt RunId `20260907-115417` failed startup.
- **RunId**: `20260907-115930`
- **Sample**: legacy `ae4a6a55-5e42-4a42-9e8e-0010b95d88e6` -> Visa2026 `7687af7c-cfa8-4313-aa62-09c681e775ff` (Murat ULAŞCAN)
- **Halt**: wait for accept / mapping suggestion / raise N. Do not import Person remainder until accept.
- **Log**: `artifacts/headless-import/Person-sample1-20260907-115930.log`
### 2026-09-07 - pilotThenHumanVerify: optional dev comparison table format

- **Phase**: strategy
- **Lock**: `import-strategy.yaml` `pilotThenHumanVerify` — development mapping check only (not Demo/Prod full Import). Sample N rows (`--max-rows`, default 1; reviewer may raise N).
- **Chat UI**: one Markdown table per sample row: Field | Legacy (VISA2015) | Imported (Visa2026). Identity header above. Lookups as labels. Mismatches bold + short list. Halt for accept or mapping suggestion.
- **Wipe note**: local PG transactional wipe 2026-09-07; `TRUNCATE BusinessTripAddress` CASCADE-cleared ApplicationProfiles — wipe SQL no longer truncates that table. Re-seed profiles (F5 / inprocess host) before Application slice.
- **Next**: Person remainder or Person `--max-rows 1` mapping check when reviewer asks.
### 2026-09-07 - FamilyProofDocument file wave .15 -> local PG (attachmentsSequence)

- **Phase**: file import
- **Mode**: `--import-visa2014-files --entity Person --property FamilyProofDocument --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**) — last wired attachmentsSequence step
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260907-112340`
- **Counts**: Person id-map **3404** / Legacy family-proof rows **450** / Posted PersonDocument **9** + PersonFamilyRelationDocument **437** (**446**) / Failed **0** / No person map **0** / No blob **0** / Oversize (>5MB) **1** / Duplicate blob **3** / already **0**. PG `"PersonDocuments"` **9**; `"PersonFamilyRelationDocuments"` **437**; id-map **446**.
- **Id-map**: `id-maps/calik-energi-local-pg/FamilyProofDocument.json`
- **Log**: `artifacts/document-copies-import/FamilyProofDocument-20260907-112340.log`
- **Next**: attachmentsSequence complete for local PG. Unwired (do not invent): RejectionDocument, BorderZoneDocument, MinistryLetterFile.
### 2026-09-07 - InvitationDocument file wave .15 -> local PG (attachmentsSequence)

- **Phase**: file import
- **Mode**: `--import-visa2014-files --entity Invitation --property InvitationDocument --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260907-111637`
- **Counts**: Invitation id-map **2938** / Legacy copy rows **3254** / Posted **3043** / Failed **0** / No parent map **206** / No blob **5** / Oversize **0** / already **0**. PG `"Invitations"` **2938**; `"InvitationDocuments"` **3043** (matches id-map).
- **No parent map**: copies whose Invitation Oid is not in Invitation.json (**2938**).
- **Id-map**: `id-maps/calik-energi-local-pg/InvitationDocument.json`
- **Log**: `artifacts/document-copies-import/InvitationDocument-20260907-111637.log`
- **Next**: FamilyProofDocument.
### 2026-09-07 - WorkPermitDocument file wave .15 -> local PG (attachmentsSequence)

- **Phase**: file import
- **Mode**: `--import-visa2014-files --entity WorkPermit --property WorkPermitDocument --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260907-111246`
- **Counts**: WorkPermit id-map **361** / Legacy copy rows **1024** / Posted **915** / Failed **0** / No parent map **107** / No blob **2** / Oversize **0** / already **0**. PG `"WorkPermits"` **361**; `"WorkPermitDocuments"` **915** (matches id-map).
- **No parent map**: copies whose WorkPermit Oid is not in the current type-slice WorkPermit.json (**361**, vs older full map **408**).
- **Id-map**: `id-maps/calik-energi-local-pg/WorkPermitDocument.json`
- **Log**: `artifacts/document-copies-import/WorkPermitDocument-20260907-111246.log`
- **Next**: InvitationDocument.
### 2026-09-07 - MedicalRecordDocument file wave .15 -> local PG (attachmentsSequence)

- **Phase**: file import
- **Mode**: `--import-visa2014-files --entity MedicalRecord --property MedicalRecordDocument --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**) — Çalik no-op as expected
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260907-111002`
- **Counts**: Person id-map **3404** / Spid link rows **2** / Importable (Copy+FileData) **0** / Posted **0** / Failed **0** / Orphan copy link **2**.
- **Log**: `artifacts/document-copies-import/MedicalRecordDocument-20260907-111002.log`
- **Next**: WorkPermitDocument.
### 2026-09-07 - EducationDocument file wave .15 -> local PG (attachmentsSequence)

- **Phase**: file import
- **Mode**: `--import-visa2014-files --entity Education --property EducationDocument --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260907-105928`
- **Counts**: Education id-map **3276** / Legacy diploma copy rows **4495** / Posted **4397** / Failed **0** / No education map **7** / No blob **34** / Oversize (>5MB) **40** / Duplicate blob **17** / already **0**.
- **Id-map**: `id-maps/calik-energi-local-pg/EducationDocument.json`
- **Log**: `artifacts/document-copies-import/EducationDocument-20260907-105928.log`
- **Next**: MedicalRecordDocument.
### 2026-09-07 - VisaDocument file wave .15 -> local PG (attachmentsSequence)

- **Phase**: file import
- **Mode**: `--import-visa2014-files --entity Visa --property VisaDocument --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260907-103843`
- **Counts**: Visa id-map **6318** / Rows with blob **6272** / Posted **6118** / Failed **0** / No visa map **72** / No blob **46** / Oversize (>5MB) **154** / already **0**.
- **Id-map**: `id-maps/calik-energi-local-pg/VisaDocument.json`
- **Log**: `artifacts/document-copies-import/VisaDocument-20260907-103843.log`
- **Next**: EducationDocument.
### 2026-09-07 - PassportDocument file wave .15 -> local PG (attachmentsSequence)

- **Phase**: file import
- **Mode**: `--import-visa2014-files --entity Passport --property PassportDocument --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260907-103028`
- **Counts**: Passport id-map **3760** / Legacy copy rows **3797** / Posted **3744** / Failed **0** / No passport map **15** / No blob **1** / Oversize (>5MB) **37** / already **0**.
- **Id-map**: `id-maps/calik-energi-local-pg/PassportCopy.json`
- **Log**: `artifacts/document-copies-import/PassportDocument-20260907-103028.log`
- **Next**: VisaDocument.
### 2026-09-07 - Person.Photo file wave .15 -> local PG (attachmentsSequence)

- **Phase**: file import
- **Mode**: `--import-visa2014-files --entity Person --property Photo --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50`
- **Outcome**: success (exit **0**, Failed **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260907-102710`
- **Lock**: `attachmentsSequence` in `application-type-import-order.yaml` (developer 2026-09-07): Photo → PassportDocument → VisaDocument → EducationDocument → MedicalRecordDocument → WorkPermitDocument → InvitationDocument → FamilyProofDocument. Unwired: RejectionDocument, BorderZoneDocument.
- **Counts**: Id-map **3404** / Processed **3404** / Patched **3333** / No blob **71** / Failed **0**.
- **Log**: `artifacts/document-copies-import/Person-Photo-20260907-102710.log`
- **Next**: PassportDocument.
### 2026-09-07 - Visa remainder .15 -> local PG (postAllTypeSlices)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Visa --visa-remainder --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260907-102109`
- **Counts**: Legacy SQL **6368** / Prepared **6343** / Posted **609** (remainder without issuing **607**, leftover with issuing **2**) / Failed **0** / already **5709** / no Passport map **25** / transform skip **19** / dedupe **6** / issuing patched **0**. PG `"Visas"` **6318**; Passport FK null **0**; issuing instance null **2085** (was 1478 before remainder +607).
- **CLI**: `--visa-remainder` posts visas whose issuing Application.Oid is not in the instance id-map (null `IssuingApplicationProfileInstance`; `MigrationImportContext` skips the officer create-from-instance rule).
- **Id-map**: `id-maps/calik-energi-local-pg/Visa.json` (**6318**)
- **Log**: `artifacts/headless-import/Visa-remainder-20260907-102109.log`
- **Next**: attachments (VisaDocument / file wave). postAllTypeSlices scalar complete (Rejection, BorderZone, Visa remainder).
### 2026-09-07 - BorderZoneItem .15 -> local PG (postAllTypeSlices)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity BorderZoneItem --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260907-095952`
- **Counts**: Legacy SQL **218** / Prepared **212** / Posted **209** / Failed **0** / transform skip **6** (parent header skipped) / missing required id-map **3** (Passport not in Person/Passport maps) / already **0**. PG `"BorderZoneItems"` **209**; BorderZone/Person/Passport FK null **0**.
- **Passport id-map misses** (3 PersonInApplication Oids): `76fe4a95-b8d3-4bee-9a77-35794649ec4e`, `b00c6e1d-29da-4176-8d3a-36a4289e37b5`, `c58b003d-32f8-4d44-8b24-8014c64a2c2b`
- **Id-map**: `id-maps/calik-energi-local-pg/BorderZoneItem.json`
- **Log**: `artifacts/headless-import/BorderZoneItem-20260907-095952.log`
- **Next**: Visa remainder (`when: no_issuing_instance`). Then attachments.
### 2026-09-07 - BorderZone headers .15 -> local PG (postAllTypeSlices)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity BorderZone --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260907-095609`
- **Counts**: Prepared **110** / Posted **110** / Failed **0** / transform skip **1** / already **0** / missing instance id-map **0**. PG `"BorderZones"` **110**; instance FK null **0**; ValidityDuration FK null **0**.
- **Id-map**: `id-maps/calik-energi-local-pg/BorderZone.json`
- **Log**: `artifacts/headless-import/BorderZone-20260907-095609.log`
- **Next**: BorderZoneItem after user approval (preview 212 / skip 6). Then Visa remainder.

### 2026-09-07 - BorderZone issued-document discovery + Excel preview

- **Phase**: discovery + Excel preview (no OData POST — importConfirmed still false)
- **Mode**: `--export-visa2014-preview --entity BorderZone|BorderZoneItem --legacy-source calik-energi-local-pg`
- **Outcome**: success (exit **0**)
- **Environment**: preview from `10.100.128.15` / `VISA2015` (no Visa2026 writes)
- **Finding**: VISA2014 has **no** issued BorderZone letter table. `dbo.BorderZoneForVisa` is the bit matrix already on instances. Synthesize from **App_Border_Zone_Permission** (`E:11`) Application rows.
- **Headers**: legacy **111** / import **110** / skip **1** (`ProcessNumber` = Imza atilmadi…, ProcessDate null). ProcessNumber → BorderZoneNumber (AS/CO); ProcessDate → StartDate; VisaPeriod.CountMonth → closest ValidityDuration 90/180/365.
- **Items**: legacy **218** / import **212** / skip **6** (parent header skipped).
- **Files**: `legacy/visa2014/preview-export/BorderZone-preview.calik-energi.xlsx`, `BorderZoneItem-preview.calik-energi.xlsx`
- **Next**: human review Excel → `importConfirmed: true` → implement `--entity BorderZone` POST. Then items. Then Visa remainder. Do not POST until confirmed.

### 2026-09-07 - RejectionItem .15 -> local PG (postAllTypeSlices)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity RejectionItem --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260907-094314`
- **Counts**: Prepared **254** / Posted **254** / Failed **0** / missing id-map **0** / already **0**. PG `"RejectionItems"` **254**; Rejection/Person/Passport FK null **0**.
- **Id-map**: archived stale July `RejectionItem.json`, wrote `id-maps/calik-energi-local-pg/RejectionItem.json`
- **Log**: `artifacts/headless-import/RejectionItem-20260907-094314.log`
- **Next**: Rejection slice complete. Next: BorderZone documents, then Visa remainder.

### 2026-09-07 - Rejection headers .15 -> local PG (postAllTypeSlices)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Rejection --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260907-094004`
- **Counts**: Prepared **207** / Posted **207** / Failed **0** / already **0** / missing instance id-map **0**. PG `"Rejections"` **207**; ApplicationProfileInstance FK null **0**.
- **Fixes before POST**: payload now sets `ApplicationProfileInstance` (not retired `Application`); archived stale July `Rejection.json` (live table was empty after instance reimport); set `ProduceRejection=true` on 11 Calik profiles that have ApplicationType `ShowRejections` (tenant JSON + live PG) so the save rule can pass.
- **Id-map**: `id-maps/calik-energi-local-pg/Rejection.json`
- **Log**: `artifacts/headless-import/Rejection-20260907-094004.log`
- **Next**: RejectionItem after user approval. Then BorderZone documents, then Visa remainder.

### 2026-09-07 - ApplicationProfileInstance App_Business_Trip_Arrival headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --inprocess --no-wait --batch-size 50 --entity ApplicationProfileInstance --application-type App_Business_Trip_Arrival` (`calik-energi-local-pg`)
- **Outcome**: success (exit **0**, FailedCount **0**) — empty skip
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260907-093248`
- **Headers**: Posted **0** / already **0**. Filter prepared **0** matches. Composite E:14 has no live Çalik rows. Instance id-map unchanged **12550**.
- **Logs**: `artifacts/headless-import/ApplicationProfileInstance-App_Business_Trip_Arrival-20260907-093248.log`
- **Next**: App_Business_Trip_Arrival skipped (no roster/progress). Band 7 complete. Wait for lock on postAllTypeSlices (Rejection, BorderZone documents, Visa remainder).

### 2026-09-07 - App_Business_Trip_Departure roster+progress .15 -> local PG

- **Phase**: import
- **Mode**: sequential `--import-visa2014 --inprocess --no-wait --batch-size 50` for ApplicationProfileInstancePerson then ApplicationProfileInstanceProgress (`calik-energi-local-pg`; instance id-map **12550**; Invitation/WorkPermit/Visa skipped)
- **Outcome**: success (each wave exit **0**, FailedCount **0**, `CHAIN_EXIT=0`)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260907-093026`
- **Roster**: Posted **731** / already **21807** / missing id-map **62**. PG people **22540** (this type **731**); instance FK null **0**. ResolvedLinks **107100**.
- **Progress**: Posted **1166** / already **37662** / no instance map **84**. PG live **38828** (this type **1166**); FK null **0**.
- **Logs**: `artifacts/headless-import/*-App_Business_Trip_Departure-20260907-093026.log`
- **Next**: App_Business_Trip_Departure inner sequence complete. Next locked type App_Business_Trip_Arrival — wait for header approval.

### 2026-09-07 - ApplicationProfileInstance App_Business_Trip_Departure headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Business_Trip_Departure --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Prepared **594** / Posted **594** / Failed **0** / transform skip **140** / already **0**. Composite `E:13`.
- **Profile lock**: all **594** on **Iş Saparyna Gitmek** (`business_trip_departure`); profile FK null **0**.
- **Reconcile**: PG `"ApplicationProfileInstances"` live **12550**; App_Business_Trip_Departure **594**.
- **Id-map**: **12550** keys copied to source
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Business_Trip_Departure-20260907-092902.log`
- **Next**: ApplicationProfileInstancePerson roster only after user approval. Then progress. Skip Invitation, WorkPermit, Visa.

### 2026-09-07 - ApplicationProfileInstance App_Reg_Check_Out_Internal skip empty .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Reg_Check_Out_Internal --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (importer exit **0**, FailedCount **0**). Empty type.
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Prepared **0** / Posted **0** / Failed **0** / transform skip **140**. No Çalik composites in the profile lock (`check_out_internal`).
- **Reconcile**: PG instances still **11956**; `check_out_internal` **0**. Band 6 registration complete.
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Reg_Check_Out_Internal-20260907-092738.log`
- **Next**: Skip roster/progress. Wait to lock App_Business_Trip_Departure then App_Business_Trip_Arrival. Then postAllTypeSlices.

### 2026-09-07 - ApplicationProfileInstance App_Reg_Info_Change_Visa skip empty .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Reg_Info_Change_Visa --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (importer exit **0**, FailedCount **0**). Empty type.
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Prepared **0** / Posted **0** / Failed **0** / transform skip **140**. Composite `E:5:na:na:2`.
- **Reconcile**: PG instances still **11956**; `reg_info_change_visa` **0**. Address **647**, passport **101** unchanged.
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Reg_Info_Change_Visa-20260907-092608.log`
- **Next**: Skip roster/progress. Wait to lock App_Reg_Check_Out_Internal (profile lock empty composites) then business trip.

### 2026-09-07 - App_Reg_Info_Change_Passport roster+progress .15 -> local PG

- **Phase**: import
- **Mode**: sequential `--import-visa2014 --inprocess --no-wait --batch-size 50` for ApplicationProfileInstancePerson then ApplicationProfileInstanceProgress (`calik-energi-local-pg`; instance id-map **11956**; Invitation/WorkPermit/Visa skipped)
- **Outcome**: success (each wave exit **0**, FailedCount **0**, `CHAIN_EXIT=0`)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260907-092403`
- **Roster**: Posted **116** / already **21691** / missing id-map **793**. PG people **21809** (this type **116**); instance FK null **0**. ResolvedLinks **104928**.
- **Progress**: Posted **202** / already **37460** / no instance map **1250**. PG live **37662** (this type **202**); FK null **0**.
- **Logs**: `artifacts/headless-import/*-App_Reg_Info_Change_Passport-20260907-092403.log`
- **Next**: App_Reg_Info_Change_Passport inner sequence complete. Next locked type App_Reg_Info_Change_Visa — wait for header approval.

### 2026-09-07 - ApplicationProfileInstance App_Reg_Info_Change_Passport headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Reg_Info_Change_Passport --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Prepared **101** / Posted **101** / Failed **0** / transform skip **140** / already **0**. Composite `E:5:na:na:1`.
- **Profile lock**: all **101** on **Hasaba alyş maglumatyň üýtgemegi (Pasport Çalışmagy)** (`reg_info_change_passport`); profile FK null **0**.
- **Reconcile**: PG `"ApplicationProfileInstances"` live **11956**; App_Reg_Info_Change_Passport **101**; App_Reg_Info_Change_Address still **647**. Visa info-change still **0**.
- **Id-map**: **11956** keys copied to source
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Reg_Info_Change_Passport-20260907-092235.log`
- **Next**: ApplicationProfileInstancePerson roster only after user approval. Then progress. Skip Invitation, WorkPermit, Visa.

### 2026-09-07 - App_Reg_Info_Change_Address roster+progress .15 -> local PG

- **Phase**: import
- **Mode**: sequential `--import-visa2014 --inprocess --no-wait --batch-size 50` for ApplicationProfileInstancePerson then ApplicationProfileInstanceProgress (`calik-energi-local-pg`; instance id-map **11855**; Invitation/WorkPermit/Visa skipped)
- **Outcome**: success (each wave exit **0**, FailedCount **0**, `CHAIN_EXIT=0`)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260907-092001`
- **Roster**: Posted **830** / already **20861** / missing id-map **909**. PG people **21693** (this type **830**); instance FK null **0**. ResolvedLinks **104348**.
- **Progress**: Posted **1294** / already **36166** / no instance map **1452**. PG live **37460** (this type **1294**); FK null **0**.
- **Logs**: `artifacts/headless-import/*-App_Reg_Info_Change_Address-20260907-092001.log`
- **Next**: App_Reg_Info_Change_Address inner sequence complete. Next locked type App_Reg_Info_Change_Passport — wait for header approval.

### 2026-09-07 - ApplicationProfileInstance App_Reg_Info_Change_Address headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Reg_Info_Change_Address --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Prepared **647** / Posted **647** / Failed **0** / transform skip **140** / already **0**. Composites `E:5` + `F:5` (address; not `:1` passport or `:2` visa).
- **Profile lock**: all **647** on **Hasaba alyş maglumatyň üýtgemegi (Salgy Çalyşmagy)** (`reg_info_change_address`); profile FK null **0**.
- **Reconcile**: PG `"ApplicationProfileInstances"` live **11855**; App_Reg_Info_Change_Address **647**; App_Reg_Check_Out still **2200**. Passport/visa info-change still **0**.
- **Id-map**: **11855** keys copied to source
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Reg_Info_Change_Address-20260907-091819.log`
- **Next**: ApplicationProfileInstancePerson roster only after user approval. Then progress. Skip Invitation, WorkPermit, Visa.

### 2026-09-07 - App_Reg_Check_Out roster+progress .15 -> local PG

- **Phase**: import
- **Mode**: sequential `--import-visa2014 --inprocess --no-wait --batch-size 50` for ApplicationProfileInstancePerson then ApplicationProfileInstanceProgress (`calik-energi-local-pg`; instance id-map **11208**; Invitation/WorkPermit/Visa skipped)
- **Outcome**: success (each wave exit **0**, FailedCount **0**, `CHAIN_EXIT=0`)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260907-091138`
- **Roster**: Posted **3418** / already **17443** / missing id-map **1739**. PG people **20863** (this type **3418**); instance FK null **0**. ResolvedLinks **100585**.
- **Progress**: Posted **4330** / already **31836** / no instance map **2746**. PG live **36166** (this type **4330**); FK null **0**.
- **Logs**: `artifacts/headless-import/*-App_Reg_Check_Out-20260907-091138.log`
- **Next**: App_Reg_Check_Out inner sequence complete. Next locked type App_Reg_Info_Change_Address — wait for header approval.

### 2026-09-07 - ApplicationProfileInstance App_Reg_Check_Out headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Reg_Check_Out --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Prepared **2200** / Posted **2200** / Failed **0** / transform skip **140** / already **0**. Composites `E:6` + `F:6`.
- **Profile lock**: all **2200** on **Hasapdan Çykarmak (Daşary ýurda gitmegi)** (`check_out`); profile FK null **0**.
- **Reconcile**: PG `"ApplicationProfileInstances"` live **11208**; App_Reg_Check_Out **2200**; App_Reg_ext **1243**; App_Reg_Check_In **1808**; App_Reg_Check_In_Internal **490**.
- **Id-map**: **11208** keys copied to source
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Reg_Check_Out-20260907-090953.log`
- **Next**: ApplicationProfileInstancePerson roster only after user approval. Then progress. Skip Invitation, WorkPermit, Visa.

### 2026-09-07 - App_Reg_ext roster+progress .15 -> local PG

- **Phase**: import
- **Mode**: sequential `--import-visa2014 --inprocess --no-wait --batch-size 50` for ApplicationProfileInstancePerson then ApplicationProfileInstanceProgress (`calik-energi-local-pg`; instance id-map **9008**; Invitation/WorkPermit/Visa skipped)
- **Outcome**: success (each wave exit **0**, FailedCount **0**, `CHAIN_EXIT=0`)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260907-090458`
- **Roster**: Posted **2661** / already **14782** / missing id-map **5157**. PG people **17445** (this type **2656**; **5** catch-up on prior types); instance FK null **0**. ResolvedLinks **84311**.
- **Progress**: Posted **2482** / already **29354** / no instance map **7076**. PG live **31836** (this type **2482**); FK null **0**.
- **Logs**: `artifacts/headless-import/*-App_Reg_ext-20260907-090458.log`
- **Next**: App_Reg_ext inner sequence complete. Next locked type App_Reg_Check_Out — wait for header approval.

### 2026-09-07 - ApplicationProfileInstance App_Reg_ext headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Reg_ext --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Prepared **1243** / Posted **1243** / Failed **0** / transform skip **140** / already **0**. Composites `E:3` + `F:3`.
- **Profile lock**: all **1243** on **Hasaba alyşy uzaltmak** (`reg_extension`); profile FK null **0**.
- **Reconcile**: PG `"ApplicationProfileInstances"` live **9008**; App_Reg_ext **1243**; App_Reg_Check_In **1808**; App_Reg_Check_In_Internal **490**.
- **Id-map**: **9008** keys copied to source
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Reg_ext-20260907-085847.log`
- **Next**: ApplicationProfileInstancePerson roster only after user approval. Then progress. Skip Invitation, WorkPermit, Visa.

### 2026-09-05 - App_Reg_Check_In_Internal roster+progress .15 -> local PG

- **Phase**: import
- **Mode**: sequential `--import-visa2014 --inprocess --no-wait --batch-size 50` for ApplicationProfileInstancePerson then ApplicationProfileInstanceProgress (`calik-energi-local-pg`; instance id-map **7765**; Invitation/WorkPermit/Visa skipped)
- **Outcome**: success (each wave exit **0**, FailedCount **0**, `CHAIN_EXIT=0`)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260905-125648`
- **Roster**: Posted **611** / already **14172** / missing id-map **7808**. PG people **14784** (this type **611**); instance FK null **0**. ResolvedLinks **72032**.
- **Progress**: Posted **980** / already **28374** / no instance map **9554**. PG live **29354** (this type **980**); FK null **0**.
- **Logs**: `artifacts/headless-import/*-App_Reg_Check_In_Internal-20260905-125648.log`
- **Next**: App_Reg_Check_In_Internal inner sequence complete. Next locked type App_Reg_ext — wait for header approval.

### 2026-09-05 - ApplicationProfileInstance App_Reg_Check_In_Internal headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Reg_Check_In_Internal --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (importer exit **0**, FailedCount **0**). First attempt failed: DataImporter.exe missing after F5; rebuilt with `dotnet build Visa2026.DataImporter.csproj -c Debug /p:BuildProjectReferences=false`.
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Prepared **490** / Posted **490** / Failed **0** / transform skip **140** / already **0**. Composites `E:4` + `F:4`.
- **Profile lock**: all **490** on **Hasaba Almak (Welaýatdan gelmegi sebäpli)** (`check_in_internal`); profile FK null **0**.
- **Reconcile**: PG `"ApplicationProfileInstances"` live **7765**; App_Reg_Check_In_Internal **490**; App_Reg_Check_In still **1808**.
- **Id-map**: **7765** keys copied to source
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Reg_Check_In_Internal-20260905-125122.log`
- **Next**: ApplicationProfileInstancePerson roster only after user approval. Then progress. Skip Invitation, WorkPermit, Visa.

### 2026-09-05 - App_Reg_Check_In roster+progress .15 -> local PG

- **Phase**: import
- **Mode**: sequential `--import-visa2014 --inprocess --no-wait --batch-size 50` for ApplicationProfileInstancePerson then ApplicationProfileInstanceProgress (`calik-energi-local-pg`; instance id-map **7275**; Invitation/WorkPermit/Visa skipped)
- **Outcome**: success (each wave exit **0**, FailedCount **0**, `CHAIN_EXIT=0`)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260905-124321`
- **Roster**: Posted **3123** / already **11049** / missing id-map **8419**. PG people **14173** (this type **3123**); instance FK null **0**. ResolvedLinks **69098**.
- **Progress**: Posted **3608** / already **24766** / no instance map **10534**. PG live **28374** (this type **3608**); FK null **0**.
- **Logs**: `artifacts/headless-import/*-App_Reg_Check_In-20260905-124321.log`
- **Next**: App_Reg_Check_In inner sequence complete. Next locked type App_Reg_Check_In_Internal — wait for header approval.

### 2026-09-05 - ApplicationProfileInstance App_Reg_Check_In headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Reg_Check_In --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Prepared **1808** / Posted **1808** / Failed **0** / transform skip **140** / already **0**. Composites `E:2` + `F:2`.
- **Profile lock**: all **1808** on **Hasaba Almak (Daşary ýurtdan gelmegi sebäpli)** (`check_in_from_abroad`); profile FK null **0**.
- **Reconcile**: PG `"ApplicationProfileInstances"` live **7275**; App_Reg_Check_In **1808**; App_Sevice_Passport still **44**.
- **Id-map**: **7275** keys copied to source
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Reg_Check_In-20260905-124144.log`
- **Next**: ApplicationProfileInstancePerson roster only after user approval. Then progress. Skip Invitation, WorkPermit, Visa.

### 2026-09-05 - App_Sevice_Passport roster+progress+Invitation+Visa chain .15 -> local PG

- **Phase**: import
- **Mode**: sequential `--import-visa2014 --inprocess --no-wait --batch-size 50` for ApplicationProfileInstancePerson, ApplicationProfileInstanceProgress, Invitation, InvitationItem, Visa (`calik-energi-local-pg`; instance id-map **5467**; WorkPermit skipped)
- **Outcome**: success (each wave exit **0**, FailedCount **0**, `CHAIN_EXIT=0`)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260905-123810`
- **Roster**: Posted **52** / already **10997** / missing id-map **11542**. PG people **11050** (this type **52**); instance FK null **0**. ResolvedLinks **54297**.
- **Progress**: Posted **209** / already **24557** / no instance map **14142**. PG live **24766** (this type **209**); FK null **0**.
- **Invitation**: Posted **34** / already **2904** / skip instance map **2**. PG live **2938** (this type **34**); instance FK null **0**. 44 headers vs 34 invitations: remaining cases have no legacy Invitation row.
- **InvitationItem**: Posted **41** / already **5228** / missing id-map **30**. PG live **5269** (this type **41**); header FK null **0**.
- **Visa**: Posted **13** / already **5696** / no Passport **25** / skip issuing **605**. PG live **5709**; **0** with issuing profile `get_invitation_service_passport` (13 catch-up on prior types). Issuing FK null **1478** (remainder). Step still required.
- **Logs**: `artifacts/headless-import/*-App_Sevice_Passport-20260905-123810.log`
- **Next**: App_Sevice_Passport inner sequence complete. Next locked type App_Reg_Check_In — wait for header approval. Then remaining registration, then business trip.

### 2026-09-05 - ApplicationProfileInstance App_Sevice_Passport headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Sevice_Passport --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Prepared **44** / Posted **44** / Failed **0** / transform skip **140** / already **0**. Composites `E:10` + `F:10`.
- **Profile lock**: all **44** on **Gulluk Pasporty Üçin Çakylyk Almak** (`get_invitation_service_passport`); profile FK null **0**.
- **Reconcile**: PG `"ApplicationProfileInstances"` live **5467**; App_Sevice_Passport **44**.
- **Id-map**: **5467** keys copied to source
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Sevice_Passport-20260905-123042.log`
- **Lock order**: App_Sevice_Passport first, then all Registration, then Business trip.
- **Next**: ApplicationProfileInstancePerson roster only after user approval. Then progress, Invitation + InvitationItem, Visa. Skip WorkPermit.

### 2026-09-05 - ApplicationProfileInstance App_Cancel_BZ skip empty .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Cancel_BZ --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (importer exit **0**, FailedCount **0**). Empty type.
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Prepared **0** / Posted **0** / Failed **0** / transform skip **140**. No Çalik composites in the profile lock (`cancel_borderzone`).
- **Reconcile**: PG instances still **5423**; `cancel_borderzone` **0**. Band 4 complete.
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Cancel_BZ-20260905-121448.log`
- **Next**: Skip roster/progress. Wait to lock App_Sevice_Passport (live E:10 + F:10, previously deferred), a registration/business-trip type, or postAllTypeSlices.

### 2026-09-05 - ApplicationProfileInstance App_Cancel_Inv skip empty .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Cancel_Inv --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (importer exit **0**, FailedCount **0**). Empty type.
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Prepared **0** / Posted **0** / Failed **0** / transform skip **140**. No Çalik composites in the profile lock (`cancel_invitation`).
- **Reconcile**: PG instances still **5423**; `cancel_invitation` **0**.
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Cancel_Inv-20260905-121245.log`
- **Next**: Skip roster/progress. Wait to lock App_Cancel_BZ (likely empty) or App_Sevice_Passport / registration / postAllTypeSlices.

### 2026-09-05 - App_Cancell_WP roster+progress .15 -> local PG

- **Phase**: import
- **Mode**: sequential `--import-visa2014 --inprocess --no-wait --batch-size 50` for ApplicationProfileInstancePerson then ApplicationProfileInstanceProgress (`calik-energi-local-pg`; instance id-map **5423**; Invitation/WorkPermit/Visa skipped)
- **Outcome**: success (each wave exit **0**, FailedCount **0**, `CHAIN_EXIT=0`)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260905-120700`
- **Roster**: Posted **196** / already **10801** / missing id-map **11575**. PG people **10998** (this type **196**); instance FK null **0**. Id-map **10998**. ResolvedLinks **54141**.
- **Progress**: Posted **176** / already **24381** / no instance map **14349**. PG live **24557** (this type **176**); FK null **0**. Id-map **24557**.
- **Logs**: `artifacts/headless-import/*-App_Cancell_WP-20260905-120700.log`
- **Next**: App_Cancell_WP inner sequence complete. Wait to lock remaining types (App_Cancel_Inv / App_Cancel_BZ empty Çalik; App_Sevice_Passport deferred) or postAllTypeSlices. Remainder Visa and BorderZone documents after all types.
### 2026-09-05 - ApplicationProfileInstance App_Cancell_WP headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Cancell_WP --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (importer exit **0**, FailedCount **0**). DataImporter.exe missing after F5; rebuilt with `BuildProjectReferences=false`.
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy SQL filter match Prepared **88** / Posted **88** / Failed **0** / Skipped (transform) **140** / already **0**. Composite `E:22` (lock also lists `E:31`).
- **Profile lock**: all **88** on **Iş Rugsatnamany Ýatyrmak** (`cancel_workpermit`); none on `change_workpermit`. Profile FK null **0**.
- **Reconcile**: PG `"ApplicationProfileInstances"` live **5423**; App_Cancell_WP **88**. Prior cancel types unchanged (cancel_visa_wp **327**, cancel_visa **56**).
- **Id-map**: **5423** keys copied to source (450112 bytes)
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Cancell_WP-20260905-120520.log`
- **Next**: ApplicationProfileInstancePerson roster only after user approval. Then progress. Skip Invitation, WorkPermit, Visa.
### 2026-09-05 - App_Cancel_Visa roster+progress .15 -> local PG

- **Phase**: import
- **Mode**: sequential `--import-visa2014 --inprocess --no-wait --batch-size 50` for ApplicationProfileInstancePerson then ApplicationProfileInstanceProgress (`calik-energi-local-pg`; instance id-map **5335**; Invitation/WorkPermit/Visa skipped)
- **Outcome**: success (each wave exit **0**, FailedCount **0**, `CHAIN_EXIT=0`)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260905-115557`
- **Roster**: Posted **115** / already **10686** / missing id-map **11771**. PG people **10802** (this type **115**); instance FK null **0**. Id-map **10802**. ResolvedLinks **53363**.
- **Progress**: Posted **108** / already **24273** / no instance map **14525** / parent-skipped **162**. PG live **24381** (this type **108**); FK null **0**. Id-map **24381**.
- **Logs**: `artifacts/headless-import/*-App_Cancel_Visa-20260905-115557.log`
- **Next**: App_Cancel_Visa inner sequence complete. Wait to lock App_Cancell_WP. Remainder Visa and BorderZone documents after all types.
### 2026-09-05 - ApplicationProfileInstance App_Cancel_Visa headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Cancel_Visa --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (importer exit **0**, FailedCount **0**). DataImporter.exe missing after F5; rebuilt with `BuildProjectReferences=false`.
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy SQL filter match Prepared **56** / Posted **56** / Failed **0** / Skipped (transform) **140** / already **0**. Composites `E:21` + `F:21`.
- **Profile lock**: all **56** on **Wizany Ýatyrmak** (`cancel_visa`); profile FK null **0**.
- **Reconcile**: PG `"ApplicationProfileInstances"` live **5335**; App_Cancel_Visa **56**; App_Cancel_Visa_and_WP still **327**.
- **Id-map**: **5335** keys copied to source (442808 bytes)
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Cancel_Visa-20260905-115422.log`
- **Next**: ApplicationProfileInstancePerson roster only after user approval. Then progress. Skip Invitation, WorkPermit, Visa.
### 2026-09-05 - App_Cancel_Visa_and_WP roster+progress .15 -> local PG

- **Phase**: import
- **Mode**: sequential `--import-visa2014 --inprocess --no-wait --batch-size 50` for ApplicationProfileInstancePerson then ApplicationProfileInstanceProgress (`calik-energi-local-pg`; instance id-map **5279**; Invitation/WorkPermit/Visa skipped)
- **Outcome**: success (each wave exit **0**, FailedCount **0**, `CHAIN_EXIT=0`)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260905-112646`
- **Roster**: Posted **621** / already **10065** / missing id-map **11886**. PG people **10687** (this type **621**); instance FK null **0**. Id-map **10687**. ResolvedLinks **53053**.
- **Progress**: Posted **654** / already **23619** / no instance map **14633** / parent-skipped **162**. PG live **24273** (this type **654**); FK null **0**. Id-map **24273**.
- **Logs**: `artifacts/headless-import/*-App_Cancel_Visa_and_WP-20260905-112646.log`
- **Next**: App_Cancel_Visa_and_WP inner sequence complete. Wait to lock next type. Remainder Visa and BorderZone documents after all types.
### 2026-09-05 - ApplicationProfileInstance App_Cancel_Visa_and_WP headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Cancel_Visa_and_WP --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (importer exit **0**, FailedCount **0**). First attempt failed: DataImporter.exe missing after VS F5; rebuilt with `dotnet build Visa2026.DataImporter.csproj -c Debug /p:BuildProjectReferences=false`.
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy SQL filter match Prepared **327** / Posted **327** / Failed **0** / Skipped (transform) **140** / already **0**. Composite `E:12:na:na:na`.
- **Profile lock**: all **327** on **Wiza we Iş Rugsatnamany Ýatyrmak** (`cancel_visa_wp`); profile FK null **0**.
- **Reconcile**: PG `"ApplicationProfileInstances"` live **5279**; App_Cancel_Visa_and_WP **327**. Prior types unchanged.
- **Id-map**: **5279** keys copied to source (438160 bytes)
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Cancel_Visa_and_WP-20260905-112200.log` (orchestrator also `artifacts/local-pg-import/data/import-logs/`)
- **Next**: ApplicationProfileInstancePerson roster only after user approval. Then progress. Skip Invitation, WorkPermit, Visa.
### 2026-09-05 - App_Additional_WP_location MovementPermitLocation from linked WorkPermit

- **Phase**: import mapping + correction
- **Strategy**: `import-strategy.yaml` `additionalWpLocationMovementPermitFromWorkPermit`. Header GoşmaçaIşlemägeRugsatÝeri first; if empty, majority `PersonInApplication.WorkPermit.WorkPermitLocation` bits (`WorkPermittedLocationName`). Do not overwrite a filled header. Header reimport of this type runs the same backfill. CLI `--correct-visa2014-movement-permit-location`.
- **Outcome**: unit tests **2** passed. Local PG correction exit **0**: updated **228**, already **52**, no fallback **1**. Live filled **280 / 281**. `4/-11741` now `Akbugdaý etraby, Mary etraby`.
- **Log**: `artifacts/headless-import/MovementPermitLocationCorrection-20260905.log`
- **Next**: Process number still has no legacy source. App_Cancel_Visa_and_WP headers after approval.
### 2026-09-05 - App_Additional_WP_location missing ProcessNumber / work-permit location

- **Phase**: discovery (local PG case 4/-11741 Mehmet Gökhan ÖZDEMİR)
- **UI**: profile `change_workpermit` requires Work permit location + Process number (readiness red). Header importer maps location from `Application.GoşmaçaIşlemägeRugsatÝeri` only; ProcessNumber is deferred to progress from `dbo.Application.ProcessNumber`.
- **Legacy ProcessNumber**: **0 / 281** enum-15 apps. ProcessDate **280 / 281**. Ministry/construction document numbers also **0**. Nothing to copy onto Visa2026 `ProcessNumber` (belgi). 4/-11741 ProcessNumber NULL, ProcessDate 2018-04-25.
- **Legacy location**: header FK **52 / 281** (matches PG `MovementPermitLocation` filled **52**). PIA.WorkPermit.WorkPermitLocation **277 / 281** apps (414 / 430 PIA rows). Only **1** app has neither. 4/-11741: header FK null; PIA WorkPermit `137/4` location OID present.
- **Importer**: already copied the 52 header names. Did not backfill from WorkPermitLocation bit matrix onto the instance.
- **Next**: wait for lock — (a) backfill instance MovementPermitLocation from linked WP location bits; (b) relax `RequireProcessNumber` / ShowProcessNumber for this profile, or invent a belgi (no legacy source).
### 2026-09-05 - Lock band 4 cancel types (screenshot order)

- **Phase**: strategy lock
- **Mode**: fill `application-type-import-order.yaml` band `4-no-issued-documents` with the five names from the ApplicationType list
- **Live (not_started)**: App_Cancel_Visa_and_WP (`E:12:na:na:na`, ~327 employee apps; profile `cancel_visa_wp`). Header/roster/progress only; skip Invitation, WorkPermit, Visa.
- **Skip empty**: App_Cancel_Inv_WP, App_Cancel_App, App_Cancel_Visa_Ext, App_Cancel_Visa_and_WP_Ext (no Çalik composites in the profile lock; VISA2015 enum 12/21/22 only).
- **Enum vs ID**: enum **12** / ID **11** = cancel visa+WP. Enum **11** / ID **12** = App_Border_Zone_Permission (already imported). Do not remap ID 12 onto cancel.
- **Not in this lock**: App_Cancel_Visa (live E:21 ~11 + F:21 ~45), App_Cancell_WP (live E:22 / ID 31 ~88), App_Cancel_Inv, App_Cancel_BZ.
- **Next**: App_Cancel_Visa_and_WP headers after approval.
### 2026-09-05 - App_Additional_WP_location roster+progress+WP .15 -> local PG

- **Phase**: import
- **Mode**: sequential `--import-visa2014 --inprocess --no-wait --batch-size 50` for ApplicationProfileInstancePerson, ApplicationProfileInstanceProgress, WorkPermit, WorkPermitItem (`calik-energi-local-pg`; instance id-map **4952**; Invitation and Visa skipped)
- **Outcome**: success (each wave exit **0**, FailedCount **0**, `CHAIN_EXIT=0`)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260905-104751`
- **Roster**: Posted **430** / already **9635** / missing id-map **12505**. PG people **10066** (this type **430**); instance FK null **0**. Id-map **10066**.
- **Progress**: Posted **1401** / already **22218** / no instance map **15281** / parent-skipped **163**. PG live **23619** (this type **1401**); FK null **0**. Id-map **23619**.
- **WorkPermit**: Posted **0** / already **361** / instance not in id-map **49**. PG live **361** (get_invitation_wp **227** + extend_visa_wp **134**); none on `change_workpermit`. FK null **0**.
- **WorkPermitItem**: Posted **0** / already **3798** / missing id-map **2699**. PG live **3798**. Headers for additional-location apps are WorkPermitLetter rows that already mapped to invitation-WP / visa-WP-ext instances (ProcessNumber → PIA → Application), not to enum-15 apps.
- **Logs**: `artifacts/headless-import/*-App_Additional_WP_location-20260905-104751.log`
- **Next**: App_Additional_WP_location inner sequence complete. Band 2 live types done. Wait to lock next type. Remainder Visa and BorderZone documents after all types.
### 2026-09-05 - ApplicationProfileInstance App_Additional_WP_location headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Additional_WP_location --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy SQL filter match Prepared **281** / Posted **281** / Failed **0** / Skipped (transform) **140** / already **0**. Composite `E:15:na:na:na`.
- **Profile lock**: all **281** on **Iş Rugsatnama goşmaça  barjak ýeri** (`change_workpermit`); profile FK null **0**.
- **Reconcile**: PG `"ApplicationProfileInstances"` live **4952**; App_Additional_WP_location **281**. Prior types unchanged.
- **Id-map**: **4952** keys copied to source (411019 bytes)
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Additional_WP_location-20260905-104532.log`
- **Next**: ApplicationProfileInstancePerson roster only after user approval. Then progress, WorkPermit + WorkPermitItem. Skip Invitation and Visa.
### 2026-09-05 - App_Border_Zone_Permission roster+progress .15 -> local PG

- **Phase**: import
- **Mode**: sequential `--import-visa2014 --inprocess --no-wait --batch-size 50` for ApplicationProfileInstancePerson then ApplicationProfileInstanceProgress (`calik-energi-local-pg`; instance id-map **4671**; Invitation/WorkPermit/Visa skipped)
- **Outcome**: success (each wave exit **0**, FailedCount **0**, `CHAIN_EXIT=0`). First chain failed: DataImporter.exe missing after VS F5 locked a full rebuild; rebuilt with `dotnet build Visa2026.DataImporter.csproj -c Debug /p:BuildProjectReferences=false`.
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260905-104113`
- **Roster**: Posted **218** / already **9417** / missing id-map **12935**. PG people **9636** (this type **218**); instance FK null **0**. Id-map **9636**.
- **Progress**: Posted **450** / already **21768** / no instance map **15843** / parent-skipped **163**. PG live **22218** (this type **450**); FK null **0**. Id-map **22218**.
- **Logs**: `artifacts/headless-import/*-App_Border_Zone_Permission-20260905-104113.log`
- **Next**: App_Border_Zone_Permission inner sequence complete. Next locked type App_Additional_WP_location — wait for header approval. Issued BorderZone documents after all type slices.
### 2026-09-05 - ApplicationProfileInstance App_Border_Zone_Permission headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Border_Zone_Permission --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy SQL filter match Prepared **111** / Posted **111** / Failed **0** / Skipped (transform) **140** / already **0**. Composite `E:11:na:na:na`.
- **Profile lock**: all **111** on **Serhet Ýaka Üçin Rugsatnama Almak** (`get_border_zone`); profile FK null **0**.
- **Reconcile**: PG `"ApplicationProfileInstances"` live **4671**; App_Border_Zone_Permission **111**. Prior types unchanged.
- **Id-map**: **4671** keys copied to source (387696 bytes)
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Border_Zone_Permission-20260905-103545.log`
- **Next**: ApplicationProfileInstancePerson roster only after user approval. Then progress. Skip Invitation, WorkPermit, Visa. BorderZone documents after all type slices.
### 2026-09-05 - App_Visa_Ext_FM header+roster+progress+Visa .15 -> local PG

- **Phase**: import
- **Mode**: sequential `--import-visa2014 --inprocess --no-wait --batch-size 50` for ApplicationProfileInstance (`--application-type App_Visa_Ext_FM`), ApplicationProfileInstancePerson, ApplicationProfileInstanceProgress, Visa (`calik-energi-local-pg`). Invitation and WorkPermit skipped.
- **Outcome**: success (each wave exit **0**, FailedCount **0**, `CHAIN_EXIT=0`). First attempt failed: DataImporter.exe missing after test build; rebuilt then retried.
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260905-103139`
- **Headers**: Prepared **333** / Posted **333** / Failed **0** / transform skip **140**. Profile **Wiza Möhletini Uzaltmak FM** (`visa_ext_fm`) all 333; profile FK null **0**. Instance id-map **4560**.
- **Roster**: Posted **680** / already **8737** / missing id-map **13153**. PG people **9418** (this type **680**); FK null **0**.
- **Progress**: Posted **1632** / already **20136** / no instance map **16292**. PG live **21768** (this type **1632**); FK null **0**.
- **Visa**: Posted **415** / already **5281** / no Passport **25** / skip issuing **602**. PG live **5696**; this type issuing **415**; issuing-null still **1478** (sticky extras kept off cases). `visas > roster` on this type **0**.
- **Logs**: `artifacts/headless-import/*-App_Visa_Ext_FM-20260905-103139.log`
- **Next**: App_Visa_Ext_FM inner sequence complete. Next locked type App_Border_Zone_Permission — wait for header approval. Remainder Visa after all types.
### 2026-09-05 - Visa issuing origin: one visa per PersonInApplication

- **Phase**: import mapping + correction
- **Mode**: Path B `Visa2014VisaIssuingApplicationItemIndex.KeepEarliestVisaPerPersonInApplication`; `--correct-visa2014-issuing-application-profile-instance` on `calik-energi-local-pg`
- **Outcome**: success (exit **0**). Unit tests **7** passed.
- **Rule**: sticky `Visa.ProcessNumber` may list many later stickers on the same PIA. Issued records keep the **earliest** `VisaIssuedDate` (then Oid) per PIA. Extra stickers stay on the Passport with `IssuingApplicationProfileInstance` null (remainder).
- **Local PG**: updated **0** (kept 3803 already-correct). Cleared sticky issuing FK **1478**. Issuing-null **1478**. `visas > roster` instances **0** (was 137 WP-ext / 175 App_Inv / …). `4/-11744` now 1 visa / 1 person.
- **Issued-by-type after clear**: App_Visa_and_WP_Ext **1396**, App_Inv **1133**, App_Inv_And_WP **943**, App_Inv_FM **260**, App_Change_Passport **55**, App_Inv_According_to_WP **16**.
- **Log**: `artifacts/headless-import/VisaIssuingCorrection-20260905-101801.log`
- **Next**: App_Visa_Ext_FM headers after approval. WorkPermitItem > roster not changed.
### 2026-09-05 - App_Visa_and_WP_Ext roster vs issued (not per-person header merge)

- **Phase**: discovery
- **Question**: WP-ext instance roster < issued visas; suspicion that instances were merged per person
- **Outcome**: headers are 1:1 with `dbo.Application.Oid` (771 enum-7 apps = 771 instances; 2527 PIA = 2527 roster). Manual-number duplicate Oids are rare (2 pairs). `application.yaml` `import_one_per_legacy_oid` / do not merge headers.
- **Roster=1 is legacy shape**: 379/771 WP-ext instances have one PIA because VISA2015 often created one Application per person for extensions — not importer collapse.
- **visas > roster**: 137 WP-ext instances (also App_Inv 175, App_Inv_And_WP 60, …). Extra visas are additional stickers for people already on the roster (`visa_people > roster` = 0), not missing roster people. Example `4/-11744`: 1 PIA, 13 Visa rows all with `ProcessNumber` on that same extension PIA (2018-06 through 2019-11). Path B follows sticky `Visa.ProcessNumber`.
- **Do not** re-merge WP-ext headers by person. Fix would be issuing-origin (one visa per application-person, not every later sticker on the same PIA) — wait for lock.
### 2026-09-05 - App_Visa_and_WP_Ext roster+progress+WP+Visa chain .15 -> local PG

- **Phase**: import
- **Mode**: sequential `--import-visa2014 --inprocess --no-wait --batch-size 50` for ApplicationProfileInstancePerson, ApplicationProfileInstanceProgress, WorkPermit, WorkPermitItem, Visa (`calik-energi-local-pg`; instance id-map **4227**; Invitation skipped)
- **Outcome**: success (each wave exit **0**, FailedCount **0**, `CHAIN_EXIT=0`)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260905-095002`
- **Roster**: Posted **2527** / already **6210** / missing id-map **13833**. PG people **8738** (this type **2527**); instance FK null **0**. ResolvedLinks **44327**. Id-map **8738**.
- **Progress**: Posted **3760** / already **16376** / no instance map **16954** / parent-skipped **165**. PG live **20136** (this type **3760**); FK null **0**. Id-map **20136**.
- **WorkPermit**: Posted **134** / already **227** / instance not in id-map **49**. PG live **361** (Inv_And_WP 227 + this type **134**); instance FK null **0**. Id-map **361**.
- **WorkPermitItem**: Posted **1683** / already **2115** / missing id-map **2699**. PG live **3798**; header FK null **0**. Id-map **3798**.
- **Visa**: Posted **2120** / already **3161** / no Passport **25** / skip issuing **1017**. PG live **5281** (this type **2120**); issuing FK null **0**. Remainder skip issuing now **1017**.
- **Logs**: `artifacts/headless-import/*-App_Visa_and_WP_Ext-20260905-095002.log`
- **Next**: App_Visa_and_WP_Ext inner sequence complete. Next locked type App_Visa_Ext_FM — wait for header approval. Remainder Visa after all types.
### 2026-09-05 - ApplicationProfileInstance App_Visa_and_WP_Ext headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Visa_and_WP_Ext --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy SQL **12691** -> type match Prepared **771** / Posted **771** / Failed **0** / Skipped (transform) **140** / already **0**.
- **Profile lock**: all **771** on **Wiza we Iş Rugsatnamasyny Uzaltmak** (`extend_visa_wp`); profile FK null **0**.
- **Reconcile**: PG `"ApplicationProfileInstances"` live **4227**; App_Visa_and_WP_Ext **771**. Prior types unchanged (App_Inv 1810, App_Inv_And_WP 1178, App_Inv_FM 228, App_Change_Passport 122, App_Inv_According_to_WP 61, App_Change_Inv 57).
- **Id-map**: **4227** keys copied to source (350844 bytes)
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Visa_and_WP_Ext-20260905-094741.log`
- **Next**: ApplicationProfileInstancePerson roster only after user approval. Then progress, WorkPermit + WorkPermitItem, Visa. Skip Invitation.
### 2026-09-05 - Lock band 2 live Çalik types; skip empty

- **Phase**: strategy lock
- **Mode**: fill `application-type-import-order.yaml` band `2-produce-visa-workpermit-borderzone`
- **Outcome**: locked (no import this step)
- **Live (not_started)**: App_Visa_and_WP_Ext (`E:7` variants, Visa+WP, skip Invitation); App_Visa_Ext_FM (`F:7`, Visa only); App_Border_Zone_Permission (`E:11`, header/roster/progress only; BorderZone docs after all types); App_Additional_WP_location (`E:15`, WP only).
- **Skip empty**: App_Visa_Ext, App_Visa_Ext_According_to_WP, App_WP_Ext, App_Exit_Visa (`E:55` skip_row), App_Visa_For_New_Born_FM — no live composites in profile lock.
- **Enum vs ID**: keep existing translations. Do not steal enum 7 onto App_Change_Visa_Category. Border zone stays `E:11` (enum 11 ~111 rows); enum 12 stays cancel visa+WP.
- **Next**: wait for user approval to start App_Visa_and_WP_Ext headers. App_Sevice_Passport remains unlocked.
### 2026-09-05 - App_Change_Passport roster+progress+Visa chain .15 -> local PG

- **Phase**: import
- **Mode**: sequential `--import-visa2014 --inprocess --no-wait --batch-size 50` for ApplicationProfileInstancePerson, ApplicationProfileInstanceProgress, Visa (`calik-energi-local-pg`; instance id-map **3456**; Invitation and WorkPermit skipped)
- **Outcome**: success (each wave exit **0**, FailedCount **0**, `CHAIN_EXIT=0`)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260905-093940`
- **Roster**: Posted **141** / already **6069** / missing id-map **16360**. PG `"ApplicationProfileInstancePeople"` **6211** (this type **141**); FK null **0**. ResolvedLinks **29661**. Id-map **6211**.
- **Progress**: Posted **244** / already **16132** / no instance map **20779** / parent-skipped **165**. PG live **16376** (this type **244**); FK null **0**. Id-map **16376**.
- **Visa**: Posted **95** / already **3066** / no Passport **25** / skip issuing **3137**. PG live **3161** (this type **95**); issuing FK null **0**. Id-map **3161**. Remainder waits.
- **Logs**: `artifacts/headless-import/*-App_Change_Passport-20260905-093940.log`
- **Next**: App_Change_Passport inner sequence complete. Band 3 locked types done (Change_Visa_Category skipped empty). Do not invent the next ApplicationType; wait for user lock. Remainder Visa after all types.
### 2026-09-05 - ApplicationProfileInstance App_Change_Passport headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Change_Passport --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Mapping**: added `F:9:na:na:na` -> App_Change_Passport (lookup-translations + profile lock). Transform skips **140** (was 170; 30 family rows no longer unmapped).
- **Counts**: type match Prepared **122** / Posted **122** / Failed **0** / Skipped (transform) **140** / already **0**. 92 employee E:9 + 30 family F:9.
- **Profile lock**: all **122** on **Wizany KP>Täze Pasporta Geçirmek** (`pasport_change`); profile FK null **0**.
- **Reconcile**: PG `"ApplicationProfileInstances"` live **3456**; App_Change_Passport **122**. Prior types unchanged.
- **Id-map**: **3456** keys copied to source (286851 bytes)
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Change_Passport-20260905-093423.log`
- **Next**: ApplicationProfileInstancePerson roster only after user approval. Then progress, Visa. Skip Invitation and WorkPermit.

### 2026-09-05 - Skip App_Change_Visa_Category empty on Calik; lock F:9 for App_Change_Passport

- **Phase**: strategy lock
- **Mode**: skip empty type + family composite (not a remap of enum 7)
- **Outcome**: locked
- **Skip**: App_Change_Visa_Category `status: skip` — live enum 8 = 0; ID 8 stays App_Visa_and_WP_Ext / App_Visa_Ext_FM.
- **F:9**: family passport-change (30 rows) now maps with E:9 to App_Change_Passport.
### 2026-09-05 - ApplicationProfileInstance App_Change_Visa_Category headers .15 -> local PG (0 matches)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Change_Visa_Category --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50`
- **Outcome**: success (importer exit **0**, FailedCount **0**) but **0 prepared / 0 posted**
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy SQL **12690** -> filter match **0** / Posted **0** / Skipped (transform) **170**
- **Root cause**: importer composite uses SubType **enum** `TypeOfApplicationForEmployee` / `TypeOfApplicationForFamilyMember`, not `*ID`. Live VISA2015: enum **8** = **0** rows. ID **8** (change-visa-category in older audits, 771 employee + 333 family) is enum **7**, already translated to **App_Visa_and_WP_Ext** (`E:7:…`) / **App_Visa_Ext_FM** (`F:7:na:na:na`). Locked composites `E:8:na:0:na` / `E:8:na:1:na` / `F:8:na:na:na` never occur.
- **Do not** remap enum 7 onto App_Change_Visa_Category without a new lock (would steal visa-extension slices).
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Change_Visa_Category-20260905-092935.log`
- **Next**: wait — skip this type as empty, or remapping decision. Next locked type **App_Change_Passport** is enum **9** / ID **10** (**92** employee). Family enum 9 (**30**) has no `F:9` translation row.
### 2026-09-05 - App_Change_Inv issued+Visa chain .15 -> local PG

- **Phase**: import
- **Mode**: sequential `--import-visa2014 --inprocess --no-wait --batch-size 50` for Invitation, InvitationItem, Visa (`calik-energi-local-pg`; instance id-map **3334**; WorkPermit skipped)
- **Outcome**: success (each wave exit **0**, FailedCount **0**, `CHAIN_EXIT=0`)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260905-092640`
- **Invitation**: Legacy **2940** / Posted **55** / already **2849** / skip instance map **36**. PG live **2904** (1666+939+195+49+55); instance FK null **0**. Id-map **2904**. 57 headers vs 55 invitations: remaining cases have no legacy Invitation row.
- **InvitationItem**: Legacy **5299** / Posted **54** / already **5174** / missing id-map **71**. PG live **5228** (3027+1726+352+69+54); header FK null **0**. Id-map **5228**.
- **Visa**: Posted **0** / already **3066** / no Passport **25** / skip issuing instance **3232**. PG live **3066** (no App_Change_Inv issuing visas). Remainder waits. Step still required (generatesVisa); zero posts is not a halt.
- **Logs**: `artifacts/headless-import/*-App_Change_Inv-20260905-092640.log`
- **Next**: App_Change_Inv inner sequence complete. Next locked type is App_Change_Visa_Category — wait for user approval to start headers.
### 2026-09-05 - ApplicationProfileInstanceProgress App_Change_Inv .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstanceProgress --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50` (no `--verbose`; scoped by instance id-map **3334**)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy apps **12690** -> Prepared **37155** / Posted **114** / Failed **0** / Parent-skipped **164** / Skipped (no instance map) **21023** / Skipped (already imported) **16018** / Seeds removed **0**
- **No instance map skips**: expected (progress for types other than the five imported types). Not a halt.
- **Reconcile**: PG `"ApplicationProfileInstanceProgresses"` live **16132**; App_Inv **8809**; App_Inv_And_WP **5800**; App_Inv_FM **1120**; App_Inv_According_to_WP **289**; App_Change_Inv **114**; instance FK null **0**
- **Id-map**: bin `ApplicationProfileInstanceProgress.json` **16132** keys copied to source (1591327 bytes)
- **Log**: `artifacts/headless-import/ApplicationProfileInstanceProgress-App_Change_Inv-20260905-092446.log`
- **Next**: Invitation headers only after user approval, then InvitationItem, then Visa. Skip WorkPermit.
### 2026-09-05 - ApplicationProfileInstancePerson App_Change_Inv roster .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstancePerson --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--verbose`; scoped by instance id-map **3334**, not `--application-type`)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Scope**: instance map **3334**. Person id-map **3404**. Existing roster keys **6013** skipped as already imported.
- **Counts**: Legacy SQL **22778** -> Prepared **22570** / Posted **57** / Failed **0** / Skipped (prepare) **208** / Skipped (missing id-map) **16500** / Skipped (already imported) **6013**
- **Missing id-map skips**: expected (PersonInApplication whose Application is not in the imported type set). Not a halt.
- **Reconcile**: PG `"ApplicationProfileInstancePeople"` **6070** (App_Inv **3277**, App_Inv_And_WP **2254**, App_Inv_FM **398**, App_Inv_According_to_WP **84**, App_Change_Inv **57**); instance FK null **0**; `"ApplicationProfileInstancePersonResolvedLinks"` **29171** (`GCRecord = 0`). Importer printed ResolvedLinks created (sum) **0** — trust PG count.
- **Id-map**: bin `ApplicationProfileInstancePerson.json` **6070** keys copied to source (503813 bytes)
- **Log**: `artifacts/headless-import/ApplicationProfileInstancePerson-App_Change_Inv-20260905-092239.log`
- **Next**: ApplicationProfileInstanceProgress only after user approval. Then Invitation + InvitationItem, then Visa. Skip WorkPermit.
### 2026-09-05 - ApplicationProfileInstance App_Change_Inv headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Change_Inv --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--verbose`)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy SQL **12690** -> type match Prepared **57** / Posted **57** / Failed **0** / Skipped (transform) **170** / Skipped (already imported) **0**
- **Transform skips**: 170 expected (non-App_Change_Inv / unprepared) - not a halt
- **Profile lock**: all **57** on **Çakylygy üýtgetmek** (`change_invitation`); profile FK null **0**. Prior types unchanged.
- **Reconcile**: PG `"ApplicationProfileInstances"` live **3334**; App_Inv **1810**; App_Inv_And_WP **1178**; App_Inv_FM **228**; App_Inv_According_to_WP **61**; App_Change_Inv **57**
- **Id-map**: bin `ApplicationProfileInstance.json` **3334** keys copied to source (276725 bytes). Prior type keys kept.
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Change_Inv-20260905-092041.log`
- **Next**: ApplicationProfileInstancePerson roster only after user approval. Do not start Progress / Invitation / Visa yet. Skip WorkPermit.
### 2026-09-05 - Locked App_Change_Inv then App_Change_Visa_Category then App_Change_Passport (band 3)

- **Phase**: strategy lock
- **Mode**: living list lock (not an import wave)
- **Outcome**: locked
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Lock**: after App_Inv_According_to_WP (band 1 done). Band 2 stays empty. Band 3 order: **App_Change_Inv** (next) → **App_Change_Visa_Category** → **App_Change_Passport**. App_Change_Inv: Invitation + InvitationItem, then Visa (skip WorkPermit). Other two: Visa only. **App_Sevice_Passport** stays unlocked (band 1 remainder deferred). Append instance id-map; do not wipe prior keys.
- **Next**: ApplicationProfileInstance headers with `--application-type App_Change_Inv` only after user approval.
### 2026-09-05 - Visa App_Inv_According_to_WP .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Visa --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50` (no `--verbose`; scoped by instance id-map **3277**; no Visa wipe)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy SQL **6348** -> Prepared **6323** / Posted **29** / Failed **0** / Skipped (transform) **19** / Dedupe merged **6** / Skipped (no Passport map) **25** / Skipped (already imported) **3037** / Skipped (issuing instance not in id-map) **3232**
- **Issuing-instance skips**: expected (remainder Visa after all types). Not a halt. Prior FM skip **3258** vs now **3232**.
- **Reconcile**: PG `"Visas"` live **3066** (App_Inv **1622**, App_Inv_And_WP **1128**, App_Inv_FM **290**, App_Inv_According_to_WP **26**); issuing FK null **0**. Posted **29** vs this-type **26**: **3** catch-up visas attached to already-imported App_Inv instances (prior App_Inv Visa reconcile was **1619**).
- **Id-map**: bin `Visa.json` **3066** keys copied to source (254481 bytes)
- **Log**: `artifacts/headless-import/Visa-App_Inv_According_to_WP-20260905-091232.log`
- **Next**: App_Inv_According_to_WP inner sequence complete. Do not invent the next ApplicationType; wait for user lock. Remainder Visa after all types.
### 2026-09-05 - InvitationItem App_Inv_According_to_WP .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity InvitationItem --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50` (no `--verbose`; scoped by Invitation id-map **2849**)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy SQL **5299** -> Prepared **5299** / Posted **69** / Failed **0** / Skipped (missing required id-map) **125** / Skipped (already imported) **5105**
- **Missing id-map skips**: expected (items whose Invitation header is not in the four imported types). Not a halt. Prior FM skip **194** minus posted **69** = **125**.
- **Reconcile**: PG `"InvitationItems"` live **5174** (App_Inv **3027**, App_Inv_And_WP **1726**, App_Inv_FM **352**, App_Inv_According_to_WP **69**); header FK null **0**
- **Id-map**: bin `InvitationItem.json` **5174** keys copied to source (429445 bytes)
- **Log**: `artifacts/headless-import/InvitationItem-App_Inv_According_to_WP-20260905-090914.log`
- **Next**: Visa only after user approval. Skip WorkPermit.
### 2026-09-04 - Invitation App_Inv_According_to_WP headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Invitation --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50` (no `--verbose`; scoped by instance id-map **3277**)
- **Outcome**: success (importer exit **0**, FailedCount **0**) after rebuild (exe/RichEdit missing from `bin\Debug\net8.0` before the run)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy SQL **2940** -> Prepared **2940** / Posted **49** / Failed **0** / Skipped (already imported) **2800** / Skipped (instance not in id-map) **91**
- **Instance-map skips**: expected (invitations whose issuing instance is not in the four imported types). Not a halt.
- **Reconcile**: PG `"Invitations"` live **2849** (App_Inv **1666**, App_Inv_And_WP **939**, App_Inv_FM **195**, App_Inv_According_to_WP **49**); instance FK null **0**. 61 headers vs 49 invitations: remaining cases have no legacy Invitation row.
- **Id-map**: bin `Invitation.json` **2849** keys copied to source (236470 bytes)
- **Log**: `artifacts/headless-import/Invitation-App_Inv_According_to_WP-20260904-174918.log`
- **Next**: InvitationItem only after user approval, then Visa. Skip WorkPermit.
### 2026-09-04 - ApplicationProfileInstanceProgress App_Inv_According_to_WP .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstanceProgress --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50` (no `--verbose`; scoped by instance id-map **3277**)
- **Outcome**: success (importer exit **0**, FailedCount **0**) after rebuild (first attempt failed: missing `DevExpress.RichEdit.v25.2.Core` in DataImporter bin)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy apps **12687** -> Prepared **37149** / Posted **289** / Failed **0** / Parent-skipped **163** / Skipped (no instance map) **21131** / Skipped (already imported) **15729** / Seeds removed **0**
- **No instance map skips**: expected (progress for types other than the four imported invitation types). Not a halt.
- **Reconcile**: PG `"ApplicationProfileInstanceProgresses"` live **16018**; App_Inv **8809**; App_Inv_And_WP **5800**; App_Inv_FM **1120**; App_Inv_According_to_WP **289**; instance FK null **0**
- **Id-map**: bin `ApplicationProfileInstanceProgress.json` **16018** keys copied to source (1579870 bytes)
- **Log**: `artifacts/headless-import/ApplicationProfileInstanceProgress-App_Inv_According_to_WP-20260904-173752.log`
- **Next**: Invitation headers only after user approval, then InvitationItem, then Visa. Skip WorkPermit.
### 2026-09-04 - ApplicationProfileInstanceProgress App_Inv_According_to_WP first attempt failed (missing RichEdit DLL)

- **Phase**: import
- **Mode**: same CLI as above; host failed before posts
- **Outcome**: failed (importer exit **1**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg`
- **Error**: `Could not load file or assembly DevExpress.RichEdit.v25.2.Core` during headless host start (OData warmup). Bin had been emptied of that DLL after the roster run.
- **Fix**: `dotnet build Visa2026.DataImporter.csproj -c Debug`, copy instance+progress id-maps source -> bin, retry.
- **Log**: `artifacts/headless-import/ApplicationProfileInstanceProgress-App_Inv_According_to_WP-20260904-173110.log`
### 2026-09-04 - ApplicationProfileInstancePerson App_Inv_According_to_WP roster .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstancePerson --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--verbose`; scoped by instance id-map **3277**, not `--application-type`)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Scope**: instance map **3277**. Person id-map **3404**. Existing roster keys **5929** skipped as already imported.
- **Counts**: Legacy SQL **22770** -> Prepared **22562** / Posted **84** / Failed **0** / Skipped (prepare) **208** / Skipped (missing id-map) **16549** / Skipped (already imported) **5929**
- **Missing id-map skips**: expected (PersonInApplication whose Application is not in the imported type set). Not a halt.
- **Startup**: ApplicationProfile approval-leg instance heal logged concurrency (`UserFriendlyException` / 0 rows affected) then continued (`App will start`). Import still exit **0**.
- **Reconcile**: PG `"ApplicationProfileInstancePeople"` **6013** (App_Inv **3277**, App_Inv_And_WP **2254**, App_Inv_FM **398**, App_Inv_According_to_WP **84**); instance FK null **0**; `"ApplicationProfileInstancePersonResolvedLinks"` **29000** (`GCRecord = 0`). Importer printed ResolvedLinks created (sum) **0** — trust PG count.
- **Id-map**: bin `ApplicationProfileInstancePerson.json` **6013** keys copied to source (499082 bytes)
- **Log**: `artifacts/headless-import/ApplicationProfileInstancePerson-App_Inv_According_to_WP-20260904-172802.log`
- **Next**: ApplicationProfileInstanceProgress only after user approval. Then Invitation + InvitationItem, then Visa. Skip WorkPermit.
### 2026-09-04 - ApplicationProfileInstance App_Inv_According_to_WP headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Inv_According_to_WP --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--verbose`)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy SQL **12687** -> type match Prepared **61** / Posted **61** / Failed **0** / Skipped (transform) **170** / Skipped (already imported) **0**
- **Transform skips**: 170 expected (non-App_Inv_According_to_WP / unprepared) - not a halt
- **Profile lock**: all **61** on **İş Rugsatnama görä Çakylyk Almak** (`get_invitation_according_to_wp`); profile FK null **0**. Prior types unchanged (App_Inv 1810 Çakylyk Almak; App_Inv_And_WP 1178; App_Inv_FM 228 Çakylyk Almak FM).
- **Reconcile**: PG `"ApplicationProfileInstances"` live **3277**; App_Inv **1810**; App_Inv_And_WP **1178**; App_Inv_FM **228**; App_Inv_According_to_WP **61**
- **Id-map**: bin `ApplicationProfileInstance.json` **3277** keys copied to source (271994 bytes). Prior type keys kept.
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Inv_According_to_WP-20260904-172223.log`
- **Next**: ApplicationProfileInstancePerson roster only after user approval. Do not start Progress / Invitation / Visa yet. Skip WorkPermit.
### 2026-09-04 - Lock ApplicationType names to unique Application Profile templates

- **Phase**: mapping / patch
- **Mode**: locked `application-type-profile-lock.yaml` (source composite -> target ApplicationType.Name -> ApplicationProfile Code/Name). Import resolver uses this file first. `--patch-visa2014-application-profile` on local PG (`calik-energi-local-pg`; instance id-map **3216**). Dry-run then write.
- **Outcome**: success (dry-run and write exit **0**, Failed **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Bug**: App_Inv_FM headers used profile **Çakylyk Almak** (`get_invitation`) because several invitation types share ApplicationType.Code. Tenant template for App_Inv_FM is **Çakylyk Almak FM** (`get_invitation_fm`).
- **Lock**: `F:0:na:na:na` -> `App_Inv_FM` -> `get_invitation_fm`. `E:0:0:na:na` stays `App_Inv` / `get_invitation`. `E:0:1:na:na` stays `App_Inv_And_WP` / `get_invitation_wp`. `E:20:na:na:na` -> `App_Inv_According_to_WP` / `get_invitation_according_to_wp` for the next slice.
- **Patch**: in scope **3216**; patched **228** (`get_invitation_fm`); already correct **2988**.
- **Reconcile**: App_Inv **1810** Çakylyk Almak; App_Inv_And_WP **1178** Çakylyk we Iş Rugsatnamasyny Almak; App_Inv_FM **228** Çakylyk Almak FM.
- **Next**: App_Inv_According_to_WP headers only after user approval. New type slices must add a lock row before import.
### 2026-09-04 - App_Inv_FM issued+Visa chain .15 -> local PG

- **Phase**: import
- **Mode**: sequential `--import-visa2014 --inprocess --no-wait --batch-size 50` for Invitation, InvitationItem, Visa (`calik-energi-local-pg`; instance id-map **3216**; WorkPermit skipped)
- **Outcome**: success (each wave exit **0**, FailedCount **0**, `CHAIN_EXIT=0`)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260904-165303`
- **Invitation**: Posted **195** / already **2605** / skip instance map **140**. PG live **2800** (1666 + 939 + 195); instance FK null **0**. Id-map **2800**.
- **InvitationItem**: Posted **352** / already **4753** / missing id-map **194**. PG live **5105** (3027 + 1726 + 352); header FK null **0**. Id-map **5105**.
- **Visa**: Posted **290** / already **2747** / no Passport **25** / skip issuing instance **3258**. PG live **3037** (1619 + 1128 + 290); issuing FK null **0**. Id-map **3037**. Remainder waits.
- **Logs**: `artifacts/headless-import/*-App_Inv_FM-20260904-165303.log`
- **Next**: App_Inv_FM inner sequence complete. Next locked type is App_Inv_According_to_WP — wait for user approval to start headers.
### 2026-09-04 - ApplicationProfileInstanceProgress App_Inv_FM .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstanceProgress --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50` (no `--verbose`; scoped by instance id-map **3216**)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy apps **12686** -> Prepared **37161** / Posted **1120** / Failed **0** / Parent-skipped **162** / Skipped (no instance map) **21432** / Skipped (already imported) **14609** / Seeds removed **0**
- **No instance map skips**: expected (progress for types other than App_Inv / App_Inv_And_WP / App_Inv_FM). Not a halt.
- **Reconcile**: PG `"ApplicationProfileInstanceProgresses"` live **15729**; App_Inv **8809**; App_Inv_And_WP **5800**; App_Inv_FM **1120**
- **Id-map**: bin `ApplicationProfileInstanceProgress.json` **15729** keys copied to source (1551409 bytes)
- **Log**: `artifacts/headless-import/ApplicationProfileInstanceProgress-App_Inv_FM-20260904-164610.log`
- **Next**: Invitation headers only after user approval, then InvitationItem, then Visa. Skip WorkPermit.
### 2026-09-04 - ApplicationProfileInstancePerson App_Inv_FM roster .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstancePerson --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--verbose`; scoped by instance id-map **3216**, not `--application-type`)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Scope**: instance map **3216**. Person id-map **3404**. Existing roster keys **5531** skipped as already imported.
- **Counts**: Legacy SQL **22769** -> Prepared **22561** / Posted **398** / Failed **0** / Skipped (prepare) **208** / Skipped (missing id-map) **16632** / Skipped (already imported) **5531**
- **Missing id-map skips**: expected (PersonInApplication whose Application is not App_Inv / App_Inv_And_WP / App_Inv_FM). Not a halt.
- **Reconcile**: PG `"ApplicationProfileInstancePeople"` **5929** (App_Inv **3277**, App_Inv_And_WP **2254**, App_Inv_FM **398**); `"ApplicationProfileInstancePersonResolvedLinks"` **28502** (`GCRecord = 0`). Importer printed ResolvedLinks created (sum) **0** — trust PG count.
- **Id-map**: bin `ApplicationProfileInstancePerson.json` **5929** keys copied to source (492110 bytes)
- **Log**: `artifacts/headless-import/ApplicationProfileInstancePerson-App_Inv_FM-20260904-164422.log`
- **Next**: ApplicationProfileInstanceProgress only after user approval. Then Invitation + InvitationItem, Visa. Skip WorkPermit.
### 2026-09-04 - ApplicationProfileInstance App_Inv_FM headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Inv_FM --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--verbose`)
- **Outcome**: success (importer exit **0**, FailedCount **0**) after rebuild (exe missing from `bin\Debug\net8.0`)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy SQL **12686** -> type match Prepared **228** / Posted **228** / Failed **0** / Skipped (transform all types) **170** / Skipped (already imported) **0**
- **Transform skips**: 170 expected (non-App_Inv_FM / unprepared) - not a halt
- **Reconcile**: PG `"ApplicationProfileInstances"` live **3216**; App_Inv **1810**; App_Inv_And_WP **1178**; App_Inv_FM **228**; `ApplicationProfileID` null **0**
- **Id-map**: bin `ApplicationProfileInstance.json` **3216** keys copied to source (266931 bytes). Prior type keys kept.
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Inv_FM-20260904-164239.log`
- **Next**: ApplicationProfileInstancePerson roster only after user approval. Do not start Progress / Invitation / Visa yet. Skip WorkPermit.
### 2026-09-04 - Locked App_Inv_FM then App_Inv_According_to_WP (band 1)

- **Phase**: strategy lock
- **Mode**: living list append (not an import wave)
- **Outcome**: locked
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Lock**: `application-type-import-order.yaml` band 1 after App_Inv_And_WP: **App_Inv_FM** (next), then **App_Inv_According_to_WP**. Both invitation + visa, no WorkPermit. Append instance id-map; do not wipe App_Inv / App_Inv_And_WP keys. Import According_to_WP only after FM inner sequence completes.
- **Next**: ApplicationProfileInstance headers with `--application-type App_Inv_FM` only after user approval.
### 2026-09-04 - App_Inv_And_WP issued+Visa chain .15 -> local PG

- **Phase**: import
- **Mode**: sequential `--import-visa2014 --inprocess --no-wait --batch-size 50` for Invitation, InvitationItem, WorkPermit, WorkPermitItem, Visa (`calik-energi-local-pg`; instance id-map **2988**; no `--application-type`)
- **Outcome**: success (each wave exit **0**, FailedCount **0**, `CHAIN_EXIT=0`)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **RunId**: `20260904-162932`
- **Invitation**: Legacy **2940** / Posted **939** / already **1666** / skip instance map **335**. PG live **2605** (App_Inv 1666 + App_Inv_And_WP 939); instance FK null **0**. Id-map **2605**.
- **InvitationItem**: Legacy **5299** / Posted **1726** / already **3027** / missing id-map **546**. PG live **4753** (3027 + 1726); header FK null **0**. Id-map **4753**.
- **WorkPermit**: Legacy **410** / Posted **227** / skip instance map **183**. PG live **227** (all App_Inv_And_WP); instance FK null **0**. Id-map **227**. Type-slice skip-create held (no null FK posts).
- **WorkPermitItem**: Legacy **6497** / Posted **2115** / missing id-map **4382** / position fallback **33**. PG live **2115**; WorkPermit FK null **0**. Id-map **2115**.
- **Visa**: Legacy **6345** / Prepared **6320** / Posted **1128** / already **1619** / no Passport **25** / skip issuing instance **3548**. PG live **2747** (1619 + 1128); issuing FK null **0**. Id-map **2747**. Remainder waits.
- **Logs**: `artifacts/headless-import/*-App_Inv_And_WP-20260904-162932.log`
- **Next**: App_Inv_And_WP inner sequence complete. Do not invent the next ApplicationType; wait for user lock. Remainder Visa after all types.
### 2026-09-04 - ApplicationProfileInstanceProgress App_Inv_And_WP .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstanceProgress --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50` (no `--verbose`; scoped by instance id-map **2988**)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy apps **12686** -> Prepared **36498** / Posted **5800** / Failed **0** / Parent-skipped **162** / Skipped (no instance map) **21889** / Skipped (already imported) **8809** / Seeds removed **0**
- **No instance map skips**: expected (progress for types other than App_Inv / App_Inv_And_WP). Not a halt.
- **Reconcile**: PG `"ApplicationProfileInstanceProgresses"` live **14609**; App_Inv **8809** kept; App_Inv_And_WP **5800**
- **Id-map**: bin `ApplicationProfileInstanceProgress.json` **14609** keys copied to source (1440886 bytes)
- **Log**: `artifacts/headless-import/ApplicationProfileInstanceProgress-App_Inv_And_WP-20260904-162615.log`
- **Next**: Invitation headers only after user approval, then InvitationItem, WorkPermit + WorkPermitItem, Visa.
### 2026-09-04 - ApplicationProfileInstancePerson App_Inv_And_WP roster .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstancePerson --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--verbose`; scoped by instance id-map **2988**, not `--application-type`)
- **Outcome**: success (importer exit **0**, FailedCount **0**) after one failed start
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Failed start**: `Visa2026.DataImporter.exe` missing from `bin\Debug\net8.0` (output cleaned). Rebuild then retry.
- **Scope**: instance map **2988** (App_Inv 1810 + App_Inv_And_WP 1178). Person id-map **3404**. Existing roster keys **3277** skipped as already imported.
- **Counts**: Legacy SQL **22769** -> Prepared **22561** / Posted **2254** / Failed **0** / Skipped (prepare) **208** / Skipped (missing id-map) **17030** / Skipped (already imported) **3277**
- **Missing id-map skips**: expected (PersonInApplication whose Application is not App_Inv / App_Inv_And_WP). Not a halt.
- **Reconcile**: PG `"ApplicationProfileInstancePeople"` **5531** (App_Inv **3277**, App_Inv_And_WP **2254**); `"ApplicationProfileInstancePersonResolvedLinks"` **27613** (`GCRecord = 0`). Importer printed ResolvedLinks created (sum) **0** — trust PG count.
- **Id-map**: bin `ApplicationProfileInstancePerson.json` **5531** keys copied to source (459076 bytes)
- **Log**: `artifacts/headless-import/ApplicationProfileInstancePerson-App_Inv_And_WP-20260904-162200.log`
- **Next**: ApplicationProfileInstanceProgress only after user approval. Then Invitation + InvitationItem, WorkPermit + WorkPermitItem, Visa.
### 2026-09-04 - ApplicationProfileInstance App_Inv_And_WP headers .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Inv_And_WP --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--verbose`)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy SQL **12686** -> type match Prepared **1178** / Posted **1178** / Failed **0** / Skipped (transform all types) **170** / Skipped (already imported) **0**
- **Transform skips**: 170 expected (non-App_Inv_And_WP / unprepared) - not a halt
- **Reconcile**: PG `"ApplicationProfileInstances"` live **2988**; App_Inv **1810** preserved; App_Inv_And_WP **1178**; `ApplicationProfileID` null **0**
- **Id-map**: bin `ApplicationProfileInstance.json` **2988** keys copied to source (248007 bytes). App_Inv keys kept.
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Inv_And_WP-20260904-161336.log`
- **Next**: ApplicationProfileInstancePerson roster only after user approval. Do not start Progress / Invitation / WorkPermit / Visa yet.
### 2026-09-04 - Locked App_Inv_And_WP (band 1) + WorkPermit type-slice skip

- **Phase**: strategy lock + importer hardening
- **Mode**: living list append (not an import wave)
- **Outcome**: locked
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Lock**: `application-type-import-order.yaml` band 1 after App_Inv: `App_Inv_And_WP` (invitation + work permit + visa). Inner sequence Invitation+items then WorkPermit+items then Visa. Append instance id-map; do not wipe App_Inv keys.
- **Importer**: WorkPermit skip-create when Application is not in the current instance id-map (same as Invitation/Visa). Log label is now `Skipped (instance not in id-map)`.
- **Next**: ApplicationProfileInstance headers with `--application-type App_Inv_And_WP` only after this lock; then wait for approval before roster.
### 2026-09-04 - Visa App_Inv .15 -> local PG (type-slice reimport)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Visa --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50` (no `--verbose`; scoped by App_Inv instance id-map **1810**)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Importer change**: skip create when IssuingApplicationProfileInstance is not in the current instance id-map (was "visa still posted"). Remainder + other types wait.
- **Counts**: Legacy SQL **6345** -> Prepared **6320** / Posted **1619** / Failed **0** / Transform skipped **19** / Dedupe **6** / Skipped (no Passport map) **25** / Skipped (issuing instance not in id-map) **4676** / already **0**
- **Reconcile**: PG `"Visas"` live **1619**; App_Inv **1619**; issuing FK null **0**
- **Id-map**: bin `Visa.json` **1619** keys copied to source (134380 bytes)
- **Log**: `artifacts/headless-import/Visa-App_Inv-20260904-160724.log`
- **Next**: App_Inv inner sequence complete (WorkPermit skipped). Do not invent the next ApplicationType; wait for user lock. Remainder Visa after all types.
### 2026-09-04 - InvitationItem App_Inv .15 -> local PG (type-slice reimport)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity InvitationItem --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50` (no `--verbose`; scoped by Invitation id-map **1666** App_Inv headers)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Person / Passport maps**: **3404** / **3760**
- **Counts**: Legacy SQL **5299** -> Prepared **5299** / Posted **3027** / Failed **0** / Skipped (missing required id-map) **2272** / Skipped (already imported) **0**
- **Missing id-map skips**: expected (items whose Invitation is not App_Inv). Not a halt.
- **Reconcile**: PG `"InvitationItems"` live **3027**; App_Inv **3027**; header FK null **0**
- **Id-map**: bin `InvitationItem.json` **3027** keys copied to source (251244 bytes)
- **Log**: `artifacts/headless-import/InvitationItem-App_Inv-20260904-160434.log`
- **Next**: Visa only after user approval (IssuingApplicationProfileInstance = App_Inv). Skip WorkPermit.
### 2026-09-04 - Invitation App_Inv headers .15 -> local PG (type-slice reimport)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Invitation --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50` (no `--verbose`; scoped by App_Inv header id-map **1810**)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Importer change**: skip create when Application is not in the current instance id-map (was "header still posted" with null FK). Other-type invitations wait for their slice.
- **Counts**: Legacy SQL **2940** -> Prepared **2940** / Posted **1666** / Failed **0** / Skipped (instance not in id-map) **1274** / Skipped (already imported) **0** / FK patched **0**
- **Reconcile**: PG `"Invitations"` live **1666**; App_Inv **1666**; instance FK null **0**
- **Id-map**: bin `Invitation.json` **1666** keys copied to source (138281 bytes)
- **Log**: `artifacts/headless-import/Invitation-App_Inv-20260904-155758.log`
- **Next**: InvitationItem only after user approval, then Visa. Skip WorkPermit.
### 2026-09-04 - ApplicationProfileInstanceProgress App_Inv .15 -> local PG (type-slice reimport)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstanceProgress --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50` (no `--verbose`; scoped by App_Inv header id-map **1810**)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy apps **12682** -> Prepared **36532** / Posted **8809** / Failed **0** / Parent-skipped **163** / Skipped (no instance map) **27723** / Skipped (already imported) **0** / Seeds removed **0**
- **No instance map skips**: expected (progress for non-App_Inv apps). Not a halt.
- **Reconcile**: PG `"ApplicationProfileInstanceProgresses"` live **8809**; App_Inv **8809**
- **Id-map**: bin `ApplicationProfileInstanceProgress.json` **8809** keys copied to source (869501 bytes)
- **Log**: `artifacts/headless-import/ApplicationProfileInstanceProgress-App_Inv-20260904-154415.log`
- **Next**: Invitation headers only after user approval, then InvitationItem, then Visa. Skip WorkPermit.
### 2026-09-04 - ApplicationProfileInstancePerson App_Inv roster .15 -> local PG (type-slice reimport)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstancePerson --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--verbose`; scoped by App_Inv header id-map, not `--application-type`)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Scope**: App_Inv headers in `ApplicationProfileInstance.json` **1810**. Person id-map **3404**.
- **Counts**: Legacy SQL **22762** -> Prepared **22554** / Posted **3277** / Failed **0** / Skipped (prepare) **208** / Skipped (missing id-map) **19277** / Skipped (already imported) **0**
- **Missing id-map skips**: expected (PersonInApplication whose Application is not App_Inv). Not a halt.
- **Reconcile**: PG `"ApplicationProfileInstancePeople"` **3277** (no GCRecord); `"ApplicationProfileInstancePersonResolvedLinks"` **16359** (`GCRecord = 0`). Importer printed ResolvedLinks created (sum) **0** — trust PG count.
- **Id-map**: bin `ApplicationProfileInstancePerson.json` **3277** keys copied to source (271994 bytes)
- **Log**: `artifacts/headless-import/ApplicationProfileInstancePerson-App_Inv-20260904-153754.log`
- **Next**: ApplicationProfileInstanceProgress only after user approval. Then Invitation + InvitationItem, then Visa. Skip WorkPermit.
### 2026-09-04 - ApplicationProfileInstance App_Inv headers .15 -> local PG (type-slice reimport)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstance --application-type App_Inv --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--verbose`)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Counts**: Legacy SQL **12682** -> type match Prepared **1810** / Posted **1810** / Failed **0** / Skipped (transform all types) **170** / Skipped (already imported) **0**
- **Transform skips**: 170 expected (non-App_Inv / unprepared) - not a halt
- **Reconcile**: PG `"ApplicationProfileInstances"` live **1810**; App_Inv **1810**; `ApplicationProfileID` null **0**
- **Id-map**: bin `ApplicationProfileInstance.json` **1810** keys copied to source (150233 bytes)
- **Log**: `artifacts/headless-import/ApplicationProfileInstance-App_Inv-20260904-153601.log`
- **Next**: ApplicationProfileInstancePerson roster only after user approval. Do not start Progress / Invitation / Visa yet.
### 2026-09-04 - AddressOfResidence import .15 -> local PG (type-slice reimport)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity AddressOfResidence --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50` (no `--verbose`, no Person reimport)
- **Outcome**: success first try (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **OtherSite**: no extra seed this run (prior Lojman-as-Other row already in tenant JSON after wipe/updateDatabase)
- **Person id-map**: **3404**
- **Counts**: Legacy SQL **4155** -> Prepared **4152** / Posted **5288** / Failed **0** / Skipped (transform) **3** / Dedupe **0** / Skipped (no Person map) **0** / Skipped (already imported) **0** / PIA-inferred posted **1136** failed **0**
- **Transform skips**: 3 expected - not a halt
- **Reconcile**: PG `"AddressesOfResidence"` `GCRecord = 0` = **5288**
- **Id-map**: bin `AddressOfResidence.json` **7383** keys (includes aliases) copied to source (612792 bytes)
- **Log**: `artifacts/headless-import/AddressOfResidence-20260904-153421.log`
- **Next**: App_Inv ApplicationProfileInstance header only after user approval. MedicalRecord is file wave. No Visa.
### 2026-09-04 - EmployeeSalary import .15 -> local PG (type-slice reimport)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity EmployeeSalary --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50` (no `--verbose`, no Person reimport)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Person id-map**: **3404**
- **Counts**: Legacy SQL **3111** -> Prepared **3048** / Posted **3048** / Failed **0** / Skipped (transform) **63** / Dedupe **0** / Skipped (no Person map) **0** / Skipped (already imported) **0**
- **Transform skips**: 63 expected (empty/unparseable Salary.Detail and/or missing Salary FK) - not a halt
- **Reconcile**: PG `"EmployeeSalaries"` `GCRecord = 0` = **3048**
- **Id-map**: bin `EmployeeSalary.json` **3048** keys copied to source (252987 bytes)
- **Log**: `artifacts/headless-import/EmployeeSalary-20260904-153234.log`
- **Next**: AddressOfResidence only after user approval. MedicalRecord is file wave.
### 2026-09-04 - EmployeePositionHistory import .15 -> local PG (type-slice reimport)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity EmployeePositionHistory --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50` (no `--verbose`, no `--supplement-permit-positions`, no Person/Education reimport)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Person id-map**: **3404** (no copy needed)
- **Counts**: Legacy SQL **3162** -> Prepared **3162** / Posted **3162** / Failed **0** / Skipped **0** / Dedupe **0** / Skipped (no Person map) **0** / Skipped (already imported) **0** / ActualPositions created **1434**
- **ActualPositions**: 1434 is expected on a wiped DB (prior non-wipe run created 55). Not a halt.
- **Lookup seed**: not needed (FailedCount 0)
- **Reconcile**: PG `"EmployeePositionHistories"` `GCRecord = 0` = **3162**
- **Id-map**: bin `EmployeePositionHistory.json` **3162** keys copied to source (262449 bytes)
- **Log**: `artifacts/headless-import/EmployeePositionHistory-20260904-153036.log`
- **Next**: EmployeeSalary only after user approval.
### 2026-09-04 - Education import .15 -> local PG (type-slice reimport)

- **Phase**: import
- **Mode**: `Seed-EducationLookupGapsToPostgres.ps1` then `--import-visa2014 --entity Education --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50` (no `--verbose`)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Seed first**: live `.15` DISTINCT institutions=**1572** specialties=**1125**; PG NameTm before seed 1566 / 1124; PG gaps 19 inst + 8 spec (INSERT 0 1 each); JSON gaps 2+2. Do not invent NameTm; omit `GCRecord` on INSERT. Labels stay in tenant JSON (do not paste console CP437 into this file).
- **Person id-map**: **3404** (no Person reimport)
- **Counts**: Legacy SQL **3276** -> Prepared **3276** / Posted **3276** / Failed **0** / Skipped **0** / Dedupe **0** / Skipped (no Person map) **0** / Skipped (already imported) **0**
- **Reconcile**: PG `"Educations"` `GCRecord = 0` = **3276**
- **Id-map**: bin `Education.json` **3276** keys copied to source (271911 bytes)
- **Log**: `artifacts/headless-import/Education-20260904-152857.log`
- **Next**: EmployeePositionHistory only after user approval.
### 2026-09-04 - Passport import .15 -> local PG (type-slice reimport)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Passport --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50` (no `--verbose`)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Person id-map**: source + bin **3404** (required; no Person reimport)
- **Counts**: Legacy SQL **3840** -> Prepared **3759** / Posted **3759** / Failed **0** / Skipped (transform) **79** / Dedupe merged **2** / Skipped (no Person map) **0** / Skipped (already imported) **0**
- **Transform skips**: 79 expected (same as prior local Passport wave) - not a halt
- **Reconcile**: PG `"Passports"` `GCRecord = 0` = **3759**
- **Id-map**: bin `Passport.json` **3760** keys (+1 expand / dedupe) copied to source (312083 bytes)
- **Log**: `artifacts/headless-import/Passport-20260904-152648.log` (run id from watch status)
- **Next**: Education only after user approval. Do not import Visa.
### 2026-09-04 - Person import .15 -> local PG (retry after ValueManager skip)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Person --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50` (no `--verbose`; bin exe after DataImporter Debug rebuild)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Prior fail**: exit 1 host crash ValueManager (template seed before UseXaf). Retry after `VISA2026_HEADLESS_IMPORT` skip.
- **Counts**: Legacy SQL **3404** -> Prepared **3404** / Posted **3404** / Failed **0** / Skipped **0** / Skipped (already imported) **0** / Dedupe merged **21** / Relinked **0**
- **Reconcile**: PG `"People"` `GCRecord = 0` = **3404** (column is NOT NULL DEFAULT 0; do not use `IS NULL`; count via `psql -f`)
- **Id-map**: bin `Person.json` **3404** keys copied to source `Visa2026.DataImporter/legacy/visa2014/id-maps/calik-energi-local-pg/Person.json` (282535 bytes)
- **Log**: `artifacts/headless-import/Person-20260904-152428.log`
- **Next**: Passport only after user approval (person-domain; no Visa).
### 2026-09-04 - Person import .15 -> local PG failed (ValueManager host start)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity Person --legacy-source calik-energi-local-pg --inprocess --no-wait --batch-size 50` (no `--verbose`)
- **Outcome**: failed (importer exit **1**, Posted **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Cause**: headless host `Startup.Configure` ran `UserReportTemplateSeedGate` before `UseXaf()` on a fresh Debug DB. Template `OnSaving` hit `SecuritySystem.CurrentUserName` and initialized SimpleValueManager; `UseXaf()` could not switch to AsyncValueManager.
- **Counts**: no Person rows posted. People still **0**.
- **Fix**: set `VISA2026_HEADLESS_IMPORT=true` in `HeadlessMigrationHost.Start`; skip template seed for headless; move F5 seed after `UseXaf()`. Do not start a second overlapping Person wave.
- **Log**: `artifacts/local-pg-import/data/import-logs/Person-20260904-152212.log`
- **Next**: retry Person only after DataImporter rebuild.
### 2026-09-04 - Drop local PG visa2026 (empty start for type-slice import)

- **Phase**: cleanup
- **Mode**: DROP DATABASE visa2026 + CREATE DATABASE (UTF8). No DB backup, no id-map bak (developer local).
- **Outcome**: success
- **Left alone**: visa2026_easytest; VISA2015 / .15; Demo/Prod on .25
- **Schema**: first --updateDatabase failed (42P01 InvitationItems before tables exist). Guarded IssuedDocumentStatusColumnsCleanupSchemaSql with to_regclass. Retry exit **0**.
- **Reconcile**: public tables **153**; People/Passports/Visas/Instances **0**; ApplicationProfiles **36**; ApplicationTypes **36**; EducationInstitutions **1566**
- **Id-maps**: calik-energi-local-pg source+bin reset to `{}`
- **Next**: Person from .15 (`calik-energi-local-pg`), then person-domain (no Visa). App_Inv slice after that.
### 2026-09-04 - Strategy lock: Application Type slices (Visa after instance)

- **Phase**: strategy
- **Mode**: docs / skill only (no import run, no Visa wipe)
- **Outcome**: locked
- **Decision**: Import Application Profile Instances one ApplicationType at a time. Inner sequence: header -> roster -> progress -> issued Invitation/WorkPermit (if generated) -> Visa (if that instance generates Visa) -> next type.
- **Living list**: `Visa2026.DataImporter/legacy/visa2014/application-type-import-order.yaml`. First type **App_Inv**. Do not invent remaining types.
- **Bands**: (1) produce invitation (incl. invitation+WP) then visa; (2) produce Visa / WorkPermit without invitation / BorderZone instances; (3) change invitation or visa (still run issued Invitation/Visa); (4) last = no issued docs including cancel types.
- **Visa**: never before IssuingApplicationProfileInstance. Local PG: wipe old Passport-first Visa and reimport per type when App_Inv reaches the visa step (not during this docs update).
- **Roster**: invitation-producing types must not require Visa.
- **After all types**: Rejection, BorderZone documents, orphan Visa remainder.
- **App_Inv local PG**: headers + roster done. Next inner step is **Progress**, then Invitation + InvitationItem, then Visa (after wipe). Skip WorkPermit.
- **Artifacts**: SKILL.md, import-practices.md, IMPORT_PLAN_AND_STRATEGY.md, import-strategy.yaml, order.yaml, VISA2014_MIGRATION.md

### 2026-09-04 - ApplicationProfileInstancePerson App_Inv roster .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity ApplicationProfileInstancePerson --legacy-source calik-energi-local-pg --inprocess --no-wait --skip-tenant-catalog-generation --batch-size 50` (no `--verbose`; bin exe after `Visa2026.DataImporter.csproj` Debug build)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Scope**: App_Inv headers already in `ApplicationProfileInstance.json` (**1810** keys). Person id-map **3404**. Do not use `import/ApplicationPeople.ps1` without `-LegacySource calik-energi-local-pg` (script default is `calik-energi`).
- **Counts**: Legacy SQL **22762** -> Prepared **22554** / Posted **3277** / Failed **0** / Skipped (prepare) **208** / Skipped (missing id-map) **19277** / Skipped (already imported) **0**
- **Missing id-map skips**: expected (PersonInApplication whose Application is not App_Inv). Not a halt.
- **Reconcile**: PG `"ApplicationProfileInstancePeople"` **3277** (table has no `GCRecord`); `"ApplicationProfileInstancePersonResolvedLinks"` **16357** (`GCRecord = 0`). Importer printed ResolvedLinks created (sum) **0** — trust PG count, not that counter (LoadLinks before commit undercounts).
- **Id-map**: bin `ApplicationProfileInstancePerson.json` **3277** keys copied to source (source was `{}` Length=2)
- **Log**: `artifacts/headless-import/ApplicationProfileInstancePerson-App_Inv-20260904.log`
- **UI**: DetailView People List + linked-record tiles come from this roster + ResolvedLinks; do not reimport Passport/Education/Salary. ListView Person count should no longer be 0.
- **Next**: Invitation headers (issued records; skip WorkPermit for App_Inv), then InvitationItem, then ApplicationProfileInstanceProgress.

### 2026-09-04 - AddressOfResidence import .15 -> local PG (OtherSite seed + resume)

- **Phase**: import
- **Mode**: `--import-visa2014 --entity AddressOfResidence --legacy-source calik-energi-local-pg --inprocess --no-wait` (no `--verbose`, no Person reimport, no People truncate, ApplicationProfileInstance / MedicalRecord not started)
- **Outcome**: success after one OtherSite seed + resume (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Encoding check**: `address-of-residence-property-map.mmd`, `address-of-residence-gaps-and-defaults.mmd`, `docs/diagrams/import/README.md` were already UTF-8 no BOM (first bytes `66 6C 6F 77` / `66 6C 6F 77` / `23 20 49 6D`, not `FF FE`)
- **Person id-map**: source + bin `Person.json` both **3404** keys (no copy needed)
- **Fail 1**: exit **1** - Prepared **4152** / Posted **5283** / Failed **3** / Skipped (transform) **3** / Skipped (no Person map) **0** / Skipped (already imported) **0** / PIA-inferred posted **1132** failed **2**. Error: `incomplete OData payload` OtherSite (1 main + 2 silent PIA payload-null). Not Lodging/Hotel/Hospital/City NameTm.
- **Cause**: live `.15` Lojman-as-Other FullAddress not in PG `OtherSites` (catalog row truncated vs live DISTINCT last segment; do not invent labels). Lodging/Hotel/Hospital/OtherSite `CityID` all null in this DB - city-scoped exact match cannot hit; scalar fallback needs the live string.
- **Seed**: sqlcmd DISTINCT Lojman AddressLine from `10.100.128.15` / `VISA2015`; INSERT 1 `OtherSites` row (omit `GCRecord`; `CityID` = Asgabat city; FullAddress from live last comma-segment, spaces collapsed). Appended one row to `other-site.calik-energi.json` without ConvertTo-Json rewrite; copied overlay to Module `other-site.json` + DataImporter/Blazor `LookupCatalogs\tenant`.
- **Resume**: Posted **3** / Failed **0** / Skipped (already imported) **4151** / PIA-inferred posted **2** skipped **1132** failed **0**. Combined Posted **5286**.
- **PG AddressesOfResidence**: `GCRecord = 0` = **5286** (column is NOT NULL DEFAULT 0; do not use `IS NULL`; count via `psql -f` so quoted identifiers survive)
- **Id-map**: copied bin `AddressOfResidence.json` -> source `Visa2026.DataImporter/legacy/visa2014/id-maps/calik-energi-local-pg/AddressOfResidence.json` (612626 bytes, **7381** keys including aliases)
- **Log**: `artifacts/headless-import/AddressOfResidence-20260904.log` (fail 1) + `artifacts/headless-import/AddressOfResidence-20260904-resume.log` (success)
- **Next**: ApplicationProfileInstance (`order.yaml`). MedicalRecord is attachments/file wave - not started.
- **Labels**: do not paste NameTm / AddressLine into this file from a console capture (CP437/mojibake). Canonical OtherSite text is `other-site.calik-energi.json`.
### 2026-09-04 - EmployeeSalary import .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity EmployeeSalary --legacy-source calik-energi-local-pg --inprocess --no-wait` (no `--verbose`, no Person reimport, no People truncate, AddressOfResidence not started)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Encoding check**: `employee-salary-property-map.mmd`, `employee-salary-gaps-and-defaults.mmd`, `docs/diagrams/import/README.md` were already UTF-8 no BOM (first bytes `66 6C 6F 77` / `66 6C 6F 77` / `23 20 49 6D`, not `FF FE`)
- **Person id-map**: source + bin `Person.json` both **3404** keys (no copy needed)
- **Counts**: Legacy SQL **3111** -> Prepared **3048** / Posted **3048** / Failed **0** / Skipped (transform) **63** / Skipped (no Person map) **0** / Skipped (already imported) **0** / Dedupe **0**
- **Transform skips**: 63 expected (empty/unparseable Salary.Detail and/or missing Salary FK) - not a halt
- **PG EmployeeSalaries**: `GCRecord = 0` = **3048** (column is NOT NULL DEFAULT 0; do not use `IS NULL`; count via `psql -f` so quoted identifiers survive)
- **Id-map**: copied bin `EmployeeSalary.json` -> source `Visa2026.DataImporter/legacy/visa2014/id-maps/calik-energi-local-pg/EmployeeSalary.json` (252987 bytes, **3048** keys)
- **Log**: `artifacts/headless-import/EmployeeSalary-20260904.log`
- **Next**: AddressOfResidence (`order.yaml`; not started)

### 2026-09-04 - EmployeePositionHistory import .15 -> local PG

- **Phase**: import
- **Mode**: `--import-visa2014 --entity EmployeePositionHistory --legacy-source calik-energi-local-pg --inprocess --no-wait` (no `--verbose`, no `--supplement-permit-positions`, no Person/Education reimport, no People truncate)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Encoding check**: `employee-position-history-property-map.mmd`, `employee-position-history-gaps-and-defaults.mmd`, `docs/diagrams/import/README.md`, `PositionDepartmentLookup-CalikEnergi.ps1` were already UTF-8 no BOM (first bytes `66 6C 6F 77` / `66 6C 6F 77` / `23 20 49 6D` / `23 20 47 65`, not `FF FE`)
- **Person id-map**: source + bin `Person.json` both **3404** keys (no copy needed)
- **Counts**: Legacy SQL **3162** -> Prepared **3162** / Posted **3162** / Failed **0** / Skipped **0** / Skipped (no Person map) **0** / Skipped (already imported) **0** / ActualPositions created **55**
- **Lookup seed**: not needed (FailedCount 0; Position/Department NameTm already present)
- **PG EmployeePositionHistories**: `GCRecord = 0` = **3162** (column is NOT NULL DEFAULT 0; do not use `IS NULL`; count via `psql -f` so quoted identifiers survive)
- **Id-map**: copied bin `EmployeePositionHistory.json` -> source `Visa2026.DataImporter/legacy/visa2014/id-maps/calik-energi-local-pg/EmployeePositionHistory.json` (262449 bytes)
- **Log**: `artifacts/headless-import/EmployeePositionHistory-20260904.log`
- **Next**: EmployeeSalary

### 2026-09-04 - Education lookup seed + resume import (.15 -> local PG)

- **Phase**: import
- **Mode**: seed `Seed-EducationLookupGapsToPostgres.ps1` then `--import-visa2014 --entity Education --inprocess --no-wait` (no `--verbose`, no Person reimport, no People truncate)
- **Outcome**: success (importer exit **0**, FailedCount **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` <- `10.100.128.15` / `VISA2015`
- **Encoding check**: `Seed-EducationLookupGapsToPostgres.ps1` and `EducationLookup-CalikEnergi.ps1` were already UTF-8 no BOM (first bytes `23 20 53 65` / `23 20 47 65`, not `FF FE`)
- **Seed source**: sqlcmd `Server=10.100.128.15 Database=VISA2015` DISTINCT institutions=**1571** specialties=**1124**
- **PG NameTm before seed**: institutions=**1513** specialties=**1099**
- **PG gaps printed**: institutions=**1281** specialties=**993** (ordinal compare vs psql NameTm still inflated; most already existed - INSERT skipped)
- **JSON gaps (real missing labels)**: institutions=**57** specialties=**31**
- **PG INSERT**: `INSERT 0 1` = **106** new rows; `INSERT 0 0` = **2168** already present; `GCRecord` is NOT NULL default **0** (do not INSERT NULL)
- **Seed script fixes this run**: psql `-c` stripped quoted identifiers (`educationinstitutions`); switched SELECT to `-f` temp SQL; omit `GCRecord` on INSERT; set `PGCLIENTENCODING=UTF8`
- **Education import**: Legacy SQL **3276** -> Prepared **3276** / Posted **63** / Failed **0** / Skipped (already imported) **3213** / Skipped (no Person map) **0**
- **PG Educations**: user query `WHERE "GCRecord" IS NULL` = **0** (column NOT NULL); live rows `GCRecord = 0` = **3276** / total **3276**
- **Id-map**: copied bin `Education.json` -> source `Visa2026.DataImporter/legacy/visa2014/id-maps/calik-energi-local-pg/Education.json` (271911 bytes)
- **Log**: `artifacts/headless-import/Education-resume-20260904.log`
- **Next**: EmployeePositionHistory
- **Labels**: do not paste NameTm into this file from a console capture (CP437/mojibake). Canonical text is `education-institution.calik-energi.json` / `specialty.calik-energi.json`. Examples that failed the first Education wave: Chungnam milli uniwersiteti, Berlin Tehnik uniwersiteti, Liverpool John Moore uniwersiteti, Suncheon/Yeosu ýörite orta hünärmen, SRI Satya Sai I.T.C.

### 2026-09-04 — Person import .15 → local PG failed (IX_People_PersonalNumber)

- **Phase**: import
- **Mode**: single-entity (`--import-visa2014 --entity Person --inprocess --verbose`)
- **Outcome**: failed / in-progress grind — `23505` on every batch save
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg` ← `10.100.128.15` / `VISA2015`
- **Preflight**: exit **0** (Person transform import=**3404** skipped=0 unmapped=0 blocking=0)
- **First attempt**: exit **1** — leftover DataImporter still bound `http://127.0.0.1:5002` after preflight
- **Retry**: killed PID 18872; import hit `23505 IX_People_PersonalNumber`
- **Actual cause**: overlapping second Person run. First wave (`Person-20260904-113417.log`) already Posted **3404** / Failed **0**. Index **does** exclude sentinel `'0'`. Do **not** TRUNCATE People (would CASCADE Passports **3759** / Visas **6294** / Educations **3213**).
- **Reconcile**: People **3404** = id-map **3404**; PN `0` **569**; real PN dupes **0**
- **Log**: `artifacts/headless-import/Person-20260904.log` (overlap) · success log `Person-20260904-113417.log`
- **Next**: Education (63 failed institutions) — not Person retry

### 2026-09-04 — Education import .15 → local PG failed (lookup gaps)

- **Phase**: import
- **Mode**: single-entity (`--entity Education --inprocess`)
- **Outcome**: failed (exit **1**) — **chain halt** (do not start EmployeePositionHistory)
- **Counts**: Legacy SQL **3276** → Prepared **3276** / Posted **3213** / Failed **63** / no Person map **0**
- **Error**: `incomplete OData payload` for EducationInstitution (e.g. Chungnam milli uniwersiteti, Berlin Tehnik uniwersiteti, Liverpool John Moore, Suncheon/Yeosu ýrite orta hünärmen, SRI Satya Sai I.T.C.)
- **Cause**: live `.15` labels not in tenant `education-institution.json` / `specialty.json` (same class as 2026-08-13 11-row gap)
- **Fix / next**: seed missing Institution/Specialty from `.15` DISTINCT → tenant JSON + local PG INSERT; resume `--entity Education` (id-map already has 3213)
- **Log**: `artifacts/headless-import/Education-20260904-114002.log`
### 2026-09-04 — Visa import .15 → local PG (retry after stale IsCancelled columns)

- **Phase**: import
- **Mode**: single-entity (`--entity Visa --inprocess`)
- **Outcome**: success on retry (exit **0**)
- **Fail 1**: `23502` null `"Visas"."IsCancelled"` — column leftover after `[NotMapped]` lifecycle; headless skip of `IssuedDocumentStatusColumnsCleanupUpdater`
- **Fix**: applied `IssuedDocumentStatusColumnsCleanupSchemaSql` DROP COLUMN (also InvitationItem/WorkPermitItem/BorderZone status cols). Stuck first process held `:5002`; killed then retry.
- **Counts**: Legacy SQL **6344** → Prepared **6319** / Posted **6294** / Failed **0** / transform skipped **19** / Dedupe **6** / no Passport map **25**
- **IssuingApplicationProfileInstance**: patched **0** (instance id-map empty — expected until ApplicationProfileInstance wave)
- **Log**: `artifacts/headless-import/Visa-retry2-*.log`
- **Follow-up**: Education next
### 2026-09-04 — Passport import .15 → local PG

- **Phase**: import
- **Mode**: single-entity (`--entity Passport --inprocess`)
- **Outcome**: success (exit **0**)
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Target**: local PostgreSQL `visa2026`
- **Counts**: Legacy SQL **3840** → Prepared **3759** / Posted **3759** / Failed **0** / transform skipped **79** / Dedupe **2** / no Person map **0**
- **Id-map**: `id-maps/calik-energi-local-pg/Passport.json` (bin copied to source)
- **Log**: `artifacts/headless-import/Passport-20260904-113521.log`
- **Follow-up**: Visa next
### 2026-09-04 — Person import .15 → local PG (after transactional wipe)

- **Phase**: import
- **Mode**: single-entity (`--import-visa2014 --entity Person --inprocess`)
- **Outcome**: success (exit **0**)
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Target**: local PostgreSQL `visa2026`
- **Counts**: Legacy SQL **3404** → Prepared **3404** / Posted **3404** / Failed **0** / Dedupe merged **21**
- **Id-map**: bin + source `id-maps/calik-energi-local-pg/Person.json`
- **Log**: `artifacts/headless-import/Person-20260904-113417.log`
- **Follow-up**: Passport next (order.yaml)
### 2026-09-04 — Wipe local PG transactional data (before ordered .15 import)

- **Phase**: cleanup
- **Target**: local PostgreSQL `visa2026` only
- **Script**: `scripts/visa2014-migration/cleanup/Wipe-LocalPostgresTransactional.sql`
- **Fix**: table name `"BorderZoneItem"` → `"BorderZoneItems"` (missing name would fail TRUNCATE)
- **Outcome**: success — People/Passports/Visas/Educations/AOR/EPH/Salary/Medical/Instances/WP/Inv → **0**
- **CASCADE**: PersonExportBatches + ApplicationProfileInstance* M2M join tables
- **Id-maps**: 53 JSON files under `calik-energi-local-pg` (source + bin) reset to `{}`
- **Kept**: ApplicationProfiles **38**, EducationInstitutions **1528**, Specialties **1104**, ProjectContracts **72**, Lodgings **59**
- **Also truncated** (script list): WorkPermitLocations **0**, BusinessTripAddress **0** — expect ModuleUpdater / F5 seed before WP / business-trip waves
- **Next**: `--import-visa2014 --entity Person` from `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`), then order.yaml
### 2026-09-04 — Wipe local PG transactional data (before ordered .15 import)

- **Phase**: cleanup
- **Mode**: full transactional wipe (`Wipe-LocalPostgresTransactional.sql`)
- **Outcome**: success
- **Target**: local PostgreSQL `visa2026` only (never VISA2015 / `.15` / Demo/Prod)
- **Before**: People **3339**, Passports **3689**, Visas **6234**, Educations **3211**, EPH **3090**, AOR **5208**, WP **412** / items **3899**, Inv **2930** / items **5218**
- **After**: all those transactional tables **0**; ApplicationProfileInstances / roster / progress **0**
- **Preserved**: ApplicationProfiles **38**, EducationInstitutions **1528**, Specialties **1104**, ProjectContracts **72**, Lodgings **59**, Users **7**
- **Id-maps**: all `calik-energi-local-pg` JSON (source + bin) → `{}` (bak-wipe-20260904 retained)
- **Next**: ordered import from `10.100.128.15` / `VISA2015` starting at **Person** (`calik-energi-local-pg`)
### 2026-09-04 — Hard-delete ApplicationProfileInstances (local PG, before .15 reimport)

- **Phase**: cleanup
- **Target**: local PostgreSQL `visa2026` only (never VISA2015 / `.15`)
- **Script**: `scripts/visa2014-migration/cleanup/ImportedApplications.postgres.sql` (rewritten for ApplicationProfileInstance; unlink headers first)
- **Why unlink first**: `"WorkPermits"` / `"Invitations"` / `"Rejections"` FKs are **ON DELETE CASCADE** — a bare instance DELETE would wipe those letters
- **Action**: NULL LatestProgressId + WP/Inv/Rejection/BorderZone/Visa issuing/WordReport FKs, then `DELETE FROM "ApplicationProfileInstances"` (**31** rows); progress **34**, people **38**, ResolvedLinks **171**, snapshots **58** CASCADE
- **Preserved**: ApplicationProfiles **38**; People **3339**; WorkPermits **412**; Invitations **2930** (unlinked)
- **Id-maps cleared** (`calik-energi-local-pg` source + bin): ApplicationProfileInstance / Progress / Person + sync-progress → `{}` (bak-20260904-112400 retained). Person/Passport/Visa/WP/Inv maps left intact for the next `.15` wave
- **Next**: import ApplicationProfileInstance from legacy `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
### 2026-08-22 — Invitation issuing origin (instance-side create + roster output lines)

- **Officer**: issued invitation origin = `Invitation.ApplicationProfileInstance` (1:N from instance `Invitations`); not root Invitation list **New**. `InvitationItem` rows on the letter are output lines (one per roster person), not input M2M links.
- **Import**: existing `Invitation.yaml` → `ApplicationProfileInstance`; `Visa2014InvitationODataImporter` backfills like WorkPermit. Import does not run roster helper (`MigrationImportContext`).
- **Order**: import ApplicationProfileInstance waves before Invitation if issuing FK should populate.


### 2026-08-22 — Visa IssuingApplicationProfileInstance (Path B + officer instance-only create)

- **Officer**: issued visa origin = `Visa.IssuingApplicationProfileInstance` (1:N from instance `IssuedVisas`); not Passport nested New; not input M2M `ApplicationProfileInstances`.
- **Import**: `--entity Visa --inprocess` posts `IssuingApplicationProfileInstance` when Application id-map resolves; re-run backfills existing Visa id-map rows. Post-pass: `--correct-visa2014-issuing-application-profile-instance` (alias `--correct-visa2014-issuing-application-item`).
- **Order**: import ApplicationProfileInstance waves before Visa if issuing FK should populate; InvitationItem correction still after issuing FK.


### 2026-08-22 — Person-related item links respect Application Profile RequirePerson*

- **Rule**: link Passport/Visa/WorkPermitItem/InvitationItem/… onto ApplicationProfileInstance only when the instance’s Application Profile has the matching **Required person-related data** checkbox on.
- **Prevent**: do not run blanket existing-item M2M for kinds the profile has off (e.g. App_Visa_and_WP_Ext → no WorkPermitItem / InvitationItem auto-link).


### 2026-08-22 — Hard-delete all ApplicationProfileInstances (local PG restart)

- **Phase**: cleanup
- **Target**: local PostgreSQL `visa2026` only
- **Action**: NULL header FKs on WorkPermit/Invitation/Rejection/BorderZone/Visa issuing/WordReport batches, then `DELETE FROM "ApplicationProfileInstances"` (**3754** rows); children/M2M/progress CASCADE
- **Preserved**: WorkPermit headers **410**, Invitation headers **2917** (unlinked); Person and other master data untouched
- **Id-maps cleared** (`calik-energi-local-pg`): ApplicationProfileInstance / Progress / Person → `{}` (bak copies retained)
- **Next**: reimport one ApplicationType at a time per instruction


### 2026-08-22 — App_Visa_and_WP_Ext full wave .15 → local PG

- **Phase**: import
- **Mode**: type-filtered ApplicationProfileInstance → progress → ApplicationProfileInstancePerson → WP/Inv header + existing-item links
- **Outcome**: success (all exit **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg`
- **Apps**: Posted **767** / Failed **0**; id-map **2979→3746**; PG apps **768** (incl. 1 seed)
- **Progress**: Posted **3741** / Failed **0** / already **14562** / no app map **17894**
- **People**: Posted **2482** / Failed **0** / already **5469** / missing id-map **14398**
- **WorkPermit headers**: Application FK patched **+133** (total with app **360**); App_Visa_and_WP_Ext: **104** apps / **133** WP headers
- **Invitation headers**: patched **0** (type does not issue invitations)
- **Existing items**: WorkPermitItem ResolvedLink/M2M **2477**; InvitationItem **0**
- **Logs**: `import-App_Visa_and_WP_Ext-*`, `import-ApplicationProgress-App_Visa_and_WP_Ext-*`, `import-ApplicationPerson-App_Visa_and_WP_Ext-*`, `*-VisaWPExt-*`


### 2026-08-22 — Newly issued WP/Inv vs existing item M2M (App_Inv_And_WP)

- **Phase**: correction / link
- **Decision**:
  - **Newly issued** `WorkPermit` / `Invitation` headers → `ApplicationProfileInstance` FK
    - WP: `WorkPermit.ProcessNumber` → `PersonInApplication` → `Application` (majority per letter)
    - Inv: `ApplicationResult.Application` (unchanged)
  - **Existing** on PIA → `WorkPermitItem` / `InvitationItem` only via ResolvedLink + M2M (`ApplicationProfileInstance.WorkPermitItems` / `InvitationItems`)
    - WP: `PersonInApplication.WorkPermit`
    - Inv: `InvitationToBeCancelled` → `PersonInInvitation` (Employee/FamilyMember match)
  - No `CurrentWorkPermitItem` / `CurrentInvitationItem` for Application Profile Instance import path
- **Code**: `Visa2014WorkPermitTransform`/`ODataImporter` Application FK + backfill; `--correct-visa2014-existing-item-links`
- **WorkPermit header re-run** (`--entity WorkPermit --inprocess`): Posted **1** / already **408** / **Application FK patched 227**; exit **0**
- **Local PG App_Inv_And_WP**: apps **1183**; with Invitation **939**; with WorkPermit **197** (227 WP headers); **183** WP headers still null FK (apps not in id-map yet)
- **Existing-item correction**: PIA scope **3915**; linked **0** — distinct apps with existing WP/Inv (**1551**) are **not** in current ApplicationProfileInstance id-map (Inv/Inv_And_WP waves); will apply when those types are imported
- **Logs**: `import-WorkPermit-link-20260822-083642.log`, `correct-existing-item-links-20260822-083722.log`


### 2026-08-21 — Invitation ApplicationProfileInstance FK backfill (App_Inv + Inv_And_WP)

- **Phase**: correction / link
- **Mode**: `--import-visa2014 --entity Invitation --inprocess` (re-run with app id-map present)
- **Outcome**: success exit **0**
- **Cause**: invitations imported earlier with empty Application id-map; payload also used obsolete `Application` key (BO is `ApplicationProfileInstance`) → all FKs null
- **Fix**: payload key `ApplicationProfileInstance`; on already-imported rows, patch FK when app now in id-map
- **Counts**: Posted **11** / Failed **0** / already **2905** / **Application FK patched 2572**; PG with_app linked for imported apps
- **Log**: `import-Invitation-link-20260821-172602.log`

### 2026-08-21 — App_Inv full wave (apps + progress + people) .15 → local PG

- **Phase**: import
- **Mode**: type-filtered ApplicationProfileInstance → progress → ApplicationProfileInstancePerson
- **Outcome**: success (all exit **0**)
- **Environment**: local PostgreSQL `visa2026` / `calik-energi-local-pg`
- **Apps**: Posted **1798** / Failed **0**; id-map **1181→2979** (kept prior App_Inv_And_WP)
- **Profile patch**: Already correct **2979** (import-time type-only fallback set `get_invitation`)
- **Progress**: Posted **8750** / Failed **0** / already **5812** / no app map **21693**
- **People**: Posted **3212** / Failed **0** / already **2257** / missing id-map **16878**
- **Logs**: `import-App_Inv-*`, `import-ApplicationProgress-App_Inv-*`, `import-ApplicationPerson-App_Inv-*`

### 2026-08-21 — ApplicationProfileInstancePerson roster for App_Inv_And_WP id-map .15 → local PG

- **Phase**: import
- **Mode**: single-entity (`--entity ApplicationProfileInstancePerson --inprocess`, Wave 2b)
- **Outcome**: success exit **0**
- **Environment**: local PostgreSQL `visa2026`
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Scope**: apps in ApplicationProfileInstance id-map (**1181**) + Person id-map (**3339**)
- **Counts**: prepared **22347** → Posted **2257** / Failed **0** / missing id-map **20090** / already **0**; transform skipped **208**
- **Resolved links**: `"ApplicationProfileInstancePersonResolvedLinks"` **9035** (Passport/Visa/Education/… auto-link under RequirePerson*)
- **Note**: UI “People locked” on issued cases is expected; import uses `MigrationImportContext.IsDataImport` so LinkPerson still runs
- **Id-map**: `ApplicationProfileInstancePerson.json` **2257**
- **Log**: `legacy/visa2014/import-logs/import-ApplicationPerson-20260821-165552.log`

### 2026-08-21 — ApplicationProfileInstanceProgress FK rename broke parent link

- **Symptom**: ListView all **At office**, Progress date empty after progress import (5812 posted).
- **Cause**: headless payload still used `Application` (old name); BO property is `ApplicationProfileInstance` → **5812 rows with NULL FK**. Status falls back to implied `IS_BEING_PREPARED` (“At office”).
- **Fix**: payload key `ApplicationProfileInstance`; delete null-FK orphans; clear progress id-map; reimport → **1181** apps linked, LatestPrimaryStateCode histogram: PROCESS_ISSUED 936 / CANCELLED 156 / REJECTED 59 / STARTED 30.
- **ListView**: prefer `LatestProgressDisplay` for Status; backfill `LatestProgressId`; `OnSaved` re-Sync so pointer is set after insert.
- **Legacy match**: legacy has no progress table — synthesis from Application scalars; after fix, latest Visa2026 state tracks that synthesis (not literal legacy UI helper 1:1).
- **Log**: `import-ApplicationProgress-relink-20260821-164108.log`

### 2026-08-21 — ApplicationProfileInstanceProgress for App_Inv_And_WP id-map .15 → local PG

- **Phase**: import
- **Mode**: single-entity (`--import-visa2014 --entity ApplicationProfileInstanceProgress --inprocess`)
- **Outcome**: success exit **0**
- **Environment**: local PostgreSQL `visa2026`
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Scope**: apps in `ApplicationProfileInstance.json` only (**1181** App_Inv_And_WP)
- **Counts**: legacy apps **12594** → prepared steps **36437** → Posted **5812** / Failed **0** / no app map **30625** / already **0**; parent-skipped **165**
- **Pre-fix**: completion-index VisaExtension SQL used `PersonInApplication.ApplicationProfileInstance` (Visa2026 name) — legacy column is **`Application`** → `Invalid column name`; restored `pia*.Application`
- **Id-map**: `ApplicationProfileInstanceProgress.json`
- **Log**: `legacy/visa2014/import-logs/import-ApplicationProgress-20260821-163326.log`

### 2026-08-21 — ApplicationProfileInstance App_Inv_And_WP type-filtered import .15 → local PG

- **Phase**: import
- **Mode**: single-entity (`--import-visa2014 --entity ApplicationProfileInstance --inprocess`)
- **Outcome**: success exit **0**
- **Environment**: local PostgreSQL `visa2026`
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Filter**: `--application-type App_Inv_And_WP` (new CLI; filter after transform by Visa2026 `ApplicationType.Name`)
- **Counts**: legacy SQL **12594** → prepared type match **1181** → Posted **1181** / Failed **0** / Already imported **0**; transform skipped **170** (all types)
- **Id-map**: `id-maps/calik-energi-local-pg/ApplicationProfileInstance.json` **1181** entries
- **CLI**: `--application-type App_Inv_And_WP --skip-tenant-catalog-generation --batch-size 50 --no-wait`
- **Log**: `legacy/visa2014/import-logs/import-App_Inv_And_WP-20260821-161243.log`
- **Follow-up**: roster (`ApplicationProfileInstancePerson`) + progress; optional `--backfill-application-approval-leg-snapshots` if Ministrlik empty
- **Profile FK gap (same day)**: import left `ApplicationProfile` null — type-only tenant JSON (36 rows, `DefaultProjectContract` null) vs via-ministry legacy contracts. Fixed resolver fallback + Wave 2 patch **1181/1181** → `get_invitation_wp`.

### 2026-08-21 — Wave 2 ApplicationProfile patch after type-only catalog fallback

- **Phase**: correction
- **Mode**: `--patch-visa2014-application-profile`
- **Outcome**: success — Patched **1181** / Failed **0** / Skipped **0**
- **Cause**: tenant `application-profile.calik-energi.json` is type-only (do not restore Wave 0b 176); import resolver required contract-variant match → silent omit of FK
- **Fix**: `ApplicationProfileCatalogGroupKey.FindProfile` + DTO `FindProfileId` fall back to type-only profile when contract variant missing
- **Histogram**: `get_invitation_wp`: 1181
- **Log**: `legacy/visa2014/import-logs/AppProfile-patch-apply.log`

### 2026-08-20 — Phase B: ApplicationProfileInstance approval-leg snapshots from shared catalog

- **Mode**: host-start / F5 (`ApplicationProfileInstanceApprovalLegBackfill` after Default seed) + existing `--backfill-application-approval-leg-snapshots`
- **Rule**: keep inferred instance `ApprovalLegProfile`; Default only when FK is empty; stamp `ApprovalLegVersionName`; fill missing snapshots. No progress delete.
- **CLI**: `dotnet run --project Visa2026.DataImporter -- --backfill-application-approval-leg-snapshots --target-connection "<pg>" --dry-run`
- Cross-skill: application-profile

### 2026-08-15 — InvitationDocument file wave .15 → local PG

- **Mode**: file-wave (`--import-visa2014-files --entity Invitation --property InvitationDocument --inprocess`)
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Target**: local PostgreSQL `visa2026`
- **Gate**: Invitation scalar present (id-map **2905**, `"Invitations"` **2906**); InvitationDocument id-map started empty
- **Pilot**: MaxRows 10 — Posted **9** / Failed **0** / No parent map **1** (Rejection ApplicationResult in TOP 10)
- **Full outcome**: success exit **0** — Posted **2998** / Failed **0** / No parent map **203** (Result=1 Rejection copies; Invitation id-map is Result=0 only) / No blob **5** / Already imported **9**
- **Legacy**: PassportCopy ApplicationResult-FK rows **3215** = 2998+9+203+5
- **Reconcile**: `"InvitationDocuments"` **3007** (2998+9); id-map `InvitationDocument.json` **3007** (copied source + bin)
- **CLI**: `Visa2026.DataImporter.exe --import-visa2014-files --entity Invitation --property InvitationDocument --legacy-source calik-energi-local-pg --inprocess --target-connection <local PG> --no-wait`
- **Log**: `artifacts/document-copies-import/InvitationDocument.log`
- **Not run**: FamilyProofDocument / MedicalRecordDocument
### 2026-08-15 — WorkPermitDocument file wave .15 → local PG

- **Mode**: file-wave (`--import-visa2014-files --entity WorkPermit --property WorkPermitDocument --inprocess`)
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Target**: local PostgreSQL `visa2026`
- **Gate**: WorkPermit scalar present (id-map **408**, `"WorkPermits"` **409**); WorkPermitDocument id-map started empty
- **Pre-fix**: `Visa2014PassportCopyLinkedDocumentImporter` now retries `.15` blob on a new SqlConnection + original `legacyConnectionString`, CommandTimeout 180, skip already-imported before blob read
- **Pilot**: MaxRows 10 — Posted **10** / Failed **0**; `"WorkPermitDocuments"` **10**
- **Full outcome**: success exit **0** — Posted **1007** / Failed **0** / No parent map **0** / No blob **2** / Already imported **10**
- **Legacy**: PassportCopy WorkPermitLetter-FK rows **1019** = 1007+10+2
- **Reconcile**: `"WorkPermitDocuments"` **1017** (1007+10); id-map `WorkPermitDocument.json` **1017** (copied source + bin)
- **CLI**: `Visa2026.DataImporter.exe --import-visa2014-files --entity WorkPermit --property WorkPermitDocument --legacy-source calik-energi-local-pg --inprocess --target-connection <local PG> --no-wait`
- **Log**: `artifacts/document-copies-import/WorkPermitDocument.log`
- **Not run yet**: InvitationDocument / FamilyProofDocument / MedicalRecordDocument
### 2026-08-15 — Invitation + InvitationItem import .15 → local PG

- **Mode**: single-entity (`--import-visa2014 --entity Invitation` then `InvitationItem --inprocess`)
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Target**: local PostgreSQL `visa2026`
- **Preflight**: `--entity Invitation,InvitationItem` exit **0** (Phase B has no Invitation transform hook; Phase A blocking=0)
- **Invitation**: Prepared **2905** / Posted **2905** / Failed **0** / ApplicationProfileInstance not in id-map **2905** (headers still posted). `"Invitations"` **2906** (2905 + 1 seed); id-map **2905**
- **InvitationItem**: Prepared **5211** / Posted **5183** / Failed **0** / Skipped missing required id-map **28**
- **Reconcile**: `"InvitationItems"` **5183** (all Person + header); id-map **5183** (copied source + bin)
- **Logs**: `artifacts/headless-import/Invitation.log`, `InvitationItem.log`
- **Not run**: ApplicationProfileInstance / ApplicationProfileInstancePerson / file waves
### 2026-08-15 — WorkPermit + WorkPermitItem import .15 → local PG

- **Mode**: single-entity (`--import-visa2014 --entity WorkPermit` then `WorkPermitItem --inprocess`)
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Target**: local PostgreSQL `visa2026`
- **Preflight**: `--entity WorkPermit,WorkPermitItem` exit **0** (WorkPermitItem import=6460 skipped=0 unmapped=0)
- **WorkPermit**: Prepared **408** / Posted **408** / Failed **0**. `"WorkPermits"` **409** (408 + 1 seed); id-map **408**. `ApplicationProfileInstanceID` all null (Application wave not imported)
- **WorkPermitItem**: Prepared **6460** / Posted **3894** / Failed **0** / Skipped missing required id-map **2566** (same EPH/position-history gap class as 2026-07-03 2613) / Position fallback **47**
- **Reconcile**: `"WorkPermitItems"` **3894** (all Person + header); id-map **3894** (copied source + bin)
- **Logs**: `artifacts/headless-import/WorkPermit.log`, `WorkPermitItem.log`
- **Not run yet**: Invitation / InvitationItem
### 2026-08-15 — AddressOfResidence import .15 → local PG (single entity)

- **Mode**: single-entity (`--import-visa2014 --entity AddressOfResidence --inprocess`)
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Target**: local PostgreSQL `visa2026`
- **Preflight**: `--entity AddressOfResidence` exit **0** (import=4103 skipped=3 unmapped=3 blocking=0)
- **Outcome**: success — Prepared **4103** / Posted **5208** / Failed **0** / Skipped (no Person map) **0** / PIA-inferred **1105** (1 skipped)
- **Reconcile**: `"AddressesOfResidence"` **5208** (all with `PersonID`); id-map **7276** keys (aliases; copied source + bin)
- **Log**: `artifacts/headless-import/AddressOfResidence.log`
- **Not run yet**: WorkPermit / Invitation (ApplicationProfileInstance id-map still empty — Application FK optional)
### 2026-08-15 — EducationDocument (diploma copies) file wave .15 → local PG

- **Mode**: file-wave (`--import-visa2014-files --entity Education --property EducationDocument --inprocess`)
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Target**: local PostgreSQL `visa2026`
- **Gate**: Education scalar present (id-map **3211**, `"Educations"` **3211**); EducationDocument id-map started empty
- **Pre-fix**: same `.15` transport retry as PassportCopy/VisaDocument (new SqlConnection + original `legacyConnectionString`, CommandTimeout 180); skip already-imported before blob read; periodic id-map save every 100 posts
- **Pilot**: MaxRows 10 — Posted **10** / Failed **0**; `"EducationDocument"` **10**
- **Full outcome**: success exit **0** — Posted **4321** / Failed **0** / No education map **1** / No blob **34** / Oversize (>5MB) **40** / Already imported **10** / Duplicate blob **16**
- **Legacy**: `dbo.PassportCopy` Education-FK rows **4422** = 4321+10+1+34+40+16
- **Reconcile**: `"EducationDocument"` **4331** (4321+10); id-map `EducationDocument.json` **4331** (copied source + bin); all have `FileID`
- **WS**: ~2.5 GB at ~1900 posts; finished ~9.5 min (10:07–10:17); no transport failures
- **CLI**: `Visa2026.DataImporter.exe --import-visa2014-files --entity Education --property EducationDocument --legacy-source calik-energi-local-pg --inprocess --target-connection <local PG> --no-wait`
- **Log**: `artifacts/document-copies-import/EducationDocument.log`
- **Not run**: WorkPermitDocument / InvitationDocument / FamilyProofDocument / MedicalRecordDocument
### 2026-08-15 — VisaDocument file wave .15 → local PG

- **Mode**: file-wave (`--import-visa2014-files --entity Visa --property VisaDocument --inprocess`)
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Target**: local PostgreSQL `visa2026`
- **Gate**: Visa scalar present (id-map **6224**, `"Visas"` **6224**); VisaDocument id-map started empty
- **Pre-fix**: same `.15` transport retry as PassportCopy (new SqlConnection + original `legacyConnectionString`, CommandTimeout 180)
- **Pilot**: MaxRows 10 — Posted **10** / Failed **0**; `"VisaDocument"` **10**
- **Full outcome**: success exit **0** — Posted **6014** / Failed **0** / No visa map **68** / No blob **46** / Oversize (>5MB) **154** / Already imported **10**
- **Reconcile**: `"VisaDocument"` **6024** (6014+10); id-map `VisaDocument.json` **6024** (copied source + bin); all have `FileData`
- **WS**: climbed to ~6.8 GB by ~5800 posts (Flush per row; no OOM). ~21 min (09:43–10:04)
- **CLI**: `Visa2026.DataImporter.exe --import-visa2014-files --entity Visa --property VisaDocument --legacy-source calik-energi-local-pg --inprocess --target-connection <local PG> --no-wait`
- **Log**: `artifacts/document-copies-import/VisaDocument.log`
- **Not run**: EducationDocument / WorkPermitDocument / other DocumentCopies steps
### 2026-08-15 — ApplicationProfileInstancePerson ResolvedLinks keep historical expired rows

- **Mode**: code (no import run) — Wave 2b `LinkPerson` / `RefreshResolvedLinks`
- Officer §10.2 valid/not-expired gate must **not** apply under `MigrationImportContext.IsDataImport`. Import uses `PersonCurrentItems` (latest current, including expired) so past related passport/visa/WP/invitation/border-zone/medical still auto-link.
- Verify: Module.Tests `CollectMissingAutoLinks_AllowsExpiredPassportDuringDataImport`. Reimport ApplicationProfileInstancePerson only if a prior run happened while the officer gate was on and historical links are missing.
- Prevent: Do not filter import ResolvedLinks by `CanLink*` / expiration.
- Cross-skill: visa2026-application-profile

### 2026-08-15 — PassportDocument file wave .15 → local PG

- **Mode**: file-wave (`--import-visa2014-files --entity Passport --property PassportDocument --inprocess`)
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Target**: local PostgreSQL `visa2026`
- **Gate**: Passport scalar present (id-map **3690**, `"Passports"` **3689**); PassportCopy id-map started empty
- **Fail 1**: first full run Posted **1312** / Failed **2370** — transport-level error on `.15` closed the reused SqlConnection; later reads `BeginExecuteReader` on closed connection. Log: `artifacts/document-copies-import/PassportDocument.log`
- **Fix**: skip already-imported before blob read; retry blob on a **new** SqlConnection using the original `legacyConnectionString` (do not reuse `SqlConnection.ConnectionString` after Open — password is stripped → `Login failed for user ReadOnlyUser`); CommandTimeout 180
- **Resume**: Posted **2347** + already **1322** = **3669**; Failed **1** (same transport class, retry then succeeded on next process)
- **Final**: exit **0** — Posted **3670** / Failed **0** / No passport map **12** / No blob **1** / Oversize (>5MB) **37** / Already imported on last pass **3670**. Legacy rows **3720** = 3670+12+1+37
- **Reconcile**: `"PassportDocuments"` **3670**; id-map `PassportCopy.json` **3670** (copied source + bin)
- **CLI**: `Visa2026.DataImporter.exe --import-visa2014-files --entity Passport --property PassportDocument --legacy-source calik-energi-local-pg --inprocess --target-connection <local PG> --no-wait`
- **Logs**: `artifacts/document-copies-import/PassportDocument.log`, `PassportDocument-resume.log`, `PassportDocument-resume2.log`, `PassportDocument-resume3.log`
- **Not run**: VisaDocument / EducationDocument / other DocumentCopies steps
### 2026-08-15 — Person.Photo file wave .15 → local PG

- **Mode**: file-wave (`--import-visa2014-files --entity Person --property Photo --inprocess`)
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Target**: local PostgreSQL `visa2026`
- **Gate**: Person scalar already imported (id-map **3339**); People `"GCRecord"=0` **3339**; photos were all null
- **Fail (pilot MaxRows 10)**: CLI reported Patched **10** / Failed **0** but PG still `with_photo=0`. Root cause: `Visa2014PersonPhotoImporter` never called `FlushAsync()`; ObjectSpace commits only at `--batch-size` (50). Remainder discarded on session dispose.
- **Fix**: `FlushAsync()` at end of Person photo importer; `Visa2014HeadlessImportSession.DisposeAsync` also flushes leftover batch.
- **Pilot after fix**: MaxRows 10 — Patched **10** / Failed **0**; PG `with_photo=10`.
- **Full outcome**: success — Processed **3339** / Patched **3268** / No blob **71** / Failed **0** (exit 0)
- **Reconcile**: `"People"` **3339** (`GCRecord=0`); `with_photo` **3268**; `no_photo` **71**; photo bytes **115,589,822** (avg ~35 KB after `ProcessPassportPhoto`)
- **CLI**: `Visa2026.DataImporter.exe --import-visa2014-files --entity Person --property Photo --legacy-source calik-energi-local-pg --inprocess --target-connection <local PG> --no-wait`
- **Log**: `artifacts/document-copies-import/Person-Photo.log`
- **Not run**: PassportDocument / other DocumentCopies steps (Person.Photo only)
### 2026-08-13 — EmployeePositionHistory import .15 → local PG (single entity)

- **Mode**: single-entity (`--import-visa2014 --entity EmployeePositionHistory --inprocess`)
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Target**: local PostgreSQL `visa2026`
- **Preflight**: `--entity EmployeePositionHistory` exit **0** (import=3090 skipped=0 unmapped=0)
- **Outcome**: success — Prepared **3090** / Posted **3090** / Failed **0** / Skipped (no Person map) **0** / ActualPositions created **1383**
- **Reconcile**: `"EmployeePositionHistories"` **3090** (all with `PersonID`); id-map **3090** keys (copied to source)
- **Log**: `artifacts/headless-import/EmployeePositionHistory-20260813.log`
- **Not run**: EmployeeSalary / AddressOfResidence / chain

### 2026-08-13 — Education import .15 → local PG (single entity)

- **Mode**: single-entity (`--import-visa2014 --entity Education --inprocess`)
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Target**: local PostgreSQL `visa2026`
- **Preflight**: `--entity Education` exit **0** (import=3211 skipped=0 unmapped=0)
- **Outcome**: success — Prepared **3211** / Posted **3211** / Failed **0** / Skipped (no Person map) **0**
- **Reconcile**: `"Educations"` **3211** (all with `PersonID`); id-map **3211** keys (copied to source `id-maps/calik-energi-local-pg/Education.json`)
- **Catalogs**: EducationInstitutions 1528 / Specialties 1104 (prior gap seeds present; no new institution/specialty failures)
- **Log**: `artifacts/headless-import/Education-20260813.log`
- **Not run**: EducationDocument file wave / EPH / chain

### 2026-08-13 — Visa import .15 → local PG (single entity)

- **Mode**: single-entity (`--import-visa2014 --entity Visa --inprocess`)
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Target**: local PostgreSQL `visa2026`
- **Preflight**: `--entity Visa` exit **0** (import=6249 skipped=19 unmapped=8 blocking=0)
- **Outcome**: success — Prepared **6249** / Posted **6224** / Failed **0** / Skipped (no Passport map) **25** / transform skipped **19** / Dedupe merged **6**
- **Reconcile**: `"Visas"` **6224** (all with `PassportID`); id-map **6224** keys (copied to source `id-maps/calik-energi-local-pg/Visa.json`)
- **Log**: `artifacts/headless-import/Visa-20260813.log`
- **Not run**: VisaDocument file wave / issuing-application corrections / Education / chain

### 2026-08-13 — Passport import .15 → local PG (single entity)

- **Mode**: single-entity (`--import-visa2014 --entity Passport --inprocess`)
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Target**: local PostgreSQL `visa2026`
- **Preflight**: `--entity Passport` exit **0** (import=3689 skipped=79 unmapped=0)
- **Outcome**: success — Prepared **3689** / Posted **3689** / Failed **0** / Skipped (no Person map) **0** / transform skipped **79** / Dedupe merged **2**
- **Reconcile**: `"Passports"` **3689** (all with `PersonID`); id-map bin **3690** keys (copied to source `id-maps/calik-energi-local-pg/Passport.json`)
- **Log**: `artifacts/headless-import/Passport-20260813.log`
- **Not run**: PassportDocument file wave / Visa / chain

### 2026-08-13 — Person import .15 → local PG (single entity)

- **Mode**: single-entity (`--import-visa2014 --entity Person --inprocess`)
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Target**: local PostgreSQL `visa2026` (localhost:5432)
- **Preflight**: `--preflight-visa2014-lookups --entity Person` exit **0** (Person transform import=3339 skipped=0 unmapped=0)
- **Outcome**: success — Prepared **3339** / Posted **3339** / Failed **0** / Skipped 0 / Dedupe merged **21**
- **Reconcile**: `"People"` **3339** (`GCRecord=0`); employees (`PersonRole=0`) **3046**; id-map bin **3339** keys (copied to source `id-maps/calik-energi-local-pg/Person.json`)
- **CLI**: `Visa2026.DataImporter.exe --import-visa2014 --entity Person --legacy-source calik-energi-local-pg --inprocess --target-connection <local PG> --no-wait --verbose`
- **Log**: `artifacts/headless-import/Person-20260813.log`
- **Noise**: headless Kestrel `:5002` JWT 500s (`IssuerSigningKey` missing) from browser batch polls — not import failures
- **Not run**: Passport / file waves / `Run-HeadlessChain.ps1` (Person-only as requested)

### 2026-08-13 — Stop import; drop local PG `visa2026` (empty start)

- **Mode**: operator request — no import. Halted Wave 2b resume; no DataImporter was running.
- **Action**: `DROP DATABASE visa2026` + `CREATE DATABASE visa2026` (UTF8, owner postgres) on localhost:5432. `public` table count **0**. Left `visa2026_easytest` alone. Demo/Prod on `.25` untouched.
- **Id-maps**: reset `id-maps/calik-energi-local-pg` (source + bin) JSON files to `{}`.
- **Next**: F5 `Visa2026 - PostgreSQL` — XAF creates schema + lookup seeds only. Do **not** run `Run-HeadlessChain.ps1`.

### 2026-08-13 — Education lookup gaps (11) then resume

- **Fail**: Education Posted 3200 / Failed **11** (`incomplete OData payload` institution/specialty). Chain halt.
- **Gaps**: 9 institutions (e.g. `Cumhuriyet uniwersiteti`, `IRT Traınıng (PTY) LTD (hünär okuwy)`, `Orta bilim, hünär şahadatnamaly`) + 9 specialties. Exact NameTm in `artifacts/headless-import/_seed-edu-gaps-20260813.sql`.
- **Seed**: INSERT PG `EducationInstitutions`/`Specialties` (IsDefault=FALSE); append calik-energi tenant JSON (no ConvertTo-Json); copy to embedded tenant json.
- **Resume**: `-StartAt Education` — **STEP_OK posted 11** (3200 already in id-map). EPH running. Log: `chain-console-resume-education-20260813-124619.log`.

### 2026-08-13 — Visa extract used Visa2026 table name against VISA2015

- **Fail**: Visa wave `Invalid object name 'dbo.ApplicationProfileInstance'` (SQL Server / VISA2015). Mechanical rename of Application → ApplicationProfileInstance hit **legacy extract SQL**.
- **Fix**: restore `dbo.Application` / `pia.Application` / `ar.Application` in 14 transform/index files. Keep result aliases `ApplicationProfileInstanceOid`.
- **Resume**: `-StartAt Visa` — **Visa STEP_OK Posted 6223**. Education running. Console: `artifacts/headless-import/chain-console-resume-visa-20260813-123447.log`.

### 2026-08-13 — Local PG wipe + Wave 2b reimport (ApplicationItem hard-remove)

- **Why**: After Phase B, local `visa2026` still had 12295 ApplicationProfileInstances / 38096 progress rows but only **11** ApplicationProfileInstancePeople (ApplicationItems CASCADE left no roster backfill).
- **Scripts**: dropped ApplicationItem from `Run-HeadlessChain.ps1` and `OnPrem-Sync.ps1`; retired `--correct-application-item-person-current` / `--correct-visa2014-issuing-application-item`; deleted `import/reimport/ApplicationItems.ps1`. Wipe SQL now truncates `ApplicationProfileInstances*` + People + ResolvedLinks (not the old Applications/ApplicationItems names). Chain `-SkipFileWaves` added (PS 5.1); `??` coalescing replaced so the chain parses on Windows PowerShell 5.1.
- **Wipe**: `Wipe-LocalPostgresTransactional.sql` — People/Apps/roster/progress/Visas → 0. Id-maps `calik-energi-local-pg` reset to `{}` (source + bin).
- **Fail 1**: `--inprocess` Person died immediately: `Cannot resolve scoped service INonSecuredObjectSpaceFactory from root provider.` Fix: `HeadlessMigrationHost` now `CreateScope()` like seed gates / batch workers.
- **Retry**: `Run-HeadlessChain.ps1 -StartAt Person -LegacySource calik-energi-local-pg -SkipFileWaves` → local PG. **Person STEP_OK Posted 3339 Failed 0** (legacy 3339, dedupe merged 21). File waves skipped; Passport+ running. Console: `artifacts/headless-import/chain-console-wipe-reimport-20260813-123100.log`. Watch: `Watch-OnPremImportLive.ps1 -Profile Local`.
- **Not in this run**: DocumentCopies file waves (resume later with `DocumentCopies.ps1`). On-prem Demo/Prod not wiped.

### 2026-08-12 — Import entity hard break: Application → ApplicationProfileInstance

- `--entity Application` rejected with clear error; use `--entity ApplicationProfileInstance` and id-map `ApplicationProfileInstance.json`.
- Related: ApplicationProfileInstanceProgress / ApplicationProfileInstancePerson entity names.
- Schema: Module cutover renames Applications* tables before import targets new OData entity.
- Orchestrators updated: OnPrem-Sync, Run-HeadlessChain, reimport/Applications.ps1, ApplicationPeople.ps1.

### 2026-08-12 — §13 Application → ApplicationProfileInstance cutover (parallel Wave 2b)

- **Hard break**: import `--entity ApplicationProfileInstance`; id-map `ApplicationProfileInstance.json`.
- **Schema**: new tables + same-Guid copy from `Applications*`, then drop old.
- **Related renames**: Progress/Person/ResolvedLink/ApprovalLegSnapshot; child FKs; Visa.Issuing*.
- **ApplicationItem**: not renamed — hard-remove path; FK to instance until dropped.
- **Cross-skill**: visa2026-application-profile §13 / R0–R6.

### 2026-08-12 — Wave 2b ApplicationPerson importer shipped

- **Entity**: `--import-visa2014 --entity ApplicationPerson --inprocess` (headless ObjectSpace only).
- **Source**: `dbo.PersonInApplication` → `ApplicationPerson` + immediate `ResolvedLinks` via `ApplicationPersonService.LinkPerson` / `RequirePerson*`.
- **Id-map**: `PersonInApplication.Oid` → `ApplicationPeople.ID` (`ApplicationPerson.json`).
- **Skip/dedupe**: same E:44/E:55 parent skips + Application+Person lowest-Oid as ApplicationItem.
- **Lock**: `ApplicationPersonRosterLockHelper` bypasses process-complete lock when `MigrationImportContext.IsDataImport`.
- **Chains**: `order.yaml`, `Run-HeadlessChain.ps1`, `OnPrem-Sync.ps1` insert ApplicationPerson before ApplicationItem; ApplicationItem marked dual-read bridge.
- **Scripts**: `import/ApplicationPeople.ps1`, `reimport/ApplicationPeople.ps1`, `cleanup/ImportedApplicationPeople.postgres.sql`.
- **Not done**: drop ApplicationItem wave; remap Visa.IssuingApplicationItem / InvitationItem / WorkPermitItem off ApplicationItem (option A).
- **Verify**: dry-run then `-MaxRows 50` against local PG with Application+Person id-maps present.

### 2026-08-12 — Locked: Application = profile instance; ApplicationPerson not ApplicationItem

- **Import entity**: still `--entity Application` (persistence = profile **instance**); templates from Wave 0b/1/3 seed.
- **People**: `ApplicationPerson` from legacy PersonInApplication — **no** `ApplicationItem` import.
- **Auto-link**: immediate resolve on ApplicationPerson create (`RequirePerson*` + valid rules); person scalars must precede Application wave.
- **Downstream**: WorkPermitItem / InvitationItem / Visa issuing attach to **Application + Person** only (no ApplicationItem bridge) — option **A**.
- **Doc**: [APPLICATION_PROFILE_CATALOG_WAVE0.md](../../../docs/VISA2014_MIGRATION/APPLICATION_PROFILE_CATALOG_WAVE0.md) Wave 2b; IMPORT_PLAN wave 3 row updated.
- **Next**: ApplicationPerson importer shipped; remap child FKs + remove ApplicationItem from chains still open.

### 2026-07-30 — File waves .15 → local PG (DocumentCopies.ps1)

- **Phase**: file-wave (`DocumentCopies.ps1`, `calik-energi-local-pg` → PostgreSQL `visa2026`)
- **Pilot** (MaxRows 10): all 7 steps Failed=0 — Person.Photo 10/10; Passport/Visa/Education/WP/Invitation docs; FamilyProof 9+1 no parent map
- **Person.Photo (full)**: Processed **3333** / Patched **3262** / No blob **71** / Failed **0** — STEP_OK
- **PassportDocument**: first full run OOM/crash ~3.2 GB WS after ~11 min; **PassportCopy id-map ~3663** entries persisted; resume with `-StartAt PassportDocument`
- **CLI**: `.\scripts\visa2014-migration\import\DocumentCopies.ps1 -LegacySource calik-energi-local-pg -TargetConnection 'Host=localhost;Port=5432;Database=visa2026;Username=postgres;Password=Visa2026Local;Persist Security Info=True;EFCoreProvider=Postgres' -StartAt Person-Photo`
- **Logs**: `artifacts/document-copies-import/*.log` (per-step `Person-Photo.log`, `PassportDocument.log`, …)
- **Prevent**: do not start second DocumentCopies while DataImporter locks DLLs; parent shell kill leaves child import running — check `Get-Process Visa2026.DataImporter` before resume
- **Resume order**: PassportDocument → VisaDocument → EducationDocument → WorkPermitDocument → InvitationDocument → FamilyProofDocument

### 2026-07-27 — Full scalar reimport .15 → local PG (scalars complete; posts running)

- **RunId**: `20260727-150349` (resume after Education seed)
- **Scalars**: all STEP_OK Failed=0 — Person 3333; Passport 3682; Visa 6132; Education 3202; EPH 3084; Salary 2977; AoR 5189; Application 12282; WP 407 / WPI 3875; Invitation 2868 / InvItem 5123; ApplicationItem 21752; Rejection 207 / RejectionItem 254; ApplicationProgress DB **38096** (legacy prepared ~38216)
- **Posts**: PersonSubcontractor + PersonRelationship OK; **post-PersonAddressPia** running; Visa FK fill still 0% until post-VisaIssuingApplicationItem / post-VisaInvitationItem
- **Watch**: Profile Local — Visa link fill expected to rise after remaining post waves

### 2026-07-27 — Full scalar reimport .15 → local PG (Education gap + resume)

- **First chain** RunId `20260727-145553`: Person/Passport/Visa OK; **Education** Posted **3198** / Failed **4** (exit 1).
- **Gaps** (exact NameTm from `.15`): institutions `Orta mekdep/Hünär şahadatnamaly`, `Jawaharlal Nehru adyndaky tehnologiki uniwersiteti`, `Netcare 911 gyssagly tiz kömek mekdebi`; specialty `Tiz kömek kömekçisi` (+ related `Mehaniki inženerçiligi` / `Orta bilim` already present).
- **Seed**: INSERT into PG `EducationInstitutions`/`Specialties` with `IsDefault=FALSE`, `GCRecord=0`; append calik-energi tenant JSON (LastIndexOf `]`); refresh DI bin overlays. SQL: `artifacts/local-pg-import/_seed-edu-gaps.sql`.
- **Resume** RunId `20260727-150349` PID 24368: Education Posted **4** / Failed **0** (already imported 3198); EPH/Salary/AoR STEP_OK; **Application** running.
- **Log**: `artifacts/local-pg-import/chain-console-from15-resume-education-20260727-150348.log`
- **Watch**: `.\scripts\visa2014-migration\Watch-OnPremImportLive.ps1 -Profile Local -ClearScreen`

### 2026-07-27 — Full scalar reimport .15 → local PG (started)

- **Phase**: full wipe + scalar chain (`Run-LocalPgScalarChain.ps1 -StartAt Person`)
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`) — legacy people≈3333 confirmed
- **Target**: PostgreSQL `visa2026` (localhost)
- **Prep**: stopped Blazor; `Wipe-LocalPostgresTransactional.sql` (People/Apps/Items/Progress → 0; kept EducationInstitutions 1520 / Specialties 1096); cleared source+bin `id-maps/calik-energi-local-pg` to `{}`; refreshed DI bin `LookupCatalogs/tenant` education-institution + specialty JSON from Module tenant catalogs
- **PID**: 31524
- **Log**: `artifacts/local-pg-import/chain-console-from15-wipe-reimport-20260727-145553.log`
- **Watch**: `.\scripts\visa2014-migration\Watch-OnPremImportLive.ps1 -Profile Local -ClearScreen`

### 2026-07-27 — Merge KYC Kobmine → Kombine (local PG)

- **Decision**: keeper = correct spelling **Kombine** (user chose option 1).
- **Apply** (`visa2026`): repointed People 515 + Applications 72; soft-deleted Kobmine row; active visas now **one** bar `KYC (…Kombine…)` = **392**.
- **SQL**: `scripts/visa2014-migration/cleanup/MergeProjectContract-KycKobmineToKombine.postgres.sql`
- **Catalog**: removed Kobmine from `project-contract.calik-energi.json` / `project-contract.json` (regex edit — do not ConvertTo-Json round-trip Turkmen).
- **Alias**: `lookup-translations.calik-energi.yaml` values[] Kobmine → Kombine; generate script normalizes Kobmine→Kombine + skip dup code.
- **Still open**: CS-1 Şatlık/Shatlık, KYM space, Subcontractor Çalyk/Çalık.
### 2026-07-27 — Local PG duplicate preview (ProjectContract / Subcontractor)

- **Evidence**: Report Dashboard Active By Project + Subcontractor bars on localhost:5001 / `visa2026`.
- **Finding**: Not exact NameTm clones — **legacy spelling variants**:
  - Project: KYC Kobmine vs Kombine; CS-1 Şatlık vs Shatlık; KYM vs KYM(;
  - Subcontractor: **Çalyk** (y, default, 2083) vs **Çalık** (ı, 365) vs Çalik (i, 5).
- **Artifacts**: `Preview-DuplicateProjectContractSubcontractor.ps1 -Profile Local`; `cleanup/DuplicateProjectContractAndSubcontractor.postgres.sql`; `cleanup/DuplicateProjectContractSubcontractor.local-pg-preview.md`.
- **Next**: user approves keeper per group, then implement Postgres merge -Apply (repoint FKs).
### 2026-07-27 — Preview duplicate ProjectContract / Subcontractor lookups

- **Ask**: Preview duplicate ProjectContract + Subcontractor before merge (post-import from `.15`).
- **Seed check**: `project-contract.calik-energi.json` (74) and `subcontractor.calik-energi.json` (131) — **0** exact NameTm/Code/LocalizationKey duplicate groups in repo JSON.
- **Tooling**: preview-only `scripts/visa2014-migration/Preview-DuplicateProjectContractSubcontractor.ps1` + `cleanup/DuplicateProjectContractAndSubcontractor.sql` (SameNameTm / SameLocalizationKey / SameCode / PrefixCandidate + Subcontractor NormalizedNameTm; Person/Application ref counts). No `-Apply` yet.
- **Note**: DB-side duplicates usually come from older seed + ForceUpdate cycles (long NameTm vs short Code title), not from the current calik JSON. PrefixCandidate needs human review (do not auto-merge Satlik/Shatlik variants).
- **Next**: run preview against Demo/Prod with SQL connection env; then decide merge groups.

### 2026-07-23 — ApplicationProgress PG reimport failed (DateTime Kind=UTC) then fixed

- **Symptom**: Watch showed posted=0 fail=1100+; `Cannot write DateTime with Kind=UTC to PostgreSQL type 'timestamp without time zone'`.
- **Cause**: Headless host uses `CreateHostBuilder` (skips `Program.Main` switch); payload used `SpecifyKind(..., Utc)`.
- **Fix**: `AppContext.SetSwitch(Npgsql.EnableLegacyTimestampBehavior)` in `HeadlessMigrationHost.Start`; ApplicationProgress payload Date → `DateTimeKind.Unspecified`.
- **Resume**: cleanup + reimport log `…processdate-issued-20260723-152159.log` — early progress posted>0 failed=0; prepared **38164** (ProcessDate⇒issued for direct migration).

### 2026-07-23 — ApplicationProgress: direct-migration ProcessDate (sene) ⇒ PROCESS_ISSUED

- **Correction**: source of truth is **Işlenmäge başlanan sene** (Application.ProcessDate), not belgi/ProcessNumber.
- **Rule**: direct migration (ministryLegCount=0, Registration + other DirectMigration): ProcessDate set ⇒ PROCESS_STARTED + PROCESS_ISSUED. ProcessNumber optional on both steps. Bel alone does not issue.
- **Reimport**: Posted/Failed from log above; state histogram from PG.
- **Log**: `reimport-ApplicationProgress-localpg-processdate-issued-20260723-150530.log`

### 2026-07-23 — ApplicationProgress.ProcessNumber field (not Description)

- **Decision**: Legacy `Application.ProcessNumber` maps to `ApplicationProgress.ProcessNumber` on `PROCESS_STARTED` (and direct-migration `PROCESS_ISSUED`); Description no longer carries the process number.
- **Target caption**: `Application.DisplayCaption` / denormalized `Application.ProcessNumber` (visa2026-application-progress).
- **Verify**: transform unit tests updated; Module helper tests pass.
- **Phase**: mapping/code (reimport optional for already-loaded Description-only rows — schema updater backfills PROCESS_STARTED Description → ProcessNumber).

### 2026-07-23 — ApplicationProgress: direct-migration ProcessNumber ⇒ PROCESS_ISSUED

- **Rule**: For apps that go straight to migration service (`ministryLegCount=0`, includes Registration + other DirectMigration): non-empty legacy `Application.ProcessNumber` (**Işlenmäge başlanan belgi**) synthesizes **PROCESS_STARTED** and **PROCESS_ISSUED**. ProcessDate alone does not issue. Ministry-routed apps unchanged (Inv/WP/extension completion only).
- **Code**: `Visa2014ApplicationProgressTransform.SynthesizeSteps`; tests updated; discovery + field-map notes.
- **Local PG reimport**: cleanup DELETE 31037; Posted **31128** / Failed **0** / Skipped no-App-map **95**; Prepared **31223**. `PROCESS_ISSUED` = **4599** (was ~4500).
- **Log**: `reimport-ApplicationProgress-localpg-direct-issued-20260723-141320.log`
- **Open**: confirm with user whether ProcessDate-only (sene, no belgi) should also issue when legacy status is Işlenen.


### 2026-07-23 — Full scalar reimport .15 → local PG (Completed)

- **Outcome**: Overall **Completed** / CHAIN_COMPLETE — RunId `20260723-120409` (resume after Education seed; Person–Visa from wipe run `20260723-115634`)
- **Source**: `10.100.128.15` / `VISA2015` → PostgreSQL `visa2026`
- **Waves (Failed=0)**: Person 3319; Passport 3667; Visa 6119; Education catch-up 4 (DB 3191); EPH 3070; Salary 2963; AoR 5176; Application **12261**; WP 406 / WPI 3860; Invitation 2864 / InvItem 5115; ApplicationItem **21707**; Rejection 207 / RejectionItem 254; ApplicationProgress (legacy ~31099; DB **31037**)
- **Post-corrections**: PersonSubcontractor, PersonRelationship, PersonAddressPia, ApplicationItemPersonCurrent, VisaIssuingApplicationItem — ran after scalars
- **Log**: `artifacts/local-pg-import/chain-console-from15-resume-education-20260723-120408.log`


### 2026-07-23 — Full scalar reimport .15 → local PG (Education gap + resume)

- **First chain** RunId `20260723-115634`: Person/Passport/Visa OK; **Education** Posted **3187** / Failed **4** (exit 1) — Institution `Stambul Arel uniwersiteti` missing (catalog had `… Uniwersitety` only).
- **Seed**: INSERT Institution + related Specialties from live `.15` (`TitleOfIEducationInstitution` / `TitleOfSpeciality` via `Spcialty` FK); append calik-energi JSON; refresh DI bin overlay.
- **Hung helper**: large-JSON regex append killed (exit -1); use LastIndexOf(`]`) append + labels from SqlClient, not Tee-Object log text (Turkmen mojibake).
- **Resume** RunId `20260723-120409`: Education catch-up Posted **4** / Failed **0**; EPH/Salary/AoR OK; **Application** running (~12k). Watch: `Watch-OnPremImportLive.ps1 -Profile Local`.


### 2026-07-23 — Full scalar reimport .15 → local PG (started)

- **Phase**: full wipe + scalar chain (`Run-LocalPgScalarChain.ps1 -StartAt Person`)
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Target**: PostgreSQL `visa2026` (localhost)
- **Prep**: `Wipe-LocalPostgresTransactional.sql` (People/apps/progress → 0; kept EducationInstitutions 1503 / Specialties 1088 incl. slash-compound Balıkesir + Gurluşyk rows); cleared bin id-maps; refreshed DataImporter bin overlay education-institution.json / specialty.json from calik-energi JSON
- **RunId**: 20260723-115634
- **Log**: `artifacts/local-pg-import/chain-console-from15-wipe-reimport-20260723-115633.log`
- **Watch**: `.\scripts\visa2014-migration\Watch-OnPremImportLive.ps1 -Profile Local -ClearScreen`
# Learnings (append-only): visa2014-to-visa2026-import

**Purpose:** Record verified discovery, strategy decisions, mapping corrections, and OData import outcomes so **each session builds on the last**.

**Loop:** [MATURITY.md](./MATURITY.md) — **read `## Entries` before every task**; **append after every import attempt** (success, failure, or partial) and after verified discovery/strategy work.




### 2026-07-22 — ActualPosition dirty titles (numeric / no-letters → "-")

- **Phase**: data-quality / mapping fix (pre-reimport + post-import cleanup)
- **Problem**: ActualPosition catalog bloated (~1410–1433 rows); many Titles are Position.Code-style numbers (617-, 1902 -, 1 216, .) — not real positions. UI shows Lookup/Organization/Config → Actual Position.
- **Rule**: if value has **no alphabetic letter**, treat as unset → canonical Name `-` (required placeholder). Real titles (letters) kept. Long task descriptions still need human CSV review (Guess=review).
- **Code**: Visa2014ActualPositionNormalizer wired into EmployeePositionHistory transform + OData importer + MiddleName cleanup + review Guess/--auto-no-letters.
- **Existing DB (from checked-in CSV)**: **193** no-letter Titles, **340** EmployeePositionHistory usages → dash via:
  `dotnet run --project Visa2026.DataImporter -- --apply-visa2014-actual-positions --auto-no-letters [--dry-run] [--api-base-url …]`
  Prefer dry-run first. Requires Blazor/OData up (cleanup is OData-only).
- **Reimport**: normalizer prevents recreating numeric ActualPositions on next EmployeePositionHistory wave.
- **Tests**: Visa2014ActualPositionNormalizerTests (build blocked this session by running `--import-visa2014 --entity ApplicationProgress` PID locking DLLs — re-run after that finishes).

### 2026-07-22 — Full scalar reimport .15 → local PG (finished)

- **Phase**: full wipe + scalar chain result
- **Overall**: Failed
- **RunId**: 20260722-111047
- **Log**: `artifacts/local-pg-import/chain-console-from15-wipe-reimport-20260722-111046.log`
- **Steps**:
```
STEP_OK Person
STEP_OK Passport
STEP_OK Visa
STEP_FAILED Education exit=1
```

### 2026-07-22 — Full scalar reimport .15 → local PG (started)

- **Phase**: full wipe + scalar chain (`Run-LocalPgScalarChain.ps1`)
- **Source**: `10.100.128.15` / `VISA2015` (calik-energi-local-pg)
- **Target**: PostgreSQL `visa2026` (localhost)
- **Prep**: `Wipe-LocalPostgresTransactional.sql`; cleared `id-maps/calik-energi-local-pg/*.json`
- **Chain update**: added `Rejection` / `RejectionItem` after `ApplicationItem`
- **Bugfix**: Person greenfield failed `Id-map not found` after wipe — `LoadOrEmpty` on Person id-map + expander no-ops if missing
- **First attempt**: Person STEP_FAILED (missing id-map); **resume**: Person STEP_OK; Passport running
- **Log**: `artifacts/local-pg-import/chain-console-from15-wipe-reimport-20260722-111046.log`
- **Watch**: `.\scripts\visa2014-migration\Watch-OnPremImportLive.ps1 -Profile Local -ClearScreen`

### 2026-07-22 — Rejection + RejectionItem importConfirmed and local PG import

- **Phase**: Phase 1b confirm + Phase 3/4 import (calik-energi-local-pg → PostgreSQL `visa2026`)
- **Confirm**: human `importConfirmed` in chat; updated `order.yaml`, discovery dossiers, entity-inventory
- **Legacy**: localhost\SQLEXPRESS VISA2015 (Integrated Security); Application id-map 12247
- **Dry-run Rejection**: prepared **207** / missing App map **0**
- **Import Rejection**: Posted **207** / Failed **0** / id-map `id-maps/calik-energi-local-pg/Rejection.json`
- **Import RejectionItem**: Posted **254** / Failed **0** / missing id-map **0** / id-map `RejectionItem.json`
- **Logs**: `import-Rejection-localpg-20260722-110447.log`, `import-RejectionItem-localpg-20260722-110510.log`
- **Note**: duplicate Numbers `178` / `AS-600159` imported with oid-tail suffixes (4 distinct headers)

### 2026-07-22 — Rejection Excel preview + importer wired

- **Phase**: Phase 1c preview + Phase 2 implementation (import gated on importConfirmed)
- **CLI**: `--export-visa2014-preview --entity Rejection|RejectionItem`; `--import-visa2014 --entity Rejection|RejectionItem` (wired; do not POST until importConfirmed)
- **Preview (local SQLEXPRESS VISA2015, Integrated Security)**: Rejection **207** import / **0** skipped; RejectionItem **254** / **0** skipped
- **Files**: `legacy/visa2014/preview-export/Rejection-preview.calik-energi.xlsx`, `RejectionItem-preview.calik-energi.xlsx` (also under bin Debug output)
- **Code**: `Visa2014RejectionTransform` / `RejectionItemTransform` / preview exporters / OData importers; Application FK required (skip if not in Application id-map)
- **Next**: human review preview → set `importConfirmed` on Rejection + RejectionItem → dry-run then in-process import after ApplicationItem

### 2026-07-22 — Rejection discovery + ApplicationResult Result split

- **Phase**: discovery (Rejection / RejectionItem) + Invitation mapping correction
- **Legacy**: `ApplicationResultEnum` — `0` Invitation, `1` Rejection (not cancel). Same `dbo.ApplicationResult` + `PersonInInvitation` (FK still named Invitation).
- **Çalik VISA2015 counts**: Result=0 **2821** headers / **2776** referenced / **4984** items; Result=1 **207** headers (**185** with items, **22** orphans) / **254** items. Duplicate Rejection Numbers: **2** (`178`, `AS-600159`).
- **Artifacts**: `discovery/Rejection.yaml`, `discovery/RejectionItem.yaml`, `field-maps/Rejection.yaml`, `field-maps/RejectionItem.yaml`, `table-mappings` `rejection-header`/`rejection-item`, `order.yaml` after ApplicationItem.
- **Locked map**: Number→RejectedDocNumber; IssuedDate→Date; Application required; Reason allow_null; id-map ApplicationResult.Oid→Rejection.ID (separate from Invitation).
- **Invitation fixes**: extract `Result = 0`; InvitationItem INNER JOIN Result=0; drop `Result==1→IsCancelled`; cancel index = PIA.Cancelled only; progress completion + ApplicationItem CurrentInvitationItem also Result=0.
- **Gates**: Excel preview + `importConfirmed` before Rejection OData importer.
- **Next**: `--export-visa2014-preview --entity Rejection` (after transform shell) → human confirm → implement importer mirroring Invitation.


### 2026-07-22 — ApplicationProgress lineage sections were empty (fixed)

- **Phase**: mapping-verify bugfix
- **Symptom**: PASS report with empty **Property lineage** table + **No sample rows** (HTML had section shells only)
- **Cause**: `ProgressVerifyReport` never assigned `PropertyLineage` / `SampleLineage`; transform did not emit `_lineage_*` / `_stepCode` on import rows
- **Fix**: `BuildStaticPropertyLineage()` + sample build in `RunVerify`; transform wires `DateSource`/`DescriptionSource` per `SynthesisStep`
- **Smoke**: exit **0**; `PropertyLineage` **7**; `SampleLineage` **20**; report `mapping-verify-ApplicationProgress-calik-energi-local-pg-20260722-051749.html`

### 2026-07-22 — ApplicationProgress verify: property lineage in report

- **Phase**: mapping-verify UX
- **Change**: transform attaches `_lineage_Date` / `_lineage_Description` / … per synthetic step; verify HTML/JSON **Property lineage** + **Sample row lineage**
- **Smoke**: exit **0**; report `mapping-verify-ApplicationProgress-calik-energi-local-pg-20260722-051330.html`
### 2026-07-22 — ApplicationProgress reimport + mapping verify exit 0 (local PG)

- **Phase**: partial-reimport + mapping-verify
- **Prep**: `ImportedApplicationProgress.postgres.sql` (NULL LatestProgressId, DELETE 30703); clear ApplicationProgress id-map
- **Import**: Posted **30704** / Failed **0** / Skipped no-App-map **66** / parent-skipped **167**; prepared **30770**; ~14 min
- **Log**: `reimport-ApplicationProgress-localpg-20260722-094015.log`
- **Verify fix**: resolve ministry-leg counts from target (same as import); treat no-Application-map as skip not missingIdMap
- **Verify**: State histogram OK; mapped **30704**; missingIdMap **0**; parity sample 50 mismatches **0**; **exit 0**
- **Report**: `mapping-verify-ApplicationProgress-calik-energi-local-pg-20260722-050004.json`
### 2026-07-22 — Mapping verify ApplicationProgress shipped

- **Phase**: mapping-verify
- **CLI**: `--verify-visa2014-mapping --entity ApplicationProgress` (Tier A State histogram + Tier B sample; string id-map `{appOid}:{stepCode}`)
- **Fields**: State (error), Application FK (error), Description (error if expected set), Date/Order (**warn**)
- **Code**: `Visa2014ApplicationProgressMappingVerify.cs`; OnPrem-Sync runs verify after ApplicationProgress wave
- **Smoke (local PG)**: State histogram **OK**; mapped **27966** / missingIdMap **597** (transform synthesizes steps not in current id-map) → exit **1**; sample mismatches were Order-only (warn)
- **Follow-up**: reimport ApplicationProgress (or refresh id-map) when missingIdMap > 0 after transform changes; then re-verify for exit 0
### 2026-07-22 — App_Visa_Ext VisaType inference → WP

- **Phase**: mapping correction
- **Change**: `App_Visa_Ext` VisaType LocalizationKey `EX` → `WP` (`Visa2014ApplicationVisaTypeInference` + UI default VisaType only)
- **Unchanged**: `App_Exit_Visa` remains `EX`; App_Visa_Ext does not inherit Month6/Multiple defaults from WP invitation types
- **Follow-up**: re-run Application VisaType correction if imported rows still have EX
### 2026-07-22 — Silent reconciliation tracking (Application verify)

- **Phase**: mapping-verify enhancement
- **Code**: `Visa2014LookupOutcomeClassifier` + silent inventory in `Visa2014MappingVerifyCommand` (JSON/HTML **Silent / implicit outcomes**)
- **Gate**: fail only on `actual_without_expected` (non-default actual when transform expected null); `default_applied` / `actual_default_tolerated` are info
- **CLI**: `--verify-visa2014-mapping --entity Application --legacy-source calik-energi-local-pg --tier B --sample 50`
- **Target**: local PostgreSQL `visa2026`
- **Counts**: mapped **12247**; missingIdMap **1** (exit 1 from id-map gap, not silent)
- **Histograms**: 4/4 OK; parity sample 50 mismatches **0**
- **Silent**: unexpected **0**; default_applied **5**; tolerated_defaults **15866** (Month6/Multiple/NORM when expected null)
- **Doc**: [MAPPING_VERIFICATION.md](../../../docs/VISA2014_MIGRATION/MAPPING_VERIFICATION.md) silent buckets + gate policy
- **Report**: `legacy/visa2014/import-logs/mapping-verify-Application-calik-energi-local-pg-20260722-034013.json`
### 2026-07-21 — ApplicationProgress reimport (local PG, after agent interrupt)

- **Phase**: partial-reimport (local PostgreSQL `visa2026`, `calik-energi-local-pg`)
- **Prep**: `ImportedApplicationProgress.postgres.sql` (NULL `LatestProgressId`, DELETE manual-entry progress); clear `ApplicationProgress.json` id-map (source + bin).
- **Run**: detached `dotnet run` background (~45 min) — **do not** use `Start-Process dotnet` with split args (startup seed failed); **do not** use multi-line `.ps1` without backticks after `--`.
- **Result**: Posted **30703** / Failed **0** / Skipped **62** (30765 rows prepared). DB `"ApplicationProgresses"` = **30703**. Exit **0**.
- **Log**: `reimport-ApplicationProgress-localpg-20260721-171015.log`
- **Spot-check**: `7/-1229` (2026-07-07) → `App_Inv`, visa period `30 (otuz) gün`, **5** progress steps.

### 2026-07-21 — Mapping verify Application (full local PG, tier B)

- **Phase**: mapping-verify
- **CLI**: `--verify-visa2014-mapping --entity Application --legacy-source calik-energi-local-pg --tier B --sample 50`
- **Target**: local PostgreSQL `visa2026`
- **Counts**: importable/mapped **12247**; missingIdMap **0**
- **Histograms**: ApplicationType / Urgency / VisaPeriod / VisaCategory — **4/4 OK**
- **Parity**: sampled **50**; mismatches **0**; missingTarget **0**
- **Exit**: **0**
- **Report**: `legacy/visa2014/import-logs/mapping-verify-Application-calik-energi-local-pg-20260721-114939.json`

### 2026-07-21 — Shipped: --verify-visa2014-mapping (Application pilot)

- **Phase**: implementation + local PG smoke
- **CLI**: `--verify-visa2014-mapping --entity Application` (Tier A histograms + Tier B stratified sample; `--full` / `--tier C` for full parity)
- **Expected**: `Visa2014ApplicationTransform.PrepareImportBatch` (same as import)
- **Actual**: headless ObjectSpace load via id-map
- **Fields**: ApplicationType, Urgency, VisaPeriod, VisaCategory, FullApplicationNumber, ApplicationDate, ProjectContract (parity only)
- **Smoke**: local PG `visa2026`, `--max-rows 200 --sample 20` → histograms 4/4 OK, mismatches 0, exit 0
- **Lessons**: histogram keys must use `CatalogMatchHelper.NormalizeKey`; optional VisaPeriod/VisaCategory rows with null expected must be skipped in Tier A (BO defaults Month6/Multiple inflate actuals)
- **Orchestrator**: `OnPrem-Sync.ps1` verifies Application after successful wave; `-SkipMappingVerify` to bypass
- **Wrapper**: `scripts/visa2014-migration/import/Verify-Mapping.ps1`
- **Doc**: [MAPPING_VERIFICATION.md](../../../docs/VISA2014_MIGRATION/MAPPING_VERIFICATION.md)

### 2026-07-21 — Design: post-import mapping verification (property + lookup)

- **Phase**: strategy / design (no CLI yet)
- **Decision**: After each scalar import/reimport wave, automate **expected vs actual** using the **same** transform pipeline as import (`field-maps` + `lookup-translations`), gated like FailedCount before the next `order.yaml` entity.
- **Tiers**: A = lookup histograms; B = sampled field parity (default); C = full reconcile for critical BOs / after mapping changes.
- **Pilot target**: Application (`ApplicationType` composite + Urgency/VisaPeriod/VisaCategory + key scalars).
- **Artifact**: [MAPPING_VERIFICATION.md](../../../docs/VISA2014_MIGRATION/MAPPING_VERIFICATION.md) — planned `--verify-visa2014-mapping`.
- **Wiring**: skill scripts table + chain checklist; import-practices / VISA2014_MIGRATION reconciliation; scripts README; field-maps `_template.yaml` `verify:` stub.
- **Next**: implement DataImporter command; opt-in `verify:` on Application.yaml; run after next Application reimport.

### 2026-07-21 — Application partial reimport (local PG, Day30 VisaPeriod mapping)

- **Phase**: partial-reimport (local PostgreSQL `visa2026`, `calik-energi-local-pg`)
- **Prep**: `ImportedApplications.postgres.sql` (12241 deleted); cleared Application + downstream id-maps (source + bin); dropped stale `Applications.LatestIsCancelled` / `LatestIsRejected` (NOT NULL blocked first attempt).
- **Result**: Posted **12247** / Failed **0** / Skipped **169** (E:33/E:55 etc.). DB `Applications` manual-entry = **12247**. Exit 0 (~1.8 min).
- **Day30**: composite `30 (otuz) gün::30` → **43** apps (spot-check `7/-1229` = `day30` / `30 (otuz) gün`, `App_Inv`).
- **Log**: `legacy/visa2014/import-logs/reimport-Application-localpg-20260721-161027.log`
- **Next**: downstream chain required (cleanup wiped items/progress): WorkPermit → Invitation → ApplicationItem → ApplicationProgress.

### 2026-07-21 — ApplicationProgress partial reimport (raw ProcessNumber / InvitationNumber descriptions)

- **Phase**: partial-reimport (local PG, after `FormatLegacyDescriptionValue` — no `ProcessNumber:` / `InvitationNumber:` labels)
- **Prep**: PG cleanup (`cleanup-applicationprogress.sql`); clear `ApplicationProgress.json` id-map; DataImporter Debug build.
- **Result**: Posted **~30680** / Failed **0** / Skipped **18** (terminal progress log truncated at 8500/30764; exit 0 ~13 min). DB = **30696** `ApplicationProgresses`.
- **Shape**: `ProcessNumber:` prefix = **0**; `InvitationNumber:` prefix = **0**; `2_REVIEW_APPROVED` + `MinisteriesDocumentNumber:` = **3274**; `PROCESS_ISSUED` = **4340**.
- **Spot-check**: `7/-1206` → `PROCESS_STARTED` Description `CO0223973`, `PROCESS_ISSUED` `CO323977`.
- **Code**: `Visa2014ApplicationProgressTransform.FormatLegacyDescriptionValue` for started/issued; ministry refs still use `FormatLegacyRef` labels.

### 2026-07-21 — ApplicationProgress partial reimport (MinisteriesDocumentNumber → leg 2)

- **Phase**: partial-reimport (local PG, after `BuildLegApprovedDescription` shift)
- **Prep**: NULL `LatestProgressId`; DELETE manual-entry `ApplicationProgresses`; clear `ApplicationProgress.json` (+ sync-progress) source id-map; rebuild DataImporter if `runtimeconfig.json` missing after killing `dotnet`/`Visa2026.DataImporter`.
- **Result**: Posted **30692** / Failed **0** / Skipped (no Application map) **66** / Parent-skipped **167**. DB = **30692** rows. Exit 0 (~25 min).
- **Shape**: `1_REVIEW_APPROVED` with Description = **0**; `2_REVIEW_APPROVED` with `MinisteriesDocumentNumber:` = **3273**; `PROCESS_ISSUED` = **4339**.
- **Log**: `reimport-ApplicationProgress-localpg-20260721-110757.log`
- **PG cleanup SQL**: `artifacts/local-pg-import/cleanup-applicationprogress.sql` (use `C:\PostgreSQL\16\bin\psql.exe` — not in PATH). **Do not** run cleanup while import is in progress.

### 2026-07-21 — ApplicationProgress: MinisteriesDocumentNumber on leg 2 only

- **Mapping**: `MinisteriesDocumentNumber` → `2_REVIEW_APPROVED` Description (Energetika); leg 1 approved has no doc (ministry from `ApprovalLegProfile`). `DocNumberForwardedToMinConstruction` → leg 3 when present.
- **Code**: `Visa2014ApplicationProgressTransform.BuildLegApprovedDescription`; tests `SynthesizeSteps_LongProcess_MinisteriesDocumentNumberOnLeg2NotLeg1`, `SynthesizeSteps_ThreeLegs_ConstructionDocOnLeg3`.


### 2026-07-21 — ApplicationProgress: ProcessDate/ProcessNumber = processing start (not issued)
### 2026-07-21 — ApplicationProgress: PROCESS_ISSUED from Invitation / WorkPermit evidence
### 2026-07-21 — ApplicationProgress partial reimport (invitation/WP completion)

- **Phase**: partial-reimport (local PG, after completion-index)
- **Result**: Posted **30692** / Failed **0** / Skipped (no Application map) **62** / Parent-skipped **167**. Completion index: **4513** apps. DB = 30692 rows (`PROCESS_ISSUED` = 4339). Exit 0 (~19 min).
- **Log**: `reimport-ApplicationProgress-localpg-20260721-092326.log`


- **Phase**: mapping / transform
- **Rule**: Legacy app is complete when it has issued **ApplicationResult** (Invitation + PersonInInvitation) and/or **PersonInApplication.WorkPermit** → synthesize `PROCESS_ISSUED` with `InvitationNumber` / `WorkPermitNumber` + issued date. `ProcessDate`/`ProcessNumber` remain **PROCESS_STARTED** only.
- **Code**: `Visa2014ApplicationProgressCompletionIndex` + `SynthesizeSteps` completion branch; auto-loaded in `PrepareImportBatch`.
- **Reimport**: partial ApplicationProgress reimport after deploy to add issued rows.

- **Phase**: partial-reimport (local PG, after ProcessDate/ProcessNumber → PROCESS_STARTED fix)
- **Result**: Posted **26348** / Failed **0** / Skipped (no Application map) **60** / Parent-skipped **172**. DB = 26348 rows. Exit 0 (~15 min).
- **Shape**: **0** `PROCESS_ISSUED` rows (was ~12063 before); `PROCESS_STARTED` = 12110 with ProcessNumber on Description. Total rows ~26k vs ~38k prior reimport.
- **Log**: `reimport-ApplicationProgress-localpg-20260721-084658.log`


- **Phase**: mapping / transform correction
- **Decision**: Legacy `Application.ProcessDate` + `ProcessNumber` mark **migration-service processing start** (`PROCESS_STARTED`), not completion. `ProcessNumber` goes on the started step Description. Do **not** synthesize `PROCESS_ISSUED` from these columns.
- **Follow-up**: `PROCESS_ISSUED` will use a separate legacy completion source (TBD).
- **Code**: `Visa2014ApplicationProgressTransform.SynthesizeSteps` — removed `migration_issued` branch; tests updated (+ app `12/-7010` / AS538188 case).
- **Reimport**: partial ApplicationProgress reimport required after deploy to drop bogus issued rows from prior transform.


### 2026-07-21 — ApplicationProgress partial reimport (ProcessDate = start only)

- **Phase**: partial-reimport (local PG, after ProcessDate/ProcessNumber → PROCESS_STARTED fix)
- **Result**: Posted **26348** / Failed **0** / Skipped (no Application map) **60** / Parent-skipped **172**. DB = 26348 rows. Exit 0 (~15 min).
- **Shape**: **0** `PROCESS_ISSUED` rows (was ~12063 before); `PROCESS_STARTED` = 12110 with ProcessNumber on Description. Total rows ~26k vs ~38k prior reimport.
- **Log**: `reimport-ApplicationProgress-localpg-20260721-084658.log`
### 2026-07-20 — ApplicationProgress partial reimport (local PG, state-only model)

- **Phase**: partial-reimport
- **Mode**: single-entity `--entity ApplicationProgress` / `--legacy-source calik-energi-local-pg` / in-process Postgres target
- **Prep**: NULLed `Applications.LatestProgressId`; DELETE 45905 progress rows for `IsManualEntry` apps; cleared `ApplicationProgress.json` (+ sync-progress) under **source and bin** id-maps; copied `Application.json` from bin → source (source tree had stubs only).
- **Result**: Posted **38411** / Failed **0** / Skipped (no Application map) **120** / Parent-skipped **172** / Seeds removed **0**. DB `"ApplicationProgresses"` = 38411. Exit 0 (~24 min).
- **Log**: `legacy/visa2014/import-logs/reimport-ApplicationProgress-localpg-20260720-171707.log`
- **Shape**: transform no longer posts `IS_BEING_PREPARED`; first ministry step = `1_REVIEW_STARTED` (`leg_1_started`). Stock `reimport/ApplicationProgress.ps1` is LocalDB/`calik-energi` — use PG cleanup + `--legacy-source calik-energi-local-pg` for this PC.
- **Note**: `LocationID` column may still exist until Module schema updater runs (F5 / DB update); new rows do not depend on Location.


### 2026-07-20 — Local PG wipe + scalar reimport from .15 (REVIEW_STARTED cleanup)

- **Phase**: end-to-end (local PostgreSQL `visa2026`, source `10.100.128.15` / `VISA2015`, legacy-source `calik-energi-local-pg`)
- **Goal**: Full scalar reimport so `ApplicationProgress` regenerates without `_REVIEW_STARTED` / «N-NJI IŞ YLALAŞYKDA» rows (transform already updated).
- **Prep**: Stopped F5 Blazor; `Wipe-LocalPostgresTransactional.sql` (People/Apps → 0); **must clear id-maps under both** `Visa2026.DataImporter\legacy\...\id-maps\calik-energi-local-pg` **and** `bin\Debug\net8.0\legacy\...\id-maps\calik-energi-local-pg`.
- **Attempt 1 (failed)**: Cleared only source-tree id-maps → Passport Posted 2 / Skipped already-imported 3661; Visa Failed 4 (stale Passport GUIDs) + Skipped already-imported 6089. **Prevent**: always wipe **bin** id-maps on local PG reimport.
- **Attempt 2**: Cleared bin+source maps, rewipe, `Run-LocalPgScalarChain.ps1 -StartAt Person -SkipTenantCatalogGeneration` (tenant catalogs already regenerated from .15). Person 3316 / Passport 3663 / Visa 6093 Failed 0.
- **Education gap**: 1 fail — Specialty `Zähmeti goramak we howpsuzlyk` missing on target (exists on .15; catalog had only `… we tehniki howpsuzlyk` variants). Inserted Specialty row (`GCRecord=0`); appended to `specialty.calik-energi.json`. Resume `-StartAt Education` → Education catch-up Posted 1 Failed 0; EPH 3068; Salary 2961; AoR 5169.
- **In progress**: RunId `20260720-152656` — Application wave Running → WorkPermit…ApplicationProgress. Watch: `.\scripts\visa2014-migration\Watch-OnPremImportLive.ps1 -Profile Local -ClearScreen`. Logs: `artifacts/local-pg-import/chain-console-from15-resume-education2-20260720.log`.
- **Artifacts**: wipe SQL; LocalPgScalarChain; specialty JSON append.

### 2026-07-17 — Visa.VisaType collapsed to WP (LocalizationKey missing on in-process DTOs)

- **Phase**: import / correction
- **Symptom**: In-process Visa import set every `Visa.VisaType` to default WP because `MapLookupDto` omitted `LocalizationKey`.
- **Fix**: copy `LocalizationKey` in `MapLookupDto`; stop silent default fallback in `ResolveVisaType` / related resolvers; `EnsureVisaTypeLookupKeysLoaded` + prepared-row histogram guard; CLI `--correct-visa-type` backfills from legacy `TypeOfVisaL:mgCode`.
- **Artifacts**: `Visa2014VisaTypeCorrection.cs`, `Visa2014VisaODataImporter.cs`, `Visa2014ODataLookupResolver*.cs`, `Program.cs`.


### 2026-07-17 — Application.VisaType inferred from ApplicationType (no legacy FK)

- **Phase**: mapping
- **Dossier**: docs/VISA2014_MIGRATION/discovery/Application.yaml / field-maps/Application.yaml
- **Symptom**: Legacy `dbo.Application` has VisaPeriod/VisaCategory but **no VisaType**; local PG import had every Application.VisaType = WP (catalog IsDefault).
- **Inference map** (`Visa2014ApplicationVisaTypeInference`, LocalizationKey):
  - `App_Inv_And_WP` / `App_Visa_and_WP_Ext` / `App_Inv_According_to_WP` / `App_Visa_Ext_According_to_WP` → **WP** (`WP-Işçi Wiza`)
  - `App_Inv` → **BS1** (`BS1-İşerwürlik`)
  - `App_Inv_FM` / `App_Visa_Ext_FM` / `App_Visa_For_New_Born_FM` → **FM** (`FM-Maşgala`)
  - `App_Visa_Ext` / `App_Exit_Visa` → **EX** (`EX-Çykyş`) — user confirmed App_Visa_Ext = EX
  - `App_Sevice_Passport` → **OF** (`OF-Gulluk`)
- **Artifacts**: transform sets `VisaType`; OData importer posts FK; `Application.TryGetDefaultVisaLookupKeys` aligned; CLI `--correct-application-visa-type` for existing DBs; unit tests 15 passed.
- **Follow-up**: run correction on local PG after stopping F5 lock.

**Canonical plan:** [docs/VISA2014_MIGRATION.md](../../../docs/VISA2014_MIGRATION.md) · [IMPORT_PLAN_AND_STRATEGY.md](../../../docs/VISA2014_MIGRATION/IMPORT_PLAN_AND_STRATEGY.md)

**Not here:** Visa2026 seed scenarios — [visa2026-dataimporter](../visa2026-dataimporter/SKILL.md). **Import runbook:** [import-practices.md](./import-practices.md).

---

## When to append (required)

| Event | Append? |
|-------|---------|
| **Import run succeeds** (pilot, batch, e2e wave, partial reimport) | **Yes** |
| **Import run fails** (exit code, OData 400, build blocked import) | **Yes** |
| **Import partial** (some rows failed/skipped) | **Yes** |
| Correction CLI (`--correct-*`) | **Yes** |
| File/image wave | **Yes** |
| Discovery dossier closed (`complete` / `blocked` / `skip`) | **Yes** |
| Excel preview exported or reviewed | **Yes** (note path + row counts) |
| Strategy decision locked or plan approved | **Yes** |
| Verified mapping fix (no import yet) | **Yes** |
| New migration script created (last resort) | **Yes** | Why existing scripts/CLI were insufficient; README row added |
| Exploratory SQL with no conclusion | No |
| User asked read-only question | No |

**Failure entries are mandatory.** They prevent the next session from repeating the same mistake.

Promote repeated patterns into [SKILL.md](./SKILL.md) after **2+** occurrences ([MATURITY.md](./MATURITY.md)).

---

## Entry templates

### Discovery / mapping

```markdown
### YYYY-MM-DD — <TargetODataEntity> — <short title>

- **Phase**: discovery | mapping
- **Dossier**: docs/VISA2014_MIGRATION/discovery/{Entity}.yaml
- **Legacy table(s)**:
- **Symptom / surprise**:
- **SQL / MCP that helped**:
- **Fix / mapping change**:
- **Reconciliation** (if any):
- **Prevent** (next session):
- **Artifacts**: field-map, lookup-translations, inventory
```

### Strategy / plan

```markdown
### YYYY-MM-DD — strategy — <decision title>

- **Phase**: strategy
- **Open decision id** (import-strategy.yaml):
- **Chosen option**:
- **Why**:
- **Artifacts**: IMPORT_PLAN_AND_STRATEGY.md, import-strategy.yaml
```

### Excel preview

```markdown
### YYYY-MM-DD — <TargetODataEntity> — excel preview

- **Phase**: excel-preview
- **Export path**: preview-export/{Entity}-preview.xlsx
- **Counts**: legacy __ → after dedupe __ → main sheet __ → skipped __
- **Surprises** (_UnmappedLookups, bad defaults):
- **Mapping fixes**:
- **Ready for importConfirmed**: yes | no
```

### Pilot / import run (success)

```markdown
### YYYY-MM-DD — <TargetODataEntity> — pilot | batch | e2e-wave

- **Phase**: import
- **Mode**: end-to-end | single-entity | partial-reimport | correction | file-wave
- **Outcome**: success
- **Environment**: local | staging | prod
- **Script / CLI**: e.g. scripts/visa2014-migration/import/OnPrem-Staging.ps1 or dotnet … --import-visa2014 --entity Person
- **Legacy source**: calik-energi | …
- **Counts**: legacy SQL __ → imported __ → target __
- **Skipped / dedupeMerged / failed**:
- **Reconciliation**: pass | partial (note)
- **Log**: legacy/visa2014/import-logs/…
- **Follow-up**:
```

### Import run (failed or partial)

```markdown
### YYYY-MM-DD — <TargetODataEntity> — import failed | partial

- **Phase**: import
- **Mode**: end-to-end | single-entity | partial-reimport | correction | file-wave
- **Outcome**: failed | partial
- **Environment**: local | staging | prod
- **Script / CLI**:
- **Exit code**:
- **Error** (snippet or OData message):
- **Counts** (if any): success __ / failed __ / skipped __
- **Root cause** (or hypothesis):
- **Fix / next step**:
- **Log**: …
- **migration-status issue** (if blocking): …
```

### Partial reimport (dev)

```markdown
### YYYY-MM-DD — <TargetODataEntity> — partial reimport

- **Phase**: import
- **Outcome**: success | failed | partial
- **Script**: scripts/visa2014-migration/reimport/<Entity>.ps1
- **Target DB**: (connection summary)
- **Steps run**: cleanup SQL | id-map rebuild | import | correction CLI
- **Counts / reconciliation**:
- **Mapping fix verified**: yes | no
- **Log**: legacy/visa2014/import-logs/reimport-…
- **Follow-up** (downstream BOs to re-run per order.yaml):
```

---

## Entries

> **Script paths (2026-07):** VISA2014 migration PowerShell/SQL moved from `scripts/local/` to **`scripts/visa2014-migration/`** — see [scripts/visa2014-migration/README.md](../../../scripts/visa2014-migration/README.md). Older entries below may still cite `scripts/local/…`; use the README index for current names.

### 2026-07-11 — Demo wipe-business + full reimport started

- **Phase**: end-to-end (on-prem Demo)
- **Mode**: end-to-end (`OnPrem-Sync.ps1 -Profile Demo -Mode Import`)
- **Outcome**: stopped on Passport fail; resumed after fix
- **Environment**: `10.100.128.25` / `Visa2026DbDemo` · `C:\visa2026-sync-demo` · task `Visa2026-OnPrem-DemoImportOnce`
- **Policy**: **do not** pass `-ContinueOnError` on Demo full import — one failed BO must stop the chain so we can fix and `-StartAt` resume
- **Passport fail**: 3642 posted / **5 failed** — legacy IssuedCountry **UAE** translated to Code `UAE` but Demo catalog only has **ARE** → `BuildPayload` null
- **Fix**: `lookup-translations.yaml` `UAE` → target **ARE**; resume `-StartAt Passport` (no ContinueOnError)
- **Side effect of ContinueOnError**: Visa completed; Education/EPH partially ran — resume skips via id-map
- **Watch**: `Watch-OnPremImportLive.ps1 -Profile Demo -ViaSsh -ClearScreen`
- **Cross-skill**: visa2026-onprem-legacy-sync

### 2026-07-10 — ApplicationProgress import live percent (Demo resume)

- **Phase**: end-to-end (on-prem Demo) — ApplicationProgress wave
- **Mode**: end-to-end (`-Profile Demo -Mode Import -StartAt ApplicationProgress`)
- **Outcome**: in progress (progress sidecar live)
- **Environment**: `Visa2026DbDemo` / `C:\visa2026-sync-demo` · RunId **20260710-052517**
- **Change**: Import path now writes `ApplicationProgress.sync-progress.json` every 100 rows with `processed/total/percent` + flushes stdout (same pattern as Sync upsert helper).
- **Counts**: **54985** prepared progress rows from **12354** legacy apps; early sample `100/54985 (0.2%)` posted=49 failed=51
- **Watch**: `Watch-OnPremImportLive.ps1 -Profile Demo -ViaSsh -ClearScreen` shows Wave progress bar from sidecar
- **Prevent**: Do not rely on redirected wave `.log` alone for mid-wave counts; redeploy DataImporter after progress-reporting changes
- **Cross-skill**: visa2026-onprem-legacy-sync

### 2026-07-10 — Full fresh Demo import started (Visa2026DbDemo / :8081)

- **Phase**: end-to-end (on-prem Demo)
- **Mode**: end-to-end (`OnPrem-Sync.ps1 -Profile Demo -Mode Import`)
- **Outcome**: in progress (Person completed; Passport running)
- **Environment**: `10.100.128.25` / `Visa2026DbDemo` · sync host `C:\visa2026-sync-demo` · legacy `10.100.128.15` / `VISA2015`
- **Script / CLI**: Scheduled Task `Visa2026-OnPrem-DemoImportOnce` → `Run-OnPremSyncOnServer.ps1 -Profile Demo -Mode Import -ContinueOnError` (SYSTEM; survives SSH)
- **Preflight**: Demo IIS upgraded **556 → 566**; wiped business rows (NULL `LatestProgressId` before delete); **no `-IncludeFileWaves`** (E: ~17 GB free; prod DB ~41 GB with files)
- **Blockers fixed this run**:
  1. Parallel prod DataImporter holds `:5002` → set `VISA2026_MIGRATION_IMPORT_URLS=http://127.0.0.1:5012` (not `ASPNETCORE_URLS`); `OnPrem-Sync.ps1` now auto-offsets Staging/Demo ports
  2. Fresh id-maps: `Visa2014IdMapHelper.Load` requires file → create empty `{}` stubs; orchestrator now stubs on `-Mode Import`
- **Counts so far**: People **3280** posted; Passport wave started; id-map `Person.json` ~272 KB
- **Watch**: `Watch-OnPremSyncRun.ps1` with `-SyncHostRoot C:\visa2026-sync-demo` (or SSH); UI `http://10.100.128.25:8081/LoginPage`
- **Prevent**: Do not clear id-map dir without leaving `{}` stubs; do not run two headless hosts on `:5002`; move Demo MDF to `E:\visa2026\sql-data\` before file waves
- **Cross-skill**: visa2026-onprem-legacy-sync | visa2026-windows-iis-deploy

### 2026-07-10 — Application ApprovalLegSnapshot backfill (Ministrlik) — tool added

- **Phase**: correction (CLI + patch script)
- **Mode**: correction
- **Outcome**: success (local + prod)
- **Environment**: local LocalDB `Visa2026`; prod `10.100.128.25\SQLEXPRESS` / `Visa2026DbProd`
- **Script / CLI**: `patch/Application-ApprovalLegSnapshots.ps1` → `--backfill-application-approval-leg-snapshots`; prod used SQL `cleanup/BackfillApplicationApprovalLegSnapshots.sql` (EF blocked by missing `BorderZoneLocation`)
- **Symptom**: Migrated via-ministry apps have progress rows but empty **Ministrlik** / status without `- Energetika` — `ApprovalLegSnapshots` never created at import.
- **Do not use**: `patch/ApplicationProgress-MinistryLegs.ps1` / `--correct-application-progress-ministry-legs` (deletes + regenerates progress).
- **Counts (local apply)**: scanned **4705** → needing **4701** → backfilled **4701**
- **Counts (prod apply)**: needing **4708** → inserted **9122** snapshots; **RemainingGaps=0**; active **9130**
- **Prevent**: Prefer snapshot backfill for label-only gaps; on schema-behind hosts use SQL patch; reserve ministry-legs patch for apps missing `*_REVIEW_*` progress rows.

### 2026-07-03 — ApplicationItem — reimport after PersonDomainDownstream FK wipe (success)

- **Phase**: partial-reimport (follow-up to Person-domain downstream cleanup that NULLed ApplicationItem FK columns)
- **Outcome**: success
- **Environment**: local — `(localdb)\mssqllocaldb` / `Visa2026`
- **Script**: `scripts/visa2014-migration/reimport/ApplicationItems.ps1` (`-Configuration Debug`)
- **Symptom before**: 21,345 ApplicationItems with `Person` only — all `CurrentPassport` / `CurrentVisa` / `CurrentPositionHistory` / `CurrentEducation` / `CurrentSalary` / `CurrentAddressOfResidence` NULL (caused by `ImportedPersonDomainChildren.sql` after Person-child reimport)
- **Import**: Prepared 21,588 / skipped 206 → **Posted 21,394** / failed **0** / id-map skip **194** (missing parent passport map)
- **Reconciliation (SQL)**: Total 21,394 — WithPassport **21,394**, WithVisa **15,294**, WithPosition **21,031**, WithEducation **12,495**, WithSalary **5,883**, WithAddress **9,142**
- **Log**: `legacy/visa2014/import-logs/reimport-ApplicationItem-20260703-121015.log`
- **Prevent**: After `reimport/PersonDomainDownstream.ps1`, always run `reimport/ApplicationItems.ps1` (or future FK repair command) — `--correct-application-item-person-current` alone does not restore Passport/Visa/Position/Address

### 2026-07-02 — ApplicationItem — partial reimport (success)

- **Phase**: import
- **Mode**: partial-reimport
- **Outcome**: success (with known skips)
- **Environment**: local — `(localdb)\mssqllocaldb` / `Visa2026`
- **Script**: `scripts/visa2014-migration/reimport/ApplicationItems.ps1` (`-Configuration Release`)
- **Legacy source**: calik-energi
- **Steps run**: cleanup SQL (21,345 deleted) → id-map rebuild → in-process import → PIA correction (started) → person-current correction (manual rerun)
- **Counts**: Prepared 21,588 / skipped 206 → **Posted 21,345** / failed 0 / id-map skip 243
- **Reconciliation**: `ApplicationItems` = 21,345; `CurrentEducation` populated 12,206; `CurrentSalary` populated 5,662 on manual-entry apps
- **Person-current correction**: 11,360 in scope; 0 updated (already set at import)
- **Log**: `legacy/visa2014/import-logs/reimport-ApplicationItem-20260702-172516.log`
- **Fixes this session**: `Get-RepoRoot.ps1` UTF-16 → UTF-8 (broke all `reimport/` scripts); `Visa2014TargetIdMapRebuild` `IdMapDirectory` + `AddressOfResidence` `_legacyRowId` string parse; `ApplicationItemPersonCurrentCorrection` `using DevExpress.ExpressApp`; cleanup SQL path `..\cleanup\ImportedApplicationItems.sql`
- **Follow-up**: PIA warnings for rows missing address id-map keys (3,290 address rows skipped at rebuild) — expected gap; ApplicationProgress unchanged (downstream BO not partial-reimported)

### 2026-06-20 — Person — bootstrap + discovery complete

- **Phase**: discovery
- **Dossier**: docs/VISA2014_MIGRATION/discovery/Person.yaml
- **Legacy table(s)**: dbo.Person (2,569 active), dbo.Employee (1:1), dbo.Passport (child)
- **Symptom / surprise**:
  - `Person.IDNumber` holds employer names, not civil ID — use `Passport.PersonalNumber`
  - Legacy `MaritalStatus.Status` is free-text family narrative, not Visa2026 catalog
  - 270 persons with multiple passports; 6 PersonalNumber collisions across different Person Oids
- **SQL / MCP that helped**: sqlcmd to `localhost\SQLEXPRESS` / VISA2015 (MCP visa2014-sql-local not in mcps folder — reload Cursor MCP)
- **Fix / mapping change**: table-mappings `person-main`, field-map with canonical passport join, Gender layer-3 rows
- **Reconciliation**: 2,569 active Person; 2,410 IsEmployee; 159 IsFamilyMember; 0 active without passport
- **Prevent**: Always read Passport for PersonalNumber; do not map IDNumber; audit MaritalStatus at importConfirmed
- **Artifacts**: schema-snapshot.md, Person.yaml, field-maps/Person.yaml, lookup-translations.yaml (Gender; Country completed in follow-up entry)

### 2026-06-20 — Person — Passport.PersonalNumber deep dive

- **Phase**: discovery
- **Legacy table(s)**: dbo.Passport (2,860 active rows)
- **Symptom / surprise**:
  - Civil ID lives on **Passport.PersonalNumber**, not Person (Person has no PersonalNumber column)
  - Person.**IDNumber** = employer/subcontractor text in production samples; legacy ImpPersonID → IDNumber link unused (0 rows)
  - Placeholders: **822** passports with `-`, **282** with `.` — map to Visa2026 sentinel `0`
  - **781** persons share PersonalNumber `-` (not unique across persons)
  - **29** persons have different PersonalNumber on different passports for same person
  - Dominant real ID length **11 digits** (Turkish TC-style)
- **Fix / mapping change**: canonical passport ORDER BY non-sentinel first, then PassportIssuedDate DESC; normalize `-`/`.` → `0`
- **Prevent**: Never upsert Person from Person.IDNumber; Passport import BO keeps per-passport PersonalNumber copy (Visa2026 Passport.PersonalNumber is hidden/legacy)

### 2026-06-20 — Person — Visa2026 PersonalNumber uniqueness

- **Phase**: discovery
- **Visa2026 rules**: `IX_People_PersonalNumber` (unique except NULL/''/'0'); `Person_PersonalNumberUniqueAmongActive` on save
- **Legacy impact**: ~1,024 persons → `"0"` (OK); **5 real PN values** each on **2 Person Oids** (same name+DOB — duplicate legacy rows)
- **Fix**: Dedupe merge on real PN before POST; **upsert/id-map on legacy Person.Oid** — not PersonalNumber as sole OData upsert key
- **Prevent**: Importer must normalize `-`/`.` → `"0"` and merge PN duplicates or OData/DB will reject second insert

### 2026-06-20 — Country — Person-scope lookup audit complete

- **Phase**: mapping
- **Legacy table(s)**: dbo.Country (1,861 rows, 240 distinct codes; many duplicate Oids per code)
- **Symptom / surprise**: Only **64** DISTINCT `NameOfCountryL` codes used on active Person (BirthCountry, ForeignAddressCountry, Passport.Citizenship) — all match Visa2026 `country.json` `Code` **1:1** (including `UAE`, not `ARE`)
- **SQL / MCP that helped**: sqlcmd UNION DISTINCT across three Person FK paths on VISA2015
- **Fix / mapping change**: 64 identity rows in `lookup-translations.yaml`; resolve by string Code not legacy Oid; `unmappedPolicy: block_row` safe for Person import
- **Prevent**: Re-audit Country DISTINCT when Application/other BOs add country FKs; do not import legacy Country table
- **Artifacts**: lookup-translations.yaml (Country audit block + values[]), migration-status.yaml (ISS-002 resolved)

### 2026-06-21 — strategy — file/image import separate from Excel

- **Phase**: strategy
- **Open decision id** (import-strategy.yaml): file-blob-strategy
- **Chosen option**: Planning locked — two tracks (scalar Excel/OData vs file wave); Person.Photo follow-up after scalar Person; attachments wave last. Transport TBD (recommend base64 PATCH for Photo, FileData two-step for scans).
- **Why**: Excel cannot hold photo/scan bytes for human review; 2,567/2,569 active Person rows have Photo (avg ~473 KB, max ~15 MB). PassportCopy ~9,157 rows deferred to attachments wave.
- **Artifacts**: FILE_AND_IMAGE_IMPORT.md, EXCEL_PREVIEW_EXPORT.md, import-strategy.yaml, field-maps/Person.yaml (Photo stubs)

### 2026-06-21 — strategy — import plan approved

- **Phase**: strategy
- **Open decision id** (import-strategy.yaml): (global approval — openDecisions[] remain for prod cutover)
- **Chosen option**: Baseline strategy in IMPORT_PLAN_AND_STRATEGY.md approved; `implementationBlocked: false`
- **Why**: Developer sign-off in chat; unblocks Excel preview CLI and `--import-visa2014` scaffolding. OData load still gated per BO by Excel preview + `importConfirmed`.
- **Artifacts**: import-strategy.yaml (status approved), IMPORT_PLAN_AND_STRATEGY.md, migration-status.yaml (ISS-001 resolved)

### 2026-06-21 — Person — excel preview export

- **Phase**: excel-preview
- **Export path**: Visa2026.DataImporter/legacy/visa2014/preview-export/Person-preview.xlsx
- **Counts**: legacy 2569 → import 2553 + duplicate_merged 5 + skipped 11
- **Surprises**: 3 sqlcmd parse junk rows skipped; 22 distinct unmapped Relationship/ProjectContract values on _UnmappedLookups sheet
- **Ready for importConfirmed**: pending human review

### 2026-06-21 — MaritalStatus — Status int approved + lookup review gate

- **Phase**: mapping | strategy
- **Legacy table(s)**: dbo.MaritalStatus (Status int 0–5 + StatusL narrative; 1,965 lookup rows)
- **Symptom / surprise**: Not free-text-only — coarse bucket is `Status` int; StatusL is family narrative (1,582 distinct prefixes for Status=0 alone)
- **Fix / mapping change**: Approved map Status 0–5 → Visa2026 `Code` (0→Öýlenen per user sign-off); StatusL → `VisaApplicationFamilyMembersText`; layer 3 in lookup-translations.yaml; preview exporter joins ms and translates
- **Prevent**: Do not set Person `importConfirmed` until person-wave queue complete (Relationship + ProjectContract next); application-wave gate before Application importConfirmed
- **Artifacts**: lookup-translations.yaml (MaritalStatus values[]), lookup-comparisons/lookup-review-queue.yaml, MaritalStatus.md/.yaml (approved), ISS-003 resolved, ISS-012 open

### 2026-06-21 — Multi-company legacy path — Çalik VISA2025

- **Phase**: strategy | tooling
- **Decision**: One legacy DB per company per Visa2026 deployment; `legacy-sources.yaml` + `--legacy-source calik-energi|gap-insaat`
- **Çalik pilot**: VISA2025 on SQLEXPRESS → LocalDB `Visa2026`; default CLI source `calik-energi`
- **Gap path**: VISA2015 + `lookup-translations.gap-insaat.yaml` (GT-15 remap preserved)
- **ProjectContract**: Çalik uses `identityPassThrough`; Gap keeps explicit GT-15 remap
- **importConfirmed**: reset for Person until `Person-preview.calik-energi.xlsx` reviewed
- **Blocker**: VISA2025 not listed on local SQLEXPRESS at agent check — attach DB in SSMS (ISS-015)
- **Artifacts**: MULTI_COMPANY_LEGACY_SOURCES.md, legacy-sources.yaml, Visa2014LegacySource.cs, lookup-translations.calik-energi.yaml, lookup-translations.gap-insaat.yaml

### 2026-06-26 — Unicode fix — sqlcmd → SqlClient

- **Phase**: tooling | excel-preview
- **Symptom**: Turkish/Turkmen characters (ö, ü, ş, ý, …) garbled in Person-preview.xlsx
- **Cause**: `sqlcmd` stdout decoded as UTF-8 on Windows; console/OEM code page mangled nvarchar text
- **Fix**: `Visa2014SqlCmdReader` now uses **Microsoft.Data.SqlClient** (`ExecuteReader`) — proper Unicode from `VISA2015`
- **Verify**: Re-export `Person-preview.calik-energi-unicode-fix.xlsx`; sheet XML contains `Gökhan`, `ý`, `ş` counts in thousands
- **Note**: Close Excel before re-exporting to default path (file lock fallback still applies)

### 2026-06-26 — ProjectContract — Çalik Energi re-audit (VISA2015)

- **Phase**: discovery | mapping
- **Legacy table(s)**: dbo.Contract; Person.Contract; Application.Contract; dbo.AppliedMinistery
- **Symptom / surprise**: Gap GT-15 remap irrelevant; 73 union codes vs 3-row tenant seed; Application-heavy codes (1574 -KIYANLI, 14306 Mary); no GT-15 in Çalik DB
- **SQL / MCP that helped**: sqlcmd ReadOnlyUser @ VISA2015 — counts 95/83/73, union Person+Application refs
- **Fix / mapping change**: Documented identity pass-through on Code; catalog seed 73 rows required before import
- **Artifacts**: ProjectContract.calik-energi.md, lookup-translations.calik-energi.yaml audit complete

### 2026-06-26 — ProjectContract deploy + Person dry-run (LocalDB)

- **Phase**: tooling | pilot-import
- **Catalog**: `project-contract.calik-energi.json` (73 rows); `Deploy-ProjectContractCalikEnergiCatalog.ps1`
- **Surprise**: Disk overlay alone does **not** override embedded `tenant/project-contract.json` — `LookupCatalogResourceLoader` prefers embedded. Deploy script copies calik → embedded, rebuilds, bumps overlay manifest **19**, then `updateDatabase --forceUpdate` with `FORCE_XAF_DB_UPDATE=true`
- **Verify**: LocalDB `Visa2026` — `project-contract created=73`; **87** `ProjectContracts` total (was 14)
- **Dry-run**: `--import-visa2014 --entity Person --legacy-source calik-energi --dry-run --max-rows 10` → **10 prepared, 0 skipped** (no POST; no API login)
- **Next**: Start Blazor on `:5001`, then live `--max-rows 10` (Admin password); full 2924 rows after spot-check
- **OData POST fixes (2026-06-26)**: ProjectContract resolve by `NameTm` prefix (Code not in EF); default Subcontractor; `PersonRole` string `"Employee"` not int; omit `IsArchived`, `VisaApplicationFamilyMembersText`, empty `Email` on POST
- **Pilot**: 7/10 posted on second batch; 3 failed duplicate PersonalNumber when prior test rows not deleted; OData DELETE returned 401 via curl — remove duplicates in UI or re-run after cleanup
- **Photo import (2026-06-26)**: `--import-visa2014-files --entity Person --property Photo` — SQL `dbo.Person.Photo` → OData PATCH via id-map; pilot 10/10 patched

### 2026-06-26 — Passport discovery (Çalik VISA2015)

- **Phase**: discovery | mapping
- **Legacy table(s)**: dbo.Passport, dbo.PassportType, dbo.Country
- **Counts**: 3684 active passports; 3241 persons; 353 multi-passport; 18 orphan Person FK; 4 duplicate PassportNumber groups (2 sentinel placeholders × 8)
- **PassportType**: Only 3 buckets on data — AD→P (3611), GL→PG (72), DP→PD (1); 231 rows reference soft-deleted type rows — map by TypeOfPassportL+mgCode composite
- **Visa2026 gaps**: Authority ← PassportIssuedPlace; Citizenship legacy column dropped (on Person); PersonalNumber hidden on Passport BO
- **Dedupe**: Visa2026 PassportNumber unique among active — sentinel `AF000000000` / `JL000000000` need Oid suffix strategy
- **Artifacts**: discovery/Passport.yaml, field-maps/Passport.yaml, lookup-comparisons/PassportType.md, order.yaml entry (ISS-005 resolved)
- **Blocked**: `--import-visa2014 --entity Passport` not implemented; importConfirmed false; needs full Person id-map first

### 2026-06-26 — Visa discovery (Çalik VISA2015)

- **Phase**: discovery | mapping
- **Dossier**: docs/VISA2014_MIGRATION/discovery/Visa.yaml
- **Legacy table(s)**: dbo.Visa, dbo.VisaType, dbo.IVisaType_Data, dbo.VisaCategory, dbo.VisaIssuedPlace, dbo.BorderZoneForVisa
- **Counts**: 6041 active visas; 4581 passports; 1460 multi-visa; 0 orphan Passport FK; 7 duplicate VisaNumber groups; 5976 inline scan blobs
- **Surprise**: VisaType labels live on IVisaType_Data (TypeOfVisaL + mgCode); 58 rows GL with null mgCode — no GL in Visa2026 visa-type.json; BorderZone is bit-matrix not comma-separated text
- **SQL / MCP that helped**: sys.columns on dbo.Visa; join counts via sqlcmd on VISA2015 SQLEXPRESS
- **Fix / mapping change**: field-maps/Visa.yaml — Passport id-map FK; sentinel AFV0000000/JLV0000000 dedupe; GöçürmeNusga → VisaDocument file wave
- **Prevent**: Approve VisaType/VisaCategory/VisaIssuedPlace/BorderZoneName layer-3 before importConfirmed; export Excel preview next
- **Artifacts**: discovery/Visa.yaml, field-maps/Visa.yaml, table-mappings visa-main, order.yaml, entity-inventory

### 2026-06-26 — VisaType lookup comparison (Çalik VISA2015)

- **Phase**: discovery | mapping
- **Scope**: dbo.Visa → IVisaType_Data (6041 rows)
- **Verdict**: Approved — 5 buckets; composite TypeOfVisaL:mgCode → LocalizationKey
- **Key mapping**: GL→OF (official/Gulluk visa, not Passport GL→PG); BS:14→BS1; default WP
- **Artifacts**: lookup-comparisons/VisaType.md, VisaType.yaml, lookup-translations.yaml

### 2026-06-26 — VisaCategory lookup comparison (Çalik VISA2015)

- **Phase**: discovery | mapping
- **Scope**: dbo.Visa → VisaCategory (6040 with FK; 1 null → skip)
- **Verdict**: Approved — köp/iki/bir gezeklik + mgCode 4/2/1 → Multiple/Double/Single (perfect 1:1)
- **Artifacts**: lookup-comparisons/VisaCategory.md, VisaCategory.yaml, lookup-translations.yaml

### 2026-06-21 — VisaIssuedPlace lookup comparison (Çalik VISA2015)

- **Phase**: discovery | mapping
- **Scope**: dbo.Visa → VisaIssuedPlace (6041 with FK; 0 null)
- **Verdict**: Approved — 22 distinct labels; 14 map to catalog (6023 rows); 8 embassy labels (18 rows) → skip_row
- **Key aliases**: Türkmenbaşy H.M.→Türkmenbaşy howa menzilindäki MGP; T-abat H.M.→Türkmenabat Howa Menzili; Farap G.Y.→Farap MGP; BERLİN→Berlin; Garabogaz→Garabogaz GY
- **Policy**: Do not default unmapped to catalog IsDefault (Aşgabat MGP) — skip preserves embassy accuracy
- **Artifacts**: lookup-comparisons/VisaIssuedPlace.md, VisaIssuedPlace.yaml, lookup-translations.yaml

### 2026-06-21 — BorderZoneName lookup comparison (Çalik VISA2015)

- **Phase**: discovery | mapping
- **Scope**: dbo.Visa → BorderZoneForVisa bit matrix (589 FK; 5452 null → Ýok)
- **Verdict**: Approved — 8 bits map to NameTm; Garabogaz şäher → Garabogaz şäheri; Sarahs unused on visas
- **Catalog**: Added 6 rows to tenant border-zone-name.json (Daşoguz şäher, Tagtabazar/Serhetabat/Farap/Etrek etrap, Ýolöten etrap)
- **Transform**: comma-separated labels in Helper bit order (not legacy space-concat)
- **Artifacts**: lookup-comparisons/BorderZoneName.md, BorderZoneName.yaml, lookup-translations.yaml

### 2026-06-21 — Visa Excel preview export (Çalik VISA2015)

- **Phase**: mapping | preview
- **CLI**: `--export-visa2014-preview --entity Visa --legacy-source calik-energi`
- **Counts**: 6041 legacy → 6016 import, 19 skipped (18 embassy + 1 null VisaCategory), 6 duplicate_merged
- **Code**: Visa2014VisaTransform.cs, Visa2014VisaPreviewExporter.cs (shared transform with future OData importer)
- **Output**: preview-export/Visa-preview.calik-energi.xlsx
- **Next**: human review → importConfirmed → Visa OData importer + VisaDocument file wave

### 2026-06-21 — Visa OData scalar + VisaDocument file import (implementation)

- **Phase**: import code
- **Files**: Visa2014VisaODataImporter.cs, Visa2014VisaDocumentImporter.cs; OData resolver extended (VisaType/VisaCategory/VisaIssuedPlace)
- **Web API**: VisaDocument registered (like PassportDocument)
- **Dry-run**: 6016 prepared, 251 would skip (Passport not in id-map) — expected orphan-passport gap
- **Blocked**: Blazor not running on :5001 for live POST; restart host then full import

### 2026-06-26 — Visa — pilot OData fix (ShowOptionalFields)

- **Phase**: import
- **Environment**: Visa2026DbDev (localhost:5001)
- **Symptom**: All Visa POSTs returned **400 Bad Request** / **"Incorrect body."**
- **Root cause**: `BuildPayload` included `ShowOptionalFields`, which is `[NotMapped]` on `Visa` — XAF OData rejects non-EDM properties (same pattern as omitting `Category` on Application POST).
- **Fix**: Drop `ShowOptionalFields` from `Visa2014VisaODataImporter.BuildPayload`; keep scalar flags (`IsCancelled`, `IsChanged`, `IsExtended`, `ExtensionRequired`) and lookups.
- **Pilot**: `--import-visa2014 --entity Visa --legacy-source calik-energi --max-rows 5` → **Posted: 5, Failed: 0**
- **Prevent**: Never POST `[NotMapped]` UI-only members (`ShowOptionalFields`, computed state) — mirror `VisaImporter.cs` / `Visa2014PassportODataImporter.cs` payload shape.

### 2026-06-26 — Visa full scalar OData import (calik-energi)

- **Phase**: import
- **CLI**: `--import-visa2014 --entity Visa --legacy-source calik-energi --no-wait`
- **Resume**: `Visa2014VisaODataImporter` loads existing `Visa.json` id-map; skips legacy OIDs already mapped (SkippedAlreadyImported) before POST — required after 5-row pilot.
- **Counts**: legacy 6041 → prepared 6016 (19 transform skip, 6 dedupe); **posted 5760**, failed 0, 251 no Passport id-map, 5 already imported; id-map **5765** entries.
- **Next**: `--import-visa2014-files` VisaDocument wave (GörmeNusga).

### 2026-06-26 — Education discovery started (calik-energi)

- **Phase**: discovery (in_progress)
- **Legacy**: `dbo.Education` — 3133 active rows, 3109 persons, 19 orphan Person FK; no varbinary (no file wave).
- **Visa2026 BO**: `Education.cs` — required lookups + optional `GraduationYear` (derived from `EducationEndDate` year; 2959 rows omit).
- **EducationLevel**: approved — mgCode → LocalizationKey (`lookup-comparisons/EducationLevel.md`).
- **Blocked**: EducationInstitution + Specialty NameTm catalog audits (1537 / 1254 distinct on data).
- **Artifacts**: `discovery/Education.yaml`, `field-maps/Education.yaml`, `education-main` in table-mappings; registered in `order.yaml`.
- **Next**: Institution/Specialty lookup comparisons → Excel preview → `importConfirmed`.

### 2026-06-26 — Education Institution + Specialty lookup gap analysis

- **Tool**: `preview-export/_education-lookup-gap/` (EduGap) — normalize match via `Visa2014CatalogMatchHelper` rules.
- **EducationInstitution**: 1037/3133 rows mapped on current 953-row seed; **2096 rows** need **1471** DISTINCT legacy labels seeded (`education-institution.calik-energi.json`).
- **Specialty**: 956/3133 mapped; **2177 rows** need **1063** DISTINCT `TitleOfSpeciality` seeded (`specialty.calik-energi.json`). Top gap: Tehniki howpsuzlyk we zähmeti goramak (401 rows).
- **Verdict**: `approved_with_catalog_seed` — identity pass-through like ProjectContract; reject skip_row without seed.
- **Artifacts**: `lookup-comparisons/EducationInstitution.md|.yaml`, `Specialty.md|.yaml`, `lookup-translations.calik-energi.yaml`, `analysis.json`.
- **Next**: generate tenant JSON seeds + manifest entries → Excel preview.

### 2026-06-26 — Education calik-energi catalogs + Excel preview

- **Script**: `scripts/local/Generate-EducationLookupCalikEnergiCatalogs.ps1` — union DISTINCT Education labels + existing seed rows.
- **Catalogs**: `education-institution.calik-energi.json` **1471** rows; `specialty.calik-energi.json` **1063** rows; tenant `manifest.json` v21.
- **Preview**: `Education-preview.calik-energi.xlsx` — **3108** import rows, 6 skipped (orphan Person FK), 25 unmapped lookup distinct (mostly edge labels); legacy SQL 3114 with-valid-Person rows.
- **Build fix**: exclude `preview-export/_education-lookup-gap/` from DataImporter csproj (nested EduGap.csproj caused duplicate assembly attributes).
- **Next**: deploy catalogs to dev DB (copy calik JSON → `education-institution.json` / `specialty.json` + `FORCE_XAF_DB_UPDATE`), human `importConfirmed`, `Visa2014EducationODataImporter`.

### 2026-06-26 — Education OData import complete (calik-energi)

- **CLI**: `--import-visa2014 --entity Education --legacy-source calik-energi`
- **Counts**: **2958 posted**, 0 failed, 150 no Person id-map, 6 transform skipped (orphan Person FK).
- **Id-map**: `id-maps/calik-energi/Education.json`
- **Country**: legacy `mgCode` often `ISO3-SUFFIX` (e.g. `GBR-WELIKOBRITANIYA`) — `NormalizeLegacyCountryMgCode` strips prefix; **ALB** added to global `country.json` manifest v3.
- **Institution**: OData import does not POST `EducationInstitution`; resolver uses normalized NameTm keeper when duplicates exist.
- **importConfirmed** 2026-06-26. Next BO: **Application** discovery.

### 2026-06-26 — EmployeePositionHistory discovery started (calik-energi)

- **Legacy**: `dbo.WorkHistoryOfEmployee` — **2993** active rows; FK `Employee` → Person (0 orphan); no `EndDate` column.
- **Visa2026 BO**: `Position` + `ActualPosition` (required) + `Department` + `StartDate`/`EndDate`; omit `ShowOptionalFields` on POST.
- **EndDate**: derive next `StartDateOnThisPosition` per Person (41 multi-history employees).
- **Lookups**: **1579** distinct `TitleOfPosition` vs tenant `position.json` **259**; **74** departments vs seed **3** — calik-energi catalog seeds pending.
- **ActualPosition**: mirror legacy position title (find-or-create by `Name`); not in tenant manifest.
- **Artifacts**: `discovery/EmployeePositionHistory.yaml`, `field-maps/EmployeePositionHistory.yaml`, `employee-position-history-main` in table-mappings; registered in `order.yaml`.
- **Next**: gap analysis scripts + `position.calik-energi.json` / `department.calik-energi.json` → Excel preview → `importConfirmed`.

### 2026-06-26 — EmployeePositionHistory catalogs + Excel preview (calik-energi)

- **Catalogs**: `position.calik-energi.json` **1579** rows, `department.calik-energi.json` **74** rows (from VISA2015 WorkHistory DISTINCT + seed union).
- **Preview**: `EmployeePositionHistory-preview.calik-energi.xlsx` — **2993** import rows, **0** skipped, **0** unmapped lookups; EndDate derived per Person.
- **ActualPosition**: `trim(Position.Code)` or `"-"` on **2289** empty-code rows.
- **Next**: deploy catalogs (`Deploy-PositionDepartmentLookupCalikEnergiCatalogs.ps1` + manifest v25), ensure `ActualPosition` Name `"-"` in target DB, OData importer + pilot.

### 2026-06-26 — EmployeePositionHistory OData import (calik-energi)

- **Deploy**: manifest v25; LookupCatalogSync position created=1377 updated=202, department created=74.
- **OData**: **2838 posted**, 0 failed, 151 no Person id-map, 4 pilot skip-already-imported; **194** ActualPositions find-or-create (~2.3 min).
- **Id-map**: `id-maps/calik-energi/EmployeePositionHistory.json`
- **Code**: `Visa2014EmployeePositionHistoryODataImporter.cs`; resolver Position/Department/ActualPosition.
- **Sign-off**: `discovery/EmployeePositionHistory.yaml` + `order.yaml` — `importConfirmed: true`, `importStatus: done`.

### 2026-06-26 — Visa VisaDocument file wave (calik-energi)

- **CLI**: `--import-visa2014-files --entity Visa --property VisaDocument --legacy-source calik-energi`
- **Counts**: **5571 posted**, 0 failed, 276 no visa map, 45 no blob, 149 oversize (>5MB); ~18 min.
- **Id-map**: `id-maps/calik-energi/VisaDocument.json`
- **Visa entity** scalar + files complete for calik-energi.

### 2026-06-26 — EmployeePositionHistory calik-energi catalogs + Excel preview

- **Scripts**: `Generate-PositionDepartmentLookupCalikEnergiCatalogs.ps1`, `Deploy-PositionDepartmentLookupCalikEnergiCatalogs.ps1` (overlay manifest v25).
- **Catalogs**: `position.calik-energi.json` **1579** rows; `department.calik-energi.json` **74** rows (union DISTINCT WorkHistory labels + tenant seed).
- **Preview**: `EmployeePositionHistory-preview.calik-energi.xlsx` — **2993** import rows, 0 skipped, 0 unmapped lookup distinct; EndDate derived per Person from next StartDate.
- **Transform**: `Visa2014EmployeePositionHistoryTransform` + preview exporter; ActualPosition = trim(Position.Code) or `"-"`.
- **Next**: human `importConfirmed`, `Visa2014EmployeePositionHistoryODataImporter` (not implemented yet).

### 2026-06-26 — Education diploma copies file wave (calik-energi)

- **Legacy source**: `dbo.PassportCopy` rows with `Education` FK (not `Passport` FK) — **4317** rows, **4287** with blob, **40** oversize; up to **15** copies per Education.
- **Target**: `EducationDocument` + `FileData` on parent `Education` (id-map required).
- **CLI**: `--import-visa2014-files --entity Education --property EducationDocument --legacy-source calik-energi`
- **Code**: `Visa2014EducationDocumentImporter.cs`; register `EducationDocument` on OData in `WebApiServiceExtensions.cs` (was missing vs PassportDocument).
- **Gate**: restart Blazor after OData registration rebuild before POST (F5 file lock).

### 2026-06-26 — File copy naming + blob dedupe (Passport / Visa / Education)

- **Naming**: `passport-{PassportNumber}-copy`, `visa-{VisaNumber}-copy`, `diploma-{PersonFirstName LastName}-copy` (+ `-2` suffix when multiple distinct blobs per parent).
- **Dedupe**: SHA256 per target parent; on resume, seed dedupe set from id-map rows (read legacy blob before skip) so duplicate diploma copies are not re-posted.
- **Already imported** (~2360 EducationDocument): still show old `passport-copy-{guid}` names — cleanup/rename separately if needed; duplicates already in DB must be deleted manually.

### 2026-06-27 — EducationDocument cleanup + resume import (calik-energi)

- **Phase**: import | tooling
- **Environment**: Visa2026DbDev (localhost:5001, LocalDB Visa2026)
- **Pre-cleanup state**: 3903 active EducationDocument rows; 3903 id-map entries; all FileName `passport-copy-{guid}`; 3 duplicate blobs (same SHA256 per Education); 1217 educations with multiple docs
- **Cleanup CLI**: `--cleanup-visa2014-education-documents` (`Visa2014EducationDocumentCleanup.cs`) — OData DELETE duplicates (XAF soft-delete GCRecord=1), PATCH FileData.FileName → `diploma-{FirstName LastName}-copy`, prune id-map for removed rows
- **Cleanup result**: 3 duplicates removed, 3900 renamed, 0 failed; id-map 3900 entries
- **Resume import**: `--import-visa2014-files --entity Education --property EducationDocument --legacy-source calik-energi --no-wait`
- **Counts**: legacy 4317 → **posted 109**, skipped already imported 3900, duplicate blob 15, no education map 231, no blob 28, oversize 34, failed 0; id-map **4009**; active DB rows **4009** (all `diploma-*` named)
### 2026-06-27 — AddressOfResidence — inference pass + re-export

- **Phase**: excel-preview | mapping
- **Export path**: `preview-export/AddressOfResidence-preview.calik-energi.xlsx`
- **Counts**: legacy **3971** → import **3968** (99.92%), skipped **3** (was 1209), unmapped lookups **3** (was 80)
- **Transform**: expanded `InferRegionMgCode` (ş./s. Aşgabat, Askabat typo, Türkmenabat/Daşoguz/Türkmenbaşy ş prefixes) and `InferCityFromAddressLine` (Mary/Lebap/Balkan/Ahal etrap defaults, hotel lines, S.Türkmenbaşy şäherçesi)
- **Remaining skips**: ~3 bare Aşgabat street lines (`1955 köç…`) with no welaýat prefix — accept or add manual override
- **Ready for importConfirmed**: **yes** after spot-check (pending human flag)

### 2026-06-27 — AddressOfResidence — Lodging orphan admin strip

- **Phase**: mapping | excel-preview
- **Problem**: after Region/City prefix removal, Lodging kept fragments like `nyn`, `etr.,`, `aýatynyň`, `Mary etrabynyň`, `etr.Guwlymayak`.
- **Root causes**: (1) `StripKnownPrefix` cut on catalog `welaýaty` left glued `nyn` when legacy used ASCII `welayatynyn`; (2) `wel\.?` regex matched only `wel` inside `welayatynyn`; (3) `etr.` glued to next word without space.
- **Fix** (`Visa2014AddressLineNormalizer.cs`): run `StripWelPrefix`/`StripEtrapPrefix` before catalog prefix match; folded-index cut + Turkmen glued suffix extension; tighten wel/etr regex; expand `StripOrphanAdministrativeFragments` (incl. `çäginde`, glued `etr.`).
- **Re-export**: import **3968**, skipped **3**; orphan Lodging prefix scan **0** bad rows (was ~72).

### 2026-06-27 — Hotel catalog — ş./şäher/wel. name cleanup

- **Phase**: excel-preview | mapping
- **Export path**: `preview-export/Hotel-preview.calik-energi.xlsx`
- **Problem**: legacy hotel `AddressLine` values kept city/region admin fragments in catalog `Name` (`ş."Mary"`, `şäh.`, `Serhetabat ş.`, `wel.Milli syýahatçylyk zolagy "Awaza"`, glued `ş."Ýyldyz"myhmanhanasy`).
- **Fix** (`Visa2014AddressLineNormalizer.NormalizeHotelCatalogName`): hotel-specific strip after Region/City; require `ş.` dot before unquoted capture (avoid eating `şaher` as `ş`+`aher`); partial `äher`/`äh.` orphans; quote unwrap + glued `"myhmanhan` spacing; restore `{city} myhmanhanasy` when strip leaves generic suffix only.
- **Wiring**: `TryBuildHotelSiteAddress` + `Visa2014HotelTransform`; AddressOfResidence Hotel column uses same normalizer.
- **Re-export**: legacy **52** → **26** catalog names (+ **26** dedupe-merged), **0** skipped.

### 2026-06-27 — Hotel + Hospital tenant catalogs (calik-energi)

- **Phase**: lookup | excel-preview
- **Generate**: `scripts/local/Generate-HotelHospitalCalikEnergiCatalog.ps1` from preview xlsx (`Import-Visa2014PreviewCatalogRows.ps1` — C# normalizer output, not PS strip).
- **Output**: `hotel.calik-energi.json` **22** rows; `hospital.calik-energi.json` **4** rows.
- **Deploy**: `scripts/local/Deploy-HotelHospitalLookupCalikEnergiCatalog.ps1` → copy to embedded `hotel.json` / `hospital.json`, manifest **v30**, then `Update-LocalDatabase.ps1 -ForceUpdate`.

### 2026-06-27 — Lodging catalog — wel./ş./w, prefix cleanup (round 2)

- **Phase**: excel-preview | mapping
- **Problem**: `FullAddress` still led with `wel.`, `wel-ň`, `w,`, `we.`, `ş.`, `S.`, `Balkanabat ş,`, orphan `ň`, `etr-n` after region/city strip (lodging used `StripRegionAndCityPrefixes` only; PS generate script was out of sync with C#).
- **Fix** (`NormalizeLodgingCatalogAddress`): lodging-specific admin strip (extends hotel patterns) + `etr-n` / ASCII `s,` şäher shorthand; `TryBuildLodgingSiteAddress` + AddressOfResidence Lodging column; `Generate-LodgingCalikEnergiCatalog.ps1` now reads **Lodging-preview** xlsx (no stale seed merge).
- **Re-export**: legacy **106** → **85** catalog rows (+ **19** dedupe-merged), orphan prefix scan **0** bad rows.

### 2026-06-27 — Lodging/hotel split — Lojman myhmanhan lines → Hotel catalog

- **Phase**: lookup | excel-preview | mapping
- **Pattern**: legacy `DocumentOfAddress=Lojman` rows whose `AddressLine` contains `myhmanhan` (folded) are **Hotel**, not Lodging — `Visa2014ResidenceClassifier.IsHotelAddressLine`; `MapResidenceType` in `Visa2014AddressOfResidenceTransform.cs`.
- **Catalog generate**: move hotel-named lines out of Lodging preview into Hotel preview; regenerate `lodging.calik-energi.json` **67** rows + `hotel.calik-energi.json` **33** rows (no myhmanhan left in lodging catalog).
- **Deploy**: tenant overlay to LocalDB before AddressOfResidence re-export (lodging/hotel FK resolution uses deployed catalogs).
- **AddressOfResidence re-export**: legacy **3971** → import **3968**, skipped **3** (unchanged vs inference pass); Type **Lodging 2378** / **PrivateHouse 1148** / **Hotel 442**; unmapped **3** (Region/City on skipped Patent-only rows — not hotel/lodging gaps).
- **Shell**: `$env:VISA2014_SQL_PASSWORD = [Environment]::GetEnvironmentVariable('VISA2014_SQL_PASSWORD','User')` — User-level env is not inherited by Cursor agent shells by default.

- **Phase**: discovery | excel-preview | lookup
- **Export path**: `preview-export/AddressOfResidence-preview.calik-energi.xlsx`
- **Counts**: legacy SQL **3971** → import **2762** → skipped **1209** → unmapped lookups **80** distinct
- **Surprises**:
  - VISA2015 city table/column is **`ŞäherEtrap`** (U+015E + U+00E4), not `ŞeherEtrap` — `OBJECT_ID` fails on wrong spelling; use `UNICODE(SUBSTRING(name,1,2))` on `sys.tables` to verify.
  - SQL row count < 4083 active because extract joins `Person` with `GCRecord IS NULL`.
  - `LookupCatalogResourceLoader.LoadCatalogFile` preferred embedded tenant JSON over disk overlay — F5 lock kept 7-row embedded `lodging.json` in running app; **fixed** to prefer `{AppBase}/LookupCatalogs/tenant/` first.
- **Lodging catalog**: `lodging.calik-energi.json` **96** rows; manifest **v28**; DB sync pending **Shift+F5 + rebuild** (Module.dll locked by debug session).
- **Ready for importConfirmed**: **no** — review `_Skipped` + `_UnmappedLookups` sheets first.

## 2026-06-27 — AddressOfResidence OData importer (calik-energi)

- **Phase**: import-code
- **Pattern**: `Visa2014AddressOfResidenceODataImporter` mirrors Education/EmployeePositionHistory — transform `PrepareImportBatch`, Person id-map, `Visa2014ODataLookupResolver` for Region/City/Lodging/Hotel/Hospital, POST + id-map.
- **CLI**: `--import-visa2014 --entity AddressOfResidence --legacy-source calik-energi [--dry-run] [--max-rows N]`
- **Gate**: `importConfirmed` still **false** in discovery — dry-run/pilot before full 3968-row load.

### 2026-06-29 — AddressOfResidence OData importer verified (dry-run + pilot gate)

- **Phase**: import-code | pilot
- **Code**: `Visa2014AddressOfResidenceODataImporter.cs`; `Visa2014ODataLookupResolver` extended with Region/City/Lodging/Hotel/Hospital; wired in `Visa2014ImportCommand.cs`.
- **Dry-run** (`--dry-run --no-wait`): legacy **3971** → prepared **3968**, transform skipped **3**, dedupe **0**, would skip **182** (Person not in id-map).
- **Pilot** (`--max-rows 5 --no-wait`): auth OK; **failed** loading lookups — `GET Hotel` returned HTML (`'<' is an invalid start of a value`) because **Hotel/Hospital were not on OData** (only Lodging was registered).
- **Fix**: register `Hotel` + `Hospital` in `WebApiServiceExtensions.cs` (same as Lodging). **Restart Blazor** after rebuild before retrying pilot.
- **Full-import blockers**: `importConfirmed: false`; ~182 rows lack Person id-map; tenant lodging/hotel/hospital catalogs must match deployed LocalDB; server must expose all five lookup entities on OData.

## 2026-06-21 — Lodging dedupe + site catalog deploy + AddressOfResidence importConfirmed (calik-energi)

- **Phase**: excel-preview | deploy | import-pilot
- **Lodging dedupe**: `BuildLodgingDedupeKey` in `Visa2014AddressLineNormalizer` — strip location fluff, compact alphanumeric key, typo folds (`Enerjy`, `Çalik`/`Çalık`, `UÝJf`); `_dedupeKey` column in preview; `ResolveLodging` falls back to dedupe key match.
- **Counts**: Lodging catalog **48 → 37** import rows (**22** duplicate_merged).
- **Deploy**: `scripts/local/Deploy-SiteLookupCalikEnergiCatalogs.ps1` (lodging + hotel + hospital + other-site); `Update-LocalDatabase.ps1 -ForceUpdate -SkipBuild` — sync created lodging **37**, hotel **34**, hospital **4**, other-site **24**.
- **Sign-off**: `Lodging-preview.calik-energi.xlsx` reviewed; `importConfirmed: true` on AddressOfResidence dossier + order.yaml **2026-06-21**.
- **Pilot**: restart Blazor after OData entity registration; use `--max-rows` for first POST batch; expect ~182 skips without full Person id-map on full run.

### 2026-06-29 — AddressOfResidence full OData import (calik-energi)

- **Phase**: pilot | batch | reconcile
- **Dry-run**: legacy **3971** → prepared **3968**, transform skipped **3**, **182** missing Person id-map.
- **Pilot** (`--max-rows 50`): **49** posted + **1** resume on full run; resolver fixes in `Visa2014ODataLookupResolver` (city `RegionName` enrich from `city.json`; lodging/other-site dedupe without OData row `CityId`; region-scoped scalar; hotel name fallback).
- **Full import**: **3737** posted, **0** failed, **182** skipped (no Person map), **49** already imported (pilot id-map); **OData count 3786** matches posted + pilot.
- **Known gaps**: **182** Person-missing rows; **3** transform skips (Patent, no Region/City FK) — unchanged from preview.
- **Docs**: `order.yaml` + `entity-inventory.yaml` `importStatus: done`; discovery dossier `complete` + `odataImport` block.
- **Next**: Application wave (`order.yaml` application-domain); optional backfill of 182 rows if Person id-map grows.

### 2026-06-29 — EmployeeSalary discovery + Excel preview (calik-energi)

- **Phase**: discovery | excel-preview
- **Legacy shape**: `dbo.Employee.Salary` FK → `dbo.Salary.Detail` (lookup text, not history). **2950** active employees; **no** legacy `Currency` or `StartDate` columns.
- **Target**: one `EmployeeSalary` per employee — `Amount` (normalized string), `Currency` **USD** (all rows; legacy dtm ignored), `StartDate` = MAX(`WorkHistoryOfEmployee.StartDateOnThisPosition`), `EndDate` null.
- **Normalizer**: `Visa2014SalaryAmountNormalizer` — extract numeric from labor-contract sentences; `1.667,00` → `1.667.00`; skip unparseable (e.g. `Alesta`).
- **Preview**: `EmployeeSalary-preview.calik-energi.xlsx` — **2887** import, **63** skipped (empty/unparseable Detail); `_AmountParse` audit sheet.
- **Blockers before OData**: `importConfirmed: false`; register `EmployeeSalary` on OData; implement importer + id-map (Person Oid key).
- **Next**: human review `_AmountParse`; then `importConfirmed: true` → OData implementation.

### 2026-06-29 — EmployeeSalary importConfirmed + OData importer

- **Phase**: importConfirmed | implementation
- **Sign-off**: `importConfirmed: true` 2026-06-29; currency fixed USD.
- **Code**: `Visa2014EmployeeSalaryODataImporter.cs`, `WebApiServiceExtensions` + `EmployeeSalary` OData, `Models.EmployeeSalary`.
- **Dry-run**: 2887 POST-ready, 63 transform skipped, 145 missing Person id-map.
- **Pilot**: 400 on POST — **restart Blazor** after `EmployeeSalary` OData registration (running host has old Web API model).
- **Fix**: OData `Currency` must be string `"USD"` not int `1` (400 Incorrect body).
- **Full import** 2026-06-29: **2740** posted, **0** failed, **145** no Person map, **2** pilot resume-skipped, **63** transform skipped. Id-map: `id-maps/calik-energi/EmployeeSalary.json`.

### 2026-06-29 — MedicalRecord discovery (SpidKepilnama file chain, calik-energi)

- **Phase**: discovery
- **Legacy path**: `IPersonn_SpidKepilnama` → `Copy` → `FileData` (`IPerson.SpidKepilnama` in VISA2014 repo) — **not** scalar medical fields on Person/Employee.
- **Çalik counts**: **2** active link rows, **0** resolvable `Copy` rows (orphan FKs), **0** importable blobs.
- **Scalar sign-off**: `DocumentNumber` = `"0"`; `IssueDate` = `MIN(AuditDataItemPersistent.ModifiedOn)` on `ObjectCreated` for `Copy` + `FileData` OIDs via `AuditedObjectWeakReference.GuidId` (sample verified 2014-01-25); `ValidityDuration` = **Month3** (90 days) → `ExpirationDate` derived on save.
- **Skip**: orphan Copy link, null `FileData.Content`, no audit row (`_issueDateSource: no_audit`), Person not in id-map.
- **Artifacts**: `discovery/MedicalRecord.yaml`, `field-maps/MedicalRecord.yaml`, `table-mappings.yaml` `medical-record-spid-kepilnama`, `order.yaml` attachments entry.
- **importConfirmed**: `true` 2026-06-29 (developer). Çalik file wave still expected 0 rows; Application wave can proceed.
- **Next**: implement file importer (`--import-visa2014-files --entity MedicalRecord --property MedicalRecordDocument`).

### 2026-06-29 — MedicalRecord file importer (calik-energi)

- **Phase**: implementation | file-import
- **Code**: `Visa2014MedicalRecordDocumentImporter.cs`, `Visa2014LegacyAuditIssueDateHelper.cs`; `MedicalRecordDocument` OData registration; CLI in `Visa2014FilesImportCommand`.
- **Flow**: Spid link → resolve Person id-map → audit `ObjectCreated` → POST `MedicalRecord` (Doc# `0`, Month3) → `FileData` → `MedicalRecordDocument`.
- **Dry-run + full run** 2026-06-29: **0** posted, **2** orphan copy links, **0** failed. Çalik has no importable blobs.
- **Note**: restart Blazor after `MedicalRecordDocument` OData registration before first POST on a host with blobs.

### 2026-06-29 — Application — Phase 1 discovery complete

- **Phase**: discovery
- **Dossier**: docs/VISA2014_MIGRATION/discovery/Application.yaml
- **Legacy table(s)**: dbo.Application (12,237 active / 18,118 total) + dbo.IRegistration_Data (numbering); SimpleProcess 8,392 / LongProcess 3,845 via XPObjectType
- **Symptom / surprise**:
  - Legacy type is **not** a single FK — composite ForEmployee/ForFamilyMember + ApplicationTypeForEmployee/FamilyMember SubType ID + invitation/visa WP flags
  - **862** duplicate `ManualApplicationNumber` groups (e.g. `1/-2` × 8) — upsert on Oid, not FullApplicationNumber
  - Contract FK only on long-process rows (3,845) — matches ministry workflow; ProjectContract calik overlay already approved
  - SubType IDs **44** (92 rows) and **55** (13 rows) have no Visa2026 ApplicationType mapping yet
- **SQL / MCP that helped**: sqlcmd `localhost\SQLEXPRESS` / VISA2015 — INFORMATION_SCHEMA + DISTINCT composite type query
- **Fix / mapping change**: `application-main` table map, `field-maps/Application.yaml`, layer 3 Urgency + VisaPeriod (Application scope) + ApplicationType composite in `lookup-translations.yaml`
- **Prevent**: Discover ApplicationItem before Excel preview (34,161 PersonInApplication rows); resolve E:44/E:55 before importConfirmed
- **Artifacts**: discovery/Application.yaml, field-maps/Application.yaml, table-mappings.yaml, lookup-translations.yaml, entity-inventory.yaml, property-gap-registry.yaml

### 2026-06-29 — ApplicationItem — Phase 1 discovery complete

- **Phase**: discovery
- **Dossier**: docs/VISA2014_MIGRATION/discovery/ApplicationItem.yaml
- **Legacy table(s)**: dbo.PersonInApplication (21,794 active / 40,414 total), TravelInformation, AddressOnBusinessTrip, WorkPermit/WorkPermitLocation
- **Symptom / surprise**:
  - schema-snapshot ~34,161 is partition total — **21,794** active after `GCRecord IS NULL` (reconcile imports on active count)
  - FM lines set **both** Employee + FamilyMember (2,759 rows) — Person FK must use Application.ForFamilyMember flag, not COALESCE
  - Legacy **WorkPermit** FK → Visa2026 **CurrentWorkPermitItem** (WorkPermitItem id-map, same Oid) — ApplicationItem ordered before WorkPermitItem in order.yaml
  - **NextVisa** not a column — 5,744 Visa rows link `ProcessNumber = PersonInApplication.Oid`
  - Parent ApplicationType **E:44** (187 item rows) / **E:55** (17 item rows) inherit header block
- **SQL / MCP that helped**: sqlcmd VISA2015 — INFORMATION_SCHEMA PersonInApplication; DISTINCT PurposeOfTravelL + CheckPoint mgCode
- **Fix / mapping change**: application-item-main table map, field-maps/ApplicationItem.yaml, layer 3 PurposeOfTravel + CheckPoint in lookup-translations.yaml
- **Prevent**: Dedupe 925 (Application+Person) groups before POST; omit ShowOptionalFields; gate FKs by ApplicationType Show* flags
- **Artifacts**: discovery/ApplicationItem.yaml, field-maps/ApplicationItem.yaml, table-mappings.yaml, entity-inventory.yaml, property-gap-registry.yaml

### 2026-06-29 — ApplicationType — E:44/E:55 approved skip_row

- **Phase**: mapping
- **Decision**: User approved skipping legacy composite keys `E:44:na:na:na` (92 apps, 187 items) and `E:55:na:na:na` (13 apps, 17 items) instead of blocking import.
- **Policy change**: `unmappedPolicy: skip_row` on ApplicationType catalog; `missingBehavior: skip_row` on Application field-map composite transform.
- **Counts**: 105 Application headers + 204 ApplicationItem rows skipped (items cascade with parent).
- **Not done**: `importConfirmed` left false — skip decision only; broader applicationWaveComplete gate still applies.
- **Artifacts**: lookup-translations.yaml#ApplicationType, field-maps/Application.yaml, lookup-comparisons/ApplicationType.md, lookup-review-queue.yaml, migration-status.yaml ISS-008

### 2026-06-29 — Application Excel preview export (calik-energi)

- **Phase**: excel-preview
- **Code**: `Visa2014ApplicationTransform.cs`, `Visa2014ApplicationPreviewExporter.cs`; wired in `Visa2014PreviewExportCommand` + `legacy-sources.yaml`.
- **SQL**: `dbo.Application` + `IRegistration_Data` + type/WP/urgency/visa/contract/border-zone/business-trip joins; `ŞäherEtrap` unicode table name; `GoşmaçaIşlemägeRugsatÝeri` movement-permit FK.
- **Transform**: ManualApplicationNumber → prefix/number; ApplicationType composite `{E|F}:{subtype}:{invWp}:{wizaWp}:{changeInfo}`; dedupe groups in `_DedupeSummary` with `keep_all_import_with_oid_upsert` (no duplicate_merged).
- **Export**: `Application-preview.calik-energi.xlsx` — **12237** legacy, **12129** import, **108** skipped (105 E:44/E:55 + 3 required-null), **862** dedupe groups, **0** duplicate_merged.
- **Next**: human review skipped sheet; then ApplicationItem preview export.

### 2026-06-29 — ApplicationProgress preview reviewed; importConfirmed

- **Phase**: excel-preview sign-off
- **Decision**: Developer approved simple/long synthesis in `ApplicationProgress-preview.calik-energi.xlsx` (32,177 rows / 108 parent skips).
- **Gate**: `importConfirmed: true` on discovery + order.yaml; OData implementation still after Application id-map.

### 2026-06-29 — ApplicationProgress synthesis approved + Excel preview

- **Decision**: Developer approved synthesis matrix (simple vs long process steps).
- **Export**: `ApplicationProgress-preview.calik-energi.xlsx` — **12,237** legacy apps → **32,177** progress rows, **108** parent skips (E:44/E:55).
- **Code**: `Visa2014ApplicationProgressTransform.cs`, `Visa2014ApplicationProgressPreviewExporter.cs`.
- **Next**: preview review → importConfirmed; OData after Application id-map; transition validation TBD.

### 2026-06-29 — Application preview reviewed; importConfirmed

- **Phase**: excel-preview sign-off
- **Decision**: Developer approved `Application-preview.calik-energi.xlsx` (12,129 import / 108 skipped).
- **Mapping lock**: `IsManualEntry=true` for all import rows (not `!AutoRegistration`) — preserves legacy numbers on OData POST.
- **Gate**: `importConfirmed: true` on discovery/Application.yaml + order.yaml.
- **OData (2026-06-29)**: `Visa2014ApplicationODataImporter` — POST `IsManualEntry=true` + `FullApplicationNumber` only (omit `ApplicationNumber`/`AppNumberPrefix`) so `Application.OnSaving` copies legacy full number without company-format rebuild. Resolver: ApplicationType by Name, Urgency by Code, VisaPeriod by LocalizationKey, BorderZoneLocation first non-Ýok label from comma list.
- **OData full (2026-06-29)**: 12,120 posted + 9 resume-skipped, 0 failed, 108 transform-skipped; ~7 min; id-map 12,129 entries. Unblocks ApplicationProgress + ApplicationItem OData.


- **Phase**: mapping + data fix
- **Symptom / surprise**: Visa2026 Person.FullName (`FirstName MiddleName LastName`) showed job titles in the middle
  (e.g. "Abdullah PROJECT MANAGER BAYSAL"). Root cause: legacy `dbo.Person.MiddleName` was used to store the
  employee's free-text **actual/company position** — VISA2014 had no dedicated field. Person.yaml mapped it 1:1 to
  Visa2026 Person.MiddleName.
- **User decisions**: (1) target = **current/latest** position-history row only (EndDate null / max StartDate);
  (2) scope = **employees only** (IsEmployee=true) — leave family members' MiddleName untouched;
  (3) employee with MiddleName but **no** EmployeePositionHistory row → **keep** MiddleName, report (nothing to attach).
- **Fix / mapping change**:
  - `Visa2014PersonTransform`: stop exporting MiddleName → Person.MiddleName; keep `_legacy_MiddleName` audit column.
  - `Visa2014PersonODataImporter`: removed MiddleName from POST payload.
  - `Visa2014EmployeePositionHistoryTransform`: extract `p.MiddleName`; on the current/latest row per person set
    ActualPosition = trim(MiddleName) when non-empty; else fall back to trim(Position.Code) or "-".
  - field-maps: Person.yaml MiddleName → propertyGaps.legacyOnly `relocate` → EmployeePositionHistory.ActualPosition;
    EmployeePositionHistory.yaml ActualPosition source updated.
- **Existing-data cleanup (already imported)**: new CLI `--cleanup-visa2014-person-middlename`
  (`Visa2014PersonMiddleNameToActualPositionCleanup`) — OData only. For each employee with MiddleName: find current
  EmployeePositionHistory, resolve/create ActualPosition by Name, PATCH it, then PATCH Person MiddleName="".
  `--dry-run` supported. PATCH clears MiddleName with **""** (JsonOptions ignores nulls → null would be omitted).
- **Prevent**: legacy "MiddleName"/name-ish columns may be repurposed free-text — verify sample values before 1:1 name mapping.
- **Artifacts**: field-maps/Person.yaml, field-maps/EmployeePositionHistory.yaml, Visa2014PersonTransform.cs,
  Visa2014PersonODataImporter.cs, Visa2014EmployeePositionHistoryTransform.cs,
  Visa2014PersonMiddleNameToActualPositionCleanup.cs, Program.cs

### 2026-06-29 — ApplicationItem Excel preview export

- **Phase**: excel-preview export
- **Export**: `ApplicationItem-preview.calik-energi.xlsx` — **21,794** legacy → **21,588** import / **206** skipped (204 parent E:44/E:55, 2 dedupe_duplicate); 925 dedupe groups from discovery dossier — only **2** groups on current VISA2015 attach.
- **SQL fixes**: bracket `dbo.[CheckPoint]` (reserved keyword); `OUTER APPLY TOP 1` for NextVisa (`Visa.ProcessNumber` has duplicate groups — naive JOIN inflated row count to 24,392).
- **Transform**: parent ApplicationType composite skip via `IsSkippedApplicationTypeComposite`; Person by ForEmployee/ForFamilyMember; (Application+Person) dedupe canonical lowest Oid → `_Skipped` `dedupe_duplicate`; WorkPermittedLocations null + `_audit_WorkPermittedLocations=pending_work_permit_location_audit`.
- **Code**: `Visa2014ApplicationItemTransform.cs`, `Visa2014ApplicationItemPreviewExporter.cs`, `Visa2014PreviewExportCommand.cs`, `Program.cs` help.
- **Next**: preview review → `importConfirmed`; OData after Application + Person + Passport + Visa id-maps.

### 2026-06-29 — ApplicationProgress seed suppression

- **Symptom**: Application OData POST auto-created `IS_BEING_PREPARED` @ `AT_OFFICE` progress rows via `OnCreated` → duplicate with synthetic ApplicationProgress import.
- **Fix**: `Application.SuppressInitialProgress` (hidden); `Visa2014ApplicationODataImporter` POST `SuppressInitialProgress=true`; `--cleanup-visa2014-application-progress-seeds` DELETEs initializer rows on Application id-map apps; `Visa2014ApplicationProgressODataImporter` removes seeds before POST + posts synthesized history.
- **Artifacts**: Application.cs, ApplicationProgressInitializer.cs, Visa2014ApplicationProgressSeedHelper.cs, Visa2014ApplicationProgressSeedCleanup.cs, Visa2014ApplicationProgressODataImporter.cs, Visa2014ODataLookupResolver (ApplicationState/Location by Code).

### 2026-06-30 — ApplicationMigrationServiceInference — excel preview

- **Phase**: excel-preview
- **Export path**: `preview-export/ApplicationMigrationServiceInference-preview.calik-energi.xlsx`
- **Scope**: `App_Reg_Check_In` (`E:2` / `F:2`) with null `DepartmentForRegistration` only — **58** legacy apps
- **Counts**: **58** total — confidence **high 7**, **medium 44**, **low 0**, **none 7** (no address / null region / DZ gap)
- **Artifacts**: `migration-service-inference.yaml`, `MigrationService-inference.md`, `Visa2014ApplicationMigrationServiceInferencePreview.cs`, `Visa2014MigrationServiceInferenceRules.cs`
- **Ready for PATCH**: **no** — `approvedForPatch: false`; review Excel first

### 2026-06-30 — ApplicationItem — OData importer

- **Phase**: import
- **Environment**: Visa2026DbDev (local OData https://localhost:5001)
- **Code**: `Visa2014ApplicationItemODataImporter.cs`, `Visa2014ODataLookupResolver.ResolveCheckPoint`, `Visa2014ImportCommand` ApplicationItem wave.
- **Transform**: reuses `Visa2014ApplicationItemTransform.PrepareImportBatch` (21,588 prepared / 206 skipped from preview).
- **POST rules**: required Application+Person+CurrentPassport id-maps; optional FKs allow_null on miss; PurposeOfTravel omitted; BorderZoneLocation string; nested BusinessTripAddress when city+address; CheckPoint OData NameTm.
- **order.yaml**: `importConfirmed: true` (developer, 2026-06-30).

### 2026-06-30 — ApplicationProgress — OData live import (calik-energi)

- **Phase**: import (live, not dry-run)
- **Environment**: https://localhost:5001 (HTTP 302), VISA2015 read-only via `VISA2014_SQL_PASSWORD` (User env; must set from User scope in Agent shells).
- **Built-in seed cleanup**: 8135 initializer rows removed before progress POST phase (do not run standalone seed cleanup CLI separately).
- **Counts**: prepared 32177; parent-skipped 108; posted **0**; failed **0**; skipped (already imported) **32177**; legacy applications 12237.
- **Id-map**: `Visa2026.DataImporter/legacy/visa2014/id-maps/calik-energi/ApplicationProgress.json` — **32177** entries.
- **Note**: Idempotent re-run — all rows already present from prior load; seed cleanup still ran on this pass.
- **order.yaml**: `importStatus: complete` with counts in notes.

### 2026-06-21 — On-prem IIS migration runbook + parallel period

- **Decision**: Officers **view/search only** in Visa2026 until cutover; legacy `VISA2015` on `10.100.128.15` remains system of record.
- **Hosts**: Visa2026 IIS `10.100.128.25` (Prod :80, Staging :8080, Demo :8081); legacy SQL `10.100.128.15`.
- **Sync**: One-way legacy → Visa2026 planned (nightly off-peak); safe because no officer writes in Visa2026 during parallel period. Full delta upsert (`--sync-visa2014`) not implemented yet — v1 catch-up is new-row id-map skip on some entities only.
- **Artifacts**: `docs/VISA2014_MIGRATION/ON_PREM_IIS_MIGRATION_RUNBOOK.md`, `import-strategy.yaml` `onPremDeployment`, `legacy-sources.yaml` profiles `calik-energi-onprem-{staging,prod,demo}`.

### 2026-06-30 — Headless XAF import (`--inprocess`)

- **Phase**: import implementation
- **Goal**: Speed Application / ApplicationItem loads by skipping OData HTTP per row while keeping XAF validation (`MigrationImportContext`, same rules as UI).
- **Architecture**: `HeadlessMigrationHost` in `Visa2026.Blazor.Server` boots `Program.CreateHostBuilder` without Kestrel; `Visa2014ObjectSpaceImportTarget` + `ObjectSpaceImportSink` apply payload dicts via `INonSecuredObjectSpaceFactory`; batch commit default 50.
- **CLI**: `--import-visa2014 --inprocess --entity Application|ApplicationItem --target-connection ...` optional `--batch-size`. Other entities remain OData-only for now.
- **Verified**: Debug build OK; dry-run Application 5 rows; live in-process 1 row on LocalDB `Visa2026` — headless host started, lookup catalogs loaded from ObjectSpace, idempotent skip (already imported).
- **Docs**: `import-practices.md` § Headless in-process; `ON_PREM_IIS_MIGRATION_RUNBOOK.md` staging example.
- **Next**: Benchmark full ApplicationItem (~21k) vs OData; extend `--inprocess` to remaining entities if needed.

### 2026-07-02 — Full clean re-migration via headless XAF host (all entities in-process)

- **Phase**: import (live, LocalDB `Visa2026`, `--inprocess` for the whole chain).
- **Goal**: User asked "all data should be imported using headless xaf host" — a full clean re-migration, not just Application/ApplicationItem.
- **Refactor**: All 8 person-domain + progress importers now write through `IVisa2014ImportTarget` (OData or ObjectSpace) + `Visa2014ODataLookupResolver`; command dispatch unified (no more per-entity in-process allow-list). `EmployeePositionHistory` calls `target.FlushAsync()` right after creating a new `ActualPosition` so the dependent row can reference it.
- **GCRecord gotcha**: Active rows in this schema are `GCRecord = 0`, **not** `NULL`. Verifying seed with `WHERE GCRecord IS NULL` falsely reports "0 active / all soft-deleted". Use `GCRecord = 0`.
- **Bug 1 — resolver subset**: `LoadFromObjectSpace` (in-process) only loaded the Application/ApplicationItem lookups. Passport failed 3585/3585 (`BuildPayload` null: no PassportType/Country) and Person silently dropped Gender/Country/Nationality/MaritalStatus FKs. Fix: mirror `LoadAsync` fully — load every lookup (Gender, Country, MaritalStatus, Relationship, PassportType, VisaType, VisaIssuedPlace, Subcontractor, Education*, Specialty, Position, Department, Region, ApplicationState/Location) plus custom maps for `BaseObject` types (`ActualPosition`, `Lodging`, `Hotel`, `Hospital`, `OtherSite` — City nav → CityId).
- **Bug 2 — GetId missing case**: `Visa2014ODataLookupResolver.GetId<T>` switch had no `ApprovalLegProfile` arm → `ResolveApprovalLegProfile` threw "Unsupported lookup type ApprovalLegProfile" for every application with an approval-leg code (5757 failed, both paths affected). Fix: add `ApprovalLegProfile alp => alp.Id`.
- **Re-run safety**: Person and Passport importers do **not** skip already-imported rows (re-running duplicates) → delete `dbo.People` + clear id-maps for a true clean start. All downstream importers (Visa, Education, EmployeePositionHistory, EmployeeSalary, AddressOfResidence, Application, ApplicationItem, ApplicationProgress) skip via their id-map, so a failed Application run can be re-run to retry only the failed rows.
- **sqlcmd**: `DELETE` against tables with filtered indexes needs `-I` (QUOTED_IDENTIFIER ON) or it errors 1934.
- **Final verified counts (calik-energi)**: People 2967 (Gender+Nationality 2967/2967), Passports 3573, Visas 5965, Educations 3101, EmployeePositionHistories 2993 (+1368 ActualPositions), EmployeeSalaries 2887, AddressesOfResidence 3968, Applications 12129 (ApprovalLegProfile 5757, ProjectContract 5750), ApplicationItems 21345, ApplicationProgresses 32177. 0 hard failures on final runs (only benign per-row data skips).
- **Orchestration**: `scripts/visa2014-migration/import/Run-HeadlessChain.ps1` runs the dependency-ordered chain in-process; tolerates minority row failures, hard-fails only on 0-posted/non-empty batches; `-StartAt <Entity>` to resume.

### 2026-07-02 — File/image waves on headless XAF (no OData)

- **Phase**: import implementation + docs
- **Goal**: User policy — all migration writes (scalar + file copies: photo, passport/visa/diploma scans, spid kepilnama) via headless ObjectSpace, not OData.
- **Code**: `IVisa2014ImportTarget.UpdateAsync`; file importers refactored to `Visa2014DocumentImportPayload.WithNestedFile` (aggregated `FileData` on `*Document`); `--import-visa2014-files` **requires** `--inprocess` (errors without it).
- **CLI examples**: `--import-visa2014-files --inprocess --entity Person --property Photo --target-connection ...`
- **Orchestration**: `Run-HeadlessChain.ps1` extended with file steps after each parent scalar BO; `OnPrem-Staging.ps1` headless-only for all entities.
- **Planned**: `FamilyProofDocument` → `PersonDocument` / `PersonFamilyRelationDocument` on same headless path (not implemented yet).
- **OData**: deprecated for migration writes; scalar `--import-visa2014` without `--inprocess` prints `WRN OData write path is deprecated`.

### 2026-07-03 — Person — PersonalNumber placeholder dedupe fix + partial reimport (calik-energi)

- **Phase**: import
- **Mode**: partial-reimport (person-domain wipe + full chain from Person)
- **Outcome**: Person success; downstream chain running
- **Environment**: local — `(localdb)\mssqllocaldb` / `Visa2026`
- **Mapping fix**: `---`, `...`, `----`, etc. normalize to `0`; sentinel dedupe on FirstName+LastName+DateOfBirth; `Person_IdentityUniqueWhenPersonalNumberIsSentinel` save rule
- **Cleanup**: `scripts/visa2014-migration/cleanup/ImportedPersonDomain.sql` (People + person-domain + manual Applications)
- **Script**: `scripts/visa2014-migration/reimport/Person.ps1` (new); `Run-HeadlessChain.ps1` fails on PS 5.1 (`??`) — ran Person via `dotnet exec` + downstream loop
- **Dry-run**: legacy 3241 → prepared **3222**, skipped 0, dedupe merged **19** (was 274)
- **Person import**: posted **3222**, failed **0**; id-map 3241 entries (+19 dedupe aliases)

### 2026-07-03 — Passport — PRT/ARE country gap + 12-row reimport (calik-energi)

- **Phase**: import (partial Passport resume)
- **Outcome**: success — **12 posted**, **0 failed**; Passports **3585** total (was 3573)
- **Root cause**: legacy `IssuedCountry` **PRT** (10) and **ARE** (2) missing from Visa2026 `Country` catalog (`UAE` exists; ISO **ARE** did not)
- **Fix**: `country.json` + `CountryLookupStrings.json` — added **PRT** (Portugaliýa) and **ARE** (same label as UAE); `manifest.json` version **8**
- **Importer**: `Visa2014PassportODataImporter` now skips rows already in Passport id-map and merges id-map on resume (like Visa)
- **DB note**: headless import did not run `LookupCatalogSyncUpdater` (stored `LookupCatalogManifestVersion` 37 ≫ manifest 8) — inserted PRT/ARE via SQL on LocalDB before reimport; greenfield/deploy should get rows from JSON sync after manifest bump or app restart
- **Reconciliation**: People **3222** (employees **2935**, family **287**); legacy employees 2950 → gap **15** (real-PN duplicate pairs only; was 2686)
- **Log**: `artifacts/headless-import/Person-reimport.log`, `downstream-reimport.log`
- **Follow-up**: restart Blazor for new Person validation rule; monitor downstream chain completion

### 2026-07-03 — Visa — resume import after Passport reimport (calik-energi)

- **Phase**: import (resume — no Visa cleanup; id-map skip for 5965 existing)
- **Outcome**: success — **10 posted**, **0 failed**; Visas **5975** total (was 5965); id-map **5975**
- **CLI**: `--import-visa2014 --entity Visa --legacy-source calik-energi --inprocess --no-build --target-connection (localdb)/Visa2026`
- **Dry-run**: 6016 prepared, 19 transform skip, 6 dedupe merged, **41** missing Passport id-map
- **Live**: skipped already-imported **5965**, skipped no Passport map **41**, posted **10**
- **Reconciliation**: legacy active **6041** → **66** still unmigrated (**41** passport id-map gap + **25** transform/dedupe skips)
- **Log**: `Visa2026.DataImporter/bin/Debug/net8.0/import_20260703_101659.log`
- **Next**: fix remaining **41** Passport id-map gaps (legacy passports not imported or dedupe aliases); optional `--import-visa2014-files` VisaDocument wave for new rows

### 2026-07-03 — Person-domain downstream partial reimport (calik-energi)

- **Phase**: partial-reimport (children only after Person reimport; keeps People + Person.json)
- **Outcome**: success — all 6 entities **0 failed**
- **Script**: `scripts/visa2014-migration/reimport/PersonDomainDownstream.ps1` + `cleanup/ImportedPersonDomainChildren.sql`
- **Cleanup**: cleared ApplicationItem person-current FKs (CurrentVisa/Passport/Education/Salary/PositionHistory/Address) before delete; did **not** delete Applications
- **Posted**: Passport **3585**, Visa **5975** (41 no Passport map), Education **3108**, EmployeePositionHistory **2993** (+1368 ActualPositions), EmployeeSalary **2887**, AddressOfResidence **5054** (includes **1086** PIA-inferred)
- **People unchanged**: **3241**
- **Follow-up**: re-run `--correct-application-item-person-current` and/or `reimport/ApplicationItems.ps1` to repopulate ApplicationItem person-current fields; optional file waves (PassportCopy, VisaDocument, EducationDocument)

### 2026-07-03 — ApplicationProgress synthesis: PROCESS_STARTED before PROCESS_ISSUED

- **Phase**: mapping fix (transform only; DB reimport pending)
- **Symptom**: Long-process apps (e.g. manual **3717**) jumped from ministry approval straight to **Issued - Migration service** — missing **In processing - Migration service**.
- **Root cause**: `Visa2014ApplicationProgressTransform.SynthesizeSteps` emitted single `migration_process` row (`PROCESS_ISSUED` @ `AT_MIGRATION_SERVICE`) with no preceding `PROCESS_STARTED` @ `AT_MIGRATION_SERVICE`.
- **Fix**: Split migration leg into `migration_started` (`PROCESS_STARTED`) + `migration_issued` (`PROCESS_ISSUED`); issued only when `ProcessDate`/`ProcessNumber` set; started also when ministry route complete without process date (no bogus issued row).
- **Tests**: `Visa2014ApplicationProgressTransformTests.cs` (xunit in DataImporter project).
- **Next**: dev partial reimport — delete imported `ApplicationProgress` rows + `--import-visa2014 --entity ApplicationProgress --inprocess` (~+12k rows for apps with process date).

### 2026-07-03 — ApplicationProgress dev reimport (calik-energi)

- **Phase**: partial-reimport (`reimport/ApplicationProgress.ps1` + `cleanup/ImportedApplicationProgress.sql`)
- **Cleanup**: deleted **39,742** progress rows; cleared `ApplicationProgress.json` id-map
- **Import**: **51,681** posted, **0** failed (~**+11,939** vs pre-fix); **11,979** `PROCESS_STARTED` @ `AT_MIGRATION_SERVICE`
- **Build note**: stop `Visa2026.Blazor.Server` if MSB3027 file-lock on `dotnet build`
- **Log**: `import_20260703_112131.log`
- **Edge case**: when legacy `ProcessDate` is before interpolated last ministry date, date sort can persist **Issued** before **In processing** — rare; follow-up clamp in transform if seen in UI

### 2026-07-03 — ApplicationProgress ministry-leg correction (profile-based leg count)

- **Phase**: patch (`patch/ApplicationProgress-MinistryLegs.ps1` + `--correct-application-progress-ministry-legs`)
- **Root cause**: `Visa2014ApplicationMinistryLegCountResolver` counted only empty snapshots; fallback `IsLongProcess` missed ~5.9k ViaMinistries apps with `ApprovalLegProfile` (legacy simple process).
- **Fix**: Resolver falls back to `ApprovalLegProfile.MinistryLegs` count; correction scopes apps missing `*_REVIEW_*` progress, backfills snapshots, prunes id-map prefixes, regenerates progress.
- **Outcome**: **3019** apps in scope; **0** ViaMinistries 2-leg profile apps still missing review steps; **6038** approval-leg snapshots on manual-entry apps.
- **Verify**: `7/-8308` now has `1_REVIEW_*` / `2_REVIEW_*` rows.
- **Follow-up**: step date ordering when `ProcessDate` precedes interpolated ministry slots.


- **Artifact**: `scripts/visa2014-migration/Compare-LegacyMigratedCounts.ps1` — legacy `VISA2015` vs LocalDB `Visa2026` BO row counts (`Legacy`, `Migrated`, `Gap`; `-ShowIdMap` optional)
- **Verified**: Person 3241/3241; Passport 3666/3585; Visa 6041/5975; Education 3133/3108; EPH 2993/2993; Salary 2950/2887; Address 4083/5054; Application 12237/12129; ApplicationItem 21794/21345; ApplicationProgress legacy apps 12237 vs 39742 progress rows

### 2026-07-03 — ApplicationItem CurrentAddressOfResidence correction (calik-energi)

- **Phase**: mapping fix + id-map rebuild + `--correct-person-address-of-residence` (live)
- **Symptom**: **12,252** imported ApplicationItems had Person/Passport/Visa/Position filled but **CurrentAddressOfResidence** empty; e.g. app **909** Milos Krcevinac (lodging) and Tanveer Alam (hotel).
- **Root cause**: `AddressOfResidence.json` id-map rebuild matched **PrivateHouse** only (`FullAddress` + `ExpirationDate`); legacy PIA lines mostly reference **Lodging/Hotel** via `pia.AddressOfResidence` FK — **3,290** legacy AOR OIDs skipped on rebuild. Importer omits FK when id-map miss (`TryAddOptionalFkFromMap`).
- **Fix**: `Visa2014AddressOfResidenceTargetMatcher` (all `ResidenceType` + site joins; `Type` stored as **int** enum in SQL); `Visa2014AddressOfResidenceIdMapAliasAppender` (PIA synthetic keys, direct `Address` OID aliases, sponsor canonical); `Visa2014ApplicationItemLegacyAddressResolver` creates missing `AddressOfResidence` on correct Person when legacy AOR exists but target row/id-map miss.
- **Id-map rebuild**: **3361** matched (+**1812** aliases appended); **607** skipped.
- **Correction live**: ApplicationItems in scope **21,394**; updated **12,248**; unchanged **9,142**; unresolved **4** (lookup gaps / unmappable legacy rows: apps **8424**, **3327**, **8780**, **7531**).
- **Verify SQL**: `CurrentAddressOfResidenceID` populated **21,390 / 21,394**; app **909** Milos + Tanveer both have lodging/hotel address.
- **Code**: `Visa2014TargetIdMapRebuild.cs`, new `Visa2014AddressOfResidence*.cs` helpers under `Visa2026.DataImporter/legacy/visa2014/`.
- **Next**: triage remaining **4** rows manually if UI-visible; consider dedupe-key lodging match in rebuild for near-miss catalog strings.


### 2026-07-03 — WorkPermit + WorkPermitItem — Çalik pilot (calik-energi)

- **Phase**: excel-preview + pilot import (LocalDB Visa2026)
- **Excel preview**: WorkPermit **401** import rows (399 letters + 2 orphan headers); WorkPermitItem **6363** rows, **0** skipped, **0** unmapped lookups
- **Catalog**: `work-permitted-location-name.json` expanded **8 → 30** distinct `NameTm` from preview (bit-matrix heuristic; fixed Şäheri suffix handling)
- **Pilot import**: WorkPermit **401/401** posted; WorkPermitItem **3750/6363** posted, **2613** skipped (EmployeePositionHistory not in id-map — legacy `WorkPermit.Position` = `WorkHistoryOfEmployee.Oid` but EPH import gap), **0** failed
- **Id-maps**: `id-maps/calik-energi/WorkPermit.json`, `WorkPermitItem.json`
- **Script**: `scripts/visa2014-migration/import/WorkPermits.ps1` (headers then items; builds DataImporter only — avoid Blazor F5 lock)
- **Order**: `order.yaml` — Application → WorkPermit → WorkPermitItem → ApplicationItem
- **Ready for importConfirmed**: no (human review Excel + 2613 EPH gap triage)
- **Next**: Invitation/InvitationItem discovery; ApplicationItem reimport with `--work-permit-item-id-map`; triage 2613 rows (reimport EPH subset or accept gap)
### 2026-07-03 — ApplicationItem reimport after WorkPermitItem wave (calik-energi)

- **Phase**: partial-reimport (`reimport/ApplicationItems.ps1` + `--work-permit-item-id-map`)
- **Script**: added `--work-permit-item-id-map` to `reimport/ApplicationItems.ps1` (was only on `import/ApplicationItems.ps1`)
- **Id-map rebuild**: WorkPermitItem **3750** matched, **2613** skipped (stale Position / no EPH)
- **Import**: **21,394** ApplicationItem posted, **0** failed
- **Verify (Visa2026)**: `CurrentWorkPermitItemID` populated **3,446** rows; legacy PIA with `WorkPermit` FK **3,802** → **356** gap (permit row not in WorkPermitItem id-map)
- **WorkPermittedLocations** on ApplicationItem: **34** (gated by `ApplicationType.ShowWorkPermittedLocations`; most types use item-level copy only when flag true)
- **Corrections**: PIA address + person-current ran after import (same script chain)
- **Next**: Invitation wave; optional WorkPermit.Application backfill; triage 2613 permit items / 356 application-line gaps

### 2026-07-03 — WorkPermitItem position FK fix (supplement EPH + fallback)

- **Root cause (2613 skips)**: legacy `WorkPermit.Position` → `WorkHistoryOfEmployee.Oid`; **2582** point at soft-deleted WH; **2523** also have soft-deleted employee (not recoverable). **~59** active employee + soft-deleted WH — fixable.
- **Fix shipped**:
  - `Visa2014EmployeePositionHistoryTransform.SupplementPermitReferencedExtractSql` + `--supplement-permit-positions` on EmployeePositionHistory import (appends soft-deleted WH referenced by active WorkPermit; active person only; `EndDate` null snapshot).
  - `Visa2014WorkPermitItemPositionResolver` — when position OID missing from id-map, pick nearest active WH for employee at permit `StartDate` (legacy GetLastPosition-style).
  - Patch script: `scripts/visa2014-migration/patch/WorkPermitItem-SupplementPositions.ps1` (supplement EPH then WorkPermitItem re-import; skips already in id-map).
- **Tests**: `Visa2014WorkPermitItemPositionResolverTests` (fallback date pick).
- **Next**: run patch on calik-energi pilot; re-run `reimport/ApplicationItems.ps1` if new WorkPermitItem rows posted; expect ~59–90 recovered items not full 2613.

### 2026-07-03 — WorkPermitItem supplement patch run (calik-energi pilot)

- **Patch**: `WorkPermitItem-SupplementPositions.ps1` on LocalDB Visa2026 + SQLEXPRESS VISA2015
- **Supplement EPH** (`--supplement-permit-positions`): **0** legacy rows (no soft-deleted WH with active person on this legacy DB for calik-energi)
- **WorkPermitItem re-import**: **47** posted via **position fallback**; **3750** already imported; **2566** still skipped (missing id-map); **0** failed
- **WorkPermitItem total**: **3797** in target + id-map rebuild match
- **ApplicationItem reimport**: **21,394** posted; id-map rebuild WorkPermitItem **3797** matched, **2566** skipped
- **Verify (Visa2026)**: `CurrentWorkPermitItemID` **3496** (was **3446**, +**50**); legacy PIA with `WorkPermit` FK **3725** on this SQL instance
- **Next**: triage remaining **2566** permit rows (mostly soft-deleted employee); Invitation wave

### 2026-07-03 — Invitation + InvitationItem wave (calik-energi pilot)

- **Legacy mapping**: header `ApplicationResult` (via `PersonInInvitation.Invitation` FK); item `PersonInInvitation` 1:1
- **Preview**: Invitation **2776** import / **185** skipped (missing dates/number); InvitationItem **5239** / **0** skipped
- **Pilot import** (LocalDB Visa2026): Invitation **2776/2776** posted; InvitationItem **4955/5239** posted, **284** skipped (missing Person/Passport/Invitation id-map), **0** failed
- **Target counts**: Invitations **2776**, InvitationItems **4955**
- **Code**: transforms/importers/preview + `scripts/visa2014-migration/import/Invitations.ps1`; `order.yaml` updated
- **Next**: triage 284 item skips; wire `CurrentInvitationItem` on ApplicationItem reimport; human `importConfirmed` after Excel review

### 2026-07-03 — ApplicationItem CurrentInvitationItem wiring (calik-energi)

- **Resolver**: SQL `OUTER APPLY` joins `PersonInApplication` → `PersonInInvitation` via `ApplicationResult.Application` + same Employee/FamilyMember; tie-break `ApplicationResult.IssuedDate DESC` (18 multi-match rows).
- **CLI**: `--invitation-item-id-map` on ApplicationItem import + `import/` / `reimport/ApplicationItems.ps1`.
- **Reimport**: **21,394** posted; `CurrentInvitationItemID` **4929** (was **0**); `CurrentWorkPermitItemID` **3496** (unchanged).
- **Gap**: legacy PIA with invitation match **5213** → **284** not in InvitationItem id-map (same skip set as invitation import).

### 2026-07-03 — Application id-map: FullApplicationNumber + ApplicationDate (no merge)

- **Root cause (`6/-909`)**: legacy has **two** active `Application` rows with same `ManualApplicationNumber` but different dates (2025-07-26 Tanveer, 2026-06-26 Milos). `--rebuild-visa2014-id-maps` matched **FullApplicationNumber only** → both legacy Oids mapped to one Visa2026 `Application` → ApplicationItems from both headers collapsed onto one parent.
- **Rule**: business identity = **FullApplicationNumber + ApplicationDate**; id-map upsert key = **legacy Application.Oid (GUID)**; ApplicationItem parent = legacy **Application** Oid via id-map (never number alone).
- **Fix shipped**: `Visa2014ApplicationTransform` identity helpers + rebuild SQL `FullApplicationNumber` AND `CAST(ApplicationDate AS date)`; collision guard on rebuild + ApplicationItem import abort; dedupe metadata keyed by number+date; tests in `Visa2014WorkPermitItemPositionResolverTests`.
- **Pilot repair**: rebuild `Application.json` id-map; reparent mis-linked ApplicationItems (e.g. Tanveer `AC7D8DDA…` → 2025-07-26 app `34AFE059…`).

### 2026-07-03 — Application id-map rebuild + ApplicationItem reparent (calik-energi pilot)

- **Backup**: `id-maps/calik-energi/Application.json.bak-collapsed` (pre-rebuild, number-only collapse).
- **Rebuild** (`--rebuild-visa2014-id-maps`, LocalDB Visa2026): Application **12129** matched, **0** skipped; cross-date collisions **0** after fix. Example `6/-909`: `f5616776…` → `C022D8D4…` (2026-06-26 Milos), `f538cb62…` → `34AFE059…` (2025-07-26 Tanveer).
- **Reparent** (`--correct-application-item-application-parent`): **21394** in scope, **1594** reparented, **19800** already correct, **0** errors.
- **Verify `6/-909`**: Tanveer item on `34AFE059…` (2025-07-26); Milos item on `C022D8D4…` (2026-06-26) — no longer merged.
- **CLI shipped**: `Visa2014ApplicationItemApplicationParentCorrection.cs` + `--correct-application-item-application-parent` in `Program.cs`; aborts if Application id-map still has cross-date collisions.

### 2026-07-03 — ApplicationItem CurrentWorkPermitItem person fallback

- **Rule**: when legacy `PersonInApplication.WorkPermit` is null but type has `ShowCurrentWorkPermitItem`, use latest `dbo.WorkPermit` per employee (`StartDateOfWorkPermit DESC`, `Oid DESC`) — same as `PersonCurrentItems.GetCurrentWorkPermitItem`.
- **Transform**: `Visa2014PersonCurrentFieldInference.BuildCurrentWorkPermitByPerson` + `TrySetApplicationItemPersonCurrentFields` (does not override explicit PIA FK).
- **Pilot correction** (`--correct-application-item-person-current`, LocalDB): **2057** `CurrentWorkPermitItem` backfilled (+ `WorkPermittedLocations` from item); **3496 → 5553** with permit FK (post-run count).
- **Note**: `6/-909` Milos/Tanveer still empty — no legacy `WorkPermit` rows for either person in VISA2015 (correct).

### 2026-07-03 — ApplicationType composite: SubType enum vs TypeOfApplication*ID

- **Root cause (`2/-291` Ismet Danış)**: importer used `TypeOfApplicationForEmployeeID` (internal seed ID) but legacy UI displays `TypeOfApplicationForEmployee` (`SubType` enum). Example: enum **9** = “Wizany täze pasporta geçirmek” (`App_Change_Passport`) but ID **10** was mapped to `App_Sevice_Passport`. ~6990 employee apps have enum ≠ ID.
- **Fix**: transforms (`Application`, `ApplicationItem`, `ApplicationProgress`) now read enum columns; skip composites `E:33` / `E:55`; added `E:21`/`E:22`/`F:21` mappings for cancel visa/WP enum values.
- **Pilot correction** (`--correct-application-type-composite`): **7933** retyped, **4136** already correct, **60** skipped. `2/-291` → `App_Change_Passport` (705).

### 2026-07-03 — Application id-map identity: number+date+ApplicationType (twin legacy apps)

- **Residual 129 type mismatches** after composite retype were **not** enum/ID bugs — **144 id-map entries** pointed multiple legacy `Application.Oid` values at one Visa2026 row (employee+family twins share `ManualApplicationNumber`+date; also same-type twins).
- **Identity key** extended to `FullApplicationNumber+ApplicationDate+ApplicationType` (`Visa2014ApplicationTransform.ApplicationImportIdentity`, target SQL joins `ApplicationTypes`).
- **Rebuild** (`Visa2014ApplicationIdMapRebuild`): greedy one-to-one target assignment + `ApplicationItem` parent overlap disambiguation; merge preserved prior id-map entries only when target slot still free (`MergePreservedApplicationIdMapEntries`).
- **Pilot** (calik-energi, LocalDB): id-map **11993** entries, **0** duplicate targets; `--correct-application-type-composite --dry-run` → **0** retypes, **11934** already correct, **59** skipped (unmapped composites).
- **Gap**: **~136** legacy apps dropped from id-map (`no target` / twin slot taken) — no matching `Applications` row for resolved type in Visa2026 (e.g. family `9/-3876` `App_Visa_Ext_FM` never imported). Needs missing Application OData import, not retype alone.

### 2026-07-04 — Full application-domain partial reimport (calik-energi)

- **Phase**: partial-reimport (dev chain after ApplicationType / id-map / progress fixes)
- **Environment**: LocalDB `Visa2026` + SQLEXPRESS `VISA2015`
- **Scripts** (in order): `reimport/Applications.ps1` → `import/WorkPermits.ps1` → `import/Invitations.ps1` → `reimport/ApplicationItems.ps1` → `reimport/ApplicationProgress.ps1`
- **Outcome**: success
- **Counts (target)**:

  | BO | Posted / in DB |
  |----|----------------|
  | Application | 12,069 |
  | ApplicationItem | 21,306 |
  | WorkPermit | 401 |
  | WorkPermitItem | 3,797 |
  | Invitation | 2,776 |
  | InvitationItem | 4,955 |
  | ApplicationProgress | 54,267 |

- **Verify `8/-967`**: `App_Reg_Check_Out` (direct migration) — **1** progress step (`IS_BEING_PREPARED` @ `AT_OFFICE`), **2** items, **0** direct-ministry review rows
- **Gotchas**:
  - `ImportedApplications.sql` matched `GCRecord IS NULL` only → deleted **0** rows while **36k** manual apps remained (`GCRecord = 0`) — fixed cleanup to `(GCRecord IS NULL OR GCRecord = 0)` + NULL ApplicationItem permit/invitation FKs before child delete
  - `Applications.ps1` pointed at wrong cleanup path (`reimport/ImportedApplications.sql`) — fixed to `../cleanup/ImportedApplications.sql`
  - After Application wipe, WorkPermit/Invitation imports posted **0** (“already imported”) until orphan BO rows + id-maps purged
  - WorkPermit `ApplicationID` still **null** on all headers — expected pilot (letter-synthesized headers; Application FK backfill deferred)
- **Officer sign-off**: legacy subtypes **E:33** (92) and **E:55** (13) confirmed **no migration** — remain `skip_row` in `lookup-translations.yaml`
- **Prevent**: After `Applications.ps1`, always run WorkPermit → Invitation → ApplicationItem → ApplicationProgress; document in [import-practices.md § Full application domain](./import-practices.md). Do **not** run `--correct-application-progress-ministry-legs` after direct-migration progress reimport.
- **Artifacts**: `cleanup/ImportedApplications.sql`, `reimport/Applications.ps1`, import-practices + scripts README + SKILL troubleshooting

### 2026-07-04 — Document copies wave (calik-energi, LocalDB)

- **Phase**: file-import
- **Environment**: LocalDB `Visa2026` + SQLEXPRESS `VISA2015` (`ReadOnlyUser` + `VISA2014_SQL_PASSWORD`)
- **Script**: `scripts/visa2014-migration/import/DocumentCopies.ps1` (also wired in `Run-HeadlessChain.ps1`)
- **Outcome**: success (FamilyProof required one fix — see below)
- **Counts (target after wave)**:

  | Wave | Posted / in DB | Skips (run log) |
  |------|----------------|-----------------|
  | Person.Photo | 3,170 | 71 no blob |
  | PassportDocument | 3,567 | 35 oversize, 12 no passport map, 1 no blob |
  | VisaDocument | 5,775 | 154 oversize, 66 no visa map, 46 no blob |
  | EducationDocument | 4,222 | 40 oversize, 16 duplicate blob, 10 no education map, 29 no blob |
  | WorkPermitDocument | 1,000 | 2 no blob, 5 already imported (pilot) |
  | InvitationDocument | 2,875 | 203 no parent map, 4 no blob |
  | FamilyProofDocument | 9 `PersonDocument` + 437 `PersonFamilyRelationDocument` | 1 oversize (>5MB), 3 duplicate blob |

- **Legacy mapping**:
  - Passport / Education / WorkPermit / Invitation scans → `dbo.PassportCopy` (implicit FK columns for WorkPermit letter + ApplicationResult)
  - Visa scan → inline `Visa.GöçürmeNusga`
  - Photo → `Person.Photo` varbinary
  - Family proof → `FamilyProofDocument.CopyOfDocument` **inline varbinary(max)** (not `FileData` FK)
- **FamilyProof fix**: initial SQL `CAST(CopyOfDocument AS varchar(36))` + `FileData` join caused SQL error 8152 — read blob per row from `CopyOfDocument` instead (`Visa2014FamilyProofDocumentImporter`)
- **New importers**: `Visa2014WorkPermitDocumentImporter`, `Visa2014InvitationDocumentImporter`, `Visa2014PassportCopyLinkedDocumentImporter`, `Visa2014FamilyProofDocumentImporter`, `Visa2014LegacyTableColumnResolver`
- **Gotcha**: Passport/Visa/Education re-run posted with `Already imported: 0` when id-map files were empty — target counts may exceed prior pilot if those maps were not saved earlier; id-maps now under `id-maps/calik-energi/*Document.json`
- **Gotcha**: `StrReplace` on `.cs` under DataImporter can save UTF-16 — convert to UTF-8 before `dotnet build` (bytes should start `117,115,105,110` = `using`)
- **Prevent**: Use `DocumentCopies.ps1 -StartAt FamilyProofDocument` only after fix; keep 5MB cap consistent with `DocumentBase` rules
- **Artifacts**: `DocumentCopies.ps1`, `Visa2014LegacyFileNameHelper` (work-permit / invitation / family-proof names)

## 2026-07-06 — ApplicationItem cancel flags fan-out + reimport (calik-energi)

- **Phase**: partial-reimport (`reimport/ApplicationItems.ps1` after `Visa2014ApplicationItemCancelledFlagsMapper` + `IsLineCancelled` on BO)
- **Script**: `scripts/visa2014-migration/reimport/ApplicationItems.ps1` (`-Configuration Debug`)
- **Outcome**: success
- **Import**: deleted 21,306 items; posted **21,306**; failed **0**; skipped missing id-map **194** (legacy 21,794 rows)
- **Cancel flags (target DB)**:

  | Column | Count |
  |--------|------:|
  | `IsCancelled` (WP) | 750 |
  | `InvitationItemIsCancelled` | 16 |
  | `VisaIsCancelled` | 3 |
  | Any line flag (OR) | **769** |
  | `RejectionIssued` | 9 |

- **Before fix**: all 769 cancelled lines landed on `IsCancelled` only; invitation/visa flags were 0
- **Mapper**: `Visa2014ApplicationItemCancelledFlagsMapper` — `App_Cancel_*` name heuristics + `Show*IsCancelled` catalog + fallback `IsCancelled`
- **UI**: computed `ApplicationItem.IsLineCancelled` (OR of type flags + `Application.IsCancelled`); column on nested + standalone ListViews
- **Gap vs legacy**: ~774 `PersonInApplication.Cancelled=1` in VISA2015 — 5-row delta expected from 194 skipped items / apps not in id-map
- **Post-corrections**: PersonAddressPia + ApplicationItemPersonCurrent ran; CurrentWorkPermitItem updated 71 on person-current pass
- **Prevent**: new UTF-16 on DataImporter `.cs` — write via PowerShell UTF-8 or verify bytes before build

### 2026-07-06 — P0 document `IsCancelled` backfill (Visa + WorkPermitItem transforms)

- **What**: `Visa2014LegacyDocumentCancellationIndex` loads `PersonInApplication` cancellation evidence and sets `IsCancelled` on **Visa** and **WorkPermitItem** import rows (not only `ApplicationItem` workflow flags).
- **Evidence**: `Cancelled=1` → `Visa2014ApplicationItemCancelledFlagsMapper.ResolveDocumentCancellation`; completed cancel subtypes **12 / 21 / 22** → direct visa/WP mapping; merged per linked `Visa` / `WorkPermit` OID.
- **Files**: `Visa2014LegacyDocumentCancellationIndex.cs`, refactored mapper, `Visa2014VisaTransform`, `Visa2014WorkPermitItemTransform`, OData payload for WP item; field-map notes in `Visa.yaml` / `WorkPermitItem.yaml`.
- **Tests**: 12 passed (`Visa2014LegacyDocumentCancellationResolverTests` + mapper tests).
- **Not done**: idempotent OData PATCH backfill for already-imported rows; `InvitationItem` already had `ApplicationResult.Result==1`.
- **Reimport**: run `WorkPermitItem` + `Visa` waves (or targeted reimport scripts) after deploy; `ApplicationItem` workflow flags unchanged.

### 2026-07-06 — Dev reimport Visa + WorkPermitItem cancellation (`VisaWorkPermitCancellation.ps1`)

- **Script**: `scripts/visa2014-migration/reimport/VisaWorkPermitCancellation.ps1 -Configuration Debug`
- **Cleanup**: `cleanup/ImportedVisaWorkPermitCancellationBackfill.sql` — null `ApplicationItems` visa/WP FKs; delete all `Visas` (+ docs) and `WorkPermitItems`; keep `WorkPermit` headers
- **Import waves** (all **0 failed**):

  | Entity | Posted | Skipped (id-map) | Notes |
  |--------|-------:|-----------------:|-------|
  | Visa | 5975 | 41 (no Passport map) | log: `import-logs/reimport-Visa-cancellation-*.log` |
  | WorkPermitItem | 3797 | 2566 | 47 position fallback |
  | ApplicationItem | 21306 | 194 | relink `CurrentVisa` / `CurrentWorkPermitItem` |

- **Cancellation counts (target `Visa2026` after import)**:

  | Layer | Count |
  |-------|------:|
  | `Visas.IsCancelled` | **685** |
  | `WorkPermitItems.IsCancelled` | **634** |
  | `ApplicationItems.IsCancelled` (WP line) | **750** (unchanged — workflow flags) |
  | `ApplicationItems.VisaIsCancelled` | **3** |
  | `ApplicationItems` with `CurrentVisaId` | 15208 |
  | `ApplicationItems` with `CurrentWorkPermitItemID` | 3580 |

- **Outcome**: **partial success** — all three OData imports completed; script exit **1** on post-import corrections (`ApplicationItems.ps1` → `--correct-person-address-of-residence`) with `hostpolicy.dll` / missing `Visa2026.DataImporter.runtimeconfig.json` after ~37 min run (likely file lock / stale `--no-build` output). Cancellation backfill itself is done; re-run corrections when build is clean: `ApplicationItems.ps1 -SkipCorrections` not needed — run correction flags only via DataImporter after `dotnet build`.
- **Before reimport (stale)**: 863 visa + 195 WP item cancelled (pre-index logic / partial state).

### 2026-07-06 — P0.5 invitation document `IsCancelled` backfill (InvitationItem)

- **What**: `Visa2014LegacyInvitationItemCancellationIndex` — `ApplicationResult.Result == 1` **plus** `PersonInApplication.Cancelled` on cancel-invitation apps matched to `PersonInInvitation` (same OUTER APPLY as ApplicationItem `CurrentInvitationItem` resolver).
- **Files**: `Visa2014LegacyInvitationItemCancellationIndex.cs`, `Visa2014InvitationItemTransform`, `field-maps/InvitationItem.yaml`, tests in `Visa2014LegacyInvitationItemCancellationResolverTests.cs`.
- **Reimport**: `scripts/visa2014-migration/reimport/InvitationCancellation.ps1` + `cleanup/ImportedInvitationItemCancellationBackfill.sql` (InvitationItems delete + ApplicationItem FK relink via `ApplicationItems.ps1`).
- **Before fix (dev)**: `InvitationItems.IsCancelled` **0** vs `ApplicationItems.InvitationItemIsCancelled` **16**.

### 2026-07-06 — Dev reimport InvitationItem cancellation (`InvitationCancellation.ps1`)

- **Script**: `scripts/visa2014-migration/reimport/InvitationCancellation.ps1 -Configuration Debug` — exit **0** (~20 min).
- **Import waves** (all **0 failed**):

  | Entity | Posted | Skipped (id-map) |
  |--------|-------:|-----------------:|
  | InvitationItem | 4955 | 284 |
  | ApplicationItem | 21306 | 194 |

- **Cancellation counts (target `Visa2026` after import)**:

  | Layer | Count | Notes |
  |-------|------:|-------|
  | `InvitationItems.IsCancelled` | **6** | was **0** |
  | `ApplicationItems.InvitationItemIsCancelled` | **16** | unchanged (workflow mirror) |
  | App items with flag **and** `CurrentInvitationItemID` | **6** | all linked docs `IsCancelled=1` |
  | App items with flag **without** `CurrentInvitationItemID` | **10** | cancel-invitation lines with no resolvable `PersonInInvitation` match |

- **Index (dry-run verbose)**: `Legacy invitation-item cancellation index: 260` PersonInInvitation OIDs — but **all 254** legacy rows with `ApplicationResult.Result = 1` are **absent** from `InvitationItem.json` (not imported; invitation header id-map gap). Verified cancelled sample `0328c30b-…` has **Result = 0** and `PersonInApplication.Cancelled = 1` — the **6** dev rows come from the **PIA cancel path**, not `Result == 1`.
- **Follow-up**: revalidate `ApplicationResult.Result == 1 → IsCancelled` heuristic (likely not cancellation); consider dropping or replacing that path in `Visa2014LegacyInvitationItemCancellationIndex` so index count matches importable rows. `InvitationItem` status distribution: 4897 none / 52 `IsChanged` / 6 `IsCancelled`.
- **UTF-8**: rewrite `.ps1` / `.sql` with `[System.IO.File]::WriteAllText(..., UTF8Encoding(false))` if Cursor `Write` corrupts encoding (parse error on first run).


### 2026-07-11 — Demo Import: stop-on-failure; Passport UAE; Education gaps

- **Stop-on-failure**: OnPrem-Sync default (no `-ContinueOnError`) confirmed on Demo — Education fail exit 1 halted chain before EmployeePositionHistory.
- **Passport**: calik Country overlay replaced base values[]; UAE identity-passed → ResolveCountry miss (ARE only). Merge Load() + explicit UAE→ARE in calik overlay. Resume Posted 5 / Failed 0.
- **Education**: 47 incomplete payloads — EducationInstitution / Specialty NameTm not in Demo tenant catalogs (encoding-sensitive labels). Fix catalogs or allow_null policy before `-StartAt Education`.

### 2026-07-11 — Zero FailedCount; Education institution/specialty seed catch-up

- **Rule**: no tolerable FailedCount unless deliberate exclusion (skipped, not failed). Documented in import-practices §7b + onprem-legacy-sync hard rule 8.
- **Education Demo**: missing 47 institution + related specialty labels vs live `.15`; SQL seed + refreshed calik-energi JSON (1500/1083). Resume Posted 47 / Failed 0.

### 2026-07-11 — Intentional exclusions require approval + registry

- **Rule**: skips only via `docs/VISA2014_MIGRATION/import-exclusions.yaml` (`status: approved`) with why, counts, approvedBy/At. FailedCount is never an exclusion.
- **Seeded**: EXC-APPTYPE-E33-E55 (105 apps / 204 items), EXC-VISA-ISSUEDPLACE-EMBASSY (18 visas).
- **Docs**: import-practices §7c, onprem hard rule 8, SKILL link, import-strategy pointer.

### 2026-07-11 — Demo AddressOfResidence diagnosis

- **Initial**: Posted 0 / Failed 5122. Gaps reported as City=...
- **Root cause 1**: `Visa2014CityLookupMatcher` requires Region match; ObjectSpace loader left `City.Region`/`RegionName` empty (Demo `RegionName` all null). Fixed ObjectSpace Region load + name-only fallback when no city has region metadata.
- **Root cause 2**: Demo `Lodgings` count was **0** after wipe (tenant lodging catalog not re-synced). `ForceUpdate` Demo -> Lodgings **76**.
- **After fix**: Posted **3069** + PIA **970**; Failed **80** (City/Lodging gaps: Serdarabat, Beýik Saparmyrat…, Serhetabat + a few lodging/other-site scalars). Chain stopped (no ContinueOnError).
- **Next**: align remaining city region links / lodging FullAddress normalize for ~80 rows; write full error list (not Take(10)).

### 2026-07-11 — AoR geography policy (b): prefer legacy Region when Wiki/OSM agrees

- **Decision**: City.Region catalog aligned to Wikipedia/OSM; import prefers legacy Region+City when that pair matches; if legacy Region is wrong but city name is unique among region-linked rows, use catalog City.Region.
- **city.json**: Serhetabat etraby → Mary; Serdarabat etraby → Lebap; Beýik Saparmyrat… → Lebap (historical Beýik district).
- **Demo SQL**: in-place RegionID updates for those cities; filled 3 Lodging.CityID nulls (Watan/Parahat/Çemenabat).
- **Code**: `Visa2014CityLookupMatcher` unique region-linked fallback; ImportApplier sets Region from City after resolve; gap exporter CLI `--export-visa2014-import-gaps`.
- **Result**: import-gap preview **80 → 9** remaining (mostly lodging/other-site scalar still unresolved after city fix).

- **Follow-up**: unique Region-FK fallback (ignore null-Region orphan + RegionName enrich duplicates) → gap preview **0**; would-post 67 legacy + 14 PIA. Resume Demo -StartAt AddressOfResidence after deploying updated DataImporter to sync host.

### 2026-07-11 — Turkmenistan geography reference SQLite DB

- **Path**: `Visa2026.DataImporter/legacy/visa2014/reference/turkmenistan-geography.db` (6 regions, ~89 cities + aliases).
- **Seed**: `region.json` + `city.json` + `geography-overrides.json` (Wiki/OSM conflict cities).
- **Rebuild**: `--rebuild-visa2014-geography-db`.
- **Import**: AddressOfResidence uses store policy (b) — keep legacy Region when it matches DB; else use DB Region for city name.

### 2026-07-11 — Mandatory lookup preflight before full Import

- **Gate**: `--preflight-visa2014-lookups` (Phase A catalog sampleQuery + Phase B entity transforms + optional target DB key check).
- **Orchestrator**: `OnPrem-Sync.ps1 -Mode Import` runs preflight automatically; `-SkipLookupPreflight` only for approved exceptions; `-LookupPreflight` enables it for Sync.
- **Wrapper**: `scripts/visa2014-migration/import/Preflight-LookupAudit.ps1`.
- **Why**: live lookup drift (Education institutions, City/Region from FullAddress) must be audited → translated → seeded before Import, not discovered mid-wave as FailedCount.

### 2026-07-11 — Skill: Full Import order = lookup resolution → preflight → Import

- **Named process**: **lookup resolution** (audit → translate → seed LookupCatalogs/tenant JSON), then **lookup preflight**, then full Import.
- **Updated**: visa2014-to-visa2026-import `SKILL.md` (§ Full Import order), `import-practices.md`; onprem-legacy-sync hard rule 9 + preflight table.
- **Calik**: base + `lookup-translations.calik-energi.yaml`.

### 2026-07-11 — user-prompts for lookup resolution / preflight

- Added `visa2014-to-visa2026-import/user-prompts.md` (resolution, preflight, Calik, full Import order).
- Linked from import `SKILL.md`; Demo/lookup openers added to onprem `user-prompts.md`.

### 2026-07-11 — Demo lookup preflight (calik-energi-onprem-demo)

- **Run**: `.25` `C:\visa2026-sync-demo`; report `lookup-preflight-demo-20260710-232046.json`.
- **Result**: FAILED — Blocking=30 Allowed=47 TargetCatalogs=0 (target key load still broken).
- **Gaps**: CityByName ~12 distinct (Atamyrat, Baharly, Balkanabat, Beyik…, Dowletli, Hojambaz, Serdar, Serdarabat, Tejen, Turkmenabat, Turkmenbasy) on Application/ApplicationItem; City (null) x2; Region free-text x3; CheckPoint sampleQuery syntax.
- **Next**: lookup resolution for CityByName + fix CheckPoint query / target key loader before full Import.

### 2026-07-11 — Obsolete visa2026-onprem-legacy-sync; sole migration skill

- **Decision**: stop using `@visa2026-onprem-legacy-sync`. All data migration (lookup resolution, preflight, Demo/Prod Import on .25) is `@visa2014-to-visa2026-import` only.
- **Obsolete skill**: `disable-model-invocation: true`; stub points here; learnings/reference kept as archive.
- **Updated**: AGENTS.md, ON_PREM runbook, on-prem-deploy MATURITY, import SKILL + user-prompts (Demo/Prod section).

### 2026-07-11 � Removed delta Sync (keep Import)

- **Removed**: `--sync-visa2014` / `--sync-full` / `--sync-since` / `--sync-state-dir` / `--no-soft-delete-sync`; `Visa2014SyncCommand` + StateStore/IdMapLoader/RowFilter; `RunSyncAsync` on OData importers; soft-delete sync query; Sync-only PS1s (Register task, Compare/Export/Watch SyncState, OnPremSyncState lib); `LegacySyncDashboard` (Module + Blazor); skill folder `visa2026-onprem-legacy-sync`.
- **Kept**: `OnPrem-Sync.ps1` Import-only; `--import-visa2014`; lookup preflight; `Visa2014SyncPayloadFkHelper`; Import progress sidecars on `Visa2014SyncUpsertHelper`; host roots `C:\visa2026-sync*`.
- **Ops**: disable Task Scheduler `Visa2026-OnPrem-LegacySync` on `.25` manually if still registered.

### 2026-07-13 � Demo hard wipe + reimport blocked by lookup preflight

- **Phase**: end-to-end (on-prem Demo wipe + Import)
- **Outcome**: wipe/seed success; Import **not started** (preflight exit 2)
- **Environment**: `10.100.128.25` / `Visa2026DbDemo` � sync host `C:\visa2026-sync-demo` � legacy `10.100.128.15` / `VISA2015`
- **Wipe**: DROP+CREATE `Visa2026DbDemo` (sqlcmd `-E -C`); cleared id-maps + `{}` stubs; `Run-Visa2026DbUpdateOnServer.ps1 -Profile Demo -ForceUpdate`; LoginPage HTTP 200; redeployed DataImporter (post delta-Sync removal)
- **Import**: `Run-OnPremSyncOnServer.ps1 -Profile Demo` stopped at lookup preflight � **30 blocking** gaps
- **Blockers**: CheckPoint sampleQuery syntax (`CheckPoint` reserved); CityByName ~12 cities on Application/ApplicationItem (encoding/normalize); City `(null)` x2; Region free-text x3 (AoR/Lodging)
- **Person..EPH Phase B**: OK in preflight
- **Next**: lookup resolution for CityByName (+ fix CheckPoint brackets on live YAML) then re-run preflight; or user-approved `-SkipLookupPreflight` (Application waves will still hit CityByName)

### 2026-07-13 � Demo preflight fixed (CityByName + CheckPoint); Import started

- **Phase**: end-to-end (Demo after hard wipe)
- **Outcome**: preflight **exit 0**; Import **Running** (RunId `20260712-205532`, wave Person)
- **Fixes**:
  1. CheckPoint sampleQuery: `pia.[CheckPoint]` (reserved word)
  2. CityByName: `identityPassThrough` + city.json NameTm identity enrich in `Visa2014LookupTranslator.Load` + fold-match targets; Application city aliases in YAML
  3. City/Region: `unmappedPolicy: allow_null` for skip-row free-text/null city gaps (still skipped at import)
  4. City sampleQuery keeps `dbo.[S�herEtrap]` (ASCII `SeherEtrap` is invalid on live VISA2015)
- **Deploy**: republished DI + YAML + `LookupCatalogs/city.json` to `C:\visa2026-sync-demo`
- **Watch**: `Watch-OnPremImportLive.ps1 -Profile Demo -ViaSsh`; log `logs\sync-run-20260712-205532.log` / wrap `demo-import-wrap-20260712-205531`

### 2026-07-13 — Demo Import restart after orphaned Person wave

- **Phase**: end-to-end (Demo Import restart)
- **Outcome**: **success** (restart); prior run 20260712-205532 was **dead/orphaned**, not slow
- **Environment**: 10.100.128.25 / Visa2026DbDemo · sync host C:\visa2026-sync-demo · legacy 10.100.128.15 / VISA2015
- **Prior failure**: RunId 20260712-205532 — DI logged headless host on :5012 then stopped (~17s); no Progress lines; People=0; status stuck Overall=Running; DataImporter: alive=False; sync-run log never written. Watch elapsed time was stale status, not slow Person.
- **Restart**: marked stale status Failed; launched wrap via `Win32_Process.Create` (survives SSH); `Run-OnPremSyncOnServer.ps1 -Profile Demo -SkipTenantCatalogGeneration -SkipLookupPreflight`
- **New run**: RunId `20260712-211301` — Person **Posted 3303 / Failed 0** (~1 min); People=3303; advanced to Passport; DI alive
- **Ops tip**: prefer `Win32_Process.Create` or equivalent detach for Demo Import on .25; do not trust Watch `Running` alone — check `alive` + People delta + Progress lines
- **Watch**: `Watch-OnPremImportLive.ps1 -Profile Demo -ViaSsh`; wrap `logs\demo-import-wrap-20260712-211259.log`; sync-run `logs\sync-run-20260712-211300.log`


### 2026-07-13 — Demo Education Failed (54 incomplete payload); catalogs gap; resume OK

- **Phase**: end-to-end (Demo Import after wipe)
- **Outcome**: Education **Failed** exit 1 (54 incomplete Institution/Specialty); after catalog refresh **Completed** Failed=0 (Posted 54 catch-up)
- **Environment**: `Visa2026DbDemo` / live `10.100.128.15` VISA2015
- **Symptom**: Person/Passport/Visa OK; Education 3115 posted / 54 failed; Overall Failed (stop-on-failure)
- **Cause**: live Education labels ahead of Demo tenant catalogs (~52 Institution + ~32 Specialty exact gaps). Preflight Education `UnmappedLookupCount=0` because `identityPassThrough` — does not prove ObjectSpace catalog rows exist.
- **Fix**: regenerated `education-institution*.json` / `specialty*.json` from live DISTINCT (~1507 / ~1085); tenant manifest **37→38**; disk overlay `C:\inetpub\visa2026-demo\LookupCatalogs\tenant\` + `Run-Visa2026DbUpdateOnServer.ps1 -Profile Demo -ForceUpdate`; resume `-StartAt Education`
- **Resume**: RunId `20260712-212802` — Education Posted 54 / already 3115 / Failed 0; Educations=3169; continued to EPH then AddressOfResidence
- **Watch**: `Watch-OnPremImportLive.ps1 -Profile Demo -ViaSsh`


### 2026-07-13 — Import reimport history archive + compare dashboard

- **Phase**: tooling (on-prem Import ops)
- **Outcome**: shipped archive + HTML index + compare CLI
- **Artifacts**:
  - `scripts/visa2014-migration/_lib/OnPremImportRunArchive.ps1`
  - `Archive-OnPremImportRun.ps1` / `Compare-OnPremImportRuns.ps1`
  - `OnPrem-Sync.ps1` archives on Complete / wave-fail / preflight-fail
  - Dashboard: `<SyncHostRoot>\history\index.html`; runs under `history\runs\<RunId>\`
- **Demo**: archived RunId `20260712-212802` (current completed reimport); need a second archive before compare is meaningful
- **Note**: Import-only (hard-delete reimport friendly); not delta Sync


### 2026-07-13 ? Import reimport history in XAF Navigation (Administrators)

- **Phase**: tooling (on-prem Import ops UI)
- **Outcome**: Operations ? Import reimport history (non-persistent host + Blazor editor)
- **Data**: reads `history\runs\<RunId>\*.json` via `ImportHistory:RootPath` (defaults from `DeploymentEnvironment:Slot`)
- **Security**: deny Users type/nav; Administrators via `IsAdministrative`
- **Ops**: app pool must read `C:\visa2026-sync*\history`; `Configure-Visa2026Production.ps1` writes ImportHistory.RootPath per slot


### 2026-07-13 - Archive file waves only after DocumentCopies
- Reimport history must be finalized after scalar corrections and optional `DocumentCopies.ps1`; archive `file-waves-status.json` plus target file-presence metrics, and force a failed archive before a non-continue file-wave exit.
### 2026-07-13 — Document copies on Import reimport history (Phase A + gap inventory)

- **Phase**: tooling (archive + XAF history UI)
- **Archive order**: scalar → postImportCorrections → optional DocumentCopies (`-IncludeFileWaves`) → Complete → Archive
- **Artifacts per RunId**: `file-waves.json` (Included + Steps), `file-presence.json` (Photo / *Document vs parents), `meta.FileWavesIncluded`
- **UI**: Operations → Import reimport history sections for file waves + file presence Left/Right
- **DocumentCopies.ps1 covered today**: Person.Photo, PassportDocument, VisaDocument, EducationDocument, WorkPermitDocument, InvitationDocument, FamilyProofDocument
- **Gap inventory (Phase B — not added yet)**: only add when `importConfirmed` + importer exists
  - MedicalRecordDocument (presence metric already; import step not in DocumentCopies.ps1)
  - RejectionDocument, BorderZoneDocument, AddressOfResidenceDocument, LodgingDocument, ProjectContractDocument, PersonFamilyRelationDocument
  - ApplicationProgress.MinistryLetterFile (FileData) — separate from DocumentCopies child docs
  - EducationDocument / VisaDocument tables may be missing on some Demo DBs (PresentCount null soft-fail) until BO/schema registered
- **Demo backfill**: RunId `20260712-212802` got `file-waves.json` Included=false + live `file-presence.json` (photos/docs mostly 0 without file waves)


### 2026-07-13 — Demo hard wipe + reimport with -IncludeFileWaves (started)

- **Phase**: end-to-end (Demo wipe + Import + DocumentCopies)
- **Outcome**: **started** (in progress)
- **Wipe**: business tables cleared (People/Apps/docs=0); lookups/templates kept; id-maps cleared under `C:\visa2026-sync-demo\data\id-maps\calik-energi-onprem-demo`
- **Import**: scheduled task `Visa2026-OnPrem-DemoImportFileWaves` → `Run-OnPremSyncOnServer.ps1 -Profile Demo -IncludeFileWaves -SkipTenantCatalogGeneration` (no ContinueOnError)
- **RunId**: `20260712-235158` Overall=Running; Person done (~3304 People); Passport Running; DI alive
- **Disk**: C ~18.6 GB free, E ~17.4 GB free — watch space during file waves (photos/scans)
- **Watch**: `Watch-OnPremImportLive.ps1 -Profile Demo -ViaSsh`; wrap `logs\demo-import-wrap-20260712-235156.log`; sync-run `logs\sync-run-20260712-235158.log`
- **Expect**: after scalar waves, DocumentCopies.ps1 then archive with `file-waves.json` Included=true + file-presence


### 2026-07-13 — Demo Education Failed (5) on IncludeFileWaves reimport; fixed + resumed

- **Run**: `20260712-235158` Failed at Education (Posted 3165 / Failed 5) — stop-on-failure
- **Cause**: missing catalog rows for composite labels
  - Institution: `Lizabon s. orta mekdep,CCVD-VCA okuw kursy`
  - Specialty (1 remaining after institution seed): exact legacy `Speciality.TitleOfSpeciality` via `Education.Spcialty` (Unicode İ/ş/ý/ç/ü)
- **Fix**: INSERT into Demo `EducationInstitutions` / `Specialties` with `GCRecord=0` (NULL GCRecord insert fails)
- **Resume**: `-StartAt Education -IncludeFileWaves -SkipLookupPreflight -SkipTenantCatalogGeneration`
- **Outcome**: Education **Completed** Failed=0 (Posted 1 catch-up / already 3169); Educations=3170; continued to EPH then AddressOfResidence (RunId `20260713-000016`)
- **Note**: keep exact Unicode from legacy when seeding; ASCII approximations do not resolve


### 2026-07-13 — Demo IncludeFileWaves: scalars OK, DocumentCopies Failed (id-map path + LocalDB)

- **Run**: `20260713-000016` Overall=Failed after ApplicationProgress Completed (scalar Fail=0; Person/Passport/Visa Pending due to `-StartAt Education`)
- **Cause 1**: `DocumentCopies.ps1` on SyncHostRoot used DataImporter default id-map under `tools\DataImporter\legacy\visa2014\id-maps\...` → `Person id-map not found` (maps live at `data\id-maps\calik-energi-onprem-demo\`)
- **Fix 1**: pass explicit `--id-map` / `--*-id-map` to `$SyncHostRoot\data\id-maps\<LegacySource>\*.json`
- **Cause 2** (file-waves-only re-run): wrap used wrong env `VISA2026_DEMO_CONNECTION` + `appsettings.json` (LocalDB); correct is `VISA2026_DEMO_SQL_CONNECTION` / `appsettings.Production.json`
- **Fix 2**: DocumentCopies sets `ConnectionStrings__DefaultConnection` + safe `--target-connection` (same as OnPrem-Sync); Demo file-waves re-run via task with correct CS
- **Also**: archive history index coerces `ElapsedSeconds` when JSON yields Object[] (avoids `op_Division`)
- **Outcome**: file-waves re-run in progress — Target SQL=`localhost\SQLEXPRESS`/`Visa2026DbDemo`; Id-map=`...\data\id-maps\...\Person.json`; Person-Photo Running

### 2026-07-13 — Production hard wipe + Import (future prod; no backup)

- **Phase**: end-to-end (Prod wipe + scalar Import)
- **Outcome**: **running** after Encrypt/login fixes
- **Wipe**: business tables cleared on `Visa2026DbProd`; id-maps cleared under `C:\visa2026-sync\data\id-maps\calik-energi-onprem-prod`; lookups/templates kept. No prod `.bak` (user: not official prod yet).
- **Disk**: moved `C:\visa2026\backups` → `E:\visa2026\backups` before wipe (C: was ~2.5 GB free)
- **Failures then fixes**:
  1. Stale DI missing `--preflight-visa2014-lookups` → refresh published DataImporter on sync host
  2. `Encrypt=False` → `Invalid value for key 'Encrypt'` (Microsoft.Data.SqlClient) → use `Encrypt=Optional` / `Mandatory`; OnPrem-Sync + ContentRoot normalize
  3. Env-normalize script mangled `VISA2014_SQL_PASSWORD` and created `SQL_SERVER_10.100.128.15=...;Encrypt=Optional` → restore password; embed Password into `VISA2014_SQL_CONNECTION`; drop mangled key
  4. SYSTEM task Login failed for ReadOnlyUser when CS lacked Password (env inject flaky) → embed password in CS
  5. Person wave: `Visa2014LegacySqlGuard.DescribeLegacyConnection` used `SqlConnectionStringBuilder` which rejected `Encrypt=Optional` even when `SqlConnection.Open` worked → try/catch + MaskConnectionForLog fallback
- **RunId**: `20260713-025546` Overall=Running; Person Completed (Posted 3306 / Failed 0; People=3306); Passport Running
- **Task**: `Visa2026-OnPrem-ProdImportOnce` → `-Profile Production -SkipTenantCatalogGeneration -SkipLookupPreflight -StartAt Person` (scalar only; file waves later)
- **Watch**: `Watch-OnPremImportLive.ps1 -Profile Production -ViaSsh`; status `C:\visa2026-sync\sync-run-status.json` (wrap Tee fills only after Run-OnPremSyncOnServer exits)


### 2026-07-13 — Prod Education Failed(15) then Failed(3); fixed with exact Unicode seeds

- **Run**: `20260713-025546` Failed at Education (Posted 3157 / Failed 15)
- **Cause**: missing `EducationInstitutions` / `Specialties` NameTm rows — same Demo gaps plus Turkmen Unicode (dotless `ı`, `ş`, `ý`, `ü`, `ç`) that ASCII/`Riko` seeds do not match
- **Fix**: INSERT exact titles from VISA2015 via hex→UTF-16 (sqlcmd `-y 0 -Y`) into Prod; also copy known Demo rows first
- **Resume**: `-StartAt Education -SkipLookupPreflight -SkipTenantCatalogGeneration`
- **Outcome**: RunId `20260713-031108` Education **Completed** Failed=0 (Posted 3 catch-up; Educations=3172); continued to EmployeePositionHistory
- **Lesson**: seed NameTm from legacy hex when console mangling hides `ı` vs `i`; verify LEN matches legacy LEN


### 2026-07-13 — Prod ApplicationItem fail; sync.env parse bug (same Demo resume path)

- **Phase**: end-to-end (Prod resume ApplicationItem)
- **Symptom**: Watch showed ApplicationItem 0/21780 for ~14m (prepare, not hang); later `-StartAt ApplicationItem` failed in ~3s with exit `-532462766` (`0xE0434352` CLR) or `Invalid value for key 'Multiple Active Result Sets'`
- **Root cause**: `Run-OnPremSyncOnServer.ps1` `Import-SyncEnvFile` used `Read-TextFileAutoEncoding -Path $Path -split` **without parentheses**. PowerShell binds `$Path -split` first → whole `sync.env` becomes one line → only `VISA2014_SQL_PASSWORD` is set (value = rest of file). `VISA2026_PROD_SQL_CONNECTION` never loads → appsettings fallback; CS builders choke on embedded newlines after `MultipleActiveResultSets=true`
- **Fix**: `(Read-TextFileAutoEncoding -Path $Path) -split` in repo + patched on `.25` prod/demo sync hosts; Encrypt normalized on prod `sync.env`
- **Resume (same as Demo)**: `Visa2026-OnPrem-ProdImportOnce` → `Run-OnPremSyncOnServer.ps1 -Profile Production -SkipTenantCatalogGeneration -SkipLookupPreflight -StartAt ApplicationItem -Parallelism 1`
- **Outcome**: RunId `20260713-040316` Overall=Running; `parallel post: 21781 row(s), workers=1`; DI posting in progress (early 0% is normal)
- **Prevent**: always parenthesize `Read-TextFileAutoEncoding` before `-split`; verify password env length ~11 not hundreds after sync.env edits

### 2026-07-13 — ApplicationItem import: sequential post (no ParallelImportPoster)

- **Phase**: import-code
- **Context**: Prod ApplicationItem hung at 0/N on first CreateAsync via ParallelImportPoster (workers=1 or 4); Demo had completed earlier with workers=4
- **Change**: `Visa2014ApplicationItemODataImporter` posts like Education/Passport — sequential `foreach` + `CreateAsync` + `FlushAsync` + progress sidecar; `--parallelism` ignored for this entity
- **OnPrem-Sync**: comment updated (Application/ApplicationProgress may still use parallelism)
- **Next**: publish DataImporter to `C:\visa2026-sync` and resume `-StartAt ApplicationItem`

### 2026-07-13 — Prod ApplicationItem sequential resume (after `_legacyRowId` fix)

- **Phase**: end-to-end (Prod `-StartAt ApplicationItem`)
- **Deploy**: published DataImporter to `C:\visa2026-sync\tools\DataImporter`; RunId `20260713-045401`
- **Bug**: first sequential build used `_legacyOid` (KeyNotFound) — must be `_legacyRowId`
- **Outcome**: sequential post alive — ~2000/21786 (~9%), posted~1984 failed=0, DB ApplicationItems rising (~2050)

### 2026-07-13 — Prod ApplicationProgress hang (batch-size 50) → sequential flush-per-row

- **Phase**: end-to-end (Prod ApplicationProgress after ApplicationItem Completed)
- **Symptom**: Watch `100/55068 posted=49 fail=51` then freeze at `200` with `DbCount=0`; DI CPU climbing; already `workers=1`
- **Root cause**: importer intended one-row commits (`progressBatchSize=1`) but with `parallelism=1` ParallelImportPoster uses the **shared** headless target opened with `--batch-size 50`. First `CommitChanges` of ~50 ApplicationProgress rows fights `Application.LatestProgress` and hangs. Early `49/51` fail ratio matched Demo noise (incomplete State/Location payloads) and is unrelated to the hang.
- **Fix**: `Visa2014ApplicationProgressODataImporter` sequential `foreach` + owned `Visa2014ObjectSpaceImportTarget(batchSize:1)` (flush per row); `--parallelism` ignored; log first 25 payload gaps to stderr
- **Deploy**: published DI to `C:\visa2026-sync\tools\DataImporter`; resume RunId `20260713-051356` `-StartAt ApplicationProgress`
- **Outcome (verified early)**: `sequential post: 55070`; ~3100/55070 posted~3091 failed=0 skipped=9; `ApplicationProgresses` DB count rising in lockstep (~3105)
- **Ops note**: wrap scripts must pass `-StartAt` on **one line** — backtick line-continuation inside `@"..."@` here-strings drops args (first resume wrongly started Person)
- **Prevent**: never batch ApplicationProgress commits; do not trust DbCount=0 alone when shared batch>1 (rows may be uncommitted)
- **Cross-skill**: visa2026-windows-iis-deploy


### 2026-07-14 — Prod file waves started (DocumentCopies only)

- **Phase**: file-wave (Production, after scalar ApplicationProgress Completed)
- **Mode**: file-wave only (`DocumentCopies.ps1`, not full scalar re-run)
- **Environment**: `C:\visa2026-sync` · `Visa2026DbProd` on `E:\visa2026\sql-data\` (~145 GB free on E:; C: ~15 GB)
- **Script**: task `Visa2026-OnPrem-ProdFileWavesOnce` → DocumentCopies `-LegacySource calik-energi-onprem-prod -StartAt Person-Photo`
- **Outcome**: started — `file-waves-status.json` Overall=Running; Person-Photo Running; id-map path correct under `data\id-maps\...`
- **Watch**: `Watch-OnPremImportLive.ps1 -Profile Production -ViaSsh -ClearScreen` (File waves table)
- **Prevent**: do not use `-IncludeFileWaves` without `-StartAt` if you only want files — that re-prepares all scalars; call DocumentCopies directly after scalar Complete


### 2026-07-14 — Prod DocumentCopies failed mid EducationDocument (filegroup full); Demo removed

- **File waves**: Person-Photo / PassportDocument / VisaDocument OK; EducationDocument Posted 3682 / Failed 605 then abort — `Could not allocate space for object dbo.FileData` (PRIMARY full)
- **Mitigation**: removed Demo IIS + `Visa2026DbDemo` (~15 GB on C:) — C: free now ~32 GB; Prod/Staging untouched
- **Resume next**: after confirming Prod data file can grow on E:, `DocumentCopies.ps1 -StartAt EducationDocument` (id-map skip already imported diplomas)
- **Cross-skill**: visa2026-windows-iis-deploy


### 2026-07-14 — Prod FileData hit SQL Express 50 GB cap; reclaim + resume EducationDocument

- **Root cause**: `Visa2026DbProd` is Express Edition — licensed max **51200 MB (50 GB)**. MDF was 100% full (`FileData` ~48.9 GB). `ALTER ... SIZE` beyond 50 GB fails with licensed limit.
- **Reclaim**: deleted ~992k `SyncRuleLogs`; `DBCC SHRINKFILE` → ~**0.7 GB** headroom (used ~49.29 / 50 GB). Demo removal freed C: only (Prod lives on E:).
- **Resume**: DocumentCopies `-StartAt EducationDocument` with EducationDocument id-map **3682** keys (match DB). Task `Visa2026-OnPrem-ProdFileWavesOnce`.
- **Risk**: remaining Education + WorkPermit/Invitation/FamilyProof may refill the 50 GB cap — lasting fix is **upgrade off Express** (Developer/Standard) or externalize FileData.
- **Cross-skill**: visa2026-windows-iis-deploy



### 2026-07-14 — Demo PostgreSQL Person pilot (scalar import, in-process)

- **Phase**: end-to-end pilot (Demo PG target, Person only)
- **Environment**: `10.100.128.25` · IIS `Visa2026-Demo` `:8081` · PG `visa2026_demo` (`EFCoreProvider=Postgres`) · sync host `C:\visa2026-sync-demo` · legacy `10.100.128.15` / `VISA2015`
- **Deploy**: republished `Visa2026.DataImporter` to `C:\visa2026-sync-demo\tools\DataImporter`; `config\sync.env` with `VISA2014_SQL_PASSWORD` + `VISA2026_MIGRATION_IMPORT_URLS=http://127.0.0.1:5012`; target CS from `C:\inetpub\visa2026-demo\appsettings.Production.json` via `ConnectionStrings__DefaultConnection`
- **Code gates (Postgres pilot)**:
  - `Visa2014LookupPreflightCommand.LoadTargetCatalogKeys` — skip SqlClient target catalog load when `DatabaseProviderDetector.IsPostgreSql`
  - `OnPrem-Sync.ps1` `Invoke-DataImporterCli` — do not append `Encrypt=Optional` to Npgsql connection strings
  - `Visa2014PersonIdMapExpander` — skip PN-collision SqlClient scan on PostgreSQL (dedupe aliases still apply)
  - `Visa2014ImportTrackingLogCleanup` — skip T-SQL `OBJECT_ID` cleanup on PostgreSQL
- **Run**: direct `--import-visa2014 --entity Person --inprocess --max-rows 50 --batch-size 25` (log `demo-Person-pilot2-20260714-002442.log`)
- **Outcome**: **exit 0** · Prepared 50 · Posted **50** / Failed **0** · PG `People` count **50** · id-map **50** keys · `PN collision skipped on PostgreSQL`
- **First attempt**: Posted 50 then exit **1** — post-import `Visa2014PersonIdMapExpander` opened `SqlConnection` on Npgsql CS (`Keyword not supported: 'host'`). Fixed by PG skip above.
- **Preflight**: use `-SkipLookupPreflight` for full Demo chain until Npgsql target catalog loader exists (Phase A legacy audit still runs when preflight enabled; target Phase A missing-target checks are empty on PG skip).
- **Still SQL-only on target** (not hit by Person pilot): duplicate guards (`Visa2014PersonIdentityDuplicateGuard`, ApplicationItem/Progress guards), `Visa2014TargetIdMapRebuild`, archive/watch DbCounts — gate or Npgsql port before full scalar chain.
- **File waves**: not attempted on PG (`FileData` / SqlClient paths unchanged).
- **Prevent**: never run `OnPrem-Sync` Encrypt normalization on Postgres CS; verify exit code after pilot — posted count alone is insufficient when post-import hooks use SqlClient.


### 2026-07-14 — Demo PG full scalar Import started (after Person pilot)

- **Phase**: end-to-end (Demo PostgreSQL scalar chain)
- **Prep**: redeployed DataImporter + OnPrem scripts to `C:\visa2026-sync-demo`; wiped pilot `People` (50); reset id-map JSON stubs
- **Task**: `Visa2026-OnPrem-DemoImportOnce` → `Run-OnPremSyncOnServer.ps1 -Profile Demo -SkipTenantCatalogGeneration -SkipLookupPreflight` (SYSTEM)
- **RunId**: `20260714-003304`
- **Early outcome**: Person **Completed** exit 0 — Posted **3310** / Failed **0** (Dedupe merged 21); PG `People`=3310; id-map expand skipped PN collision on PostgreSQL
- **In progress**: Passport done → current wave **Visa** (DI still running)
- **Watch**: `Watch-OnPremImportLive.ps1 -Profile Demo -ViaSsh` · status `C:\visa2026-sync-demo\sync-run-status.json`
- **Note**: wrap script must use real newlines (literal `` `r`n `` text breaks the scheduled task)


### 2026-07-14 — Demo PG Education catalog gap; regenerate from VISA2015 + resume

- **Symptom**: Run `20260714-003304` Education Failed exit 1 — Posted 3158 / Failed 18 (incomplete Institution/Specialty payloads); repo `education-institution.calik-energi.json` (1507 rows) lacked live labels (e.g. Lizabon, IHK Regensburg, Puerto-Riko, Jemgyyet ylymlary)
- **First seed attempt**: deployed repo calik-energi JSON + manifest bump 39 + ForceUpdate → PG counts 1507/1085 but same 18 failures (stale catalog vs live VISA2015)
- **Fix**: `generate-from-legacy.ps1` on `.25` — sqlcmd `10.100.128.15` / `VISA2015` DISTINCT Education institutions + specialties → overlay `C:\inetpub\visa2026-demo\LookupCatalogs\tenant\` + ForceUpdate (manifest 40) → **1512** institutions / **1089** specialties
- **Resume**: `-StartAt Education` RunId `20260714-005140` — Education **Completed** Posted **18** catch-up / Failed **0** (3158 already in id-map); continued to EmployeePositionHistory
- **Prevent**: for Demo PG greenfield, always regenerate tenant education/specialty JSON from live legacy before Import; repo calik-energi files can lag live `.15`


### 2026-07-14 — Demo PG AddressOfResidence SqlClient host; Watch DbCounts blank

- **Watch**: ViaSsh DbCount column empty — `Get-OnPremImportLiveSnapshot.ps1` always used `sqlcmd` / `Visa2026DbDemo` on SQLEXPRESS (DB gone / not PG). Fix: Demo detects `EFCoreProvider=Postgres` from `C:\inetpub\visa2026-demo\appsettings.Production.json` and uses `psql` → `visa2026_demo` (quoted table names). Sample now shows Person|3310 … AddressOfResidence|5148.
- **AddressOfResidence Failed(5148)**: every row `Keyword not supported: 'host'` — `TryMatchExistingAddressAsync` opened `SqlConnection` on Npgsql CS (site-match before Create). Also gated `Visa2014AddressOfResidenceSiteDuplicateGuard.LoadFromSqlAsync` for PG.
- **Fix**: skip site-match SqlClient path when `DatabaseProviderDetector.IsPostgreSql`; republish DI to `C:\visa2026-sync-demo`
- **Resume**: `-StartAt AddressOfResidence` RunId `20260714-005636` — **Completed** Posted **5148** / Failed **0** (incl. PIA-inferred 1125); continued to EmployeeSalary
- **Prevent**: any per-row target SqlClient helper fails loudly N times on Postgres; gate at entry. Watch must know Demo is PG after Express Demo removal.


### 2026-07-14 ? Demo PG ApplicationItem T-SQL bracket DELETE (RegistrationTravelHistorySync)

- **Symptom**: Run `20260714-005636` ApplicationItem ~61% ? sidecar **posted=49 fail=13319+**; PG `ApplicationItems`=0; `.err` `42601: syntax error at or near "["` on `DELETE FROM [TravelHistories] WHERE [SourceApplicationItemID] = ?`
- **Cause**: `RegistrationTravelHistorySyncService` `ExecuteSqlRaw` T-SQL runs on every ApplicationItem save (clear soft-deleted travel rows before unique index). Demo Import uses **`--inprocess`** OData on `:5012` ? fix must land in **`C:\visa2026-sync-demo\tools\DataImporter\Visa2026.Module.dll`**, not IIS alone.
- **Fix**: replace raw DELETE with EF `IgnoreQueryFilters()` + `RemoveRange`/`Remove` + `SaveChanges()` (provider-agnostic). Deploy Module DLL to DataImporter folder; restart `-StartAt ApplicationItem`.
- **Resume**: RunId `20260714-013121` ? after deploy **inserted 1091+ / failed 0** within first ~1100 rows (0 err lines).
- **Prevent**: any `ExecuteSqlRaw` with `[brackets]` breaks on Npgsql; prefer EF or quote/bracket translate like `Visa2026EFCoreDbContext.IndexFilter`. In-process import = DataImporter Module copy, not only `C:\inetpub\visa2026-demo`.

### 2026-07-22 — Local PG wipe reimport: Education Failed=1 (slash-compound NameTm) then resume OK
- **Context**: Full scalar chain .15 → local PG after wipe; Person/Passport/Visa OK; Education Posted 3189 / Failed 1 then chain stop.
- **Failing oid**: `3e713db3-2a3e-4913-a154-15eefd0fc005` — Institution `Balıkesir uniwersiteti/Sakarya uniwersiteti`, Specialty `Gurluşyk inženeri/Gurluşyk mugallymy`.
- **Cause**: Labels present in regenerated `education-institution.calik-energi.json` / `specialty.calik-energi.json`, but in-process import used **stale disk overlay** `DataImporter/bin/Debug/net8.0/LookupCatalogs/tenant/{education-institution,specialty}.json` (2026-07-15) — `LoadCatalogFile` prefers disk over embedded. `-SkipTenantCatalogGeneration` left overlay untouched; PG lacked both rows.
- **Fix**: Regenerated calik catalogs from `10.100.128.15` DISTINCT Education labels; copied to bin overlay; bumped tenant `manifest.json` **40→41**; INSERT missing Institution/Specialty with `GCRecord=0`; resume `-StartAt Education -SkipTenantCatalogGeneration` → Education catch-up OK, continued EPH/Salary/…
- **Practice**: After Education catalog regen, refresh **AppBase overlay** (or re-run tenant catalog generation from Person) and ensure rows exist in target DB before `-StartAt Education`. `GCRecord` NOT NULL on PG lookups.

### 2026-07-22 — Local PG full scalar resume completed (Education fix → Overall OK)
- **Resume**: `Run-LocalPgScalarChain.ps1 -StartAt Education -SkipTenantCatalogGeneration` after Education Institution/Specialty seed + overlay refresh.
- **Waves**: Education catch-up Posted **1**; EPH 3070; Salary 2963; AoR 5175; Application 12248; WP 406 / WPI 3860; Invitation 2857 / InvItem 5106; ApplicationItem **21676**; **Rejection 207 / RejectionItem 254**; ApplicationProgress ~30k (Failed 0, small skip count for missing maps).
- **Outcome**: chain finished with **EXIT=0** / Overall Completed (post-correction PIA address warnings for unmapped address keys remain; non-blocking).

### 2026-07-22 — ApplicationProgress PROCESS_REJECTED from Rejection coverage OR legacy Rejected
- **Rule**: `effectiveRejected = Application.Rejected=1 OR (ApplicationItemCount > 0 AND ApplicationItemCount == RejectionItemCount)`.
- **Legacy proxy**: `PersonInApplication` vs `PersonInInvitation` under `ApplicationResult.Result=1` (same as imported Rejection/RejectionItem).
- **Date**: prefer max Rejection `IssuedDate`; else `ProcessDate`; else prior step.
- **Code**: `Visa2014ApplicationProgressRejectionIndex` + `SynthesizeSteps(..., rejection)`; partial coverage does not reject; blocks `PROCESS_ISSUED`.
- **Docs**: discovery + field-maps ApplicationProgress terminal override notes updated.

### 2026-07-22 — ApplicationProgress clean reimport (local PG, rejection coverage)
- **Prep**: `ImportedApplicationProgress.postgres.sql` DELETE **30584**; clear ApplicationProgress id-map; build Debug.
- **First attempt**: failed — rejection index SQL `STRING_AGG … WITHIN GROUP` not supported on legacy host (Incorrect syntax near '('). Fixed: plain `STRING_AGG`.
- **Import**: Posted **30725** / Failed **0** / Skipped no-App-map **66** / Parent-skipped **169**; Prepared **30791**.
- **Evidence**: `PROCESS_REJECTED` steps in PG = **141** (coverage OR legacy Rejected).
- **Log**: `artifacts/local-pg-import/data/import-logs/reimport-ApplicationProgress-localpg-20260722-130503.log`
- **Next**: optional `--verify-visa2014-mapping --entity ApplicationProgress`.

### 2026-07-22 — Visa.IssuingApplicationItem post-pass (ProcessNumber + extension sibling)

- **Rule**: (1) Prefer legacy `Visa.ProcessNumber` → `PersonInApplication` (FK exists). (2) Else: next passport sibling by issued date after a previous visa that sat on an **extension** PIA (employee/FM subtype **7**); tie-break latest App date then PIA oid. Null if neither matches.
- **Why post-pass**: Visa wave runs before ApplicationItem; transform leaves `IssuingApplicationItem` null.
- **Code**: `Visa2014VisaIssuingApplicationItemIndex` (+ unit tests), `Visa2014VisaIssuingApplicationItemCorrection`, CLI `--correct-visa2014-issuing-application-item`; wired in `OnPrem-Sync.ps1` / `Run-HeadlessChain.ps1` / `order.yaml` postImportCorrections.
- **Local PG apply** (`calik-energi-local-pg` → `visa2026`): index **5923** links (processnumber=5887, extension_sibling=36); updated **5727**; no legacy link **243**; missing ApplicationItem map/target **132**; PG `Visas` linked **5727** / unlinked **375** / total **6102**.
- **Idempotent**: re-run should report Already correct for the same links.
- **Docs**: `field-maps/Visa.yaml`, `discovery/Visa.yaml` note the correction.
- **Not in scope**: `InvitationItem` on Visa (legacy has no Invitation FK).

### 2026-07-22 — ApplicationProgress PROCESS_ISSUED for extension subtype 7 (full visa coverage)

- **Rule**: For employee/FM subtype **7**, synthesize `PROCESS_ISSUED` when every non-deleted PIA has a `Visa.ProcessNumber` → that PIA. Inv/WP completion evidence still wins when present. Partial coverage does not issue. Cancelled/Rejected still block.
- **Date / Description**: `max(VisaIssuedDate)` / sample `VisaNumber` (`SourceLabel=VisaNumber`).
- **Code**: `Visa2014ApplicationProgressCompletionIndex` second SQL + merge; unit tests; field-map + discovery notes.
- **Local PG reimport**: DELETE ApplicationProgress **30725**; Posted **30794** / Failed **0** / Skipped no-App-map **66** / Parent-skipped **162**.
- **Completion index**: **4432** apps (invitation/work-permit=**4370**, visa-extension-added=**62**).
- **Log**: `reimport-ApplicationProgress-localpg-20260722-155408.log`

### 2026-07-22 — Family-member visas force VisaType FM (override legacy WP)

- **Problem**: Legacy often stores `WP:11` on visas for `IsFamilyMember` persons (e.g. `A1733149` / Serpil Demirbilek); import copied WP faithfully (~637 visas / 170 persons on Çalik).
- **Rule**: `Person.IsFamilyMember=1 AND IsEmployee=0` → `VisaType` LocalizationKey **FM** (before TypeOfVisaL:mgCode map). Employees unchanged.
- **Code**: `Visa2014VisaTransform.ResolveVisaTypeLocalizationKey`; `--correct-visa-type` reuses transform; wired in OnPrem-Sync / HeadlessChain postImportCorrections.
- **Local PG correction**: Visas in scope **6143**; updated **652**; already correct **5450**; histogram WP=3422, BS1=1651, FM=1000, OF=55, EX=15. `A1733149` → FM; FM-person still-WP → **0**.

### 2026-07-22 — IssuingApplicationItem: trust ProcessNumber only for extension subtype 7

- **Bug**: `A1733547` linked to invitation `6/-820` because sticky `ProcessNumber` beat extension sibling `6/-930`.
- **Rule**: (1) ProcessNumber→PIA when app is subtype **7**; (2) else extension sibling; (3) else remaining ProcessNumber (invitation etc.).
- **Local PG**: index processnumber=4967 / extension_sibling=956; correction updated **911**; `A1733547` → `6/-930`.

### 2026-07-23 — ApplicationProgress wipe + reimport (local PG)

- **Wipe**: DELETE **31019**; cleared `ApplicationProgress` id-map.
- **Import**: Posted **31019** / Failed **0** (log `reimport-ApplicationProgress-localpg-20260723-085454.log`).
- **Shape**: `PROCESS_ISSUED` = **4500** (was ~4275 before sibling extension completion).
- **Spot-check**: `6/-930` → `PROCESS_ISSUED` @ 2026-06-23 Description `A1733547`.

### 2026-07-24 — Path B hybrid: Visa.InvitationItem closest-match post-pass

- **Rule (hybrid)**: Keep `--correct-visa2014-issuing-application-item` (ProcessNumber/sibling). New `--correct-visa2014-invitation-item` runs after it: when IssuingApplicationItem type has `CanIssueInvitation`, closest unused InvitationItem under that app (same person; prefer IssuedDate > ApplicationDate; IssuedDate < Visa.IssueDate smallest gap; set IsUsed). No match → null, do not fail. Never calls Module Path A.
- **Code**: `Visa2014VisaInvitationItemLinkMatcher` (+ 5 unit tests), `Visa2014VisaInvitationItemCorrection`; wired in `Program.cs`, `order.yaml`, `OnPrem-Sync.ps1`, `Run-HeadlessChain.ps1`, `field-maps/Visa.yaml`. Doc §4.1 in `docs/VISA_ISSUING_APPLICATION_AND_INVITATION_LINK.md`.
- **Path A polish**: Visa `IssuingApplicationItem` / `InvitationItem` `AllowEdit=False` again.
- **Ops**: Dry-run after issuing-app correction, e.g. `dotnet run --project Visa2026.DataImporter --no-launch-profile -c Debug -- --correct-visa2014-invitation-item --legacy-source calik-energi-local-pg --target-connection "Host=localhost;Port=5432;Database=visa2026;Username=postgres;Password=***;EFCoreProvider=Postgres" --dry-run --verbose`. Requires Visa id-map + `CanIssue*` flags true on ApplicationTypes.
- **Build/tests**: DataImporter Debug build OK; matcher tests 5/5 passed. Full DB dry-run deferred until id-map present in this workspace.

### 2026-07-24 — IssuingApplicationItem: predecessor visa orientation (extension)

- **Rule**: When the new visa follows a prior visa on the same passport (extension/transfer), prefer the ApplicationItem that references that **predecessor** (`CurrentVisa` / legacy PIA.Visa).
- **Path B**: existing `extension_sibling` unchanged; added target-side fallback `target_predecessor` in `Visa2014VisaIssuingApplicationItemCorrection` when ProcessNumber/sibling leave IssuingApplicationItem null.
- **Path A**: `VisaIssuingLinkPathAMatcher` now prefers `ApplicationItem.CurrentVisa == predecessor` before unused-invitation / latest-app heuristics.
- **Docs**: §3.5 + §4.1.2 in `VISA_ISSUING_APPLICATION_AND_INVITATION_LINK.md`; Visa field-map + discovery notes.

### 2026-07-24 — Full scalar reimport .15 → local PG (started)

- **Phase**: wipe + scalar chain (Run-LocalPgScalarChain.ps1 -StartAt Person)
- **Source**: `10.100.128.15` / `VISA2015` (`calik-energi-local-pg`)
- **Target**: PostgreSQL `visa2026`
- **Prep**: `Wipe-LocalPostgresTransactional.sql`; cleared `id-maps/calik-energi-local-pg/*.json` to `{}`; App_Inv CanIssue* still true
- **Post-corrections**: includes VisaType + VisaIssuingApplicationItem + **VisaInvitationItem** (Path B)
- **Log**: `artifacts/local-pg-import/chain-console-from15-wipe-reimport-20260724-113332.log`
- **PID**: 28192
- **Watch**: `Watch-OnPremImportLive.ps1 -Profile Local -ClearScreen`

### 2026-07-24 — Visa wave failed: stale bin id-maps after wipe

- **Symptom**: Passport Posted **3** / Skipped already **3667**; Visa Posted **0** / Skipped already **6119** / Failed **1** (`FK Passport target ... not found`). DB Passports=3, Visas=0.
- **Cause**: wipe cleared `legacy/.../id-maps/calik-energi-local-pg` to `{}`, but DataImporter reads **copied** maps under `bin/Debug/net8.0/legacy/visa2014/id-maps/...` which still held prior run.
- **Fix**: reset bin (+ src) maps except current `Person.json`; truncate Passports; resume `Run-LocalPgScalarChain.ps1 -StartAt Passport -SkipTenantCatalogGeneration`.
- **Resume PID**: 20416  Log: `artifacts/local-pg-import/chain-console-from15-resume-passport-20260724-113618.log`

### 2026-07-24 — Education fail 1/3193: missing Palakkad institution (+ specialty)

- **ERR**: incomplete OData payload `EducationInstitution=Palakkad döwlet politehniki ýörite orta hünärmen mekdebi`; `Specialty=Barlag-ölçeg enjamlarynyň tehnologiýasy`
- **Fix**: INSERT both into local PG (`IsDefault=false`); append NameTm rows to `education-institution.calik-energi.json` / `specialty.calik-energi.json`
- **Resume**: `Run-LocalPgScalarChain.ps1 -StartAt Education -SkipTenantCatalogGeneration` PID 27680

### 2026-07-24 — Watch live table includes post-* Visa patch waves

- **Ask**: show Visa IssuingApplicationItem / InvitationItem (and other) post-corrections in `Watch-OnPremImportLive`.
- **Change**: `Run-LocalPgScalarChain.ps1` and `OnPrem-Sync.ps1` register `post-VisaType`, `post-VisaIssuingApplicationItem`, `post-VisaInvitationItem`, … in `sync-run-status.json` and call `Set-OnPremSyncRunWaveStarted/Completed` with teed logs.
- **Stats**: `Get-OnPremWaveLogStats` parses `INF … updated:` and `Visas in id-map/scope`.
- **Note**: already-running chain process still uses the old script body for posts — watch post rows apply on the **next** chain start.

### 2026-07-24 — Visa.ProcessNumber (legacy PIA Oid) property + import

- **Legacy**: `Visa.ProcessNumber` is `IPersonInApplication` (uniqueidentifier FK to PersonInApplication), not Application belgi string.
- **Target**: `Visa.ProcessNumber` `Guid?` on Visa2026; schema updater `VisaProcessNumberSchemaUpdater`; read-only UI.
- **Import**: scalar Visa transform/OData POST includes ProcessNumber; `--correct-visa2014-issuing-application-item` also backfills Guid for already-imported visas.
- **Distinct**: `IssuingApplicationItem` remains domain ApplicationItem link; ProcessNumber keeps legacy PIA Oid for lineage.
- **Next**: restart Blazor (schema), re-run issuing correction (or Visa reimport) to fill existing rows.

### 2026-07-24 — Visa.ProcessNumber = legacy ASNumber (Işlenen belgisi), not PIA Guid

- **UI**: Legacy Visa DetailView first field `Işlenen belgisi` is `ASNumber` (string e.g. C00138718) typed from stamp image.
- **Correction**: `Visa.ProcessNumber` is now `string?` ← ASNumber; `LegacyPersonInApplicationOid` `Guid?` ← legacy ProcessNumber PIA FK.
- **Schema**: rename mistaken uuid ProcessNumber → LegacyPersonInApplicationOid when present; add varchar ProcessNumber.
- **Backfill**: issuing-application-item correction fills both ASNumber and PIA Oid for existing imports.

### 2026-07-30 — Local PG file waves resumed (VisaDocument → FamilyProof)

- **Profile**: `calik-energi-local-pg` → local PG `visa2026`; legacy read `10.100.128.15` / `VISA2015`.
- **Resume**: `DocumentCopies.ps1 -StartAt VisaDocument` (port `5012` via `VISA2026_MIGRATION_IMPORT_URLS`).
- **Outcome (this session)**:
  - **VisaDocument**: 5932 already imported; 154 oversize (>5MB) skipped; 0 new posted.
  - **EducationDocument**: 4312 posted; 40 oversize; 34 no blob.
  - **WorkPermitDocument**: 1004 posted; 2 no blob.
  - **InvitationDocument**: 2960 posted; 209 no parent map; 4 no blob.
  - **FamilyProofDocument**: 436 posted (9 PersonDocument + 427 PersonFamilyRelationDocument); 1 oversize; 3 duplicate blob.
- **Id-map counts (bin)**: PassportCopy 3663, VisaDocument 5932, EducationDocument 4322, WorkPermitDocument 1014, InvitationDocument 2969, FamilyProofDocument 446.
- **Failures / workarounds**:
  - **WorkPermit start**: `DevExtreme.AspNet.Data` missing in DataImporter `bin` — copy from `Visa2026.Blazor.Server\bin\Debug\net8.0\` (DocumentCopies build uses `/p:BuildProjectReferences=false`).
  - **Invitation / FamilyProof after prior step**: `dotnet run --no-build` → `hostpolicy.dll` not found; `Visa2026.DataImporter.runtimeconfig.json` absent after each step. **Fix**: rebuild DataImporter before next step, or invoke `Visa2026.DataImporter.exe` directly for single wave.
  - **Full solution build** blocked when F5 **Visa2026.Blazor.Server** holds DLL locks — use DataImporter-only build or stop F5 first.
- **Watch**: `Watch-OnPremImportLive.ps1 -Profile Local` reads `artifacts/local-pg-import/file-waves-status.json`.

### 2026-08-11 — Application Profile patch skip analysis (Wave 0b, local PG)

- **Symptom**: `--patch-visa2014-application-profile` skipped **5103** apps (`TYPE_ONLY_GAP` for registration types: `check_in`, `check_out`, `check_in_info_change`).
- **Cause**: `NameLooksLikeContractVariant` treated Turkmen titles with descriptive parentheses (e.g. `Hasaba Almak (Daşary ýurtdan…)`) as contract-variant suffixes; type-only matcher excluded valid profiles.
- **Fix**: `ApplicationProfileCatalogGrouping.NameLooksLikeContractVariant` — contract suffix = short inner text **without spaces**; patch `--skip-report` histogram in `Visa2014ApplicationProfilePatch.cs`.
- **Verify**: Dry-run local `visa2026` → Skipped (no profile) **0**; Already correct **7520**; Patched **4754**. Doc: `docs/VISA2014_MIGRATION/analysis/APPLICATION_PROFILE_PATCH_SKIP_ANALYSIS.md`

### 2026-09-07 — Visa scan corpus profiled on `.15` (discovery only; feeds document field extraction plan)

- **Scope**: read-only profiling of legacy visa copies to judge feasibility of AI/OCR field extraction in Visa2026. **No import, no writes.**
- **Source**: `10.100.128.15` / `VISA2015`, blob column **`dbo.Visa.Göçürme Nusga`** (`varbinary(max)`), stored **inline on the visa row** — not in `dbo.Copy` / `dbo.PassportCopy`. Confirms the `discovery/Visa.yaml` note. MCP `visa2014-sql-remote` was **not loaded** this session; used `System.Data.SqlClient` from PowerShell with `ReadOnlyUser` + `$env:SQL_SERVER_10.100.128.15`.
- **Inventory** (`GCRecord IS NULL`): **6,392** active rows, **6,327 with a scan** (99.0%); min **51 KB**, avg **1.2 MB**, max **25.7 MB**, total **7.4 GB**.
- **Formats**: **100% raster** — JPEG/JFIF **5,655** (89.4%), PNG **672** (10.6%), **zero PDFs**. So the Spire PDF **text-layer** path in `ScanOcrExtractor` is useless for visa copies; raster OCR is the only route.
- **Resolution** (400-row random sample; dims parsed from JPEG SOF0 / PNG IHDR, only first 64 KB fetched per row): short side **<500 px 3.5%** (unusable), 500–799 px 6.3%, 800–1199 px 28.5%, 1200–1899 px 43.5%, ≥1900 px 18.3%. p50 **2.96 MPix**. ⇒ **~90% workable**, ~10% marginal/unusable. Small PNGs (~250–700 px) are the unusable cluster.
- **Content**: every sample is a **Turkmenistan visa sticker** on a passport page — i.e. this system's **own issued output** — a fixed-layout bilingual form (PLACE OF ISSUE, DATE OF ISSUE, DOCUMENT NUMBER, VALID FROM, VALID UNTIL, DURATION OF STAY, TYPE OF VISA, NUMBER OF ENTRIES, ÇAKYLYK/INVITATION, SURNAME, GIVEN NAMES, PASSPORT No, SEX, DATE OF BIRTH, CITIZENSHIP). Machine-printed, not handwritten ⇒ anchor/template extraction is viable, not just free-form OCR.
- **MRZ present** on the sticker (2 lines). **Line 2 is standard MRV-B with check digits**: doc no(9)+cd, nationality(3), DOB(6)+cd, sex, expiry(6)+cd. **Line 1 is Turkmenistan-specific and has two generations**: old **`V{TYPE2}<SURNAME<<GIVEN`** (2014–2018 samples) vs new **`{TYPE2}TKM…`** (2024–2026 samples, e.g. `WPTKMSENTURK<<IBRAHIM`, `BSTKM…`). A stock ICAO MRZ library will **mis-parse line 1** — custom parser required. `TYPE2` matches the legacy `VisaType` codes (`WP`, `BS`).
- **Ground truth on the same row**: `VisaNumber`, `ASNumber`, `VisaIssuedDate`, `VisaStartDate`, `VisaEndDate`, `VisaType` / `VisaCategory` / `VisaIssuedPlace` FKs. ⇒ the 6,327 scans are a **labelled benchmark**; extraction accuracy can be measured at scale with **no manual labelling**.
- **Verified 2/2 spot checks** (`A0982228` 2015 flatbed, `A1607916` 2025 photo): MRZ document number, DOB, sex and expiry matched the printed zone **and** the DB columns exactly.
- **Normalization gap**: invitation number is printed with a hyphen (`AS-515666`) but stored without (`ASNumber = AS515666`); newer series is `CO…` (`CO0167186`). Strip separators before comparing.
- **OCR hazards**: (1) red arrival **rubber stamp overlaps the MRZ / data fields** on many stickers — red ink over black text, so **red-channel suppression** is a cheap, high-value preprocessing step; (2) a minority are **phone photos** with content rotation, page curvature and hologram glare — note **frame aspect ≠ content rotation** (99.3% of frames are portrait, yet sticker content can still be sideways).
- **Cross-ref**: explains the **154 oversize (>5 MB) `VisaDocument` skips** in the 2026-07-30 file wave — max blob is 25.7 MB.
- **Outcome**: `docs/DOCUMENT_SCAN_FIELD_EXTRACTION.md` updated with an evidence section + slice corrections; legacy corpus proposed as the accuracy benchmark for the OCR decision gate.
- **PII**: sample images written to **`%TEMP%\visa-legacy-scan-sample`** only — never into the repo.

### 2026-09-07 — Invitation / WorkPermit / Education / Passport copies profiled (`dbo.PassportCopy`)

- **Scope**: same read-only profiling as the visa entry above, extended to header-document copies. **No import, no writes.**
- **`dbo.PassportCopy` is the shared scan table** for four owners (blob column **`Göçürme`**, `varbinary(max)`), not passport-only:

  | Owner FK column | References |
  |---|---|
  | `Implicit_IWorkPermitLetter_IsRugsatnamaGocurme` | `dbo.WorkPermitLetter` |
  | `Implicit_IApplicationResult_NetijeGocurme` | `dbo.ApplicationResult` (`Result=0` Invitation · `Result=1` Rejection) |
  | `Education` | `dbo.Education` |
  | `Passport` | `dbo.Passport` |

- **Corrects a stale dossier note**: `discovery/WorkPermit.yaml` says "WorkPermitDocument / WorkPermitImage — no dbo scan table found". They **are** in `PassportCopy` via the `Implicit_IWorkPermitLetter_…` FK. `dbo.Visa` keeps its **own** inline blob (previous entry) — it is *not* in `PassportCopy`.
- **Inventory** (`GCRecord IS NULL`):

  | Owner | rows | with blob | min | avg | max | total |
  |---|---|---|---|---|---|---|
  | Education | 4,500 | 4,466 | 18 KB | 881 KB | **16.1 MB** | 3.8 GB |
  | Passport | 3,799 | 3,798 | 28 KB | 734 KB | **18.5 MB** | 2.7 GB |
  | Invitation (`Result=0`) | 3,092 | 3,087 | 29 KB | 715 KB | 3.0 MB | 2.1 GB |
  | WorkPermitLetter | 1,024 | 1,022 | 67 KB | 1.01 MB | 4.3 MB | 1.0 GB |
  | Rejection (`Result=1`) | 164 | 163 | 97 KB | 328 KB | 2.7 MB | 52 MB |

- **Formats: still zero PDF anywhere.** JPEG dominates (Education 4,408 · Passport 3,677 · Invitation 3,026 · WorkPermit 1,004); PNG is a small minority; plus **TIFF-LE ×3** (2 Education, 1 Passport) and **BMP ×4** (1 Education, 3 Passport) — so TIFF/BMP handling is not hypothetical. Combined with `dbo.Visa`, the whole legacy corpus is ~**18.9k scans, 0 PDFs**.
- **Pages per owner** (copies are sibling rows, one per page):

  | Owner | avg | distribution |
  |---|---|---|
  | **WorkPermitLetter** | **2.53** | **never 1 page** — 2p×281, 3p×73, 4p×28, 5p×10, 6p×6, 7p×5, 10p×1 |
  | Education | 1.72 | 1p×1258, **2p×1129**, 3p×85, 4p×95, … 15p×1 |
  | Invitation | 1.05 | 1p×2982 (95.7%), 2p×125, 3p×8 |
  | Passport | 1.01 | 1p×3741, 2p×23, 3p×4 |

- **Invitation copy = A4 typed letterhead** (State Migration Service `ÇAKYLYK - INVITATION`), 2480×3507 (300 DPI) or 1654×2340 (200 DPI). **Fixed layout stable 2017 → 2026** with only minor label wording drift (`BS` vs `BS1`, `1 AY (MONTH(s))` vs `1 AY (Month)`) ⇒ anchor on the Turkmen labels, **not** exact strings or fixed pixel boxes. **All four DB header fields appear verbatim**: `Number` (Çakylygyň belgisi), `IssuedDate` (Resmileşdirilen senesi), `DateOfExpire` ("… çenli güýjüni saklaýar"), `ASBelgisi` (bottom right). Also carries lookup-resolvable `type of visa` (BS1), `number of entries` (Iki gezeklik → Double), `visa period` (1 AY → Month1), `border zone` (Ýok → None), **and an invited-person table** (surname/name, sex, DOB, passport type+no, citizenship) that maps to `InvitationItem`. ⇒ **the easiest OCR target in the corpus.**
- **Legacy data gap found**: `inv2` (`ASGH457223`, 2017) has **empty `ASBelgisi` in the DB** but the scan clearly prints `AS-621785`. Extraction could backfill missing legacy scalars from the copies.
- **WorkPermit letter = the hardest target.** Page 1 is the `RUGSATNAMA` letter at 3310×4704 (~400 DPI) whose **number and date are HANDWRITTEN** (`« 01 » 04 201 4 ý.`, `№ 769`) — printed-text OCR will not read these; handwriting recognition is a materially weaker problem. Pages 2..n are the annexed roster (`SANAWY`) → `WorkPermitItem`, a hard table: **merged `AS №` cells spanning rows**, multi-line cells (DOB + citizenship stacked), **two dates in one cell** (permitted from/until), and blue stamps overlapping the last rows. The letter states its own annex length ("2 sahypadan ybarat"), which explains the never-single-page distribution.
- **Consequences recorded in `docs/DOCUMENT_SCAN_FIELD_EXTRACTION.md`**: (1) a logical document can be **N sibling files**, not one file, so the input normalizer needs a set-level entry point; (2) **table → child-row extraction** (`InvitationItem` / `WorkPermitItem`) is a distinct new capability, explicitly out of scope for the scalar slices; (3) WorkPermit stays **verification-only** with page-1 number/date treated as officer-entered.
- **PII**: samples written to `%TEMP%\visa-legacy-hdr-sample` only, then deleted; nothing in the repo..
