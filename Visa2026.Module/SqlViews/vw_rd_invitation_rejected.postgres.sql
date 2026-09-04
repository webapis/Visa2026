-- Report Dashboard: Invitations Rejected (rejected-by-project) — PostgreSQL.
DROP VIEW IF EXISTS vw_rd_invitation_rejected;
CREATE VIEW vw_rd_invitation_rejected AS
SELECT
    ri."ID"                                                                 AS "ID",
    'rejection-item'                                                        AS "SourceKind",
    p."ID"                                                                  AS "PersonOid",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p."FirstName"), ''),
        NULLIF(BTRIM(p."MiddleName"), ''),
        NULLIF(BTRIM(p."LastName"), '')
    )                                                                       AS "PersonName",
    COALESCE(
        NULLIF(BTRIM(apc."NameTm"), ''),
        NULLIF(BTRIM(pc."NameTm"), ''),
        NULLIF(BTRIM(spc."NameTm"), ''),
        '(No project)'
    )                                                                       AS "ProjectName",
    COALESCE(apc."NameTm", pc."NameTm", spc."NameTm", '')                   AS "ProjectNameRaw",
    COALESCE(apc."NameTm", pc."NameTm", spc."NameTm", '')                   AS "ProjectNameTm",
    p."PersonRole"                                                          AS "PersonRoleCode",
    COALESCE(NULLIF(BTRIM(r."RejectedDocNumber"), ''), '')                  AS "DocumentNumber",
    CASE WHEN (r."Date")::date > DATE '1900-01-01' THEN r."Date" ELSE NULL END AS "RecordDate",
    COALESCE(
        NULLIF(BTRIM(apc."NameTm"), ''),
        NULLIF(BTRIM(pc."NameTm"), ''),
        NULLIF(BTRIM(spc."NameTm"), ''),
        '(No project)'
    )                                                                       AS "StatusLabel",
    'st-cat-1'                                                              AS "StatusCssClass",
    COALESCE(p."IsArchived", FALSE)                                         AS "IsArchived"
FROM "RejectionItems" ri
INNER JOIN "Rejections" r
    ON r."ID" = ri."RejectionID" AND COALESCE(r."GCRecord", 0) = 0
INNER JOIN "ApplicationProfileInstances" a
    ON a."ID" = r."ApplicationProfileInstanceID" AND COALESCE(a."GCRecord", 0) = 0
INNER JOIN "ApplicationProfiles" apf
    ON apf."ID" = a."ApplicationProfileID"
   AND COALESCE(apf."GCRecord", 0) = 0
   AND COALESCE(apf."ProduceInvitation", FALSE) = TRUE
INNER JOIN "People" p
    ON p."ID" = ri."PersonID" AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" apc
    ON apc."ID" = a."ProjectContractID" AND COALESCE(apc."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = p."ProjectContractID" AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" sp
    ON sp."ID" = p."SponsoringEmployeeID" AND COALESCE(sp."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" spc
    ON spc."ID" = sp."ProjectContractID" AND COALESCE(spc."GCRecord", 0) = 0
WHERE COALESCE(ri."GCRecord", 0) = 0
  AND ri."PersonID" IS NOT NULL

UNION ALL

SELECT
    a."ID"                                                                  AS "ID",
    'application'                                                           AS "SourceKind",
    first_p."ID"                                                            AS "PersonOid",
    COALESCE(
        NULLIF(CONCAT_WS(' ',
            NULLIF(BTRIM(first_p."FirstName"), ''),
            NULLIF(BTRIM(first_p."MiddleName"), ''),
            NULLIF(BTRIM(first_p."LastName"), '')
        ), ''),
        NULLIF(BTRIM(a."FullApplicationNumber"), ''),
        NULLIF(BTRIM(a."ApplicationNumber"), ''),
        ''
    )                                                                       AS "PersonName",
    COALESCE(
        NULLIF(BTRIM(apc."NameTm"), ''),
        NULLIF(BTRIM(pc."NameTm"), ''),
        NULLIF(BTRIM(spc."NameTm"), ''),
        '(No project)'
    )                                                                       AS "ProjectName",
    COALESCE(apc."NameTm", pc."NameTm", spc."NameTm", '')                   AS "ProjectNameRaw",
    COALESCE(apc."NameTm", pc."NameTm", spc."NameTm", '')                   AS "ProjectNameTm",
    COALESCE(first_p."PersonRole", 0)                                       AS "PersonRoleCode",
    COALESCE(
        NULLIF(BTRIM(a."FullApplicationNumber"), ''),
        NULLIF(BTRIM(a."ApplicationNumber"), ''),
        ''
    )                                                                       AS "DocumentNumber",
    a."ApplicationDate"                                                     AS "RecordDate",
    COALESCE(
        NULLIF(BTRIM(apc."NameTm"), ''),
        NULLIF(BTRIM(pc."NameTm"), ''),
        NULLIF(BTRIM(spc."NameTm"), ''),
        '(No project)'
    )                                                                       AS "StatusLabel",
    'st-cat-1'                                                              AS "StatusCssClass",
    COALESCE(first_p."IsArchived", FALSE)                                   AS "IsArchived"
FROM "ApplicationProfileInstances" a
INNER JOIN "ApplicationProfiles" apf
    ON apf."ID" = a."ApplicationProfileID"
   AND COALESCE(apf."GCRecord", 0) = 0
   AND COALESCE(apf."ProduceInvitation", FALSE) = TRUE
LEFT JOIN "ProjectContracts" apc
    ON apc."ID" = a."ProjectContractID" AND COALESCE(apc."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT ap."StateID"
    FROM "ApplicationProfileInstanceProgresses" ap
    WHERE ap."ApplicationProfileInstanceID" = a."ID"
      AND COALESCE(ap."GCRecord", 0) = 0
    ORDER BY ap."Date" DESC NULLS LAST, ap."ID" DESC
    LIMIT 1
) latest_ap ON TRUE
INNER JOIN "ApplicationStates" ast
    ON ast."ID" = latest_ap."StateID"
   AND COALESCE(ast."GCRecord", 0) = 0
   AND ast."Code" = 'PROCESS_REJECTED'
LEFT JOIN LATERAL (
    SELECT ap_row."PersonId"
    FROM "ApplicationProfileInstancePeople" ap_row
    WHERE ap_row."ApplicationProfileInstanceId" = a."ID"
    ORDER BY ap_row."PersonId"
    LIMIT 1
) first_m2m ON TRUE
LEFT JOIN "People" first_p
    ON first_p."ID" = first_m2m."PersonId" AND COALESCE(first_p."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = first_p."ProjectContractID" AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" sp
    ON sp."ID" = first_p."SponsoringEmployeeID" AND COALESCE(sp."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" spc
    ON spc."ID" = sp."ProjectContractID" AND COALESCE(spc."GCRecord", 0) = 0
WHERE COALESCE(a."GCRecord", 0) = 0
  AND NOT EXISTS (
        SELECT 1
        FROM "Rejections" r
        WHERE r."ApplicationProfileInstanceID" = a."ID"
          AND COALESCE(r."GCRecord", 0) = 0
    );