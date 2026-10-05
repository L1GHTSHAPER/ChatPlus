using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ChatPlus
{
    /// <summary>Text helpers: rich text tags, mentions, keyword lists. Plain .NET, no Unity types.</summary>
    internal static class TextUtil
    {
        // Only TextMeshPro's own tags, so that text such as "a < b > c" stays as it is.
        static readonly Regex RichTextTag = new Regex(
            "<(?:/?(?:align|allcaps|alpha|b|br|color|cspace|font|font-weight|gradient|i|indent|line-height|" +
            "line-indent|link|lowercase|margin|margin-left|margin-right|mark|material|mspace|nobr|noparse|page|pos|" +
            "rotate|s|size|smallcaps|space|sprite|strikethrough|style|sub|sup|u|uppercase|voffset|width|zwj|zwsp)" +
            "(?:[=\\s][^<>]*)?|#[0-9A-Fa-f]{3,8})>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        static readonly char[] KeywordSeparators = { ',', ';', '\n', '\r' };

        /// <summary>Removes TextMeshPro rich text tags such as &lt;color=#ff0&gt; or &lt;b&gt;.</summary>
        public static string StripTags(string text)
        {
            return string.IsNullOrEmpty(text) ? string.Empty : RichTextTag.Replace(text, string.Empty);
        }

        /// <summary>Tags removed, line breaks and tabs turned into spaces.</summary>
        public static string Plain(string text)
        {
            string stripped = StripTags(text);
            if (stripped.IndexOfAny(new[] { '\r', '\n', '\t' }) < 0)
                return stripped;
            var plain = new StringBuilder(stripped.Length);
            foreach (char c in stripped)
                plain.Append(c == '\r' || c == '\n' || c == '\t' ? ' ' : c);
            return plain.ToString();
        }

        public static List<string> ParseKeywords(string value)
        {
            var words = new List<string>();
            if (string.IsNullOrEmpty(value))
                return words;
            foreach (string part in value.Split(KeywordSeparators, StringSplitOptions.RemoveEmptyEntries))
            {
                string word = part.Trim();
                if (word.Length > 0 && !words.Exists(w => string.Equals(w, word, StringComparison.OrdinalIgnoreCase)))
                    words.Add(word);
            }
            return words;
        }

        public static string JoinKeywords(IEnumerable<string> words)
        {
            return string.Join(", ", words);
        }

        /// <summary>
        /// True when the message contains the player's name (at least 2 characters) or one of the keywords as a whole
        /// word, ignoring case and rich text tags.
        /// </summary>
        public static bool IsMention(string message, string playerName, IList<string> keywords)
        {
            if (string.IsNullOrEmpty(message))
                return false;
            string text = StripTags(message).ToLowerInvariant();
            string name = StripTags(playerName).Trim();
            if (name.Length >= 2 && ContainsWord(text, name.ToLowerInvariant()))
                return true;
            if (keywords == null)
                return false;
            foreach (string keyword in keywords)
            {
                if (ContainsWord(text, keyword.Trim().ToLowerInvariant()))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Ordinal search for <paramref name="word"/> that is not part of a longer word. Both strings must already be
        /// lower-cased.
        /// </summary>
        public static bool ContainsWord(string text, string word)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(word))
                return false;
            int index = text.IndexOf(word, StringComparison.Ordinal);
            while (index >= 0)
            {
                int end = index + word.Length;
                bool startsWord = index == 0 || !IsWordChar(text[index - 1]) || !IsWordChar(word[0]);
                bool endsWord = end >= text.Length || !IsWordChar(text[end]) || !IsWordChar(word[word.Length - 1]);
                if (startsWord && endsWord)
                    return true;
                index = text.IndexOf(word, index + 1, StringComparison.Ordinal);
            }
            return false;
        }

        // Letters and digits of scripts that separate words with spaces. CJK text has no such boundaries, so a
        // neighbouring CJK character never prevents a match.
        static bool IsWordChar(char c)
        {
            return c < '⺀' && (char.IsLetterOrDigit(c) || c == '_');
        }
    }
}
