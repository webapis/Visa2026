using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Creates Report Dashboard SQL views on PostgreSQL (SqlViewsUpdater is SQL Server-only).
/// </summary>
public sealed class ReportDashboardPostgresViewsUpdater : ModuleUpdater
{
    public ReportDashboardPostgresViewsUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        if (!DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
            return;

        CreateViewVisaExtensionStatus();
        CreateViewRdPassport();
        CreateViewRdWorkPermit();
        CreateViewRdVisaAppProgress();
        CreateViewRdProjects();
        CreateViewRdPersonRoles();
        CreateViewRdVisaState();
        CreateViewRdVisaByCategory();
        CreateViewRdVisaByType();
        CreateViewRdVisaByPeriod();
        CreateViewRdVisaByDaysRemaining();
        CreateViewRdApplication();
        CreateViewRdEducation();
        CreateViewRdEducationByCountry();
        CreateViewRdPositionHistory();
        CreateViewRdRegistration();
        CreateViewRdToBeCheckedIn();
        CreateViewRdToBeCheckedOut();
    }
private void CreateViewVisaExtensionStatus()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS ""View_VisaExtensionStatus"";", true);
        ExecuteNonQueryCommand(@"
-- PostgreSQL counterpart of SqlViewsUpdater.CreateViewVisaExtensionStatus (SQL Server).
-- Note: ApplicationItems.""CurrentVisaId"" (mixed case) — not CurrentVisaID.
CREATE VIEW ""View_VisaExtensionStatus"" AS
SELECT
    ai.""ID"",
    ai.""ApplicationID"",
    ai.""CurrentVisaId"" AS ""ExpiringVisaID"",
    ai.""PersonID"",
    ai.""CurrentPassportID"" AS ""PassportID"",
    a.""ApplicationNumber"",
    a.""ApplicationDate"",
    latest_ap.""StateID"" AS ""CurrentStateID"",
    latest_ap.""Date"" AS ""StatusDate"",
    latest_ap.""Description"" AS ""StatusDescription"",
    CASE
        WHEN COALESCE(v.""IsCancelled"", FALSE) THEN 0
        WHEN v.""ExpirationDate"" IS NULL THEN 0
        WHEN (v.""ExpirationDate""::date - CURRENT_DATE) < 0 THEN 0
        ELSE (v.""ExpirationDate""::date - CURRENT_DATE)
    END AS ""DaysRemainingOnVisa"",
    (SELECT iv.""ID"" FROM ""Visas"" iv
     WHERE iv.""IssuingApplicationItemID"" = ai.""ID""
     LIMIT 1) AS ""IssuedVisaID"",
    (SELECT ri.""ID""
     FROM ""Rejections"" r
     JOIN ""RejectionItems"" ri ON ri.""RejectionID"" = r.""ID""
     WHERE r.""ApplicationID"" = a.""ID"" AND ri.""PersonID"" = ai.""PersonID""
     LIMIT 1) AS ""RejectionItemID""
FROM ""ApplicationItems"" ai
JOIN ""Applications"" a ON ai.""ApplicationID"" = a.""ID""
JOIN ""ApplicationTypes"" at ON a.""ApplicationTypeID"" = at.""ID""
LEFT JOIN ""Visas"" v ON ai.""CurrentVisaId"" = v.""ID""
LEFT JOIN LATERAL (
    SELECT ap.""StateID"", ap.""Date"", ap.""Description""
    FROM ""ApplicationProgresses"" ap
    WHERE ap.""ApplicationID"" = a.""ID""
    ORDER BY ap.""Date"" DESC NULLS LAST, ap.""ID"" DESC
    LIMIT 1
) latest_ap ON TRUE
WHERE at.""Name"" IN (
      'App_Visa_Ext',
      'App_Visa_Ext_According_to_WP',
      'App_Visa_Ext_FM',
      'App_Visa_and_WP_Ext'
);
", true);
    }

    private void CreateViewRdPassport()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_passport;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: Passport (PostgreSQL).
-- One row per ApplicationItem that references a CurrentPassport.
-- Date filter (dashboard top-right) applies to Applications.ApplicationDate in the C# loader.
-- Soft-delete: COALESCE(""GCRecord"", 0) = 0. IsArchived is exposed for app-side toggle.
CREATE VIEW vw_rd_passport AS
SELECT
    ai.""ID""                                                                 AS ""ID"",
    pp.""ID""                                                                 AS ""PassportOid"",
    p.""ID""                                                                  AS ""PersonOid"",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p.""FirstName""), ''),
        NULLIF(BTRIM(p.""MiddleName""), ''),
        NULLIF(BTRIM(p.""LastName""), '')
    )                                                                       AS ""PersonName"",
    COALESCE(
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        ''
    )                                                                       AS ""ProjectName"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                                 AS ""ProjectNameRaw"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                                 AS ""ProjectNameTm"",
    p.""PersonRole""                                                          AS ""PersonRoleCode"",
    COALESCE(pp.""PassportNumber"", '')                                       AS ""PassportNumber"",
    pp.""IssueDate""                                                          AS ""IssueDate"",
    pp.""ExpirationDate""                                                     AS ""ExpirationDate"",
    a.""ApplicationDate""                                                     AS ""ApplicationDate"",
    COALESCE(NULLIF(BTRIM(pt.""NameTm""), ''), pt.""Name"", 'Unknown')          AS ""TypeLabel"",
    COALESCE(NULLIF(BTRIM(nat.""NameTm""), ''), nat.""Name"", 'Unknown')         AS ""CitizenshipLabel"",
    CASE
      WHEN pp.""ExpirationDate"" IS NULL                                      THEN 'Pending'
      WHEN (pp.""ExpirationDate"")::date < CURRENT_DATE                        THEN 'Expired'
      WHEN (pp.""ExpirationDate"")::date <= (CURRENT_DATE + INTERVAL '30 days')::date
                                                                             THEN 'Expiring (<30 days)'
      WHEN (pp.""ExpirationDate"")::date <= (CURRENT_DATE + INTERVAL '90 days')::date
                                                                             THEN 'Valid (31-90 days)'
      ELSE                                                                   'Valid (>90 days)'
    END                                                                     AS ""ValidityLabel"",
    CASE
      WHEN pp.""ExpirationDate"" IS NULL                                      THEN 'st-pending'
      WHEN (pp.""ExpirationDate"")::date < CURRENT_DATE                        THEN 'st-expiring'
      WHEN (pp.""ExpirationDate"")::date <= (CURRENT_DATE + INTERVAL '30 days')::date
                                                                             THEN 'st-expiring'
      WHEN (pp.""ExpirationDate"")::date <= (CURRENT_DATE + INTERVAL '90 days')::date
                                                                             THEN 'st-pending'
      ELSE                                                                   'st-approved'
    END                                                                     AS ""ValidityCssClass"",
    COALESCE(p.""IsArchived"", FALSE)                                         AS ""IsArchived""
FROM ""ApplicationItems"" ai
INNER JOIN ""Applications"" a
    ON a.""ID"" = ai.""ApplicationID""
   AND COALESCE(a.""GCRecord"", 0) = 0
INNER JOIN ""Passports"" pp
    ON pp.""ID"" = ai.""CurrentPassportID""
   AND COALESCE(pp.""GCRecord"", 0) = 0
