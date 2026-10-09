using System;
using System.Linq;
using System.Text;

namespace ChatPlus.Tests
{
    internal static class NicknameFormatTests
    {
        internal static void Run(Action<bool, string> check)
        {
            var bytes = Encoding.Unicode.GetBytes("<b>Марк</b>");
            var style = new OutgoingStyle();
            check(ReferenceEquals(bytes, NicknameFormat.Apply(bytes, style)), "disabled nickname styling preserves original bytes and manual tags");
            check(NicknameFormat.Apply(null, style) == null, "null nickname payload is preserved");
            style.Enabled = true;
            style.ColorMode = MessageColorMode.Solid;
            style.Color = "#123456";
            var wire = NicknameFormat.Apply(bytes, style);
            string name = Encoding.Unicode.GetString(wire);
            check(name == "<#123456>Марк</color>", "nickname style replaces prior tags and scopes color to the name");
            check(Encoding.Unicode.GetString(bytes) == "<b>Марк</b>", "nickname styling never mutates source name bytes");
            check(name + ": Hello" == "<#123456>Марк</color>: Hello", "nickname style closes before separator and message");
            check(ReferenceEquals(wire, NicknameFormat.Apply(wire, style)), "reapplying nickname style does not nest or grow tags");
            style.Bold = true; style.Italic = true; style.Font = MessageFont.LiberationSans;
            name = NicknameFormat.Compose("Nick", style);
            check(name.StartsWith("<font=\"LiberationSans SDF\"><b><i><#123456>") && name.EndsWith("</color></i></b></font>"),
                "nickname font, bold, italic and color wrappers close in reverse order");
            check(TextUtil.StripTags(name) == "Nick", "nickname font styling preserves visible identity");
            check(NicknameFormat.Compose("/Nick", style).Contains("<#123456>/Nick</color>"), "a slash in a nickname is not treated as a chat command");
            style = new OutgoingStyle { Enabled = true, ColorMode = MessageColorMode.Gradient, Color = "#FF0000", EndColor = "#0000FF" };
            const string emojiName = "А👩‍💻e\u0301🇷🇺";
            name = NicknameFormat.Compose(emojiName, style);
            check(TextUtil.StripTags(name) == emojiName, "nickname gradient preserves Cyrillic, accents and emoji sequences");
            check(name.Contains(">👩‍💻</color>") && name.Contains(">e\u0301</color>") && name.Contains(">🇷🇺</color>"),
                "nickname gradient never splits emoji or combining characters");
            check(name.StartsWith("<#FF0000>") && name.EndsWith("<#0000FF>🇷🇺</color>"), "nickname gradient uses both selected endpoints");
            var messageStyle = new OutgoingStyle { Enabled = true, Bold = true };
            string fullMessage = new string('m', OutgoingFormat.WireLimit - 7);
            check(OutgoingFormat.TryCompose(fullMessage, messageStyle, out string message) && message.Length == OutgoingFormat.WireLimit,
                "message keeps its entire 250-character wire budget independently of nickname style");
            check(!message.Contains("color") && !name.Contains("<b>"), "nickname and message style snapshots are independent");
            bytes = Encoding.Unicode.GetBytes(new string('n', 600));
            check(ReferenceEquals(bytes, NicknameFormat.Apply(bytes, style)), "oversized nickname formatting falls back to original without truncating identity");
            check(NicknameFormat.Compose("<b></b>", style) == "<b></b>", "empty visible nickname keeps original formatting");
            check(NicknameFormat.Compose("", style) == "", "empty nickname stays empty");
            style.ColorMode = MessageColorMode.Solid; style.Color = "invalid";
            check(NicknameFormat.Compose("Nick", style) == "<#F2C46D>Nick</color>", "invalid nickname color uses established fallback");
            check(NicknameFormat.Apply(Encoding.Unicode.GetBytes("😀"), style).SequenceEqual(Encoding.Unicode.GetBytes("<#F2C46D>😀</color>")),
                "wire nickname uses the game's UTF-16 encoding including surrogate pairs");
        }
    }
}
