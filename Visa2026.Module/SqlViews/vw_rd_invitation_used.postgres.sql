-- Report Dashboard: Used Invitations — PostgreSQL.
-- Population: one invitation line that issued one visa (Visa.IssuingInvitationItem).
-- A visa cancelled later still counts. Linked to an Application Profile Instance.
-- One row per person on the invitation.
DROP VIEW IF EXISTS vw_rd_invitation_used;
CREATE VIEW vw_rd_invitation_used AS
SELECT
    ii."ID"                                                                 AS "ID",
    p."ID"                                                                  AS "PersonOid",
    inv."ID"                                                                AS "InvitationOid",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p."FirstName"), ''),
        NULLIF(BTRIM(p."MiddleName"), ''),
        NULLIF(BTRIM(p."LastName"), '')
    )                                                                       AS "PersonName",
    COALESCE(
        NULLIF(BTRIM(apc."NameTm"), ''),
        NULLIF(BTRIM(pc."NameTm"), ''),
        NULLIF(BTRIM(spc."NameTm"), ''),
        '(No project)'
    )                                                                       AS "ProjectName",
    COALESCE(apc."NameTm", pc."NameTm", spc."NameTm", '')                   AS "ProjectNameRaw",
    COALESCE(apc."NameTm", pc."NameTm", spc."NameTm", '')                   AS "ProjectNameTm",
    p."PersonRole"                                                          AS "PersonRoleCode",
    COALESCE(NULLIF(BTRIM(act."Name"), ''), '')                             AS "PositionLabel",
    COALESCE(
        NULLIF(BTRIM(sub."NameTm"), ''),
        NULLIF(BTRIM(sub."Name"), ''),
        ''
    )                                                                       AS "SubcontractorLabel",
    COALESCE(
        NULLIF(BTRIM(a."FullApplicationNumber"), ''),
        NULLIF(BTRIM(a."ApplicationNumber"), ''),
        ''
    )                                                                       AS "ApplicationNumber",
    CASE
        WHEN a."ApplicationDate" IS NOT NULL AND (a."ApplicationDate")::date > DATE '1900-01-01'
            THEN a."ApplicationDate"
        ELSE NULL
    END                                                                     AS "ApplicationDate",
    COALESCE(NULLIF(BTRIM(inv."InvitationNumber"), ''), '')                 AS "InvitationNumber",
    CASE WHEN (inv."ExpirationDate")::date > DATE '1900-01-01' THEN inv."ExpirationDate" ELSE NULL END AS "ExpirationDate",
    CASE WHEN (inv."StartDate")::date > DATE '1900-01-01' THEN inv."StartDate" ELSE NULL END AS "IssuedDate",
    COALESCE(
        NULLIF(BTRIM(vp."NameTm"), ''),
        NULLIF(BTRIM(vp."Name"), ''),
        '(No period)'
    )                                                                       AS "VisaPeriodLabel",
    COALESCE(
        NULLIF(BTRIM(vc."NameTm"), ''),
        NULLIF(BTRIM(vc."Name"), ''),
        '(No category)'
    )                                                                       AS "VisaCategoryLabel",
    COALESCE(
        NULLIF(BTRIM(vt."NameTm"), ''),
        NULLIF(BTRIM(vt."Name"), ''),
        '(No type)'
    )                                                                       AS "VisaTypeLabel",
    CONCAT_WS(' · ',
        COALESCE(NULLIF(BTRIM(vp."NameTm"), ''), NULLIF(BTRIM(vp."Name"), ''), '(No period)'),
        COALESCE(NULLIF(BTRIM(vc."NameTm"), ''), NULLIF(BTRIM(vc."Name"), ''), '(No category)'),
        COALESCE(NULLIF(BTRIM(vt."NameTm"), ''), NULLIF(BTRIM(vt."Name"), ''), '(No type)')
    )                                                                       AS "PeriodCategoryTypeLabel",
    COALESCE(mig_issued."Date", mig_started."Date")                         AS "MigrationDate",
    COALESCE(mig_issued."ProcessNumber", mig_started."ProcessNumber", '')   AS "MigrationResultNumber",
    CASE
        WHEN COALESCE(mig_issued."Date", mig_started."Date") IS NULL THEN ''
        ELSE to_char(COALESCE(mig_issued."Date", mig_started."Date"), 'DD.MM.YYYY')
            || CASE
                WHEN COALESCE(mig_issued."ProcessNumber", mig_started."ProcessNumber", '') = '' THEN ''
                ELSE chr(10) || '№ ' || COALESCE(mig_issued."ProcessNumber", mig_started."ProcessNumber")
            END
    END                                                                     AS "MigrationLabel",
    COALESCE(apf."ChangeInvitations", FALSE)                                AS "ValidityExtended",
    ministries."Approval1Date"                                              AS "Approval1Date",
    ministries."Approval1Number"                                            AS "Approval1Number",
    ministries."Approval1Ministry"                                          AS "Approval1Ministry",
    ministries."Approval2Date"                                              AS "Approval2Date",
    ministries."Approval2Number"                                            AS "Approval2Number",
    ministries."Approval2Ministry"                                          AS "Approval2Ministry",
    ministries."Approval3Date"                                              AS "Approval3Date",
    ministries."Approval3Number"                                            AS "Approval3Number",
    ministries."Approval3Ministry"                                          AS "Approval3Ministry",
    COALESCE(
        NULLIF(BTRIM(apc."NameTm"), ''),
        NULLIF(BTRIM(pc."NameTm"), ''),
        NULLIF(BTRIM(spc."NameTm"), ''),
        '(No project)'
    )                                                                       AS "StatusLabel",
    'st-cat-1'                                                              AS "StatusCssClass",
    COALESCE(p."IsArchived", FALSE)                                         AS "IsArchived",
    issued_visa."ID"                                                        AS "VisaOid",
    COALESCE(NULLIF(BTRIM(issued_visa."VisaNumber"), ''), '')               AS "VisaNumber",
    CASE
        WHEN issued_visa."IssueDate" IS NOT NULL AND (issued_visa."IssueDate")::date > DATE '1900-01-01'
            THEN issued_visa."IssueDate"
        ELSE NULL
    END                                                                     AS "VisaIssueDate",
    COALESCE(
        NULLIF(BTRIM(place."NameTm"), ''),
        NULLIF(BTRIM(place."Name"), ''),
        ''
    )                                                                       AS "VisaIssuedPlaceLabel",
    CASE
        WHEN issued_visa."StartDate" IS NOT NULL AND (issued_visa."StartDate")::date > DATE '1900-01-01'
            THEN issued_visa."StartDate"
        ELSE NULL
    END                                                                     AS "VisaStartDate",
    CASE
        WHEN issued_visa."ExpirationDate" IS NOT NULL AND (issued_visa."ExpirationDate")::date > DATE '1900-01-01'
            THEN issued_visa."ExpirationDate"
        ELSE NULL
    END                                                                     AS "VisaEndDate"