INNER JOIN ""People"" p
    ON p.""ID"" = ai.""PersonID""
   AND COALESCE(p.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = COALESCE(a.""ProjectContractID"", p.""ProjectContractID"")
   AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sp
    ON sp.""ID"" = p.""SponsoringEmployeeID"" AND COALESCE(sp.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sp.""ProjectContractID"" AND COALESCE(spc.""GCRecord"", 0) = 0
LEFT JOIN ""PassportTypes"" pt
    ON pt.""ID"" = pp.""PassportTypeID"" AND COALESCE(pt.""GCRecord"", 0) = 0
LEFT JOIN ""Countries"" nat
    ON nat.""ID"" = p.""NationalityID"" AND COALESCE(nat.""GCRecord"", 0) = 0
WHERE COALESCE(ai.""GCRecord"", 0) = 0
  AND ai.""CurrentPassportID"" IS NOT NULL;
", true);
    }
    private void CreateViewRdWorkPermit()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_work_permit;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: valid WorkPermitItems by days remaining (By Days Remaining).
-- One row per valid (non-cancelled, not expired) item; persons may appear more than once.
-- Buckets: < 10 days / < 1 month / < 3..6 months / ≥ 6 months.
CREATE VIEW vw_rd_work_permit AS
SELECT
    wpi.""ID""                                                                AS ""ID"",
    p.""ID""                                                                  AS ""PersonOid"",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p.""FirstName""), ''),
        NULLIF(BTRIM(p.""MiddleName""), ''),
        NULLIF(BTRIM(p.""LastName""), '')
    )                                                                       AS ""PersonName"",
    COALESCE(
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        ''
    )                                                                       AS ""ProjectName"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                                 AS ""ProjectNameRaw"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                                 AS ""ProjectNameTm"",
    p.""PersonRole""                                                          AS ""PersonRoleCode"",
    COALESCE(NULLIF(BTRIM(wpi.""WorkPermitNumber""), ''), NULLIF(BTRIM(wpi.""ASNumber""), ''), '') AS ""WorkPermitNumber"",
    CASE WHEN (wpi.""ExpirationDate"")::date > DATE '1900-01-01' THEN wpi.""ExpirationDate"" ELSE NULL END AS ""ExpirationDate"",
    (wpi.""ExpirationDate"")::date - CURRENT_DATE                             AS ""DaysRemaining"",
    CASE
        WHEN (wpi.""ExpirationDate"")::date - CURRENT_DATE < 10  THEN '< 10 days'
        WHEN (wpi.""ExpirationDate"")::date - CURRENT_DATE < 30  THEN '< 1 month'
        WHEN (wpi.""ExpirationDate"")::date - CURRENT_DATE < 90  THEN '< 3 months'
        WHEN (wpi.""ExpirationDate"")::date - CURRENT_DATE < 120 THEN '< 4 months'
        WHEN (wpi.""ExpirationDate"")::date - CURRENT_DATE < 150 THEN '< 5 months'
        WHEN (wpi.""ExpirationDate"")::date - CURRENT_DATE < 180 THEN '< 6 months'
        ELSE '≥ 6 months'
    END                                                                     AS ""ValidityLabel"",
    CASE
        WHEN (wpi.""ExpirationDate"")::date - CURRENT_DATE < 30  THEN 'st-expiring'
        WHEN (wpi.""ExpirationDate"")::date - CURRENT_DATE < 90  THEN 'st-pending'
        ELSE 'st-approved'
    END                                                                     AS ""ValidityCssClass"",
    COALESCE(p.""IsArchived"", FALSE)                                         AS ""IsArchived""
FROM ""WorkPermitItems"" wpi
INNER JOIN ""People"" p
    ON p.""ID"" = wpi.""PersonID""
   AND COALESCE(p.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = p.""ProjectContractID"" AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sp
    ON sp.""ID"" = p.""SponsoringEmployeeID"" AND COALESCE(sp.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sp.""ProjectContractID"" AND COALESCE(spc.""GCRecord"", 0) = 0
WHERE COALESCE(wpi.""GCRecord"", 0) = 0
  AND COALESCE(wpi.""IsCancelled"", FALSE) = FALSE
  AND wpi.""PersonID"" IS NOT NULL
  AND wpi.""ExpirationDate"" IS NOT NULL
  AND (wpi.""ExpirationDate"")::date >= CURRENT_DATE;
", true);
    }
    private void CreateViewRdVisaAppProgress()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_visa_app_progress;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: Visa — Application Progress (PostgreSQL).
CREATE VIEW vw_rd_visa_app_progress AS
SELECT
    ai.""ID""                                                                 AS ""ID"",
    p.""ID""                                                                  AS ""PersonOid"",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p.""FirstName""), ''),
        NULLIF(BTRIM(p.""MiddleName""), ''),
        NULLIF(BTRIM(p.""LastName""), '')
    )                                                                       AS ""PersonName"",
    COALESCE(
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        ''
    )                                                                       AS ""ProjectName"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                                 AS ""ProjectNameRaw"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                                 AS ""ProjectNameTm"",
    p.""PersonRole""                                                          AS ""PersonRoleCode"",
    COALESCE(
        NULLIF(BTRIM(a.""FullApplicationNumber""), ''),
        NULLIF(BTRIM(a.""ApplicationNumber""), ''),
        ''
    )                                                                       AS ""ApplicationNumber"",
    a.""ApplicationDate""                                                     AS ""ApplicationDate"",
    COALESCE(
        NULLIF(BTRIM(ast.""NameTm""), ''),
        NULLIF(BTRIM(ast.""Name""), ''),
        'Being Prepared'
    )                                                                       AS ""ProgressStateLabel"",
    CASE
      WHEN ast.""Code"" IN ('PROCESS_ISSUED', '1_REVIEW_APPROVED', '2_REVIEW_APPROVED')
                                                                             THEN 'st-approved'
      WHEN ast.""Code"" IN ('PROCESS_REJECTED', 'PROCESS_CANCELLED', '1_REVIEW_REJECTED', '2_REVIEW_REJECTED')
                                                                             THEN 'st-expiring'
      ELSE                                                                   'st-pending'
    END                                                                     AS ""ProgressStateCssClass"",
    COALESCE(p.""IsArchived"", FALSE)                                         AS ""IsArchived""
FROM ""ApplicationItems"" ai
INNER JOIN ""Applications"" a
    ON a.""ID"" = ai.""ApplicationID""
   AND COALESCE(a.""GCRecord"", 0) = 0
INNER JOIN ""ApplicationTypes"" at
    ON at.""ID"" = a.""ApplicationTypeID""
   AND COALESCE(at.""GCRecord"", 0) = 0
INNER JOIN ""People"" p
    ON p.""ID"" = ai.""PersonID""
   AND COALESCE(p.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = COALESCE(a.""ProjectContractID"", p.""ProjectContractID"")
   AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sp
    ON sp.""ID"" = p.""SponsoringEmployeeID""
   AND COALESCE(sp.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sp.""ProjectContractID""
   AND COALESCE(spc.""GCRecord"", 0) = 0
LEFT JOIN LATERAL (
    SELECT ap.""StateID""
    FROM ""ApplicationProgresses"" ap
    WHERE ap.""ApplicationID"" = a.""ID""
      AND COALESCE(ap.""GCRecord"", 0) = 0
    ORDER BY ap.""Date"" DESC NULLS LAST, ap.""ID"" DESC
    LIMIT 1
) latest_ap ON TRUE
LEFT JOIN ""ApplicationStates"" ast
    ON ast.""ID"" = latest_ap.""StateID""
   AND COALESCE(ast.""GCRecord"", 0) = 0
WHERE COALESCE(ai.""GCRecord"", 0) = 0
  AND ai.""CurrentVisaId"" IS NOT NULL
  AND at.""Name"" IN (
        'App_Visa_Ext',
        'App_Visa_Ext_According_to_WP',
        'App_Visa_Ext_FM',
        'App_Visa_and_WP_Ext'
    );
", true);
    }
    private void CreateViewRdProjects()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_projects;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: project chips (PostgreSQL). NameTm only on ProjectContracts.
CREATE VIEW vw_rd_projects AS
SELECT
    pc.""ID""                                                                 AS ""ProjectOid"",
    p.""PersonRole""                                                          AS ""PersonRoleCode"",
    COALESCE(NULLIF(BTRIM(pc.""NameTm""), ''), '')                            AS ""ProjectNameTm"",
    COALESCE(NULLIF(BTRIM(pc.""NameTm""), ''), '')                            AS ""ProjectNameRaw"",
    COUNT(*)::bigint                                                        AS ""PersonCount""
FROM ""People"" p
LEFT JOIN ""People"" sp
    ON sp.""ID"" = p.""SponsoringEmployeeID""
   AND COALESCE(sp.""GCRecord"", 0) = 0
INNER JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = COALESCE(p.""ProjectContractID"", sp.""ProjectContractID"")
   AND COALESCE(pc.""GCRecord"", 0) = 0
WHERE COALESCE(p.""GCRecord"", 0) = 0
  AND COALESCE(p.""IsArchived"", FALSE) = FALSE
  AND COALESCE(p.""ProjectContractID"", sp.""ProjectContractID"") IS NOT NULL
GROUP BY
    pc.""ID"",
    p.""PersonRole"",
    COALESCE(NULLIF(BTRIM(pc.""NameTm""), ''), '');
", true);
    }
    private void CreateViewRdPersonRoles()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_person_roles;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: person-type tab counts (PostgreSQL).
