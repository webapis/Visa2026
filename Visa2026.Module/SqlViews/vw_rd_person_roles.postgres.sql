-- Report Dashboard: person-type tab counts (PostgreSQL).
DROP VIEW IF EXISTS vw_rd_person_roles;
CREATE VIEW vw_rd_person_roles AS
SELECT
    p."PersonRole"                                                      AS "PersonRoleCode",
    COUNT(*)::bigint                                                    AS "PersonCount"
FROM "People" p
WHERE COALESCE(p."GCRecord", 0) = 0
  AND COALESCE(p."IsArchived", FALSE) = FALSE
GROUP BY p."PersonRole";
