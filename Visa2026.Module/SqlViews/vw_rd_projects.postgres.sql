-- Report Dashboard: project chips (PostgreSQL). NameTm only on ProjectContracts.
DROP VIEW IF EXISTS vw_rd_projects;
CREATE VIEW vw_rd_projects AS
SELECT
    pc."ID"                                                                 AS "ProjectOid",
    p."PersonRole"                                                          AS "PersonRoleCode",
    COALESCE(NULLIF(BTRIM(pc."NameTm"), ''), '')                            AS "ProjectNameTm",
    COALESCE(NULLIF(BTRIM(pc."NameTm"), ''), '')                            AS "ProjectNameRaw",
    COUNT(*)::bigint                                                        AS "PersonCount"
FROM "People" p
LEFT JOIN "People" sp
    ON sp."ID" = p."SponsoringEmployeeID"
   AND COALESCE(sp."GCRecord", 0) = 0
INNER JOIN "ProjectContracts" pc
    ON pc."ID" = COALESCE(p."ProjectContractID", sp."ProjectContractID")
   AND COALESCE(pc."GCRecord", 0) = 0
WHERE COALESCE(p."GCRecord", 0) = 0
  AND COALESCE(p."IsArchived", FALSE) = FALSE
  AND COALESCE(p."ProjectContractID", sp."ProjectContractID") IS NOT NULL
GROUP BY
    pc."ID",
    p."PersonRole",
    COALESCE(NULLIF(BTRIM(pc."NameTm"), ''), '');
