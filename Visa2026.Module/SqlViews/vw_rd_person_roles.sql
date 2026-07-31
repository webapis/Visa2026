-- Report Dashboard: person-type tab counts (Employees / Family / Temporary Visitors).
-- Non-archived people only; all people in role (project optional).
CREATE OR ALTER VIEW [dbo].[vw_rd_person_roles] AS
SELECT
    p.PersonRole                                                        AS PersonRoleCode,
    COUNT_BIG(*)                                                        AS PersonCount
FROM People p
WHERE ISNULL(p.GCRecord, 0) = 0
  AND ISNULL(p.IsArchived, 0) = 0
GROUP BY p.PersonRole;