CREATE VIEW vw_rd_person_roles AS
SELECT
    p.""PersonRole""                                                      AS ""PersonRoleCode"",
    COUNT(*)::bigint                                                    AS ""PersonCount""
FROM ""People"" p
WHERE COALESCE(p.""GCRecord"", 0) = 0
  AND COALESCE(p.""IsArchived"", FALSE) = FALSE
GROUP BY p.""PersonRole"";
", true);
    }
    private void CreateViewRdVisaState()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_visa_state;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: Visa State — Extension Started (PostgreSQL).
-- Plus: Application ProgressHistory must not contain PROCESS_CANCELLED.
CREATE VIEW vw_rd_visa_state AS
WITH ranked_visas AS (
    SELECT
        v.""ID"" AS ""VisaID"",
        pp.""PersonID"",
        v.""VisaNumber"",
        v.""ExpirationDate"",
        v.""StartDate"",
        v.""IssueDate"",
        ROW_NUMBER() OVER (
            PARTITION BY pp.""PersonID""
            ORDER BY v.""StartDate"" DESC NULLS LAST, v.""IssueDate"" DESC NULLS LAST, v.""ID"" DESC
        ) AS rn
    FROM ""Visas"" v
    INNER JOIN ""Passports"" pp
        ON pp.""ID"" = v.""PassportID""
       AND COALESCE(pp.""GCRecord"", 0) = 0
    WHERE COALESCE(v.""GCRecord"", 0) = 0
      AND COALESCE(v.""IsCancelled"", FALSE) = FALSE
      AND v.""StartDate"" IS NOT NULL
      AND (v.""StartDate"")::date > DATE '1900-01-01'
      AND (v.""StartDate"")::date <= CURRENT_DATE
),
ext_items AS (
    SELECT
        ai.""ID"" AS ""ApplicationItemID"",
        ai.""PersonID"",
        ai.""CurrentVisaId"" AS ""VisaID"",
        a.""ID"" AS ""ApplicationID"",
        a.""ApplicationNumber"",
        a.""FullApplicationNumber"",
        a.""ApplicationDate"",
        a.""ProjectContractID"" AS ""ApplicationProjectContractID""
    FROM ""ApplicationItems"" ai
    INNER JOIN ""Applications"" a
        ON a.""ID"" = ai.""ApplicationID""
       AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationTypes"" at
        ON at.""ID"" = a.""ApplicationTypeID""
       AND COALESCE(at.""GCRecord"", 0) = 0
    WHERE COALESCE(ai.""GCRecord"", 0) = 0
      AND ai.""CurrentVisaId"" IS NOT NULL
      AND at.""Name"" IN (
            'App_Visa_Ext',
            'App_Visa_Ext_According_to_WP',
            'App_Visa_Ext_FM',
            'App_Visa_and_WP_Ext'
        )
)
SELECT
    ei.""ApplicationItemID""                                              AS ""ID"",
    p.""ID""                                                              AS ""PersonOid"",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p.""FirstName""), ''),
        NULLIF(BTRIM(p.""MiddleName""), ''),
        NULLIF(BTRIM(p.""LastName""), '')
    )                                                                   AS ""PersonName"",
    COALESCE(
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        ''
    )                                                                   AS ""ProjectName"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                             AS ""ProjectNameRaw"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                             AS ""ProjectNameTm"",
    p.""PersonRole""                                                      AS ""PersonRoleCode"",
    COALESCE(NULLIF(BTRIM(rv.""VisaNumber""), ''), '')                    AS ""VisaNumber"",
    CASE WHEN (rv.""ExpirationDate"")::date > DATE '1900-01-01' THEN rv.""ExpirationDate"" ELSE NULL END AS ""ExpirationDate"",
    'Extension Started'                                                 AS ""StateLabel"",
    'st-pending'                                                        AS ""StateCssClass"",
    COALESCE(p.""IsArchived"", FALSE)                                     AS ""IsArchived""
FROM ext_items ei
INNER JOIN ranked_visas rv
    ON rv.""VisaID"" = ei.""VisaID""
   AND rv.""PersonID"" = ei.""PersonID""
   AND rv.rn = 1
INNER JOIN ""People"" p
    ON p.""ID"" = ei.""PersonID""
   AND COALESCE(p.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = COALESCE(ei.""ApplicationProjectContractID"", p.""ProjectContractID"")
   AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sp
    ON sp.""ID"" = p.""SponsoringEmployeeID""
   AND COALESCE(sp.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sp.""ProjectContractID""
   AND COALESCE(spc.""GCRecord"", 0) = 0
WHERE rv.""ExpirationDate"" IS NOT NULL
  AND (rv.""ExpirationDate"")::date >= CURRENT_DATE
  AND NOT EXISTS (
        SELECT 1
        FROM ""ApplicationProgresses"" ap
        INNER JOIN ""ApplicationStates"" ast
            ON ast.""ID"" = ap.""StateID""
           AND COALESCE(ast.""GCRecord"", 0) = 0
        WHERE ap.""ApplicationID"" = ei.""ApplicationID""
          AND COALESCE(ap.""GCRecord"", 0) = 0
          AND ast.""Code"" = 'PROCESS_CANCELLED'
      );
", true);
    }
    private void CreateViewRdVisaByCategory()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_visa_by_category;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: valid visas by VisaCategory only (not Visa State).
-- One row per valid visa (person may appear more than once).
CREATE VIEW vw_rd_visa_by_category AS
SELECT
    v.""ID""                                                              AS ""ID"",
    p.""ID""                                                              AS ""PersonOid"",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p.""FirstName""), ''),
        NULLIF(BTRIM(p.""MiddleName""), ''),
        NULLIF(BTRIM(p.""LastName""), '')
    )                                                                   AS ""PersonName"",
    COALESCE(
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        ''
    )                                                                   AS ""ProjectName"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                             AS ""ProjectNameRaw"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                             AS ""ProjectNameTm"",
    p.""PersonRole""                                                      AS ""PersonRoleCode"",
    COALESCE(NULLIF(BTRIM(v.""VisaNumber""), ''), '')                     AS ""VisaNumber"",
    CASE WHEN (v.""ExpirationDate"")::date > DATE '1900-01-01' THEN v.""ExpirationDate"" ELSE NULL END AS ""ExpirationDate"",
    COALESCE(NULLIF(BTRIM(vc.""NameTm""), ''), NULLIF(BTRIM(vc.""Name""), ''), 'Unknown') AS ""CategoryLabel"",
    COALESCE(NULLIF(BTRIM(vc.""NameTm""), ''), NULLIF(BTRIM(vc.""Name""), ''), 'Unknown') AS ""StatusLabel"",
    'st-cat-1'                                                          AS ""StatusCssClass"",
    COALESCE(p.""IsArchived"", FALSE)                                     AS ""IsArchived""
