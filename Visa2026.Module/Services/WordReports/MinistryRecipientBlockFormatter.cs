using System;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// Layout helpers for <c>Ministry.RecipientBlock</c> in Word letters (two-line stepped addressee).
    /// </summary>
    internal static class MinistryRecipientBlockFormatter
    {
        /// <summary>
        /// Splits the ministry addressee into two right-aligned lines when a known pattern matches
        /// (e.g. <c>… elektroenergetika korporasiýasynyň başlygy …</c>) or when the value already
        /// contains a newline. Otherwise returns a single line and <paramref name="line2"/> is <c>null</c>.
        /// </summary>
        public static (string Line1, string? Line2) SplitIntoAddressLines(string? recipientBlock)
        {
            if (string.IsNullOrWhiteSpace(recipientBlock))
                return (string.Empty, null);

            var s = recipientBlock.Trim();

            var nl = s.IndexOfAny(new[] { '\r', '\n' });
            if (nl >= 0)
            {
                var a = s[..nl].Trim();
                var b = s[(nl + 1)..].Trim();
                while (b.Length > 0 && (b[0] == '\r' || b[0] == '\n'))
                    b = b[1..].TrimStart();
                if (a.Length > 0 && b.Length > 0)
                    return (a, b);
            }

            const string korporaMarker = " korporasiýasynyň";
            var i = s.IndexOf(korporaMarker, StringComparison.Ordinal);
            if (i > 0)
            {
                var line1 = s[..i].TrimEnd();
                var line2 = s[(i + 1)..].TrimStart();
                if (line1.Length > 0 && line2.Length > 0)
                    return (line1, line2);
            }

            return (s, null);
        }
    }
}
