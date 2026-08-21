using System.Reflection;
using DevExpress.ExpressApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateConvert;

/// <summary>E8: provider seam, sanitizer, Q7 / Q13 / Q14.</summary>
public class TemplateConvertAiProviderTests
{
    private static ApplicationProfilePlaceholderSet FullSet()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var service = new ApplicationProfilePlaceholderSetService(catalog);
        return service.GetSet(new ApplicationProfilePlaceholderSetQuery
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

    private static TemplateMappingRequest SampleRequest(ApplicationProfilePlaceholderSet set)
    {
        var region = new DocumentRegion.WordSpan("body/0", 0, 12);
        return new TemplateMappingRequest
        {
            Format = TemplateSourceFormat.Docx,
            Regions =
            [
                new DocumentExtractRegion(region, "TR********20", ValueKind.Identifier, RowIndex: null),
            ],
            AllowedTokens = set.Allowed.Select(e => new AllowedToken(e.ShortCode, e.LabelEn, e.Scope)).ToList(),
            PlaceholderSetFingerprint = set.Fingerprint,
            PreMatched =
            [
                new DeterministicMatch(region, "{{ds.AFNUM}}", "AFNUM"),
            ],
        };
    }

    [Fact]
    public void Q7_TemplateMappingRequest_property_graph_carries_no_BO_and_no_raw_identifier()
    {
        var forbiddenPropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "RawValue",
            "Instance",
            "ApplicationProfile",
            "ObjectSpace",
            "IObjectSpace",
            "PassportNumber",
            "PersonalNumber",
        };

        var roots = new[] { typeof(TemplateMappingRequest), typeof(TemplateChatTurnRequest) };
        var visited = new HashSet<Type>();
        var queue = new Queue<Type>(roots);

        while (queue.Count > 0)
        {
            var type = queue.Dequeue();
            if (!visited.Add(type))
                continue;

            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                Assert.False(
                    forbiddenPropertyNames.Contains(prop.Name),
                    $"Property {type.Name}.{prop.Name} would leak case data or a BO into the provider.");

                foreach (var related in ExpandType(prop.PropertyType))
                {
                    if (related.Namespace == "Visa2026.Module.BusinessObjects")
                        Assert.Fail($"Provider request graph must not reference {related.FullName} via {type.Name}.{prop.Name}.");

                    if (typeof(IObjectSpace).IsAssignableFrom(related))
                        Assert.Fail($"Provider request graph must not carry IObjectSpace via {type.Name}.{prop.Name}.");

                    if (ShouldWalk(related) && !visited.Contains(related))
                        queue.Enqueue(related);
                }
            }
        }

