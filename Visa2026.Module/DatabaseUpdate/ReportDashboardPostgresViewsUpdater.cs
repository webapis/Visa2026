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
        CreateViewRdWorkPermitActive();
        CreateViewRdWorkPermitAppProgress();
        CreateViewRdInvitationReady();
        CreateViewRdInvitationInProcess();
        CreateViewRdApplicationViaMinistryInvitationOnProcess();
        CreateViewRdApplicationViaMinistryInvitationOnProcessByPeriodCategoryType();
        CreateViewRdApplicationViaMinistryInvitationCompletedBase();
        CreateViewRdApplicationViaMinistryInvitationCompleted();
        CreateViewRdApplicationViaMinistryInvitationCompletedByPeriodCategoryType();
        CreateViewRdApplicationViaMinistryVisaExtensionOnProcessBase();
        CreateViewRdApplicationViaMinistryVisaExtensionOnProcess();
        CreateViewRdApplicationViaMinistryVisaExtensionOnProcessByPeriodCategoryType();
        CreateViewRdApplicationViaMinistryVisaExtensionCompletedBase();
        CreateViewRdApplicationViaMinistryVisaExtensionCompleted();
        CreateViewRdApplicationViaMinistryVisaExtensionCompletedByPeriodCategoryType();
        CreateViewRdApplicationViaMinistryOtherOnProcess();
        CreateViewRdApplicationViaMinistryOtherCompleted();
        CreateViewRdApplicationDirectMigrationOnProcessA();
        CreateViewRdApplicationDirectMigrationProcessComplete();
        CreateViewRdIncompletePersonsByMissingArea();
        CreateViewRdPersonSearch();
        CreateViewRdInvitationRejected();
        CreateViewRdInvitationUsed();
        CreateViewRdInvitationValidUntil();
        CreateViewRdVisaAppProgress();
        CreateViewRdVisaOnExtension();
        CreateViewRdVisaOnExtensionByPeriodCategoryType();
        CreateViewRdVisaExtensionResult();
        CreateViewRdVisaExtensionResultByPeriodCategoryType();
        CreateViewRdProjects();
        CreateViewRdPersonRoles();
        CreateViewRdVisaState();
        CreateViewRdVisaByCategory();
        CreateViewRdVisaByType();
        CreateViewRdVisaByPeriod();
        CreateViewRdVisaActiveByProject();
        CreateViewRdVisaActiveByPeriodCategoryType();
        CreateViewRdVisaByDaysRemaining();
        CreateViewRdVisaExtensionRequired();
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
        ExecuteNonQueryCommand(ReportDashboardPostgresRosterSql.ViewVisaExtensionStatusSql, true);
    }

    private void CreateViewRdPassport()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_passport;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: Passport (PostgreSQL).
-- One row per roster line with a resolved passport (M2M) or legacy ApplicationItem.CurrentPassport.
-- Date filter (dashboard top-right) applies to Applications.ApplicationDate in the C# loader.
-- Soft-delete: COALESCE(""GCRecord"", 0) = 0. IsArchived is exposed for app-side toggle.
CREATE VIEW vw_rd_passport AS
SELECT
    line.""ID"",
    line.""PassportOid"",
    line.""PersonOid"",
    line.""PersonName"",
    line.""ProjectName"",
    line.""ProjectNameRaw"",
    line.""ProjectNameTm"",
    line.""PersonRoleCode"",
    line.""PassportNumber"",
    line.""IssueDate"",
    line.""ExpirationDate"",
    line.""ApplicationDate"",
    line.""TypeLabel"",
    line.""CitizenshipLabel"",
    line.""ValidityLabel"",
    line.""ValidityCssClass"",
    line.""IsArchived""
FROM (
    SELECT
        ap.""ID""                                                                 AS ""ID"",
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
        COALESCE(pc.""NameTm"", spc.""NameTm"", '')                             AS ""ProjectNameRaw"",
        COALESCE(pc.""NameTm"", spc.""NameTm"", '')                             AS ""ProjectNameTm"",
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
    FROM ""ApplicationPeople"" ap
    INNER JOIN ""Applications"" a
        ON a.""ID"" = ap.""ApplicationId"" AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationPersonResolvedLinks"" rl_pass
        ON rl_pass.""ApplicationPersonId"" = ap.""ID""
       AND rl_pass.""LinkKind"" = " + ReportDashboardPostgresRosterSql.LinkKindPassport + @"
       AND COALESCE(rl_pass.""GCRecord"", 0) = 0
    INNER JOIN ""Passports"" pp
        ON pp.""ID"" = rl_pass.""LinkedObjectId"" AND COALESCE(pp.""GCRecord"", 0) = 0
    INNER JOIN ""People"" p
        ON p.""ID"" = ap.""PersonId"" AND COALESCE(p.""GCRecord"", 0) = 0
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
    WHERE COALESCE(ap.""GCRecord"", 0) = 0
      AND rl_pass.""LinkedObjectId"" IS NOT NULL

    UNION ALL

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
        COALESCE(pc.""NameTm"", spc.""NameTm"", '')                             AS ""ProjectNameRaw"",
        COALESCE(pc.""NameTm"", spc.""NameTm"", '')                             AS ""ProjectNameTm"",
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
        ON a.""ID"" = ai.""ApplicationID"" AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""Passports"" pp
        ON pp.""ID"" = ai.""CurrentPassportID"" AND COALESCE(pp.""GCRecord"", 0) = 0
    INNER JOIN ""People"" p
        ON p.""ID"" = ai.""PersonID"" AND COALESCE(p.""GCRecord"", 0) = 0
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
      AND ai.""CurrentPassportID"" IS NOT NULL
      " + ReportDashboardPostgresRosterSql.LegacyApplicationItemOnly + @"
) line;
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

    private void CreateViewRdWorkPermitActive()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_work_permit_active;", true);
        ExecuteNonQueryCommand(@"
CREATE VIEW vw_rd_work_permit_active AS
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
        '(No project)'
    )                                                                       AS ""ProjectName"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                                 AS ""ProjectNameRaw"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                                 AS ""ProjectNameTm"",
    p.""PersonRole""                                                          AS ""PersonRoleCode"",
    COALESCE(NULLIF(BTRIM(wpi.""WorkPermitNumber""), ''), NULLIF(BTRIM(wpi.""ASNumber""), ''), '') AS ""WorkPermitNumber"",
    CASE WHEN (wpi.""ExpirationDate"")::date > DATE '1900-01-01' THEN wpi.""ExpirationDate"" ELSE NULL END AS ""ExpirationDate"",
    COALESCE(
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        '(No project)'
    )                                                                       AS ""StatusLabel"",
    'st-cat-1'                                                              AS ""StatusCssClass"",
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

        private void CreateViewRdWorkPermitAppProgress()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_work_permit_app_progress;", true);
        ExecuteNonQueryCommand(ReportDashboardPostgresRosterSql.WorkPermitAppProgressViewSql, true);
    }
