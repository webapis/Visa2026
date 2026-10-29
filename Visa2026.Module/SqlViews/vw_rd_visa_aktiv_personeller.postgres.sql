-- Report Dashboard: aktiv personeller — PostgreSQL.
-- One row per person on Active Visa (latest current visa).
-- PROJECT is the department. Firması is the subcontractor.
-- A missing current work permit leaves the permit end empty.
-- A visa linked to a cancellation instance (ActionFamily Cancellation)
-- or a check-out instance (Registration + CheckOut, including check_out_internal)
-- is closed even when ExpirationDate is still in the future. The link is enough;
-- LatestPrimaryStateCode may still be blank. Then keep the latest remaining visa.
DROP VIEW IF EXISTS vw_rd_visa_aktiv_personeller;
CREATE VIEW vw_rd_visa_aktiv_personeller AS
WITH open_visas AS (
    SELECT src.*
    FROM vw_rd_visa_active_by_project src
    WHERE NOT EXISTS (
        SELECT 1
        FROM "ApplicationProfileInstanceVisas" j
        INNER JOIN "ApplicationProfileInstances" api
            ON api."ID" = j."ApplicationProfileInstanceId"
           AND COALESCE(api."GCRecord", 0) = 0
        INNER JOIN "ApplicationProfiles" ap
            ON ap."ID" = api."ApplicationProfileID"
           AND COALESCE(ap."GCRecord", 0) = 0
        WHERE j."VisaId" = src."ID"
          AND (
              ap."ActionFamily" = 1
              OR (
                  ap."ActionFamily" = 2
                  AND COALESCE(ap."RegistrationKind", 0) = 2
              )
          )
    )
),
open_ranked AS (
    SELECT ov.*,
           ROW_NUMBER() OVER (
               PARTITION BY ov."PersonOid"
               ORDER BY ov."ExpirationDate" DESC NULLS LAST, ov."ID" DESC
           ) AS aktiv_rn
    FROM open_visas ov
)
SELECT
    a."ID"                                                                  AS "ID",
    a."PersonOid"                                                           AS "PersonOid",
    a."ID"                                                                  AS "VisaOid",
    a."PersonName"                                                          AS "PersonName",
    CASE WHEN (p."DateOfBirth")::date > DATE '1900-01-01' THEN p."DateOfBirth" ELSE NULL END AS "DateOfBirth",
    COALESCE(
        NULLIF(BTRIM(cur_pass."PassportNumber"), ''),
        NULLIF(BTRIM(a."PassportNumber"), ''),
        ''
    )                                                                       AS "PassportNumber",
    CASE
        WHEN (cur_pass."ExpirationDate")::date > DATE '1900-01-01' THEN cur_pass."ExpirationDate"
        ELSE NULL
    END                                                                     AS "PassportExpirationDate",
    COALESCE(
        NULLIF(BTRIM(visa_pos."NameTm"), ''),
        NULLIF(BTRIM(visa_pos."Name"), ''),
        ''
    )                                                                       AS "PositionLabel",
    COALESCE(dept."Name", '')                                               AS "DepartmentLabel",
    COALESCE(
        NULLIF(BTRIM(sub."NameTm"), ''),
        NULLIF(BTRIM(sub."Name"), ''),
        ''
    )                                                                       AS "SubcontractorLabel",
    CASE
        WHEN (permit."ExpirationDate")::date > DATE '1900-01-01' THEN permit."ExpirationDate"
        ELSE NULL
    END                                                                     AS "WorkPermitEndDate",
    a."VisaNumber"                                                          AS "VisaNumber",
    CASE
        WHEN (vsrc."StartDate")::date > DATE '1900-01-01' THEN vsrc."StartDate"
        ELSE NULL
    END                                                                     AS "VisaStartDate",
    a."ExpirationDate"                                                      AS "VisaEndDate",
    COALESCE(NULLIF(BTRIM(reg."NameTm"), ''), '')                           AS "AddressRegionTm",
    COALESCE(NULLIF(BTRIM(city."NameTm"), ''), '')                          AS "AddressCityTm",
    COALESCE(NULLIF(BTRIM(addr."FullAddress"), ''), '')                     AS "AddressStreet",
    a."ProjectName"                                                         AS "ProjectName",
    a."ProjectNameRaw"                                                      AS "ProjectNameRaw",
    a."ProjectNameTm"                                                       AS "ProjectNameTm",
    a."PersonRoleCode"                                                      AS "PersonRoleCode",
    COALESCE(NULLIF(BTRIM(a."StatusLabel"), ''), '(No project)')            AS "StatusLabel",
    'st-cat-1'                                                              AS "StatusCssClass",
    a."IsArchived"                                                          AS "IsArchived"
FROM open_ranked a
INNER JOIN "People" p
    ON p."ID" = a."PersonOid" AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "Visas" vsrc
    ON vsrc."ID" = a."ID" AND COALESCE(vsrc."GCRecord", 0) = 0
LEFT JOIN "Subcontractors" sub
    ON sub."ID" = p."SubcontractorID" AND COALESCE(sub."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT ps."PassportNumber", ps."ExpirationDate"
    FROM "Passports" ps
    WHERE ps."PersonID" = p."ID"
      AND COALESCE(ps."GCRecord", 0) = 0
    ORDER BY ps."IssueDate" DESC NULLS LAST, ps."ID" DESC
    LIMIT 1
) cur_pass ON TRUE
LEFT JOIN LATERAL (
    SELECT wpi."ExpirationDate"
    FROM "WorkPermitItems" wpi
    WHERE COALESCE(wpi."GCRecord", 0) = 0
      AND wpi."PersonID" = p."ID"
      AND wpi."ExpirationDate" IS NOT NULL
      AND (wpi."ExpirationDate")::date >= CURRENT_DATE
      AND NOT EXISTS (
          SELECT 1
          FROM "ApplicationProfileInstanceWorkPermitItems" j
          INNER JOIN "ApplicationProfileInstances" api ON api."ID" = j."ApplicationProfileInstanceId"
          INNER JOIN "ApplicationProfiles" ap ON ap."ID" = api."ApplicationProfileID"
          WHERE j."WorkPermitItemId" = wpi."ID"
            AND COALESCE(api."GCRecord", 0) = 0
            AND COALESCE(ap."GCRecord", 0) = 0
            AND ap."ActionFamily" = 1)
    ORDER BY wpi."StartDate" DESC NULLS LAST, wpi."ExpirationDate" DESC NULLS LAST, wpi."ID" DESC
    LIMIT 1
) permit ON TRUE
LEFT JOIN LATERAL (
    SELECT eph."PositionID"
    FROM "EmployeePositionHistories" eph
    WHERE eph."PersonID" = p."ID"
      AND COALESCE(eph."GCRecord", 0) = 0
    ORDER BY
        CASE
            WHEN eph."EndDate" IS NULL OR (eph."EndDate")::date >= CURRENT_DATE THEN 0
            ELSE 1
        END,
        eph."StartDate" DESC NULLS LAST,
        eph."ID" DESC
    LIMIT 1
) cur_pos ON TRUE
LEFT JOIN "Positions" visa_pos
    ON visa_pos."ID" = cur_pos."PositionID" AND COALESCE(visa_pos."GCRecord", 0) = 0
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
    SELECT ad."FullAddress", ad."RegionID", ad."CityID"
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
LEFT JOIN "Cities" city
    ON city."ID" = addr."CityID" AND COALESCE(city."GCRecord", 0) = 0
WHERE a.aktiv_rn = 1;
