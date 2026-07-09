using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Blazor.Server.Services.Migration;
using Visa2026.DataImporter.Migration;
using Visa2026.Module.Services.MigrationImport;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Writes legacy import rows through in-process XAF ObjectSpace (headless host).
/// </summary>
internal interface IVisa2014ImportTarget
{
    Task<Guid?> CreateAsync(Type entityType, IReadOnlyDictionary<string, object?> payload);

    Task UpdateAsync(Type entityType, Guid id, IReadOnlyDictionary<string, object?> payload);

    Task SoftDeleteAsync(Type entityType, Guid id);

    Task FlushAsync();
}

internal sealed class Visa2014ODataImportTarget : IVisa2014ImportTarget
{
    private readonly ApiClient _api;

    public Visa2014ODataImportTarget(ApiClient api) => _api = api;

    public async Task<Guid?> CreateAsync(Type entityType, IReadOnlyDictionary<string, object?> payload)
    {
        var entityName = entityType.Name;
        return entityName switch
        {
            nameof(Application) => (await _api.CreateAsync<Application>(entityName, payload))?.Id,
            nameof(ApplicationItem) => (await _api.CreateAsync<ApplicationItem>(entityName, payload))?.Id,
            nameof(ApplicationProgress) => (await _api.CreateAsync<ApplicationProgress>(entityName, payload))?.Id,
            nameof(Person) => (await _api.CreateAsync<Person>(entityName, payload))?.Id,
            nameof(Passport) => (await _api.CreateAsync<Passport>(entityName, payload))?.Id,
            nameof(Visa) => (await _api.CreateAsync<Visa>(entityName, payload))?.Id,
            nameof(Education) => (await _api.CreateAsync<Education>(entityName, payload))?.Id,
            nameof(EmployeePositionHistory) => (await _api.CreateAsync<EmployeePositionHistory>(entityName, payload))?.Id,
            nameof(EmployeeSalary) => (await _api.CreateAsync<EmployeeSalary>(entityName, payload))?.Id,
            nameof(AddressOfResidence) => (await _api.CreateAsync<AddressOfResidence>(entityName, payload))?.Id,
            nameof(ActualPosition) => (await _api.CreateAsync<ActualPosition>(entityName, payload))?.Id,
            _ => throw new NotSupportedException($"OData import target does not support {entityName}."),
        };
    }

    public async Task UpdateAsync(Type entityType, Guid id, IReadOnlyDictionary<string, object?> payload)
    {
        var entityName = entityType.Name;
        await _api.UpdateAsync(entityName, id, payload);
    }

    public Task SoftDeleteAsync(Type entityType, Guid id) =>
        throw new NotSupportedException("Soft-delete sync requires --inprocess.");

    public Task FlushAsync() => Task.CompletedTask;
}

internal sealed class Visa2014DryRunImportTarget : IVisa2014ImportTarget
{
    public Task<Guid?> CreateAsync(Type entityType, IReadOnlyDictionary<string, object?> payload) =>
        Task.FromResult<Guid?>(Guid.NewGuid());

    public Task UpdateAsync(Type entityType, Guid id, IReadOnlyDictionary<string, object?> payload) =>
        Task.CompletedTask;

    public Task SoftDeleteAsync(Type entityType, Guid id) =>
        Task.CompletedTask;

    public Task FlushAsync() => Task.CompletedTask;
}

internal sealed class Visa2014ObjectSpaceImportTarget : IVisa2014ImportTarget, IDisposable
{
    private readonly INonSecuredObjectSpaceFactory _factory;
    private readonly int _batchSize;
    private readonly Dictionary<string, BatchState> _batches = new(StringComparer.Ordinal);

    public Visa2014ObjectSpaceImportTarget(INonSecuredObjectSpaceFactory factory, int batchSize = 50)
    {
        _factory = factory;
        _batchSize = Math.Max(1, batchSize);
    }

    public Task<Guid?> CreateAsync(Type entityType, IReadOnlyDictionary<string, object?> payload)
    {
        var key = entityType.FullName ?? entityType.Name;
        if (!_batches.TryGetValue(key, out var batch))
        {
            var objectSpace = _factory.CreateNonSecuredObjectSpace(entityType);
            MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);
            batch = new BatchState(objectSpace);
            _batches[key] = batch;
        }

        var entity = batch.ObjectSpace.CreateObject(entityType);
        Migration.ObjectSpaceImportSink.ApplyPayload(batch.ObjectSpace, entity, payload);
        batch.Pending++;

        if (batch.Pending >= _batchSize)
            CommitBatch(batch);

        return Task.FromResult<Guid?>(((BaseObject)entity).ID);
    }

    public Task UpdateAsync(Type entityType, Guid id, IReadOnlyDictionary<string, object?> payload)
    {
        var key = entityType.FullName ?? entityType.Name;
        if (!_batches.TryGetValue(key, out var batch))
        {
            var objectSpace = _factory.CreateNonSecuredObjectSpace(entityType);
            MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);
            batch = new BatchState(objectSpace);
            _batches[key] = batch;
        }

        var entity = batch.ObjectSpace.GetObjectByKey(entityType, id);
        if (entity == null)
            throw new InvalidOperationException($"Update target {entityType.Name}({id}) not found.");

        Migration.ObjectSpaceImportSink.ApplyPayload(batch.ObjectSpace, entity, payload);
        batch.Pending++;
        if (batch.Pending >= _batchSize)
            CommitBatch(batch);
        return Task.CompletedTask;
    }

    public Task SoftDeleteAsync(Type entityType, Guid id)
    {
        var key = entityType.FullName ?? entityType.Name;
        if (!_batches.TryGetValue(key, out var batch))
        {
            var objectSpace = _factory.CreateNonSecuredObjectSpace(entityType);
            MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);
            batch = new BatchState(objectSpace);
            _batches[key] = batch;
        }

        var entity = batch.ObjectSpace.GetObjectByKey(entityType, id);
        if (entity == null)
            throw new InvalidOperationException($"Soft-delete target {entityType.Name}({id}) not found.");

        batch.ObjectSpace.Delete(entity);
        batch.Pending++;
        if (batch.Pending >= _batchSize)
            CommitBatch(batch);
        return Task.CompletedTask;
    }

    public Task FlushAsync()
    {
        foreach (var batch in _batches.Values)
        {
            if (batch.Pending > 0)
                CommitBatch(batch);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        foreach (var batch in _batches.Values)
            batch.ObjectSpace.Dispose();
        _batches.Clear();
    }

    private static void CommitBatch(BatchState batch)
    {
        batch.ObjectSpace.CommitChanges();
        batch.Pending = 0;
    }

    private sealed class BatchState(IObjectSpace objectSpace)
    {
        public IObjectSpace ObjectSpace { get; } = objectSpace;
        public int Pending { get; set; }
    }
}
