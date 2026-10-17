-- Report Dashboard: A Maglumat (Active WorkPermit) — PostgreSQL.
-- One person row. Every valid work permit (not cancelled, not yet expired) is listed
-- in WorkPermitBundle, newest start date first. Passport and the scalar permit
-- columns stay on the newest permit.
DROP VIEW IF EXISTS vw_rd_work_permit_active_locations;
DROP VIEW IF EXISTS vw_rd_work_permit_active;
CREATE VIEW vw_rd_work_permit_active AS
SELECT
    picked."ID"                                                             AS "ID",
    p."ID"                                                                  AS "PersonOid",
    picked."WorkPermitID"                                                   AS "WorkPermitOid",
    COALESCE(pass."ID", cur_pass."ID")                                      AS "PassportOid",
    cur_visa."ID"                                                           AS "VisaOid",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p."FirstName"), ''),
        NULLIF(BTRIM(p."MiddleName"), ''),
        NULLIF(BTRIM(p."LastName"), '')
    )                                                                       AS "PersonName",
    CASE WHEN (p."DateOfBirth")::date > DATE '1900-01-01' THEN p."DateOfBirth" ELSE NULL END AS "DateOfBirth",
    COALESCE(NULLIF(BTRIM(nat."Code"), ''), '')                             AS "CitizenshipCode",
    COALESCE(NULLIF(BTRIM(COALESCE(pass."PassportNumber", cur_pass."PassportNumber")), ''), '') AS "PassportNumber",
    CASE
        WHEN (COALESCE(pass."ExpirationDate", cur_pass."ExpirationDate"))::date > DATE '1900-01-01'
            THEN COALESCE(pass."ExpirationDate", cur_pass."ExpirationDate")
        ELSE NULL
    END                                                                     AS "PassportExpirationDate",
    COALESCE(edu."LevelName", '')                                              AS "EducationLevelLabel",
    COALESCE(edu."InstitutionName", '')                                         AS "EducationInstitutionLabel",
    COALESCE(NULLIF(BTRIM(act."Name"), ''), '')                             AS "PositionLabel",
    COALESCE(NULLIF(BTRIM(reg."NameTm"), ''), '')                           AS "AddressRegionTm",
    COALESCE(NULLIF(BTRIM(city."NameTm"), ''), '')                          AS "AddressCityTm",
    COALESCE(NULLIF(BTRIM(addr."FullAddress"), ''), '')                     AS "AddressStreet",
    COALESCE(NULLIF(BTRIM(picked."WorkPermitNumber"), ''), NULLIF(BTRIM(picked."ASNumber"), ''), '') AS "WorkPermitNumber",
    CASE WHEN (picked."StartDate")::date > DATE '1900-01-01' THEN picked."StartDate" ELSE NULL END AS "StartDate",
    CASE WHEN (picked."ExpirationDate")::date > DATE '1900-01-01' THEN picked."ExpirationDate" ELSE NULL END AS "ExpirationDate",
    COALESCE(all_permits."Bundle", '')                                      AS "WorkPermitBundle",
    COALESCE(all_permits."Locations", '')                                   AS "WorkPermitLocationsLabel",
    COALESCE(NULLIF(BTRIM(cur_visa."VisaNumber"), ''), '')                  AS "VisaNumber",
    CASE WHEN (cur_visa."StartDate")::date > DATE '1900-01-01' THEN cur_visa."StartDate" ELSE NULL END AS "VisaStartDate",
    CASE WHEN (cur_visa."ExpirationDate")::date > DATE '1900-01-01' THEN cur_visa."ExpirationDate" ELSE NULL END AS "VisaEndDate",
    ''                                                                      AS "Notes",
    COALESCE(
        NULLIF(BTRIM(pc."NameTm"), ''),
        NULLIF(BTRIM(spc."NameTm"), ''),
        '(No project)'
    )                                                                       AS "ProjectName",
    COALESCE(pc."NameTm", spc."NameTm", '')                                 AS "ProjectNameRaw",
    COALESCE(pc."NameTm", spc."NameTm", '')                                 AS "ProjectNameTm",
    p."PersonRole"                                                          AS "PersonRoleCode",
    COALESCE(
        NULLIF(BTRIM(pc."NameTm"), ''),
        NULLIF(BTRIM(spc."NameTm"), ''),
        '(No project)'
    )                                                                       AS "StatusLabel",
    'st-cat-1'                                                              AS "StatusCssClass",
    COALESCE(p."IsArchived", FALSE)                                         AS "IsArchived"
