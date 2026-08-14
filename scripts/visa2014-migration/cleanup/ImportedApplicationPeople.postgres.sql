-- Delete skip-navigation People join (+ ResolvedLinks) for local PG reimport (Wave 2b).
-- Join table has no roster-line ID. Keeps ApplicationProfileInstance headers and People.

BEGIN;

DELETE FROM "ApplicationProfileInstancePersonResolvedLinks" rl
USING "ApplicationProfileInstances" a
WHERE rl."ApplicationProfileInstanceId" = a."ID"
  AND a."IsManualEntry" = TRUE
  AND (a."GCRecord" IS NULL OR a."GCRecord" = 0);

DELETE FROM "ApplicationProfileInstancePeople" ap
USING "ApplicationProfileInstances" a
WHERE ap."ApplicationProfileInstanceId" = a."ID"
  AND a."IsManualEntry" = TRUE
  AND (a."GCRecord" IS NULL OR a."GCRecord" = 0);

COMMIT;

SELECT COUNT(*) AS RemainingApplicationPeople
FROM "ApplicationProfileInstancePeople" ap
INNER JOIN "ApplicationProfileInstances" a ON a."ID" = ap."ApplicationProfileInstanceId"
WHERE a."IsManualEntry" = TRUE
  AND (a."GCRecord" IS NULL OR a."GCRecord" = 0);
