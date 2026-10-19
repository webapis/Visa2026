namespace Visa2026.Module.Services.PersonList;

/// <summary>
/// Splits a list's visible width across columns in proportion to their content.
/// Icon columns keep a small fixed width. The remaining width goes to the other
/// columns by how wide their text is, scaled so the total never exceeds the screen.
/// </summary>
public static class PersonListColumnShare
{
    public const int FlexPreferredCap = 420;
    public const int CellChrome = 12;
    public const int IconMin = 40;
    public const int IconMax = 56;
    public const int FlexFloor = 28;

    /// <summary>Narrowest a text column may be, so a header word stays on one line.</summary>
    public const int FlexMin = 76;

    /// <summary>Columns no wider than this keep their content width. Spare pane width goes to wider columns.</summary>
    public const int ShortColumnMax = 120;

    public readonly record struct Measure(string Field, int Preferred, bool Fixed);

    public readonly record struct Width(string Field, int Pixels);

    public static IReadOnlyList<Width> SharePixels(IReadOnlyList<Measure> columns, int available)
    {
        if (columns == null || columns.Count == 0 || available <= 0)
            return Array.Empty<Width>();

        var prepared = new List<(string Field, int Pixels, bool Fixed)>(columns.Count);
        foreach (var column in columns)
        {
            var content = Math.Max(0, column.Preferred) + CellChrome;
            if (column.Fixed)
            {
                prepared.Add((column.Field, Math.Clamp(content, IconMin, IconMax), true));
                continue;
            }

            prepared.Add((column.Field, Math.Clamp(content, FlexMin, FlexPreferredCap), false));
        }

        var fixedSum = prepared.Where(column => column.Fixed).Sum(column => column.Pixels);
        if (fixedSum >= available || prepared.All(column => column.Fixed))
            return ScaleToAvailable(prepared.Select(column => (column.Field, (double)column.Pixels)).ToList(), available);

        var contentSum = prepared.Sum(column => column.Pixels);
        if (contentSum > available)
            return ShrinkToAvailable(prepared, available);

        var surplus = available - contentSum;
        var growers = prepared.Where(column => !column.Fixed && column.Pixels > ShortColumnMax).ToList();
        if (growers.Count == 0)
        {
            var widest = prepared.Where(column => !column.Fixed).MaxBy(column => column.Pixels);
            if (widest.Field != null)
                growers.Add(widest);
        }

        var weightSum = growers.Sum(column => column.Pixels);
        if (weightSum <= 0)
            weightSum = 1;

        var exact = new List<(string Field, double Pixels)>(prepared.Count);
        foreach (var column in prepared)
        {
            var extra = growers.Any(grower => string.Equals(grower.Field, column.Field, StringComparison.Ordinal))
                ? surplus * (column.Pixels / (double)weightSum)
                : 0;
            exact.Add((column.Field, column.Pixels + extra));
        }

        return LargestRemainder(exact, available);
    }

