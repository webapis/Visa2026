-- AddressOfResidence ownership shift: check-in-linked backfill (PostgreSQL).
-- Prefer pinning instance Address1 (and Address2 when profile max was 2 / RequireAddress2)
-- from the person's linked / then-current addresses; re-own AoR rows onto the instance.
-- Review orphan report before deleting anything.
--
-- Safe to re-run: skips instances that already have Address1Id.

BEGIN;

-- 1) Re-own AoRs that are already pinned via ResolvedLinks onto a case.
UPDATE "AddressesOfResidence" a
SET
  "OwningApplicationProfileInstanceId" = l."ApplicationProfileInstanceId",
  "RosterPersonId" = CASE
      WHEN a."ID" = i."Address1Id" OR a."ID" = i."Address2Id" THEN NULL
      ELSE l."PersonId"
    END
FROM "ApplicationProfileInstancePersonResolvedLinks" l
JOIN "ApplicationProfileInstances" i ON i."ID" = l."ApplicationProfileInstanceId"
WHERE l."LinkKind" = 3 -- AddressOfResidence enum value; verify if different in your DB
  AND l."LinkedObjectId" = a."ID"
  AND (l."GCRecord" IS NULL OR l."GCRecord" = 0)
  AND (a."GCRecord" IS NULL OR a."GCRecord" = 0)
  AND a."OwningApplicationProfileInstanceId" IS NULL;

-- 2) For check-in style instances missing Address1, take the person's newest AoR
--    that is linked to this instance (or legacy Person-owned), prefer LinkSlotIndex=1.
WITH candidates AS (
  SELECT
    i."ID" AS instance_id,
    COALESCE(
      (SELECT l."LinkedObjectId"
       FROM "ApplicationProfileInstancePersonResolvedLinks" l
       WHERE l."ApplicationProfileInstanceId" = i."ID"
         AND l."LinkKind" = 3
         AND l."LinkedObjectId" IS NOT NULL
         AND (l."GCRecord" IS NULL OR l."GCRecord" = 0)
       ORDER BY CASE WHEN l."LinkSlotIndex" = 1 THEN 0 ELSE 1 END, l."LinkSlotIndex", l."ID"
       LIMIT 1),
      (SELECT a."ID"
       FROM "AddressesOfResidence" a
       JOIN "ApplicationProfileInstancePeople" p
         ON p."PersonId" = a."PersonID"
        AND p."ApplicationProfileInstanceId" = i."ID"
       WHERE (a."GCRecord" IS NULL OR a."GCRecord" = 0)
       ORDER BY a."ID" DESC
       LIMIT 1)
    ) AS address_id
  FROM "ApplicationProfileInstances" i
  JOIN "ApplicationProfiles" ap ON ap."ID" = i."ApplicationProfileID"
  WHERE i."Address1Id" IS NULL
    AND (ap."RequireAddress1" = true OR ap."RequirePersonAddressOfResidence" = true
         OR ap."Code" ILIKE '%check_in%' OR ap."Code" ILIKE '%checkin%')
)
UPDATE "ApplicationProfileInstances" i
SET "Address1Id" = c.address_id
FROM candidates c
WHERE i."ID" = c.instance_id
  AND c.address_id IS NOT NULL
  AND i."Address1Id" IS NULL;

UPDATE "AddressesOfResidence" a
SET
  "OwningApplicationProfileInstanceId" = i."ID",
  "RosterPersonId" = NULL
FROM "ApplicationProfileInstances" i
WHERE i."Address1Id" = a."ID"
  AND a."OwningApplicationProfileInstanceId" IS NULL;

-- 3) Address2 when RequireAddress2 / legacy max >= 2 and a second linked AoR exists.
WITH slot2 AS (
  SELECT
    i."ID" AS instance_id,
    l."LinkedObjectId" AS address_id
  FROM "ApplicationProfileInstances" i
  JOIN "ApplicationProfiles" ap ON ap."ID" = i."ApplicationProfileID"
  JOIN "ApplicationProfileInstancePersonResolvedLinks" l
    ON l."ApplicationProfileInstanceId" = i."ID"
   AND l."LinkKind" = 3
   AND l."LinkSlotIndex" = 2
   AND l."LinkedObjectId" IS NOT NULL
   AND (l."GCRecord" IS NULL OR l."GCRecord" = 0)
  WHERE i."Address2Id" IS NULL
    AND (ap."RequireAddress2" = true OR COALESCE(ap."PersonAddressOfResidenceMaxCount", 1) >= 2)
)
UPDATE "ApplicationProfileInstances" i
SET "Address2Id" = s.address_id
FROM slot2 s
WHERE i."ID" = s.instance_id
  AND s.address_id IS NOT NULL
  AND i."Address2Id" IS NULL
  AND (i."Address1Id" IS NULL OR i."Address1Id" <> s.address_id);

UPDATE "AddressesOfResidence" a
SET
  "OwningApplicationProfileInstanceId" = i."ID",
  "RosterPersonId" = NULL
FROM "ApplicationProfileInstances" i
WHERE i."Address2Id" = a."ID"
  AND (a."OwningApplicationProfileInstanceId" IS NULL OR a."RosterPersonId" IS NOT NULL);

COMMIT;