using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ChatPlus
{
    /// <summary>
    /// Added to every chat line the mod knows: keeps the line's history entry, so the line can be drawn again when
    /// a setting changes, and supports mouse selection and copying.
    /// </summary>
    internal sealed class ChatLine : MonoBehaviour, IPointerClickHandler, IPointerDownHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        internal ChatEntry Entry;
        TMP_Text _text;
        UnityEngine.UI.ScrollRect _scroll;

        internal static ChatLine MakeSelectable(TMP_Text text)
        {
            ChatLine line = text.GetComponent<ChatLine>() ?? text.gameObject.AddComponent<ChatLine>();
            line._text = text;
            text.raycastTarget = true;
            return line;
        }

        internal static ChatLine Attach(TMP_Text text, ChatEntry entry)
        {
            ChatLine line = MakeSelectable(text);
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
            if (eventData == null || eventData.button != PointerEventData.InputButton.Right)
                return;
            Plugin plugin = Plugin.Instance;
            if (plugin == null || !plugin.RightClickCopy.Value)
                return;
            if (ChatSelection.Contains(_text) && ChatSelection.Copy())
                return;
            if (Entry == null)
                return;
            string text = ChatFormat.CopyText(Entry);
            if (text.Length == 0)
                return;
            GUIUtility.systemCopyBuffer = text;
            Toast.Show(Lang.Current.Copied);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData != null) ChatSelection.Begin(_text, eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (ChatSelection.Enabled && eventData.button == PointerEventData.InputButton.Left)
                ChatSelection.BeginDrag(_text, eventData);
            else
            {
                _scroll = GetComponentInParent<UnityEngine.UI.ScrollRect>();
                _scroll?.OnBeginDrag(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (ChatSelection.Enabled && eventData.button == PointerEventData.InputButton.Left)
                ChatSelection.Drag(eventData);
            else
                _scroll?.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ChatSelection.EndDrag(eventData);
            _scroll?.OnEndDrag(eventData);
            _scroll = null;
        }
    }
}
