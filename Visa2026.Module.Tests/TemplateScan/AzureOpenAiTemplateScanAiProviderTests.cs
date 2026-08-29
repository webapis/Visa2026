#nullable enable

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.TemplateScan.Adapters;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class AzureOpenAiTemplateScanAiProviderTests
{
    private static ApplicationProfilePlaceholderSet FullSet()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        return new ApplicationProfilePlaceholderSetService(catalog).GetSet(new ApplicationProfilePlaceholderSetQuery
        {
            Profile = new ApplicationProfile(),
            DataScope = ApplicationProfileTemplateDataScope.ApplicationHeader,
            TemplateKind = ApplicationProfileTemplateKind.Word,
        });
    }

    private static TemplateAiScanOptions EnabledOptions() => new()
    {
        Provider = AzureOpenAiTemplateScanAiProvider.ProviderKey,
        RequestTimeoutSeconds = 30,
        AzureOpenAI = new TemplateAiScanAzureOpenAiOptions
        {
            Endpoint = "https://example.openai.azure.com/",
            Deployment = "gpt-4o",
            ApiVersion = "2024-10-21",
            ApiKey = "test-key",
        },
    };

    private static ScanFieldPlanRequest SampleRequest(ApplicationProfilePlaceholderSet set) =>
        new()
        {
            ScanKind = ScanKind.BlankForm,
            Playbook = new ScanAuthoringPlaybook { Markdown = "rules", Fingerprint = "fp", VersionLabel = "fp" },
            PlaceholderSet = set,
            Pages =
            [
                new ScanFieldPlanPagePayload
                {
                    PageIndex = 0,
                    PngBytes = ScanTestImageFactory.CreatePngWithDimensions(100, 100),
                    WidthPx = 100,
                    HeightPx = 100,
                },
            ],
            OcrLines = [new ScanOcrLine { PageIndex = 0, Text = "Full application number" }],
        };

    private static string WrapChatCompletion(string contentJson)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("choices");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WritePropertyName("message");
            writer.WriteStartObject();
            writer.WriteString("role", "assistant");
            writer.WriteString("content", contentJson);
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    [Fact]
    public void IsEnabled_requires_provider_key_and_secrets()
    {
        var off = new AzureOpenAiTemplateScanAiProvider(
            Options.Create(new TemplateAiScanOptions { Provider = "None" }),
            new HttpClient());
        Assert.False(off.IsEnabled);

        var on = new AzureOpenAiTemplateScanAiProvider(Options.Create(EnabledOptions()), new HttpClient());
        Assert.True(on.IsEnabled);
        Assert.Equal("AzureOpenAI", on.Key);
    }

    [Fact]
    public async Task ProposeFieldPlan_parses_model_json()
    {
        var set = FullSet();
        var request = SampleRequest(set);
        var planJson =
            """
            {"yellowHighlightCount":1,"fields":[{"fieldId":"f1","pageIndex":0,"labelText":"Full application number","proposedToken":"{{ds.AFNUM}}","confidence":"High","scope":"Header","box":{"left":0.1,"top":0.2,"right":0.9,"bottom":0.25}}],"gaps":[],"rationale":"ok"}
            """;

        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(WrapChatCompletion(planJson), Encoding.UTF8, "application/json"),
        });

        var provider = new AzureOpenAiTemplateScanAiProvider(
            Options.Create(EnabledOptions()),
            new HttpClient(handler));

        var proposal = await provider.ProposeFieldPlanAsync(request);

        Assert.Equal("AzureOpenAI", proposal.Source);
        Assert.Single(proposal.Fields);
        Assert.Equal("{{ds.AFNUM}}", proposal.Fields[0].ProposedToken);
    }

    [Fact]
    public void Di_resolves_azure_provider_when_configured()
    {
        var services = new ServiceCollection();
        services.AddTemplateScan();
        services.Configure<TemplateAiScanOptions>(o =>
        {
            o.Provider = AzureOpenAiTemplateScanAiProvider.ProviderKey;
            o.AzureOpenAI = EnabledOptions().AzureOpenAI;
            o.AzureOpenAI.ApiKey = "k";
        });

        var provider = services.BuildServiceProvider().GetRequiredService<ITemplateScanAiProvider>();
        Assert.IsType<AzureOpenAiTemplateScanAiProvider>(provider);
    }


    [Fact]
    public async Task ProposeDocxLayout_parses_structure_preserving_paragraphs()
    {
        var set = FullSet();
        var fieldPlan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
            Proposal = new ScanFieldPlanProposal
            {
                Fields =
                [
                    new ScanDetectedFieldDraft
                    {
                        FieldId = "f1",
                        PageIndex = 0,
                        LabelText = "Application number",
                        ProposedToken = "{{ds.AFNUM}}",
                        Confidence = ScanFieldConfidence.High,
                        Scope = ScanFieldScope.Header,
                        Box = ScanBoundingBox.FullPage,
                    },
                ],
                Source = "test",
            },
        });

        var layoutJson =
            """
            {"blocks":[{"kind":"paragraph","align":"left","text":"No {{ds.AFNUM}}"},{"kind":"paragraph","align":"right","text":"Turkmenistanyň Döwlet migrasiýa gullugyna"},{"kind":"paragraph","align":"left","text":"Adaty tertipde!"}],"rationale":"letter"}
            """;

        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(WrapChatCompletion(layoutJson), Encoding.UTF8, "application/json"),
        });

        var provider = new AzureOpenAiTemplateScanAiProvider(
            Options.Create(EnabledOptions()),
            new HttpClient(handler));

        var proposal = await provider.ProposeDocxLayoutAsync(new ScanDocxLayoutRequest
        {
            FieldPlan = fieldPlan,
            Playbook = new ScanAuthoringPlaybookService().GetPlaybook(),
            Pages =
            [
                new ScanPageImage
                {
                    PageIndex = 0,
                    PngBytes = ScanTestImageFactory.CreatePngWithDimensions(100, 100),
                    WidthPx = 100,
                    HeightPx = 100,
                },
            ],
        });

        Assert.True(proposal.Blocks.Count >= 3);
        Assert.Contains(proposal.Blocks, b => b.Text != null && b.Text.Contains("{{ds.AFNUM}}", StringComparison.Ordinal));
        Assert.Contains(proposal.Blocks, b => string.Equals(b.Align, "right", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(proposal.Blocks, b => b.Kind == "field");
    }

    [Fact]
    public async Task ProposeDocxLayout_parses_twoColumn_and_styles()
    {
        var set = FullSet();
        var fieldPlan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
            Proposal = new ScanFieldPlanProposal
            {
                Fields =
                [
                    new ScanDetectedFieldDraft
                    {
                        FieldId = "f1",
                        PageIndex = 0,
                        LabelText = "№ 4/-434",
                        ProposedToken = "{{ds.AFNUM}}",
                        Confidence = ScanFieldConfidence.High,
                        Scope = ScanFieldScope.Header,
                        Box = ScanBoundingBox.FullPage,
                    },
                ],
                Source = "test",
            },
        });

        var layoutJson =
            """
            {"blocks":[{"kind":"twoColumn","text":"№ {{ds.AFNUM}}","rightText":"Türkmenistanyň Döwlet\nmigrasiýa gullugyna","align":"left","rightAlign":"right"},{"kind":"paragraph","align":"left","style":"italic","text":"Adaty tertipde!"},{"kind":"paragraph","align":"justify","text":"Body text."}],"rationale":"letter-layout"}
            """;

        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(WrapChatCompletion(layoutJson), Encoding.UTF8, "application/json"),
        });

        var provider = new AzureOpenAiTemplateScanAiProvider(
            Options.Create(EnabledOptions()),
            new HttpClient(handler));

        var proposal = await provider.ProposeDocxLayoutAsync(new ScanDocxLayoutRequest
        {
            FieldPlan = fieldPlan,
            Playbook = new ScanAuthoringPlaybookService().GetPlaybook(),
            Pages =
            [
                new ScanPageImage
                {
                    PageIndex = 0,
                    PngBytes = ScanTestImageFactory.CreatePngWithDimensions(100, 100),
                    WidthPx = 100,
                    HeightPx = 100,
                },
            ],
        });

        var header = Assert.Single(proposal.Blocks, b => b.Kind == "twoColumn");
        Assert.Contains("{{ds.AFNUM}}", header.Text, StringComparison.Ordinal);
        Assert.Contains("migrasiýa", header.RightText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("right", header.RightAlign);
        Assert.Contains(proposal.Blocks, b => b.Style == "italic");
        Assert.Contains(proposal.Blocks, b => b.Align == "justify");
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _factory;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> factory) => _factory = factory;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_factory(request));
    }
}
