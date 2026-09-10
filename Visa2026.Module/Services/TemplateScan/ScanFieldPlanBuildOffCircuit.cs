#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// OpenXML yellow/token extract is synchronous. Running it on the Blazor circuit
/// freezes SignalR (reconnect banner) and drops the Review pdf.js page into the HTML outline.
/// </summary>
public static class ScanFieldPlanBuildOffCircuit
{
    public static Task<ScanFieldPlan> RunAsync(
        IScanFieldPlanService service,
        ScanFieldPlanBuildRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(request);
        return Task.Run(
            () => service.BuildAsync(request, cancellationToken),
            cancellationToken);
    }
}