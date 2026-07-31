using System;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Visa2026.Module.Services.ImportHistory;

namespace Visa2026.Blazor.Server.Editors;

public class ImportReimportHistoryModel : ComponentModelBase
{
    public override Type ComponentType => typeof(ImportReimportHistoryComponent);

    public IImportReimportHistoryReader? Reader
    {
        get => GetPropertyValue<IImportReimportHistoryReader?>();
        set => SetPropertyValue(value);
    }
}
