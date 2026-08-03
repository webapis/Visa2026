-- Undo aggressive soft-deletes from prior Address City heal experiments.
BEGIN;
UPDATE "Cities"
SET "GCRecord" = 0
WHERE "GCRecord" IN (999001, 999002);
COMMIT;

SELECT count(*) FILTER (WHERE "GCRecord" IS NULL OR "GCRecord"=0) AS active_cities,
       count(*) FILTER (WHERE "GCRecord" IN (999001, 999002)) AS still_soft
FROM "Cities";
