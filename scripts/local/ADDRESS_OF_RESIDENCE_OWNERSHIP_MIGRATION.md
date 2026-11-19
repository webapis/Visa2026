# AddressOfResidence ownership migration

Addresses are **case-owned** (`OwningApplicationProfileInstance` + optional `RosterPerson`).
Person DetailView address tab and Person intake residence step are removed.

## Schema

- `AddressesOfResidence.PersonID` nullable (legacy dual-read).
- `OwningApplicationProfileInstanceId`, `RosterPersonId`
- `ApplicationProfileInstances.Address1Id` / `Address2Id`
- Profile `RequireAddress1` / `RequireAddress2` (obsolete `RequirePersonAddressOfResidence` dual-read)

**Person required on AddressOfResidence is already dropped** (nullable PersonID via `EnsureAddressOfResidencePersonNullablePostgres`).

## Scripts (PostgreSQL)

1. Apply schema via app startup / `ApplicationProfileSchemaSql.ApplyIfMissing`.
2. Review: `scripts/local/Report-OrphanAddressOfResidence.sql`
3. Backfill check-in-linked: `scripts/local/Backfill-AddressOfResidenceOwnership.sql`
   - Verify `LinkKind` enum value for AddressOfResidence in your DB before running.
4. Re-run orphan report; quarantine leftovers — no silent delete.

## Import follow-up

VISA2014 address import should become instance/roster-scoped in a later wave (document in visa2014-to-visa2026-import learnings when implemented).