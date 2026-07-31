-- Active Visa (V): same population; StatusLabel = Period · Category · Type.
DROP VIEW IF EXISTS vw_rd_visa_active_by_period_category_type;
CREATE VIEW vw_rd_visa_active_by_period_category_type AS
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
    CONCAT_WS(' · ',
        COALESCE(NULLIF(BTRIM(b."PeriodLabel"), ''), '(No period)'),
        COALESCE(NULLIF(BTRIM(c."CategoryLabel"), ''), '(No category)'),
        COALESCE(NULLIF(BTRIM(t."TypeLabel"), ''), '(No type)')
    ) AS "StatusLabel",
    b."StatusCssClass",
    b."DaysRemaining",
    b."IsOneLastValidPerPerson",
    b."IsArchived"
FROM vw_rd_visa_by_period b
LEFT JOIN vw_rd_visa_by_category c ON c."ID" = b."ID"
LEFT JOIN vw_rd_visa_by_type t ON t."ID" = b."ID";
