using System;
using System.Collections.Generic;
using System.Text;

namespace ChatPlus
{
    // TMP supplies spans in the original string for rendered characters. Copying those spans preserves
    // supplementary Unicode characters and literal tags inside <noparse>, without copying formatting tags.
    internal readonly struct SelectionGlyph
    {
        internal readonly int Index;
        internal readonly int Length;
        internal readonly string Replacement;

        internal SelectionGlyph(int index, int length, string replacement = null)
        {
            Index = index;
            Length = length;
            Replacement = replacement;
        }
    }

    internal static class SelectionText
    {
        internal static string Slice(string source, IReadOnlyList<SelectionGlyph> glyphs, int anchor, int caret)
        {
            if (source == null || glyphs == null) return string.Empty;
            int first = Math.Max(0, Math.Min(anchor, caret));
            int last = Math.Min(glyphs.Count, Math.Max(anchor, caret));
            var result = new StringBuilder();
            for (int i = first; i < last; i++)
            {
                SelectionGlyph glyph = glyphs[i];
                if (glyph.Replacement != null)
                    result.Append(glyph.Replacement);
                else if (glyph.Index >= 0 && glyph.Length > 0 && glyph.Index <= source.Length - glyph.Length)
                    result.Append(source, glyph.Index, glyph.Length);
            }
            return result.ToString();
        }
    }
}
