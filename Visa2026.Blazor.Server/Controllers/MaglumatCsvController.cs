using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Public Maglumat CSV for Excel (Power Query From Web).
/// Auth: shared API key in query string ?key=... (not XAF/JWT).
/// SQL uses the app DefaultConnection on the server.
/// </summary>
[ApiController]
[Route("api/maglumat")]
[AllowAnonymous]
public sealed class MaglumatCsvController : ControllerBase
{
    private readonly IConfiguration configuration;
    private readonly ILogger<MaglumatCsvController> logger;

    public MaglumatCsvController(IConfiguration configuration, ILogger<MaglumatCsvController> logger)
    {
        this.configuration = configuration;
        this.logger = logger;
    }

    /// <summary>
    /// GET /api/maglumat?key=...&amp;validOnly=true
    /// </summary>
    [HttpGet]
    [Produces("text/csv")]
    public async Task<IActionResult> GetCsv(
        [FromQuery] string? key,
        [FromQuery] bool validOnly = false,
        CancellationToken cancellationToken = default)
    {
        var configuredKey = configuration["MaglumatCsvExport:ApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            logger.LogWarning("Maglumat CSV requested but MaglumatCsvExport:ApiKey is not configured.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                "Maglumat CSV export is not configured.");
        }

        if (!ApiKeyMatches(configuredKey, key))
            return Unauthorized();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("ConnectionString");
        if (string.IsNullOrWhiteSpace(connectionString))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Database is not configured.");

        const string sql = @"
SELECT
    FullName,
    BirthAndNationality,
    PassportBlock,
    EducationLevelTm,
    PositionNameTm,
    ResidenceAddress,
    PermitBlock,
    VisaBlock,
    Remarks,
    IsValid
FROM dbo.View_ForeignWorkerMaglumat
WHERE (@validOnly = 0 OR IsValid = 1)
ORDER BY FullName
";

        var sb = new StringBuilder(capacity: 64 * 1024);
        sb.AppendLine(string.Join(',',
            "FullName",
            "BirthAndNationality",
            "PassportBlock",
            "EducationLevelTm",
            "PositionNameTm",
            "ResidenceAddress",
            "PermitBlock",
            "VisaBlock",
            "Remarks",
            "IsValid"));

        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@validOnly", validOnly ? 1 : 0);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                sb.Append(Csv(reader, 0)).Append(',');
                sb.Append(Csv(reader, 1)).Append(',');
                sb.Append(Csv(reader, 2)).Append(',');
                sb.Append(Csv(reader, 3)).Append(',');
                sb.Append(Csv(reader, 4)).Append(',');
                sb.Append(Csv(reader, 5)).Append(',');
                sb.Append(Csv(reader, 6)).Append(',');
                sb.Append(Csv(reader, 7)).Append(',');
                sb.Append(Csv(reader, 8)).Append(',');
                sb.Append(reader.IsDBNull(9) ? "" : (reader.GetBoolean(9) ? "1" : "0"));
                sb.AppendLine();
            }
        }

        // UTF-8 BOM helps Excel open Turkmen text correctly.
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        var bytes = utf8.GetBytes(sb.ToString());
        var fileName = $"maglumat-{DateTime.UtcNow:yyyyMMdd}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    private static bool ApiKeyMatches(string configured, string? provided)
    {
        var a = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
        var b = SHA256.HashData(Encoding.UTF8.GetBytes(provided ?? string.Empty));
        return CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static string Csv(SqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return string.Empty;

        var value = reader.GetValue(ordinal)?.ToString() ?? string.Empty;
        if (value.Length == 0)
            return string.Empty;

        var needsQuotes = value.Contains(',') || value.Contains('"')
            || value.Contains('\r') || value.Contains('\n');
        if (!needsQuotes)
            return value;

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}