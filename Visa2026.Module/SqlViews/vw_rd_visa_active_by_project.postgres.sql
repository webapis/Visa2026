-- Active Visa (P): population from vw_rd_visa_by_period; StatusLabel = Project.
DROP VIEW IF EXISTS vw_rd_visa_active_by_project;
CREATE VIEW vw_rd_visa_active_by_project AS
SELECT
    b."ID",
    b."PersonOid",
    b."PassportID",
    b."PassportNumber",
    b."PersonName",
    b."ProjectName",
    b."ProjectNameRaw",
    b."ProjectNameTm",
    b."PersonRoleCode",
    b."VisaNumber",
    b."ExpirationDate",
    b."PeriodDays",
    b."PeriodLabel",
    COALESCE(NULLIF(BTRIM(b."ProjectName"), ''), '(No project)') AS "StatusLabel",
    b."StatusCssClass",
    b."DaysRemaining",
    b."IsOneLastValidPerPerson",
    b."IsArchived"
FROM vw_rd_visa_by_period b;
