using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChatPlus
{
    /// <summary>Two badges beside the game's reopen-chat button, using the game's counter appearance.</summary>
    internal static class HiddenChatNotifications
    {
        static readonly HiddenMessageCounts Counts = new HiddenMessageCounts();
        static TextChannelManager _chat;
        static GameObject _original;
        static GameObject _global;
        static GameObject _local;
        static TMP_Text _globalText;
        static TMP_Text _localText;

        static void UseChat(TextChannelManager chat)
        {
            if (_chat == chat)
                return;
            Clear();
            _chat = chat;
        }

        internal static void MessageAdded(TextChannelManager chat, bool isLocal, bool own)
        {
            try
            {
                UseChat(chat);
                UIManager ui = GameAccess.UI;
                if (ui != null)
                    Counts.Receive(isLocal, own, ui.IsMessagePanelHidden);
            }
            catch (Exception e)
            {
                Plugin.LogError("Could not count collapsed chat messages: ", e);
            }
        }

        internal static void Refresh()
        {
            try
            {
                TextChannelManager chat = GameAccess.Chat;
                UseChat(chat);
                UIManager ui = GameAccess.UI;
                if (chat == null || ui == null)
                    return;
                bool hidden = ui.IsMessagePanelHidden;
                Counts.ObserveVisibility(hidden);
                if (!hidden && _global == null && _local == null)
                    return;
                GameObject original = chat.HiddenCounter;
                if (original == null)
                    return;
                if (_original != original || _global == null || _local == null)
                    Prepare(original);
                // Leave the native counter alone if its UI structure could not be copied.
                if (_globalText == null || _localText == null)
                    return;
                if (original.activeSelf)
                    original.SetActive(false);
                Plugin plugin = Plugin.Instance;
                Lang lang = Lang.Current;
                Show(_global, _globalText, hidden && plugin.ShowGlobalCounter.Value, Counts.Global, lang.GlobalCounterPrefix);
                Show(_local, _localText, hidden && plugin.ShowLocalCounter.Value, Counts.Local, lang.LocalCounterPrefix);
            }
            catch (Exception e)
            {
                Plugin.LogError("Could not update collapsed chat counters: ", e);
            }
        }

        static void Prepare(GameObject original)
        {
            DestroyBadges();
            RectTransform source = original.transform as RectTransform;
            if (source == null || original.GetComponentInChildren<TMP_Text>(true) == null)
                return;
            float height = Mathf.Max(30f, source.rect.height);
            float width = Mathf.Max(72f, source.rect.width * 2f);
            _global = CreateBadge(original, "ChatPlus.GlobalCounter", width, height, height * 0.55f, out _globalText);
            _local = CreateBadge(original, "ChatPlus.LocalCounter", width, height, -height * 0.55f, out _localText);
            _original = original;
        }

        static GameObject CreateBadge(GameObject original, string name, float width, float height, float y, out TMP_Text text)
        {
            GameObject badge = UnityEngine.Object.Instantiate(original, original.transform.parent, false);
            badge.name = name;
            badge.SetActive(false);
            RectTransform rect = (RectTransform)badge.transform;
            RectTransform source = (RectTransform)original.transform;
            // Keep the original right edge, so wider labels do not protrude past a button at the screen edge.
            rect.anchorMin = rect.anchorMax = source.anchorMax;
            rect.pivot = new Vector2(1f, source.pivot.y);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = source.anchoredPosition + new Vector2((1f - source.pivot.x) * source.rect.width, y);
            foreach (Graphic graphic in badge.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
            LayoutElement layout = badge.GetComponent<LayoutElement>() ?? badge.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            text = badge.GetComponentInChildren<TMP_Text>(true);
            if (text.transform != badge.transform)
            {
                text.rectTransform.anchorMin = Vector2.zero;
                text.rectTransform.anchorMax = Vector2.one;
                text.rectTransform.offsetMin = new Vector2(4f, 0f);
                text.rectTransform.offsetMax = new Vector2(-4f, 0f);
            }
            text.enableAutoSizing = true;
            text.fontSizeMax = text.fontSize;
            text.fontSizeMin = Mathf.Min(12f, text.fontSize);
            text.alignment = TextAlignmentOptions.Center;
            return badge;
        }

        static void Show(GameObject badge, TMP_Text text, bool enabled, int count, string prefix)
        {
            bool visible = enabled && count > 0;
            if (visible)
            {
                string label = prefix + ": " + count.ToString(CultureInfo.InvariantCulture);
                if (text.text != label)
                    text.text = label;
            }
            if (badge.activeSelf != visible)
                badge.SetActive(visible);
        }

        static void DestroyBadges()
        {
            if (_global != null)
                UnityEngine.Object.Destroy(_global);
            if (_local != null)
                UnityEngine.Object.Destroy(_local);
            _global = _local = null;
            _globalText = _localText = null;
            _original = null;
        }

        internal static void Clear()
        {
            // Restore the native counter if the plugin is unloaded while the chat is collapsed.
            if (_chat != null && _original != null)
                _original.SetActive(_chat.HiddenNotificationCount > 0);
            DestroyBadges();
            Counts.ObserveVisibility(false);
            _chat = null;
        }
    }
}
