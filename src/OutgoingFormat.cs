using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ChatPlus
{
    public enum MessageFont { GameDefault, LiberationSans }
    public enum MessageColorMode { Original, Solid, Gradient }

    internal sealed class OutgoingStyle
    {
        public bool Enabled;
        public MessageFont Font;
        public MessageColorMode ColorMode;
        public string Color = "#F2C46D";
        public string EndColor = "#6AA8FF";
        public bool Bold;
        public bool Italic;
    }

    /// <summary>Only standard TMP tags and fonts shipped in Resources on an unmodified client.</summary>
    internal static class OutgoingFormat
    {
        public const int WireLimit = 250;
        public const string LiberationAsset = "LiberationSans SDF";
        const int ColorOverhead = 17; // <#RRGGBB> + </color>
        const int MaxGradientBands = 8;

        public static bool IsCommand(string text) => !string.IsNullOrEmpty(text) && text.TrimStart().StartsWith("/", StringComparison.Ordinal);

        public static bool HasManualFormatting(string text) => !string.Equals(text, TextUtil.StripTags(text), StringComparison.Ordinal);

        /// <summary>Never truncate either the user's text or a tag. Return false when the wire budget cannot fit.</summary>
        public static bool TryCompose(string text, OutgoingStyle style, out string result)
        {
            result = text ?? string.Empty;
            if (style == null || !style.Enabled || result.Length == 0 || IsCommand(result))
                return true;
            // Preserve deliberately typed tags. In particular, do not split tags into gradient bands.
            if (HasManualFormatting(result))
                return result.Length <= WireLimit;

            string open = string.Empty, close = string.Empty;
            if (style.Font == MessageFont.LiberationSans)
            {
                open += "<font=\"" + LiberationAsset + "\">";
                close = "</font>" + close;
            }
            if (style.Bold) { open += "<b>"; close = "</b>" + close; }
            if (style.Italic) { open += "<i>"; close = "</i>" + close; }

            int budget = WireLimit - result.Length - open.Length - close.Length;
            if (budget < 0)
                return false;
            string body = result;
            if (style.ColorMode == MessageColorMode.Solid || style.ColorMode == MessageColorMode.Gradient)
            {
                if (!TryColor(style.Color, out string first))
                    first = "#F2C46D";
                if (style.ColorMode == MessageColorMode.Gradient)
                {
                    if (!TryColor(style.EndColor, out string last))
                        last = "#6AA8FF";
                    int[] elements = TextElements(result);
                    int bands = Math.Min(Math.Min(MaxGradientBands, elements.Length), budget / ColorOverhead);
                    int minimum = first == last || elements.Length == 1 ? 1 : 2;
                    if (bands < minimum)
                        return false;
                    if (first == last)
                        bands = 1;
                    var gradient = new StringBuilder(result.Length + bands * ColorOverhead);
                    for (int band = 0; band < bands; band++)
                    {
                        int begin = band * elements.Length / bands;
                        int end = (band + 1) * elements.Length / bands;
                        int startIndex = elements[begin];
                        int endIndex = end == elements.Length ? result.Length : elements[end];
                        gradient.Append('<').Append(Blend(first, last, band, bands)).Append('>')
                            .Append(result, startIndex, endIndex - startIndex).Append("</color>");
                    }
                    body = gradient.ToString();
                }
                else
                {
                    if (budget < ColorOverhead)
                        return false;
                    body = "<" + first + ">" + body + "</color>";
                }
            }
            result = open + body + close;
            return true;
        }

        public static bool TryColor(string value, out string hex)
        {
            if (!ChatFormat.TryParseColor(value, out hex))
                return false;
            // Outgoing colors are opaque; no invisible text or alpha-dependent preview.
            hex = hex.Substring(0, 7);
            return true;
        }

        static string Blend(string first, string last, int band, int count)
        {
            if (count == 1) return first;
            var color = new StringBuilder("#", 7);
            for (int channel = 1; channel < 7; channel += 2)
            {
                int a = int.Parse(first.Substring(channel, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int b = int.Parse(last.Substring(channel, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int value = (int)Math.Round(a + (b - a) * (double)band / (count - 1), MidpointRounding.AwayFromZero);
                color.Append(value.ToString("X2", CultureInfo.InvariantCulture));
            }
            return color.ToString();
        }

        // Older Unity Mono versions do not join emoji sequences in StringInfo. Keep joiners,
        // variation selectors, skin tones and pairs of regional indicators in one color band there too.
        static int[] TextElements(string text)
        {
            int[] starts = StringInfo.ParseCombiningCharacters(text);
            var joined = new List<int>(starts.Length);
            int regions = 0;
            for (int i = 0; i < starts.Length; i++)
            {
                int index = starts[i];
                int codepoint = char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1])
                    ? char.ConvertToUtf32(text, index) : text[index];
                bool regional = codepoint >= 0x1F1E6 && codepoint <= 0x1F1FF;
                bool continuation = index > 0 && (text[index - 1] == '\u200D' || codepoint == 0x200D ||
                    (codepoint >= 0xFE00 && codepoint <= 0xFE0F) || (codepoint >= 0x1F3FB && codepoint <= 0x1F3FF) ||
                    (regional && regions % 2 == 1));
                if (!continuation) joined.Add(index);
                int end = i + 1 == starts.Length ? text.Length : starts[i + 1];
                regions = regional ? regions + (end - index) / 2 : 0;
            }
            return joined.ToArray();
        }
    }
}