FROM ""Visas"" v
INNER JOIN ""Passports"" pp
    ON pp.""ID"" = v.""PassportID""
   AND COALESCE(pp.""GCRecord"", 0) = 0
INNER JOIN ""People"" p
    ON p.""ID"" = pp.""PersonID""
   AND COALESCE(p.""GCRecord"", 0) = 0
LEFT JOIN ""VisaCategories"" vc
    ON vc.""ID"" = v.""VisaCategoryID""
   AND COALESCE(vc.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = p.""ProjectContractID""
   AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sp
    ON sp.""ID"" = p.""SponsoringEmployeeID""
   AND COALESCE(sp.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sp.""ProjectContractID""
   AND COALESCE(spc.""GCRecord"", 0) = 0
WHERE COALESCE(v.""GCRecord"", 0) = 0
  AND COALESCE(v.""IsCancelled"", FALSE) = FALSE
  AND v.""ExpirationDate"" IS NOT NULL
  AND (v.""ExpirationDate"")::date >= CURRENT_DATE;
", true);
    }
    private void CreateViewRdVisaByType()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_visa_by_type;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: valid visas by VisaType only (not Visa State).
CREATE VIEW vw_rd_visa_by_type AS
SELECT
    v.""ID""                                                              AS ""ID"",
    p.""ID""                                                              AS ""PersonOid"",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p.""FirstName""), ''),
        NULLIF(BTRIM(p.""MiddleName""), ''),
        NULLIF(BTRIM(p.""LastName""), '')
    )                                                                   AS ""PersonName"",
    COALESCE(
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        ''
    )                                                                   AS ""ProjectName"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                             AS ""ProjectNameRaw"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                             AS ""ProjectNameTm"",
    p.""PersonRole""                                                      AS ""PersonRoleCode"",
    COALESCE(NULLIF(BTRIM(v.""VisaNumber""), ''), '')                     AS ""VisaNumber"",
    CASE WHEN (v.""ExpirationDate"")::date > DATE '1900-01-01' THEN v.""ExpirationDate"" ELSE NULL END AS ""ExpirationDate"",
    COALESCE(NULLIF(BTRIM(vt.""NameTm""), ''), NULLIF(BTRIM(vt.""Name""), ''), 'Unknown') AS ""TypeLabel"",
    COALESCE(NULLIF(BTRIM(vt.""NameTm""), ''), NULLIF(BTRIM(vt.""Name""), ''), 'Unknown') AS ""StatusLabel"",
    'st-cat-1'                                                          AS ""StatusCssClass"",
    COALESCE(p.""IsArchived"", FALSE)                                     AS ""IsArchived""
FROM ""Visas"" v
INNER JOIN ""Passports"" pp
    ON pp.""ID"" = v.""PassportID""
   AND COALESCE(pp.""GCRecord"", 0) = 0
INNER JOIN ""People"" p
    ON p.""ID"" = pp.""PersonID""
   AND COALESCE(p.""GCRecord"", 0) = 0
