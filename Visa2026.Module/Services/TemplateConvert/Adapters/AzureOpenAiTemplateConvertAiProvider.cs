using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert.Adapters;

/// <summary>
/// First real adapter (E10). Talks to Azure OpenAI Chat Completions over HTTP - no vendor SDK
/// reference, so Q14 stays intact. Secrets come from options / environment, never from code.
/// </summary>
public sealed class AzureOpenAiTemplateConvertAiProvider : ITemplateConvertAiProvider
{
    public const string ProviderKey = "AzureOpenAI";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly TemplateAiConvertOptions _options;
    private readonly HttpClient _http;

    public AzureOpenAiTemplateConvertAiProvider(
        IOptions<TemplateAiConvertOptions> options,
        IHttpClientFactory httpClientFactory)
        : this(options, CreateClient(httpClientFactory, options?.Value))
    {
    }

    /// <summary>Test seam: inject a handler-backed client without IHttpClientFactory.</summary>
    public AzureOpenAiTemplateConvertAiProvider(IOptions<TemplateAiConvertOptions> options, HttpClient httpClient)
    {
        _options = options?.Value ?? new TemplateAiConvertOptions();
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public string Key => ProviderKey;

    public bool IsEnabled =>
        string.Equals(_options.Provider, ProviderKey, StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(ResolveApiKey())
        && !string.IsNullOrWhiteSpace(_options.AzureOpenAI.Endpoint)
        && !string.IsNullOrWhiteSpace(_options.AzureOpenAI.Deployment);

    public async Task<TemplateMappingPlan> ProposeMappingAsync(
        TemplateMappingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsEnabled)
        {
            return TemplateMappingPlan.FromDeterministic(
                request.PreMatched,
                rationale: "Azure OpenAI is not configured - using deterministic matches only.");
        }

        var user = BuildMappingUserPrompt(request);
        var json = await CompleteJsonAsync(MappingSystemPrompt, user, cancellationToken).ConfigureAwait(false);
        var parsed = ParsePlan(json, request);
        return parsed ?? TemplateMappingPlan.FromDeterministic(request.PreMatched, rationale: "Azure OpenAI returned an unreadable plan - using deterministic matches.");
    }

    public async Task<TemplateChatTurnResult> ApplyChatAdjustmentAsync(
        TemplateChatTurnRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsEnabled)
        {
            return new TemplateChatTurnResult(
                false,
                "AI assistance is turned off. Adjust the mapping by converting again with a different file, or edit the template in Word or Excel after save.",
                null,
                ChatRejectReason.NotUnderstood);
        }

        var user = BuildChatUserPrompt(request);
        var json = await CompleteJsonAsync(ChatSystemPrompt, user, cancellationToken).ConfigureAwait(false);
        return ParseChat(json, request) ?? new TemplateChatTurnResult(
            false,
            "I could not apply that mapping change. Ask to remap a specific field to a placeholder from this profile.",
            null,
            ChatRejectReason.NotUnderstood);
    }

    private static HttpClient CreateClient(IHttpClientFactory? factory, TemplateAiConvertOptions? options)
    {
        if (factory != null)
            return factory.CreateClient();

        var client = new HttpClient();
        var seconds = Math.Clamp(options?.RequestTimeoutSeconds ?? 60, 5, 180);
        client.Timeout = TimeSpan.FromSeconds(seconds);
        return client;
    }

