using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures <see cref="BusinessObjects.PdfGenerationBatch.RequestedCulture"/> exists before EF schema sync
/// (packaging-notes localization; safe on prod when column was added in code before DB migrated).
/// </summary>
public sealed class PdfGenerationBatchRequestedCultureUpdater : ModuleUpdater
{
    public PdfGenerationBatchRequestedCultureUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.PdfGenerationBatches', N'U') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.PdfGenerationBatches', N'RequestedCulture') IS NULL
    ALTER TABLE dbo.PdfGenerationBatches ADD RequestedCulture nvarchar(10) NULL;", false);
    }
}
