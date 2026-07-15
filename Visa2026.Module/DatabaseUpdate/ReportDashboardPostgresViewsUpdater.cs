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

        CreateViewRdPassport();
    }

    private void CreateViewRdPassport()
    {
        // DROP first: CREATE OR REPLACE cannot insert/reorder columns.
        ExecuteNonQueryCommand(@"DROP VIEW IF EXISTS vw_rd_passport;", true);
        ExecuteNonQueryCommand(@"
CREATE VIEW vw_rd_passport AS
SELECT
    pp.""ID""                                                                 AS ""ID"",
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
    pp.""ExpirationDate""                                                     AS ""ExpirationDate"",
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
FROM (
    SELECT
        pp0.*,
        ROW_NUMBER() OVER (
            PARTITION BY pp0.""PersonID""
            ORDER BY pp0.""IssueDate"" DESC NULLS LAST, pp0.""ID"" DESC
        ) AS rn
    FROM ""Passports"" pp0
    WHERE COALESCE(pp0.""GCRecord"", 0) = 0
      AND COALESCE(pp0.""IsCancelled"", FALSE) = FALSE
) pp
INNER JOIN ""People"" p
    ON p.""ID"" = pp.""PersonID""
   AND COALESCE(p.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = p.""ProjectContractID"" AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sp
    ON sp.""ID"" = p.""SponsoringEmployeeID"" AND COALESCE(sp.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sp.""ProjectContractID"" AND COALESCE(spc.""GCRecord"", 0) = 0
LEFT JOIN ""PassportTypes"" pt
    ON pt.""ID"" = pp.""PassportTypeID"" AND COALESCE(pt.""GCRecord"", 0) = 0
LEFT JOIN ""Countries"" nat
    ON nat.""ID"" = p.""NationalityID"" AND COALESCE(nat.""GCRecord"", 0) = 0
WHERE pp.rn = 1
", true);
    }
}