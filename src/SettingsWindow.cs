using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace ChatPlus
{
    /// <summary>
    /// In-game settings window (IMGUI). The component is enabled only while the window is open, so it costs
    /// nothing otherwise. Every control writes straight to the config entries, which apply at once.
    /// </summary>
    internal sealed class SettingsWindow : MonoBehaviour
    {
        const int WindowId = 0x43504C53;
        const float Width = 600f;
        float LabelWidth => Mathf.Clamp((_rect.width - 40f) * 0.32f, 90f, 170f);
        const float ArrowWidth = 26f;
        const float ValueWidth = 58f;
        const float Indent = 26f;
        // How long the "click again" state of the Clear history button lasts.
        const float ConfirmTime = 3f;

        static readonly GUILayoutOption[] Shrinkable = { GUILayout.MinWidth(40f), GUILayout.ExpandWidth(true) };

        UiSkin _skin;
        GUISkin _guiSkin;
        WindowInputShield _shield;
        GUI.WindowFunction _drawWindow;
        Rect _rect;
        bool _placed;
        float _screenHeight;
        Vector2 _scroll;
        int _tab;
        int _pendingTab = -1;
        readonly Vector2[] _tabScrolls = new Vector2[5];
        bool _windowExpanded;
        bool _timeExpanded;
        bool _previewDrawn;
        // The IMGUI control of this window that holds the mouse (slider or window drag), released on close.
        int _hotControl;
        float _confirmClearUntil;
        readonly MessagePreview _messagePreview = new MessagePreview();
        readonly Dictionary<ConfigEntry<string>, string> _colorDrafts = new Dictionary<ConfigEntry<string>, string>();
        readonly Dictionary<ConfigEntry<string>, string> _colorValues = new Dictionary<ConfigEntry<string>, string>();
        readonly HashSet<ConfigEntry<string>> _expandedColors = new HashSet<ConfigEntry<string>>();
        string _editorDraft = string.Empty;

        public void Toggle()
        {
            SetOpen(!enabled);
        }

        public void SetOpen(bool open)
        {
            if (open == enabled)
                return;
            enabled = open;
            if (_shield == null) _shield = new WindowInputShield("ChatPlus");
            _shield.Set(open);
            if (open)
            {
                ChatSelection.Clear();
                Lang.Invalidate();
                TMPDraftFromChat();
                return;
            }
            // Closed with the hotkey while a slider is held: nothing would release the mouse otherwise, and other
            // IMGUI windows would ignore clicks.
            if (_hotControl != 0 && GUIUtility.hotControl == _hotControl)
                GUIUtility.hotControl = 0;
            _hotControl = 0;
        }

        void OnDisable()
        {
            _shield?.Set(false);
            _messagePreview.Hide();
        }

        void LateUpdate()
        {
            _messagePreview.RenderPending();
        }

        void OnGUI()
        {
            // A manual preview render can invoke GUI callbacks; never draw or queue F3 inside it.
            if (_messagePreview.Rendering || UiEnvironment.PreviewRendering || Plugin.Instance == null)
                return;
            if (_skin == null)
            { _skin = new UiSkin(); _guiSkin = _skin.CreateGuiSkin(GUI.skin); }
            if (_drawWindow == null)
                _drawWindow = DrawWindow;

            // Keep the window the same physical size on high resolutions (laid out for 1080p).
            float scale = UiEnvironment.Scale;
            Matrix4x4 previousMatrix = GUI.matrix;
            GUISkin previousSkin = GUI.skin;
            try
            {
            GUI.skin = _guiSkin;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float screenWidth = Screen.width / scale;
            _screenHeight = Screen.height / scale;
            if (!_placed)
            {
                _rect = new Rect(Mathf.Round((screenWidth - Width) * 0.5f), Mathf.Round(_screenHeight * 0.06f), Width, 0f);
                _placed = true;
            }

            float windowWidth = Mathf.Min(Width, screenWidth - 24f);
            _rect.width = windowWidth;
            if (Event.current.type == EventType.Repaint) _previewDrawn = false;
            _rect = GUILayout.Window(WindowId, _rect, _drawWindow, GUIContent.none, _skin.Window, GUILayout.Width(windowWidth));
            _rect.x = Mathf.Clamp(_rect.x, 0f, Mathf.Max(0f, screenWidth - _rect.width));
            _rect.y = Mathf.Clamp(_rect.y, 0f, Mathf.Max(0f, _screenHeight - _rect.height));
            }
            finally { GUI.matrix = previousMatrix; GUI.skin = previousSkin; }

            if (Event.current.type == EventType.Repaint)
            {
                _shield?.Place(_rect, scale);
                if (!enabled || !_previewDrawn) _messagePreview.Hide();
            }
        }

        void DrawWindow(int id)
        {
            Plugin plugin = Plugin.Instance;
            Lang lang = Lang.Current;
            int hotControlBefore = GUIUtility.hotControl;

            // Change the control tree only on Layout, after the old tab has finished its mouse event.
            if (Event.current.type == EventType.Layout && _pendingTab >= 0)
            {
                _tabScrolls[_tab] = _scroll;
                _tab = _pendingTab;
                _pendingTab = -1;
                _scroll = _tabScrolls[_tab];
                _confirmClearUntil = 0f;
                GUI.FocusControl(null);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("C+", _skin.Logo);
            GUILayout.BeginVertical();
            GUILayout.Label("ChatPlus", _skin.Title);
            GUILayout.Label(lang.SettingsSubtitle, _skin.Subtitle);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", _skin.CloseButton))
                SetOpen(false);
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            DrawTabs(lang);
            GUILayout.Space(8f);

            // A stable body height prevents the window jumping around when tabs or advanced controls change.
            float viewHeight = Mathf.Min(580f, Mathf.Max(100f, _screenHeight - (_rect.width < 480f ? 220f : 180f)));
            _scroll = GUILayout.BeginScrollView(_scroll, false, false, GUI.skin.horizontalScrollbar,
                GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.Height(viewHeight));
            GUILayout.BeginVertical();
            DrawSettings(plugin, lang);
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            GUILayout.Space(12f);
            GUILayout.BeginHorizontal();
            GUILayout.Label(lang.AutoSaved, _skin.Hint);
            GUILayout.FlexibleSpace();
            GUILayout.Label("LightShaper", _skin.Hint);
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 66f));

            // A control in this window took or released the mouse during this event.
            if (GUIUtility.hotControl != hotControlBefore)
                _hotControl = GUIUtility.hotControl;
        }

        void DrawTabs(Lang lang)
        {
            string[] names = { lang.TabAppearance, lang.TabMessages, lang.TabNickname, lang.TabHistory, lang.TabControls };
            int columns = _rect.width < 480f ? 2 : names.Length;
            for (int row = 0; row < (names.Length + columns - 1) / columns; row++)
            {
                GUILayout.BeginHorizontal();
                for (int column = 0; column < columns; column++)
                {
                    int tab = row * columns + column;
                    if (tab >= names.Length) { GUILayout.FlexibleSpace(); break; }
                    if (GUILayout.Button(names[tab], tab == _tab ? _skin.SelectedTab : _skin.Tab))
                        _pendingTab = tab;
                }
                GUILayout.EndHorizontal();
            }
        }

        void DrawSettings(Plugin plugin, Lang lang)
        {
            switch (_tab)
            {
                case 0: DrawAppearance(plugin, lang); break;
                case 1: DrawMessageEditor(plugin, lang); break;
                case 2: DrawNicknameEditor(plugin, lang); break;
                case 3: DrawHistory(plugin, lang); break;
                case 4: DrawControls(plugin, lang); break;
            }
        }

        void DrawAppearance(Plugin plugin, Lang lang)
        {
            bool enabled = GUI.enabled;
            GUILayout.BeginVertical(_skin.Panel);
            GUILayout.Label(lang.ReadingTitle, _skin.SectionTitle);
            LargeSlider(plugin.TextSize, lang.TextSize, Plugin.MinTextSize, Plugin.MaxTextSize, 5, "%");
            LargeSlider(plugin.BackgroundOpacity, lang.BackgroundOpacity, 0, 100, 5, "%");
            GUILayout.Label(lang.BackgroundOpacityHint, _skin.Hint);
            GUILayout.Space(8f);
            GUILayout.Label(lang.PreviewLive, _skin.Label);
            // Only the background changes alpha; the sample keeps the same text opacity as the real chat.
            Rect preview = GUILayoutUtility.GetRect(10f, Mathf.Max(102f, plugin.TextSize.Value), GUILayout.ExpandWidth(true));
            GUI.Box(preview, GUIContent.none, _skin.Preview);
            Color tint = GUI.color;
            GUI.color = new Color(tint.r, tint.g, tint.b, tint.a * plugin.BackgroundOpacity.Value / 100f);
            GUI.Box(preview, GUIContent.none, _skin.PreviewPaper);
            GUI.color = tint;
            if (Event.current.type == EventType.Repaint)
            {
                string sample = lang.PreviewChat;
                if (plugin.Timestamps.Value)
                {
                    var style = plugin.BuildStyle(string.Empty, lang);
                    if (style.TimeColor == Presets.DefaultTimeColor) style.TimeColor = "#796B5B";
                    string time = ChatFormat.Stamp(DateTime.Now, DateTime.Now, style);
                    sample = time + sample.Replace("\n", "\n" + time);
                }
                _messagePreview.Draw(new Rect(preview.x + 12f, preview.y + 10f, preview.width - 24f, preview.height - 20f),
                    sample, 16f * plugin.TextSize.Value / 100f, UiSkin.TextColor);
                _previewDrawn = true;
            }
            GUILayout.Space(8f);
            ToggleRow(plugin.Timestamps, lang.ShowTime);
            _timeExpanded = GUILayout.Toggle(_timeExpanded, (_timeExpanded ? "▼ " : "► ") + lang.TimeAdvanced, _skin.ToggleLabel);
            if (_timeExpanded)
            {
                GUI.enabled = enabled && plugin.Timestamps.Value;
                ChoiceRow(lang.TimeFormat, ChatFormat.FormatTime(DateTime.Now, plugin.TimeFormat.Value), step =>
                    plugin.TimeFormat.Value = Presets.Step(Presets.TimeFormats, plugin.TimeFormat.Value, step, false));
                ColorRow(lang.TimeColor, plugin.TimeColor, Presets.TimeColors, lang);
                IntRow(plugin.TimeSize, lang.TimeSize, 50, 100, 5, "%");
                ToggleRow(plugin.TimeOnNotifications, lang.TimeOnNotifications);
                GUI.enabled = enabled;
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(_skin.Panel);
            _windowExpanded = GUILayout.Toggle(_windowExpanded, (_windowExpanded ? "▼ " : "► ") + lang.WindowAdvanced, _skin.ToggleLabel);
            if (_windowExpanded)
            {
                IntRow(plugin.Width, lang.Width, Plugin.MinWidth, Plugin.MaxWidth, 5, "%");
                IntRow(plugin.Height, lang.Height, Plugin.MinHeight, Plugin.MaxHeight, 5, "%");
                ToggleRow(plugin.ResizeHandle, lang.ResizeHandle);
                GUILayout.Label(lang.ResizeHint, _skin.Hint);
                ToggleRow(plugin.RememberPosition, lang.RememberPosition);
                ToggleRow(plugin.CullHiddenLines, lang.CullHiddenLines);
                GUILayout.Space(6f);
                if (GUILayout.Button(lang.ResetWindow, _skin.Button)) plugin.ResetWindow();
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(_skin.Panel);
            GUILayout.Label(lang.SectionNotifications, _skin.SectionTitle);
            ToggleRow(plugin.ShowGlobalCounter, lang.ShowGlobalCounter);
            ToggleRow(plugin.ShowLocalCounter, lang.ShowLocalCounter);
            GUILayout.Label(lang.CountersHint, _skin.Hint);
            GUILayout.EndVertical();
        }

        void DrawHistory(Plugin plugin, Lang lang)
        {
            bool enabled = GUI.enabled;
            GUILayout.BeginVertical(_skin.Panel);
            GUILayout.Label(lang.SectionHistory, _skin.SectionTitle);
            IntRow(plugin.GlobalLimit, lang.GlobalLimit, 25, 1000, 25, string.Empty);
            IntRow(plugin.LocalLimit, lang.LocalLimit, 25, 1000, 25, string.Empty);
            GUILayout.Label(lang.LimitsHint, _skin.Hint);
            ToggleRow(plugin.RestoreHistory, lang.RestoreHistory);
            GUI.enabled = enabled && plugin.RestoreHistory.Value;
            ToggleRow(plugin.RestoreNotifications, lang.RestoreNotifications, Indent);
            GUI.enabled = enabled;
            ToggleRow(plugin.SaveHistory, lang.SaveHistory);
            GUI.enabled = enabled && plugin.SaveHistory.Value;
            ToggleRow(plugin.KeepAfterRestart, lang.KeepAfterRestart, Indent);
            GUI.enabled = enabled;
            ToggleRow(plugin.DailyLogs, lang.DailyLogs);
            GUILayout.Space(6f);
            if (GUILayout.Button(lang.OpenFolder, _skin.Button))
                plugin.OpenHistoryFolder();
            GUILayout.Space(8f);
            bool wide = _rect.width >= 480f;
            if (wide) GUILayout.BeginHorizontal();
            if (GUILayout.Button(lang.ClearChat, _skin.Button))
                ChatLines.Clear();
            // The history cannot be brought back, so clearing it takes a second click.
            bool confirming = Time.unscaledTime < _confirmClearUntil;
            if (GUILayout.Button(confirming ? lang.ConfirmClear : lang.ClearHistory, _skin.Button))
            {
                if (confirming)
                {
                    plugin.History.Clear();
                    _confirmClearUntil = 0f;
                }
                else
                {
                    _confirmClearUntil = Time.unscaledTime + ConfirmTime;
                }
            }
            if (wide)
            {
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.Label(string.Format(lang.HistoryCount, plugin.History.Count), _skin.Hint);
            GUILayout.EndVertical();
        }

        void DrawControls(Plugin plugin, Lang lang)
        {
            bool enabled = GUI.enabled;
            GUILayout.BeginVertical(_skin.Panel);
            GUILayout.Label(lang.ControlsTitle, _skin.SectionTitle);
            ShortcutRow(lang.ScrollGesture, lang.DragKey);
            ShortcutRow(lang.SelectGesture, plugin.SelectText.Value ? lang.SelectKey : lang.SelectionOff);
            ShortcutRow(lang.CopyGesture, "Ctrl + C");
            ShortcutRow(lang.SettingsSubtitle, plugin.WindowKey.Value.ToString());
            GUILayout.Space(8f);
            ToggleRow(plugin.SelectText, lang.SelectText);
            ToggleRow(plugin.RightClickCopy, lang.RightClickCopy);
            ToggleRow(plugin.RecallSent, lang.RecallSent);
            GUILayout.Label(lang.SelectTextHint, _skin.Hint);
            GUILayout.Label(string.Format(lang.WindowHint, plugin.WindowKey.Value), _skin.Hint);
            GUILayout.EndVertical();

            // Extras
            GUILayout.BeginVertical(_skin.Panel);
            GUILayout.Label(lang.SectionExtras, _skin.SectionTitle);
            ToggleRow(plugin.HighlightMentions, lang.HighlightMentions);
            GUI.enabled = enabled && plugin.HighlightMentions.Value;
            ColorRow(lang.HighlightColor, plugin.HighlightColor, Presets.HighlightColors, lang);
            GUILayout.BeginHorizontal(_skin.Row);
            GUILayout.Label(lang.Keywords, _skin.Label, GUILayout.Width(LabelWidth));
            IList<string> keywords = plugin.KeywordList;
            GUILayout.Label(keywords.Count > 0 ? TextUtil.JoinKeywords(keywords) : lang.None, _skin.ToggleLabel, Shrinkable);
            GUILayout.EndHorizontal();
            GUILayout.Label(lang.KeywordsHint, _skin.Hint);
            GUI.enabled = enabled;
            GUILayout.EndVertical();
        }

        void ShortcutRow(string label, string key)
        {
            GUILayout.BeginHorizontal(_skin.Row);
            GUILayout.Label(label, _skin.Label, Shrinkable);
            GUILayout.Label(key, _skin.Value, GUILayout.Width(_rect.width < 480f ? 125f : 180f));
            GUILayout.EndHorizontal();
        }

        void LargeSlider(ConfigEntry<int> entry, string label, int min, int max, int step, string suffix)
        {
            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _skin.Label, Shrinkable);
            GUILayout.Label(entry.Value + suffix, _skin.Value, GUILayout.Width(ValueWidth));
            GUILayout.EndHorizontal();
            float raw = GUILayout.HorizontalSlider(entry.Value, min, max, _skin.Slider, _skin.Thumb);
            if (Mathf.Abs(raw - entry.Value) > 0.001f)
                entry.Value = Mathf.Clamp(Mathf.RoundToInt(raw / step) * step, min, max);
        }

        void TMPDraftFromChat()
        {
            var input = GameAccess.MessageInput;
            if (input != null && !string.IsNullOrEmpty(input.text) && !ChatCommands.TryParse(input.text, out _))
                _editorDraft = input.text;
        }

        void DrawMessageEditor(Plugin plugin, Lang lang)
        {
            bool enabled = GUI.enabled;
            GUILayout.BeginVertical(_skin.Panel);
            GUILayout.Label(lang.SectionOutgoing, _skin.SectionTitle);
            DrawStyleControls(lang.OutgoingEnable, plugin.OutgoingEnabled, plugin.OutgoingFont, plugin.OutgoingColorMode,
                plugin.OutgoingColor, plugin.OutgoingEndColor, plugin.OutgoingBold, plugin.OutgoingItalic, lang);
            GUI.enabled = enabled;
            GUILayout.Label(lang.OutgoingDraft, _skin.Label);
            _editorDraft = GUILayout.TextArea(_editorDraft, 1000, _skin.EditorInput, GUILayout.Height(54f));
            bool fits = OutgoingFormat.TryCompose(_editorDraft, plugin.BuildOutgoingStyle(), out string wire);
            if (!plugin.OutgoingEnabled.Value && _editorDraft.Length > OutgoingFormat.WireLimit)
                fits = false;
            GUILayout.Label(fits ? string.Format(lang.OutgoingBudget, wire.Length, OutgoingFormat.WireLimit) : lang.OutgoingTooLong, _skin.Hint);
            GUILayout.Label(lang.OutgoingPreview, _skin.Label);
            Rect preview = GUILayoutUtility.GetRect(10f, 64f, GUILayout.ExpandWidth(true));
            GUI.Box(preview, GUIContent.none, _skin.Panel);
            if (Event.current.type == EventType.Repaint)
            {
                string sample = string.IsNullOrEmpty(_editorDraft) ? lang.OutgoingSample : _editorDraft;
                if (!OutgoingFormat.TryCompose(sample, plugin.BuildOutgoingStyle(), out string previewText))
                    previewText = "<noparse>" + sample + "</noparse>";
                _messagePreview.Draw(new Rect(preview.x + 8f, preview.y + 4f, preview.width - 16f, preview.height - 8f),
                    previewText, 18f, UiSkin.TextColor);
                _previewDrawn = true;
            }
            DrawMessageEditorActions(plugin, lang, fits, enabled);
            GUILayout.EndVertical();
        }

        void DrawStyleControls(string label, ConfigEntry<bool> apply, ConfigEntry<MessageFont> fontEntry,
            ConfigEntry<MessageColorMode> modeEntry, ConfigEntry<string> color, ConfigEntry<string> endColor,
            ConfigEntry<bool> bold, ConfigEntry<bool> italic, Lang lang)
        {
            bool enabled = GUI.enabled;
            ToggleRow(apply, label);
            GUI.enabled = enabled && apply.Value;
            MessageFont font = fontEntry.Value;
            ChoiceRow(lang.OutgoingFont, font == MessageFont.LiberationSans ? "Liberation Sans" : lang.OutgoingDefaultFont,
                step => fontEntry.Value = fontEntry.Value == MessageFont.GameDefault ? MessageFont.LiberationSans : MessageFont.GameDefault);
            GUILayout.Label(font == MessageFont.LiberationSans ? lang.OutgoingFontHint : lang.OutgoingDefaultHint, _skin.Hint);
            int mode = Mathf.Clamp((int)modeEntry.Value, 0, 2);
            ChoiceRow(lang.OutgoingColorMode, lang.OutgoingColorModes[mode],
                step => modeEntry.Value = (MessageColorMode)((mode + step + 3) % 3));
            if (mode != 0)
                EditableColorRow(mode == 2 ? lang.OutgoingStartColor : lang.TimeColor, color, lang);
            if (mode == 2)
                EditableColorRow(lang.OutgoingEndColor, endColor, lang);
            ToggleRow(bold, lang.OutgoingBold);
            ToggleRow(italic, lang.OutgoingItalic);
            GUI.enabled = enabled;
        }

        void DrawMessageEditorActions(Plugin plugin, Lang lang, bool fits, bool enabled)
        {
            bool wide = _rect.width >= 480f;
            if (wide) GUILayout.BeginHorizontal();
            GUI.enabled = enabled && fits && !string.IsNullOrWhiteSpace(_editorDraft) && GameAccess.MessageInput != null;
            if (GUILayout.Button(lang.OutgoingInsert, _skin.Button))
            {
                var input = GameAccess.MessageInput;
                input.text = _editorDraft;
                SetOpen(false);
                input.ActivateInputField();
                input.MoveTextEnd(false);
            }
            GUI.enabled = enabled;
            if (GUILayout.Button(lang.OutgoingReset, _skin.Button))
            {
                plugin.OutgoingEnabled.Value = false;
                plugin.OutgoingFont.Value = MessageFont.GameDefault;
                plugin.OutgoingColorMode.Value = MessageColorMode.Original;
                plugin.OutgoingColor.Value = "#F2C46D";
                plugin.OutgoingEndColor.Value = "#6AA8FF";
                plugin.OutgoingBold.Value = false;
                plugin.OutgoingItalic.Value = false;
                _colorDrafts.Clear();
                _colorValues.Clear();
            }
            if (wide)
            {
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.Label(lang.OutgoingHint, _skin.Hint);
        }

        void DrawNicknameEditor(Plugin plugin, Lang lang)
        {
            GUILayout.BeginVertical(_skin.Panel);
            GUILayout.Label(lang.NicknameTitle, _skin.SectionTitle);
            DrawStyleControls(lang.NicknameEnable, plugin.NicknameEnabled, plugin.NicknameFont, plugin.NicknameColorMode,
                plugin.NicknameColor, plugin.NicknameEndColor, plugin.NicknameBold, plugin.NicknameItalic, lang);
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && plugin.NicknameEnabled.Value;
            ToggleRow(plugin.NicknameWorld, lang.NicknameWorld);
            GUI.enabled = enabled;
            string name = GameAccess.LocalPlayerName;
            if (string.IsNullOrEmpty(name)) name = lang.NicknameSample;
            var style = plugin.BuildNicknameStyle();
            bool fits = OutgoingFormat.TryComposeNickname(name, style, out _);
            GUILayout.Label(lang.OutgoingPreview, _skin.Label);
            Rect preview = GUILayoutUtility.GetRect(10f, 88f, GUILayout.ExpandWidth(true));
            GUI.Box(preview, GUIContent.none, _skin.Panel);
            if (Event.current.type == EventType.Repaint)
            {
                OutgoingFormat.TryCompose(lang.OutgoingSample, plugin.BuildOutgoingStyle(), out string message);
                _messagePreview.Draw(new Rect(preview.x + 8f, preview.y + 4f, preview.width - 16f, preview.height - 8f),
                    NicknameFormat.Compose(name, style) + ": " + message, 18f, UiSkin.TextColor);
                _previewDrawn = true;
            }
            if (!fits) GUILayout.Label(lang.NicknameFallback, _skin.Hint);
            if (GUILayout.Button(lang.OutgoingReset, _skin.Button))
            {
                plugin.NicknameEnabled.Value = false;
                plugin.NicknameWorld.Value = true;
                plugin.NicknameFont.Value = MessageFont.GameDefault;
                plugin.NicknameColorMode.Value = MessageColorMode.Original;
                plugin.NicknameColor.Value = "#F2C46D";
                plugin.NicknameEndColor.Value = "#6AA8FF";
                plugin.NicknameBold.Value = plugin.NicknameItalic.Value = false;
                _colorDrafts.Clear();
                _colorValues.Clear();
            }
            GUILayout.Label(lang.NicknameHint, _skin.Hint);
            GUILayout.Label(lang.NicknameWorldHint, _skin.Hint);
            GUILayout.EndVertical();
        }

        void EditableColorRow(string label, ConfigEntry<string> entry, Lang lang)
        {
            bool expanded = _expandedColors.Contains(entry);
            if (!_colorDrafts.TryGetValue(entry, out string draft) || !_colorValues.TryGetValue(entry, out string last) || last != entry.Value)
                draft = entry.Value;
            if (!OutgoingFormat.TryColor(entry.Value, out string actual)) actual = "#F2C46D";
            GUILayout.BeginHorizontal(_skin.Row);
            GUILayout.Label(label, _skin.Label, GUILayout.Width(LabelWidth));
            int step = GUILayout.Button("◄", _skin.SmallButton, GUILayout.Width(ArrowWidth)) ? -1 : 0;
            string next = GUILayout.TextField(draft, 9, _skin.EditorHex, Shrinkable);
            if (GUILayout.Button("►", _skin.SmallButton, GUILayout.Width(ArrowWidth))) step = 1;
            if (ColorUtility.TryParseHtmlString(actual, out Color color))
            {
                Color previous = GUI.backgroundColor;
                GUI.backgroundColor = color;
                if (GUILayout.Button(GUIContent.none, _skin.ColorButton))
                {
                    if (!_expandedColors.Add(entry)) _expandedColors.Remove(entry);
                }
                GUI.backgroundColor = previous;
            }
            GUILayout.EndHorizontal();
            bool valid = (next.StartsWith("#", StringComparison.Ordinal) ? next.Length == 7 : next.Length == 6) && OutgoingFormat.TryColor(next, out _);
            if (step != 0)
            {
                entry.Value = Presets.Step(Presets.MessageColors, entry.Value, step, true);
                next = entry.Value;
                valid = true;
            }
            else if (valid && OutgoingFormat.TryColor(next, out string normalized))
                entry.Value = normalized;
            _colorDrafts[entry] = next;
            _colorValues[entry] = entry.Value;
            GUILayout.Label(valid ? lang.OutgoingColorHint : lang.OutgoingInvalidColor, _skin.Hint);
            if (expanded)
            {
                int rgb = Convert.ToInt32(actual.Substring(1), 16);
                int r = ColorChannel("R", (rgb >> 16) & 255);
                int g = ColorChannel("G", (rgb >> 8) & 255);
                int b = ColorChannel("B", rgb & 255);
                string changed = "#" + ((r << 16) | (g << 8) | b).ToString("X6");
                if (((r << 16) | (g << 8) | b) != rgb)
                {
                    entry.Value = changed;
                    _colorDrafts[entry] = changed;
                    _colorValues[entry] = changed;
                }
            }
        }

        int ColorChannel(string label, int value)
        {
            GUILayout.BeginHorizontal(_skin.Row);
            GUILayout.Space(LabelWidth - 26f);
            GUILayout.Label(label, _skin.Label, GUILayout.Width(26f));
            int next = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, 0f, 255f, _skin.Slider, _skin.Thumb));
            GUILayout.Label(next.ToString(), _skin.Value, GUILayout.Width(ValueWidth));
            GUILayout.EndHorizontal();
            return next;
        }

        void ToggleRow(ConfigEntry<bool> entry, string label, float indent = 0f)
        {
            GUILayout.BeginHorizontal(_skin.Row);
            if (indent > 0f)
                GUILayout.Space(indent);
            if (GUILayout.Button(label, _skin.ToggleLabel, Shrinkable))
                entry.Value = !entry.Value;
            bool value = GUILayout.Toggle(entry.Value, GUIContent.none, _skin.Check);
            GUILayout.EndHorizontal();
            if (value != entry.Value)
                entry.Value = value;
        }

        void IntRow(ConfigEntry<int> entry, string label, int min, int max, int step, string suffix)
        {
            GUILayout.BeginHorizontal(_skin.Row);
            GUILayout.Label(label, _skin.Label, GUILayout.Width(LabelWidth));
            float raw = GUILayout.HorizontalSlider(entry.Value, min, max, _skin.Slider, _skin.Thumb);
            int value = entry.Value;
            // Only a moved slider snaps to the step: a value set elsewhere (e.g. by the resize handle) stays as it is.
            if (Mathf.Abs(raw - entry.Value) > 0.001f)
                value = Mathf.Clamp(Mathf.RoundToInt(raw / step) * step, min, max);
            GUILayout.Label(value + suffix, _skin.Value, GUILayout.Width(ValueWidth));
            GUILayout.EndHorizontal();
            if (value != entry.Value)
                entry.Value = value;
        }

        void ChoiceRow(string label, string value, Action<int> change)
        {
            GUILayout.BeginHorizontal(_skin.Row);
            GUILayout.Label(label, _skin.Label, GUILayout.Width(LabelWidth));
            int step = 0;
            if (GUILayout.Button("◄", _skin.SmallButton, GUILayout.Width(ArrowWidth)))
                step = -1;
            GUILayout.Label(value, _skin.Field, Shrinkable);
            if (GUILayout.Button("►", _skin.SmallButton, GUILayout.Width(ArrowWidth)))
                step = 1;
            // Keeps this field as wide as the color fields, which have a sample on the right.
            GUILayout.Label(GUIContent.none, _skin.Swatch);
            GUILayout.EndHorizontal();
            if (step != 0)
                change(step);
        }

        void ColorRow(string label, ConfigEntry<string> entry, string[] presets, Lang lang)
        {
            GUILayout.BeginHorizontal(_skin.Row);
            GUILayout.Label(label, _skin.Label, GUILayout.Width(LabelWidth));
            int step = 0;
            if (GUILayout.Button("◄", _skin.SmallButton, GUILayout.Width(ArrowWidth)))
                step = -1;
            GUILayout.Label(lang.ColorName(Presets.IndexOf(presets, entry.Value, true)), _skin.Field, Shrinkable);
            if (GUILayout.Button("►", _skin.SmallButton, GUILayout.Width(ArrowWidth)))
                step = 1;
            Rect sample = GUILayoutUtility.GetRect(18f, 18f, _skin.Swatch);
            if (Event.current.type == EventType.Repaint && ChatFormat.TryParseColor(entry.Value, out string hex) &&
                ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                color.a = GUI.enabled ? 1f : 0.4f;
                GUI.DrawTexture(sample, _skin.White, ScaleMode.StretchToFill, true, 0f, color, 0f, 4f);
            }
            GUILayout.EndHorizontal();
            if (step != 0)
                entry.Value = Presets.Step(presets, entry.Value, step, true);
        }

        void OnDestroy()
        {
            _shield?.Dispose();
            if (_guiSkin != null) Destroy(_guiSkin);
            _messagePreview.Destroy();
            if (_skin != null)
                _skin.Destroy();
        }
    }
}