FROM (
    SELECT DISTINCT ON (wpi."PersonID")
        wpi."ID",
        wpi."PersonID",
        wpi."WorkPermitID",
        wpi."PassportID",
        wpi."WorkPermitNumber",
        wpi."ASNumber",
        wpi."StartDate",
        wpi."ExpirationDate"
    FROM "WorkPermitItems" wpi
    WHERE COALESCE(wpi."GCRecord", 0) = 0
      AND wpi."PersonID" IS NOT NULL
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
            AND ap."ActionFamily" = 1
            AND BTRIM(COALESCE(api."LatestPrimaryStateCode", '')) = 'PROCESS_ISSUED')
    ORDER BY wpi."PersonID", wpi."StartDate" DESC NULLS LAST, wpi."ExpirationDate" DESC NULLS LAST, wpi."ID" DESC
) picked
INNER JOIN "People" p
    ON p."ID" = picked."PersonID" AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT string_agg(
        COALESCE(v."WorkPermitID"::text, '') || E'\x1e' ||
        v."ID"::text || E'\x1e' ||
        replace(
            COALESCE(NULLIF(BTRIM(v."WorkPermitNumber"), ''), NULLIF(BTRIM(v."ASNumber"), ''), ''),
            E'\x1e',
            '') || E'\x1e' ||
        CASE
            WHEN (v."StartDate")::date > DATE '1900-01-01' THEN to_char(v."StartDate", 'DD.MM.YYYY')
            ELSE ''
        END || E'\x1e' ||
        CASE
            WHEN (v."ExpirationDate")::date > DATE '1900-01-01' THEN to_char(v."ExpirationDate", 'DD.MM.YYYY')
            ELSE ''
        END || E'\x1e' ||
        replace(replace(replace(COALESCE(BTRIM(v."WorkPermittedLocations"), ''), E'\x1e', ' '), E'\n', ' '), E'\r', ' '),
        E'\n'
        ORDER BY v."StartDate" DESC NULLS LAST, v."ExpirationDate" DESC NULLS LAST, v."ID" DESC
    ) AS "Bundle",
    string_agg(
        replace(replace(replace(COALESCE(BTRIM(v."WorkPermittedLocations"), ''), E'\x1e', ' '), E'\n', ' '), E'\r', ' '),
        E'\n\n'
        ORDER BY v."StartDate" DESC NULLS LAST, v."ExpirationDate" DESC NULLS LAST, v."ID" DESC
    ) AS "Locations"
    FROM "WorkPermitItems" v
    WHERE v."PersonID" = picked."PersonID"
      AND COALESCE(v."GCRecord", 0) = 0
      AND v."ExpirationDate" IS NOT NULL
      AND (v."ExpirationDate")::date >= CURRENT_DATE
      AND NOT EXISTS (
          SELECT 1
          FROM "ApplicationProfileInstanceWorkPermitItems" j
          INNER JOIN "ApplicationProfileInstances" api ON api."ID" = j."ApplicationProfileInstanceId"
          INNER JOIN "ApplicationProfiles" ap ON ap."ID" = api."ApplicationProfileID"
          WHERE j."WorkPermitItemId" = v."ID"
            AND COALESCE(api."GCRecord", 0) = 0
            AND COALESCE(ap."GCRecord", 0) = 0
            AND ap."ActionFamily" = 1
            AND BTRIM(COALESCE(api."LatestPrimaryStateCode", '')) = 'PROCESS_ISSUED')
) all_permits ON TRUE
LEFT JOIN "Countries" nat
    ON nat."ID" = p."NationalityID" AND COALESCE(nat."GCRecord", 0) = 0
