using System;

namespace ChatPlus
{
    /// <summary>Choices offered by the settings window. Any other value can be written into the config file.</summary>
    internal static class Presets
    {
        public static readonly string[] TimeFormats = { "HH:mm", "HH:mm:ss", "h:mm tt", "h:mm:ss tt", "[HH:mm]" };

        // Parallel to Lang.ColorNames: Soft, Gray, Gold, Sky, Mint, Rose, White.
        public static readonly string[] TimeColors = { "#F5EDE1A6", "#B9AFA8", "#F2C46D", "#8EC5FF", "#8FD6B5", "#F4A6B8", "#FFFFFF" };
        public static readonly string[] HighlightColors = { "#F5EDE1", "#B9AFA8", "#F2C46D", "#8EC5FF", "#8FD6B5", "#F4A6B8", "#FFFFFF" };

        public const string DefaultTimeColor = "#F5EDE1A6";
        public const string DefaultHighlightColor = "#F2C46D";

        /// <summary>Index of a value in a list of presets (colors compared after normalization), or -1.</summary>
        public static int IndexOf(string[] presets, string value, bool isColor)
        {
            string wanted = value ?? string.Empty;
            if (isColor && ChatFormat.TryParseColor(wanted, out string normalized))
                wanted = normalized;
            for (int i = 0; i < presets.Length; i++)
            {
                string preset = presets[i];
                if (isColor && ChatFormat.TryParseColor(preset, out string presetColor))
                    preset = presetColor;
                if (string.Equals(preset, wanted, isColor ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        /// <summary>The next or previous preset; from a custom value, the first or last one.</summary>
        public static string Step(string[] presets, string value, int step, bool isColor)
        {
            int index = IndexOf(presets, value, isColor);
            if (index < 0)
                index = step > 0 ? 0 : presets.Length - 1;
            else
                index = (index + step + presets.Length) % presets.Length;
            return presets[index];
        }
    }
}
