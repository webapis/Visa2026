-- Report Dashboard: Oturum sayısı — PostgreSQL.
-- Employees with active work permit and active valid visa (one person once).
-- Rows = department (PROJECT). Columns = region + kamp(Lodging) / adres.
DROP VIEW IF EXISTS vw_rd_registration_oturum_sayisi;
CREATE VIEW vw_rd_registration_oturum_sayisi AS
WITH people AS (
    SELECT
        a."PersonOid",
        COALESCE(NULLIF(BTRIM(dept."Name"), ''), '(No department)') AS "DepartmentLabel",
        CASE
            WHEN COALESCE(reg."LocalizationKey", '') = 'BN'
              OR COALESCE(reg."NameTm", '') ILIKE '%Balkan%'
              OR COALESCE(reg."Name", '') ILIKE '%Balkan%' THEN 'BN'
            WHEN COALESCE(reg."LocalizationKey", '') = 'AS'
              OR COALESCE(reg."NameTm", '') ILIKE '%Aşgabat%'
              OR COALESCE(reg."NameTm", '') ILIKE '%Asgabat%'
              OR COALESCE(reg."Name", '') ILIKE '%Ashgabat%' THEN 'AS'
            WHEN COALESCE(reg."LocalizationKey", '') = 'AH'
              OR COALESCE(reg."NameTm", '') ILIKE '%Ahal%'
              OR COALESCE(reg."Name", '') ILIKE '%Ahal%' THEN 'AH'
            WHEN COALESCE(reg."LocalizationKey", '') = 'LB'
              OR COALESCE(reg."NameTm", '') ILIKE '%Lebap%'
              OR COALESCE(reg."Name", '') ILIKE '%Lebap%' THEN 'LB'
            WHEN COALESCE(reg."LocalizationKey", '') = 'MR'
              OR COALESCE(reg."NameTm", '') ILIKE '%Mary%'
              OR COALESCE(reg."Name", '') ILIKE '%Mary%' THEN 'MR'
            ELSE NULL
        END AS "RegionCode",
        CASE WHEN addr."Type" = 0 THEN TRUE ELSE FALSE END AS "IsKamp"
    FROM vw_rd_work_permit_active a
    INNER JOIN "People" p
        ON p."ID" = a."PersonOid" AND COALESCE(p."GCRecord", 0) = 0
    INNER JOIN vw_rd_visa_active_by_project v
        ON v."PersonOid" = a."PersonOid"
       AND COALESCE(v."IsOneLastValidPerPerson", FALSE) = TRUE
       AND COALESCE(v."IsArchived", FALSE) = FALSE
    LEFT JOIN LATERAL (
        SELECT COALESCE(NULLIF(BTRIM(dep."NameTm"), ''), NULLIF(BTRIM(dep."Name"), ''), '') AS "Name"
        FROM "People" dept_person
        LEFT JOIN "People" dept_sponsor
            ON dept_sponsor."ID" = dept_person."SponsoringEmployeeID"
           AND COALESCE(dept_sponsor."GCRecord", 0) = 0
        INNER JOIN "EmployeePositionHistories" dept_eph
            ON dept_eph."PersonID" = CASE
                WHEN dept_person."PersonRole" = 1 AND dept_sponsor."ID" IS NOT NULL THEN dept_sponsor."ID"
                ELSE dept_person."ID"
            END
           AND COALESCE(dept_eph."GCRecord", 0) = 0
        LEFT JOIN "Departments" dep
            ON dep."ID" = dept_eph."DepartmentID"
           AND COALESCE(dep."GCRecord", 0) = 0
        WHERE dept_person."ID" = p."ID"
        ORDER BY
            CASE
                WHEN dept_eph."EndDate" IS NULL OR (dept_eph."EndDate")::date >= CURRENT_DATE THEN 0
                ELSE 1
            END,
            dept_eph."StartDate" DESC NULLS LAST,
            dept_eph."ID" DESC
        LIMIT 1
    ) dept ON TRUE
    LEFT JOIN LATERAL (
        SELECT ad."Type", ad."RegionID"
        FROM "AddressesOfResidence" ad
        WHERE ad."PersonID" = p."ID"
          AND COALESCE(ad."GCRecord", 0) = 0
        ORDER BY
            CASE
                WHEN ad."ExpirationDate" IS NULL OR (ad."ExpirationDate")::date >= CURRENT_DATE THEN 0
                ELSE 1
            END,
            ad."ExpirationDate" DESC NULLS FIRST,
            ad."ID" DESC
        LIMIT 1
    ) addr ON TRUE
    LEFT JOIN "Regions" reg
        ON reg."ID" = addr."RegionID" AND COALESCE(reg."GCRecord", 0) = 0
    WHERE COALESCE(a."PersonRoleCode", 0) = 0
      AND COALESCE(a."IsArchived", FALSE) = FALSE
),
counted AS (
    SELECT
        p."DepartmentLabel",
        COUNT(*) FILTER (WHERE p."RegionCode" = 'BN' AND p."IsKamp")::int AS "BalkanKampCount",
        COUNT(*) FILTER (WHERE p."RegionCode" = 'BN' AND NOT p."IsKamp")::int AS "BalkanAdresCount",
        COUNT(*) FILTER (WHERE p."RegionCode" = 'AS')::int AS "AsgabatCount",
        COUNT(*) FILTER (WHERE p."RegionCode" = 'AS' AND p."IsKamp")::int AS "AsgabatKampCount",
        COUNT(*) FILTER (WHERE p."RegionCode" = 'AS' AND NOT p."IsKamp")::int AS "AsgabatAdresCount",
        COUNT(*) FILTER (WHERE p."RegionCode" = 'AH')::int AS "AhalCount",
        COUNT(*) FILTER (WHERE p."RegionCode" = 'AH' AND p."IsKamp")::int AS "AhalKampCount",
        COUNT(*) FILTER (WHERE p."RegionCode" = 'AH' AND NOT p."IsKamp")::int AS "AhalAdresCount",
        COUNT(*) FILTER (WHERE p."RegionCode" = 'LB')::int AS "LebapCount",
        COUNT(*) FILTER (WHERE p."RegionCode" = 'LB' AND p."IsKamp")::int AS "LebapKampCount",
        COUNT(*) FILTER (WHERE p."RegionCode" = 'LB' AND NOT p."IsKamp")::int AS "LebapAdresCount",
        COUNT(*) FILTER (WHERE p."RegionCode" = 'MR' AND NOT p."IsKamp")::int AS "MaryAdresCount",
        COUNT(*) FILTER (WHERE p."RegionCode" = 'MR' AND p."IsKamp")::int AS "MaryKampCount"
    FROM people p
    WHERE p."RegionCode" IS NOT NULL
    GROUP BY p."DepartmentLabel"
)
SELECT
    md5(c."DepartmentLabel")::uuid AS "ID",
    c."DepartmentLabel" AS "DepartmentLabel",
    c."BalkanKampCount",
    c."BalkanAdresCount",
    c."AsgabatCount",
    c."AsgabatKampCount",
    c."AsgabatAdresCount",
    c."AhalCount",
    c."AhalKampCount",
    c."AhalAdresCount",
    c."LebapCount",
    c."LebapKampCount",
    c."LebapAdresCount",
    c."MaryAdresCount",
    c."MaryKampCount",
    (c."BalkanKampCount" + c."BalkanAdresCount" + c."AsgabatCount" + c."AhalCount"
        + c."LebapCount" + c."MaryAdresCount" + c."MaryKampCount")::int AS "TotalCount",
    c."DepartmentLabel" AS "ProjectName",
    c."DepartmentLabel" AS "ProjectNameRaw",
    c."DepartmentLabel" AS "ProjectNameTm",
    0 AS "PersonRoleCode",
    c."DepartmentLabel" AS "StatusLabel",
    'st-cat-1' AS "StatusCssClass",
    FALSE AS "IsArchived"
FROM counted c;