LEFT JOIN "Passports" pass
    ON pass."ID" = picked."PassportID" AND COALESCE(pass."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT ps."ID", ps."PassportNumber", ps."ExpirationDate"
    FROM "Passports" ps
    WHERE picked."PassportID" IS NULL
      AND ps."PersonID" = p."ID"
      AND COALESCE(ps."GCRecord", 0) = 0
    ORDER BY ps."IssueDate" DESC NULLS LAST, ps."ID" DESC
    LIMIT 1
) cur_pass ON TRUE
LEFT JOIN LATERAL (
    SELECT
        COALESCE(NULLIF(BTRIM(el."NameTm"), ''), NULLIF(BTRIM(el."Name"), ''), '') AS "LevelName",
        COALESCE(NULLIF(BTRIM(ei."NameTm"), ''), NULLIF(BTRIM(ei."Name"), ''), '') AS "InstitutionName"
    FROM "Educations" ed
    LEFT JOIN "EducationLevels" el
        ON el."ID" = ed."EducationLevelID" AND COALESCE(el."GCRecord", 0) = 0
    LEFT JOIN "EducationInstitutions" ei
        ON ei."ID" = ed."EducationInstitutionID" AND COALESCE(ei."GCRecord", 0) = 0
    WHERE ed."PersonID" = p."ID"
      AND COALESCE(ed."GCRecord", 0) = 0
    ORDER BY ed."GraduationYear" DESC NULLS LAST, ed."ID" DESC
    LIMIT 1
) edu ON TRUE
LEFT JOIN LATERAL (
    SELECT eph."ActualPositionID"
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
LEFT JOIN "ActualPositions" act
    ON act."ID" = cur_pos."ActualPositionID" AND COALESCE(act."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT a."FullAddress", a."RegionID", a."CityID"
    FROM "AddressesOfResidence" a
    WHERE a."PersonID" = p."ID"
      AND COALESCE(a."GCRecord", 0) = 0
    ORDER BY
        CASE
            WHEN a."ExpirationDate" IS NULL OR (a."ExpirationDate")::date >= CURRENT_DATE THEN 0
            ELSE 1
        END,
        a."ExpirationDate" DESC NULLS FIRST,
        a."ID" DESC
    LIMIT 1
) addr ON TRUE
LEFT JOIN "Regions" reg
    ON reg."ID" = addr."RegionID" AND COALESCE(reg."GCRecord", 0) = 0
LEFT JOIN "Cities" city
    ON city."ID" = addr."CityID" AND COALESCE(city."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT v."ID", v."VisaNumber", v."StartDate", v."ExpirationDate"
    FROM "Visas" v
    INNER JOIN "Passports" vp
        ON vp."ID" = v."PassportID" AND COALESCE(vp."GCRecord", 0) = 0
    WHERE vp."PersonID" = p."ID"
      AND COALESCE(v."GCRecord", 0) = 0
      AND (v."StartDate")::date <= CURRENT_DATE
      AND (v."ExpirationDate")::date >= CURRENT_DATE
      AND NOT EXISTS (
          SELECT 1
          FROM "ApplicationProfileInstanceVisas" jv
          INNER JOIN "ApplicationProfileInstances" api ON api."ID" = jv."ApplicationProfileInstanceId"
          INNER JOIN "ApplicationProfiles" ap ON ap."ID" = api."ApplicationProfileID"
          WHERE jv."VisaId" = v."ID"
            AND COALESCE(api."GCRecord", 0) = 0
            AND COALESCE(ap."GCRecord", 0) = 0
            AND ap."ActionFamily" = 1
            AND BTRIM(COALESCE(api."LatestPrimaryStateCode", '')) = 'PROCESS_ISSUED')
    ORDER BY v."StartDate" DESC NULLS LAST, v."IssueDate" DESC NULLS LAST, v."ID" DESC
    LIMIT 1
) cur_visa ON TRUE
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = p."ProjectContractID" AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" sp
    ON sp."ID" = p."SponsoringEmployeeID" AND COALESCE(sp."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" spc
    ON spc."ID" = sp."ProjectContractID" AND COALESCE(spc."GCRecord", 0) = 0;

CREATE VIEW vw_rd_work_permit_active_locations AS
SELECT * FROM vw_rd_work_permit_active;