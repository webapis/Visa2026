-- Report Dashboard: Personel sayi — PostgreSQL.
-- One row per department. Employees only, one person even with several work permits.
-- Aktiv = active work permit and active valid visa (one last per person).
-- Aktiv olmayan = active work permit and no active valid visa.
DROP VIEW IF EXISTS vw_rd_registration_oturum_sayisi;
DROP VIEW IF EXISTS vw_rd_work_permit_personel_sayi;
CREATE VIEW vw_rd_work_permit_personel_sayi AS
WITH people AS (
    SELECT
        a."PersonOid",
        COALESCE(NULLIF(BTRIM(dept."Name"), ''), '(No department)') AS "DepartmentLabel",
        EXISTS (
            SELECT 1
            FROM vw_rd_visa_active_by_project v
            WHERE v."PersonOid" = a."PersonOid"
              AND COALESCE(v."IsOneLastValidPerPerson", FALSE) = TRUE
              AND COALESCE(v."IsArchived", FALSE) = FALSE
        ) AS "HasActiveVisa"
    FROM vw_rd_work_permit_active a
    INNER JOIN "People" p
        ON p."ID" = a."PersonOid" AND COALESCE(p."GCRecord", 0) = 0
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
    WHERE COALESCE(a."PersonRoleCode", 0) = 0
      AND COALESCE(a."IsArchived", FALSE) = FALSE
)
SELECT
    md5(p."DepartmentLabel")::uuid                                          AS "ID",
    p."DepartmentLabel"                                                     AS "DepartmentLabel",
    COUNT(*) FILTER (WHERE p."HasActiveVisa")::int                          AS "ActiveCount",
    COUNT(*) FILTER (WHERE NOT p."HasActiveVisa")::int                      AS "InactiveCount",
    p."DepartmentLabel"                                                     AS "ProjectName",
    p."DepartmentLabel"                                                     AS "ProjectNameRaw",
    p."DepartmentLabel"                                                     AS "ProjectNameTm",
    0                                                                       AS "PersonRoleCode",
    p."DepartmentLabel"                                                     AS "StatusLabel",
    'st-cat-1'                                                              AS "StatusCssClass",
    FALSE                                                                   AS "IsArchived"
FROM people p
GROUP BY p."DepartmentLabel";