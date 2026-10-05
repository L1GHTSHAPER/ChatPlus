using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ChatPlus
{
    /// <summary>How chat lines are decorated. A snapshot of the settings; plain .NET, no Unity types.</summary>
    internal sealed class LineStyle
    {
        public bool Timestamps = true;
        public string TimeFormat = ChatFormat.DefaultTimeFormat;
        /// <summary>Normalized "#RRGGBB" or "#RRGGBBAA"; null for the text's own color.</summary>
        public string TimeColor;
        public int TimeSize = 100;
        public bool TimeOnNotifications = true;
        /// <summary>Put in front of the time for lines from another day.</summary>
        public string DateFormat = "dd.MM";
        public bool Highlight;
        /// <summary>Normalized "#RRGGBBAA"; null disables highlighting.</summary>
        public string HighlightColor;
        public string MyName = string.Empty;
        public IList<string> Keywords = new List<string>();
    }

    /// <summary>Builds the text of a chat line from its history entry. Plain .NET, no Unity types.</summary>
    internal static class ChatFormat
    {
        public const string DefaultTimeFormat = "HH:mm";
        static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        /// <summary>The line as the game showed it, with the time in front and mentions highlighted.</summary>
        public static string Compose(ChatEntry entry, LineStyle style, DateTime now)
        {
            string line = entry.Line ?? string.Empty;
            if (style == null)
                return line;
            if (style.Highlight && style.HighlightColor != null && entry.Kind == ChatKind.Player &&
                TextUtil.IsMention(entry.Message, style.MyName, style.Keywords))
                line = "<mark=" + style.HighlightColor + ">" + line + "</mark>";
            if (style.Timestamps && (entry.Kind != ChatKind.Notification || style.TimeOnNotifications))
                line = Stamp(entry.Time, now, style) + line;
            return line;
        }

        /// <summary>Rich text for the time of a line, followed by a space.</summary>
        public static string Stamp(DateTime time, DateTime now, LineStyle style)
        {
            string text = FormatTime(time, style.TimeFormat);
            if (time.Date != now.Date)
                text = FormatDate(time, style.DateFormat) + " " + text;
            bool colored = !string.IsNullOrEmpty(style.TimeColor);
            bool sized = style.TimeSize > 0 && style.TimeSize != 100;
            bool literal = text.IndexOf('<') >= 0 || text.IndexOf('>') >= 0;
            var stamp = new StringBuilder(text.Length + 48);
            if (colored)
                stamp.Append("<color=").Append(style.TimeColor).Append('>');
            if (sized)
                stamp.Append("<size=").Append(style.TimeSize.ToString(Invariant)).Append("%>");
            if (literal)
                stamp.Append("<noparse>");
            stamp.Append(text);
            if (literal)
                stamp.Append("</noparse>");
            if (sized)
                stamp.Append("</size>");
            if (colored)
                stamp.Append("</color>");
            return stamp.Append(' ').ToString();
        }

        /// <summary>The time in a .NET custom format such as HH:mm; an invalid format falls back to HH:mm.</summary>
        public static string FormatTime(DateTime time, string format)
        {
            if (string.IsNullOrEmpty(format))
                format = DefaultTimeFormat;
            try
            {
                return time.ToString(format, Invariant);
            }
            catch (FormatException)
            {
                return time.ToString(DefaultTimeFormat, Invariant);
            }
        }

        static string FormatDate(DateTime time, string format)
        {
            try
            {
                return time.ToString(string.IsNullOrEmpty(format) ? "dd.MM" : format, Invariant);
            }
            catch (FormatException)
            {
                return time.ToString("dd.MM", Invariant);
            }
        }

        /// <summary>What a right click copies: the message text without tags (for a notification, its text).</summary>
        public static string CopyText(ChatEntry entry)
        {
            return TextUtil.StripTags(entry.Message).Trim();
        }

        /// <summary>
        /// Accepts #RGB, #RGBA, #RRGGBB or #RRGGBBAA, with or without '#', and returns "#RRGGBB" or "#RRGGBBAA".
        /// </summary>
        public static bool TryParseColor(string value, out string normalized)
        {
            normalized = null;
            if (string.IsNullOrEmpty(value))
                return false;
            string hex = value.Trim();
            if (hex.StartsWith("#", StringComparison.Ordinal))
                hex = hex.Substring(1);
            if (hex.Length != 3 && hex.Length != 4 && hex.Length != 6 && hex.Length != 8)
                return false;
            foreach (char c in hex)
            {
                if (!Uri.IsHexDigit(c))
                    return false;
            }
            if (hex.Length <= 4)
            {
                var full = new StringBuilder(hex.Length * 2);
                foreach (char c in hex)
                    full.Append(c).Append(c);
                hex = full.ToString();
            }
            normalized = "#" + hex.ToUpperInvariant();
            return true;
        }

        /// <summary>A color with its alpha replaced: "#RRGGBB" + alpha, for highlight colors that must be see-through.</summary>
        public static string WithAlpha(string normalized, byte alpha)
        {
            if (string.IsNullOrEmpty(normalized) || normalized.Length < 7)
                return normalized;
            return normalized.Substring(0, 7) + alpha.ToString("X2", Invariant);
        }
    }
}
