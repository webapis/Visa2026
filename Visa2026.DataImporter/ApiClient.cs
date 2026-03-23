using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Visa2026.DataImporter;

/// <summary>
/// Reusable HTTP client that handles JWT authentication
/// and all OData CRUD operations against the Visa2026 API.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _userName;
    private readonly string _password;
    private string? _token;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public ApiClient(string baseUrl, string userName, string password)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _userName = userName;
        _password = password;

        // Disable SSL validation for localhost dev certificate.
        // Remove HttpClientHandler in production.
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        _http = new HttpClient(handler);
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // ------------------------------------------------------------------
    // Wait for server to be ready
    // ------------------------------------------------------------------

    /// <summary>
    /// Polls the server until it responds or the timeout is reached.
    /// Call this before LoginAsync() when starting alongside the server.
    /// </summary>
    public async Task WaitForServerAsync(
        int maxWaitSeconds = 60,
        int pollIntervalSeconds = 3)
    {
        Console.WriteLine($"Waiting for server at {_baseUrl} to be ready...");
        var deadline = DateTime.UtcNow.AddSeconds(maxWaitSeconds);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                // Ping the swagger endpoint — it's always available and lightweight
                var response = await _http.GetAsync($"{_baseUrl}/swagger/index.html");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Server is ready.\n");
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Server not up yet — keep waiting
            }

            Console.WriteLine($"  Server not ready yet, retrying in {pollIntervalSeconds}s...");
            await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds));
        }

        throw new TimeoutException(
            $"Server at {_baseUrl} did not become ready within {maxWaitSeconds} seconds. " +
            "Make sure Visa2026.Blazor.Server is running.");
    }

    // ------------------------------------------------------------------
    // Authentication
    // ------------------------------------------------------------------

    /// <summary>
    /// Authenticates and stores the JWT token for subsequent requests.
    /// Call this once before any CRUD operation.
    /// </summary>
    public async Task LoginAsync()
    {
        Console.WriteLine($"Authenticating as '{_userName}'...");

        var response = await _http.PostAsJsonAsync(
            $"{_baseUrl}/api/Authentication/Authenticate",
            new { userName = _userName, password = _password });

        response.EnsureSuccessStatusCode();

        _token = await response.Content.ReadAsStringAsync();
        _token = _token.Trim('"');

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);

        Console.WriteLine("Authentication successful.\n");
    }

    // ------------------------------------------------------------------
    // Generic CRUD helpers
    // ------------------------------------------------------------------

    /// <summary>GET all records from an OData entity set.</summary>
    public async Task<List<T>> GetAllAsync<T>(string entityName)
    {
        var url = $"{_baseUrl}/api/odata/{entityName}";
        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"GET {url} -> {(int)response.StatusCode} {response.ReasonPhrase}. " +
                $"Body: {(body.Length > 600 ? body[..600] + "..." : body)}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ODataListResponse<T>>(json, JsonOptions);
        return result?.Value ?? new List<T>();
    }

    /// <summary>GET a single record by its GUID key.</summary>
    public async Task<T?> GetByIdAsync<T>(string entityName, Guid id)
    {
        var response = await _http.GetAsync($"{_baseUrl}/api/odata/{entityName}({id})");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return default;
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    /// <summary>POST a new record and return the created entity.</summary>
    public async Task<T?> CreateAsync<T>(string entityName, object payload)
    {
        var url = $"{_baseUrl}/api/odata/{entityName}";
        var response = await _http.PostAsJsonAsync(url, payload);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"POST {url} -> {(int)response.StatusCode} {response.ReasonPhrase}. " +
                $"Body: {(body.Length > 600 ? body[..600] + "..." : body)}");
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    /// <summary>PATCH (partial update) an existing record by GUID.</summary>
    public async Task UpdateAsync(string entityName, Guid id, object payload)
    {
        var content = JsonContent.Create(payload);
        var request = new HttpRequestMessage(HttpMethod.Patch,
            $"{_baseUrl}/api/odata/{entityName}({id})")
        {
            Content = content
        };
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>DELETE a record by GUID.</summary>
    public async Task DeleteAsync(string entityName, Guid id)
    {
        var response = await _http.DeleteAsync(
            $"{_baseUrl}/api/odata/{entityName}({id})");
        response.EnsureSuccessStatusCode();
    }

    /// <summary>GET with OData query options ($filter, $orderby, $top, etc.)</summary>
    public async Task<List<T>> QueryAsync<T>(string entityName, string odataQuery)
    {
        var url = $"{_baseUrl}/api/odata/{entityName}?{odataQuery}";
        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            // Include the response body so callers can log the exact XAF/OData error
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"GET {url} -> {(int)response.StatusCode} {response.ReasonPhrase}. " +
                $"Body: {(body.Length > 600 ? body[..600] + "..." : body)}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ODataListResponse<T>>(json, JsonOptions);
        return result?.Value ?? new List<T>();
    }
}

/// <summary>Wrapper that matches the OData { "value": [...] } envelope.</summary>
public class ODataListResponse<T>
{
    [JsonPropertyName("value")]
    public List<T> Value { get; set; } = new();
}