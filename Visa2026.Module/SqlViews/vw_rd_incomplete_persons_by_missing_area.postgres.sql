-- Report Dashboard: Incomplete persons (PostgreSQL).
-- ProjectContracts exposes NameTm only (no Name column).
DROP VIEW IF EXISTS vw_rd_incomplete_persons_by_missing_area;
CREATE VIEW vw_rd_incomplete_persons_by_missing_area AS
SELECT
    p."ID"                                                                  AS "ID",
    p."ID"                                                                  AS "PersonOid",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p."FirstName"), ''),
        NULLIF(BTRIM(p."MiddleName"), ''),
        NULLIF(BTRIM(p."LastName"), '')
    )                                                                       AS "PersonName",
    COALESCE(NULLIF(BTRIM(pc."NameTm"), ''), '')                            AS "ProjectName",
    COALESCE(pc."NameTm", '')                                               AS "ProjectNameRaw",
    COALESCE(pc."NameTm", '')                                               AS "ProjectNameTm",
    p."PersonRole"                                                          AS "PersonRoleCode",
    CASE p."PersonRole"
        WHEN 0 THEN 'Employee'
        WHEN 1 THEN 'Family Member'
        WHEN 2 THEN 'Temporary Visitor'
        ELSE 'Unknown'
    END                                                                     AS "PersonTypeLabel",
    CONCAT_WS(', ',
        CASE WHEN COALESCE(p."IncompleteMissingPersonalData", false) THEN 'Personal data' END,
        CASE WHEN COALESCE(p."IncompleteMissingPassport", false) THEN 'Passport' END,
        CASE WHEN COALESCE(p."IncompleteMissingCv", false) THEN 'CV' END,
        CASE WHEN COALESCE(p."IncompleteMissingPhoto", false) THEN 'Photo' END,
        CASE WHEN COALESCE(p."IncompleteMissingEducation", false) THEN 'Education' END,
        CASE WHEN COALESCE(p."IncompleteMissingMedical", false) THEN 'Medical' END,
        CASE WHEN COALESCE(p."IncompleteMissingAddress", false) THEN 'Address' END,
        CASE WHEN COALESCE(p."IncompleteMissingFamilyDocs", false) THEN 'Family docs' END,
        CASE WHEN COALESCE(p."IncompleteMissingOther", false) THEN 'Other' END
    )                                                                       AS "MissingAreasLabel",
    COALESCE(p."IncompleteNotes", '')                                       AS "IncompleteNotes",
    p."IncompleteMarkedOn"                                                  AS "IncompleteMarkedOn",
    COALESCE(p."IncompleteMarkedBy", '')                                    AS "IncompleteMarkedBy",
    CASE
        WHEN p."IncompleteMarkedOn" IS NULL THEN COALESCE(p."IncompleteMarkedBy", '')
        WHEN NULLIF(BTRIM(p."IncompleteMarkedBy"), '') IS NULL
            THEN to_char(p."IncompleteMarkedOn", 'DD.MM.YYYY')
        ELSE to_char(p."IncompleteMarkedOn", 'DD.MM.YYYY') || ' · ' || BTRIM(p."IncompleteMarkedBy")
    END                                                                     AS "MarkedLabel",
    COALESCE(p."IncompleteMissingPersonalData", false)                      AS "MissingPersonalData",
    COALESCE(p."IncompleteMissingPassport", false)                          AS "MissingPassport",
    COALESCE(p."IncompleteMissingCv", false)                                AS "MissingCv",
    COALESCE(p."IncompleteMissingPhoto", false)                             AS "MissingPhoto",
    COALESCE(p."IncompleteMissingEducation", false)                         AS "MissingEducation",
    COALESCE(p."IncompleteMissingMedical", false)                           AS "MissingMedical",
    COALESCE(p."IncompleteMissingAddress", false)                           AS "MissingAddress",
    COALESCE(p."IncompleteMissingFamilyDocs", false)                        AS "MissingFamilyDocs",
    COALESCE(p."IncompleteMissingOther", false)                             AS "MissingOther",
    COALESCE(p."IsArchived", false)                                         AS "IsArchived"
FROM "People" p
LEFT JOIN "ProjectContracts" pc ON pc."ID" = p."ProjectContractID" AND COALESCE(pc."GCRecord", 0) = 0
WHERE COALESCE(p."GCRecord", 0) = 0
  AND COALESCE(p."IsDataIncomplete", false) = true;