private void CreateViewRdInvitationReady()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_invitation_ready;", true);
        ExecuteNonQueryCommand(@"
CREATE VIEW vw_rd_invitation_ready AS
SELECT
    ii.""ID""                                                                 AS ""ID"",
    p.""ID""                                                                  AS ""PersonOid"",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p.""FirstName""), ''),
        NULLIF(BTRIM(p.""MiddleName""), ''),
        NULLIF(BTRIM(p.""LastName""), '')
    )                                                                       AS ""PersonName"",
    COALESCE(
        NULLIF(BTRIM(apc.""NameTm""), ''),
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        '(No project)'
    )                                                                       AS ""ProjectName"",
    COALESCE(apc.""NameTm"", pc.""NameTm"", spc.""NameTm"", '')                   AS ""ProjectNameRaw"",
    COALESCE(apc.""NameTm"", pc.""NameTm"", spc.""NameTm"", '')                   AS ""ProjectNameTm"",
    p.""PersonRole""                                                          AS ""PersonRoleCode"",
    COALESCE(NULLIF(BTRIM(inv.""InvitationNumber""), ''), '')                 AS ""InvitationNumber"",
    CASE WHEN (inv.""ExpirationDate"")::date > DATE '1900-01-01' THEN inv.""ExpirationDate"" ELSE NULL END AS ""ExpirationDate"",
    CASE WHEN (inv.""StartDate"")::date > DATE '1900-01-01' THEN inv.""StartDate"" ELSE NULL END AS ""IssuedDate"",
    COALESCE(
        NULLIF(BTRIM(vp.""NameTm""), ''),
        NULLIF(BTRIM(vp.""Name""), ''),
        '(No period)'
    )                                                                       AS ""VisaPeriodLabel"",
    COALESCE(
        NULLIF(BTRIM(vc.""NameTm""), ''),
        NULLIF(BTRIM(vc.""Name""), ''),
        '(No category)'
    )                                                                       AS ""VisaCategoryLabel"",
    COALESCE(
        NULLIF(BTRIM(vt.""NameTm""), ''),
        NULLIF(BTRIM(vt.""Name""), ''),
        '(No type)'
    )                                                                       AS ""VisaTypeLabel"",
    COALESCE(
        NULLIF(BTRIM(apc.""NameTm""), ''),
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        '(No project)'
    )                                                                       AS ""StatusLabel"",
    'st-cat-1'                                                              AS ""StatusCssClass"",
    COALESCE(p.""IsArchived"", FALSE)                                         AS ""IsArchived""
FROM ""InvitationItems"" ii
INNER JOIN ""Invitations"" inv
    ON inv.""ID"" = ii.""InvitationID"" AND COALESCE(inv.""GCRecord"", 0) = 0
INNER JOIN ""People"" p
    ON p.""ID"" = ii.""PersonID"" AND COALESCE(p.""GCRecord"", 0) = 0
LEFT JOIN ""VisaPeriods"" vp
    ON vp.""ID"" = inv.""VisaPeriodID"" AND COALESCE(vp.""GCRecord"", 0) = 0
LEFT JOIN ""VisaCategories"" vc
    ON vc.""ID"" = inv.""VisaCategoryID"" AND COALESCE(vc.""GCRecord"", 0) = 0
LEFT JOIN ""Applications"" a
    ON a.""ID"" = inv.""ApplicationID"" AND COALESCE(a.""GCRecord"", 0) = 0
