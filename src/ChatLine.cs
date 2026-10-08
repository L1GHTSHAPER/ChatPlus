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
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        internal ChatEntry Entry;
        TMP_Text _text;
        UnityEngine.UI.ScrollRect _scroll;
        bool _selecting;
        PointerEventData _dragEvent;

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
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
            StopDrag();
            _scroll = GetComponentInParent<UnityEngine.UI.ScrollRect>();
            // Choose once at the press. Releasing Shift or pressing it during a swipe must not change its owner.
            // Touch gestures always keep the native scrolling behavior.
            _selecting = ChatSelection.Enabled && eventData.pointerId < 0 &&
                (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            if (_selecting)
                ChatSelection.Begin(_text, eventData);
            else
                ChatSelection.Clear();
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            // ScrollRect normally receives this before a drag and stops its previous inertia.
            // Our IDragHandler makes this line the event target, so forward that phase too.
            if (_scroll == null) _scroll = GetComponentInParent<UnityEngine.UI.ScrollRect>();
            _scroll?.OnInitializePotentialDrag(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
            // The Button and this drag handler share a GameObject: Unity otherwise leaves it eligible for a click
            // after dragging, which opens a player card when the user only wanted to scroll.
            eventData.eligibleForClick = false;
            _dragEvent = eventData;
            _selecting = _selecting && ChatSelection.BeginDrag(_text, eventData);
            if (!_selecting)
                _scroll?.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragEvent == null) return;
            if (_selecting)
                ChatSelection.Drag(eventData);
            else
                _scroll?.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragEvent == null) return;
            _dragEvent = eventData;
            StopDrag();
        }

        void StopDrag()
        {
            if (_dragEvent != null)
            {
                if (_selecting) ChatSelection.EndDrag(_dragEvent);
                else _scroll?.OnEndDrag(_dragEvent);
            }
            _dragEvent = null;
            _selecting = false;
            _scroll = null;
        }

        void OnDisable()
        {
            bool selecting = _selecting;
            StopDrag();
            if (selecting) ChatSelection.Clear();
        }
    }
}
