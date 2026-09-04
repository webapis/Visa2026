using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ExcelReports;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateConvert.Adapters;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateConvert;

/// <summary>E10: Azure OpenAI HTTP adapter (no vendor SDK) + DI / Q14.</summary>
public class AzureOpenAiTemplateConvertAiProviderTests
{
    private static ApplicationProfilePlaceholderSet FullSet()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        return new ApplicationProfilePlaceholderSetService(catalog).GetSet(new ApplicationProfilePlaceholderSetQuery
        {
            Profile = new ApplicationProfile
            {
                RequirePersonPassport = true,
                RequirePersonVisa = true,
                RequirePersonEducation = true,
                RequirePersonAddressOfResidence = true,
                RequirePersonPosition = true,
                RequirePersonSalary = true,
                RequirePersonMedical = true,
                RequirePersonInvitationItem = true,
                RequirePersonWorkPermitItem = true,
                RequirePersonBorderZoneItem = true,
                RequirePersonRejectionItem = true,
                RequirePersonTravelHistory = true,
            },
            DataScope = ApplicationProfileTemplateDataScope.Both,
            TemplateKind = ApplicationProfileTemplateKind.Word,
        });
    }

    private static TemplateAiConvertOptions EnabledOptions() => new()
    {
        Provider = AzureOpenAiTemplateConvertAiProvider.ProviderKey,
        RequestTimeoutSeconds = 30,
        RedactIdentifiersInExtract = true,
        AzureOpenAI = new TemplateAiConvertAzureOpenAiOptions
        {
            Endpoint = "https://example.openai.azure.com/",
            Deployment = "gpt-test",
            ApiVersion = "2024-10-21",
            ApiKey = "test-key",
        },
    };

    private static TemplateMappingRequest SampleRequest(ApplicationProfilePlaceholderSet set)
    {
        var region = new DocumentRegion.WordSpan("body/0", 0, 12);
        return new TemplateMappingRequest
        {
            Format = TemplateSourceFormat.Docx,
            Regions =
            [
                new DocumentExtractRegion(region, "TR********20", ValueKind.Identifier, null),
            ],
            AllowedTokens = set.Allowed.Select(e => new AllowedToken(e.ShortCode, e.LabelEn, e.Scope)).ToList(),
            PlaceholderSetFingerprint = set.Fingerprint,
            PreMatched =
            [
                new DeterministicMatch(region, "{{ds.AFNUM}}", "AFNUM"),
            ],
        };
    }

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
        var off = new AzureOpenAiTemplateConvertAiProvider(
            Options.Create(new TemplateAiConvertOptions { Provider = "None" }),
            new HttpClient());
        Assert.False(off.IsEnabled);

        var on = new AzureOpenAiTemplateConvertAiProvider(
            Options.Create(EnabledOptions()),
            new HttpClient());
        Assert.True(on.IsEnabled);
        Assert.Equal("AzureOpenAI", on.Key);
    }

    [Fact]
    public async Task ProposeMapping_parses_model_json_into_a_plan()
    {
        var set = FullSet();
        var request = SampleRequest(set);
        var regionKey = AzureOpenAiTemplateConvertAiProvider.RegionKey(request.Regions[0].Region);
        var planJson = "{\"substitutions\":[{\"regionKey\":\"" + regionKey + "\",\"token\":\"{{ds.AFNUM}}\"}],\"gaps\":[],\"rationale\":\"ok\"}";

        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(WrapChatCompletion(planJson), Encoding.UTF8, "application/json"),
        });

        var provider = new AzureOpenAiTemplateConvertAiProvider(
            Options.Create(EnabledOptions()),
            new HttpClient(handler));

        var plan = await provider.ProposeMappingAsync(request);
        Assert.Single(plan.Substitutions);
        Assert.Equal("{{ds.AFNUM}}", plan.Substitutions[0].Token);
        Assert.Equal("ok", plan.Rationale);
        Assert.True(handler.CallCount >= 1);
    }

    [Fact]
    public async Task Chat_refuse_json_returns_OutOfScope_without_plan()
    {
        var set = FullSet();
        var request = SampleRequest(set);
        var chatJson = "{\"accepted\":false,\"rejectReason\":\"OutOfScopeContentEdit\",\"replyText\":\"Mapping only.\",\"plan\":null}";

        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(WrapChatCompletion(chatJson), Encoding.UTF8, "application/json"),
        });

        var provider = new AzureOpenAiTemplateConvertAiProvider(
            Options.Create(EnabledOptions()),
            new HttpClient(handler));

        var result = await provider.ApplyChatAdjustmentAsync(new TemplateChatTurnRequest
        {
            Message = "remap company",
            CurrentPlan = TemplateMappingPlan.FromDeterministic(request.PreMatched),
            Regions = request.Regions,
            AllowedTokens = request.AllowedTokens,
            PlaceholderSetFingerprint = request.PlaceholderSetFingerprint,
        });

        Assert.False(result.Accepted);
        Assert.Equal(ChatRejectReason.OutOfScopeContentEdit, result.RejectReason);
        Assert.Null(result.UpdatedPlan);
    }

    [Fact]
    public async Task ConvertAsync_uses_AI_plan_when_provider_is_enabled()
    {
        var original = TemplateConvertFixtures.CreateWordDocument(
            new[] { "Arza TRM-2026-120 senesi 20.01.2026." });

        var placeholderSets = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService());
        var analyzer = new TemplateCandidateAnalyzer(Options.Create(new TemplateSuitabilityOptions()));
        var outlineReader = new TemplateDocumentOutlineReader();

        var probe = new TemplateConvertOrchestrator(
            placeholderSets,
            new ApplicationProfileInstanceValueMapService(),
            analyzer,
            outlineReader,
            new TemplateTokenWriter(),
            new TemplateConversionDiffGate(),
            new TemplateResidualValueScanner(),
            new EphemeralTemplateValidationService(
                new UserReportPlaceholderExtractor(),
                new ExcelTemplatePlaceholderExtractor(),
                new PermissiveValidator(),
                new PermissiveValidator()));

        var analysis = probe.Analyze(new TemplateConvertAnalyzeRequest
        {
            Profile = new ApplicationProfile { Name = "Test" },
            Instance = new ApplicationProfileInstance
            {
                FullApplicationNumber = "TRM-2026-120",
                ApplicationDate = new DateTime(2026, 1, 20),
            },
            Content = original,
            FileName = "letter.docx",
        });

        Assert.NotEmpty(analysis.ConvertibleHighlights);
        var hit = analysis.ConvertibleHighlights[0];
        var regionKey = AzureOpenAiTemplateConvertAiProvider.RegionKey(hit.Region);
        var planJson = "{\"substitutions\":[{\"regionKey\":\"" + regionKey + "\",\"token\":\"" + hit.Token + "\"}],\"gaps\":[],\"rationale\":\"ai\"}";

        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(WrapChatCompletion(planJson), Encoding.UTF8, "application/json"),
        });

        var ai = new AzureOpenAiTemplateConvertAiProvider(
            Options.Create(EnabledOptions()),
            new HttpClient(handler));

        var orchestrator = new TemplateConvertOrchestrator(
            placeholderSets,
            new ApplicationProfileInstanceValueMapService(),
            analyzer,
            outlineReader,
            new TemplateTokenWriter(),
            new TemplateConversionDiffGate(),
            new TemplateResidualValueScanner(),
            new EphemeralTemplateValidationService(
                new UserReportPlaceholderExtractor(),
                new ExcelTemplatePlaceholderExtractor(),
                new PermissiveValidator(),
                new PermissiveValidator()),
            ai,
            new TemplateMappingPlanSanitizer(),
            Options.Create(EnabledOptions()));

        var outcome = await orchestrator.ConvertAsync(analysis, original);
        Assert.True(outcome.Diff.Passed);
        Assert.NotEmpty(outcome.Applied);
        Assert.True(handler.CallCount >= 1);
    }

    [Fact]
    public void Q14_allows_Adapters_folder_but_not_vendor_SDK_namespaces()
    {
        var module = typeof(ITemplateConvertAiProvider).Assembly;

        var sdkNamespaces = module.GetTypes()
            .Where(t => t.Namespace != null && (
                t.Namespace.StartsWith("Azure.AI", StringComparison.Ordinal)
                || t.Namespace.StartsWith("OpenAI", StringComparison.Ordinal)
                || t.Namespace.StartsWith("Anthropic", StringComparison.Ordinal)))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(sdkNamespaces.Count == 0, "Vendor SDK namespaces in Module: " + string.Join(", ", sdkNamespaces));

        Assert.Contains(
            module.GetTypes(),
            t => t.Name == nameof(AzureOpenAiTemplateConvertAiProvider)
                 && t.Namespace == "Visa2026.Module.Services.TemplateConvert.Adapters");

        var referenced = module.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(n => n.Contains("Azure.AI", StringComparison.OrdinalIgnoreCase)
                        || n.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(referenced.Count == 0, "Vendor assemblies referenced: " + string.Join(", ", referenced));
    }

    [Fact]
    public void DI_resolves_AzureOpenAI_provider_key()
    {
        var services = new ServiceCollection();
        services.AddTemplateConvert();
        services.Configure<TemplateAiConvertOptions>(o =>
        {
            o.Provider = "AzureOpenAI";
            o.AzureOpenAI.Endpoint = "https://example.openai.azure.com/";
            o.AzureOpenAI.Deployment = "gpt-test";
            o.AzureOpenAI.ApiKey = "test-key";
        });

        using var sp = services.BuildServiceProvider();
        var ai = sp.GetRequiredService<ITemplateConvertAiProvider>();
        Assert.Equal("AzureOpenAI", ai.Key);
        Assert.True(ai.IsEnabled);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Assert.True(request.Headers.Contains("api-key"));
            return Task.FromResult(_respond(request));
        }
    }

    private sealed class PermissiveValidator : IUserReportValidationService, IExcelReportValidationService
    {
        public Task<IList<PlaceholderValidationResult>> ValidatePlaceholdersAsync(
            IList<string> placeholders,
            UserReportBoType boType) =>
            Task.FromResult(Build(placeholders));

        public Task<IList<PlaceholderValidationResult>> ValidatePlaceholdersAsync(
            IList<string> placeholders,
            UserReportBoType boType,
            ExcelMergeMode mergeMode) =>
            Task.FromResult(Build(placeholders));

        private static IList<PlaceholderValidationResult> Build(IList<string> placeholders) =>
            placeholders
                .Select(p => new PlaceholderValidationResult { PlaceholderKey = p, IsValid = true })
                .ToList();
    }
}