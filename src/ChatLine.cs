using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ChatPlus
{
    /// <summary>
    /// Added to every chat line the mod knows: keeps the line's history entry, so the line can be drawn again when
    /// a setting changes, and copies the message on a right click.
    /// </summary>
    internal sealed class ChatLine : MonoBehaviour, IPointerClickHandler
    {
        internal ChatEntry Entry;
        TMP_Text _text;

        internal static ChatLine Attach(TMP_Text text, ChatEntry entry)
        {
            ChatLine line = text.GetComponent<ChatLine>();
            if (line == null)
                line = text.gameObject.AddComponent<ChatLine>();
            line._text = text;
            line.Entry = entry;
            line.Render(ChatLines.Style, DateTime.Now);
            LineSizer.Apply(text);
            return line;
        }

        internal void Render(LineStyle style, DateTime now)
        {
            if (_text == null || Entry == null)
                return;
            string text = ChatFormat.Compose(Entry, style, now);
            if (!string.Equals(_text.text, text, StringComparison.Ordinal))
                _text.text = text;
        }

        // The line's Button opens the player's ID card on a left click and ignores other buttons.
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Right || Entry == null)
                return;
            Plugin plugin = Plugin.Instance;
            if (plugin == null || !plugin.RightClickCopy.Value)
                return;
            string text = ChatFormat.CopyText(Entry);
            if (text.Length == 0)
                return;
            GUIUtility.systemCopyBuffer = text;
            Toast.Show(Lang.Current.Copied);
        }
    }
}
