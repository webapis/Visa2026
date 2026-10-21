-- Report Dashboard: çalışma izni listesi — PostgreSQL.
-- Same people as A. Maglumat. PROJECT is the department. Firması is the subcontractor.
-- Mesleki Görevleri is the visa-report position already on the active view.
DROP VIEW IF EXISTS vw_rd_work_permit_calisma_izni_listesi;
CREATE VIEW vw_rd_work_permit_calisma_izni_listesi AS
SELECT
    a."ID"                                                                  AS "ID",
    a."PersonOid"                                                           AS "PersonOid",
    a."VisaOid"                                                             AS "VisaOid",
    a."PersonName"                                                          AS "PersonName",
    a."DateOfBirth"                                                         AS "DateOfBirth",
    a."PassportNumber"                                                      AS "PassportNumber",
    a."PassportExpirationDate"                                              AS "PassportExpirationDate",
    a."PositionLabel"                                                       AS "PositionLabel",
    COALESCE(dept."Name", '')                                               AS "DepartmentLabel",
    COALESCE(
        NULLIF(BTRIM(sub."NameTm"), ''),
        NULLIF(BTRIM(sub."Name"), ''),
        ''
    )                                                                       AS "SubcontractorLabel",
    a."ExpirationDate"                                                      AS "WorkPermitEndDate",
    a."VisaNumber"                                                          AS "VisaNumber",
    a."VisaEndDate"                                                         AS "VisaEndDate",
    a."AddressRegionTm"                                                     AS "AddressRegionTm",
    a."AddressCityTm"                                                       AS "AddressCityTm",
    a."AddressStreet"                                                       AS "AddressStreet",
    a."ProjectName"                                                         AS "ProjectName",
    a."ProjectNameRaw"                                                      AS "ProjectNameRaw",
    a."ProjectNameTm"                                                       AS "ProjectNameTm",
    a."PersonRoleCode"                                                      AS "PersonRoleCode",
    a."StatusLabel"                                                         AS "StatusLabel",
    a."StatusCssClass"                                                      AS "StatusCssClass",
    a."IsArchived"                                                          AS "IsArchived"
FROM vw_rd_work_permit_active a
INNER JOIN "People" p
    ON p."ID" = a."PersonOid" AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "Subcontractors" sub
    ON sub."ID" = p."SubcontractorID" AND COALESCE(sub."GCRecord", 0) = 0
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
) dept ON TRUE;
