-- Merge ProjectContract typo Kobmine → Kombine (keep correct spelling).
-- Local PostgreSQL visa2026. Preview: set @apply false conceptually — this file is APPLY.
-- Keep:  KYC (Kiyanlı Kombine Elektrik Santrali projesi)
-- Extra: KYC (Kiyanlı Kobmine Elektrik Santrali projesi)

BEGIN;

SELECT pc."ID", pc."NameTm",
       (SELECT COUNT(*) FROM "People" p WHERE p."ProjectContractID" = pc."ID" AND (p."GCRecord" IS NULL OR p."GCRecord" = 0)) AS person_refs
FROM "ProjectContracts" pc
WHERE ("GCRecord" IS NULL OR "GCRecord" = 0) AND pc."NameTm" LIKE 'KYC (%'
ORDER BY pc."NameTm";

WITH ids AS (
  SELECT
    (SELECT "ID" FROM "ProjectContracts"
     WHERE ("GCRecord" IS NULL OR "GCRecord" = 0)
       AND "NameTm" = 'KYC (Kiyanlı Kombine Elektrik Santrali projesi)' LIMIT 1) AS keep_id,
    (SELECT "ID" FROM "ProjectContracts"
     WHERE ("GCRecord" IS NULL OR "GCRecord" = 0)
       AND "NameTm" = 'KYC (Kiyanlı Kobmine Elektrik Santrali projesi)' LIMIT 1) AS extra_id
)
SELECT keep_id, extra_id FROM ids;

DO $$
DECLARE
  keep_id uuid;
  extra_id uuid;
BEGIN
  SELECT "ID" INTO keep_id FROM "ProjectContracts"
  WHERE ("GCRecord" IS NULL OR "GCRecord" = 0)
    AND "NameTm" = 'KYC (Kiyanlı Kombine Elektrik Santrali projesi)' LIMIT 1;
  SELECT "ID" INTO extra_id FROM "ProjectContracts"
  WHERE ("GCRecord" IS NULL OR "GCRecord" = 0)
    AND "NameTm" = 'KYC (Kiyanlı Kobmine Elektrik Santrali projesi)' LIMIT 1;

  IF keep_id IS NULL THEN
    RAISE EXCEPTION 'Keeper Kombine row not found';
  END IF;
  IF extra_id IS NULL THEN
    RAISE NOTICE 'Extra Kobmine row already gone — nothing to merge';
    RETURN;
  END IF;

  DELETE FROM "UserReportTemplateProjectContracts" u
  WHERE u."ProjectContractId" = extra_id
    AND EXISTS (
      SELECT 1 FROM "UserReportTemplateProjectContracts" k
      WHERE k."UserReportTemplateId" = u."UserReportTemplateId"
        AND k."ProjectContractId" = keep_id
    );

  UPDATE "People" SET "ProjectContractID" = keep_id WHERE "ProjectContractID" = extra_id;
  UPDATE "Applications" SET "ProjectContractID" = keep_id WHERE "ProjectContractID" = extra_id;
  UPDATE "UserReportTemplateProjectContracts" SET "ProjectContractId" = keep_id WHERE "ProjectContractId" = extra_id;

  UPDATE "ProjectContractApprovalLegProfiles" SET "GCRecord" = 1
  WHERE "ProjectContractId" = extra_id AND ("GCRecord" IS NULL OR "GCRecord" = 0);
  UPDATE "ProjectContractDocuments" SET "GCRecord" = 1
  WHERE "ProjectContractID" = extra_id AND ("GCRecord" IS NULL OR "GCRecord" = 0);
  UPDATE "ProjectContractImages" SET "GCRecord" = 1
  WHERE "ProjectContractID" = extra_id AND ("GCRecord" IS NULL OR "GCRecord" = 0);

  UPDATE "ProjectContracts" SET "GCRecord" = 1
  WHERE "ID" = extra_id AND ("GCRecord" IS NULL OR "GCRecord" = 0);

  RAISE NOTICE 'Merged Kobmine % into Kombine %', extra_id, keep_id;
END $$;

SELECT "ProjectName", COUNT(*) AS cnt
FROM vw_rd_visa_by_period
WHERE NOT "IsArchived" AND "ProjectName" LIKE 'KYC (%'
GROUP BY "ProjectName";

COMMIT;