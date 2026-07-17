-- PostgreSQL counterpart of SqlViewsUpdater.CreateViewVisaExtensionStatus (SQL Server).
-- Note: ApplicationItems."CurrentVisaId" (mixed case) — not CurrentVisaID.
DROP VIEW IF EXISTS "View_VisaExtensionStatus";
CREATE VIEW "View_VisaExtensionStatus" AS
SELECT
    ai."ID",
    ai."ApplicationID",
    ai."CurrentVisaId" AS "ExpiringVisaID",
    ai."PersonID",
    ai."CurrentPassportID" AS "PassportID",
    a."ApplicationNumber",
    a."ApplicationDate",
    latest_ap."StateID" AS "CurrentStateID",
    latest_ap."Date" AS "StatusDate",
    latest_ap."Description" AS "StatusDescription",
    CASE
        WHEN COALESCE(v."IsCancelled", FALSE) THEN 0
        WHEN v."ExpirationDate" IS NULL THEN 0
        WHEN (v."ExpirationDate"::date - CURRENT_DATE) < 0 THEN 0
        ELSE (v."ExpirationDate"::date - CURRENT_DATE)
    END AS "DaysRemainingOnVisa",
    (SELECT iv."ID" FROM "Visas" iv
     WHERE iv."IssuingApplicationItemID" = ai."ID"
     LIMIT 1) AS "IssuedVisaID",
    (SELECT ri."ID"
     FROM "Rejections" r
     JOIN "RejectionItems" ri ON ri."RejectionID" = r."ID"
     WHERE r."ApplicationID" = a."ID" AND ri."PersonID" = ai."PersonID"
     LIMIT 1) AS "RejectionItemID"
FROM "ApplicationItems" ai
JOIN "Applications" a ON ai."ApplicationID" = a."ID"
JOIN "ApplicationTypes" at ON a."ApplicationTypeID" = at."ID"
LEFT JOIN "Visas" v ON ai."CurrentVisaId" = v."ID"
LEFT JOIN LATERAL (
    SELECT ap."StateID", ap."Date", ap."Description"
    FROM "ApplicationProgresses" ap
    WHERE ap."ApplicationID" = a."ID"
    ORDER BY ap."Date" DESC NULLS LAST, ap."ID" DESC
    LIMIT 1
) latest_ap ON TRUE
WHERE at."Name" IN (
      'App_Visa_Ext',
      'App_Visa_Ext_According_to_WP',
      'App_Visa_Ext_FM',
      'App_Visa_and_WP_Ext'
);