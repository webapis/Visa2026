using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class ApplicationItemImporter
{
    private readonly ApiClient _api;
    private const string Entity = "ApplicationItem";

    public ApplicationItemImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<ApplicationItem>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var appNum = item.Application?.ApplicationNumber ?? "No App";
            var personName = item.CurrentPositionHistory?.Person?.FullName ?? "Unknown Person";
            Console.WriteLine($"  [{item.Id}] App: {appNum} | Person: {personName}");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<ApplicationItem?> CreateOneAsync(
        Guid applicationId,
        Guid? currentWorkPermitItemId = null,
        Guid? currentPositionHistoryId = null,
        Guid? currentRegistrationId = null,
        Guid? currentEmployeeContractId = null)
    {
        Console.WriteLine($"=== POST {Entity} ===");

        var payload = new
        {
            // Required link
            Application = new { ID = applicationId },

            // Optional links that exist on ApplicationItem
            CurrentWorkPermitItem = currentWorkPermitItemId.HasValue ? new { ID = currentWorkPermitItemId.Value } : null,
            CurrentPositionHistory = currentPositionHistoryId.HasValue ? new { ID = currentPositionHistoryId.Value } : null,
            CurrentRegistration = currentRegistrationId.HasValue ? new { ID = currentRegistrationId.Value } : null,
            CurrentEmployeeContract = currentEmployeeContractId.HasValue ? new { ID = currentEmployeeContractId.Value } : null,
        };

        try
        {
            var created = await _api.CreateAsync<ApplicationItem>(Entity, payload);
            Console.WriteLine($"  Created ApplicationItem ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating ApplicationItem: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<ApplicationItem> records)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                var payload = new
                {
                    Application = record.Application != null ? new { ID = record.Application.Id } : null,

                    // Optional relations that exist on ApplicationItem
                    CurrentWorkPermitItem = record.CurrentWorkPermitItem != null ? new { ID = record.CurrentWorkPermitItem.Id } : null,
                    CurrentPositionHistory = record.CurrentPositionHistory != null ? new { ID = record.CurrentPositionHistory.Id } : null,
                    CurrentRegistration = record.CurrentRegistration != null ? new { ID = record.CurrentRegistration.Id } : null,
                    CurrentEmployeeContract = record.CurrentEmployeeContract != null ? new { ID = record.CurrentEmployeeContract.Id } : null,

                    // Flags
                    InvitationItemIsIssued = record.InvitationItemIsIssued,
                    WorkPermitItemIsIssued = record.WorkPermitItemIsIssued,
                    RejectionIssued = record.RejectionIssued,
                    VisaIssued = record.VisaIssued
                };

                await _api.CreateAsync<ApplicationItem>(Entity, payload);
                Console.WriteLine($"  ✓ Imported Item for App: {record.Application?.ApplicationNumber ?? "Unknown"}");
                success++;
            }
            catch (Exception ex)
        {
                Console.WriteLine($"  ✗ Failed import: {ex.Message}");
                fail++;
            }
        }

        Console.WriteLine($"  Done. Success={success}, Failed={fail}\n");
    }

    // ------------------------------------------------------------------
    // DELETE
    // ------------------------------------------------------------------
    public async Task DeleteAsync(Guid id)
    {
        await _api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted ApplicationItem {id}\n");
    }
}