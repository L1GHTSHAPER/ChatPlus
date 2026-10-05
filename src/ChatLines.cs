using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChatPlus
{
    /// <summary>
    /// What the mod does with the lines of the game's chat panel: decorates and records new lines, puts the session's
    /// history back after a lobby change, applies the line limits and clears the chat.
    /// </summary>
    internal static class ChatLines
    {
        /// <summary>Set while the mod adds a line of its own through AddNotification; such lines are not recorded.</summary>
        internal static bool Suppress;

        /// <summary>Bumped when a setting that changes how lines look is changed.</summary>
        internal static int StyleVersion;

        static LineStyle _style;
        static int _builtVersion = -1;
        static string _builtName;
        static Lang _builtLang;

        /// <summary>The current decoration settings (rebuilt when they or your name change).</summary>
        internal static LineStyle Style
        {
            get
            {
                string name = GameAccess.LocalPlayerName;
                Lang lang = Lang.Current;
                if (_style == null || _builtVersion != StyleVersion || name != _builtName || lang != _builtLang)
                {
                    _style = Plugin.Instance.BuildStyle(name, lang);
                    _builtVersion = StyleVersion;
                    _builtName = name;
                    _builtLang = lang;
                }
                return _style;
            }
        }

        /// <summary>Makes the game keep as many lines per tab as the settings say.</summary>
        internal static void ApplyLimits()
        {
            Plugin plugin = Plugin.Instance;
            if (plugin != null)
                GameAccess.SetLineLimits(plugin.GlobalLimit.Value, plugin.LocalLimit.Value);
        }

        /// <summary>
        /// The game removes only one old line per new line, so after the limit was lowered the extra lines are removed
        /// here (when the next line arrives, so that moving the slider does not wipe the chat at once).
        /// </summary>
        internal static void Trim(List<GameObject> lines, int limit)
        {
            if (lines == null)
                return;
            limit = Math.Max(1, limit);
            while (lines.Count > limit)
            {
                GameObject oldest = lines[0];
                lines.RemoveAt(0);
                if (oldest != null)
                    UnityEngine.Object.Destroy(oldest);
            }
        }

        static int Limit(bool isLocal)
        {
            Plugin plugin = Plugin.Instance;
            return isLocal ? plugin.LocalLimit.Value : plugin.GlobalLimit.Value;
        }

        /// <summary>Called after the game added a player's message (yours or someone else's) to a chat tab.</summary>
        internal static void OnMessageAdded(TextChannelManager chat, string userName, string text, bool isLocal, int senderIndex, bool own, LineMark mark)
        {
            List<GameObject> lines = GameAccess.Lines(chat, isLocal);
            TMP_Text line = FindNewLine(lines, mark);
            if (line == null)
                return;
            var entry = new ChatEntry
            {
                Time = DateTime.Now,
                IsLocal = isLocal,
                Kind = own ? ChatKind.Own : ChatKind.Player,
                SteamId = own ? GameAccess.OwnSteamId(chat) : GameAccess.SteamIdAt(senderIndex),
                Name = userName ?? string.Empty,
                Message = text ?? string.Empty,
                Line = line.text ?? string.Empty
            };
            ChatLine.Attach(line, entry);
            Plugin.Instance.History.Add(entry);
            Trim(lines, Limit(isLocal));
        }

        /// <summary>Called after the game added a notification line (someone joined, XP...) to the Global tab.</summary>
        internal static void OnNotificationAdded(TextChannelManager chat, string text, LineMark mark)
        {
            List<GameObject> lines = GameAccess.Lines(chat, false);
            TMP_Text line = FindNewLine(lines, mark);
            if (line == null)
                return;
            var entry = new ChatEntry
            {
                Time = DateTime.Now,
                IsLocal = false,
                Kind = ChatKind.Notification,
                Message = text ?? string.Empty,
                Line = line.text ?? string.Empty
            };
            ChatLine.Attach(line, entry);
            if (!Suppress)
                Plugin.Instance.History.Add(entry);
            Trim(lines, Limit(false));
        }

        /// <summary>
        /// The line the game's method has just added: the newest line after the mark that is not handled yet (a line
        /// another mod adds during the call already has a ChatLine). Nothing when the method was skipped (e.g. by
        /// another mod's prefix). Without a mark (our prefix did not run) only the newest line is considered.
        /// </summary>
        static TMP_Text FindNewLine(List<GameObject> lines, LineMark mark)
        {
            if (lines == null || lines.Count == 0)
                return null;
            int stop = mark != null ? Math.Max(0, lines.Count - 8) : lines.Count - 1;
            for (int i = lines.Count - 1; i >= stop; i--)
            {
                GameObject line = lines[i];
                if (mark != null && ReferenceEquals(line, mark.Newest))
                    return null;
                if (line == null || line.GetComponent<ChatLine>() != null)
                    continue;
                return line.GetComponent<TMP_Text>();
            }
            return null;
        }

        /// <summary>Called when the game has set up the chat of a lobby: puts the session's earlier lines back.</summary>
        internal static void OnChatStarted(TextChannelManager chat)
        {
            GameAccess.SetChat(chat);
            ApplyLimits();
            TMP_Text prefab = GameAccess.TextPrefab(chat);
            if (prefab != null && prefab.fontSize > 1f)
                LineSizer.BaseSize = prefab.fontSize;
            Plugin plugin = Plugin.Instance;
            if (!plugin.RestoreHistory.Value)
                return;
            List<ChatEntry> entries = plugin.History.Snapshot();
            if (entries.Count == 0)
                return;
            UIManager ui = GameAccess.FindUI();
            int restored = Restore(chat, ui, prefab, entries, false, plugin.RestoreNotifications.Value) +
                           Restore(chat, ui, prefab, entries, true, false);
            if (restored > 0)
                Plugin.Log.LogInfo($"Restored {restored} chat lines from earlier in this session.");
        }

        static int Restore(TextChannelManager chat, UIManager ui, TMP_Text prefab, List<ChatEntry> entries, bool isLocal, bool notifications)
        {
            Transform parent = GameAccess.Content(ui, isLocal);
            List<GameObject> lines = GameAccess.Lines(chat, isLocal);
            if (parent == null || lines == null || prefab == null)
                return 0;

            // Lines this lobby has shown already (if any arrived before the chat started) are not repeated.
            var shown = new HashSet<ChatEntry>();
            foreach (GameObject existing in lines)
            {
                ChatLine known = existing != null ? existing.GetComponent<ChatLine>() : null;
                if (known != null && known.Entry != null)
                    shown.Add(known.Entry);
            }

            // The newest entries that fit, leaving room for the separator line.
            int room = Limit(isLocal) - 1;
            var picked = new List<ChatEntry>();
            for (int i = entries.Count - 1; i >= 0 && picked.Count < room; i--)
            {
                ChatEntry entry = entries[i];
                if (entry.IsLocal != isLocal || shown.Contains(entry))
                    continue;
                if (entry.Kind == ChatKind.Notification && !notifications)
                    continue;
                if (entry.Kind == ChatKind.Player && GameAccess.IsBlocked(entry.SteamId))
                    continue;
                picked.Add(entry);
            }
            if (picked.Count == 0)
                return 0;
            picked.Reverse();

            // In front of any line the lobby has shown already, oldest first.
            int index = 0;
            foreach (ChatEntry entry in picked)
            {
                TMP_Text line = CreateLine(prefab, parent, entry.Line);
                Button button = line.GetComponent<Button>();
                if (button != null)
                {
                    if (entry.Kind == ChatKind.Notification)
                    {
                        button.interactable = false;
                    }
                    else if (entry.Kind == ChatKind.Player && !string.IsNullOrEmpty(entry.SteamId))
                    {
                        string name = entry.Name;
                        string steamId = entry.SteamId;
                        button.onClick.AddListener(() => GameAccess.OpenIdCard(name, steamId));
                    }
                }
                ChatLine.Attach(line, entry);
                line.transform.SetSiblingIndex(index);
                lines.Insert(index, line.gameObject);
                index++;
            }

            TMP_Text separator = CreateLine(prefab, parent, Lang.Current.EarlierMessages);
            Button separatorButton = separator.GetComponent<Button>();
            if (separatorButton != null)
                separatorButton.interactable = false;
            LineSizer.Apply(separator);
            separator.transform.SetSiblingIndex(index);
            lines.Insert(index, separator.gameObject);

            Trim(lines, Limit(isLocal));
            return picked.Count;
        }

        static TMP_Text CreateLine(TMP_Text prefab, Transform parent, string text)
        {
            TMP_Text line = UnityEngine.Object.Instantiate(prefab, parent);
            line.text = text ?? string.Empty;
            return line;
        }

        /// <summary>Draws all lines again after a setting that changes their look was changed.</summary>
        internal static void RenderAll()
        {
            UIManager ui = GameAccess.UI;
            if (ui == null)
                return;
            LineStyle style = Style;
            DateTime now = DateTime.Now;
            for (int tab = 0; tab < 2; tab++)
            {
                Transform content = GameAccess.Content(ui, tab == 1);
                if (content == null)
                    continue;
                foreach (ChatLine line in content.GetComponentsInChildren<ChatLine>(true))
                    line.Render(style, now);
            }
        }

        /// <summary>Removes every line from both chat tabs (the history is kept).</summary>
        internal static int Clear()
        {
            TextChannelManager chat = GameAccess.Chat;
            int removed = 0;
            for (int tab = 0; tab < 2; tab++)
            {
                List<GameObject> lines = GameAccess.Lines(chat, tab == 1);
                if (lines == null)
                    continue;
                foreach (GameObject line in lines)
                {
                    if (line != null)
                    {
                        UnityEngine.Object.Destroy(line);
                        removed++;
                    }
                }
                lines.Clear();
            }
            return removed;
        }
    }
}
