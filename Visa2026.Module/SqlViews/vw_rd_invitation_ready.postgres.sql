-- Report Dashboard: Ready Invitations (ready-by-project / ready-by-period-category) — PostgreSQL.
DROP VIEW IF EXISTS vw_rd_invitation_ready;
CREATE VIEW vw_rd_invitation_ready AS
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
    COALESCE(
        NULLIF(BTRIM(vp."NameTm"), ''),
        NULLIF(BTRIM(vp."Name"), ''),
        '(No period)'
    )                                                                       AS "VisaPeriodLabel",
    COALESCE(
        NULLIF(BTRIM(vc."NameTm"), ''),
        NULLIF(BTRIM(vc."Name"), ''),
        '(No category)'
    )                                                                       AS "VisaCategoryLabel",
    COALESCE(
        NULLIF(BTRIM(vt."NameTm"), ''),
        NULLIF(BTRIM(vt."Name"), ''),
        '(No type)'
    )                                                                       AS "VisaTypeLabel",
    COALESCE(
        NULLIF(BTRIM(apc."NameTm"), ''),
        NULLIF(BTRIM(pc."NameTm"), ''),
        NULLIF(BTRIM(spc."NameTm"), ''),
        '(No project)'
    )                                                                       AS "StatusLabel",
    'st-cat-1'                                                              AS "StatusCssClass",
    COALESCE(p."IsArchived", FALSE)                                         AS "IsArchived"
FROM "InvitationItems" ii
INNER JOIN "Invitations" inv
    ON inv."ID" = ii."InvitationID" AND COALESCE(inv."GCRecord", 0) = 0
INNER JOIN "People" p
    ON p."ID" = ii."PersonID" AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "VisaPeriods" vp
    ON vp."ID" = inv."VisaPeriodID" AND COALESCE(vp."GCRecord", 0) = 0
LEFT JOIN "VisaCategories" vc
    ON vc."ID" = inv."VisaCategoryID" AND COALESCE(vc."GCRecord", 0) = 0
LEFT JOIN "ApplicationProfileInstances" a
    ON a."ID" = inv."ApplicationProfileInstanceID" AND COALESCE(a."GCRecord", 0) = 0
LEFT JOIN "VisaTypes" vt
    ON vt."ID" = a."VisaTypeID" AND COALESCE(vt."GCRecord", 0) = 0
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