LEFT JOIN ""VisaTypes"" vt
    ON vt.""ID"" = a.""VisaTypeID"" AND COALESCE(vt.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" apc
    ON apc.""ID"" = a.""ProjectContractID"" AND COALESCE(apc.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = p.""ProjectContractID"" AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sp
    ON sp.""ID"" = p.""SponsoringEmployeeID"" AND COALESCE(sp.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sp.""ProjectContractID"" AND COALESCE(spc.""GCRecord"", 0) = 0
WHERE COALESCE(ii.""GCRecord"", 0) = 0
  AND COALESCE(ii.""IsUsed"", FALSE) = FALSE
  AND COALESCE(ii.""IsCancelled"", FALSE) = FALSE
  AND COALESCE(ii.""IsChanged"", FALSE) = FALSE
  AND ii.""PersonID"" IS NOT NULL
  AND inv.""ExpirationDate"" IS NOT NULL
  AND (inv.""ExpirationDate"")::date >= CURRENT_DATE;

", true);
    }
    private void CreateViewRdInvitationInProcess()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_invitation_in_process;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: Invitations In Process (in-process) — PostgreSQL.
CREATE VIEW vw_rd_invitation_in_process AS
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
        '(No project)'
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
    )                                                                       AS ""StatusLabel"",
    CASE
      WHEN ast.""Code"" IN ('PROCESS_ISSUED', '1_REVIEW_APPROVED', '2_REVIEW_APPROVED')
                                                                              THEN 'st-approved'
      WHEN ast.""Code"" IN ('PROCESS_REJECTED', 'PROCESS_CANCELLED', '1_REVIEW_REJECTED', '2_REVIEW_REJECTED')
                                                                              THEN 'st-expiring'
      ELSE                                                                          'st-pending'
    END                                                                     AS ""StatusCssClass"",
    COALESCE(ast.""Code"", '')                                                AS ""ProgressStateCode"",
    COALESCE(first_p.""IsArchived"", FALSE)                                   AS ""IsArchived""
FROM ""Applications"" a
INNER JOIN ""ApplicationTypes"" at
    ON at.""ID"" = a.""ApplicationTypeID""
   AND COALESCE(at.""GCRecord"", 0) = 0
   AND COALESCE(at.""CanIssueInvitation"", FALSE) = TRUE
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
" + ReportDashboardPostgresRosterSql.FirstApplicationPersonLateralJoin + @"
LEFT JOIN ""People"" first_p
    ON first_p.""ID"" = COALESCE(first_m2m.""PersonId"", first_legacy.""PersonID"")
   AND COALESCE(first_p.""GCRecord"", 0) = 0
WHERE COALESCE(a.""GCRecord"", 0) = 0
  AND NOT EXISTS (
        SELECT 1
        FROM ""Invitations"" inv
        WHERE inv.""ApplicationID"" = a.""ID""
          AND COALESCE(inv.""GCRecord"", 0) = 0
    )
  AND (
        ast.""Code"" IS NULL
        OR ast.""Code"" NOT IN (
            'PROCESS_ISSUED',
            'PROCESS_REJECTED',
            'PROCESS_CANCELLED',
            '1_REVIEW_REJECTED',
            '2_REVIEW_REJECTED',
            '3_REVIEW_REJECTED',
            '4_REVIEW_REJECTED',
            '5_REVIEW_REJECTED')
      );
", true);
    }
    private void ExecuteEmbeddedPostgresView(string resourceLeaf) =>
        ExecuteNonQueryCommand(ReportDashboardSqlViewResource.Load(resourceLeaf), true);

    private void CreateViewRdApplicationViaMinistryInvitationOnProcess() =>
        ExecuteEmbeddedPostgresView(
            "vw_rd_application_via_ministry_invitation_on_process.postgres.sql");

    private void CreateViewRdApplicationViaMinistryInvitationOnProcessByPeriodCategoryType() =>
        ExecuteEmbeddedPostgresView(
            "vw_rd_application_via_ministry_invitation_on_process_by_period_category_type.postgres.sql");

    private void CreateViewRdApplicationViaMinistryInvitationCompletedBase() =>
        ExecuteEmbeddedPostgresView(
            "vw_rd_application_via_ministry_invitation_completed_base.postgres.sql");

    private void CreateViewRdApplicationViaMinistryInvitationCompleted() =>
        ExecuteEmbeddedPostgresView(
            "vw_rd_application_via_ministry_invitation_completed.postgres.sql");

    private void CreateViewRdApplicationViaMinistryInvitationCompletedByPeriodCategoryType() =>
        ExecuteEmbeddedPostgresView(
            "vw_rd_application_via_ministry_invitation_completed_by_period_category_type.postgres.sql");

    private void CreateViewRdApplicationViaMinistryVisaExtensionOnProcessBase() =>
        ExecuteEmbeddedPostgresView(
            "vw_rd_application_via_ministry_visa_extension_on_process_base.postgres.sql");

    private void CreateViewRdApplicationViaMinistryVisaExtensionOnProcess() =>
        ExecuteEmbeddedPostgresView(
            "vw_rd_application_via_ministry_visa_extension_on_process.postgres.sql");

    private void CreateViewRdApplicationViaMinistryVisaExtensionOnProcessByPeriodCategoryType() =>
        ExecuteEmbeddedPostgresView(
            "vw_rd_application_via_ministry_visa_extension_on_process_by_period_category_type.postgres.sql");

    private void CreateViewRdApplicationViaMinistryVisaExtensionCompletedBase() =>
        ExecuteEmbeddedPostgresView(
            "vw_rd_application_via_ministry_visa_extension_completed_base.postgres.sql");

    private void CreateViewRdApplicationViaMinistryVisaExtensionCompleted() =>
        ExecuteEmbeddedPostgresView(
            "vw_rd_application_via_ministry_visa_extension_completed.postgres.sql");

    private void CreateViewRdApplicationViaMinistryVisaExtensionCompletedByPeriodCategoryType() =>
        ExecuteEmbeddedPostgresView(
            "vw_rd_application_via_ministry_visa_extension_completed_by_period_category_type.postgres.sql");

    private void CreateViewRdApplicationViaMinistryOtherOnProcess() =>
        ExecuteEmbeddedPostgresView(
            "vw_rd_application_via_ministry_other_on_process.postgres.sql");

    private void CreateViewRdApplicationViaMinistryOtherCompleted() =>
        ExecuteEmbeddedPostgresView(
            "vw_rd_application_via_ministry_other_completed.postgres.sql");

    private void CreateViewRdApplicationDirectMigrationOnProcessA() =>
        ExecuteEmbeddedPostgresView(
            "vw_rd_application_direct_migration_on_process_a.postgres.sql");

    private void CreateViewRdApplicationDirectMigrationProcessComplete() =>
        ExecuteEmbeddedPostgresView(
            "vw_rd_application_direct_migration_process_complete.postgres.sql");

    private void CreateViewRdIncompletePersonsByMissingArea() =>
        ExecuteEmbeddedPostgresView(
            "vw_rd_incomplete_persons_by_missing_area.postgres.sql");

    private void CreateViewRdPersonSearch() =>
        ExecuteEmbeddedPostgresView("vw_rd_person_search.postgres.sql");

    private void CreateViewRdInvitationRejected()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_invitation_rejected;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: Invitations Rejected (rejected-by-project) — PostgreSQL.
CREATE VIEW vw_rd_invitation_rejected AS
SELECT
    ri.""ID""                                                                 AS ""ID"",
    'rejection-item'                                                        AS ""SourceKind"",
    p.""ID""                                                                  AS ""PersonOid"",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p.""FirstName""), ''),
        NULLIF(BTRIM(p.""MiddleName""), ''),
        NULLIF(BTRIM(p.""LastName""), '')
    )                                                                       AS ""PersonName"",
    COALESCE(
        NULLIF(BTRIM(apc.""NameTm""), ''),
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        '(No project)'
    )                                                                       AS ""ProjectName"",
    COALESCE(apc.""NameTm"", pc.""NameTm"", spc.""NameTm"", '')                   AS ""ProjectNameRaw"",
    COALESCE(apc.""NameTm"", pc.""NameTm"", spc.""NameTm"", '')                   AS ""ProjectNameTm"",
    p.""PersonRole""                                                          AS ""PersonRoleCode"",
    COALESCE(NULLIF(BTRIM(r.""RejectedDocNumber""), ''), '')                  AS ""DocumentNumber"",
    CASE WHEN (r.""Date"")::date > DATE '1900-01-01' THEN r.""Date"" ELSE NULL END AS ""RecordDate"",
    COALESCE(
        NULLIF(BTRIM(apc.""NameTm""), ''),
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        '(No project)'
    )                                                                       AS ""StatusLabel"",
    'st-cat-1'                                                              AS ""StatusCssClass"",
    COALESCE(p.""IsArchived"", FALSE)                                         AS ""IsArchived""
FROM ""RejectionItems"" ri
INNER JOIN ""Rejections"" r
    ON r.""ID"" = ri.""RejectionID"" AND COALESCE(r.""GCRecord"", 0) = 0
INNER JOIN ""Applications"" a
    ON a.""ID"" = r.""ApplicationID"" AND COALESCE(a.""GCRecord"", 0) = 0
INNER JOIN ""ApplicationTypes"" at
    ON at.""ID"" = a.""ApplicationTypeID""
   AND COALESCE(at.""GCRecord"", 0) = 0
   AND COALESCE(at.""CanIssueInvitation"", FALSE) = TRUE
