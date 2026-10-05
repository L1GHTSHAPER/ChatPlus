using System;
using TMPro;
using UnityEngine;

namespace ChatPlus
{
    /// <summary>
    /// Added to the content object of each chat tab: gives every line in it the text size from the settings,
    /// including lines that other mods add (they all come from the game's line prefab).
    /// </summary>
    internal sealed class LineSizer : MonoBehaviour
    {
        // Font size of the game's MessageText prefab, in case the prefab cannot be read.
        const float DefaultBaseSize = 46.8f;
        // New lines are always at the end; this many of the newest lines are checked when lines are added.
        const int RecentLines = 24;

        internal static float BaseSize = DefaultBaseSize;
        /// <summary>Bumped when the text size setting changes, so every line is resized once.</summary>
        internal static int Version;

        int _count = -1;
        int _newestId;
        int _version = -1;

        static int Percent
        {
            get
            {
                Plugin plugin = Plugin.Instance;
                return plugin != null ? plugin.TextSize.Value : 100;
            }
        }

        static float TargetSize => BaseSize * Percent / 100f;

        internal static void Apply(TMP_Text text)
        {
            // At 100% lines are left alone, so another mod that sets the text size is not fought with.
            if (text == null || Percent == 100)
                return;
            float size = TargetSize;
            if (Math.Abs(text.fontSize - size) > 0.01f)
                text.fontSize = size;
        }

        void LateUpdate()
        {
            Transform content = transform;
            int count = content.childCount;
            int newestId = count > 0 ? content.GetChild(count - 1).GetInstanceID() : 0;
            bool resized = _version != Version;
            if (!resized && count == _count && newestId == _newestId)
                return;
            _count = count;
            _newestId = newestId;
            _version = Version;
            // After the setting went back to 100% every line is reset once; new lines then keep the game's size.
            if (!resized && Percent == 100)
                return;

            float size = TargetSize;
            int first = resized ? 0 : Math.Max(0, count - RecentLines);
            for (int i = first; i < count; i++)
            {
                TMP_Text text = content.GetChild(i).GetComponent<TMP_Text>();
                if (text != null && Math.Abs(text.fontSize - size) > 0.01f)
                    text.fontSize = size;
            }
        }
    }
}
