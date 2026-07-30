-- Report Dashboard: Person search (PostgreSQL).
-- One row per Person. Backs the Person search category: officers type a term, pick a
-- result row, and open the person dossier.
-- Status buckets follow the person's current visa (latest non-cancelled visa across all
-- of the person's passports).
-- SearchText is a lowercased haystack (name parts + personal number + every passport
-- number) so Preview loader and XAF ListView criteria can filter identically.
-- ProjectContracts exposes NameTm only (no Name column).
DROP VIEW IF EXISTS vw_rd_person_search;
CREATE VIEW vw_rd_person_search AS
SELECT
    p."ID"                                                                  AS "ID",
    p."ID"                                                                  AS "PersonOid",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p."FirstName"), ''),
        NULLIF(BTRIM(p."MiddleName"), ''),
        NULLIF(BTRIM(p."LastName"), '')
    )                                                                       AS "PersonName",
    COALESCE(
        NULLIF(BTRIM(pc."NameTm"), ''),
        NULLIF(BTRIM(spc."NameTm"), ''),
        ''
    )                                                                       AS "ProjectName",
    COALESCE(pc."NameTm", spc."NameTm", '')                                 AS "ProjectNameRaw",
    COALESCE(pc."NameTm", spc."NameTm", '')                                 AS "ProjectNameTm",
    p."PersonRole"                                                          AS "PersonRoleCode",
    CASE p."PersonRole"
        WHEN 0 THEN 'Employee'
        WHEN 1 THEN 'Family Member'
        WHEN 2 THEN 'Temporary Visitor'
        ELSE 'Unknown'
    END                                                                     AS "PersonTypeLabel",
    COALESCE(p."PersonalNumber", '')                                        AS "PersonalNumber",
    COALESCE(cp."PassportNumber", '')                                       AS "PassportNumber",
    COALESCE(cv."VisaNumber", '')                                           AS "VisaNumber",
    cv."ExpirationDate"                                                     AS "VisaExpirationDate",
    CASE
        WHEN cv."ExpirationDate" IS NULL THEN ''
        ELSE to_char(cv."ExpirationDate", 'DD.MM.YYYY')
    END                                                                     AS "VisaExpiryLabel",
    CASE
        WHEN cv."ExpirationDate" IS NULL                                    THEN 'No visa'
        WHEN (cv."ExpirationDate")::date < CURRENT_DATE                      THEN 'Expired'
        WHEN (cv."ExpirationDate")::date <= (CURRENT_DATE + INTERVAL '30 days')::date
                                                                             THEN 'Expiring (<30 days)'
        ELSE                                                                 'Valid'
    END                                                                     AS "StatusLabel",
    CASE
        WHEN cv."ExpirationDate" IS NULL                                    THEN ''
        WHEN (cv."ExpirationDate")::date < CURRENT_DATE                      THEN 'st-expiring'
        WHEN (cv."ExpirationDate")::date <= (CURRENT_DATE + INTERVAL '30 days')::date
                                                                             THEN 'st-pending'
        ELSE                                                                 'st-approved'
    END                                                                     AS "StatusCssClass",
    LOWER(CONCAT_WS(' ',
        NULLIF(BTRIM(p."FirstName"), ''),
        NULLIF(BTRIM(p."MiddleName"), ''),
        NULLIF(BTRIM(p."LastName"), ''),
        NULLIF(BTRIM(p."PersonalNumber"), ''),
        allp."PassportNumbers"
    ))                                                                      AS "SearchText",
    COALESCE(p."IsArchived", FALSE)                                         AS "IsArchived"
FROM "People" p
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = p."ProjectContractID" AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" sp
    ON sp."ID" = p."SponsoringEmployeeID" AND COALESCE(sp."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" spc
    ON spc."ID" = sp."ProjectContractID" AND COALESCE(spc."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT pp."PassportNumber", pp."ExpirationDate"
    FROM "Passports" pp
    WHERE pp."PersonID" = p."ID"
      AND COALESCE(pp."GCRecord", 0) = 0
      AND COALESCE(pp."IsCancelled", FALSE) = FALSE
    ORDER BY pp."ExpirationDate" DESC NULLS LAST, pp."IssueDate" DESC NULLS LAST
    LIMIT 1
) cp ON TRUE
LEFT JOIN LATERAL (
    SELECT v."VisaNumber", v."ExpirationDate"
    FROM "Visas" v
    INNER JOIN "Passports" vp
        ON vp."ID" = v."PassportID" AND COALESCE(vp."GCRecord", 0) = 0
    WHERE vp."PersonID" = p."ID"
      AND COALESCE(v."GCRecord", 0) = 0
      AND COALESCE(v."IsCancelled", FALSE) = FALSE
    ORDER BY v."ExpirationDate" DESC NULLS LAST, v."IssueDate" DESC NULLS LAST
    LIMIT 1
) cv ON TRUE
LEFT JOIN LATERAL (
    SELECT string_agg(DISTINCT NULLIF(BTRIM(pp2."PassportNumber"), ''), ' ') AS "PassportNumbers"
    FROM "Passports" pp2
    WHERE pp2."PersonID" = p."ID"
      AND COALESCE(pp2."GCRecord", 0) = 0
) allp ON TRUE
WHERE COALESCE(p."GCRecord", 0) = 0;
