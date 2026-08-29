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
    /// <summary>Minimum match score to snap a field onto a yellow blob (else keep AI box).</summary>
    private const double MinAcceptScore = 1.4;

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

        var assignments = new Dictionary<int, List<ScanDetectedField>>();
        var usedFields = new HashSet<string>(StringComparer.Ordinal);

        var pairs = new List<(double Score, int FieldIndex, int YellowIndex)>();
        for (var fi = 0; fi < fields.Count; fi++)
        {
            for (var yi = 0; yi < orderedYellows.Count; yi++)
                pairs.Add((Score(fields[fi].Box, orderedYellows[yi]), fi, yi));
        }

        foreach (var pair in pairs.OrderByDescending(static p => p.Score))
        {
            if (pair.Score < MinAcceptScore)
                break;

            var field = fields[pair.FieldIndex];
            if (!usedFields.Add(field.FieldId))
                continue;

            if (!assignments.TryGetValue(pair.YellowIndex, out var list))
            {
                list = new List<ScanDetectedField>();
                assignments[pair.YellowIndex] = list;
            }

            if (list.Count > 0 && !CanShareYellow(list[0], field))
                continue;

            list.Add(field);
        }

        // Leftovers: prefer a strong AI↔yellow score; else zip remaining fields to unused
        // highlighter spans by reading order (filters already removed letter fragments).
        foreach (var field in fields.OrderBy(DocumentOrder))
        {
            if (usedFields.Contains(field.FieldId))
                continue;

            var unused = Enumerable.Range(0, orderedYellows.Count)
                .Where(i => !assignments.ContainsKey(i))
                .Select(i => (Index: i, Score: Score(field.Box, orderedYellows[i])))
                .OrderByDescending(static t => t.Score)
                .ToList();

            if (unused.Count > 0 && unused[0].Score >= MinAcceptScore)
            {
                assignments[unused[0].Index] = new List<ScanDetectedField> { field };
                usedFields.Add(field.FieldId);
            }
        }

        var leftoverFields = fields
            .Where(f => !usedFields.Contains(f.FieldId))
            .OrderBy(DocumentOrder)
            .ToList();
        var leftoverYellows = Enumerable.Range(0, orderedYellows.Count)
            .Where(i => !assignments.ContainsKey(i))
            .OrderBy(i => orderedYellows[i].Top)
            .ThenBy(i => orderedYellows[i].Left)
            .ToList();

        var zip = Math.Min(leftoverFields.Count, leftoverYellows.Count);
        for (var i = 0; i < zip; i++)
        {
            assignments[leftoverYellows[i]] = new List<ScanDetectedField> { leftoverFields[i] };
            usedFields.Add(leftoverFields[i].FieldId);
        }

        // Extra leftover fields that share a compound yellow with an already-assigned sibling.
        foreach (var field in fields.Where(f => !usedFields.Contains(f.FieldId)).OrderBy(DocumentOrder))
        {
            var shareYi = assignments
                .Where(kv => kv.Value.Any(existing => CanShareYellow(existing, field)))
                .Select(static kv => (int?)kv.Key)
                .FirstOrDefault();
            if (shareYi is null)
                continue;
            assignments[shareYi.Value].Add(field);
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

        foreach (var field in fields)
        {
            if (result.All(r => !string.Equals(r.FieldId, field.FieldId, StringComparison.Ordinal)))
                result.Add(field); // keep original AI box
        }

        return result;
    }

    private static bool CanShareYellow(ScanDetectedField a, ScanDetectedField b)
    {
        var ca = ShortCode(a);
        var cb = ShortCode(b);
        if (ca.Length == 0 || cb.Length == 0)
            return false;

        static bool Pair(string x, string y, string p, string q) =>
            (x == p && y == q) || (x == q && y == p);

        return Pair(ca, cb, "AFNUM", "ADAT")
               || Pair(ca, cb, "TPCNT", "TPCTX")
               || Pair(ca, cb, "VPER", "VCAT");
    }

    private static string ShortCode(ScanDetectedField field)
    {
        if (!TemplateTokenSyntax.TryGetShortCode(field.ProposedToken ?? string.Empty, out var code))
            return string.Empty;
        return code.ToUpperInvariant();
    }

    private static ScanBoundingBox SplitTallBoxForSingle(ScanDetectedField field, ScanBoundingBox box)
    {
        var code = ShortCode(field);
        var height = box.Bottom - box.Top;
        var width = box.Right - box.Left;
        if (height < width * 1.2)
            return box;

        return code switch
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
        var belowBias = MidY(yellow) >= MidY(ai) - 0.01
            ? MidY(yellow) * 0.9
            : -0.35;
        var area = Math.Max(0, (yellow.Right - yellow.Left) * (yellow.Bottom - yellow.Top));
        var areaBoost = Math.Min(1.2, area * 60.0);
        // Tiny fragments (de/sa/sany) get almost no areaBoost and rarely clear MinAcceptScore.
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

    private static double MidX(ScanBoundingBox b) => (b.Clamp().Left + b.Clamp().Right) * 0.5;
    private static double MidY(ScanBoundingBox b) => (b.Clamp().Top + b.Clamp().Bottom) * 0.5;

    private static int DocumentOrder(ScanDetectedField field)
    {
        var code = ShortCode(field);
        return code switch
        {
            "AFNUM" => 10,
            "ADAT" => 20,
            "URGENCY_NAMETM" => 30,
            "TPCNT" => 40,
            "TPCTX" => 50,
            "VPER" => 60,
            "VCAT" => 70,
            _ => 400,
        };
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
            SourceRegion = field.SourceRegion,
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

            var step = Math.Max(1, Math.Min(width, height) / 700);
            for (var y = 0; y < height; y += step)
            {
                for (var x = 0; x < width; x += step)
                {
                    if (IsHighlighterYellow(work.GetPixel(x, y)))
                        mask[y * width + x] = true;
                }
            }

            // Small dilate only — larger radius merges text-edge noise into fake blobs.
            var dilateRadius = Math.Max(1, Math.Min(2, step));
            mask = Dilate(mask, width, height, radius: dilateRadius);

            var boxes = ConnectedComponents(mask, width, height, MinBlobFor(step));
            var result = new List<ScanBoundingBox>();
            foreach (var b in boxes)
            {
                var density = YellowDensity(work, b, step);
                if (density < 0.22)
                    continue;

                var nb = new ScanBoundingBox(
                    b.Left / (double)width,
                    b.Top / (double)height,
                    b.Right / (double)width,
                    b.Bottom / (double)height).Clamp();
                var nw = nb.Right - nb.Left;
                var nh = nb.Bottom - nb.Top;
                // Real highlighter spans are word-sized; reject letter fragments (de / sa / sany).
                if (nw * nh < 0.0030 || nw < 0.04 || nh < 0.009)
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

    private static int MinBlobFor(int step) => Math.Max(14, 48 / Math.Max(1, step * step));

    internal static bool IsHighlighterYellow(Color c)
    {
        if (c.A < 180)
            return false;

        // Paper / near-white.
        if (c.R > 248 && c.G > 248 && c.B > 220)
            return false;

        // Too dark = ink / anti-aliased text edges, not highlighter.
        if (c.R < 185 || c.G < 165)
            return false;

        var chroma = (c.R + c.G) / 2.0 - c.B;
        if (chroma < 40)
            return false;

        // Strong warm yellow / lime marker (incl. pale #FFF59D).
        if (c.B <= 200 && (c.R - c.B) >= 35 && (c.G - c.B) >= 22)
            return true;

        var max = Math.Max(c.R, Math.Max(c.G, c.B));
        var min = Math.Min(c.R, Math.Min(c.G, c.B));
        var delta = max - min;
        if (delta < 35)
            return false;
        var sat = delta / (double)max;
        if (sat < 0.18)
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

        return hue is >= 35 and <= 75;
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

        return MergeNearby(boxes, gap: 8);
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