using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class InvitationImporter
{
    private readonly ApiClient _api;
    private const string Entity = "Invitation";

    public InvitationImporter(ApiClient api)
    {
        _api = api;
    }

    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<Invitation>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var appNum = item.ApplicationProfileInstance?.ApplicationNumber ?? "No App";
            Console.WriteLine($"  [{item.Id}] Inv#: {item.InvitationNumber} (App: {appNum}) - Issued: {item.IssuedDate:d}");
        }
        Console.WriteLine();
    }

    public async Task<Invitation?> CreateOneAsync(
        string invitationNumber,
        DateTime issuedDate,
        DateTime expirationDate,
        Guid? applicationId,
        Guid visaCategoryId,
        Guid visaPeriodId)
    {
        Console.WriteLine($"=== POST {Entity}: {invitationNumber} ===");

        var payload = new Dictionary<string, object?>
        {
            ["InvitationNumber"] = invitationNumber,
            ["IssuedDate"] = issuedDate,
            ["ExpirationDate"] = expirationDate,
            ["VisaCategory"] = new { ID = visaCategoryId },
            ["VisaPeriod"] = new { ID = visaPeriodId },
            ["IsVisaStartAndEndDateDefined"] = false,
        };
        if (applicationId.HasValue)
            payload["Application"] = new { ID = applicationId.Value };

        try
        {
            var created = await _api.CreateAsync<Invitation>(Entity, payload);
            Console.WriteLine($"  Created Invitation ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating Invitation: {ex.Message}");
            return null;
        }
    }

    public async Task BulkImportAsync(IEnumerable<Invitation> records)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["InvitationNumber"] = record.InvitationNumber,
                    ["IssuedDate"] = record.IssuedDate,
                    ["ExpirationDate"] = record.ExpirationDate,
                    ["IsVisaStartAndEndDateDefined"] = record.IsVisaStartAndEndDateDefined,
                    ["VisaStartDate"] = record.VisaStartDate,
                    ["VisaEndDate"] = record.VisaEndDate,
                    ["Application"] = record.ApplicationProfileInstance != null ? new { ID = record.ApplicationProfileInstance.Id } : null,
                    ["VisaCategory"] = record.VisaCategory != null ? new { ID = record.VisaCategory.Id } : null,
                    ["VisaPeriod"] = record.VisaPeriod != null ? new { ID = record.VisaPeriod.Id } : null,
                };

                await _api.CreateAsync<Invitation>(Entity, payload);
                Console.WriteLine($"  ✓ Imported Invitation: {record.InvitationNumber}");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed '{record.InvitationNumber}': {ex.Message}");
                fail++;
            }
        }

        Console.WriteLine($"  Done. Success={success}, Failed={fail}\n");
    }

    public async Task DeleteAsync(Guid id)
    {
        await _api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted Invitation {id}\n");
    }
}