INNER JOIN ""People"" p
    ON p.""ID"" = ri.""PersonID"" AND COALESCE(p.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" apc
    ON apc.""ID"" = a.""ProjectContractID"" AND COALESCE(apc.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = p.""ProjectContractID"" AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sp
    ON sp.""ID"" = p.""SponsoringEmployeeID"" AND COALESCE(sp.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sp.""ProjectContractID"" AND COALESCE(spc.""GCRecord"", 0) = 0
WHERE COALESCE(ri.""GCRecord"", 0) = 0
  AND ri.""PersonID"" IS NOT NULL

UNION ALL

SELECT
    a.""ID""                                                                  AS ""ID"",
    'application'                                                           AS ""SourceKind"",
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
        NULLIF(BTRIM(apc.""NameTm""), ''),
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        '(No project)'
    )                                                                       AS ""ProjectName"",
    COALESCE(apc.""NameTm"", pc.""NameTm"", spc.""NameTm"", '')                   AS ""ProjectNameRaw"",
    COALESCE(apc.""NameTm"", pc.""NameTm"", spc.""NameTm"", '')                   AS ""ProjectNameTm"",
    COALESCE(first_p.""PersonRole"", 0)                                       AS ""PersonRoleCode"",
    COALESCE(
        NULLIF(BTRIM(a.""FullApplicationNumber""), ''),
        NULLIF(BTRIM(a.""ApplicationNumber""), ''),
        ''
    )                                                                       AS ""DocumentNumber"",
    a.""ApplicationDate""                                                     AS ""RecordDate"",
    COALESCE(
        NULLIF(BTRIM(apc.""NameTm""), ''),
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        '(No project)'
    )                                                                       AS ""StatusLabel"",
    'st-cat-1'                                                              AS ""StatusCssClass"",
    COALESCE(first_p.""IsArchived"", FALSE)                                   AS ""IsArchived""
FROM ""Applications"" a
INNER JOIN ""ApplicationTypes"" at
    ON at.""ID"" = a.""ApplicationTypeID""
   AND COALESCE(at.""GCRecord"", 0) = 0
   AND COALESCE(at.""CanIssueInvitation"", FALSE) = TRUE
LEFT JOIN ""ProjectContracts"" apc
    ON apc.""ID"" = a.""ProjectContractID"" AND COALESCE(apc.""GCRecord"", 0) = 0
LEFT JOIN LATERAL (
    SELECT ap.""StateID""
    FROM ""ApplicationProgresses"" ap
    WHERE ap.""ApplicationID"" = a.""ID""
      AND COALESCE(ap.""GCRecord"", 0) = 0
    ORDER BY ap.""Date"" DESC NULLS LAST, ap.""ID"" DESC
    LIMIT 1
) latest_ap ON TRUE
INNER JOIN ""ApplicationStates"" ast
    ON ast.""ID"" = latest_ap.""StateID""
   AND COALESCE(ast.""GCRecord"", 0) = 0
   AND ast.""Code"" = 'PROCESS_REJECTED'
" + ReportDashboardPostgresRosterSql.FirstApplicationPersonLateralJoin + @"
LEFT JOIN ""People"" first_p
    ON first_p.""ID"" = COALESCE(first_m2m.""PersonId"", first_legacy.""PersonID"") AND COALESCE(first_p.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = first_p.""ProjectContractID"" AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sp
    ON sp.""ID"" = first_p.""SponsoringEmployeeID"" AND COALESCE(sp.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sp.""ProjectContractID"" AND COALESCE(spc.""GCRecord"", 0) = 0
WHERE COALESCE(a.""GCRecord"", 0) = 0
  AND NOT EXISTS (
        SELECT 1
        FROM ""Rejections"" r
        WHERE r.""ApplicationID"" = a.""ID""
          AND COALESCE(r.""GCRecord"", 0) = 0
    );
", true);
    }
    private void CreateViewRdInvitationUsed()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_invitation_used;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: Used Invitations (used) — PostgreSQL.
CREATE VIEW vw_rd_invitation_used AS
SELECT
    ii.""ID""                                                                 AS ""ID"",
    p.""ID""                                                                  AS ""PersonOid"",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p.""FirstName""), ''),
        NULLIF(BTRIM(p.""MiddleName""), ''),
        NULLIF(BTRIM(p.""LastName""), '')
    )                                                                       AS ""PersonName"",
    COALESCE(
        NULLIF(BTRIM(apc.""NameTm""), ''),
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        '(No project)'
    )                                                                       AS ""ProjectName"",
    COALESCE(apc.""NameTm"", pc.""NameTm"", spc.""NameTm"", '')                   AS ""ProjectNameRaw"",
    COALESCE(apc.""NameTm"", pc.""NameTm"", spc.""NameTm"", '')                   AS ""ProjectNameTm"",
    p.""PersonRole""                                                          AS ""PersonRoleCode"",
    COALESCE(NULLIF(BTRIM(inv.""InvitationNumber""), ''), '')                 AS ""InvitationNumber"",
    CASE WHEN (inv.""ExpirationDate"")::date > DATE '1900-01-01' THEN inv.""ExpirationDate"" ELSE NULL END AS ""ExpirationDate"",
    CASE WHEN (inv.""StartDate"")::date > DATE '1900-01-01' THEN inv.""StartDate"" ELSE NULL END AS ""IssuedDate"",
    COALESCE(
        NULLIF(BTRIM(apc.""NameTm""), ''),
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        '(No project)'
    )                                                                       AS ""StatusLabel"",
    'st-cat-1'                                                              AS ""StatusCssClass"",
    COALESCE(p.""IsArchived"", FALSE)                                         AS ""IsArchived""
FROM ""InvitationItems"" ii
INNER JOIN ""Invitations"" inv
    ON inv.""ID"" = ii.""InvitationID"" AND COALESCE(inv.""GCRecord"", 0) = 0
INNER JOIN ""People"" p
    ON p.""ID"" = ii.""PersonID"" AND COALESCE(p.""GCRecord"", 0) = 0