LEFT JOIN ""VisaTypes"" vt
    ON vt.""ID"" = v.""VisaTypeID""
   AND COALESCE(vt.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = p.""ProjectContractID""
   AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sp
    ON sp.""ID"" = p.""SponsoringEmployeeID""
   AND COALESCE(sp.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sp.""ProjectContractID""
   AND COALESCE(spc.""GCRecord"", 0) = 0
WHERE COALESCE(v.""GCRecord"", 0) = 0
  AND COALESCE(v.""IsCancelled"", FALSE) = FALSE
  AND v.""ExpirationDate"" IS NOT NULL
  AND (v.""ExpirationDate"")::date >= CURRENT_DATE;
", true);
    }
    private void CreateViewRdVisaByPeriod()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_visa_by_period;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: valid visas by nearest granted period (StartDate → ExpirationDate).
-- Chart labels: 1 month / 3 months / 6 months / 1 year. Valid visas only. No start/end columns in UI.
CREATE VIEW vw_rd_visa_by_period AS
SELECT
    x.""ID"",
    x.""PersonOid"",
    x.""PersonName"",
    x.""ProjectName"",
    x.""ProjectNameRaw"",
    x.""ProjectNameTm"",
    x.""PersonRoleCode"",
    x.""VisaNumber"",
    x.""ExpirationDate"",
    x.""PeriodDays"",
    x.""PeriodLabel"",
    x.""PeriodLabel""                                                     AS ""StatusLabel"",
    CASE x.""PeriodLabel""
        WHEN '1 month'   THEN 'st-cat-1'
        WHEN '3 months'  THEN 'st-cat-2'
        WHEN '6 months'  THEN 'st-cat-3'
        ELSE                  'st-cat-4'
    END                                                                 AS ""StatusCssClass"",
    x.""IsArchived""
FROM (
    SELECT
        v.""ID""                                                          AS ""ID"",
        p.""ID""                                                          AS ""PersonOid"",
        CONCAT_WS(' ',
            NULLIF(BTRIM(p.""FirstName""), ''),
            NULLIF(BTRIM(p.""MiddleName""), ''),
            NULLIF(BTRIM(p.""LastName""), '')
        )                                                               AS ""PersonName"",
        COALESCE(
            NULLIF(BTRIM(pc.""NameTm""), ''),
            NULLIF(BTRIM(spc.""NameTm""), ''),
            ''
        )                                                               AS ""ProjectName"",
        COALESCE(pc.""NameTm"", spc.""NameTm"", '')                         AS ""ProjectNameRaw"",
        COALESCE(pc.""NameTm"", spc.""NameTm"", '')                         AS ""ProjectNameTm"",
        p.""PersonRole""                                                  AS ""PersonRoleCode"",
        COALESCE(NULLIF(BTRIM(v.""VisaNumber""), ''), '')                 AS ""VisaNumber"",
        CASE WHEN (v.""ExpirationDate"")::date > DATE '1900-01-01' THEN v.""ExpirationDate"" ELSE NULL END AS ""ExpirationDate"",
        GREATEST(0, (v.""ExpirationDate"")::date - (v.""StartDate"")::date) AS ""PeriodDays"",
        CASE
            WHEN ABS(GREATEST(0, (v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 30)
               <= LEAST(
                    ABS(GREATEST(0, (v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 90),
                    ABS(GREATEST(0, (v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 180),
                    ABS(GREATEST(0, (v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 365)
                  )
                THEN '1 month'
            WHEN ABS(GREATEST(0, (v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 90)
               <= LEAST(
                    ABS(GREATEST(0, (v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 180),
                    ABS(GREATEST(0, (v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 365)
                  )
                THEN '3 months'
            WHEN ABS(GREATEST(0, (v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 180)
               <= ABS(GREATEST(0, (v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 365)
                THEN '6 months'
            ELSE '1 year'
        END                                                             AS ""PeriodLabel"",
        COALESCE(p.""IsArchived"", FALSE)                                 AS ""IsArchived""
    FROM ""Visas"" v
    INNER JOIN ""Passports"" pp
        ON pp.""ID"" = v.""PassportID""
       AND COALESCE(pp.""GCRecord"", 0) = 0
    INNER JOIN ""People"" p
        ON p.""ID"" = pp.""PersonID""
       AND COALESCE(p.""GCRecord"", 0) = 0
    LEFT JOIN ""ProjectContracts"" pc
        ON pc.""ID"" = p.""ProjectContractID""
       AND COALESCE(pc.""GCRecord"", 0) = 0
    LEFT JOIN ""People"" sp
        ON sp.""ID"" = p.""SponsoringEmployeeID""
       AND COALESCE(sp.""GCRecord"", 0) = 0
    LEFT JOIN ""ProjectContracts"" spc
        ON spc.""ID"" = sp.""ProjectContractID""
       AND COALESCE(spc.""GCRecord"", 0) = 0
    WHERE COALESCE(v.""GCRecord"", 0) = 0
      AND COALESCE(v.""IsCancelled"", FALSE) = FALSE
      AND v.""ExpirationDate"" IS NOT NULL
      AND (v.""ExpirationDate"")::date >= CURRENT_DATE
      AND v.""StartDate"" IS NOT NULL
      AND (v.""StartDate"")::date > DATE '1900-01-01'
) x;
", true);
    }
    private void CreateViewRdVisaByDaysRemaining()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_visa_by_days_remaining;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: valid visas by days remaining until expiry (By Days Remaining).
-- Buckets: < 10 days / < 1 month / < 3..6 months / ≥ 6 months. Valid visas only.
CREATE VIEW vw_rd_visa_by_days_remaining AS
SELECT
    v.""ID""                                                              AS ""ID"",
    p.""ID""                                                              AS ""PersonOid"",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p.""FirstName""), ''),
        NULLIF(BTRIM(p.""MiddleName""), ''),
        NULLIF(BTRIM(p.""LastName""), '')
    )                                                                   AS ""PersonName"",
    COALESCE(
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        ''
    )                                                                   AS ""ProjectName"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                             AS ""ProjectNameRaw"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                             AS ""ProjectNameTm"",
    p.""PersonRole""                                                      AS ""PersonRoleCode"",
    COALESCE(NULLIF(BTRIM(v.""VisaNumber""), ''), '')                     AS ""VisaNumber"",
    CASE WHEN (v.""ExpirationDate"")::date > DATE '1900-01-01' THEN v.""ExpirationDate"" ELSE NULL END AS ""ExpirationDate"",
    (v.""ExpirationDate"")::date - CURRENT_DATE                           AS ""DaysRemaining"",
    CASE
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 10  THEN '< 10 days'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 30  THEN '< 1 month'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 90  THEN '< 3 months'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 120 THEN '< 4 months'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 150 THEN '< 5 months'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 180 THEN '< 6 months'
        ELSE '≥ 6 months'
    END                                                                 AS ""RemainingLabel"",
    CASE
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 10  THEN '< 10 days'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 30  THEN '< 1 month'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 90  THEN '< 3 months'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 120 THEN '< 4 months'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 150 THEN '< 5 months'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 180 THEN '< 6 months'
        ELSE '≥ 6 months'
    END                                                                 AS ""StatusLabel"",
    CASE
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 30  THEN 'st-expiring'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 90  THEN 'st-pending'
        ELSE 'st-approved'
    END                                                                 AS ""StatusCssClass"",
    COALESCE(p.""IsArchived"", FALSE)                                     AS ""IsArchived""
FROM ""Visas"" v
INNER JOIN ""Passports"" pp
    ON pp.""ID"" = v.""PassportID""
   AND COALESCE(pp.""GCRecord"", 0) = 0
INNER JOIN ""People"" p
    ON p.""ID"" = pp.""PersonID""
   AND COALESCE(p.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = p.""ProjectContractID""
   AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sp
    ON sp.""ID"" = p.""SponsoringEmployeeID""
   AND COALESCE(sp.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sp.""ProjectContractID""
   AND COALESCE(spc.""GCRecord"", 0) = 0
WHERE COALESCE(v.""GCRecord"", 0) = 0
  AND COALESCE(v.""IsCancelled"", FALSE) = FALSE
  AND v.""ExpirationDate"" IS NOT NULL
  AND (v.""ExpirationDate"")::date >= CURRENT_DATE;
", true);
    }
    private void CreateViewRdApplication()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_application;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: Application category (PostgreSQL).
CREATE VIEW vw_rd_application AS
SELECT
    a.""ID""                                                                  AS ""ID"",
    first_p.""ID""                                                            AS ""PersonOid"",
    COALESCE(
        NULLIF(CONCAT_WS(' ',
            NULLIF(BTRIM(first_p.""FirstName""), ''),
            NULLIF(BTRIM(first_p.""MiddleName""), ''),
            NULLIF(BTRIM(first_p.""LastName""), '')
        ), ''),
        NULLIF(BTRIM(a.""FullApplicationNumber""), ''),
        NULLIF(BTRIM(a.""ApplicationNumber""), ''),
        ''
    )                                                                       AS ""PersonName"",
    COALESCE(
        NULLIF(BTRIM(pc.""NameTm""), ''),
        ''
    )                                                                       AS ""ProjectName"",
    COALESCE(pc.""NameTm"", '')                                               AS ""ProjectNameRaw"",
    COALESCE(pc.""NameTm"", '')                                               AS ""ProjectNameTm"",
    COALESCE(first_p.""PersonRole"", 0)                                       AS ""PersonRoleCode"",
    COALESCE(
        NULLIF(BTRIM(a.""FullApplicationNumber""), ''),
        NULLIF(BTRIM(a.""ApplicationNumber""), ''),
        ''
    )                                                                       AS ""ApplicationNumber"",
    a.""ApplicationDate""                                                     AS ""ApplicationDate"",
    COALESCE(
        NULLIF(BTRIM(ast.""NameTm""), ''),
        NULLIF(BTRIM(ast.""Name""), ''),
        'Being Prepared'
    )                                                                       AS ""ProgressStateLabel"",
    CASE
      WHEN ast.""Code"" IN ('PROCESS_ISSUED', '1_REVIEW_APPROVED', '2_REVIEW_APPROVED')
                                                                             THEN 'st-approved'
      WHEN ast.""Code"" IN ('PROCESS_REJECTED', 'PROCESS_CANCELLED', '1_REVIEW_REJECTED', '2_REVIEW_REJECTED')
                                                                             THEN 'st-expiring'
      ELSE                                                                   'st-pending'
    END                                                                     AS ""ProgressStateCssClass"",
    COALESCE(ast.""Code"", '')                                                AS ""ProgressStateCode"",`r`n    COALESCE(
        NULLIF(BTRIM(at.""NameTm""), ''),
        NULLIF(BTRIM(at.""Name""), ''),
        'Unknown'
    )                                                                       AS ""TypeLabel"",
    COALESCE(first_p.""IsArchived"", FALSE)                                   AS ""IsArchived""
FROM ""Applications"" a
LEFT JOIN ""ApplicationTypes"" at
    ON at.""ID"" = a.""ApplicationTypeID""
   AND COALESCE(at.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = a.""ProjectContractID""
   AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN LATERAL (
    SELECT ap.""StateID""
    FROM ""ApplicationProgresses"" ap
    WHERE ap.""ApplicationID"" = a.""ID""
      AND COALESCE(ap.""GCRecord"", 0) = 0
    ORDER BY ap.""Date"" DESC NULLS LAST, ap.""ID"" DESC
    LIMIT 1
) latest_ap ON TRUE
LEFT JOIN ""ApplicationStates"" ast
    ON ast.""ID"" = latest_ap.""StateID""
   AND COALESCE(ast.""GCRecord"", 0) = 0
LEFT JOIN LATERAL (
    SELECT ai.""PersonID""
    FROM ""ApplicationItems"" ai
    WHERE ai.""ApplicationID"" = a.""ID""
      AND COALESCE(ai.""GCRecord"", 0) = 0
    ORDER BY ai.""ID""
    LIMIT 1
) first_ai ON TRUE
LEFT JOIN ""People"" first_p
    ON first_p.""ID"" = first_ai.""PersonID""
   AND COALESCE(first_p.""GCRecord"", 0) = 0
WHERE COALESCE(a.""GCRecord"", 0) = 0;
", true);
    }
    private void CreateViewRdEducation()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_education;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: Education (PostgreSQL).
-- One row per Education; soft-delete COALESCE(""GCRecord"", 0) = 0.
CREATE VIEW vw_rd_education AS
SELECT
    e.""ID""                                                                  AS ""ID"",
    p.""ID""                                                                  AS ""PersonOid"",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p.""FirstName""), ''),
        NULLIF(BTRIM(p.""MiddleName""), ''),
        NULLIF(BTRIM(p.""LastName""), '')
    )                                                                       AS ""PersonName"",
    COALESCE(
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        ''
    )                                                                       AS ""ProjectName"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                                 AS ""ProjectNameRaw"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                                 AS ""ProjectNameTm"",
    p.""PersonRole""                                                          AS ""PersonRoleCode"",
    COALESCE(
        NULLIF(BTRIM(ei.""NameTm""), ''),
        NULLIF(BTRIM(ei.""Name""), ''),
        ''
    )                                                                       AS ""InstitutionName"",
    COALESCE(NULLIF(BTRIM(e.""GraduationYear""), ''), '')                     AS ""GraduationYear"",
    COALESCE(
        NULLIF(BTRIM(el.""NameTm""), ''),
        NULLIF(BTRIM(el.""Name""), ''),
        'Unknown'
    )                                                                       AS ""LevelLabel"",
    COALESCE(
        NULLIF(BTRIM(c.""NameTm""), ''),
        NULLIF(BTRIM(c.""Name""), ''),
        'Unknown'
    )                                                                       AS ""CountryLabel"",
    COALESCE(
        NULLIF(BTRIM(sp.""NameTm""), ''),
        NULLIF(BTRIM(sp.""Name""), ''),
        'Unknown'
    )                                                                       AS ""SpecialtyLabel"",
    COALESCE(p.""IsArchived"", FALSE)                                         AS ""IsArchived""
FROM ""Educations"" e
INNER JOIN ""People"" p
    ON p.""ID"" = e.""PersonID""
   AND COALESCE(p.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = p.""ProjectContractID"" AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sponsor
    ON sponsor.""ID"" = p.""SponsoringEmployeeID"" AND COALESCE(sponsor.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sponsor.""ProjectContractID"" AND COALESCE(spc.""GCRecord"", 0) = 0
LEFT JOIN ""EducationLevels"" el
    ON el.""ID"" = e.""EducationLevelID"" AND COALESCE(el.""GCRecord"", 0) = 0
LEFT JOIN ""EducationInstitutions"" ei
    ON ei.""ID"" = e.""EducationInstitutionID"" AND COALESCE(ei.""GCRecord"", 0) = 0
LEFT JOIN ""Countries"" c
    ON c.""ID"" = e.""EducationCountryID"" AND COALESCE(c.""GCRecord"", 0) = 0
LEFT JOIN ""Specialties"" sp
    ON sp.""ID"" = e.""SpecialtyID"" AND COALESCE(sp.""GCRecord"", 0) = 0
WHERE COALESCE(e.""GCRecord"", 0) = 0;
", true);
    }

    private void CreateViewRdEducationByCountry()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_education_by_country;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: Education by-country (education country only) for PostgreSQL.
-- Dedicated view for Education ""By Country"" sub-report.
CREATE VIEW vw_rd_education_by_country AS
SELECT
    e.""ID""                                                                  AS ""ID"",
    p.""ID""                                                                  AS ""PersonOid"",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p.""FirstName""), ''),
        NULLIF(BTRIM(p.""MiddleName""), ''),
        NULLIF(BTRIM(p.""LastName""), '')
    )                                                                       AS ""PersonName"",
    COALESCE(
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        ''
    )                                                                       AS ""ProjectName"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                                 AS ""ProjectNameRaw"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                                 AS ""ProjectNameTm"",
    p.""PersonRole""                                                          AS ""PersonRoleCode"",
    COALESCE(
        NULLIF(BTRIM(ei.""NameTm""), ''),
        NULLIF(BTRIM(ei.""Name""), ''),
        ''
    )                                                                       AS ""InstitutionName"",
    COALESCE(NULLIF(BTRIM(e.""GraduationYear""), ''), '')                     AS ""GraduationYear"",
    COALESCE(
        NULLIF(BTRIM(c.""NameTm""), ''),
        NULLIF(BTRIM(c.""Name""), ''),
        'Unknown'
    )                                                                       AS ""CountryLabel"",
    COALESCE(p.""IsArchived"", FALSE)                                         AS ""IsArchived""
FROM ""Educations"" e
INNER JOIN ""People"" p
    ON p.""ID"" = e.""PersonID""
   AND COALESCE(p.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = p.""ProjectContractID"" AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sponsor
    ON sponsor.""ID"" = p.""SponsoringEmployeeID"" AND COALESCE(sponsor.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sponsor.""ProjectContractID"" AND COALESCE(spc.""GCRecord"", 0) = 0
LEFT JOIN ""EducationInstitutions"" ei
    ON ei.""ID"" = e.""EducationInstitutionID"" AND COALESCE(ei.""GCRecord"", 0) = 0
LEFT JOIN ""Countries"" c
    ON c.""ID"" = e.""EducationCountryID"" AND COALESCE(c.""GCRecord"", 0) = 0
WHERE COALESCE(e.""GCRecord"", 0) = 0;
", true);
    }

    private void CreateViewRdPositionHistory()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_position_history;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: Position History (PostgreSQL).
CREATE VIEW vw_rd_position_history AS
SELECT
    eph.""ID""                                                                AS ""ID"",
    p.""ID""                                                                  AS ""PersonOid"",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p.""FirstName""), ''),
        NULLIF(BTRIM(p.""MiddleName""), ''),
        NULLIF(BTRIM(p.""LastName""), '')
    )                                                                       AS ""PersonName"",
    COALESCE(
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        ''
    )                                                                       AS ""ProjectName"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                                 AS ""ProjectNameRaw"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                                 AS ""ProjectNameTm"",
    p.""PersonRole""                                                          AS ""PersonRoleCode"",
    COALESCE(
        NULLIF(BTRIM(pos.""NameTm""), ''),
        NULLIF(BTRIM(pos.""Name""), ''),
        'Unknown'
    )                                                                       AS ""PositionName"",
    eph.""StartDate""                                                         AS ""StartDate"",
    CASE
      WHEN eph.""EndDate"" IS NULL
        OR (eph.""EndDate"")::date >= CURRENT_DATE
                                                                              THEN 'Current'
      ELSE                                                                    'Ended'
    END                                                                     AS ""StatusLabel"",
    CASE
      WHEN eph.""EndDate"" IS NULL
        OR (eph.""EndDate"")::date >= CURRENT_DATE
                                                                              THEN 'st-approved'
      ELSE                                                                    'st-pending'
    END                                                                     AS ""StatusCssClass"",
    COALESCE(
        NULLIF(BTRIM(pos.""NameTm""), ''),
        NULLIF(BTRIM(pos.""Name""), ''),
        'Unknown'
    )                                                                       AS ""PositionLabel"",
    COALESCE(
        NULLIF(BTRIM(ap.""Name""), ''),
        'Unknown'
    )                                                                       AS ""ActualPositionLabel"",
    COALESCE(p.""IsArchived"", FALSE)                                         AS ""IsArchived""
FROM ""EmployeePositionHistories"" eph
INNER JOIN ""People"" p
    ON p.""ID"" = eph.""PersonID""
   AND COALESCE(p.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = p.""ProjectContractID"" AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sponsor
    ON sponsor.""ID"" = p.""SponsoringEmployeeID"" AND COALESCE(sponsor.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sponsor.""ProjectContractID"" AND COALESCE(spc.""GCRecord"", 0) = 0
LEFT JOIN ""Positions"" pos
    ON pos.""ID"" = eph.""PositionID"" AND COALESCE(pos.""GCRecord"", 0) = 0
LEFT JOIN ""ActualPositions"" ap
    ON ap.""ID"" = eph.""ActualPositionID"" AND COALESCE(ap.""GCRecord"", 0) = 0
WHERE COALESCE(eph.""GCRecord"", 0) = 0;
", true);
    }

    private void CreateViewRdRegistration()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_registration;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: Registration category (PostgreSQL).
-- One row per not-expired visa: latest registration Application via ApplicationItem.CurrentVisa.
CREATE VIEW vw_rd_registration AS
WITH ranked AS (
    SELECT
        ai.""ID"" AS ""ID"",
        p.""ID"" AS ""PersonOid"",
        CONCAT_WS(' ',
            NULLIF(BTRIM(p.""FirstName""), ''),
            NULLIF(BTRIM(p.""MiddleName""), ''),
            NULLIF(BTRIM(p.""LastName""), '')
        ) AS ""PersonName"",
        COALESCE(
            NULLIF(BTRIM(pc.""NameTm""), ''),
            NULLIF(BTRIM(spc.""NameTm""), ''),
            ''
        ) AS ""ProjectName"",
        COALESCE(pc.""NameTm"", spc.""NameTm"", '') AS ""ProjectNameRaw"",
        COALESCE(pc.""NameTm"", spc.""NameTm"", '') AS ""ProjectNameTm"",
        p.""PersonRole"" AS ""PersonRoleCode"",
        COALESCE(NULLIF(BTRIM(v.""VisaNumber""), ''), '') AS ""VisaNumber"",
        v.""ExpirationDate"" AS ""VisaExpirationDate"",
        COALESCE(
            NULLIF(BTRIM(a.""FullApplicationNumber""), ''),
            NULLIF(BTRIM(a.""ApplicationNumber""), ''),
            ''
        ) AS ""ApplicationNumber"",
        a.""ApplicationDate"" AS ""ApplicationDate"",
        at.""Name"" AS ""ApplicationTypeName"",
        COALESCE(
            NULLIF(BTRIM(at.""NameTm""), ''),
            NULLIF(BTRIM(at.""Name""), ''),
            'Unknown'
        ) AS ""ApplicationTypeLabel"",
        COALESCE(
            NULLIF(BTRIM(ast.""NameTm""), ''),
            NULLIF(BTRIM(ast.""Name""), ''),
            'OFISDE'
        ) AS ""ProgressStateLabel"",
        CASE
            WHEN ast.""Code"" IN ('PROCESS_ISSUED') THEN 'st-approved'
            WHEN ast.""Code"" IN ('PROCESS_REJECTED', 'PROCESS_CANCELLED') THEN 'st-expiring'
            WHEN ast.""Code"" IS NULL THEN 'st-pending'
            ELSE 'st-pending'
        END AS ""ProgressStateCssClass"",
        COALESCE(ast.""Code"", 'AT_OFFICE') AS ""ProgressStateCode"",
        ((v.""ExpirationDate"")::date - CURRENT_DATE) AS ""DaysRemaining"",
        CASE
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 7   THEN '< 7 days'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 14  THEN '< 14 days'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 30  THEN '< 1 month'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 90  THEN '< 3 months'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 180 THEN '< 6 months'
            ELSE 'â‰¥ 6 months'
        END AS ""ExpiryBucketLabel"",
        CASE
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 14  THEN 'st-expiring'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 90  THEN 'st-pending'
            ELSE 'st-approved'
        END AS ""ExpiryBucketCssClass"",
        COALESCE(p.""IsArchived"", FALSE) AS ""IsArchived"",
        COALESCE(
            NULLIF(BTRIM(city.""NameTm""), ''),
            NULLIF(BTRIM(city.""Name""), ''),
            'Unknown city'
        ) AS ""CityLabel"",
        ROW_NUMBER() OVER (
            PARTITION BY v.""ID""
            ORDER BY a.""ApplicationDate"" DESC NULLS LAST, a.""ID"" DESC, ai.""ID"" DESC
        ) AS rn
    FROM ""Visas"" v
    INNER JOIN ""Passports"" pp
        ON pp.""ID"" = v.""PassportID"" AND COALESCE(pp.""GCRecord"", 0) = 0
    INNER JOIN ""People"" p
        ON p.""ID"" = pp.""PersonID"" AND COALESCE(p.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationItems"" ai
        ON ai.""CurrentVisaId"" = v.""ID"" AND COALESCE(ai.""GCRecord"", 0) = 0
    INNER JOIN ""Applications"" a
        ON a.""ID"" = ai.""ApplicationID"" AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationTypes"" at
        ON at.""ID"" = a.""ApplicationTypeID"" AND COALESCE(at.""GCRecord"", 0) = 0
    LEFT JOIN ""AddressesOfResidence"" addr
        ON addr.""ID"" = ai.""CurrentAddressOfResidenceID"" AND COALESCE(addr.""GCRecord"", 0) = 0
    LEFT JOIN ""Cities"" city
        ON city.""ID"" = addr.""CityID"" AND COALESCE(city.""GCRecord"", 0) = 0
    LEFT JOIN ""ProjectContracts"" pc
        ON pc.""ID"" = COALESCE(a.""ProjectContractID"", p.""ProjectContractID"")
       AND COALESCE(pc.""GCRecord"", 0) = 0
    LEFT JOIN ""People"" sp
        ON sp.""ID"" = p.""SponsoringEmployeeID"" AND COALESCE(sp.""GCRecord"", 0) = 0
    LEFT JOIN ""ProjectContracts"" spc
        ON spc.""ID"" = sp.""ProjectContractID"" AND COALESCE(spc.""GCRecord"", 0) = 0
    LEFT JOIN LATERAL (
        SELECT ap.""StateID""
        FROM ""ApplicationProgresses"" ap
        WHERE ap.""ApplicationID"" = a.""ID""
          AND COALESCE(ap.""GCRecord"", 0) = 0
        ORDER BY ap.""Date"" DESC NULLS LAST, ap.""ID"" DESC
        LIMIT 1
    ) latest_ap ON TRUE
    LEFT JOIN ""ApplicationStates"" ast
        ON ast.""ID"" = latest_ap.""StateID"" AND COALESCE(ast.""GCRecord"", 0) = 0
    WHERE COALESCE(v.""GCRecord"", 0) = 0
      AND COALESCE(v.""IsCancelled"", FALSE) = FALSE
      AND (v.""ExpirationDate"")::date >= CURRENT_DATE
      AND at.""Name"" IN (
            'App_Reg_Check_In',
            'App_Reg_Check_In_Internal',
            'App_Reg_Check_Out',
            'App_Reg_Check_Out_Internal',
            'App_Reg_ext',
            'App_Reg_Info_Change_Address',
            'App_Reg_Info_Change_Passport',
            'App_Reg_Info_Change_Visa'
        )
)
SELECT
    ""ID"",
    ""PersonOid"",
    ""PersonName"",
    ""ProjectName"",
    ""ProjectNameRaw"",
    ""ProjectNameTm"",
    ""PersonRoleCode"",
    ""VisaNumber"",
    ""VisaExpirationDate"",
    ""ApplicationNumber"",
    ""ApplicationDate"",
    ""ApplicationTypeName"",
    ""ApplicationTypeLabel"",
    ""ProgressStateLabel"",
    ""ProgressStateCssClass"",
    ""ProgressStateCode"",
    ""DaysRemaining"",
    ""ExpiryBucketLabel"",
    ""ExpiryBucketCssClass"",
    ""IsArchived"",
    ""CityLabel""
FROM ranked
WHERE rn = 1;
", true);
    }
    private void CreateViewRdToBeCheckedIn()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_to_be_checked_in;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: To Be Checked In (Registration).
-- Valid visas with no ApplicationItem.CurrentVisa link to any App_Reg_* type.
-- Person must be in-country: latest TravelHistory is ExternalArrival.
-- Chart: days since that arrival TravelDate.
CREATE VIEW vw_rd_to_be_checked_in AS
WITH reg_linked AS (
    SELECT DISTINCT ai.""CurrentVisaId"" AS ""VisaId""
    FROM ""ApplicationItems"" ai
    INNER JOIN ""Applications"" a
        ON a.""ID"" = ai.""ApplicationID"" AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationTypes"" at
        ON at.""ID"" = a.""ApplicationTypeID"" AND COALESCE(at.""GCRecord"", 0) = 0
    WHERE COALESCE(ai.""GCRecord"", 0) = 0
      AND ai.""CurrentVisaId"" IS NOT NULL
      AND at.""Name"" IN (
            'App_Reg_Check_In',
            'App_Reg_Check_In_Internal',
            'App_Reg_Check_Out',
            'App_Reg_Check_Out_Internal',
            'App_Reg_ext',
            'App_Reg_Info_Change_Address',
            'App_Reg_Info_Change_Passport',
            'App_Reg_Info_Change_Visa'
        )
),
latest_travel AS (
    SELECT DISTINCT ON (th.""PersonID"")
        th.""PersonID"",
        th.""Discriminator"",
        th.""TravelDate"" AS ""EntryDate""
    FROM ""TravelHistories"" th
    WHERE COALESCE(th.""GCRecord"", 0) = 0
    ORDER BY th.""PersonID"", th.""TravelDate"" DESC NULLS LAST, th.""ID"" DESC
)
SELECT
    v.""ID"" AS ""ID"",
    p.""ID"" AS ""PersonOid"",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p.""FirstName""), ''),
        NULLIF(BTRIM(p.""MiddleName""), ''),
        NULLIF(BTRIM(p.""LastName""), '')
    ) AS ""PersonName"",
    COALESCE(
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        ''
    ) AS ""ProjectName"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '') AS ""ProjectNameRaw"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '') AS ""ProjectNameTm"",
    p.""PersonRole"" AS ""PersonRoleCode"",
    COALESCE(NULLIF(BTRIM(v.""VisaNumber""), ''), '') AS ""VisaNumber"",
    v.""ExpirationDate"" AS ""VisaExpirationDate"",
    lt.""EntryDate"" AS ""EntryDate"",
    (CURRENT_DATE - (lt.""EntryDate"")::date) AS ""DaysSinceEntry"",
    CASE
        WHEN CURRENT_DATE - (lt.""EntryDate"")::date < 7  THEN '< 1 week'
        WHEN CURRENT_DATE - (lt.""EntryDate"")::date < 14 THEN '< 2 weeks'
        WHEN CURRENT_DATE - (lt.""EntryDate"")::date < 21 THEN '< 3 weeks'
        WHEN CURRENT_DATE - (lt.""EntryDate"")::date < 28 THEN '< 4 weeks'
        WHEN CURRENT_DATE - (lt.""EntryDate"")::date < 30 THEN '< 1 month'
        ELSE '≥ 1 month'
    END AS ""EntryBucketLabel"",
    CASE
        WHEN CURRENT_DATE - (lt.""EntryDate"")::date < 14 THEN 'st-expiring'
        WHEN CURRENT_DATE - (lt.""EntryDate"")::date < 30 THEN 'st-pending'
        ELSE 'st-approved'
    END AS ""EntryBucketCssClass"",
    COALESCE(p.""IsArchived"", FALSE) AS ""IsArchived""
FROM ""Visas"" v
INNER JOIN ""Passports"" pp
    ON pp.""ID"" = v.""PassportID"" AND COALESCE(pp.""GCRecord"", 0) = 0
INNER JOIN ""People"" p
    ON p.""ID"" = pp.""PersonID"" AND COALESCE(p.""GCRecord"", 0) = 0
INNER JOIN latest_travel lt
    ON lt.""PersonID"" = p.""ID""
   AND lt.""Discriminator"" = 'ExternalArrival'
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = p.""ProjectContractID"" AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sp
    ON sp.""ID"" = p.""SponsoringEmployeeID"" AND COALESCE(sp.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sp.""ProjectContractID"" AND COALESCE(spc.""GCRecord"", 0) = 0
WHERE COALESCE(v.""GCRecord"", 0) = 0
  AND COALESCE(v.""IsCancelled"", FALSE) = FALSE
  AND (v.""ExpirationDate"")::date >= CURRENT_DATE
  AND NOT EXISTS (
        SELECT 1 FROM reg_linked rl WHERE rl.""VisaId"" = v.""ID""
  );
", true);
    }

    private void CreateViewRdToBeCheckedOut()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_to_be_checked_out;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: To Be Checked Out (Registration).
-- Valid visas expiring within 1 week (DaysRemaining < 7), no Check-Out / Check-Out Internal on CurrentVisa.
-- Chart: < 1 day · < 2 days · … · < 7 days.
CREATE VIEW vw_rd_to_be_checked_out AS
WITH checkout_linked AS (
    SELECT DISTINCT ai.""CurrentVisaId"" AS ""VisaId""
    FROM ""ApplicationItems"" ai
    INNER JOIN ""Applications"" a
        ON a.""ID"" = ai.""ApplicationID"" AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationTypes"" at
        ON at.""ID"" = a.""ApplicationTypeID"" AND COALESCE(at.""GCRecord"", 0) = 0
    WHERE COALESCE(ai.""GCRecord"", 0) = 0
      AND ai.""CurrentVisaId"" IS NOT NULL
      AND at.""Name"" IN ('App_Reg_Check_Out', 'App_Reg_Check_Out_Internal')
)
SELECT
    v.""ID"" AS ""ID"",
    p.""ID"" AS ""PersonOid"",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p.""FirstName""), ''),
        NULLIF(BTRIM(p.""MiddleName""), ''),
        NULLIF(BTRIM(p.""LastName""), '')
    ) AS ""PersonName"",
    COALESCE(
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        ''
    ) AS ""ProjectName"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '') AS ""ProjectNameRaw"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '') AS ""ProjectNameTm"",
    p.""PersonRole"" AS ""PersonRoleCode"",
    COALESCE(NULLIF(BTRIM(v.""VisaNumber""), ''), '') AS ""VisaNumber"",
    v.""ExpirationDate"" AS ""VisaExpirationDate"",
    ((v.""ExpirationDate"")::date - CURRENT_DATE) AS ""DaysRemaining"",
    CASE
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 1 THEN '< 1 day'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 2 THEN '< 2 days'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 3 THEN '< 3 days'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 4 THEN '< 4 days'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 5 THEN '< 5 days'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 6 THEN '< 6 days'
        ELSE '< 7 days'
    END AS ""ExpiryBucketLabel"",
    CASE
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 3 THEN 'st-expiring'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 5 THEN 'st-pending'
        ELSE 'st-approved'
    END AS ""ExpiryBucketCssClass"",
    COALESCE(p.""IsArchived"", FALSE) AS ""IsArchived""
FROM ""Visas"" v
INNER JOIN ""Passports"" pp
    ON pp.""ID"" = v.""PassportID"" AND COALESCE(pp.""GCRecord"", 0) = 0
INNER JOIN ""People"" p
    ON p.""ID"" = pp.""PersonID"" AND COALESCE(p.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = p.""ProjectContractID"" AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sp
    ON sp.""ID"" = p.""SponsoringEmployeeID"" AND COALESCE(sp.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sp.""ProjectContractID"" AND COALESCE(spc.""GCRecord"", 0) = 0
WHERE COALESCE(v.""GCRecord"", 0) = 0
  AND COALESCE(v.""IsCancelled"", FALSE) = FALSE
  AND (v.""ExpirationDate"")::date >= CURRENT_DATE
  AND (v.""ExpirationDate"")::date - CURRENT_DATE < 7
  AND NOT EXISTS (
        SELECT 1 FROM checkout_linked cl WHERE cl.""VisaId"" = v.""ID""
  );
", true);
    }
}