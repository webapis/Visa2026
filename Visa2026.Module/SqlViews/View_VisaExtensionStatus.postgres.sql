-- M2M-only View_VisaExtensionStatus (heal/ModuleUpdater prefer ReportDashboardPostgresRosterSql.ViewVisaExtensionStatusSql).
DROP VIEW IF EXISTS "View_VisaExtensionStatus";
CREATE VIEW "View_VisaExtensionStatus" AS
WITH visa_ext_roster AS (
    SELECT
        md5(concat(ap."ApplicationProfileInstanceId"::text, ap."PersonId"::text))::uuid AS "LineId",
        a."ID" AS "ApplicationProfileInstanceID",
        ap."PersonId" AS "PersonID",
        rl_visa."LinkedObjectId" AS "ExpiringVisaID",
        rl_pass."LinkedObjectId" AS "PassportID"
    FROM "ApplicationProfileInstancePeople" ap
    INNER JOIN "ApplicationProfileInstances" a
        ON a."ID" = ap."ApplicationProfileInstanceId" AND COALESCE(a."GCRecord", 0) = 0
    INNER JOIN "ApplicationProfiles" apf
        ON apf."ID" = a."ApplicationProfileID" AND COALESCE(apf."GCRecord", 0) = 0
    INNER JOIN "ApplicationProfileInstancePersonResolvedLinks" rl_visa
        ON rl_visa."ApplicationProfileInstanceId" = ap."ApplicationProfileInstanceId" AND rl_visa."PersonId" = ap."PersonId"
       AND rl_visa."LinkKind" = 1
       AND rl_visa."LinkedObjectId" IS NOT NULL
       AND COALESCE(rl_visa."GCRecord", 0) = 0
    LEFT JOIN "ApplicationProfileInstancePersonResolvedLinks" rl_pass
        ON rl_pass."ApplicationProfileInstanceId" = ap."ApplicationProfileInstanceId" AND rl_pass."PersonId" = ap."PersonId"
       AND rl_pass."LinkKind" = 0
       AND COALESCE(rl_pass."GCRecord", 0) = 0
    WHERE COALESCE(apf."ProduceVisa", FALSE) = TRUE
      AND COALESCE(apf."RequirePersonVisa", FALSE) = TRUE
      AND COALESCE(apf."ProduceInvitation", FALSE) = FALSE
      AND COALESCE(apf."ActionFamily", 0) = 0
)
SELECT
    roster."LineId" AS "ID",
    roster."ApplicationProfileInstanceID",
    roster."ExpiringVisaID",
    roster."PersonID",
    roster."PassportID",
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
     WHERE iv."IssuingApplicationProfileInstanceID" = roster."ApplicationProfileInstanceID"
       AND roster."PassportID" IS NOT NULL
       AND iv."PassportID" = roster."PassportID"
     LIMIT 1) AS "IssuedVisaID",
    (SELECT ri."ID"
     FROM "Rejections" r
     JOIN "RejectionItems" ri ON ri."RejectionID" = r."ID"
     WHERE r."ApplicationProfileInstanceID" = a."ID" AND ri."PersonID" = roster."PersonID"
     LIMIT 1) AS "RejectionItemID"
FROM visa_ext_roster roster
JOIN "ApplicationProfileInstances" a ON roster."ApplicationProfileInstanceID" = a."ID"
LEFT JOIN "Visas" v ON roster."ExpiringVisaID" = v."ID"
LEFT JOIN LATERAL (
    SELECT ap."StateID", ap."Date", ap."Description"
    FROM "ApplicationProfileInstanceProgresses" ap
    WHERE ap."ApplicationProfileInstanceID" = a."ID"
    ORDER BY ap."Date" DESC NULLS LAST, ap."ID" DESC
    LIMIT 1
) latest_ap ON TRUE;