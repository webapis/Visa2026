using Visa2026.Module.Services.PersonDossier;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class PersonDossierDocumentHtmlBuilderTests
{
    [Fact]
    public void Build_WrapsHtmlDocument_BuildFragment_DoesNot()
    {
        var snapshot = MinimalSnapshot("Ada Lovelace");

        var document = PersonDossierDocumentHtmlBuilder.Build(snapshot, cultureName: null);
        var fragment = PersonDossierDocumentHtmlBuilder.BuildFragment(snapshot, cultureName: null);

        Assert.Contains("<html>", document, StringComparison.Ordinal);
        Assert.Contains("<body", document, StringComparison.Ordinal);
        Assert.DoesNotContain("<html>", fragment, StringComparison.Ordinal);
        Assert.DoesNotContain("<body", fragment, StringComparison.Ordinal);
        Assert.Contains(fragment, document, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildFragment_HtmlEncodesDisplayNameAndFieldValues()
    {
        var snapshot = new PersonDossierSnapshot
        {
            PersonDisplayName = "<script>alert(1)</script>",
            IdentityFields =
            [
                new PersonDossierField { Label = "Note & Co", Value = "A < B & C" },
            ],
        };

        var html = PersonDossierDocumentHtmlBuilder.BuildFragment(snapshot, cultureName: null);

        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html, StringComparison.Ordinal);
        Assert.Contains("Note &amp; Co", html, StringComparison.Ordinal);
        Assert.Contains("A &lt; B &amp; C", html, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildFragment_EmptyStatusTileValue_RendersDash()
    {
        var snapshot = new PersonDossierSnapshot
        {
            PersonDisplayName = "Test Person",
            StatusTiles =
            [
                new PersonDossierStatusTile
                {
                    TileId = "visa",
                    Label = "Visa",
                    Value = "   ",
                    StatusLabel = "Pending",
                    StatusCssClass = "st-pending",
                },
            ],
        };

        var html = PersonDossierDocumentHtmlBuilder.BuildFragment(snapshot, cultureName: null);

        Assert.Contains(">Visa<", html, StringComparison.Ordinal);
        Assert.Contains(">-</div>", html, StringComparison.Ordinal);
        Assert.Contains("#fdf1dc", html, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildFragment_CurrentRecord_RendersApprovedPillColor()
    {
        var snapshot = new PersonDossierSnapshot
        {
            PersonDisplayName = "Test Person",
            Sections =
            [
                new PersonDossierSection
                {
                    SectionId = "Passports",
                    SectionLabel = "Passports",
                    ColumnHeaders = ["Number"],
                    Records =
                    [
                        new PersonDossierRecord
                        {
                            RecordKey = "Passport:1",
                            Cells = ["P-1"],
                            IsCurrent = true,
                            StatusLabel = "Valid",
                            StatusCssClass = "st-approved",
                        },
                    ],
                },
            ],
        };

        var html = PersonDossierDocumentHtmlBuilder.BuildFragment(snapshot, cultureName: null);

        Assert.Contains("Passports (1)", html, StringComparison.Ordinal);
        Assert.Contains("#e3f4e6", html, StringComparison.Ordinal);
        Assert.Contains(">P-1<", html, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildFragment_PhotoDataUri_RendersImg()
    {
        var snapshot = new PersonDossierSnapshot
        {
            PersonDisplayName = "With Photo",
            PhotoDataUri = "data:image/png;base64,AAAA",
        };

        var html = PersonDossierDocumentHtmlBuilder.BuildFragment(snapshot, cultureName: null);

        Assert.Contains("<img src=\"data:image/png;base64,AAAA\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_NullSnapshot_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            PersonDossierDocumentHtmlBuilder.Build(null!, cultureName: null));
        Assert.Throws<ArgumentNullException>(() =>
            PersonDossierDocumentHtmlBuilder.BuildFragment(null!, cultureName: null));
    }

    private static PersonDossierSnapshot MinimalSnapshot(string displayName) =>
        new() { PersonDisplayName = displayName };
}