    /// <summary>
    /// Caps some columns and raises others to a minimum. Pixels move between columns so the total stays the same.
    /// </summary>
    public static IReadOnlyList<Width> Rebalance(
        IReadOnlyList<Width> widths,
        IReadOnlyList<(string Field, int Maximum)> caps,
        IReadOnlyList<(string Field, int Minimum)> floors,
        int donorFloor)
    {
        if (widths == null || widths.Count == 0)
            return widths ?? Array.Empty<Width>();

        var pixels = new int[widths.Count];
        for (var i = 0; i < widths.Count; i++)
            pixels[i] = widths[i].Pixels;

        int IndexOf(string field)
        {
            for (var i = 0; i < widths.Count; i++)
            {
                if (string.Equals(widths[i].Field, field, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        var reserved = new HashSet<int>();
        foreach (var floor in floors)
        {
            var index = IndexOf(floor.Field);
            if (index >= 0)
                reserved.Add(index);
        }

        var freed = 0;
        foreach (var cap in caps)
        {
            var index = IndexOf(cap.Field);
            if (index < 0 || pixels[index] <= cap.Maximum)
                continue;

            freed += pixels[index] - cap.Maximum;
            pixels[index] = cap.Maximum;
            reserved.Add(index);
        }

        freed = PourIntoFloors(pixels, floors, IndexOf, freed);
        if (freed > 0)
        {
            var target = -1;
            foreach (var floor in floors)
            {
                target = IndexOf(floor.Field);
                if (target >= 0)
                    break;
            }

            if (target < 0 && caps.Count > 0)
                target = IndexOf(caps[0].Field);
            if (target >= 0)
                pixels[target] += freed;
        }

        while (true)
        {
            var needy = -1;
            var need = 0;
            foreach (var floor in floors)
            {
                var index = IndexOf(floor.Field);
                if (index < 0)
                    continue;
                var shortfall = floor.Minimum - pixels[index];
                if (shortfall > need)
                {
                    need = shortfall;
                    needy = index;
                }
            }

            if (needy < 0)
                break;

            var donor = -1;
            var room = 0;
            for (var i = 0; i < pixels.Length; i++)
            {
                if (reserved.Contains(i))
                    continue;
                var spare = pixels[i] - donorFloor;
                if (spare > room)
                {
                    room = spare;
                    donor = i;
                }
            }

            if (donor < 0 || room <= 0)
                break;

            var move = Math.Min(need, room);
            pixels[donor] -= move;
            pixels[needy] += move;
        }

        var result = new Width[widths.Count];
        for (var i = 0; i < widths.Count; i++)
            result[i] = new Width(widths[i].Field, pixels[i]);
        return result;
    }

    private static int PourIntoFloors(
        int[] pixels,
        IReadOnlyList<(string Field, int Minimum)> floors,
        Func<string, int> indexOf,
        int amount)
    {
        while (amount > 0)
        {
            var best = -1;
            var bestNeed = 0;
            foreach (var floor in floors)
            {
                var index = indexOf(floor.Field);
                if (index < 0)
                    continue;
                var need = floor.Minimum - pixels[index];
                if (need > bestNeed)
                {
                    bestNeed = need;
                    best = index;
                }
            }

            if (best < 0)
                return amount;

            var give = Math.Min(amount, bestNeed);
            pixels[best] += give;
            amount -= give;
        }

        return 0;
    }

    /// <summary>
    /// Short columns keep the width of their text. Only the wider columns shrink so the row still fits.
    /// </summary>
    private static IReadOnlyList<Width> ShrinkToAvailable(
        List<(string Field, int Pixels, bool Fixed)> prepared,
        int available)
    {
        var lockedSum = prepared
            .Where(column => column.Fixed || column.Pixels <= ShortColumnMax)
            .Sum(column => column.Pixels);
        var growers = prepared.Where(column => !column.Fixed && column.Pixels > ShortColumnMax).ToList();
        var growerSum = growers.Sum(column => column.Pixels);
        var room = available - lockedSum;
        if (growers.Count == 0 || growerSum <= 0 || room < growers.Count)
            return ScaleToAvailable(prepared.Select(column => (column.Field, (double)column.Pixels)).ToList(), available);

        var exact = new List<(string Field, double Pixels)>(prepared.Count);
        foreach (var column in prepared)
        {
            var pixels = column.Fixed || column.Pixels <= ShortColumnMax
                ? column.Pixels
                : column.Pixels / (double)growerSum * room;
            exact.Add((column.Field, pixels));
        }

        return LargestRemainder(exact, available);
    }

    private static IReadOnlyList<Width> ScaleToAvailable(
        List<(string Field, double Pixels)> exact,
        int available)
    {
        var sum = exact.Sum(column => column.Pixels);
        if (sum <= 0)
            sum = exact.Count;

        var scaled = exact
            .Select(column => (column.Field, column.Pixels / sum * available))
            .ToList();
        return LargestRemainder(scaled, available);
    }

    private static IReadOnlyList<Width> LargestRemainder(
        List<(string Field, double Pixels)> exact,
        int available)
    {
        var floors = exact.Select(column => (column.Field, Floor: (int)Math.Floor(column.Pixels), Fraction: column.Pixels - Math.Floor(column.Pixels))).ToList();
        var leftover = available - floors.Sum(column => column.Floor);
        var order = floors
            .Select((column, index) => (column.Fraction, index))
            .OrderByDescending(column => column.Fraction)
            .ThenBy(column => column.index)
            .ToList();

        var pixels = floors.Select(column => column.Floor).ToArray();
        for (var n = 0; n < leftover && n < order.Count; n++)
            pixels[order[n].index]++;

        var result = new Width[exact.Count];
        for (var i = 0; i < exact.Count; i++)
            result[i] = new Width(exact[i].Field, Math.Max(0, pixels[i]));
        return result;
    }
}
