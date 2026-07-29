-- Report Dashboard: Active WorkPermit (P) — valid WorkPermitItems by project.
-- One row per valid (non-cancelled, not expired) item; persons may appear more than once.
-- StatusLabel = Project (Person.ProjectContract, else sponsor).
DROP VIEW IF EXISTS vw_rd_work_permit_active;
CREATE VIEW vw_rd_work_permit_active AS
SELECT
    wpi."ID"                                                                AS "ID",
    p."ID"                                                                  AS "PersonOid",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p."FirstName"), ''),
        NULLIF(BTRIM(p."MiddleName"), ''),
        NULLIF(BTRIM(p."LastName"), '')
    )                                                                       AS "PersonName",
    COALESCE(
        NULLIF(BTRIM(pc."NameTm"), ''),
        NULLIF(BTRIM(spc."NameTm"), ''),
        '(No project)'
    )                                                                       AS "ProjectName",
    COALESCE(pc."NameTm", spc."NameTm", '')                                 AS "ProjectNameRaw",
    COALESCE(pc."NameTm", spc."NameTm", '')                                 AS "ProjectNameTm",
    p."PersonRole"                                                          AS "PersonRoleCode",
    COALESCE(NULLIF(BTRIM(wpi."WorkPermitNumber"), ''), NULLIF(BTRIM(wpi."ASNumber"), ''), '') AS "WorkPermitNumber",
    CASE WHEN (wpi."ExpirationDate")::date > DATE '1900-01-01' THEN wpi."ExpirationDate" ELSE NULL END AS "ExpirationDate",
    COALESCE(
        NULLIF(BTRIM(pc."NameTm"), ''),
        NULLIF(BTRIM(spc."NameTm"), ''),
        '(No project)'
    )                                                                       AS "StatusLabel",
    'st-cat-1'                                                              AS "StatusCssClass",
    COALESCE(p."IsArchived", FALSE)                                         AS "IsArchived"
FROM "WorkPermitItems" wpi
INNER JOIN "People" p
    ON p."ID" = wpi."PersonID"
   AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = p."ProjectContractID" AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" sp
    ON sp."ID" = p."SponsoringEmployeeID" AND COALESCE(sp."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" spc
    ON spc."ID" = sp."ProjectContractID" AND COALESCE(spc."GCRecord", 0) = 0
WHERE COALESCE(wpi."GCRecord", 0) = 0
  AND COALESCE(wpi."IsCancelled", FALSE) = FALSE
  AND wpi."PersonID" IS NOT NULL
  AND wpi."ExpirationDate" IS NOT NULL
  AND (wpi."ExpirationDate")::date >= CURRENT_DATE;