    private string? ResolveApiKey()
    {
        var fromEnv = Environment.GetEnvironmentVariable("TEMPLATE_AI_CONVERT_AZURE_OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv.Trim();

        return string.IsNullOrWhiteSpace(_options.AzureOpenAI.ApiKey) ? null : _options.AzureOpenAI.ApiKey.Trim();
    }

    private async Task<string> CompleteJsonAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        var azure = _options.AzureOpenAI;
        var endpoint = (azure.Endpoint ?? string.Empty).TrimEnd('/');
        var deployment = Uri.EscapeDataString(azure.Deployment ?? string.Empty);
        var apiVersion = Uri.EscapeDataString(string.IsNullOrWhiteSpace(azure.ApiVersion) ? "2024-10-21" : azure.ApiVersion);
        var url = $"{endpoint}/openai/deployments/{deployment}/chat/completions?api-version={apiVersion}";

        var payload = new
        {
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = Truncate(userPrompt, _options.MaxDocumentCharacters) },
            },
            temperature = 0,
            response_format = new { type = "json_object" },
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("api-key", ResolveApiKey());
        req.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.RequestTimeoutSeconds, 5, 180)));

        using var response = await _http.SendAsync(req, timeout.Token).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Azure OpenAI HTTP {(int)response.StatusCode}: {TrimForError(body)}");

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("Azure OpenAI returned an empty completion.");

        return content;
    }

    private const string MappingSystemPrompt =
        """
        You propose placeholder mappings for visa office Word/Excel templates.
        Rules:
        1. Mapping only - never rewrite, restyle, translate, or change layout.
        2. Use only tokens from allowedTokens (exact token strings).
        3. Prefer preMatched decisions unless a clearer allowed token fits the same region.
        4. Regions must use the addresses provided; do not invent addresses.
        5. Reply with JSON only:
        {"substitutions":[{"regionKey":"...","token":"{{ds.AFNUM}}"}],"loops":[],"gaps":[{"regionKey":"...","literalPreview":"...","suggestedPropertyName":null}],"rationale":"..."}
        regionKey is the region's key from the user payload.
        """;

    private const string ChatSystemPrompt =
        """
        You adjust placeholder mappings for visa office templates.
        Rules:
        1. Mapping only - refuse rewrite/restyle/translate/layout/format requests.
        2. Use only tokens from allowedTokens.
        3. Reply with JSON only:
        {"accepted":true,"rejectReason":null,"replyText":"...","plan":{"substitutions":[{"regionKey":"...","token":"..."}],"loops":[],"gaps":[],"rationale":"..."}}
        On refuse: accepted=false, rejectReason one of OutOfScopeContentEdit|TokenNotInProfileSet|AmbiguousRegion|NotUnderstood, plan=null.
        """;

    private static string BuildMappingUserPrompt(TemplateMappingRequest request)
    {
        var payload = new
        {
            format = request.Format.ToString(),
            placeholderSetFingerprint = request.PlaceholderSetFingerprint,
            allowedTokens = request.AllowedTokens.Select(t => new { t.ShortCode, token = GuessToken(t), t.DisplayName, scope = t.Scope.ToString() }),
            regions = request.Regions.Select(r => new { regionKey = RegionKey(r.Region), preview = r.MaskedPreview, kind = r.Kind?.ToString(), r.RowIndex }),
            preMatched = request.PreMatched.Select(m => new { regionKey = RegionKey(m.Region), m.Token, m.ShortCode }),
        };
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static string BuildChatUserPrompt(TemplateChatTurnRequest request)
    {
        var payload = new
        {
            message = request.Message,
            placeholderSetFingerprint = request.PlaceholderSetFingerprint,
            allowedTokens = request.AllowedTokens.Select(t => new { t.ShortCode, token = GuessToken(t), t.DisplayName }),
            regions = request.Regions.Select(r => new { regionKey = RegionKey(r.Region), preview = r.MaskedPreview }),
            currentPlan = new
            {
                substitutions = request.CurrentPlan.Substitutions.Select(s => new { regionKey = RegionKey(s.Region), s.Token }),
                loops = request.CurrentPlan.Loops.Select(l => new { start = RegionKey(l.Start), end = RegionKey(l.End), l.CollectionToken }),
                gaps = request.CurrentPlan.Gaps.Select(g => new { regionKey = RegionKey(g.Region), g.LiteralPreview, g.SuggestedPropertyName }),
            },
        };
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static string GuessToken(AllowedToken token) =>
        token.Scope == UserReportPlaceholderScope.Row
            ? "{{." + token.ShortCode + "}}"
            : "{{ds." + token.ShortCode + "}}";

    private TemplateMappingPlan? ParsePlan(string json, TemplateMappingRequest request)
    {
        var dto = JsonSerializer.Deserialize<PlanDto>(ExtractJsonObject(json), JsonOptions);
        if (dto == null)
            return null;

        var byKey = request.Regions.ToDictionary(r => RegionKey(r.Region), r => r.Region, StringComparer.Ordinal);
        var allowed = request.AllowedTokens
            .Select(t => t.ShortCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var subs = new List<TokenSubstitution>();
        foreach (var s in dto.Substitutions ?? [])
        {
            if (string.IsNullOrWhiteSpace(s.RegionKey) || string.IsNullOrWhiteSpace(s.Token))
                continue;
            if (!byKey.TryGetValue(s.RegionKey, out var region))
                continue;
            if (!TemplateTokenSyntax.TryGetShortCode(s.Token, out var code) || !allowed.Contains(code))
                continue;
            subs.Add(new TokenSubstitution(region, s.Token.Trim()));
        }

        var gaps = new List<MappingGap>();
        foreach (var g in dto.Gaps ?? [])
        {
            if (string.IsNullOrWhiteSpace(g.RegionKey) || !byKey.TryGetValue(g.RegionKey, out var region))
                continue;
            gaps.Add(new MappingGap(g.LiteralPreview ?? string.Empty, g.SuggestedPropertyName, region));
        }

        if (subs.Count == 0 && request.PreMatched.Count > 0)
            return TemplateMappingPlan.FromDeterministic(request.PreMatched, gaps, dto.Rationale);

        return new TemplateMappingPlan(subs, Array.Empty<LoopMarker>(), gaps, dto.Rationale);
    }

    private TemplateChatTurnResult? ParseChat(string json, TemplateChatTurnRequest request)
    {
        var dto = JsonSerializer.Deserialize<ChatDto>(ExtractJsonObject(json), JsonOptions);
        if (dto == null)
            return null;

        if (!dto.Accepted)
        {
            ChatRejectReason? reason = null;
            if (Enum.TryParse<ChatRejectReason>(dto.RejectReason, ignoreCase: true, out var parsed))
                reason = parsed;

            return new TemplateChatTurnResult(
                false,
                string.IsNullOrWhiteSpace(dto.ReplyText) ? "I can only change placeholder mapping." : dto.ReplyText!,
                null,
                reason ?? ChatRejectReason.NotUnderstood);
        }

        if (dto.Plan == null)
            return null;

        var mappingRequest = new TemplateMappingRequest
        {
            Format = TemplateSourceFormat.Docx,
            Regions = request.Regions,
            AllowedTokens = request.AllowedTokens,
            PlaceholderSetFingerprint = request.PlaceholderSetFingerprint,
            PreMatched = request.CurrentPlan.Substitutions
                .Select(s => new DeterministicMatch(
                    s.Region,
                    s.Token,
                    TemplateTokenSyntax.TryGetShortCode(s.Token, out var code) ? code : s.Token))
                .ToList(),
        };

        var plan = ParsePlan(JsonSerializer.Serialize(dto.Plan, JsonOptions), mappingRequest);
        if (plan == null)
            return null;

        return new TemplateChatTurnResult(
            true,
            string.IsNullOrWhiteSpace(dto.ReplyText) ? "Updated the mapping." : dto.ReplyText!,
            plan,
            null);
    }

    internal static string RegionKey(DocumentRegion region) => region switch
    {
        DocumentRegion.WordSpan w => $"word:{w.ParagraphAddress}:{w.Start}:{w.Length}",
        DocumentRegion.ExcelCell e => $"excel:{e.SheetName}:{e.CellReference}",
        _ => "unknown",
    };

    private static string ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
            return text;
        return text[start..(end + 1)];
    }

    private static string Truncate(string value, int max)
    {
        if (max <= 0 || value.Length <= max)
            return value;
        return value[..max];
    }

    private static string TrimForError(string body) =>
        body.Length <= 240 ? body : body[..240] + "...";

    private sealed class PlanDto
    {
        public List<SubDto>? Substitutions { get; set; }
        public List<GapDto>? Gaps { get; set; }
        public string? Rationale { get; set; }
    }

    private sealed class SubDto
    {
        public string? RegionKey { get; set; }
        public string? Token { get; set; }
    }

    private sealed class GapDto
    {
        public string? RegionKey { get; set; }
        public string? LiteralPreview { get; set; }
        public string? SuggestedPropertyName { get; set; }
    }

    private sealed class ChatDto
    {
        public bool Accepted { get; set; }
        public string? RejectReason { get; set; }
        public string? ReplyText { get; set; }
        public PlanDto? Plan { get; set; }
    }
}