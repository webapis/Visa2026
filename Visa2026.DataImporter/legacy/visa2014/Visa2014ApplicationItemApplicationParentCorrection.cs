using System.Threading.Tasks;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014ApplicationItemApplicationParentCorrection
{
    public static Task<int> RunCommandAsync(IReadOnlyList<string> args, bool verbose)
    {
        _ = args;
        _ = verbose;
        Console.Error.WriteLine("ERR --correct-application-item-application-parent is retired (Phase B: ApplicationItem removed).");
        return Task.FromResult(1);
    }
}