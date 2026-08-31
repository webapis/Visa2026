#nullable enable

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan.Adapters;

/// <summary>S2: Azure OpenAI vision adapter for scan field plans. HTTP only — no vendor SDK (Q14 parity with Convert).</summary>
public sealed class AzureOpenAiTemplateScanAiProvider : ITemplateScanAiProvider
{
    public const string ProviderKey = "AzureOpenAI";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly TemplateAiScanOptions _options;
    private readonly HttpClient _http;

    public AzureOpenAiTemplateScanAiProvider(
        IOptions<TemplateAiScanOptions> options,
        IHttpClientFactory httpClientFactory)
        : this(options, CreateClient(httpClientFactory, options?.Value))
    {
    }

    public AzureOpenAiTemplateScanAiProvider(IOptions<TemplateAiScanOptions> options, HttpClient httpClient)
    {
        _options = options?.Value ?? new TemplateAiScanOptions();
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public string Key => ProviderKey;

    public bool IsEnabled =>
        string.Equals(_options.Provider, ProviderKey, StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(ResolveApiKey())
        && !string.IsNullOrWhiteSpace(_options.AzureOpenAI.Endpoint)
        && !string.IsNullOrWhiteSpace(_options.AzureOpenAI.Deployment);

    public async Task<ScanFieldPlanProposal> ProposeFieldPlanAsync(
        ScanFieldPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsEnabled)
            return DeterministicScanFieldPlanner.Build(request);

        var userParts = BuildVisionUserContent(request);
        var json = await CompleteJsonAsync(FieldPlanSystemPrompt, userParts, cancellationToken).ConfigureAwait(false);
        var parsed = ParseFieldPlan(json, request);
        return parsed ?? DeterministicScanFieldPlanner.Build(request);
    }

    public async Task<ScanClarificationResult> ClarifyAsync(
        ScanClarificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsEnabled)
        {
            return new ScanClarificationResult
            {
                Accepted = false,
                ReplyText = "AI assistance is turned off. Review detected fields on the list or enable a vision provider.",
                Plan = ScanFieldPlanMapper.ToProposal(request.CurrentPlan, Key),
            };
        }

        var user = BuildClarificationUserPrompt(request);
        var json = await CompleteJsonAsync(ClarificationSystemPrompt, new object[] { new { type = "text", text = user } }, cancellationToken)
            .ConfigureAwait(false);
        return ParseClarification(json, request)
            ?? new ScanClarificationResult
            {
                Accepted = false,
                ReplyText = "I could not apply that clarification. Name a specific label and the placeholder token you want.",
                Plan = ScanFieldPlanMapper.ToProposal(request.CurrentPlan, Key),
            };
    }

    public async Task<ScanDocxLayoutProposal> ProposeDocxLayoutAsync(
        ScanDocxLayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsEnabled)
            return DeterministicScanDocxLayoutPlanner.Build(request);

        try
        {
            var userParts = BuildLayoutUserContent(request);
            var json = await CompleteJsonAsync(LayoutSystemPrompt, userParts, cancellationToken).ConfigureAwait(false);
            var parsed = ParseLayout(json, request);
            if (parsed != null && parsed.Blocks.Count > 0)
                return parsed;
        }
        catch (Exception ex) when (ex is not OperationCanceledException
            and not OutOfMemoryException
            and not StackOverflowException)
        {
            // Fall through to deterministic reconstruction.
        }

