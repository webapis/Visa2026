-- Preview near-duplicate ProjectContract / Subcontractor on Visa2026 PostgreSQL (local import).
-- Read-only. Used by Preview-DuplicateProjectContractSubcontractor.ps1 -Profile Local.

\pset border 2
\pset format aligned

\echo === ProjectContract family tokens (leading word before space/paren) ===
WITH active AS (
  SELECT "ID", btrim("NameTm") AS name_tm,
         lower(split_part(regexp_replace(btrim("NameTm"), '[\(\)].*$', ''), ' ', 1)) AS token
  FROM "ProjectContracts"
  WHERE ("GCRecord" IS NULL OR "GCRecord" = 0) AND NULLIF(btrim("NameTm"), '') IS NOT NULL
),
tok AS (
  SELECT token FROM active WHERE token <> '' GROUP BY token HAVING COUNT(*) > 1
)
SELECT a.token AS family_token, a."ID"::text AS id, a.name_tm,
       (SELECT COUNT(*) FROM "People" p WHERE p."ProjectContractID" = a."ID" AND (p."GCRecord" IS NULL OR p."GCRecord" = 0)) AS person_refs,
       (SELECT COUNT(*) FROM "Applications" app WHERE app."ProjectContractID" = a."ID" AND (app."GCRecord" IS NULL OR app."GCRecord" = 0)) AS app_refs,
       (SELECT COUNT(*) FROM vw_rd_visa_by_period v WHERE NOT v."IsArchived" AND v."ProjectNameTm" = a.name_tm) AS active_visa_rows
FROM active a
JOIN tok t ON t.token = a.token
ORDER BY a.token, person_refs DESC, a.name_tm;

\echo
\echo === Subcontractor Calik / Calyk / Calik variants ===
SELECT "ID"::text AS id, "NameTm", "IsDefault",
       encode(convert_to("NameTm", 'UTF8'), 'hex') AS nametm_hex,
       (SELECT COUNT(*) FROM "People" p WHERE p."SubcontractorID" = s."ID" AND (p."GCRecord" IS NULL OR p."GCRecord" = 0)) AS person_refs
FROM "Subcontractors" s
WHERE ("GCRecord" IS NULL OR "GCRecord" = 0)
  AND (
    lower("NameTm") LIKE '%alyk%ener%'
    OR lower("NameTm") LIKE '%alık%ener%'
    OR lower("NameTm") LIKE '%alik%ener%'
    OR lower("NameTm") LIKE 'calik%'
  )
ORDER BY person_refs DESC, "NameTm";

\echo
\echo Preview complete — no rows modified.