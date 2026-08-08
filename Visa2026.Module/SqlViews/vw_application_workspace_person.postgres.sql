-- Application workspace: linked people per Application (PostgreSQL).
DROP VIEW IF EXISTS vw_application_workspace_person;
CREATE VIEW vw_application_workspace_person AS
SELECT
    ap."ID"                                                          AS "ApplicationPersonId",
    ap."ApplicationId"                                               AS "ApplicationId",
    ap."PersonId"                                                    AS "PersonId",
    trim(both ' ' from concat_ws(' ',
        NULLIF(p."FirstName", ''),
        NULLIF(p."MiddleName", ''),
        NULLIF(p."LastName", '')))                                   AS "FullName",
    p."PersonRole"                                                   AS "PersonRole",
    p."PersonalNumber"                                               AS "PersonalNumber",
    ap."LinkedAt"                                                    AS "LinkedAt"
FROM "ApplicationPeople" ap
INNER JOIN "People" p ON p."ID" = ap."PersonId"
WHERE COALESCE(ap."GCRecord", 0) = 0
  AND COALESCE(p."GCRecord", 0) = 0;
