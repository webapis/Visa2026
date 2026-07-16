-- Report Dashboard: Visa State — Extension Started (PostgreSQL).
-- Plus: Application ProgressHistory must not contain PROCESS_CANCELLED.
DROP VIEW IF EXISTS vw_rd_visa_state;
CREATE VIEW vw_rd_visa_state AS
WITH ranked_visas AS (
    SELECT
        v."ID" AS "VisaID",
        pp."PersonID",
        v."VisaNumber",
        v."ExpirationDate",
        v."StartDate",
        v."IssueDate",
        ROW_NUMBER() OVER (
            PARTITION BY pp."PersonID"
            ORDER BY v."StartDate" DESC NULLS LAST, v."IssueDate" DESC NULLS LAST, v."ID" DESC
        ) AS rn
    FROM "Visas" v
    INNER JOIN "Passports" pp
        ON pp."ID" = v."PassportID"
       AND COALESCE(pp."GCRecord", 0) = 0
    WHERE COALESCE(v."GCRecord", 0) = 0
      AND COALESCE(v."IsCancelled", FALSE) = FALSE
      AND v."StartDate" IS NOT NULL
      AND (v."StartDate")::date > DATE '1900-01-01'
      AND (v."StartDate")::date <= CURRENT_DATE
),
ext_items AS (
    SELECT
        ai."ID" AS "ApplicationItemID",
        ai."PersonID",
        ai."CurrentVisaId" AS "VisaID",
        a."ID" AS "ApplicationID",
        a."ApplicationNumber",
        a."FullApplicationNumber",
        a."ApplicationDate",
        a."ProjectContractID" AS "ApplicationProjectContractID"
    FROM "ApplicationItems" ai
    INNER JOIN "Applications" a
        ON a."ID" = ai."ApplicationID"
       AND COALESCE(a."GCRecord", 0) = 0
    INNER JOIN "ApplicationTypes" at
        ON at."ID" = a."ApplicationTypeID"
       AND COALESCE(at."GCRecord", 0) = 0
    WHERE COALESCE(ai."GCRecord", 0) = 0
      AND ai."CurrentVisaId" IS NOT NULL
      AND at."Name" IN (
            'App_Visa_Ext',
            'App_Visa_Ext_According_to_WP',
            'App_Visa_Ext_FM',
            'App_Visa_and_WP_Ext'
        )
)
SELECT
    ei."ApplicationItemID"                                              AS "ID",
    p."ID"                                                              AS "PersonOid",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p."FirstName"), ''),
        NULLIF(BTRIM(p."MiddleName"), ''),
        NULLIF(BTRIM(p."LastName"), '')
    )                                                                   AS "PersonName",
    COALESCE(
        NULLIF(BTRIM(pc."NameTm"), ''),
        NULLIF(BTRIM(spc."NameTm"), ''),
        ''
    )                                                                   AS "ProjectName",
    COALESCE(pc."NameTm", spc."NameTm", '')                             AS "ProjectNameRaw",
    COALESCE(pc."NameTm", spc."NameTm", '')                             AS "ProjectNameTm",
    p."PersonRole"                                                      AS "PersonRoleCode",
    COALESCE(NULLIF(BTRIM(rv."VisaNumber"), ''), '')                    AS "VisaNumber",
    CASE WHEN (rv."ExpirationDate")::date > DATE '1900-01-01' THEN rv."ExpirationDate" ELSE NULL END AS "ExpirationDate",
    'Extension Started'                                                 AS "StateLabel",
    'st-pending'                                                        AS "StateCssClass",
    COALESCE(p."IsArchived", FALSE)                                     AS "IsArchived"
FROM ext_items ei
INNER JOIN ranked_visas rv
    ON rv."VisaID" = ei."VisaID"
   AND rv."PersonID" = ei."PersonID"
   AND rv.rn = 1
INNER JOIN "People" p
    ON p."ID" = ei."PersonID"
   AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = COALESCE(ei."ApplicationProjectContractID", p."ProjectContractID")
   AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" sp
    ON sp."ID" = p."SponsoringEmployeeID"
   AND COALESCE(sp."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" spc
    ON spc."ID" = sp."ProjectContractID"
   AND COALESCE(spc."GCRecord", 0) = 0
WHERE rv."ExpirationDate" IS NOT NULL
  AND (rv."ExpirationDate")::date >= CURRENT_DATE
  AND NOT EXISTS (
        SELECT 1
        FROM "ApplicationProgresses" ap
        INNER JOIN "ApplicationStates" ast
            ON ast."ID" = ap."StateID"
           AND COALESCE(ast."GCRecord", 0) = 0
        WHERE ap."ApplicationID" = ei."ApplicationID"
          AND COALESCE(ap."GCRecord", 0) = 0
          AND ast."Code" = 'PROCESS_CANCELLED'
      );
