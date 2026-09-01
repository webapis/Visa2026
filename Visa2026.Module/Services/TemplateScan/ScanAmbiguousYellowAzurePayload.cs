#nullable enable

using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>JSON user payload for Azure yellow-mark refinement. Short codes stay the reply key.</summary>
public static class ScanAmbiguousYellowAzurePayload
{
    public static object Build(ScanAmbiguousYellowRefinementRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var allowed = ScanPlaceholderManualAiDto.BuildAllowedTokensByBo(request.PlaceholderSet.Allowed);

        return new
        {
            sourceKind = request.SourceKind.ToString(),
            playbookFingerprint = request.Playbook.Fingerprint,
            placeholderSetFingerprint = request.PlaceholderSet.Fingerprint,
            allowedTokensByBo = allowed,
            marks = request.Marks.Select(static m => new
            {
                m.FieldId,
                yellowText = m.YellowText,
                m.ColumnHeader,
                scope = m.Scope.ToString(),
                surroundingSnippet = m.SurroundingSnippet,
                printedLabel = m.PrintedLabel,
                sheetName = m.SheetName,
                headerRow = m.HeaderRow,
                localProposedToken = m.LocalProposedToken,
                localCandidates = m.LocalCandidates.Select(static c => new
                {
                    c.ShortCode,
                    c.Token,
                    c.ScorePercent,
                    c.Reason,
                }),
            }),
            schema = """
                {"marks":[{"fieldId":"id","proposedToken":"{{.PLN}} or compound cell template","confidence":"High|Medium|Low","candidates":[{"shortCode":"PLN","token":"{{.PLN}}","scorePercent":92,"reason":"column Familiýasy"}]}],"rationale":"..."}
                """,
        };
    }
}
