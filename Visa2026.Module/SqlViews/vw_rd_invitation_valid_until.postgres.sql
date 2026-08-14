-- Report Dashboard: Invitation Valid Until (valid-until) — PostgreSQL.
DROP VIEW IF EXISTS vw_rd_invitation_valid_until;
CREATE VIEW vw_rd_invitation_valid_until AS
SELECT
    ii."ID"                                                                 AS "ID",
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
    COALESCE(NULLIF(BTRIM(inv."InvitationNumber"), ''), '')                 AS "InvitationNumber",
    CASE WHEN (inv."ExpirationDate")::date > DATE '1900-01-01' THEN inv."ExpirationDate" ELSE NULL END AS "ExpirationDate",
    CASE WHEN (inv."StartDate")::date > DATE '1900-01-01' THEN inv."StartDate" ELSE NULL END AS "IssuedDate",
    (inv."ExpirationDate")::date - CURRENT_DATE                             AS "DaysRemaining",
    CASE
        WHEN (inv."ExpirationDate")::date - CURRENT_DATE < 1   THEN '< 1 day'
        WHEN (inv."ExpirationDate")::date - CURRENT_DATE < 7   THEN '< 1 week'
        WHEN (inv."ExpirationDate")::date - CURRENT_DATE < 14  THEN '< 2 weeks'
        WHEN (inv."ExpirationDate")::date - CURRENT_DATE < 21  THEN '< 3 weeks'
        WHEN (inv."ExpirationDate")::date - CURRENT_DATE < 30  THEN '< 1 month'
        WHEN (inv."ExpirationDate")::date - CURRENT_DATE < 60  THEN '< 2 months'
        WHEN (inv."ExpirationDate")::date - CURRENT_DATE < 90  THEN '< 3 months'
        ELSE '≥ 3 months'
    END                                                                     AS "ValidityLabel",
    CASE
        WHEN (inv."ExpirationDate")::date - CURRENT_DATE < 7   THEN 'st-expiring'
        WHEN (inv."ExpirationDate")::date - CURRENT_DATE < 30  THEN 'st-pending'
        ELSE 'st-approved'
    END                                                                     AS "ValidityCssClass",
    COALESCE(p."IsArchived", FALSE)                                         AS "IsArchived"
FROM "InvitationItems" ii
INNER JOIN "Invitations" inv
    ON inv."ID" = ii."InvitationID" AND COALESCE(inv."GCRecord", 0) = 0
INNER JOIN "People" p
    ON p."ID" = ii."PersonID" AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "ApplicationProfileInstances" a
    ON a."ID" = inv."ApplicationProfileInstanceID" AND COALESCE(a."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" apc
    ON apc."ID" = a."ProjectContractID" AND COALESCE(apc."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = p."ProjectContractID" AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" sp
    ON sp."ID" = p."SponsoringEmployeeID" AND COALESCE(sp."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" spc
    ON spc."ID" = sp."ProjectContractID" AND COALESCE(spc."GCRecord", 0) = 0
WHERE COALESCE(ii."GCRecord", 0) = 0
  AND COALESCE(ii."IsUsed", FALSE) = FALSE
  AND COALESCE(ii."IsCancelled", FALSE) = FALSE
  AND COALESCE(ii."IsChanged", FALSE) = FALSE
  AND ii."PersonID" IS NOT NULL
  AND inv."ExpirationDate" IS NOT NULL
  AND (inv."ExpirationDate")::date >= CURRENT_DATE;