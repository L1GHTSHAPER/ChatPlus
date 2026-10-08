using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace ChatPlus
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("OnTogether.exe")]
    [BepInDependency(ChatCommands.CommandApiGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "ontogether.chatplus";
        public const string PluginName = "ChatPlus";
        public const string PluginVersion = "1.4.2";

        internal const int MinWidth = 70;
        internal const int MaxWidth = 300;
        internal const int MinHeight = 50;
        internal const int MaxHeight = 200;
        internal const int MinTextSize = 60;
        internal const int MaxTextSize = 200;
        // Lines kept in memory and in the history file.
        const int HistoryCapacity = 5000;
        // Sent messages remembered for Up/Down.
        const int SentCapacity = 50;

        // The config file is checked this often for edits made in the mod manager while the game is running.
        const float ConfigPollInterval = 1f;
        // A changed file is reloaded once it has stayed unchanged this long (the editor may still be writing it).
        const float ConfigSettleTime = 0.5f;
        // Changes made in game are written this long after the last one, so dragging a slider writes the file once.
        const float SaveDelay = 0.75f;
        // A config file that could not be read or written (e.g. locked by another program) is tried again after this.
        const float RetryDelay = 2f;
        // Redrawing hundreds of chat lines is done once a slider has rested this long, not on every step.
        const float RestyleDelay = 0.2f;

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        internal ConfigEntry<UiLanguage> Language;
        internal ConfigEntry<KeyboardShortcut> WindowKey;

        internal ConfigEntry<bool> Timestamps;
        internal ConfigEntry<string> TimeFormat;
        internal ConfigEntry<string> TimeColor;
        internal ConfigEntry<int> TimeSize;
        internal ConfigEntry<bool> TimeOnNotifications;

        internal ConfigEntry<int> GlobalLimit;
        internal ConfigEntry<int> LocalLimit;
        internal ConfigEntry<bool> RestoreHistory;
        internal ConfigEntry<bool> RestoreNotifications;
        internal ConfigEntry<bool> SaveHistory;
        internal ConfigEntry<bool> KeepAfterRestart;
        internal ConfigEntry<bool> DailyLogs;

        internal ConfigEntry<int> Width;
        internal ConfigEntry<int> Height;
        internal ConfigEntry<int> TextSize;
        internal ConfigEntry<int> BackgroundOpacity;
        internal ConfigEntry<bool> ResizeHandle;
        internal ConfigEntry<bool> RememberPosition;
        internal ConfigEntry<string> Position;
        internal ConfigEntry<bool> CullHiddenLines;

        internal ConfigEntry<bool> ShowGlobalCounter;
        internal ConfigEntry<bool> ShowLocalCounter;

        internal ConfigEntry<bool> HighlightMentions;
        internal ConfigEntry<string> HighlightColor;
        internal ConfigEntry<string> Keywords;
        internal ConfigEntry<bool> RecallSent;
        internal ConfigEntry<bool> RightClickCopy;
        internal ConfigEntry<bool> SelectText;

        internal ConfigEntry<bool> OutgoingEnabled;
        internal ConfigEntry<MessageFont> OutgoingFont;
        internal ConfigEntry<MessageColorMode> OutgoingColorMode;
        internal ConfigEntry<string> OutgoingColor;
        internal ConfigEntry<string> OutgoingEndColor;
        internal ConfigEntry<bool> OutgoingBold;
        internal ConfigEntry<bool> OutgoingItalic;

        internal ChatHistory History { get; private set; }
        internal SentHistory Sent { get; } = new SentHistory(SentCapacity);
        internal IList<string> KeywordList => _keywords;

        GameObject _host;
        SettingsWindow _window;
        ModDock _dock;
        Harmony _harmony;
        List<string> _keywords = new List<string>();
        // The chat input whose caret is moved to the end after a recalled message was put into it.
        TMP_InputField _caretToEnd;
        // Settings changed in game and not saved yet, with their new values. When the file is edited in the mod
        // manager at the same time, these win and every other setting takes the value from the file.
        readonly Dictionary<ConfigEntryBase, object> _unsaved = new Dictionary<ConfigEntryBase, object>();
        bool _reloading;
        float _saveAt = -1f;
        float _restyleAt = -1f;
        float _resizeTextAt = -1f;
        float _nextConfigPoll;
        float _reloadAt = -1f;
        DateTime _configStamp;
        DateTime _pendingStamp;
        bool _fileErrorLogged;
        static string _lastError;

        void Awake()
        {
            Instance = this;
            Log = Logger;
            BindConfig();
            UiEnvironment.LanguageOverride = () => Lang.Current == Lang.Russian;
            // Bind() has written any missing keys; from now on saves are batched (see SaveDelay).
            Config.SaveOnConfigSet = false;
            _keywords = TextUtil.ParseKeywords(Keywords.Value);
            _configStamp = ConfigStamp();

            History = new ChatHistory(Path.Combine(Paths.BepInExRootPath, PluginName), HistoryCapacity, message => Log.LogWarning(message));
            History.DailyLogs = DailyLogs.Value;
            History.Open(SaveHistory.Value && KeepAfterRestart.Value, SaveHistory.Value);
            if (History.Count > 0)
                Log.LogInfo($"Loaded {History.Count} chat lines from the previous session.");

            _host = new GameObject(PluginName) { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(_host);
            _window = _host.AddComponent<SettingsWindow>();
            _window.enabled = false;
            _dock = ModDock.Register(PluginName, "chat", _window.Toggle, () => _window.enabled,
                () => PluginName + " · " + WindowKey.Value, () => GameAccess.Chat != null);
            _host.AddComponent<Toast>();

            _harmony = new Harmony(PluginGuid);
            UiEnvironment.InstallInputGuard(_harmony);
            Patch(typeof(AddMessagePatch), "new messages get no time and are not saved");
            Patch(typeof(AddNotificationPatch), "notifications get no time and are not saved");
            Patch(typeof(ChatStartPatch), "the history is not brought back after a lobby change");
            Patch(typeof(ResetMaterialsPatch), "a long chat is slower when it fades in and out");
            Patch(typeof(UiStartPatch), "the chat window cannot be resized");
            Patch(typeof(BeginMovePatch), "a resized chat may not be dragged to the right edge");
            Patch(typeof(EndMovePatch), "the chat position is not remembered");
            Patch(typeof(EnterPatch), "the /chatplus command and Up/Down recall will not work");
            Patch(typeof(OutgoingMessagePatch), "outgoing message formatting will not work");
            Patch(typeof(ReceivedCounterPatch), "collapsed chat counters update only at the end of the frame");
            Patch(typeof(HideChatCounterPatch), "collapsed chat counters update only at the end of the frame");
            GameAccess.LogMissingFields();
            ChatCommands.RegisterWithCommandApi();

            Config.SettingChanged += OnSettingChanged;
            Log.LogInfo($"{PluginName} {PluginVersion} loaded. Settings window: {WindowKey.Value} or /chatplus");
        }

        void BindConfig()
        {
            Language = Config.Bind("General", "Language", UiLanguage.Auto,
                "Language of the settings window and of the mod's chat messages. Auto follows the game's language.");
            WindowKey = Config.Bind("General", "SettingsWindow", new KeyboardShortcut(KeyCode.F3),
                "Opens or closes the settings window (same as typing /chatplus). Ignored while typing.");

            Timestamps = Config.Bind("Time", "Enabled", true,
                "Show the time in front of every chat message.");
            TimeFormat = Config.Bind("Time", "Format", "HH:mm",
                "How the time looks, as a .NET format: HH:mm = 14:05, HH:mm:ss = 14:05:09, h:mm tt = 2:05 PM, [HH:mm] = [14:05]. " +
                "Messages from another day also show the date.");
            TimeColor = Config.Bind("Time", "Color", Presets.DefaultTimeColor,
                "Color of the time: #RRGGBB, or #RRGGBBAA with transparency. Empty: the color of the text.");
            TimeSize = Config.Bind("Time", "Size", 85,
                new ConfigDescription("Size of the time, in percent of the message text.", new AcceptableValueRange<int>(50, 100)));
            TimeOnNotifications = Config.Bind("Time", "OnNotifications", true,
                "Also show the time in front of the game's notifications (someone joined, XP...).");

            GlobalLimit = Config.Bind("History", "GlobalLines", 200,
                new ConfigDescription("How many lines the Global tab keeps (the game keeps 50). Very large values make the chat slower.",
                    new AcceptableValueRange<int>(25, 1000)));
            LocalLimit = Config.Bind("History", "LocalLines", 100,
                new ConfigDescription("How many lines the Local tab keeps (the game keeps 25).", new AcceptableValueRange<int>(25, 1000)));
            RestoreHistory = Config.Bind("History", "RestoreAfterLobbyChange", true,
                "When you join a lobby (again), the chat shows the messages from earlier in this game session.");
            RestoreNotifications = Config.Bind("History", "RestoreNotifications", false,
                "Also bring back the game's notifications (someone joined, XP...), not only messages.");
            SaveHistory = Config.Bind("History", "SaveToFile", true,
                "Write the chat of this game session to BepInEx/ChatPlus/chat-history.tsv (opens in Excel or any text editor). " +
                "On the next game start it is moved to chat-history.previous.tsv.");
            KeepAfterRestart = Config.Bind("History", "KeepAfterRestart", false,
                "Do not start a new history when the game starts: the chat shows the messages of the previous game session too. " +
                "Needs SaveToFile.");
            DailyLogs = Config.Bind("History", "DailyLogs", false,
                "Also write every chat line to readable text logs, one file per day: BepInEx/ChatPlus/logs/yyyy-MM-dd.txt.");

            Width = Config.Bind("Window", "Width", 100,
                new ConfigDescription("Width of the chat window, in percent of the game's size.", new AcceptableValueRange<int>(MinWidth, MaxWidth)));
            Height = Config.Bind("Window", "Height", 100,
                new ConfigDescription("Height of the chat window, in percent of the game's size.", new AcceptableValueRange<int>(MinHeight, MaxHeight)));
            TextSize = Config.Bind("Window", "TextSize", 100,
                new ConfigDescription("Size of the chat text, in percent of the game's size.", new AcceptableValueRange<int>(MinTextSize, MaxTextSize)));
            BackgroundOpacity = Config.Bind("Window", "BackgroundOpacity", 100,
                new ConfigDescription("Chat background and ChatPlus button opacity in percent: 0 = transparent, 100 = original opacity. Text is unaffected.",
                    new AcceptableValueRange<int>(0, 100)));
            ResizeHandle = Config.Bind("Window", "ResizeHandle", true,
                "Show a handle at the top-right corner of the chat (while the chat is active) to resize it with the mouse. " +
                "Double-click the handle for the game's size.");
            RememberPosition = Config.Bind("Window", "RememberPosition", true,
                "Keep the chat where you dragged it, also after a lobby change or a game restart.");
            Position = Config.Bind("Window", "Position", string.Empty,
                "Where the chat was dragged: its bottom-left corner in UI units (x;y). Saved automatically; empty = the game's place.");
            CullHiddenLines = Config.Bind("Window", "CullHiddenLines", true,
                "Lines scrolled out of view are not drawn, which keeps a long chat fast. Turn it off if chat text shows " +
                "outside the chat window.");

            ShowGlobalCounter = Config.Bind("Notifications", "ShowGlobalCounter", true,
                "Show a separate Global message counter while the chat is collapsed. Hiding it does not stop counting; " +
                "both counters reset when the chat is expanded.");
            ShowLocalCounter = Config.Bind("Notifications", "ShowLocalCounter", true,
                "Show a separate Local message counter while the chat is collapsed. Hiding it does not stop counting; " +
                "both counters reset when the chat is expanded.");

            HighlightMentions = Config.Bind("Extras", "HighlightMentions", true,
                "Highlight messages that contain your name or one of the keywords.");
            HighlightColor = Config.Bind("Extras", "HighlightColor", Presets.DefaultHighlightColor,
                "Color of the highlight: #RRGGBB (it is made see-through), or #RRGGBBAA.");
            Keywords = Config.Bind("Extras", "Keywords", string.Empty,
                "More words that highlight a message, separated by commas, e.g. nicknames: mark, markus. " +
                "Case does not matter; only whole words match.");
            RecallSent = Config.Bind("Extras", "RecallSentMessages", true,
                "In the empty chat input field, Up and Down bring back the messages and commands you sent before.");
            RightClickCopy = Config.Bind("Extras", "RightClickCopies", true,
                "A right click copies the selection on that message, or the whole message when nothing is selected.");
            SelectText = Config.Bind("Extras", "SelectText", true,
                "Hold Shift before dragging with the left mouse button to select chat text, including across messages. " +
                "A normal left drag or touch swipe scrolls. Ctrl+C copies; Escape clears. The gesture mode is fixed when pressed.");

            OutgoingEnabled = Config.Bind("Outgoing", "Enabled", false,
                "Format messages you send using standard tags visible on unmodified clients. Commands and manually tagged messages are left alone.");
            OutgoingFont = Config.Bind("Outgoing", "Font", MessageFont.GameDefault,
                "GameDefault or LiberationSans. LiberationSans is built into the game but has no Cyrillic glyphs; Russian letters use the usual fallback font.");
            OutgoingColorMode = Config.Bind("Outgoing", "ColorMode", MessageColorMode.Original,
                "Original, Solid or Gradient. Gradients use up to eight standard color bands, reduced when needed to fit the game's 250-character message limit.");
            OutgoingColor = Config.Bind("Outgoing", "Color", "#F2C46D", "Message color, or the first gradient color: #RRGGBB.");
            OutgoingEndColor = Config.Bind("Outgoing", "EndColor", "#6AA8FF", "Last gradient color: #RRGGBB.");
            OutgoingBold = Config.Bind("Outgoing", "Bold", false, "Use the game's bold text tag on outgoing messages.");
            OutgoingItalic = Config.Bind("Outgoing", "Italic", false, "Use the game's italic text tag on outgoing messages.");
        }

        internal OutgoingStyle BuildOutgoingStyle() => new OutgoingStyle
        {
            Enabled = OutgoingEnabled.Value,
            Font = OutgoingFont.Value,
            ColorMode = OutgoingColorMode.Value,
            Color = OutgoingColor.Value,
            EndColor = OutgoingEndColor.Value,
            Bold = OutgoingBold.Value,
            Italic = OutgoingItalic.Value
        };

        void Patch(Type patchClass, string consequence)
        {
            try
            {
                _harmony.CreateClassProcessor(patchClass).Patch();
            }
            catch (Exception e)
            {
                Log.LogError($"Could not patch the game ({consequence}): {e}");
            }
        }

        internal LineStyle BuildStyle(string myName, Lang lang)
        {
            var style = new LineStyle
            {
                Timestamps = Timestamps.Value,
                TimeFormat = TimeFormat.Value,
                TimeSize = TimeSize.Value,
                TimeOnNotifications = TimeOnNotifications.Value,
                DateFormat = lang.DateFormat,
                Highlight = HighlightMentions.Value,
                MyName = myName ?? string.Empty,
                Keywords = _keywords
            };
            if (ChatFormat.TryParseColor(TimeColor.Value, out string timeColor))
                style.TimeColor = timeColor;
            if (ChatFormat.TryParseColor(HighlightColor.Value, out string highlight))
            {
                // The highlight is drawn over the text, so it must be see-through.
                if (highlight.Length == 7)
                    highlight = ChatFormat.WithAlpha(highlight, 0x45);
                else if (Convert.ToInt32(highlight.Substring(7, 2), 16) > 0x99)
                    highlight = ChatFormat.WithAlpha(highlight, 0x99);
                style.HighlightColor = highlight;
            }
            return style;
        }

        void Update()
        {
            try
            {
                HandleHotkeys();
                HandleRecall();
                ChatWindow.Tick();
                ChatSelection.Tick(_window != null && _window.enabled);
                PollConfigFile();
                float now = Time.unscaledTime;
                if (_restyleAt >= 0f && now >= _restyleAt)
                {
                    _restyleAt = -1f;
                    ChatLines.RenderAll();
                }
                if (_resizeTextAt >= 0f && now >= _resizeTextAt)
                {
                    _resizeTextAt = -1f;
                    LineSizer.Version++;
                }
                if (_saveAt >= 0f && now >= _saveAt)
                    SaveNow();
            }
            catch (Exception e)
            {
                LogError("Update failed: ", e);
            }
        }

        void LateUpdate()
        {
            HiddenChatNotifications.Refresh();
            try { ChatSelection.LateTick(); }
            catch (Exception e) { LogError("Could not update chat selection: ", e); ChatSelection.Clear(); }
            // After the input field has handled the arrow key itself (it moves the caret to the start).
            if (_caretToEnd == null)
                return;
            try
            {
                _caretToEnd.MoveTextEnd(false);
            }
            catch (Exception e)
            {
                LogError("Could not move the caret: ", e);
            }
            _caretToEnd = null;
        }

        void HandleHotkeys()
        {
            KeyboardShortcut key = WindowKey.Value;
            if (!Pressed(key) || GameAccess.IsAnyTextFieldFocused())
                return;
            ToggleWindow();
        }

        // KeyboardShortcut.IsDown() does not fire while any other key is held (e.g. W while walking), so only the
        // shortcut's own keys are checked.
        static bool Pressed(KeyboardShortcut shortcut)
        {
            KeyCode mainKey = shortcut.MainKey;
            if (mainKey == KeyCode.None || !Input.GetKeyDown(mainKey))
                return false;
            foreach (KeyCode modifier in shortcut.Modifiers)
            {
                if (!Input.GetKey(modifier))
                    return false;
            }
            return true;
        }

        /// <summary>Up / Down in the chat input field bring back sent messages.</summary>
        void HandleRecall()
        {
            if (!RecallSent.Value || Sent.Count == 0)
                return;
            bool up = Input.GetKeyDown(KeyCode.UpArrow);
            bool down = !up && Input.GetKeyDown(KeyCode.DownArrow);
            if (!up && !down)
                return;
            TMP_InputField input = GameAccess.MessageInput;
            if (input == null || !input.isFocused)
                return;
            string text = up ? Sent.Older(input.text) : Sent.Newer(input.text);
            if (text == null)
                return;
            input.text = text;
            _caretToEnd = input;
        }

        internal void ToggleWindow()
        {
            _window.Toggle();
        }

        internal void OpenHistoryFolder()
        {
            string folder = History.Folder;
            try
            {
                Directory.CreateDirectory(folder);
                System.Diagnostics.Process.Start("explorer.exe", "\"" + folder + "\"")?.Dispose();
            }
            catch (Exception)
            {
                try
                {
                    Application.OpenURL(new Uri(folder).AbsoluteUri);
                }
                catch (Exception e)
                {
                    Log.LogWarning("Could not open the history folder: " + e.Message);
                }
            }
        }

        internal void ResetWindow()
        {
            ChatWindow.ResetSize();
            ChatWindow.ResetPosition();
        }

        internal string DescribeStatus(Lang lang)
        {
            return string.Format(lang.Status,
                Timestamps.Value ? lang.On : lang.Off, ChatFormat.FormatTime(DateTime.Now, TimeFormat.Value),
                GlobalLimit.Value, LocalLimit.Value, Width.Value, Height.Value, TextSize.Value, History.Count,
                SaveHistory.Value ? History.FilePath : lang.Off);
        }

        void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            ConfigEntryBase changed = e.ChangedSetting;
            if (!_reloading)
            {
                _unsaved[changed] = changed.BoxedValue;
                _saveAt = Time.unscaledTime + SaveDelay;
            }
            try
            {
                ApplySetting(changed);
            }
            catch (Exception ex)
            {
                LogError("Could not apply a setting: ", ex);
            }
        }

        void ApplySetting(ConfigEntryBase changed)
        {
            if (changed == Keywords)
            {
                _keywords = TextUtil.ParseKeywords(Keywords.Value);
                RestyleLines();
            }
            else if (changed == Timestamps || changed == TimeFormat || changed == TimeColor || changed == TimeSize ||
                     changed == TimeOnNotifications || changed == HighlightMentions || changed == HighlightColor)
            {
                RestyleLines();
            }
            else if (changed == Language)
            {
                Lang.Invalidate();
                RestyleLines();
            }
            else if (changed == GlobalLimit || changed == LocalLimit)
            {
                ChatLines.ApplyLimits();
            }
            else if (changed == SaveHistory)
            {
                History.SetSaving(SaveHistory.Value);
            }
            else if (changed == DailyLogs)
            {
                History.DailyLogs = DailyLogs.Value;
            }
            else if (changed == Width || changed == Height || changed == ResizeHandle)
            {
                ChatWindow.Apply();
            }
            else if (changed == TextSize)
            {
                _resizeTextAt = Time.unscaledTime + RestyleDelay;
            }
            else if (changed == BackgroundOpacity)
            {
                ChatWindow.ApplyOpacity();
            }
            else if (changed == SelectText && !SelectText.Value)
            {
                ChatSelection.Clear();
            }
            else if (changed == RememberPosition)
            {
                if (!RememberPosition.Value)
                    Position.Value = string.Empty;
            }
            else if (changed == Position)
            {
                ChatWindow.ApplyPosition();
            }
            else if (changed == CullHiddenLines)
            {
                ChatWindow.ApplyCulling();
            }
            else if (changed == ShowGlobalCounter || changed == ShowLocalCounter)
            {
                HiddenChatNotifications.Refresh();
            }
        }

        /// <summary>New lines use the changed look at once; the existing ones are redrawn shortly (see RestyleDelay).</summary>
        void RestyleLines()
        {
            ChatLines.StyleVersion++;
            _restyleAt = Time.unscaledTime + RestyleDelay;
        }

        /// <summary>Applies edits made to the config file while the game is running (e.g. in the mod manager).</summary>
        void PollConfigFile()
        {
            float now = Time.unscaledTime;
            if (_reloadAt >= 0f)
            {
                if (now < _reloadAt)
                    return;
                DateTime stamp = ConfigStamp();
                if (stamp == _configStamp)
                {
                    // Already read, or replaced by our own save in the meantime.
                    _reloadAt = -1f;
                    return;
                }
                if (stamp != _pendingStamp)
                {
                    _pendingStamp = stamp;
                    _reloadAt = now + ConfigSettleTime;
                    return;
                }
                _reloadAt = TryReloadConfig() ? -1f : now + RetryDelay;
                return;
            }

            if (now < _nextConfigPoll)
                return;
            _nextConfigPoll = now + ConfigPollInterval;
            DateTime current = ConfigStamp();
            if (current == _configStamp)
                return;
            _pendingStamp = current;
            _reloadAt = now + ConfigSettleTime;
        }

        /// <summary>
        /// Reads the config file again, keeping the settings changed in game that are not saved yet.
        /// Returns false when the file could not be read.
        /// </summary>
        bool TryReloadConfig()
        {
            if (!File.Exists(Config.ConfigFilePath))
            {
                SaveNow();
                return true;
            }
            // Taken before reading, so a write that happens during the read is noticed next time.
            DateTime stamp = ConfigStamp();
            _reloading = true;
            try
            {
                Config.Reload();
                foreach (KeyValuePair<ConfigEntryBase, object> change in _unsaved)
                    change.Key.BoxedValue = change.Value;
            }
            catch (Exception e)
            {
                LogFileErrorOnce("Could not read the config file, will try again: " + e.Message);
                return false;
            }
            finally
            {
                _reloading = false;
            }
            _configStamp = stamp;
            _fileErrorLogged = false;
            Log.LogInfo("Settings reloaded from the config file.");
            return true;
        }

        void SaveNow()
        {
            _saveAt = -1f;
            // An edit made in the mod manager since our last save is read first, so that saving does not undo it.
            if (File.Exists(Config.ConfigFilePath) && ConfigStamp() != _configStamp && !TryReloadConfig())
            {
                _saveAt = Time.unscaledTime + RetryDelay;
                return;
            }
            try
            {
                Config.Save();
                _unsaved.Clear();
                _configStamp = ConfigStamp();
                _fileErrorLogged = false;
            }
            catch (Exception e)
            {
                LogFileErrorOnce("Could not save the settings, will try again: " + e.Message);
                _saveAt = Time.unscaledTime + RetryDelay;
            }
        }

        void LogFileErrorOnce(string message)
        {
            if (_fileErrorLogged)
                return;
            _fileErrorLogged = true;
            Log.LogWarning(message);
        }

        DateTime ConfigStamp()
        {
            try
            {
                return File.GetLastWriteTimeUtc(Config.ConfigFilePath);
            }
            catch (Exception)
            {
                return default;
            }
        }

        /// <summary>Logs each distinct error once instead of every frame or every message.</summary>
        internal static void LogError(string what, Exception e)
        {
            string message = what + e.GetType().Name + ": " + e.Message;
            if (message == _lastError)
                return;
            _lastError = message;
            Log?.LogError(what + e);
        }

        void OnApplicationQuit()
        {
            if (_saveAt >= 0f)
                SaveNow();
        }

        void OnDestroy()
        {
            if (_dock != null) Destroy(_dock.gameObject);
            ChatSelection.Clear();
            HiddenChatNotifications.Clear();
            Config.SettingChanged -= OnSettingChanged;
            if (_saveAt >= 0f)
                SaveNow();
            if (_harmony != null)
                _harmony.UnpatchSelf();
            ChatWindow.Unload();
            if (_host != null)
                Destroy(_host);
        }
    }
}