FROM "InvitationItems" ii
INNER JOIN "Invitations" inv
    ON inv."ID" = ii."InvitationID" AND COALESCE(inv."GCRecord", 0) = 0
INNER JOIN "People" p
    ON p."ID" = ii."PersonID" AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "VisaPeriods" vp
    ON vp."ID" = inv."VisaPeriodID" AND COALESCE(vp."GCRecord", 0) = 0
LEFT JOIN "VisaCategories" vc
    ON vc."ID" = inv."VisaCategoryID" AND COALESCE(vc."GCRecord", 0) = 0
INNER JOIN "ApplicationProfileInstances" a
    ON a."ID" = inv."ApplicationProfileInstanceID" AND COALESCE(a."GCRecord", 0) = 0
LEFT JOIN "ApplicationProfiles" apf
    ON apf."ID" = a."ApplicationProfileID" AND COALESCE(apf."GCRecord", 0) = 0
LEFT JOIN "VisaTypes" vt
    ON vt."ID" = a."VisaTypeID" AND COALESCE(vt."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" apc
    ON apc."ID" = a."ProjectContractID" AND COALESCE(apc."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = p."ProjectContractID" AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" sp
    ON sp."ID" = p."SponsoringEmployeeID" AND COALESCE(sp."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" spc
    ON spc."ID" = sp."ProjectContractID" AND COALESCE(spc."GCRecord", 0) = 0
LEFT JOIN "Subcontractors" sub
    ON sub."ID" = p."SubcontractorID" AND COALESCE(sub."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT eph."ActualPositionID"
    FROM "EmployeePositionHistories" eph
    WHERE eph."PersonID" = p."ID"
      AND COALESCE(eph."GCRecord", 0) = 0
    ORDER BY
        CASE
            WHEN eph."EndDate" IS NULL OR (eph."EndDate")::date >= CURRENT_DATE THEN 0
            ELSE 1
        END,
        eph."StartDate" DESC NULLS LAST,
        eph."ID" DESC
    LIMIT 1
) cur_pos ON TRUE
LEFT JOIN "ActualPositions" act
    ON act."ID" = cur_pos."ActualPositionID" AND COALESCE(act."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT pr."Date" AS "Date", NULLIF(BTRIM(pr."ProcessNumber"), '') AS "ProcessNumber"
    FROM "ApplicationProfileInstanceProgresses" pr
    INNER JOIN "ApplicationStates" st
        ON st."ID" = pr."StateID" AND COALESCE(st."GCRecord", 0) = 0
    WHERE a."ID" IS NOT NULL
      AND pr."ApplicationProfileInstanceID" = a."ID"
      AND COALESCE(pr."GCRecord", 0) = 0
      AND BTRIM(COALESCE(st."Code", '')) = 'PROCESS_ISSUED'
    ORDER BY pr."Date" DESC NULLS LAST, pr."ID" DESC
    LIMIT 1
) mig_issued ON TRUE
LEFT JOIN LATERAL (
    SELECT pr."Date" AS "Date", NULLIF(BTRIM(pr."ProcessNumber"), '') AS "ProcessNumber"
    FROM "ApplicationProfileInstanceProgresses" pr
    INNER JOIN "ApplicationStates" st
        ON st."ID" = pr."StateID" AND COALESCE(st."GCRecord", 0) = 0
    WHERE a."ID" IS NOT NULL
      AND pr."ApplicationProfileInstanceID" = a."ID"
      AND COALESCE(pr."GCRecord", 0) = 0
      AND BTRIM(COALESCE(st."Code", '')) = 'PROCESS_STARTED'
    ORDER BY pr."Date" DESC NULLS LAST, pr."ID" DESC
    LIMIT 1
) mig_started ON TRUE
LEFT JOIN LATERAL (
    SELECT
        MAX(dec.d) FILTER (WHERE legs.seq = 1) AS "Approval1Date",
        MAX(dec.n) FILTER (WHERE legs.seq = 1) AS "Approval1Number",
        MAX(legs.name) FILTER (WHERE legs.seq = 1) AS "Approval1Ministry",
        MAX(dec.d) FILTER (WHERE legs.seq = 2) AS "Approval2Date",
        MAX(dec.n) FILTER (WHERE legs.seq = 2) AS "Approval2Number",
        MAX(legs.name) FILTER (WHERE legs.seq = 2) AS "Approval2Ministry",
        MAX(dec.d) FILTER (WHERE legs.seq = 3) AS "Approval3Date",
        MAX(dec.n) FILTER (WHERE legs.seq = 3) AS "Approval3Number",
        MAX(legs.name) FILTER (WHERE legs.seq = 3) AS "Approval3Ministry"
    FROM (
        SELECT snap."Sequence" AS seq, BTRIM(snap."MinistryShortName") AS name
        FROM "ApplicationProfileInstanceApprovalLegSnapshots" snap
        WHERE a."ID" IS NOT NULL
          AND snap."ApplicationProfileInstanceId" = a."ID"
          AND COALESCE(snap."GCRecord", 0) = 0
        UNION ALL
        SELECT leg."Sequence", BTRIM(m."ShortNameTm")
        FROM "ApprovalLegProfileMinistryLegs" leg
        INNER JOIN "ApprovingMinistries" m
            ON m."ID" = leg."ApprovingMinistryId" AND COALESCE(m."GCRecord", 0) = 0
        WHERE a."ApprovalLegProfileID" IS NOT NULL
          AND leg."ApprovalLegProfileId" = a."ApprovalLegProfileID"
          AND COALESCE(leg."GCRecord", 0) = 0
          AND NOT EXISTS (
              SELECT 1
              FROM "ApplicationProfileInstanceApprovalLegSnapshots" snap2
              WHERE snap2."ApplicationProfileInstanceId" = a."ID"
                AND COALESCE(snap2."GCRecord", 0) = 0
          )
    ) legs
    LEFT JOIN LATERAL (
        SELECT pr."Date" AS d,
               COALESCE(
                   NULLIF(BTRIM(pr."ResultNumber"), ''),
                   CASE
                       WHEN position(':' IN COALESCE(pr."Description", '')) > 0
                            AND BTRIM(substring(pr."Description" FROM position(':' IN pr."Description") + 1)) ~ '[0-9]'
                       THEN BTRIM(substring(pr."Description" FROM position(':' IN pr."Description") + 1))
                       ELSE NULL
                   END
               ) AS n
        FROM "ApplicationProfileInstanceProgresses" pr
        INNER JOIN "ApplicationStates" st
            ON st."ID" = pr."StateID" AND COALESCE(st."GCRecord", 0) = 0
        WHERE a."ID" IS NOT NULL
          AND pr."ApplicationProfileInstanceID" = a."ID"
          AND COALESCE(pr."GCRecord", 0) = 0
          AND legs.seq BETWEEN 1 AND 5
          AND BTRIM(COALESCE(st."Code", '')) IN (
              legs.seq::text || '_REVIEW_APPROVED',
              legs.seq::text || '_REVIEW_REJECTED')
        ORDER BY pr."Date" DESC NULLS LAST, pr."ID" DESC
        LIMIT 1
    ) dec ON TRUE
) ministries ON TRUE
LEFT JOIN LATERAL (
    SELECT
        vis."ID",
        vis."VisaNumber",
        vis."IssueDate",
        vis."StartDate",
        vis."ExpirationDate",
        vis."VisaIssuedPlaceID"
    FROM "Visas" vis
    WHERE vis."IssuingInvitationItemID" = ii."ID"
      AND COALESCE(vis."GCRecord", 0) = 0
    ORDER BY vis."IssueDate" DESC NULLS LAST, vis."ID" DESC
    LIMIT 1
) issued_visa ON TRUE
LEFT JOIN "VisaIssuedPlaces" place
    ON place."ID" = issued_visa."VisaIssuedPlaceID" AND COALESCE(place."GCRecord", 0) = 0
WHERE COALESCE(ii."GCRecord", 0) = 0
  AND inv."ApplicationProfileInstanceID" IS NOT NULL
  AND issued_visa."ID" IS NOT NULL
  AND ii."PersonID" IS NOT NULL;