LEFT JOIN ""Applications"" a
    ON a.""ID"" = inv.""ApplicationID"" AND COALESCE(a.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" apc
    ON apc.""ID"" = a.""ProjectContractID"" AND COALESCE(apc.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = p.""ProjectContractID"" AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sp
    ON sp.""ID"" = p.""SponsoringEmployeeID"" AND COALESCE(sp.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sp.""ProjectContractID"" AND COALESCE(spc.""GCRecord"", 0) = 0
WHERE COALESCE(ii.""GCRecord"", 0) = 0
  AND COALESCE(ii.""IsUsed"", FALSE) = TRUE
  AND ii.""PersonID"" IS NOT NULL;
", true);
    }
    private void CreateViewRdInvitationValidUntil()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_invitation_valid_until;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: Invitation Valid Until (valid-until) — PostgreSQL.
CREATE VIEW vw_rd_invitation_valid_until AS
SELECT
    ii.""ID""                                                                 AS ""ID"",
    p.""ID""                                                                  AS ""PersonOid"",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p.""FirstName""), ''),
        NULLIF(BTRIM(p.""MiddleName""), ''),
        NULLIF(BTRIM(p.""LastName""), '')
    )                                                                       AS ""PersonName"",
    COALESCE(
        NULLIF(BTRIM(apc.""NameTm""), ''),
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        '(No project)'
    )                                                                       AS ""ProjectName"",
    COALESCE(apc.""NameTm"", pc.""NameTm"", spc.""NameTm"", '')                   AS ""ProjectNameRaw"",
    COALESCE(apc.""NameTm"", pc.""NameTm"", spc.""NameTm"", '')                   AS ""ProjectNameTm"",
    p.""PersonRole""                                                          AS ""PersonRoleCode"",
    COALESCE(NULLIF(BTRIM(inv.""InvitationNumber""), ''), '')                 AS ""InvitationNumber"",
    CASE WHEN (inv.""ExpirationDate"")::date > DATE '1900-01-01' THEN inv.""ExpirationDate"" ELSE NULL END AS ""ExpirationDate"",
    CASE WHEN (inv.""StartDate"")::date > DATE '1900-01-01' THEN inv.""StartDate"" ELSE NULL END AS ""IssuedDate"",
    (inv.""ExpirationDate"")::date - CURRENT_DATE                             AS ""DaysRemaining"",
    CASE
        WHEN (inv.""ExpirationDate"")::date - CURRENT_DATE < 1   THEN '< 1 day'
        WHEN (inv.""ExpirationDate"")::date - CURRENT_DATE < 7   THEN '< 1 week'
        WHEN (inv.""ExpirationDate"")::date - CURRENT_DATE < 14  THEN '< 2 weeks'
        WHEN (inv.""ExpirationDate"")::date - CURRENT_DATE < 21  THEN '< 3 weeks'
        WHEN (inv.""ExpirationDate"")::date - CURRENT_DATE < 30  THEN '< 1 month'
        WHEN (inv.""ExpirationDate"")::date - CURRENT_DATE < 60  THEN '< 2 months'
        WHEN (inv.""ExpirationDate"")::date - CURRENT_DATE < 90  THEN '< 3 months'
        ELSE '≥ 3 months'
    END                                                                     AS ""ValidityLabel"",
    CASE
        WHEN (inv.""ExpirationDate"")::date - CURRENT_DATE < 7   THEN 'st-expiring'
        WHEN (inv.""ExpirationDate"")::date - CURRENT_DATE < 30  THEN 'st-pending'
        ELSE 'st-approved'
    END                                                                     AS ""ValidityCssClass"",
    COALESCE(p.""IsArchived"", FALSE)                                         AS ""IsArchived""
FROM ""InvitationItems"" ii
INNER JOIN ""Invitations"" inv
    ON inv.""ID"" = ii.""InvitationID"" AND COALESCE(inv.""GCRecord"", 0) = 0
INNER JOIN ""People"" p
    ON p.""ID"" = ii.""PersonID"" AND COALESCE(p.""GCRecord"", 0) = 0
