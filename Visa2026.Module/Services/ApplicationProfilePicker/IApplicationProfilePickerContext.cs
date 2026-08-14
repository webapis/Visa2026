using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationProfilePicker;

public sealed class ApplicationProfilePickerOpenContext
{
    public ApplicationProfileInstanceProgressRouteKind? CreationProgressRoute { get; set; }

    public string? SourceListViewId { get; set; }

    /// <summary>When set, picker runs the Person / Dossier start-application flow (slice 11).</summary>
    public Guid? SeedPersonId { get; set; }

    /// <summary>After create, return to Person Dossier instead of opening ApplicationProfileInstance DetailView.</summary>
    public bool StayOnSourceAfterCreate { get; set; }
}

public interface IApplicationProfilePickerContext
{
    ApplicationProfilePickerOpenContext Context { get; set; }

    /// <summary>Frame that opened the picker — required for Blazor MDI <c>ShowView</c> after create.</summary>
    Frame? SourceFrame { get; set; }
}
