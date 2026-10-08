using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChatPlus
{
    /// <summary>
    /// Cached, exception-safe access to the game objects and private fields the mod uses.
    /// The game's singleton getters fall back to FindAnyObjectByType when the instance is missing (e.g. in the main
    /// menu), so lookups of missing instances are throttled. A field that cannot be found (after a game update) is
    /// null here, and the feature that needs it switches itself off.
    /// </summary>
    internal static class GameAccess
    {
        const float LookupInterval = 1f;

        static TextChannelManager _chat;
        static UIManager _ui;
        static float _nextChatLookup;
        static float _nextUiLookup;
        static readonly List<string> MissingFields = new List<string>();

        // TextChannelManager
        static readonly AccessTools.FieldRef<TextChannelManager, TMP_Text> TextPrefabRef = Field<TextChannelManager, TMP_Text>("_textPrefab");
        static readonly AccessTools.FieldRef<TextChannelManager, List<GameObject>> GlobalLinesRef = Field<TextChannelManager, List<GameObject>>("_messageObjectsGlobal");
        static readonly AccessTools.FieldRef<TextChannelManager, List<GameObject>> LocalLinesRef = Field<TextChannelManager, List<GameObject>>("_messageObjectsLocal");
        static readonly AccessTools.FieldRef<TextChannelManager, string> OwnIdRef = Field<TextChannelManager, string>("_playerId");

        // UIManager: the chat panel
        static readonly AccessTools.FieldRef<UIManager, RectTransform> PanelRootRef = Field<UIManager, RectTransform>("_messagePanelParent");
        static readonly AccessTools.FieldRef<UIManager, GameObject> PanelBackgroundRef = Field<UIManager, GameObject>("_messagePanel");
        static readonly AccessTools.FieldRef<UIManager, RectTransform> CornerBottomLeftRef = Field<UIManager, RectTransform>("_messageCornerTransform1");
        static readonly AccessTools.FieldRef<UIManager, Transform> GlobalScrollRef = Field<UIManager, Transform>("_globalMessagePanel");
        static readonly AccessTools.FieldRef<UIManager, Transform> LocalScrollRef = Field<UIManager, Transform>("_localMessagePanel");
        static readonly AccessTools.FieldRef<UIManager, RectTransform> GlobalTabRef = Field<UIManager, RectTransform>("_globalButtonTransform");
        static readonly AccessTools.FieldRef<UIManager, RectTransform> LocalTabRef = Field<UIManager, RectTransform>("_localButtonTransform");
        static readonly AccessTools.FieldRef<UIManager, List<Image>> PanelImagesRef = Field<UIManager, List<Image>>("_messagePanelImages");
        static readonly AccessTools.FieldRef<UIManager, float> LimitMinXRef = Field<UIManager, float>("_messageLimitMinX");
        static readonly AccessTools.FieldRef<UIManager, float> LimitMinYRef = Field<UIManager, float>("_messageLimitMinY");
        static readonly AccessTools.FieldRef<UIManager, float> LimitMaxXRef = Field<UIManager, float>("_messageLimitMaxX");
        static readonly AccessTools.FieldRef<UIManager, bool> MessageDragRef = Field<UIManager, bool>("_isMessageDrag");
        // A chat line in the scene whose shared font material carries the outline keyword the game switches.
        static readonly AccessTools.FieldRef<UIManager, TextMeshProUGUI> FontTextRef = Field<UIManager, TextMeshProUGUI>("_messageTextForFont");

        // GameSettings: how many lines each chat tab keeps
        static readonly AccessTools.FieldRef<GameSettings, int> GlobalLimitRef = Field<GameSettings, int>("_globalMessageLimitCount");
        static readonly AccessTools.FieldRef<GameSettings, int> LocalLimitRef = Field<GameSettings, int>("_localMessageLimitCount");

        public static TextChannelManager Chat => Lookup(ref _chat, ref _nextChatLookup, () => NetworkSingleton<TextChannelManager>.I);
        public static UIManager UI => Lookup(ref _ui, ref _nextUiLookup, () => MonoSingleton<UIManager>.I);

        static T Lookup<T>(ref T cached, ref float nextLookup, Func<T> find) where T : UnityEngine.Object
        {
            if (cached == null && Time.unscaledTime >= nextLookup)
            {
                nextLookup = Time.unscaledTime + LookupInterval;
                try
                {
                    cached = find();
                }
                catch (Exception)
                {
                    cached = null;
                }
            }
            return cached;
        }

        /// <summary>Looks the UIManager up now, without the throttling of <see cref="UI"/> (for one-off use).</summary>
        public static UIManager FindUI()
        {
            try
            {
                UIManager ui = MonoSingleton<UIManager>.I;
                if (ui != null)
                    _ui = ui;
                return ui;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Looks the TextChannelManager up now, without the throttling of <see cref="Chat"/>.</summary>
        public static TextChannelManager FindChat()
        {
            try
            {
                TextChannelManager chat = NetworkSingleton<TextChannelManager>.I;
                if (chat != null)
                    _chat = chat;
                return chat;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Remembers the instances the game just started, so they need no lookup.</summary>
        public static void SetChat(TextChannelManager chat)
        {
            if (chat != null)
                _chat = chat;
        }

        public static void SetUI(UIManager ui)
        {
            if (ui != null)
                _ui = ui;
        }

        public static void LogMissingFields()
        {
            if (MissingFields.Count > 0)
                Plugin.Log.LogWarning("Game fields not found (the game was probably updated), some features are off: " +
                    string.Join(", ", MissingFields));
        }

        // ---------------- TextChannelManager ----------------

        public static TMP_Text TextPrefab(TextChannelManager chat)
        {
            return TextPrefabRef != null && chat != null ? TextPrefabRef(chat) : null;
        }

        /// <summary>The game's list of line objects of one chat tab, oldest first.</summary>
        public static List<GameObject> Lines(TextChannelManager chat, bool isLocal)
        {
            AccessTools.FieldRef<TextChannelManager, List<GameObject>> field = isLocal ? LocalLinesRef : GlobalLinesRef;
            return field != null && chat != null ? field(chat) : null;
        }

        public static string OwnSteamId(TextChannelManager chat)
        {
            return OwnIdRef != null && chat != null ? OwnIdRef(chat) ?? string.Empty : string.Empty;
        }

        /// <summary>The name your messages are sent with (may contain rich text tags).</summary>
        public static string LocalPlayerName
        {
            get
            {
                TextChannelManager chat = Chat;
                return chat != null ? chat.UserName ?? string.Empty : string.Empty;
            }
        }

        // ---------------- UIManager ----------------

        public static RectTransform PanelRoot(UIManager ui) => Get(PanelRootRef, ui);
        public static GameObject PanelBackground(UIManager ui) => Get(PanelBackgroundRef, ui);
        public static RectTransform CornerBottomLeft(UIManager ui) => Get(CornerBottomLeftRef, ui);
        public static Transform GlobalScroll(UIManager ui) => Get(GlobalScrollRef, ui);
        public static Transform LocalScroll(UIManager ui) => Get(LocalScrollRef, ui);
        public static List<Image> PanelImages(UIManager ui) => Get(PanelImagesRef, ui);
        public static RectTransform TabButton(UIManager ui, bool local) => Get(local ? LocalTabRef : GlobalTabRef, ui);

        public static bool HasDragLimits => LimitMinXRef != null && LimitMinYRef != null && LimitMaxXRef != null;

        public static void SetDragLimits(UIManager ui, float minX, float minY, float? maxX)
        {
            if (ui == null || !HasDragLimits)
                return;
            LimitMinXRef(ui) = minX;
            LimitMinYRef(ui) = minY;
            if (maxX.HasValue)
                LimitMaxXRef(ui) = maxX.Value;
        }

        /// <summary>While set, the game does not fade the chat panel out (it is set while the panel is dragged).</summary>
        public static void SetPanelHeld(UIManager ui, bool held)
        {
            if (ui != null && MessageDragRef != null)
                MessageDragRef(ui) = held;
        }

        public static Transform Content(UIManager ui, bool isLocal)
        {
            if (ui == null)
                return null;
            try
            {
                return isLocal ? ui.TextContentLocalTransform : ui.TextContentGlobalTransform;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Whether the chat font's outline (UNDERLAY_ON) is on; false when it cannot be read.</summary>
        public static bool TryGetFontOutline(out bool outline)
        {
            outline = false;
            UIManager ui = UI;
            TextMeshProUGUI text = FontTextRef != null && ui != null ? FontTextRef(ui) : null;
            Material material = text != null ? text.fontSharedMaterial : null;
            if (material == null)
                return false;
            outline = material.IsKeywordEnabled("UNDERLAY_ON");
            return true;
        }

        public static TMP_InputField MessageInput
        {
            get
            {
                UIManager ui = UI;
                return ui != null ? ui.MessageInput : null;
            }
        }

        // ---------------- GameSettings ----------------

        /// <summary>Sets how many lines the game keeps in each chat tab (its own defaults are 50 and 25).</summary>
        public static void SetLineLimits(int global, int local)
        {
            if (GlobalLimitRef == null || LocalLimitRef == null)
                return;
            GameSettings settings;
            try
            {
                settings = ScriptableSingleton<GameSettings>.I;
            }
            catch (Exception)
            {
                return;
            }
            if (settings == null)
                return;
            if (GlobalLimitRef(settings) != global)
                GlobalLimitRef(settings) = global;
            if (LocalLimitRef(settings) != local)
                LocalLimitRef(settings) = local;
        }

        // ---------------- Players ----------------

        /// <summary>Steam ID of the player with the given index in the lobby's player list, or an empty string.</summary>
        public static string SteamIdAt(int index)
        {
            try
            {
                PlayerPanelController panel = NetworkSingleton<PlayerPanelController>.I;
                List<string> ids = panel != null ? panel.PlayerSteamIDs : null;
                return ids != null && index >= 0 && index < ids.Count ? ids[index] ?? string.Empty : string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static PlayerController PlayerBySteamId(string steamId)
        {
            if (string.IsNullOrEmpty(steamId))
                return null;
            PlayerPanelController panel = NetworkSingleton<PlayerPanelController>.I;
            if (panel == null || panel.PlayerSteamIDs == null || panel.PlayerControllers == null)
                return null;
            int index = panel.PlayerSteamIDs.IndexOf(steamId);
            return index >= 0 && index < panel.PlayerControllers.Count ? panel.PlayerControllers[index] : null;
        }

        /// <summary>True for players you ignore or muted in the chat (the game hides their messages).</summary>
        public static bool IsBlocked(string steamId)
        {
            if (string.IsNullOrEmpty(steamId))
                return false;
            try
            {
                DataManager data = MonoSingleton<DataManager>.I;
                BanData ban = data != null ? data.BanData : null;
                if (ban == null)
                    return false;
                return (ban.IgnorePlayers != null && ban.IgnorePlayers.Contains(steamId)) ||
                       (ban.MutedPlayers != null && ban.MutedPlayers.Contains(steamId));
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Opens a player's ID card, as clicking their message does.</summary>
        public static void OpenIdCard(string name, string steamId)
        {
            try
            {
                TemporaryPhoto photo = MonoSingleton<TemporaryPhoto>.I;
                if (photo == null)
                    return;
                PlayerController player = PlayerBySteamId(steamId);
                if (player != null)
                    photo.CapturePhoto(player);
                else
                    photo.ButtonOpenIdCheck(name, steamId);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Could not open the ID card: " + e.Message);
            }
        }

        // ---------------- Input ----------------

        /// <summary>True while any text field has keyboard focus (chat, journal, to-do list...).</summary>
        public static bool IsAnyTextFieldFocused()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;
            GameObject selected = eventSystem.currentSelectedGameObject;
            if (selected == null)
                return false;
            TMP_InputField tmpInput = selected.GetComponent<TMP_InputField>();
            if (tmpInput != null && tmpInput.isFocused)
                return true;
            InputField legacyInput = selected.GetComponent<InputField>();
            return legacyInput != null && legacyInput.isFocused;
        }

        // ---------------- Notifications ----------------

        /// <summary>
        /// Shows a client-side line in the chat tab the player is looking at. Nothing is sent to other players, and the
        /// line is not saved in the history.
        /// </summary>
        public static void Notify(string message)
        {
            TextChannelManager chat = Chat;
            if (chat == null)
                return;
            // noparse: command syntax such as <percent> must not be read as rich text tags.
            string text = "<color=#9FD1FF>[ChatPlus]</color> <noparse>" + message + "</noparse>";
            try
            {
                if (chat.Islocal && TryAddLocalLine(chat, text))
                    return;
                ChatLines.Suppress = true;
                try
                {
                    chat.AddNotification(text);
                }
                finally
                {
                    ChatLines.Suppress = false;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Could not show a chat notification: " + e.Message);
            }
        }

        /// <summary>Mirrors TextChannelManager.AddNotification, but for the Local tab.</summary>
        static bool TryAddLocalLine(TextChannelManager chat, string text)
        {
            Transform parent = Content(UI, true);
            TMP_Text prefab = TextPrefab(chat);
            List<GameObject> lines = Lines(chat, true);
            if (parent == null || prefab == null || lines == null)
                return false;
            TMP_Text line = UnityEngine.Object.Instantiate(prefab, parent);
            Button button = line.GetComponent<Button>();
            if (button != null)
                button.interactable = false;
            line.text = text;
            // Time and right-click copy like the lines in the Global tab; not saved in the history.
            ChatLine.Attach(line, new ChatEntry
            {
                Time = DateTime.Now,
                IsLocal = true,
                Kind = ChatKind.Notification,
                Message = text,
                Line = text
            });
            lines.Add(line.gameObject);
            ChatLines.Trim(lines, Plugin.Instance.LocalLimit.Value);
            return true;
        }

        static T Get<T>(AccessTools.FieldRef<UIManager, T> field, UIManager ui) where T : class
        {
            return field != null && ui != null ? field(ui) : null;
        }

        static AccessTools.FieldRef<TOwner, TField> Field<TOwner, TField>(string name)
        {
            try
            {
                return AccessTools.FieldRefAccess<TOwner, TField>(name);
            }
            catch (Exception)
            {
                MissingFields.Add(typeof(TOwner).Name + "." + name);
                return null;
            }
        }
    }
}
