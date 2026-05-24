using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

/// <summary>Legacy importer for multi-row <c>Company</c>. Prefer <see cref="OrganizationSingletonImporter"/> and the <c>Company</c> Excel sheet (→ <c>CompanyProfile</c>).</summary>
[Obsolete("Use OrganizationSingletonImporter / CompanyProfile OData entity. See docs/ORGANIZATION_SINGLETON_REFACTOR_PLAN.md.")]
public class CompanyImporter : BaseImporter<Company>
{
    public CompanyImporter(ApiClient api) : base(api, "Company")
    {
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public Task ListAllAsync()
    {
        return base.ListAllAsync(item => 
            Console.WriteLine($"  [{item.Id}] {item.Name} (Prefix: {item.AppNumberPrefix})"));
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<Company?> CreateOneAsync(
        string name,
        string? code,
        string address,
        string phoneNumber,
        string email,
        string taxInfo,
        string appNumberPrefix,
        int appNumberPadding,
        bool isDefault)
    {
        Console.WriteLine($"=== POST {EntityName}: {name} ===");

        var payload = new
        {
            Name = name,
            Code = code,
            Address = address,
            PhoneNumber = phoneNumber,
            Email = email,
            TaxInformation = taxInfo,
            AppNumberPrefix = appNumberPrefix,
            ApplicationNumberPadding = appNumberPadding,
            IsDefault = isDefault
        };

        try
        {
            var created = await Api.CreateAsync<Company>(EntityName, payload);
            Console.WriteLine($"  Created Company ID: {created?.Id}");
            Console.WriteLine();
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating company: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<Company> records)
    {
        await BulkImportLoopAsync(records, record => new
        {
            Name = record.Name,
            Code = record.Code,
            Address = record.Address,
            PhoneNumber = record.PhoneNumber,
            Email = record.Email,
            TaxInformation = record.TaxInformation,
            AppNumberPrefix = record.AppNumberPrefix,
            ApplicationNumberPadding = record.ApplicationNumberPadding,
            IsDefault = record.IsDefault
        });
    }
}