        Assert.Contains(typeof(DocumentExtractRegion), visited);
        Assert.Contains(typeof(AllowedToken), visited);
        Assert.Contains(typeof(DeterministicMatch), visited);
        Assert.NotNull(typeof(DocumentExtractRegion).GetProperty(nameof(DocumentExtractRegion.MaskedPreview)));
        Assert.Null(typeof(DocumentExtractRegion).GetProperty("RawValue"));
        Assert.Null(typeof(DeterministicMatch).GetProperty("RawValue"));
    }

    private static bool ShouldWalk(Type type) =>
        type.Namespace != null
        && type.Namespace.StartsWith("Visa2026.Module.Services.TemplateConvert", StringComparison.Ordinal);

    private static IEnumerable<Type> ExpandType(Type type)
    {
        if (type.IsGenericType)
        {
            yield return type;
            foreach (var arg in type.GetGenericArguments())
            {
                foreach (var nested in ExpandType(arg))
                    yield return nested;
            }

            yield break;
        }

        if (type.IsArray)
        {
            foreach (var nested in ExpandType(type.GetElementType()!))
                yield return nested;
            yield break;
        }

        yield return type;
    }

    [Fact]
    public async Task None_provider_returns_the_deterministic_plan_and_refuses_chat()
    {
        var set = FullSet();
        var request = SampleRequest(set);
        var provider = new NoneTemplateConvertAiProvider();

        Assert.Equal("None", provider.Key);
        Assert.False(provider.IsEnabled);

        var plan = await provider.ProposeMappingAsync(request);
        Assert.Single(plan.Substitutions);
        Assert.Equal("{{ds.AFNUM}}", plan.Substitutions[0].Token);
        Assert.Contains("turned off", plan.Rationale, StringComparison.OrdinalIgnoreCase);

        var chat = await provider.ApplyChatAdjustmentAsync(new TemplateChatTurnRequest
        {
            Message = "Rewrite the greeting in English",
            CurrentPlan = plan,
            Regions = request.Regions,
            AllowedTokens = request.AllowedTokens,
            PlaceholderSetFingerprint = request.PlaceholderSetFingerprint,
        });

        Assert.False(chat.Accepted);
        Assert.Null(chat.UpdatedPlan);
        Assert.Equal(ChatRejectReason.NotUnderstood, chat.RejectReason);
        Assert.Contains("turned off", chat.ReplyText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Q13_sanitizer_drops_tokens_outside_the_profile_set()
    {
        var set = FullSet();
        var knownA = new DocumentRegion.WordSpan("body/0", 0, 5);
        var knownB = new DocumentRegion.WordSpan("body/1", 0, 5);
        var regions = new[]
        {
            new DocumentExtractRegion(knownA, "hello", ValueKind.Text, null),
            new DocumentExtractRegion(knownB, "world", ValueKind.Text, null),
        };

        var proposed = new TemplateMappingPlan(
            [
                new TokenSubstitution(knownA, "{{ds.AFNUM}}"),
                new TokenSubstitution(knownB, "{{ds.NOTAREALTOKEN}}"),
            ],
            Array.Empty<LoopMarker>(),
            Array.Empty<MappingGap>(),
            Rationale: null);

        var sanitizer = new TemplateMappingPlanSanitizer();
        var clean = sanitizer.Sanitize(proposed, set, regions, out var dropped);

        Assert.Single(clean.Substitutions);
        Assert.Equal("{{ds.AFNUM}}", clean.Substitutions[0].Token);
        Assert.Contains(dropped, static d => d.Contains("NOTAREALTOKEN", StringComparison.OrdinalIgnoreCase)
            || d.Contains("not in the profile", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Sanitizer_drops_unknown_regions_and_overlaps()
    {
        var set = FullSet();
        var known = new DocumentRegion.WordSpan("body/0", 0, 10);
        var unknown = new DocumentRegion.WordSpan("body/9", 0, 4);
        var overlap = new DocumentRegion.WordSpan("body/0", 5, 8);
        var regions = new[] { new DocumentExtractRegion(known, "abcdefghij", ValueKind.Text, null) };

        var proposed = new TemplateMappingPlan(
            [
                new TokenSubstitution(known, "{{ds.AFNUM}}"),
                new TokenSubstitution(unknown, "{{ds.ADAT}}"),
                new TokenSubstitution(overlap, "{{ds.AYEAR}}"),
            ],
            Array.Empty<LoopMarker>(),
            Array.Empty<MappingGap>(),
            null);

        var clean = new TemplateMappingPlanSanitizer().Sanitize(proposed, set, regions, out var dropped);

        Assert.Single(clean.Substitutions);
        Assert.Equal("{{ds.AFNUM}}", clean.Substitutions[0].Token);
        Assert.Equal(2, dropped.Count);
    }

    [Fact]
    public void Builder_masks_identifier_shaped_previews()
    {
        var masked = TemplateMappingRequestBuilder.MaskPreview("A1234567", ValueKind.Identifier, redactIdentifiers: true);
        Assert.Equal("A1****67", masked);
        Assert.DoesNotContain("1234", masked);

        var plain = TemplateMappingRequestBuilder.MaskPreview("Ashgabat", ValueKind.Text, redactIdentifiers: true);
        Assert.Equal("Ashgabat", plain);
    }

    [Fact]
    public void Q14_DI_resolves_None_and_Module_has_no_vendor_types()
    {
        var services = new ServiceCollection();
        services.AddTemplateConvert();
        services.Configure<TemplateAiConvertOptions>(o =>
        {
            o.Enabled = true;
            o.Provider = "None";
        });

        using var sp = services.BuildServiceProvider();
        var ai = sp.GetRequiredService<ITemplateConvertAiProvider>();
        Assert.Equal("None", ai.Key);
        Assert.False(ai.IsEnabled);
        Assert.NotNull(sp.GetRequiredService<ITemplateMappingPlanSanitizer>());

        services = new ServiceCollection();
        services.AddTemplateConvert();
        services.Configure<TemplateAiConvertOptions>(o => o.Provider = "DoesNotExist");
        using var fallback = services.BuildServiceProvider();
        Assert.Equal("None", fallback.GetRequiredService<ITemplateConvertAiProvider>().Key);

        var module = typeof(ITemplateConvertAiProvider).Assembly;
        // Adapter type names under ...Adapters may mention the vendor key; SDK namespaces / assemblies must not.
        var sdkNamespaces = module.GetTypes()
            .Where(t => t.Namespace != null && (
                t.Namespace.StartsWith("Azure.AI", StringComparison.Ordinal)
                || t.Namespace.StartsWith("OpenAI", StringComparison.Ordinal)))
            .Select(t => t.FullName)
            .ToList();
        Assert.True(sdkNamespaces.Count == 0, "Vendor SDK namespaces in Module: " + string.Join(", ", sdkNamespaces));
    }

    [Fact]
    public async Task Q14_stub_adapter_can_replace_None_without_Module_vendor_types()
    {
        var stub = new StubTemplateConvertAiProvider();
        Assert.Equal("Stub", stub.Key);
        Assert.True(stub.IsEnabled);

        var set = FullSet();
        var request = SampleRequest(set);
        var plan = await stub.ProposeMappingAsync(request);
        Assert.Empty(plan.Substitutions);

        var sanitized = new TemplateMappingPlanSanitizer().Sanitize(plan, set, request.Regions, out _);
        Assert.Empty(sanitized.Substitutions);
    }

    /// <summary>Test-only adapter in the test assembly - proves Q14's stub-adapter path.</summary>
    private sealed class StubTemplateConvertAiProvider : ITemplateConvertAiProvider
    {
        public string Key => "Stub";
        public bool IsEnabled => true;

        public Task<TemplateMappingPlan> ProposeMappingAsync(
            TemplateMappingRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(TemplateMappingPlan.Empty("stub"));

        public Task<TemplateChatTurnResult> ApplyChatAdjustmentAsync(
            TemplateChatTurnRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new TemplateChatTurnResult(false, "stub", null, ChatRejectReason.NotUnderstood));
    }
}