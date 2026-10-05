using System;
using System.Globalization;
using System.Text;

namespace ChatPlus
{
    /// <summary>
    /// Lines of the history file (tab-separated, one chat line per row) and of the readable daily logs.
    /// Plain .NET, no Unity types.
    /// </summary>
    internal static class HistoryFormat
    {
        public const string Header = "time\tchannel\ttype\tsteam_id\tname\tmessage\tline";
        const string RecordTime = "yyyy-MM-dd'T'HH:mm:ss";
        const string ReadableTime = "yyyy-MM-dd HH:mm:ss";
        const int Columns = 7;
        static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static string ToRecord(ChatEntry entry)
        {
            var record = new StringBuilder(64 + 2 * ((entry.Line ?? string.Empty).Length + (entry.Message ?? string.Empty).Length));
            record.Append(entry.Time.ToString(RecordTime, Invariant)).Append('\t')
                .Append(entry.IsLocal ? 'L' : 'G').Append('\t')
                .Append(KindCode(entry.Kind)).Append('\t');
            AppendEscaped(record, entry.SteamId).Append('\t');
            AppendEscaped(record, entry.Name).Append('\t');
            AppendEscaped(record, entry.Message).Append('\t');
            AppendEscaped(record, entry.Line);
            return record.ToString();
        }

        public static bool TryParse(string record, out ChatEntry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(record))
                return false;
            // Escaping keeps tabs out of the fields, so every tab is a separator. Extra columns are ignored.
            string[] parts = record.Split('\t');
            if (parts.Length < Columns)
                return false;
            if (!DateTime.TryParseExact(parts[0], RecordTime, Invariant, DateTimeStyles.None, out DateTime time))
                return false;
            bool isLocal;
            if (parts[1] == "L")
                isLocal = true;
            else if (parts[1] == "G")
                isLocal = false;
            else
                return false;
            if (!TryParseKind(parts[2], out ChatKind kind))
                return false;
            entry = new ChatEntry
            {
                Time = time,
                IsLocal = isLocal,
                Kind = kind,
                SteamId = Unescape(parts[3]),
                Name = Unescape(parts[4]),
                Message = Unescape(parts[5]),
                Line = Unescape(parts[6])
            };
            return true;
        }

        /// <summary>"[2026-10-03 14:05:09] [Global] Name: message", without rich text tags.</summary>
        public static string ToReadable(ChatEntry entry)
        {
            var line = new StringBuilder(64 + (entry.Message ?? string.Empty).Length);
            line.Append('[').Append(entry.Time.ToString(ReadableTime, Invariant)).Append("] [")
                .Append(entry.Kind == ChatKind.Notification ? "Info" : entry.IsLocal ? "Local" : "Global").Append("] ");
            if (entry.Kind != ChatKind.Notification)
                line.Append(TextUtil.Plain(entry.Name).Trim()).Append(": ");
            line.Append(TextUtil.Plain(entry.Message));
            return line.ToString();
        }

        public static string Escape(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : AppendEscaped(new StringBuilder(value.Length + 8), value).ToString();
        }

        static StringBuilder AppendEscaped(StringBuilder target, string value)
        {
            if (string.IsNullOrEmpty(value))
                return target;
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\':
                        target.Append("\\\\");
                        break;
                    case '\t':
                        target.Append("\\t");
                        break;
                    case '\n':
                        target.Append("\\n");
                        break;
                    case '\r':
                        target.Append("\\r");
                        break;
                    default:
                        target.Append(c);
                        break;
                }
            }
            return target;
        }

        public static string Unescape(string value)
        {
            if (string.IsNullOrEmpty(value) || value.IndexOf('\\') < 0)
                return value ?? string.Empty;
            var text = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c != '\\' || i == value.Length - 1)
                {
                    text.Append(c);
                    continue;
                }
                char next = value[++i];
                switch (next)
                {
                    case '\\':
                        text.Append('\\');
                        break;
                    case 't':
                        text.Append('\t');
                        break;
                    case 'n':
                        text.Append('\n');
                        break;
                    case 'r':
                        text.Append('\r');
                        break;
                    default:
                        // Not written by Escape: keep it as it is.
                        text.Append('\\').Append(next);
                        break;
                }
            }
            return text.ToString();
        }

        static char KindCode(ChatKind kind)
        {
            switch (kind)
            {
                case ChatKind.Own:
                    return 'O';
                case ChatKind.Notification:
                    return 'N';
                default:
                    return 'P';
            }
        }

        static bool TryParseKind(string code, out ChatKind kind)
        {
            switch (code)
            {
                case "P":
                    kind = ChatKind.Player;
                    return true;
                case "O":
                    kind = ChatKind.Own;
                    return true;
                case "N":
                    kind = ChatKind.Notification;
                    return true;
                default:
                    kind = ChatKind.Player;
                    return false;
            }
        }
    }
}