        return DeterministicScanDocxLayoutPlanner.Build(request);
    }

    public async Task<ScanAmbiguousYellowRefinementResult> RefineAmbiguousYellowMarksAsync(
        ScanAmbiguousYellowRefinementRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsEnabled || request.Marks.Count == 0)
            return PassthroughAmbiguousYellow(request);

        try
        {
            var userText = BuildAmbiguousYellowUserPayload(request);
            var json = await CompleteJsonAsync(
                    AmbiguousYellowSystemPrompt,
                    [new { type = "text", text = userText }],
                    cancellationToken)
                .ConfigureAwait(false);

            var parsed = ParseAmbiguousYellow(json, request);
            if (parsed != null && parsed.Marks.Count > 0)
                return parsed;
        }
        catch (Exception ex) when (ex is not OperationCanceledException
            and not OutOfMemoryException
            and not StackOverflowException)
        {
            // Fall through — keep local rules result.
        }

        return PassthroughAmbiguousYellow(request);
    }

    private object[] BuildLayoutUserContent(ScanDocxLayoutRequest request)
    {
        var allowedTokens = request.FieldPlan.Fields
            .Where(static f => !string.IsNullOrWhiteSpace(f.ProposedToken))
            .Select(f => new
            {
                f.FieldId,
                f.LabelText,
                token = f.ProposedToken,
                scope = f.Scope.ToString(),
                box = new { f.Box.Left, f.Box.Top, f.Box.Right, f.Box.Bottom },
                f.PageIndex,
            })
            .ToList();

        var textPayload = new
        {
            playbookFingerprint = request.Playbook.Fingerprint,
            mappedFields = allowedTokens,
            ocrLines = request.OcrLines.Select(l => new { l.PageIndex, l.Text }),
            valueHints = request.ValueHints.Select(h => new { h.Token, h.MaskedValue, h.LabelText }),
            schema = """
                {"blocks":[
                  {"kind":"twoColumn","text":"left cell (use \\n for lines)","rightText":"right cell","align":"left","rightAlign":"right","style":"normal|italic|bold|boldItalic","rightStyle":"normal|italic|bold|boldItalic"},
                  {"kind":"paragraph","align":"left|right|center|justify","style":"normal|italic|bold|boldItalic","text":"Full paragraph; embed {{ds.AFNUM}} where data values belong"},
                  {"kind":"blank"}
                ],"rationale":"..."}
                """,
            instructions = """
                Reconstruct the scanned ministry letter as Word blocks that MATCH the scan's layout and alignment — not a flat stack of left-aligned lines.
                Keep static boilerplate wording (Turkmen/Turkish/etc.) intact.
                For EVERY entry in mappedFields, replace the corresponding value ON THE SCAN with that exact token (even if valueHints differ — the scan may be from another case).
                Typical placements: header № / application number → AFNUM; letter date → ADAT; urgency line → Urgency_NameTm; person count → TPCNT/TPCTX; visa period/category → VPER/VCAT.
                LAYOUT RULES (critical):
                - When the scan has LEFT content and RIGHT content on the SAME horizontal band (e.g. №/date left + addressee right; director title left + signatory name right), emit kind=twoColumn with text=left and rightText=right. Use \\n inside a cell for multi-line stacks.
                - Header twoColumn MUST be: left = №/application number AND letter date (stacked); right = addressee only (e.g. Türkmenistanyň Döwlet migrasiýa gullugyna). NEVER put ADAT/date alone on the right.
                - Addressee block on the scan's right side → rightAlign=right (usually inside twoColumn).
                - Body paragraphs that span the page width → align=justify.
                - Short urgency line like \"Adaty tertipde!\" → align=left, style=italic when italic on the scan.
                - Signature title/name in bold on the scan → style=bold / rightStyle=bold.
                - Do NOT put the right-side addressee as a left-aligned paragraph under the header; keep it opposite via twoColumn.
                - Do NOT drop the addressee text; it is static boilerplate, not a placeholder.
                Do NOT leave mapped values as literals when a token exists.
                Do NOT append leftover tokens as a list at the end of the document.
                Do NOT emit a flat Label: {{token}} catalog.
                Do NOT invent placeholders outside mappedFields.
                Prefer kind=paragraph or twoColumn; use kind=blank for empty lines.
                """,
        };

        var parts = new List<object>
        {
            new { type = "text", text = Truncate(JsonSerializer.Serialize(textPayload, JsonOptions), _options.MaxPromptCharacters) },
        };

        foreach (var page in request.Pages.Take(5))
        {
            var b64 = Convert.ToBase64String(page.PngBytes);
            parts.Add(new
            {
                type = "image_url",
                image_url = new { url = $"data:image/png;base64,{b64}", detail = "high" },
            });
        }

        return parts.ToArray();
    }

    private ScanDocxLayoutProposal? ParseLayout(string json, ScanDocxLayoutRequest request)
    {
        var dto = JsonSerializer.Deserialize<LayoutDto>(ExtractJsonObject(json), JsonOptions);
        if (dto?.Blocks == null || dto.Blocks.Count == 0)
            return null;

        var allowed = request.FieldPlan.Fields
            .Where(static f => !string.IsNullOrWhiteSpace(f.ProposedToken))
            .Select(static f => f.ProposedToken!.Trim())
            .ToHashSet(StringComparer.Ordinal);

        var allowedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in allowed)
        {
            if (TemplateTokenSyntax.TryGetShortCode(token, out var code))
                allowedCodes.Add(code);
        }

        var blocks = new List<ScanDocxBlock>();
        foreach (var b in dto.Blocks)
        {
            var kind = string.IsNullOrWhiteSpace(b.Kind) ? "paragraph" : b.Kind.Trim();
            if (string.Equals(kind, "blank", StringComparison.OrdinalIgnoreCase))
            {
                blocks.Add(new ScanDocxBlock { Kind = "blank", Align = NormalizeAlign(b.Align) });
                continue;
            }

            if (string.Equals(kind, "twoColumn", StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind, "columns", StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind, "row", StringComparison.OrdinalIgnoreCase))
            {
                var leftRaw = FirstNonEmpty(b.Text, b.Left) ?? string.Empty;
                var rightRaw = FirstNonEmpty(b.RightText, b.Right) ?? string.Empty;
                var left = SanitizeEmbeddedTokens(leftRaw, allowed, allowedCodes);
                var right = SanitizeEmbeddedTokens(rightRaw, allowed, allowedCodes);
                if (string.IsNullOrWhiteSpace(left) && string.IsNullOrWhiteSpace(right))
                    continue;

                blocks.Add(new ScanDocxBlock
                {
                    Kind = "twoColumn",
                    Text = left,
                    RightText = right,
                    Align = NormalizeAlign(FirstNonEmpty(b.Align, b.LeftAlign) ?? "left"),
                    RightAlign = NormalizeAlign(b.RightAlign ?? "right"),
                    Style = NormalizeStyle(FirstNonEmpty(b.Style, b.LeftStyle)),
                    RightStyle = NormalizeStyle(b.RightStyle ?? b.Style),
                });
                continue;
            }

            if (string.Equals(kind, "loopOpen", StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind, "loopClose", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(b.Token))
                    blocks.Add(new ScanDocxBlock { Kind = kind, Token = b.Token.Trim(), Align = NormalizeAlign(b.Align) });
                continue;
            }

            if (string.Equals(kind, "field", StringComparison.OrdinalIgnoreCase))
            {
                var token = b.Token?.Trim();
                if (string.IsNullOrWhiteSpace(token) || !IsAllowedToken(token, allowed, allowedCodes))
                    continue;

                blocks.Add(new ScanDocxBlock
                {
                    Kind = "field",
                    Text = b.Text,
                    Token = token,
                    Align = NormalizeAlign(b.Align),
                    Style = NormalizeStyle(b.Style),
                });
                continue;
            }

            var text = SanitizeEmbeddedTokens(b.Text ?? string.Empty, allowed, allowedCodes);
            if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(b.Token))
                continue;

            if (!string.IsNullOrWhiteSpace(b.Token) && IsAllowedToken(b.Token, allowed, allowedCodes)
                && string.IsNullOrWhiteSpace(text))
            {
                text = b.Token.Trim();
            }

            blocks.Add(new ScanDocxBlock
            {
                Kind = "paragraph",
                Text = text,
                Align = NormalizeAlign(b.Align),
                Style = NormalizeStyle(b.Style),
            });
        }

        if (blocks.Count == 0)
            return null;

        return new ScanDocxLayoutProposal
        {
            Blocks = blocks,
            Rationale = string.IsNullOrWhiteSpace(dto.Rationale) ? ProviderKey : dto.Rationale.Trim(),
        };
    }

    private static bool IsAllowedToken(string token, HashSet<string> allowed, HashSet<string> allowedCodes)
    {
        var trimmed = token.Trim();
        if (allowed.Contains(trimmed))
            return true;
        return TemplateTokenSyntax.TryGetShortCode(trimmed, out var code) && allowedCodes.Contains(code);
    }

    private static string SanitizeEmbeddedTokens(string text, HashSet<string> allowed, HashSet<string> allowedCodes)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return System.Text.RegularExpressions.Regex.Replace(
            text,
            @"\{\{[^{}]+\}\}",
            match => IsAllowedToken(match.Value, allowed, allowedCodes) ? match.Value : string.Empty);
    }

    private static string? NormalizeAlign(string? align)
    {
        if (string.IsNullOrWhiteSpace(align))
            return "left";

        var value = align.Trim().ToLowerInvariant();
        return value is "left" or "right" or "center" or "justify" or "both"
            ? (value == "both" ? "justify" : value)
            : "left";
    }

    private static string? NormalizeStyle(string? style)
    {
        if (string.IsNullOrWhiteSpace(style))
            return null;

        var value = style.Trim().ToLowerInvariant()
            .Replace("-", "", StringComparison.Ordinal)
            .Replace("_", "", StringComparison.Ordinal);
        return value switch
        {
            "italic" or "italics" or "i" => "italic",
            "bold" or "b" or "strong" => "bold",
            "bolditalic" or "italicbold" or "bolditalics" => "boldItalic",
            "normal" or "regular" => "normal",
            _ => null,
        };
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static HttpClient CreateClient(IHttpClientFactory? factory, TemplateAiScanOptions? options)
    {
        if (factory != null)
            return factory.CreateClient();

        var client = new HttpClient();
        var seconds = Math.Clamp(options?.RequestTimeoutSeconds ?? 90, 5, 180);
        client.Timeout = TimeSpan.FromSeconds(seconds);
        return client;
    }

    private string? ResolveApiKey()
    {
        var fromEnv = Environment.GetEnvironmentVariable("TEMPLATE_AI_SCAN_AZURE_OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv.Trim();

        // Local dev often configures Convert only — allow the same key for Scan.
        fromEnv = Environment.GetEnvironmentVariable("TEMPLATE_AI_CONVERT_AZURE_OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv.Trim();

        return string.IsNullOrWhiteSpace(_options.AzureOpenAI.ApiKey) ? null : _options.AzureOpenAI.ApiKey.Trim();
    }

    private async Task<string> CompleteJsonAsync(
        string systemPrompt,
        object[] userContentParts,
        CancellationToken cancellationToken)
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
                new { role = "user", content = userContentParts },
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

    private object[] BuildVisionUserContent(ScanFieldPlanRequest request)
    {
        var allowed = request.PlaceholderSet.Allowed
            .Select(e => new
            {
                e.ShortCode,
                token = e.BuildWordToken(
                    e.Scope == UserReportPlaceholderScope.Row
                        ? UserReportPlaceholderScope.Row
                        : UserReportPlaceholderScope.Header),
                e.LabelEn,
                example = e.ExampleValue,
                path = e.CanonicalPath,
                scope = e.Scope.ToString(),
            });

        var textPayload = new
        {
            scanKind = request.ScanKind.ToString(),
            playbookFingerprint = request.Playbook.Fingerprint,
            placeholderSetFingerprint = request.PlaceholderSet.Fingerprint,
            allowedTokens = allowed,
            ocrLines = request.OcrLines.Select(l => new { l.PageIndex, l.Text }),
            valueHints = request.ValueHints.Select(h => new { h.Token, h.MaskedValue, h.LabelText }),
            deterministicSeeds = request.DeterministicSeeds.Select(f => new
            {
                f.FieldId,
                f.LabelText,
                f.ProposedToken,
                box = new { f.Box.Left, f.Box.Top, f.Box.Right, f.Box.Bottom },
            }),
            schema = """
                {"yellowHighlightCount":3,"fields":[{"fieldId":"uuid","pageIndex":0,"labelText":"exact yellow text","proposedToken":"{{ds.AFNUM}} or null","confidence":"High|Medium|Low","scope":"Header|Row|LoopBoundary","box":{"left":0.1,"top":0.2,"right":0.9,"bottom":0.25}}],"gaps":[{"fieldId":"uuid","labelText":"yellow snippet with no library match","suggestedPropertyName":null}],"pendingQuestions":[],"rationale":"..."}
                """,
        };

        var parts = new List<object>
        {
            new { type = "text", text = Truncate(JsonSerializer.Serialize(textPayload, JsonOptions), _options.MaxPromptCharacters) },
        };

        foreach (var page in request.Pages.Take(5))
        {
            var b64 = Convert.ToBase64String(page.PngBytes);
            parts.Add(new
            {
                type = "image_url",
                image_url = new { url = $"data:image/png;base64,{b64}", detail = "high" },
            });
        }

        return parts.ToArray();
    }

    private ScanFieldPlanProposal? ParseFieldPlan(string json, ScanFieldPlanRequest request)
    {
        var dto = JsonSerializer.Deserialize<FieldPlanDto>(ExtractJsonObject(json), JsonOptions);
        if (dto == null)
            return null;

        var allowed = request.PlaceholderSet.Allowed
            .Select(static e => e.ShortCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var fields = new List<ScanDetectedFieldDraft>();
        foreach (var f in dto.Fields ?? [])
        {
            if (string.IsNullOrWhiteSpace(f.FieldId) || string.IsNullOrWhiteSpace(f.LabelText))
                continue;

            ScanFieldConfidence confidence = ScanFieldConfidence.Medium;
            if (!string.IsNullOrWhiteSpace(f.Confidence)
                && Enum.TryParse(f.Confidence, true, out ScanFieldConfidence parsed))
                confidence = parsed;

            ScanFieldScope scope = ScanFieldScope.Header;
            if (!string.IsNullOrWhiteSpace(f.Scope)
                && Enum.TryParse(f.Scope, true, out ScanFieldScope scopeParsed))
                scope = scopeParsed;

            var token = string.IsNullOrWhiteSpace(f.ProposedToken) ? null : f.ProposedToken.Trim();
            if (token != null
                && (!TemplateTokenSyntax.TryGetShortCode(token, out var code) || !allowed.Contains(code)))
                token = null;

            fields.Add(new ScanDetectedFieldDraft
            {
                FieldId = f.FieldId,
                PageIndex = f.PageIndex,
                LabelText = f.LabelText.Trim(),
                ProposedToken = token,
                Confidence = confidence,
                Scope = scope,
                Box = f.Box == null
                    ? ScanBoundingBox.FullPage
                    : new ScanBoundingBox(f.Box.Left, f.Box.Top, f.Box.Right, f.Box.Bottom).Clamp(),
            });
        }

        var gaps = (dto.Gaps ?? [])
            .Where(g => !string.IsNullOrWhiteSpace(g.LabelText))
            .Select(g => new ScanGapDraft(
                string.IsNullOrWhiteSpace(g.FieldId) ? Guid.NewGuid().ToString("N") : g.FieldId,
                g.LabelText.Trim(),
                g.SuggestedPropertyName))
            .ToList();

        return new ScanFieldPlanProposal
        {
            Fields = fields,
            Gaps = gaps,
            PendingQuestions = Array.Empty<ScanClarificationPrompt>(),
            Rationale = dto.Rationale,
            Source = ProviderKey,
            YellowHighlightCount = dto.YellowHighlightCount,
        };
    }

    private string BuildClarificationUserPrompt(ScanClarificationRequest request)
    {
        var allowed = request.PlaceholderSet.Allowed
            .Select(e => new
            {
                e.ShortCode,
                token = e.BuildWordToken(
                    e.Scope == UserReportPlaceholderScope.Row
                        ? UserReportPlaceholderScope.Row
                        : UserReportPlaceholderScope.Header),
                e.LabelEn,
                example = e.ExampleValue,
                path = e.CanonicalPath,
                scope = e.Scope.ToString(),
            });

        var current = new
        {
            fields = request.CurrentPlan.Fields.Select(f => new
            {
                f.FieldId,
                f.PageIndex,
                f.LabelText,
                f.ProposedToken,
                confidence = f.Confidence.ToString(),
                scope = f.Scope.ToString(),
                box = new { f.Box.Left, f.Box.Top, f.Box.Right, f.Box.Bottom },
            }),
            gaps = request.CurrentPlan.Gaps.Select(g => new { g.FieldId, g.LabelText, g.SuggestedPropertyName }),
            pendingQuestions = request.CurrentPlan.PendingQuestions.Select(q => new { q.Question, q.SuggestedAnswers }),
        };

        var payload = new
        {
            officerMessage = request.OfficerMessage,
            playbookFingerprint = request.Playbook.Fingerprint,
            placeholderSetFingerprint = request.PlaceholderSet.Fingerprint,
            allowedTokens = allowed,
            currentPlan = current,
            schema = """
                {"accepted":true,"replyText":"short officer-facing reply","fields":[{"fieldId":"uuid","pageIndex":0,"labelText":"label","proposedToken":"{{ds.AFNUM}} or null","confidence":"High|Medium|Low","scope":"Header|Row|LoopBoundary","box":{"left":0.1,"top":0.2,"right":0.9,"bottom":0.25}}],"gaps":[{"fieldId":"uuid","labelText":"snippet","suggestedPropertyName":null}],"pendingQuestions":[{"question":"...","suggestedAnswers":["a","b"]}],"rationale":"..."}
                """,
        };

        return Truncate(JsonSerializer.Serialize(payload, JsonOptions), _options.MaxPromptCharacters);
    }

    private ScanClarificationResult? ParseClarification(string json, ScanClarificationRequest request)
    {
        var dto = JsonSerializer.Deserialize<ClarificationDto>(ExtractJsonObject(json), JsonOptions);
        if (dto == null)
            return null;

        var proposal = ParseFieldPlan(json, new ScanFieldPlanRequest
        {
            ScanKind = request.CurrentPlan.ScanKind,
            Playbook = request.Playbook,
            PlaceholderSet = request.PlaceholderSet,
            Pages = Array.Empty<ScanFieldPlanPagePayload>(),
            OcrLines = Array.Empty<ScanOcrLine>(),
        });

        if (proposal == null)
            return null;

        return new ScanClarificationResult
        {
            Accepted = dto.Accepted,
            ReplyText = string.IsNullOrWhiteSpace(dto.ReplyText)
                ? (dto.Accepted ? "Updated the field plan." : "Could not apply that clarification.")
                : dto.ReplyText.Trim(),
            Plan = proposal,
        };
    }

    private const string ClarificationSystemPrompt =
        """
        You help officers clarify scan field mapping before a Word template is generated.
        Rules:
        1. Mapping only — adjust which labels map to allowed placeholders; never rewrite ministry boilerplate or change layout intent.
        2. Use only tokens from allowedTokens.
        3. Preserve fieldId values when updating existing fields; add new fieldIds for newly detected labels.
        4. Set accepted=false when the officer asks for out-of-scope edits (wording, fonts, translation, scan quality).
        5. Reply with JSON only matching the schema in the user message.
        """;

    private const string LayoutSystemPrompt =
        """
        You reconstruct a scanned ministry letter as a Word template layout for local OOXML generation.
        Rules:
        1. Preserve document structure, alignment, and static boilerplate; do not flatten into Label: token rows.
        2. Match scan layout: side-by-side bands use kind=twoColumn; body uses align=justify; italic/bold when visible on the scan.
        3. Embed only tokens listed in mappedFields, using their exact {{ds.…}} spelling.
        4. Place EVERY mappedFields token in-context where that data appears on the scan; never dump unused tokens at the end.
        5. Leave legal/boilerplate sentences intact aside from those token substitutions.
        6. Reading order top-to-bottom.
        7. Reply with JSON only matching the schema in the user message.
        """;

    private const string AmbiguousYellowSystemPrompt =
        """
        You map YELLOW-HIGHLIGHTED SAMPLE LITERALS in Word/Excel templates to allowed merge placeholders.
        CRITICAL:
        - Yellow text is FICTITIOUS SAMPLE DATA for template authoring (e.g. "Erol", "Hilmi"). NEVER match against a live case database or officer roster.
        - Compare yellow text to placeholder manual labels, exampleValue shapes, and Excel column headers only.
        - exampleValue in the manual shows the KIND of value (date, country code, name), not a person to look up.
        MERGE TOOL RULES:
        - Word header scalars: {{ds.ShortCode}}; roster row inside loops: {{.ShortCode}}.
        - Excel sanaw tables: row {{.ShortCode}}; footer/header scalars {{ds.ShortCode}}; loop marker {{#ds.rows}}.
        - One yellow cell may need MULTIPLE tokens separated by ", " or "/" (preserve separators in proposedToken).
        - Tokens must pass Extract/Validate for UserReportGenerator / ExcelReportGenerator.
        TASK:
        1. For each mark, rank allowed tokens with scorePercent 0-100 and brief reason.
        2. proposedToken = best match (compound allowed). Use only allowedTokens short codes.
        3. Prefer columnHeader when present (Excel). Use localCandidates as hints, you may override.
        4. confidence: High (>=80), Medium (55-79), Low (<55).
        5. Reply JSON only per user schema.
        """;

    private static ScanAmbiguousYellowRefinementResult PassthroughAmbiguousYellow(ScanAmbiguousYellowRefinementRequest request)
    {
        return new ScanAmbiguousYellowRefinementResult
        {
            Marks = request.Marks.Select(static m => new ScanAmbiguousYellowMarkResult
            {
                FieldId = m.FieldId,
                ProposedToken = m.LocalProposedToken,
                Confidence = ScanFieldConfidence.Medium,
                Candidates = m.LocalCandidates,
            }).ToList(),
            Source = "local",
        };
    }

    private string BuildAmbiguousYellowUserPayload(ScanAmbiguousYellowRefinementRequest request)
    {
        var allowed = request.PlaceholderSet.Allowed
            .Select(e => new
            {
                e.ShortCode,
                tokenHeader = e.BuildWordToken(UserReportPlaceholderScope.Header),
                tokenRow = e.BuildWordToken(UserReportPlaceholderScope.Row),
                e.LabelEn,
                labelTk = e.LabelTk,
                labelTr = e.LabelTr,
                example = e.ExampleValue,
                path = e.CanonicalPath,
                scope = e.Scope.ToString(),
            });

        var payload = new
        {
            sourceKind = request.SourceKind.ToString(),
            playbookFingerprint = request.Playbook.Fingerprint,
            placeholderSetFingerprint = request.PlaceholderSet.Fingerprint,
            allowedTokens = allowed,
            marks = request.Marks.Select(m => new
            {
                m.FieldId,
                yellowText = m.YellowText,
                m.ColumnHeader,
                scope = m.Scope.ToString(),
                localProposedToken = m.LocalProposedToken,
                localCandidates = m.LocalCandidates.Select(c => new
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

        return Truncate(JsonSerializer.Serialize(payload, JsonOptions), _options.MaxPromptCharacters);
    }

    private ScanAmbiguousYellowRefinementResult? ParseAmbiguousYellow(
        string json,
        ScanAmbiguousYellowRefinementRequest request)
    {
        var dto = JsonSerializer.Deserialize<AmbiguousYellowDto>(ExtractJsonObject(json), JsonOptions);
        if (dto?.Marks == null || dto.Marks.Count == 0)
            return null;

        var allowed = request.PlaceholderSet.Allowed
            .Select(static e => e.ShortCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var results = new List<ScanAmbiguousYellowMarkResult>();
        foreach (var mark in dto.Marks)
        {
            if (string.IsNullOrWhiteSpace(mark.FieldId))
                continue;

            var candidates = (mark.Candidates ?? [])
                .Where(c => !string.IsNullOrWhiteSpace(c.ShortCode) && allowed.Contains(c.ShortCode!))
                .Select(c => new ScanTokenAlternative(
                    ResolveToken(c.Token, c.ShortCode!, request.PlaceholderSet, mark.Scope),
                    c.ShortCode!,
                    Math.Clamp(c.ScorePercent ?? 0, 0, 100),
                    c.Reason ?? "AI rank"))
                .Where(c => !string.IsNullOrWhiteSpace(c.Token))
                .OrderByDescending(static c => c.ScorePercent)
                .ToList();

            var token = string.IsNullOrWhiteSpace(mark.ProposedToken)
                ? candidates.FirstOrDefault()?.Token
                : mark.ProposedToken.Trim();

            token = SanitizeAiToken(token, request.PlaceholderSet);
            if (token == null && candidates.Count > 0)
                token = candidates[0].Token;

            ScanFieldConfidence confidence = ScanFieldConfidence.Medium;
            if (!string.IsNullOrWhiteSpace(mark.Confidence)
                && Enum.TryParse(mark.Confidence, true, out ScanFieldConfidence parsed))
                confidence = parsed;

            results.Add(new ScanAmbiguousYellowMarkResult
            {
                FieldId = mark.FieldId,
                ProposedToken = token,
                Confidence = confidence,
                Candidates = candidates,
            });
        }

        return new ScanAmbiguousYellowRefinementResult
        {
            Marks = results,
            Rationale = dto.Rationale,
            Source = ProviderKey,
        };
    }

    private static string ResolveToken(
        string? token,
        string shortCode,
        ApplicationProfilePlaceholderSet placeholderSet,
        string? scopeHint)
    {
        if (!string.IsNullOrWhiteSpace(token))
            return token.Trim();

        if (!placeholderSet.Contains(shortCode))
            return string.Empty;

        var entry = placeholderSet.Allowed.First(e =>
            string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));

        var usage = entry.Scope == UserReportPlaceholderScope.Row
            || string.Equals(scopeHint, "Row", StringComparison.OrdinalIgnoreCase)
            ? UserReportPlaceholderScope.Row
            : UserReportPlaceholderScope.Header;

        return entry.BuildWordToken(usage);
    }

    private static string? SanitizeAiToken(string? token, ApplicationProfilePlaceholderSet placeholderSet)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var trimmed = token.Trim();
        if (trimmed.Contains("{{", StringComparison.Ordinal))
            return trimmed;

        if (!TemplateTokenSyntax.TryGetShortCode(trimmed, out var code)
            || !placeholderSet.Contains(code))
            return null;

        var entry = placeholderSet.Allowed.First(e =>
            string.Equals(e.ShortCode, code, StringComparison.OrdinalIgnoreCase));
        return entry.BuildWordToken(
            entry.Scope == UserReportPlaceholderScope.Row
                ? UserReportPlaceholderScope.Row
                : UserReportPlaceholderScope.Header);
    }

    private sealed class AmbiguousYellowDto
    {
        public List<AmbiguousYellowMarkDto>? Marks { get; set; }

        public string? Rationale { get; set; }
    }

    private sealed class AmbiguousYellowMarkDto
    {
        public string? FieldId { get; set; }

        public string? ProposedToken { get; set; }

        public string? Confidence { get; set; }

        public string? Scope { get; set; }

        public List<AmbiguousYellowCandidateDto>? Candidates { get; set; }
    }

    private sealed class AmbiguousYellowCandidateDto
    {
        public string? ShortCode { get; set; }

        public string? Token { get; set; }

        public int? ScorePercent { get; set; }

        public string? Reason { get; set; }
    }

    private const string FieldPlanSystemPrompt =
        """
        You detect YELLOW HIGHLIGHTER spans on scanned ministry letters and map only those spans to allowed merge placeholders.
        Rules:
        1. Count every distinct yellow highlighter region in yellowHighlightCount (0 if none).
        2. Emit fields ONLY for yellow-highlighted text. Never map non-highlighted company names, addressees, signatories, or boilerplate.
        3. When one yellow region contains MULTIPLE values, emit MULTIPLE fields (split them). Examples:
           - "№ 4/-434" + "28.04.2026 ý." → AFNUM and ADAT
           - "18 (on sekiz)" → TPCNT and TPCTX
           - "6 (alty) aý köp gezeklik" → VPER and VCAT
           - "Adaty tertipde!" → Urgency_NameTm
        4. Use only tokens from allowedTokens (see ShortCode, token, LabelEn, example). Never invent placeholders.
        5. If a yellow snippet still has no library match after splitting, leave proposedToken null and add a gap.
        6. Normalized bounding boxes 0..1 for EACH yellow snippet (left, top, right, bottom) tightly around that yellow ink only — never full-page, never covering non-yellow text.
        7. Reply with JSON only matching the schema in the user message.
        """;

    private static string ExtractJsonObject(string content)
    {
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        if (start < 0 || end <= start)
            return content;

        return content[start..(end + 1)];
    }

    private static string TrimForError(string body) =>
        body.Length <= 400 ? body : body[..400] + "…";

    private static string Truncate(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
            return text;

        return text[..maxChars] + "…";
    }

    private sealed class LayoutDto
    {
        public List<LayoutBlockDto>? Blocks { get; set; }

        public string? Rationale { get; set; }
    }

    private sealed class LayoutBlockDto
    {
        public string? Kind { get; set; }

        public string? Text { get; set; }

        public string? Token { get; set; }

        public string? Align { get; set; }

        public string? RightText { get; set; }

        public string? RightAlign { get; set; }

        public string? Style { get; set; }

        public string? RightStyle { get; set; }

        public string? Left { get; set; }

        public string? Right { get; set; }

        public string? LeftAlign { get; set; }

        public string? LeftStyle { get; set; }
    }
    private sealed class FieldPlanDto
    {
        public int YellowHighlightCount { get; set; }

        public List<FieldDto>? Fields { get; set; }

        public List<GapDto>? Gaps { get; set; }

        public string? Rationale { get; set; }
    }

    private sealed class FieldDto
    {
        public string? FieldId { get; set; }

        public int PageIndex { get; set; }

        public string? LabelText { get; set; }

        public string? ProposedToken { get; set; }

        public string? Confidence { get; set; }

        public string? Scope { get; set; }

        public BoxDto? Box { get; set; }
    }

    private sealed class BoxDto
    {
        public double Left { get; set; }

        public double Top { get; set; }

        public double Right { get; set; }

        public double Bottom { get; set; }
    }

    private sealed class GapDto
    {
        public string? FieldId { get; set; }

        public string? LabelText { get; set; }

        public string? SuggestedPropertyName { get; set; }
    }

    private sealed class ClarificationDto
    {
        public bool Accepted { get; set; }

        public string? ReplyText { get; set; }
    }
}
