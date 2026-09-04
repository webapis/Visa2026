-- Report Dashboard: To Be Checked In (Registration).
-- Valid visas with no registration visa link on the M2M roster (ResolvedLink LinkKind = Visa).
-- Person must be in-country: latest TravelHistory is ExternalArrival.
-- Chart: days since that arrival TravelDate.
DROP VIEW IF EXISTS vw_rd_to_be_checked_in;
CREATE VIEW vw_rd_to_be_checked_in AS
WITH reg_linked AS (
    SELECT DISTINCT rl."LinkedObjectId" AS "VisaId"
    FROM "ApplicationProfileInstancePersonResolvedLinks" rl
    INNER JOIN "ApplicationProfileInstancePeople" ap
        ON ap."ApplicationProfileInstanceId" = rl."ApplicationProfileInstanceId" AND ap."PersonId" = rl."PersonId"
    INNER JOIN "ApplicationProfileInstances" a
        ON a."ID" = ap."ApplicationProfileInstanceId" AND COALESCE(a."GCRecord", 0) = 0
    INNER JOIN "ApplicationProfiles" apf
        ON apf."ID" = a."ApplicationProfileID" AND COALESCE(apf."GCRecord", 0) = 0
    WHERE COALESCE(rl."GCRecord", 0) = 0
      AND rl."LinkKind" = 1
      AND rl."LinkedObjectId" IS NOT NULL
      AND COALESCE(apf."ActionFamily", 0) = 2
),
latest_travel AS (
    SELECT DISTINCT ON (th."PersonID")
        th."PersonID",
        th."Discriminator",
        th."TravelDate" AS "EntryDate"
    FROM "TravelHistories" th
    WHERE COALESCE(th."GCRecord", 0) = 0
    ORDER BY th."PersonID", th."TravelDate" DESC NULLS LAST, th."ID" DESC
)
SELECT
    v."ID" AS "ID",
    p."ID" AS "PersonOid",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p."FirstName"), ''),
        NULLIF(BTRIM(p."MiddleName"), ''),
        NULLIF(BTRIM(p."LastName"), '')
    ) AS "PersonName",
    COALESCE(
        NULLIF(BTRIM(pc."NameTm"), ''),
        NULLIF(BTRIM(spc."NameTm"), ''),
        ''
    ) AS "ProjectName",
    COALESCE(pc."NameTm", spc."NameTm", '') AS "ProjectNameRaw",
    COALESCE(pc."NameTm", spc."NameTm", '') AS "ProjectNameTm",
    p."PersonRole" AS "PersonRoleCode",
    COALESCE(NULLIF(BTRIM(v."VisaNumber"), ''), '') AS "VisaNumber",
    v."ExpirationDate" AS "VisaExpirationDate",
    lt."EntryDate" AS "EntryDate",
    (CURRENT_DATE - (lt."EntryDate")::date) AS "DaysSinceEntry",
    CASE
        WHEN CURRENT_DATE - (lt."EntryDate")::date < 7  THEN '< 1 week'
        WHEN CURRENT_DATE - (lt."EntryDate")::date < 14 THEN '< 2 weeks'
        WHEN CURRENT_DATE - (lt."EntryDate")::date < 21 THEN '< 3 weeks'
        WHEN CURRENT_DATE - (lt."EntryDate")::date < 28 THEN '< 4 weeks'
        WHEN CURRENT_DATE - (lt."EntryDate")::date < 30 THEN '< 1 month'
        ELSE '≥ 1 month'
    END AS "EntryBucketLabel",
    CASE
        WHEN CURRENT_DATE - (lt."EntryDate")::date < 14 THEN 'st-expiring'
        WHEN CURRENT_DATE - (lt."EntryDate")::date < 30 THEN 'st-pending'
        ELSE 'st-approved'
    END AS "EntryBucketCssClass",
    COALESCE(p."IsArchived", FALSE) AS "IsArchived"
FROM "Visas" v
INNER JOIN "Passports" pp
    ON pp."ID" = v."PassportID" AND COALESCE(pp."GCRecord", 0) = 0
INNER JOIN "People" p
    ON p."ID" = pp."PersonID" AND COALESCE(p."GCRecord", 0) = 0
INNER JOIN latest_travel lt
    ON lt."PersonID" = p."ID"
   AND lt."Discriminator" = 'ExternalArrival'
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = p."ProjectContractID" AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" sp
    ON sp."ID" = p."SponsoringEmployeeID" AND COALESCE(sp."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" spc
    ON spc."ID" = sp."ProjectContractID" AND COALESCE(spc."GCRecord", 0) = 0
WHERE COALESCE(v."GCRecord", 0) = 0
  AND NOT EXISTS (
        SELECT 1 FROM reg_linked rl WHERE rl."VisaId" = v."ID"
  );
