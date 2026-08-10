using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.Services.OfficerShell;

public sealed class OfficerShellPaginationState
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public sealed class OfficerShellPaginationResult<T>
{
    public IReadOnlyList<T> PageItems { get; init; } = Array.Empty<T>();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public int Total { get; init; }
    public int TotalPages { get; init; } = 1;
    public int Start { get; init; }
    public int End { get; init; }
}

public static class OfficerShellPagination
{
    public static readonly int[] PageSizeOptions = { 10, 25, 50 };

    public static OfficerShellPaginationResult<T> Paginate<T>(IReadOnlyList<T> items, OfficerShellPaginationState state)
    {
        var total = items?.Count ?? 0;
        var pageSize = state.PageSize > 0 ? state.PageSize : PageSizeOptions[1];
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        var page = Math.Min(Math.Max(1, state.Page), totalPages);
        var startIndex = total == 0 ? 0 : (page - 1) * pageSize;
        var endIndex = Math.Min(startIndex + pageSize, total);
        IReadOnlyList<T> pageItems = total == 0
            ? Array.Empty<T>()
            : items!.Skip(startIndex).Take(pageSize).ToList();

        return new OfficerShellPaginationResult<T>
        {
            PageItems = pageItems,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = totalPages,
            Start = total == 0 ? 0 : startIndex + 1,
            End = endIndex,
        };
    }
}
