using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ChatPlus.Tests
{
    internal static class Program
    {
        static int _passes;
        static int _failures;

        static void Check(bool ok, string what)
        {
            if (ok)
            {
                _passes++;
                return;
            }
            _failures++;
            Console.WriteLine("FAIL " + what);
        }

        static void Equal(string expected, string actual, string what)
        {
            Check(expected == actual, what + "\n     expected: " + Show(expected) + "\n     actual:   " + Show(actual));
        }

        static string Show(string s) => s == null ? "(null)" : "\"" + s.Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";

        static int Main()
        {
            TextUtilTests();
            HistoryFormatTests();
            ChatFormatTests();
            SentHistoryTests();
            PresetTests();
            ChatHistoryTests();
            Console.WriteLine($"{_passes} passed, {_failures} failed");
            return _failures == 0 ? 0 : 1;
        }

        static ChatEntry Entry(string time, bool local, ChatKind kind, string name, string message, string line, string steam = "")
        {
            return new ChatEntry
            {
                Time = DateTime.ParseExact(time, "yyyy-MM-dd HH:mm:ss", null),
                IsLocal = local,
                Kind = kind,
                SteamId = steam,
                Name = name,
                Message = message,
                Line = line
            };
        }

        static string GameLine(string color, string name, string message) => "<color=#" + color + "ff>" + name + ":</color> " + message;

        // ---------------- TextUtil ----------------
        static void TextUtilTests()
        {
            bool Mention(string message, string name, params string[] keywords) => TextUtil.IsMention(message, name, keywords.ToList());

            Check(Mention("hey Mark, look", "Mark"), "name in sentence");
            Check(!Mention("hey Markus", "Mark"), "name inside longer word");
            Check(Mention("@mark hi", "<color=#f00>Mark</color>"), "tags in name, @prefix, case");
            Check(Mention("привет, Марк!", "Марк"), "cyrillic exact");
            Check(Mention("ПРИВЕТ МАРК", "Марк"), "cyrillic upper case");
            Check(!Mention("Маркиз пришёл", "Марк"), "cyrillic longer word");
            Check(!Mention("hello M there", "M"), "1-char name ignored");
            Check(Mention("ping me mk", "Mark", "mk"), "keyword");
            Check(!Mention("", "Mark"), "empty message");
            Check(!TextUtil.IsMention("hi", null, null), "null name and keywords");
            Equal("a|b|c", string.Join("|", TextUtil.ParseKeywords("a, b;; c,\nA , ")), "ParseKeywords dedupe/trim");
            Equal("Ann <3", TextUtil.StripTags("<color=#fff>Ann</color> <3"), "StripTags keeps <3");
            Equal("a < b > c", TextUtil.StripTags("a < b > c"), "StripTags keeps comparisons");
            Equal("<bob> and <3 >", TextUtil.StripTags("<bob> and <3 >"), "StripTags keeps unknown tags");
            Equal("red bold it link x", TextUtil.StripTags("<#FF0000>red</color> <B>bold</b> <i>it</i> <link=\"id\">link</link> <sprite name=\"heart\">x"), "StripTags removes TMP tags");
            Equal("wide", TextUtil.StripTags("<font-weight=700><cspace=1em>wide</cspace></font-weight>"), "StripTags hyphenated tags");
            Equal("hi", TextUtil.StripTags("<mark=#FFD25A40><size=80%>hi</size></mark><br>"), "StripTags mark/size/br");
            Equal("a b c", TextUtil.Plain("a\nb\tc"), "Plain removes line breaks");
            Equal("Bob hi", TextUtil.Plain("<b>Bob</b> hi"), "Plain strips tags");
        }

        // ---------------- HistoryFormat ----------------
        static void HistoryFormatTests()
        {
            string[] tricky =
            {
                "", "plain", "tab\there", "line\nbreak", "cr\rlf\r\n", "back\\slash", "\\t not a tab", "trailing\\",
                "<color=#E7806Dff>Ник:</color> привет 👋", "a\\\tb", "\t\t", "\\\\n"
            };
            foreach (string s in tricky)
                Equal(s, HistoryFormat.Unescape(HistoryFormat.Escape(s)), "escape round trip " + Show(s));
            Check(!HistoryFormat.Escape("a\tb\nc\rd").Contains('\t') && !HistoryFormat.Escape("a\tb\nc\rd").Contains('\n'), "escaped text has no tabs or line breaks");
            Equal("keep \\x as is", HistoryFormat.Unescape("keep \\x as is"), "unknown escape kept");

            ChatEntry original = Entry("2026-10-03 14:05:09", true, ChatKind.Player, "<b>Bob\t</b>", "hi\nthere \\o/", GameLine("E7806D", "<b>Bob\t</b>", "hi\nthere \\o/"), "76561198000000001");
            string record = HistoryFormat.ToRecord(original);
            Check(record.Split('\t').Length == 7, "record has 7 columns: " + Show(record));
            Check(record.StartsWith("2026-10-03T14:05:09\tL\tP\t76561198000000001\t", StringComparison.Ordinal), "record prefix: " + Show(record));
            Check(HistoryFormat.TryParse(record, out ChatEntry parsed), "parse own record");
            Check(parsed != null && parsed.Time == original.Time && parsed.IsLocal && parsed.Kind == ChatKind.Player &&
                  parsed.SteamId == original.SteamId && parsed.Name == original.Name && parsed.Message == original.Message &&
                  parsed.Line == original.Line, "record round trip");

            foreach (ChatKind kind in new[] { ChatKind.Own, ChatKind.Notification })
            {
                ChatEntry e = Entry("2026-01-31 23:59:59", false, kind, "", "x", "x");
                Check(HistoryFormat.TryParse(HistoryFormat.ToRecord(e), out ChatEntry p) && p.Kind == kind && !p.IsLocal, "kind round trip " + kind);
            }
            Check(HistoryFormat.TryParse(record + "\textra\tcolumns", out _), "extra columns are ignored");
            Check(!HistoryFormat.TryParse(HistoryFormat.Header, out _), "header is not a record");
            Check(!HistoryFormat.TryParse("", out _), "empty line");
            Check(!HistoryFormat.TryParse("2026-10-03T14:05:09\tG\tP\tid\tname\tmsg", out _), "6 columns");
            Check(!HistoryFormat.TryParse("2026-13-03T14:05:09\tG\tP\tid\tname\tmsg\tline", out _), "bad date");
            Check(!HistoryFormat.TryParse("2026-10-03T14:05:09\tX\tP\tid\tname\tmsg\tline", out _), "bad channel");
            Check(!HistoryFormat.TryParse("2026-10-03T14:05:09\tG\tZ\tid\tname\tmsg\tline", out _), "bad kind");

            Equal("[2026-10-03 14:05:09] [Local] Bob: hi there \\o/", HistoryFormat.ToReadable(original), "readable player line");
            Equal("[2026-10-03 14:05:09] [Info] Anna joined", HistoryFormat.ToReadable(
                Entry("2026-10-03 14:05:09", false, ChatKind.Notification, "", "<color=#fff>Anna</color> joined", "...")), "readable notification");
            Equal("[2026-10-03 14:05:09] [Global] Me: yo", HistoryFormat.ToReadable(
                Entry("2026-10-03 14:05:09", false, ChatKind.Own, "Me", "yo", "...")), "readable own line");
        }

        // ---------------- ChatFormat ----------------
        static void ChatFormatTests()
        {
            DateTime now = new DateTime(2026, 10, 3, 18, 0, 0);
            ChatEntry player = Entry("2026-10-03 14:05:09", false, ChatKind.Player, "Bob", "hey Mark", GameLine("E7806D", "Bob", "hey Mark"), "1");
            ChatEntry note = Entry("2026-10-03 14:05:09", false, ChatKind.Notification, "", "Anna joined", "Anna joined");
            ChatEntry own = Entry("2026-10-03 14:05:09", false, ChatKind.Own, "Mark", "hi Mark", GameLine("E7806D", "Mark", "hi Mark"), "2");

            var off = new LineStyle { Timestamps = false };
            Equal(player.Line, ChatFormat.Compose(player, off, now), "no decoration");
            Equal(player.Line, ChatFormat.Compose(player, null, now), "null style");

            var plain = new LineStyle { Timestamps = true, TimeFormat = "HH:mm", TimeSize = 100 };
            Equal("14:05 " + player.Line, ChatFormat.Compose(player, plain, now), "plain time");

            var styled = new LineStyle { Timestamps = true, TimeFormat = "HH:mm:ss", TimeColor = "#F5EDE1A6", TimeSize = 85 };
            Equal("<color=#F5EDE1A6><size=85%>14:05:09</size></color> " + player.Line, ChatFormat.Compose(player, styled, now), "styled time");

            var noNotes = new LineStyle { Timestamps = true, TimeOnNotifications = false, TimeSize = 100 };
            Equal("Anna joined", ChatFormat.Compose(note, noNotes, now), "notifications without time");
            Equal("14:05 Anna joined", ChatFormat.Compose(note, plain, now), "notifications with time");

            Equal("2:05 PM ", ChatFormat.Stamp(player.Time, now, new LineStyle { TimeFormat = "h:mm tt", TimeSize = 100 }), "12-hour format");
            Equal("14:05 ", ChatFormat.Stamp(player.Time, now, new LineStyle { TimeFormat = "", TimeSize = 100 }), "empty format = HH:mm");
            Equal("14:05 ", ChatFormat.Stamp(player.Time, now, new LineStyle { TimeFormat = "%", TimeSize = 100 }), "invalid format falls back");
            Equal("<noparse><14:05></noparse> ", ChatFormat.Stamp(player.Time, now, new LineStyle { TimeFormat = "<HH:mm>", TimeSize = 100 }), "angle brackets are not tags");
            Equal("02.10 14:05 ", ChatFormat.Stamp(new DateTime(2026, 10, 2, 14, 5, 0), now, new LineStyle { TimeFormat = "HH:mm", DateFormat = "dd.MM", TimeSize = 100 }), "another day shows the date");
            Equal("10/02 14:05 ", ChatFormat.Stamp(new DateTime(2026, 10, 2, 14, 5, 0), now, new LineStyle { TimeFormat = "HH:mm", DateFormat = "MM/dd", TimeSize = 100 }), "date format en");

            var highlight = new LineStyle { Timestamps = false, Highlight = true, HighlightColor = "#F2C46D45", MyName = "<b>Mark</b>", Keywords = new List<string>() };
            Equal("<mark=#F2C46D45>" + player.Line + "</mark>", ChatFormat.Compose(player, highlight, now), "mention highlighted");
            Equal(own.Line, ChatFormat.Compose(own, highlight, now), "own message not highlighted");
            ChatEntry other = Entry("2026-10-03 14:05:09", false, ChatKind.Player, "Mark", "hello all", GameLine("E7806D", "Mark", "hello all"), "3");
            Equal(other.Line, ChatFormat.Compose(other, highlight, now), "sender's name does not count as a mention");
            var keyword = new LineStyle { Timestamps = false, Highlight = true, HighlightColor = "#F2C46D45", MyName = "", Keywords = new List<string> { "all" } };
            Equal("<mark=#F2C46D45>" + other.Line + "</mark>", ChatFormat.Compose(other, keyword, now), "keyword highlighted");
            var both = new LineStyle { Timestamps = true, TimeFormat = "HH:mm", TimeSize = 100, Highlight = true, HighlightColor = "#F2C46D45", MyName = "Mark" };
            Equal("14:05 <mark=#F2C46D45>" + player.Line + "</mark>", ChatFormat.Compose(player, both, now), "time outside the highlight");
            var noColor = new LineStyle { Timestamps = false, Highlight = true, HighlightColor = null, MyName = "Mark" };
            Equal(player.Line, ChatFormat.Compose(player, noColor, now), "no highlight without a color");

            Equal("hey Mark", ChatFormat.CopyText(player), "copy text");
            Equal("Anna joined", ChatFormat.CopyText(Entry("2026-10-03 14:05:09", false, ChatKind.Notification, "", " <b>Anna</b> joined ", "")), "copy notification text");

            string Color(string value) => ChatFormat.TryParseColor(value, out string c) ? c : "(invalid)";
            Equal("#AABBCC", Color("abc"), "#RGB without #");
            Equal("#AABBCCDD", Color("#abcd"), "#RGBA");
            Equal("#F5EDE1", Color(" #f5ede1 "), "#RRGGBB trimmed");
            Equal("#F5EDE1A6", Color("#F5EDE1A6"), "#RRGGBBAA");
            Equal("(invalid)", Color("#12345"), "5 digits");
            Equal("(invalid)", Color("#GGGGGG"), "not hex");
            Equal("(invalid)", Color(""), "empty");
            Equal("(invalid)", Color(null), "null");
            Equal("#F2C46D45", ChatFormat.WithAlpha("#F2C46D", 0x45), "WithAlpha adds");
            Equal("#F2C46D99", ChatFormat.WithAlpha("#F2C46DFF", 0x99), "WithAlpha replaces");
        }

        // ---------------- SentHistory ----------------
        static void SentHistoryTests()
        {
            var sent = new SentHistory(3);
            Check(sent.Older("") == null, "nothing sent yet");
            sent.Add("one");
            sent.Add("two");
            sent.Add("two");
            sent.Add("   ");
            sent.Add("");
            sent.Add("three");
            sent.Add("four");
            Check(sent.Count == 3, "capacity and duplicates: " + sent.Count);
            Check(sent.Older("typed") == null, "does not replace typed text");
            Equal("four", sent.Older(""), "up 1");
            Equal("three", sent.Older("four"), "up 2");
            Equal("two", sent.Older("three"), "up 3");
            Check(sent.Older("two") == null, "stays at the oldest");
            Equal("three", sent.Newer("two"), "down 1");
            Equal("four", sent.Newer("three"), "down 2");
            Equal("", sent.Newer("four"), "down past the newest empties the field");
            Check(sent.Newer("") == null, "down when not browsing");
            Equal("four", sent.Older(""), "browse again");
            Check(sent.Older("four edited") == null, "editing stops browsing");
            Check(sent.Newer("four edited") == null, "down after editing");
            Equal("four", sent.Older(""), "browse from empty again");
            sent.Add("five");
            Equal("five", sent.Older(""), "sending resets browsing");
        }

        // ---------------- Presets ----------------
        static void PresetTests()
        {
            Check(Presets.IndexOf(Presets.TimeColors, "#f5ede1a6", true) == 0, "color index case-insensitive");
            Check(Presets.IndexOf(Presets.TimeColors, "fff", true) == Presets.TimeColors.Length - 1, "short color matches #FFFFFF");
            Check(Presets.IndexOf(Presets.TimeColors, "#123456", true) == -1, "custom color");
            Equal("#B9AFA8", Presets.Step(Presets.TimeColors, Presets.DefaultTimeColor, 1, true), "next color");
            Equal("#FFFFFF", Presets.Step(Presets.TimeColors, Presets.DefaultTimeColor, -1, true), "previous color wraps");
            Equal(Presets.TimeFormats[0], Presets.Step(Presets.TimeFormats, "yyyy", 1, false), "custom format, next = first");
            Equal(Presets.TimeFormats[Presets.TimeFormats.Length - 1], Presets.Step(Presets.TimeFormats, "yyyy", -1, false), "custom format, previous = last");
            Equal("HH:mm:ss", Presets.Step(Presets.TimeFormats, "HH:mm", 1, false), "next format");
            Check(Presets.TimeColors.Length == 7 && Presets.HighlightColors.Length == 7, "color lists match the 7 names");
        }

        // ---------------- ChatHistory ----------------
        static void ChatHistoryTests()
        {
            string folder = Path.Combine(Path.GetTempPath(), "chatplus-tests-" + Guid.NewGuid().ToString("N"));
            var warnings = new List<string>();
            try
            {
                string file = Path.Combine(folder, ChatHistory.FileName);
                string previous = Path.Combine(folder, ChatHistory.PreviousFileName);

                // A new session: nothing on disk yet.
                var history = new ChatHistory(folder, 5, warnings.Add);
                history.Open(false, true);
                Check(history.Count == 0 && !File.Exists(file), "fresh start");
                ChatEntry a = Entry("2026-10-03 10:00:00", false, ChatKind.Player, "Bob", "hi\tthere", GameLine("E7806D", "Bob", "hi\tthere"), "1");
                ChatEntry b = Entry("2026-10-03 10:01:00", true, ChatKind.Own, "Me", "yo", GameLine("8796BF", "Me", "yo"), "2");
                history.Add(a);
                history.Add(b);
                byte[] bytes = File.ReadAllBytes(file);
                Check(bytes.Length > 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "file starts with a UTF-8 BOM");
                Check(Encoding.UTF8.GetString(bytes).IndexOf('\uFEFF', 1) < 0, "only one BOM");
                string[] lines = File.ReadAllLines(file, Encoding.UTF8);
                Check(lines.Length == 3 && lines[0] == HistoryFormat.Header, "header + 2 records: " + lines.Length);

                // Next game start without KeepAfterRestart: the file is moved aside.
                var next = new ChatHistory(folder, 5, warnings.Add);
                next.Open(false, true);
                Check(next.Count == 0 && !File.Exists(file) && File.Exists(previous), "old session archived");
                Check(File.ReadAllLines(previous, Encoding.UTF8).Length == 3, "archive has the old records");

                // Archive again: the older archive is replaced.
                next.Add(a);
                var third = new ChatHistory(folder, 5, warnings.Add);
                third.Open(false, true);
                Check(File.ReadAllLines(previous, Encoding.UTF8).Length == 2, "archive replaced by the newer session");

                // KeepAfterRestart: the file is read back, broken lines dropped, capacity applied.
                var lines2 = new List<string> { HistoryFormat.Header };
                for (int i = 0; i < 7; i++)
                    lines2.Add(HistoryFormat.ToRecord(Entry("2026-10-03 11:00:0" + i, false, ChatKind.Player, "P" + i, "m" + i, "line" + i, "s" + i)));
                lines2.Insert(3, "garbage line");
                File.WriteAllLines(file, lines2, new UTF8Encoding(true));
                var kept = new ChatHistory(folder, 5, warnings.Add);
                kept.Open(true, true);
                List<ChatEntry> snapshot = kept.Snapshot();
                Check(snapshot.Count == 5 && snapshot[0].Name == "P2" && snapshot[4].Name == "P6", "loaded the newest 5: " + string.Join(",", snapshot.Select(e => e.Name)));
                Check(File.ReadAllLines(file, Encoding.UTF8).Length == 6, "file rewritten to header + 5");
                kept.Add(a);
                Check(kept.Count == 5 && kept.Snapshot()[4].Message == "hi\tthere", "capacity keeps the newest");
                Check(File.ReadAllLines(file, Encoding.UTF8).Length == 7, "appended after load");

                // Saving off: nothing written; turning it on writes the whole memory.
                var quiet = new ChatHistory(folder, 5, warnings.Add);
                quiet.Open(false, false);
                quiet.Add(a);
                Check(!File.Exists(file), "not saving: no file");
                quiet.SetSaving(true);
                Check(File.Exists(file) && File.ReadAllLines(file, Encoding.UTF8).Length == 2, "turning saving on writes what was said");
                quiet.Add(b);
                Check(File.ReadAllLines(file, Encoding.UTF8).Length == 3, "then appends");
                quiet.Clear();
                Check(quiet.Count == 0 && File.ReadAllLines(file, Encoding.UTF8).Length == 1, "clear leaves the header");

                // Daily readable logs.
                quiet.DailyLogs = true;
                quiet.Add(a);
                quiet.Add(Entry("2026-10-04 00:00:01", false, ChatKind.Notification, "", "<b>Anna</b> joined", "x"));
                string log1 = Path.Combine(quiet.LogFolder, "2026-10-03.txt");
                string log2 = Path.Combine(quiet.LogFolder, "2026-10-04.txt");
                Check(File.Exists(log1) && File.Exists(log2), "one log per day");
                Equal("[2026-10-03 10:00:00] [Global] Bob: hi there", File.ReadAllLines(log1, Encoding.UTF8).Single(), "log line");
                Equal("[2026-10-04 00:00:01] [Info] Anna joined", File.ReadAllLines(log2, Encoding.UTF8).Single(), "log notification");

                // Locked file: a warning, once, and no exception.
                warnings.Clear();
                int before = quiet.Count;
                using (new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    quiet.Add(a);
                    quiet.Add(b);
                }
                Check(warnings.Count == 1, "locked file warns once: " + warnings.Count);
                Check(quiet.Count == before + 2, "memory still updated while the file is locked");
                quiet.Add(a);
                Check(File.ReadAllLines(file, Encoding.UTF8).Length == 1 + before + 3, "after the lock the whole history is written again, lines from the lock included");
                quiet.Add(b);
                Check(File.ReadAllLines(file, Encoding.UTF8).Length == 1 + before + 4, "then appends again");
            }
            finally
            {
                try
                {
                    Directory.Delete(folder, true);
                }
                catch (Exception)
                {
                    // Leave it to the temp folder cleanup.
                }
            }
        }
    }
}
