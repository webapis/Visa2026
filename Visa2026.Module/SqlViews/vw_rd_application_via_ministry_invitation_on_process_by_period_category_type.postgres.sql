-- Invitation on Process (V) — PostgreSQL.
DROP VIEW IF EXISTS vw_rd_application_via_ministry_invitation_on_process_by_period_category_type;
CREATE VIEW vw_rd_application_via_ministry_invitation_on_process_by_period_category_type AS
SELECT
    b."ID", b."ApplicationOid", b."ApplicationItemOid", b."PersonOid", b."CurrentStateID",
    b."PersonName", b."ProjectName", b."ProjectNameRaw", b."ProjectNameTm", b."PersonRoleCode",
    b."PositionLabel", b."ApplicationTypeLabel", b."VisaPeriodLabel", b."VisaTypeLabel", b."ApplicationNumber", b."ApplicationDate",
    b."ProgressStateCode", b."StatusLabel", b."StatusCssClass", b."IsArchived",
    COALESCE(NULLIF(BTRIM(vp."NameTm"), ''), NULLIF(BTRIM(vp."Name"), ''), '(No period)') AS "PeriodLabel",
    COALESCE(NULLIF(BTRIM(vc."NameTm"), ''), NULLIF(BTRIM(vc."Name"), ''), '(No category)') AS "CategoryLabel",
    COALESCE(NULLIF(BTRIM(vt."NameTm"), ''), NULLIF(BTRIM(vt."Name"), ''), '(No type)') AS "TypeLabel"
FROM vw_rd_application_via_ministry_invitation_on_process b
LEFT JOIN "Applications" a ON a."ID" = b."ApplicationOid" AND COALESCE(a."GCRecord", 0) = 0
LEFT JOIN "VisaPeriods" vp ON vp."ID" = a."VisaPeriodID" AND COALESCE(vp."GCRecord", 0) = 0
LEFT JOIN "VisaCategories" vc ON vc."ID" = a."VisaCategoryID" AND COALESCE(vc."GCRecord", 0) = 0
LEFT JOIN "VisaTypes" vt ON vt."ID" = a."VisaTypeID" AND COALESCE(vt."GCRecord", 0) = 0;
