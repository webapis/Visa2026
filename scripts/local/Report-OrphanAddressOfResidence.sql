-- Orphan AddressOfResidence report (PostgreSQL).
-- Person-owned AoRs with no OwningApplicationProfileInstance and no check-in-linked instance.
-- Do NOT delete until signed off.

SELECT
  a."ID" AS address_id,
  a."PersonID" AS legacy_person_id,
  p."FullName" AS person_name,
  a."Type",
  a."FullAddress",
  a."CityID",
  (SELECT COUNT(*) FROM "ApplicationProfileInstancePersonResolvedLinks" l
   WHERE l."LinkedObjectId" = a."ID" AND (l."GCRecord" IS NULL OR l."GCRecord" = 0)) AS resolved_link_count,
  (SELECT COUNT(*) FROM "ApplicationProfileInstancePeople" ip
   WHERE ip."PersonId" = a."PersonID") AS person_case_count
FROM "AddressesOfResidence" a
LEFT JOIN "People" p ON p."ID" = a."PersonID"
WHERE (a."GCRecord" IS NULL OR a."GCRecord" = 0)
  AND a."OwningApplicationProfileInstanceId" IS NULL
  AND a."PersonID" IS NOT NULL
ORDER BY p."FullName", a."ID";