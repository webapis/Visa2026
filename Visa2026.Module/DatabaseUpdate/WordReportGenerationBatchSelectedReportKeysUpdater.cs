using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures <see cref="BusinessObjects.WordReportGenerationBatch.SelectedReportKeysJson"/> exists before EF schema sync
/// (Resminamalar subset selection; safe when column was added in code before DB migrated).
/// </summary>
public sealed class WordReportGenerationBatchSelectedReportKeysUpdater : ModuleUpdater
{
    public WordReportGenerationBatchSelectedReportKeysUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.WordReportGenerationBatches', N'U') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.WordReportGenerationBatches', N'SelectedReportKeysJson') IS NULL
    ALTER TABLE dbo.WordReportGenerationBatches ADD SelectedReportKeysJson nvarchar(max) NULL;", false);
    }
}
