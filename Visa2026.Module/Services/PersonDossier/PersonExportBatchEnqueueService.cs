using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PersonLinkedDocuments;

namespace Visa2026.Module.Services.PersonDossier;

public sealed class PersonExportBatchEnqueueResult
{
    public Guid BatchId { get; init; }

    /// <summary>Document-copies records queued, plus the dossier document itself.</summary>
    public int RecordCount { get; init; }
}

/// <summary>Queues a director hand-over export for one person (see <c>docs/PERSON_DOSSIER.md</c> phase 4).</summary>
public sealed class PersonExportBatchEnqueueService
{
    private readonly INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;

    public PersonExportBatchEnqueueService(INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory)
    {
        this.nonSecuredObjectSpaceFactory = nonSecuredObjectSpaceFactory;
    }

    public bool TryEnqueuePerson(
        IObjectSpace objectSpace,
        Person person,
        string requestedBy,
        string? requestedCulture,
        out PersonExportBatchEnqueueResult? result,
        out string? errorMessageKey)
    {
        result = null;
        errorMessageKey = null;

        if (objectSpace == null || person == null)
        {
            errorMessageKey = "PersonDossier.Export.ErrorNoPerson";
            return false;
        }

        if (string.IsNullOrWhiteSpace(requestedBy))
        {
            errorMessageKey = "PersonDossier.Export.ErrorNotSignedIn";
            return false;
        }

        var personId = (Guid)objectSpace.GetKeyValue(person);
        if (personId == Guid.Empty)
        {
            errorMessageKey = "PersonDossier.Export.ErrorNoPerson";
            return false;
        }

        // The dossier document is always produced, so an export is worth running even for a person
        // with no scans on file at all.
        var copies = PersonLinkedDocumentsResolver.Resolve(objectSpace, person);
        int recordCount = copies.Sections
            .SelectMany(section => section.Records)
            .Count(record => record.Files.Any(file => file.HasContent && file.FileDataId != Guid.Empty))
            + 1;

        using var os = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<PersonExportBatch>();
        var batch = os.CreateObject<PersonExportBatch>();
        batch.RequestedBy = requestedBy;
        batch.RequestedCulture = requestedCulture;
        batch.PersonID = personId;
        batch.PersonDisplayName = person.FullName;
        batch.TotalRecords = recordCount;
        batch.ProcessedRecords = 0;
        batch.Status = PersonExportBatchStatus.Queued;
        os.CommitChanges();

        result = new PersonExportBatchEnqueueResult
        {
            BatchId = (Guid)os.GetKeyValue(batch)!,
            RecordCount = recordCount
        };
        return true;
    }
}
