#nullable enable

using System.Drawing;
using System.Drawing.Imaging;

using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Snaps Review overlay boxes to yellow-highlighter regions detected on the page PNG.
/// Vision often returns correct tokens with wrong/coarse boxes.
/// </summary>
public static class ScanFieldBoxLocalizer
{
    public static ScanFieldPlan Apply(ScanFieldPlan plan, IReadOnlyList<ScanPageImage> pages)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(pages);
        if (plan.Fields.Count == 0 || pages.Count == 0)
            return plan;

        var byPage = pages.ToDictionary(static p => p.PageIndex);
        var fields = new List<ScanDetectedField>(plan.Fields.Count);

        foreach (var group in plan.Fields.GroupBy(static f => f.PageIndex))
        {
            if (!byPage.TryGetValue(group.Key, out var page) || page.PngBytes.Length < 100)
            {
                fields.AddRange(group);
                continue;
            }

            var yellows = ScanYellowRegionDetector.Detect(page.PngBytes);
            if (yellows.Count == 0)
            {
                fields.AddRange(group);
                continue;
            }

            fields.AddRange(AssignBoxes(group.ToList(), yellows));
        }

        return new ScanFieldPlan
        {
            PlaceholderSet = plan.PlaceholderSet,
            ScanKind = plan.ScanKind,
            Fields = fields,
            StaticRegions = plan.StaticRegions,
            Gaps = plan.Gaps,
            PendingQuestions = plan.PendingQuestions,
            Rationale = AppendTag(plan.Rationale, "box-localize"),
            Source = plan.Source,
            YellowHighlightCount = plan.YellowHighlightCount,
        };
    }

    private static IReadOnlyList<ScanDetectedField> AssignBoxes(
        IReadOnlyList<ScanDetectedField> fields,
        IReadOnlyList<ScanBoundingBox> yellows)
    {
        var orderedYellows = yellows
            .OrderBy(static y => y.Top)
            .ThenBy(static y => y.Left)
            .ToList();

        if (orderedYellows.Count == 0)
            return fields.ToList();

        // Match each field to the best yellow using the (possibly shifted) AI box as a soft prior.
        // Do NOT zip by document order alone — that parks tokens on ghost blobs between paragraphs.
        var assignments = new Dictionary<int, List<ScanDetectedField>>(); // yellow index -> fields
        var usedFields = new HashSet<string>(StringComparer.Ordinal);

        var pairs = new List<(double Score, int FieldIndex, int YellowIndex)>();
        for (var fi = 0; fi < fields.Count; fi++)
        {
            for (var yi = 0; yi < orderedYellows.Count; yi++)
                pairs.Add((Score(fields[fi].Box, orderedYellows[yi]), fi, yi));
        }

        foreach (var pair in pairs.OrderByDescending(static p => p.Score))
        {
            if (pair.Score < 0.02)
                break;
            var field = fields[pair.FieldIndex];
            if (!usedFields.Add(field.FieldId))
                continue;

            if (!assignments.TryGetValue(pair.YellowIndex, out var list))
            {
                list = new List<ScanDetectedField>();
                assignments[pair.YellowIndex] = list;
            }

            // Prefer one field per yellow; allow sharing when AI boxes clearly sit on the same blob.
            if (list.Count > 0 && pair.Score < 0.12 && !BoxesOverlapHorizontally(field.Box, list[0].Box))
                continue;

            list.Add(field);
        }

        // Leftover fields: place on nearest unused yellow, else subdivide the nearest occupied yellow.
        foreach (var field in fields.OrderBy(DocumentOrder))
        {
            if (usedFields.Contains(field.FieldId))
                continue;

            var unused = Enumerable.Range(0, orderedYellows.Count)
                .Where(i => !assignments.ContainsKey(i))
                .Select(i => (Index: i, Score: Score(field.Box, orderedYellows[i])))
                .OrderByDescending(static t => t.Score)
                .ToList();

            if (unused.Count > 0 && unused[0].Score > -0.5)
            {
                assignments[unused[0].Index] = new List<ScanDetectedField> { field };
                usedFields.Add(field.FieldId);
                continue;
            }

            var nearest = Enumerable.Range(0, orderedYellows.Count)
                .Select(i => (Index: i, Score: Score(field.Box, orderedYellows[i])))
                .OrderByDescending(static t => t.Score)
                .First();
            if (!assignments.TryGetValue(nearest.Index, out var share))
            {
                share = new List<ScanDetectedField>();
                assignments[nearest.Index] = share;
            }

            share.Add(field);
            usedFields.Add(field.FieldId);
        }

        var result = new List<ScanDetectedField>(fields.Count);
        foreach (var (yi, group) in assignments.OrderBy(static kv => kv.Key))
        {
            var box = orderedYellows[yi].Clamp();
            var ordered = group
                .OrderBy(DocumentOrder)
                .ThenBy(static f => f.LabelText, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (ordered.Count == 1)
            {
                result.Add(WithBox(ordered[0], SplitTallBoxForSingle(ordered[0], box)));
                continue;
            }

            // Multiple tokens on one yellow blob: subdivide (vertical for tall header bands, else horizontal).
            var width = Math.Max(1e-6, box.Right - box.Left);
            var height = Math.Max(1e-6, box.Bottom - box.Top);
            var vertical = height >= width * 1.15 && ordered.Count <= 3;
            for (var n = 0; n < ordered.Count; n++)
            {
                ScanBoundingBox slice;
                if (vertical)
                {
                    var top = box.Top + height * n / ordered.Count;
                    var bottom = box.Top + height * (n + 1) / ordered.Count;
                    slice = new ScanBoundingBox(box.Left, top, box.Right, bottom);
                }
                else
                {
                    var left = box.Left + width * n / ordered.Count;
                    var right = box.Left + width * (n + 1) / ordered.Count;
                    slice = new ScanBoundingBox(left, box.Top, right, box.Bottom);
                }

                result.Add(WithBox(ordered[n], slice));
            }
        }

        // Preserve any field we somehow missed (should not happen).
        foreach (var field in fields)
        {
            if (result.All(r => !string.Equals(r.FieldId, field.FieldId, StringComparison.Ordinal)))
                result.Add(field);
        }

        return result;
    }

    /// <summary>AFNUM+ADAT often share one tall yellow; single-token fields keep the full box.</summary>
    private static ScanBoundingBox SplitTallBoxForSingle(ScanDetectedField field, ScanBoundingBox box)
    {
        if (!TemplateTokenSyntax.TryGetShortCode(field.ProposedToken ?? string.Empty, out var code))
            return box;

        var height = box.Bottom - box.Top;
        var width = box.Right - box.Left;
        if (height < width * 1.2)
            return box;

        return code.ToUpperInvariant() switch
        {
            "AFNUM" => new ScanBoundingBox(box.Left, box.Top, box.Right, box.Top + height * 0.5),
            "ADAT" => new ScanBoundingBox(box.Left, box.Top + height * 0.5, box.Right, box.Bottom),
            _ => box,
        };
    }

    private static double Score(ScanBoundingBox ai, ScanBoundingBox yellow)
    {
        ai = ai.Clamp();
        yellow = yellow.Clamp();
        var iou = IoU(ai, yellow);
        var hOverlap = HorizontalOverlapRatio(ai, yellow);
        var dx = Math.Abs(MidX(ai) - MidX(yellow));
        // Vision boxes are often shifted UP into whitespace. Prefer yellow ink that shares
        // the column (horizontal) and sits at/below the AI box — not ghost blobs at the AI Y.
        var belowBias = MidY(yellow) >= MidY(ai) - 0.01
            ? MidY(yellow) * 0.9
            : -0.35;
        var area = Math.Max(0, (yellow.Right - yellow.Left) * (yellow.Bottom - yellow.Top));
        var areaBoost = Math.Min(1.2, area * 60.0); // prefer real highlighter spans over speckles
        return iou * 3.0 + hOverlap * 3.0 - dx * 2.0 + belowBias + areaBoost;
    }

    private static double HorizontalOverlapRatio(ScanBoundingBox a, ScanBoundingBox b)
    {
        a = a.Clamp();
        b = b.Clamp();
        var left = Math.Max(a.Left, b.Left);
        var right = Math.Min(a.Right, b.Right);
        if (right <= left)
            return 0;
        var denom = Math.Max(1e-9, Math.Min(a.Right - a.Left, b.Right - b.Left));
        return (right - left) / denom;
    }

    private static bool BoxesOverlapHorizontally(ScanBoundingBox a, ScanBoundingBox b) =>
        HorizontalOverlapRatio(a, b) >= 0.15;

    private static double CenterDistance(ScanBoundingBox a, ScanBoundingBox b)
    {
        var dx = MidX(a) - MidX(b);
        var dy = MidY(a) - MidY(b);
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double MidX(ScanBoundingBox b) => (b.Clamp().Left + b.Clamp().Right) * 0.5;
    private static double MidY(ScanBoundingBox b) => (b.Clamp().Top + b.Clamp().Bottom) * 0.5;

    private static int DocumentOrder(ScanDetectedField field)
    {
        if (TemplateTokenSyntax.TryGetShortCode(field.ProposedToken ?? string.Empty, out var code))
        {
            return code.ToUpperInvariant() switch
            {
                "AFNUM" => 10,
                "ADAT" => 20,
                "URGENCY_NAMETM" => 30,
                "TPCNT" => 40,
                "TPCTX" => 50,
                "VPER" => 60,
                "VCAT" => 70,
                _ => 500,
            };
        }

        var label = field.LabelText ?? string.Empty;
        if (label.Contains('№') || label.Contains('/'))
            return 10;
        if (label.Contains("tertipde", StringComparison.OrdinalIgnoreCase))
            return 30;
        return 400;
    }

    private static ScanDetectedField WithBox(ScanDetectedField field, ScanBoundingBox box) =>
        new()
        {
            FieldId = field.FieldId,
            Box = box.Clamp(),
            PageIndex = field.PageIndex,
            LabelText = field.LabelText,
            ProposedToken = field.ProposedToken,
            Confidence = field.Confidence,
            Scope = field.Scope,
        };

    private static double IoU(ScanBoundingBox a, ScanBoundingBox b)
    {
        a = a.Clamp();
        b = b.Clamp();
        var left = Math.Max(a.Left, b.Left);
        var top = Math.Max(a.Top, b.Top);
        var right = Math.Min(a.Right, b.Right);
        var bottom = Math.Min(a.Bottom, b.Bottom);
        if (right <= left || bottom <= top)
            return 0;

        var inter = (right - left) * (bottom - top);
        var areaA = Math.Max(1e-9, (a.Right - a.Left) * (a.Bottom - a.Top));
        var areaB = Math.Max(1e-9, (b.Right - b.Left) * (b.Bottom - b.Top));
        return inter / (areaA + areaB - inter);
    }

    private static string AppendTag(string? rationale, string tag)
    {
        if (string.IsNullOrWhiteSpace(rationale))
            return tag;
        if (rationale.Contains(tag, StringComparison.OrdinalIgnoreCase))
            return rationale;
        return rationale.Trim() + ";" + tag;
    }
}

/// <summary>Finds yellow highlighter blobs on a page PNG as normalized bounding boxes.</summary>
public static class ScanYellowRegionDetector
{
    public static IReadOnlyList<ScanBoundingBox> Detect(byte[] pngBytes)
    {
        ArgumentNullException.ThrowIfNull(pngBytes);
        if (pngBytes.Length < 24)
            return Array.Empty<ScanBoundingBox>();

        try
        {
            using var input = new MemoryStream(pngBytes, writable: false);
            using var source = new Bitmap(input);
            return DetectBitmap(source);
        }
        catch
        {
            return Array.Empty<ScanBoundingBox>();
        }
    }

    internal static IReadOnlyList<ScanBoundingBox> DetectBitmap(Bitmap source)
    {
        Bitmap work = source;
        Bitmap? scaled = null;
        try
        {
            const int maxWidth = 900;
            if (source.Width > maxWidth)
            {
                var scale = (double)maxWidth / source.Width;
                var w = maxWidth;
                var h = Math.Max(1, (int)Math.Round(source.Height * scale));
                scaled = new Bitmap(w, h);
                using (var g = Graphics.FromImage(scaled))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                    g.DrawImage(source, 0, 0, w, h);
                }

                work = scaled;
            }

            var width = work.Width;
            var height = work.Height;
            var mask = new bool[width * height];

            // Dense enough sample for thin highlighter strokes (avoid step-grid ghost islands).
            var step = Math.Max(1, Math.Min(width, height) / 700);
            for (var y = 0; y < height; y += step)
            {
                for (var x = 0; x < width; x += step)
                {
                    if (IsHighlighterYellow(work.GetPixel(x, y)))
                        mask[y * width + x] = true;
                }
            }

            var dilateRadius = Math.Max(1, step);
            mask = Dilate(mask, width, height, radius: dilateRadius);

            var boxes = ConnectedComponents(mask, width, height, MinBlobFor(step));
            var result = new List<ScanBoundingBox>();
            foreach (var b in boxes)
            {
                var density = YellowDensity(work, b, step);
                if (density < 0.12)
                    continue;

                var nb = new ScanBoundingBox(
                    b.Left / (double)width,
                    b.Top / (double)height,
                    b.Right / (double)width,
                    b.Bottom / (double)height).Clamp();
                var nw = nb.Right - nb.Left;
                var nh = nb.Bottom - nb.Top;
                // Reject speckles / dilated noise islands (ghost boxes between paragraphs).
                if (nw * nh < 0.0008 || nw < 0.012 || nh < 0.006)
                    continue;

                result.Add(nb);
            }

            return result
                .OrderBy(static b => b.Top)
                .ThenBy(static b => b.Left)
                .ToList();
        }
        finally
        {
            scaled?.Dispose();
        }
    }

    private static double YellowDensity(Bitmap bmp, (int Left, int Top, int Right, int Bottom) box, int step)
    {
        var total = 0;
        var yellow = 0;
        var s = Math.Max(1, step);
        for (var y = box.Top; y < box.Bottom; y += s)
        {
            for (var x = box.Left; x < box.Right; x += s)
            {
                total++;
                if (IsHighlighterYellow(bmp.GetPixel(x, y)))
                    yellow++;
            }
        }

        return total == 0 ? 0 : yellow / (double)total;
    }

    private static int MinBlobFor(int step) => Math.Max(10, 36 / Math.Max(1, step * step));

    internal static bool IsHighlighterYellow(Color c)
    {
        if (c.A < 180)
            return false;

        // Reject near-white / paper.
        if (c.R > 248 && c.G > 248 && c.B > 230)
            return false;

        // RGB gate: warm yellow / lime highlighter (incl. pale #FFF59D).
        if (c.R >= 165 && c.G >= 145 && c.B <= 210
            && (c.R - c.B) >= 28 && (c.G - c.B) >= 18
            && (c.R + c.G) > c.B * 2 + 40)
            return true;

        // HSV: hue ~35–75°, decent saturation, not too dark.
        var max = Math.Max(c.R, Math.Max(c.G, c.B));
        var min = Math.Min(c.R, Math.Min(c.G, c.B));
        if (max < 140)
            return false;
        var delta = max - min;
        if (delta < 25)
            return false;
        var sat = delta / (double)max;
        if (sat < 0.12)
            return false;

        double hue;
        if (max == c.R)
            hue = 60.0 * (((c.G - c.B) / (double)delta) % 6);
        else if (max == c.G)
            hue = 60.0 * (((c.B - c.R) / (double)delta) + 2);
        else
            hue = 60.0 * (((c.R - c.G) / (double)delta) + 4);
        if (hue < 0)
            hue += 360;

        return hue is >= 32 and <= 78;
    }

    private static bool[] Dilate(bool[] mask, int width, int height, int radius)
    {
        if (radius <= 0)
            return mask;

        var next = new bool[mask.Length];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (!mask[y * width + x])
                    continue;

                for (var dy = -radius; dy <= radius; dy++)
                {
                    var yy = y + dy;
                    if (yy < 0 || yy >= height)
                        continue;
                    for (var dx = -radius; dx <= radius; dx++)
                    {
                        var xx = x + dx;
                        if (xx < 0 || xx >= width)
                            continue;
                        next[yy * width + xx] = true;
                    }
                }
            }
        }

        return next;
    }

    private static List<(int Left, int Top, int Right, int Bottom)> ConnectedComponents(
        bool[] mask,
        int width,
        int height,
        int minPixels)
    {
        var visited = new bool[mask.Length];
        var boxes = new List<(int Left, int Top, int Right, int Bottom)>();
        var queue = new Queue<int>();

        for (var i = 0; i < mask.Length; i++)
        {
            if (!mask[i] || visited[i])
                continue;

            queue.Enqueue(i);
            visited[i] = true;
            var count = 0;
            var minX = width;
            var minY = height;
            var maxX = 0;
            var maxY = 0;

            while (queue.Count > 0)
            {
                var idx = queue.Dequeue();
                count++;
                var x = idx % width;
                var y = idx / width;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;

                TryEnqueue(x + 1, y);
                TryEnqueue(x - 1, y);
                TryEnqueue(x, y + 1);
                TryEnqueue(x, y - 1);
            }

            if (count >= minPixels)
                boxes.Add((minX, minY, maxX + 1, maxY + 1));

            void TryEnqueue(int xx, int yy)
            {
                if (xx < 0 || yy < 0 || xx >= width || yy >= height)
                    return;
                var ni = yy * width + xx;
                if (!mask[ni] || visited[ni])
                    return;
                visited[ni] = true;
                queue.Enqueue(ni);
            }
        }

        return MergeNearby(boxes, gap: 6);
    }

    private static List<(int Left, int Top, int Right, int Bottom)> MergeNearby(
        List<(int Left, int Top, int Right, int Bottom)> boxes,
        int gap)
    {
        var list = boxes.OrderBy(static b => b.Top).ThenBy(static b => b.Left).ToList();
        var changed = true;
        while (changed)
        {
            changed = false;
            for (var i = 0; i < list.Count; i++)
            {
                for (var j = i + 1; j < list.Count; j++)
                {
                    var a = list[i];
                    var b = list[j];
                    if (!Near(a, b, gap))
                        continue;

                    list[i] = (
                        Math.Min(a.Left, b.Left),
                        Math.Min(a.Top, b.Top),
                        Math.Max(a.Right, b.Right),
                        Math.Max(a.Bottom, b.Bottom));
                    list.RemoveAt(j);
                    changed = true;
                    break;
                }

                if (changed)
                    break;
            }
        }

        return list;
    }

    private static bool Near(
        (int Left, int Top, int Right, int Bottom) a,
        (int Left, int Top, int Right, int Bottom) b,
        int gap)
    {
        // Same line only — never glue a ghost above the paragraph to body yellow below.
        var aHeight = Math.Max(1, a.Bottom - a.Top);
        var bHeight = Math.Max(1, b.Bottom - b.Top);
        var aMid = (a.Top + a.Bottom) / 2.0;
        var bMid = (b.Top + b.Bottom) / 2.0;
        var maxLineSlack = Math.Max(gap * 2, Math.Max(aHeight, bHeight) * 0.7);
        if (Math.Abs(aMid - bMid) > maxLineSlack)
            return false;

        return !(a.Right + gap < b.Left
                 || b.Right + gap < a.Left
                 || a.Bottom + gap < b.Top
                 || b.Bottom + gap < a.Top);
    }
}