LEFT JOIN ""Applications"" a
    ON a.""ID"" = inv.""ApplicationID"" AND COALESCE(a.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" apc
    ON apc.""ID"" = a.""ProjectContractID"" AND COALESCE(apc.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = p.""ProjectContractID"" AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sp
    ON sp.""ID"" = p.""SponsoringEmployeeID"" AND COALESCE(sp.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sp.""ProjectContractID"" AND COALESCE(spc.""GCRecord"", 0) = 0
WHERE COALESCE(ii.""GCRecord"", 0) = 0
  AND COALESCE(ii.""IsUsed"", FALSE) = FALSE
  AND COALESCE(ii.""IsCancelled"", FALSE) = FALSE
  AND COALESCE(ii.""IsChanged"", FALSE) = FALSE
  AND ii.""PersonID"" IS NOT NULL
  AND inv.""ExpirationDate"" IS NOT NULL
  AND (inv.""ExpirationDate"")::date >= CURRENT_DATE;
", true);
    }
    private void CreateViewRdVisaAppProgress()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_visa_app_progress CASCADE;", true);
        ExecuteNonQueryCommand(ReportDashboardPostgresRosterSql.VisaAppProgressViewSql, true);
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
        ExecuteNonQueryCommand(ReportDashboardPostgresRosterSql.VisaStateViewSql, true);
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
-- Report Dashboard: valid visas by nearest granted period (PostgreSQL).
-- Shared by Active Visa (P)/(V) preview and Open ListView (VwRdVisaByPeriod).
CREATE VIEW vw_rd_visa_by_period AS
SELECT
    x.""ID"",
    x.""PersonOid"",
    x.""PassportID"",
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
    GREATEST(0, (x.""ExpirationDate"")::date - CURRENT_DATE)              AS ""DaysRemaining"",
    (x.""Rn"" = 1)                                                        AS ""IsOneLastValidPerPerson"",
    x.""IsArchived""
FROM (
    SELECT
        v.""ID""                                                          AS ""ID"",
        p.""ID""                                                          AS ""PersonOid"",
        v.""PassportID""                                                  AS ""PassportID"",
        COALESCE(NULLIF(BTRIM(pp.""PassportNumber""), ''), '')           AS ""PassportNumber"",
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
        COALESCE(p.""IsArchived"", FALSE)                                 AS ""IsArchived"",
        ROW_NUMBER() OVER (
            PARTITION BY p.""ID""
            ORDER BY v.""ExpirationDate"" DESC NULLS LAST, v.""ID"" DESC
        )                                                               AS ""Rn""
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
-- Report Dashboard: valid visas by days remaining until expiry (Visa Validity).
-- IsOneLastValidPerPerson: latest ExpirationDate per person (ties: highest ID) — ListView/Preview toggle parity.
DROP VIEW IF EXISTS vw_rd_visa_by_days_remaining;
CREATE VIEW vw_rd_visa_by_days_remaining AS
SELECT
    x.""ID"",
    x.""PersonOid"",
    x.""PassportID"",
    x.""PassportNumber"",
    x.""PersonName"",
    x.""ProjectName"",
    x.""ProjectNameRaw"",
    x.""ProjectNameTm"",
    x.""PersonRoleCode"",
    x.""VisaNumber"",
    x.""ExpirationDate"",
    x.""DaysRemaining"",
    x.""RemainingLabel"",
    x.""StatusLabel"",
    x.""StatusCssClass"",
    (x.""Rn"" = 1)                                                        AS ""IsOneLastValidPerPerson"",
    x.""IsArchived""
FROM (
    SELECT
        v.""ID""                                                          AS ""ID"",
        p.""ID""                                                          AS ""PersonOid"",
        v.""PassportID""                                                  AS ""PassportID"",
        COALESCE(NULLIF(BTRIM(pp.""PassportNumber""), ''), '')           AS ""PassportNumber"",
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
        (v.""ExpirationDate"")::date - CURRENT_DATE                       AS ""DaysRemaining"",
        CASE
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 10  THEN '< 10 days'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 30  THEN '< 1 month'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 90  THEN '< 3 months'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 120 THEN '< 4 months'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 150 THEN '< 5 months'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 180 THEN '< 6 months'
            ELSE '≥ 6 months'
        END                                                             AS ""RemainingLabel"",
        CASE
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 10  THEN '< 10 days'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 30  THEN '< 1 month'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 90  THEN '< 3 months'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 120 THEN '< 4 months'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 150 THEN '< 5 months'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 180 THEN '< 6 months'
            ELSE '≥ 6 months'
        END                                                             AS ""StatusLabel"",
        CASE
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 30  THEN 'st-expiring'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 90  THEN 'st-pending'
            ELSE 'st-approved'
        END                                                             AS ""StatusCssClass"",
        COALESCE(p.""IsArchived"", FALSE)                                 AS ""IsArchived"",
        ROW_NUMBER() OVER (
            PARTITION BY p.""ID""
            ORDER BY v.""ExpirationDate"" DESC NULLS LAST, v.""ID"" DESC
        )                                                               AS ""Rn""
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
) x;
", true);
    }
        private void CreateViewRdVisaExtensionRequired()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_visa_extension_required;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: Extension Required (P)/(V) (PostgreSQL).
CREATE VIEW vw_rd_visa_extension_required AS
WITH valid_visas AS (
    SELECT
        v.""ID"" AS ""ID"",
        p.""ID"" AS ""PersonOid"",
        v.""PassportID"" AS ""PassportID"",
        COALESCE(NULLIF(BTRIM(pp.""PassportNumber""), ''), '') AS ""PassportNumber"",
        CONCAT_WS(' ',
            NULLIF(BTRIM(p.""FirstName""), ''),
            NULLIF(BTRIM(p.""MiddleName""), ''),
            NULLIF(BTRIM(p.""LastName""), '')
        ) AS ""PersonName"",
        COALESCE(NULLIF(BTRIM(pc.""NameTm""), ''), NULLIF(BTRIM(spc.""NameTm""), ''), '') AS ""ProjectName"",
        COALESCE(pc.""NameTm"", spc.""NameTm"", '') AS ""ProjectNameRaw"",
        COALESCE(pc.""NameTm"", spc.""NameTm"", '') AS ""ProjectNameTm"",
        p.""PersonRole"" AS ""PersonRoleCode"",
        COALESCE(NULLIF(BTRIM(v.""VisaNumber""), ''), '') AS ""VisaNumber"",
        CASE WHEN (v.""ExpirationDate"")::date > DATE '1900-01-01' THEN v.""ExpirationDate"" ELSE NULL END AS ""ExpirationDate"",
        CASE
            WHEN ((v.""ExpirationDate"")::date - (v.""StartDate"")::date) < 0 THEN 0
            ELSE ((v.""ExpirationDate"")::date - (v.""StartDate"")::date)
        END AS ""PeriodDays"",
        CASE
            WHEN ABS(((v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 30)
                 <= ABS(((v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 90)
             AND ABS(((v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 30)
                 <= ABS(((v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 180)
             AND ABS(((v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 30)
                 <= ABS(((v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 365) THEN '1 month'
            WHEN ABS(((v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 90)
                 <= ABS(((v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 180)
             AND ABS(((v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 90)
                 <= ABS(((v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 365) THEN '3 months'
            WHEN ABS(((v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 180)
                 <= ABS(((v.""ExpirationDate"")::date - (v.""StartDate"")::date) - 365) THEN '6 months'
            ELSE '1 year'
        END AS ""PeriodLabel"",
        COALESCE(NULLIF(BTRIM(vc.""NameTm""), ''), NULLIF(BTRIM(vc.""Name""), ''), '(No category)') AS ""CategoryLabel"",
        COALESCE(NULLIF(BTRIM(vt.""NameTm""), ''), NULLIF(BTRIM(vt.""Name""), ''), '(No type)') AS ""TypeLabel"",
        COALESCE(p.""IsArchived"", FALSE) AS ""IsArchived"",
        ROW_NUMBER() OVER (
            PARTITION BY p.""ID""
            ORDER BY v.""ExpirationDate"" DESC, v.""ID"" DESC
        ) AS rn
    FROM ""Visas"" v
    INNER JOIN ""Passports"" pp ON pp.""ID"" = v.""PassportID"" AND COALESCE(pp.""GCRecord"", 0) = 0
    INNER JOIN ""People"" p ON p.""ID"" = pp.""PersonID"" AND COALESCE(p.""GCRecord"", 0) = 0
    LEFT JOIN ""VisaCategories"" vc ON vc.""ID"" = v.""VisaCategoryID"" AND COALESCE(vc.""GCRecord"", 0) = 0
    LEFT JOIN ""VisaTypes"" vt ON vt.""ID"" = v.""VisaTypeID"" AND COALESCE(vt.""GCRecord"", 0) = 0
    LEFT JOIN ""ProjectContracts"" pc ON pc.""ID"" = p.""ProjectContractID"" AND COALESCE(pc.""GCRecord"", 0) = 0
    LEFT JOIN ""People"" sp ON sp.""ID"" = p.""SponsoringEmployeeID"" AND COALESCE(sp.""GCRecord"", 0) = 0
    LEFT JOIN ""ProjectContracts"" spc ON spc.""ID"" = sp.""ProjectContractID"" AND COALESCE(spc.""GCRecord"", 0) = 0
    WHERE COALESCE(v.""GCRecord"", 0) = 0
      AND COALESCE(v.""IsCancelled"", FALSE) = FALSE
      AND v.""ExpirationDate"" IS NOT NULL
      AND (v.""ExpirationDate"")::date >= CURRENT_DATE
      AND v.""StartDate"" IS NOT NULL
      AND (v.""StartDate"")::date > DATE '1900-01-01'
),
"
            + ReportDashboardPostgresRosterSql.CteVisaExtensionRosterLines() + @",
"
            + ReportDashboardPostgresRosterSql.UnfinishedExtensionPeopleCte() + @"
SELECT
    v.""ID"",
    v.""PersonOid"",
    v.""PassportID"",
    v.""PassportNumber"",
    v.""PersonName"",
    v.""ProjectName"",
    v.""ProjectNameRaw"",
    v.""ProjectNameTm"",
    v.""PersonRoleCode"",
    v.""VisaNumber"",
    v.""ExpirationDate"",
    v.""PeriodDays"",
    v.""PeriodLabel"",
    v.""CategoryLabel"",
    v.""TypeLabel"",
    GREATEST(0, (v.""ExpirationDate"")::date - CURRENT_DATE) AS ""DaysRemaining"",
    COALESCE(NULLIF(BTRIM(v.""ProjectName""), ''), '(No project)') AS ""StatusLabel"",
    'st-cat-1' AS ""StatusCssClass"",
    v.""IsArchived""
FROM valid_visas v
WHERE v.rn = 1
  AND NOT EXISTS (
        SELECT 1
        FROM unfinished_extension_people u
        WHERE u.""PersonID"" = v.""PersonOid""
    );
", true);
    }

    private void CreateViewRdVisaActiveByProject()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_visa_active_by_project;", true);
        ExecuteNonQueryCommand(@"
-- Active Visa (P): population from vw_rd_visa_by_period; StatusLabel = Project.
CREATE VIEW vw_rd_visa_active_by_project AS
SELECT
    b.""ID"",
    b.""PersonOid"",
    b.""PassportID"",
    b.""PassportNumber"",
    b.""PersonName"",
    b.""ProjectName"",
    b.""ProjectNameRaw"",
    b.""ProjectNameTm"",
    b.""PersonRoleCode"",
    b.""VisaNumber"",
    b.""ExpirationDate"",
    b.""PeriodDays"",
    b.""PeriodLabel"",
    COALESCE(NULLIF(BTRIM(b.""ProjectName""), ''), '(No project)') AS ""StatusLabel"",
    b.""StatusCssClass"",
    b.""DaysRemaining"",
    b.""IsOneLastValidPerPerson"",
    b.""IsArchived""
FROM vw_rd_visa_by_period b;
", true);
    }

    private void CreateViewRdVisaActiveByPeriodCategoryType()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_visa_active_by_period_category_type;", true);
        ExecuteNonQueryCommand(@"
-- Active Visa (V): same population; StatusLabel = Period · Category · Type.
CREATE VIEW vw_rd_visa_active_by_period_category_type AS
SELECT
    b.""ID"",
    b.""PersonOid"",
    b.""PassportID"",
    b.""PassportNumber"",
    b.""PersonName"",
    b.""ProjectName"",
    b.""ProjectNameRaw"",
    b.""ProjectNameTm"",
    b.""PersonRoleCode"",
    b.""VisaNumber"",
    b.""ExpirationDate"",
    b.""PeriodDays"",
    b.""PeriodLabel"",
    CONCAT_WS(' · ',
        COALESCE(NULLIF(BTRIM(b.""PeriodLabel""), ''), '(No period)'),
        COALESCE(NULLIF(BTRIM(c.""CategoryLabel""), ''), '(No category)'),
        COALESCE(NULLIF(BTRIM(t.""TypeLabel""), ''), '(No type)')
    ) AS ""StatusLabel"",
    b.""StatusCssClass"",
    b.""DaysRemaining"",
    b.""IsOneLastValidPerPerson"",
    b.""IsArchived""
FROM vw_rd_visa_by_period b
LEFT JOIN vw_rd_visa_by_category c ON c.""ID"" = b.""ID""
LEFT JOIN vw_rd_visa_by_type t ON t.""ID"" = b.""ID"";
", true);
    }

    private void CreateViewRdVisaOnExtension()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_visa_on_extension;", true);
        ExecuteNonQueryCommand(@"
-- Visa On Extension (P): in-flight extension apps; StatusLabel = Project · State.
CREATE VIEW vw_rd_visa_on_extension AS
SELECT
    b.""ID"",
    b.""ApplicationOid"",
    b.""PersonOid"",
    b.""ExpiringVisaID"",
    b.""PassportID"",
    b.""PassportNumber"",
    b.""CurrentStateID"",
    b.""PersonName"",
    b.""ProjectName"",
    b.""ProjectNameRaw"",
    b.""ProjectNameTm"",
    b.""PersonRoleCode"",
    b.""ApplicationNumber"",
    b.""ApplicationDate"",
    b.""StatusDate"",
    b.""ProgressStateCode"",
    b.""ProgressStateLabel"",
    b.""ProgressStateCssClass"",
    b.""DaysRemainingOnVisa"",
    CONCAT(
        COALESCE(NULLIF(BTRIM(b.""ProjectName""), ''), '(No project)'),
        ' · ',
        COALESCE(NULLIF(BTRIM(b.""ProgressStateLabel""), ''), 'Being Prepared')
    ) AS ""StatusLabel"",
    b.""IsArchived""
FROM vw_rd_visa_app_progress b
WHERE b.""ProgressStateCode"" IS NULL
   OR BTRIM(b.""ProgressStateCode"") = ''
   OR (
        b.""ProgressStateCode"" NOT IN ('PROCESS_ISSUED', 'PROCESS_CANCELLED', 'PROCESS_REJECTED')
        AND RIGHT(BTRIM(b.""ProgressStateCode""), 16) <> '_REVIEW_REJECTED'
      );
", true);
    }

    private void CreateViewRdVisaOnExtensionByPeriodCategoryType()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_visa_on_extension_by_period_category_type;", true);
        ExecuteNonQueryCommand(@"
-- Visa On Extension (V): in-flight; StatusLabel = Period · Category · Type · State.
CREATE VIEW vw_rd_visa_on_extension_by_period_category_type AS
SELECT
    b.""ID"",
    b.""ApplicationOid"",
    b.""PersonOid"",
    b.""ExpiringVisaID"",
    b.""PassportID"",
    b.""PassportNumber"",
    b.""CurrentStateID"",
    b.""PersonName"",
    b.""ProjectName"",
    b.""ProjectNameRaw"",
    b.""ProjectNameTm"",
    b.""PersonRoleCode"",
    b.""ApplicationNumber"",
    b.""ApplicationDate"",
    b.""StatusDate"",
    b.""ProgressStateCode"",
    b.""ProgressStateLabel"",
    b.""ProgressStateCssClass"",
    b.""DaysRemainingOnVisa"",
    CONCAT_WS(' · ',
        COALESCE(NULLIF(BTRIM(vp.""NameTm""), ''), NULLIF(BTRIM(vp.""Name""), ''), '(No period)'),
        COALESCE(NULLIF(BTRIM(vc.""NameTm""), ''), NULLIF(BTRIM(vc.""Name""), ''), '(No category)'),
        COALESCE(NULLIF(BTRIM(vt.""NameTm""), ''), NULLIF(BTRIM(vt.""Name""), ''), '(No type)'),
        COALESCE(NULLIF(BTRIM(b.""ProgressStateLabel""), ''), 'Being Prepared')
    ) AS ""StatusLabel"",
    b.""IsArchived""
FROM vw_rd_visa_app_progress b
LEFT JOIN ""Applications"" a
    ON a.""ID"" = b.""ApplicationOid"" AND COALESCE(a.""GCRecord"", 0) = 0
LEFT JOIN ""VisaPeriods"" vp
    ON vp.""ID"" = a.""VisaPeriodID"" AND COALESCE(vp.""GCRecord"", 0) = 0
LEFT JOIN ""VisaCategories"" vc
    ON vc.""ID"" = a.""VisaCategoryID"" AND COALESCE(vc.""GCRecord"", 0) = 0
LEFT JOIN ""VisaTypes"" vt
    ON vt.""ID"" = a.""VisaTypeID"" AND COALESCE(vt.""GCRecord"", 0) = 0
WHERE b.""ProgressStateCode"" IS NULL
   OR BTRIM(b.""ProgressStateCode"") = ''
   OR (
        b.""ProgressStateCode"" NOT IN ('PROCESS_ISSUED', 'PROCESS_CANCELLED', 'PROCESS_REJECTED')
        AND RIGHT(BTRIM(b.""ProgressStateCode""), 16) <> '_REVIEW_REJECTED'
      );
", true);
    }

    private void CreateViewRdVisaExtensionResult()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_visa_extension_result;", true);
        ExecuteNonQueryCommand(@"
-- Extension Result (P): terminal (Issued/Cancelled/Rejected/*_REVIEW_REJECTED); StatusLabel = Project · State.
CREATE VIEW vw_rd_visa_extension_result AS
SELECT
    b.""ID"",
    b.""ApplicationOid"",
    b.""PersonOid"",
    b.""ExpiringVisaID"",
    b.""PassportID"",
    b.""PassportNumber"",
    b.""CurrentStateID"",
    b.""PersonName"",
    b.""ProjectName"",
    b.""ProjectNameRaw"",
    b.""ProjectNameTm"",
    b.""PersonRoleCode"",
    b.""ApplicationNumber"",
    b.""ApplicationDate"",
    b.""StatusDate"",
    b.""ProgressStateCode"",
    b.""ProgressStateLabel"",
    b.""ProgressStateCssClass"",
    b.""DaysRemainingOnVisa"",
    CONCAT(
        COALESCE(NULLIF(BTRIM(b.""ProjectName""), ''), '(No project)'),
        ' · ',
        COALESCE(NULLIF(BTRIM(b.""ProgressStateLabel""), ''), 'Being Prepared')
    ) AS ""StatusLabel"",
    b.""IsArchived""
FROM vw_rd_visa_app_progress b
WHERE b.""ProgressStateCode"" IN ('PROCESS_ISSUED', 'PROCESS_CANCELLED', 'PROCESS_REJECTED')
   OR RIGHT(BTRIM(b.""ProgressStateCode""), 16) = '_REVIEW_REJECTED';
", true);
    }

    private void CreateViewRdVisaExtensionResultByPeriodCategoryType()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_visa_extension_result_by_period_category_type;", true);
        ExecuteNonQueryCommand(@"
-- Extension Result (V): terminal (Issued/Cancelled/Rejected/*_REVIEW_REJECTED); StatusLabel = Period · Category · Type · State.
CREATE VIEW vw_rd_visa_extension_result_by_period_category_type AS
SELECT
    b.""ID"",
    b.""ApplicationOid"",
    b.""PersonOid"",
    b.""ExpiringVisaID"",
    b.""PassportID"",
    b.""PassportNumber"",
    b.""CurrentStateID"",
    b.""PersonName"",
    b.""ProjectName"",
    b.""ProjectNameRaw"",
    b.""ProjectNameTm"",
    b.""PersonRoleCode"",
    b.""ApplicationNumber"",
    b.""ApplicationDate"",
    b.""StatusDate"",
    b.""ProgressStateCode"",
    b.""ProgressStateLabel"",
    b.""ProgressStateCssClass"",
    b.""DaysRemainingOnVisa"",
    CONCAT_WS(' · ',
        COALESCE(NULLIF(BTRIM(vp.""NameTm""), ''), NULLIF(BTRIM(vp.""Name""), ''), '(No period)'),
        COALESCE(NULLIF(BTRIM(vc.""NameTm""), ''), NULLIF(BTRIM(vc.""Name""), ''), '(No category)'),
        COALESCE(NULLIF(BTRIM(vt.""NameTm""), ''), NULLIF(BTRIM(vt.""Name""), ''), '(No type)'),
        COALESCE(NULLIF(BTRIM(b.""ProgressStateLabel""), ''), 'Being Prepared')
    ) AS ""StatusLabel"",
    b.""IsArchived""
FROM vw_rd_visa_app_progress b
LEFT JOIN ""Applications"" a
    ON a.""ID"" = b.""ApplicationOid"" AND COALESCE(a.""GCRecord"", 0) = 0
LEFT JOIN ""VisaPeriods"" vp
    ON vp.""ID"" = a.""VisaPeriodID"" AND COALESCE(vp.""GCRecord"", 0) = 0
LEFT JOIN ""VisaCategories"" vc
    ON vc.""ID"" = a.""VisaCategoryID"" AND COALESCE(vc.""GCRecord"", 0) = 0
LEFT JOIN ""VisaTypes"" vt
    ON vt.""ID"" = a.""VisaTypeID"" AND COALESCE(vt.""GCRecord"", 0) = 0
WHERE b.""ProgressStateCode"" IN ('PROCESS_ISSUED', 'PROCESS_CANCELLED', 'PROCESS_REJECTED')
   OR RIGHT(BTRIM(b.""ProgressStateCode""), 16) = '_REVIEW_REJECTED';
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
    COALESCE(ast.""Code"", '')                                                AS ""ProgressStateCode"",
    COALESCE(
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
    SELECT ap_row.""PersonId""
    FROM ""ApplicationPeople"" ap_row
    WHERE ap_row.""ApplicationId"" = a.""ID""
      AND COALESCE(ap_row.""GCRecord"", 0) = 0
    ORDER BY ap_row.""LinkedAt"", ap_row.""ID""
    LIMIT 1
) first_m2m ON TRUE
LEFT JOIN LATERAL (
    SELECT ai.""PersonID""
    FROM ""ApplicationItems"" ai
    WHERE ai.""ApplicationID"" = a.""ID""
      AND COALESCE(ai.""GCRecord"", 0) = 0
      AND NOT EXISTS (
            SELECT 1
            FROM ""ApplicationPeople"" ap_roster
            WHERE ap_roster.""ApplicationId"" = a.""ID""
              AND COALESCE(ap_roster.""GCRecord"", 0) = 0
      )
    ORDER BY ai.""ID""
    LIMIT 1
) first_legacy ON TRUE
LEFT JOIN ""People"" first_p
    ON first_p.""ID"" = COALESCE(first_m2m.""PersonId"", first_legacy.""PersonID"")
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
        ExecuteNonQueryCommand(ReportDashboardPostgresRosterSql.RegistrationViewSql, true);
    }
    private void CreateViewRdToBeCheckedIn()
    {
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_to_be_checked_in;", true);
        ExecuteNonQueryCommand(@"
-- Report Dashboard: To Be Checked In (Registration).
-- Valid visas with no registration CurrentVisa link (M2M resolved visa + legacy ApplicationItem).
-- Person must be in-country: latest TravelHistory is ExternalArrival.
-- Chart: days since that arrival TravelDate.
CREATE VIEW vw_rd_to_be_checked_in AS
WITH " + ReportDashboardPostgresRosterSql.CteRegLinkedVisaIds() + @",
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
WITH " + ReportDashboardPostgresRosterSql.CteCheckoutLinkedVisaIds() + @"
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
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 0 THEN 'Expired'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 1 THEN '< 1 day'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 2 THEN '< 2 days'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 3 THEN '< 3 days'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 4 THEN '< 4 days'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 5 THEN '< 5 days'
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 6 THEN '< 6 days'
        ELSE '< 7 days'
    END AS ""ExpiryBucketLabel"",
    CASE
        WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 0 THEN 'st-expiring'
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
  AND (v.""ExpirationDate"")::date - CURRENT_DATE < 7
  AND NOT EXISTS (
        SELECT 1 FROM checkout_linked cl WHERE cl.""VisaId"" = v.""ID""
  );
", true);
    }
}
