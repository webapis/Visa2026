-- ApplicationProfileInstance workspace: linked people per ApplicationProfileInstance (PostgreSQL).
DROP VIEW IF EXISTS vw_application_workspace_person;
CREATE VIEW vw_application_workspace_person AS
SELECT
    ap."PersonId"                                                    AS "PersonId",
    ap."ApplicationProfileInstanceId"                                AS "ApplicationProfileInstanceId",
    trim(both ' ' from concat_ws(' ',
        NULLIF(p."FirstName", ''),
        NULLIF(p."MiddleName", ''),
        NULLIF(p."LastName", '')))                                   AS "FullName",
    p."PersonRole"                                                   AS "PersonRole",
    p."PersonalNumber"                                               AS "PersonalNumber"
FROM "ApplicationProfileInstancePeople" ap
INNER JOIN "People" p ON p."ID" = ap."PersonId"
WHERE COALESCE(p."GCRecord", 0) = 0;
