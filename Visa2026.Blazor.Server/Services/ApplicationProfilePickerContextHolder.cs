using DevExpress.ExpressApp;
using Visa2026.Module.Services.ApplicationProfilePicker;

namespace Visa2026.Blazor.Server.Services;

public sealed class ApplicationProfilePickerContextHolder : IApplicationProfilePickerContext
{
    public ApplicationProfilePickerOpenContext Context { get; set; } = new();

    public Frame? SourceFrame { get; set; }
}
