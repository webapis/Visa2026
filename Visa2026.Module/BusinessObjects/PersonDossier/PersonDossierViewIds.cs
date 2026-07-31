namespace Visa2026.Module.BusinessObjects.PersonDossier;

public static class PersonDossierViewIds
{
    /// <summary>
    /// Owner view id for preview-slot requests raised from the dossier. Passing this keeps
    /// <c>VisaPreviewSlotCloseController</c> from closing the slot when the officer navigates
    /// into the dossier.
    /// </summary>
    public const string DetailView = "PersonDossierHost_DetailView";
}
