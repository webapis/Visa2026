using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.ApplicationPersonLink;

public interface IApplicationProfileInstancePersonLinkQueryService
{
    IReadOnlyList<ApplicationProfileInstancePersonLinkCandidateRow> SearchCandidates(
        IObjectSpace objectSpace,
        Guid applicationId,
        string? searchText,
        int maxResults = 25);
}
