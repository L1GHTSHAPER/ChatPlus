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
        const float Width = 540f;
        const float LabelWidth = 170f;
        const float ArrowWidth = 26f;
        const float ValueWidth = 58f;
        const float Indent = 26f;
        // How long the "click again" state of the Clear history button lasts.
        const float ConfirmTime = 3f;

        static readonly GUILayoutOption[] Shrinkable = { GUILayout.MinWidth(40f), GUILayout.ExpandWidth(true) };

        UiSkin _skin;
        GUI.WindowFunction _drawWindow;
        Rect _rect;
        bool _placed;
        float _screenHeight;
        Vector2 _scroll;
        float _contentHeight = 700f;
        // The IMGUI control of this window that holds the mouse (slider or window drag), released on close.
        int _hotControl;
        GameObject _blockerCanvas;
        RectTransform _blocker;
        float _confirmClearUntil;

        public void Toggle()
        {
            SetOpen(!enabled);
        }

        public void SetOpen(bool open)
        {
            if (open == enabled)
                return;
            enabled = open;
            if (open)
            {
                Lang.Invalidate();
                return;
            }
            // Closed with the hotkey while a slider is held: nothing would release the mouse otherwise, and other
            // IMGUI windows would ignore clicks.
            if (_hotControl != 0 && GUIUtility.hotControl == _hotControl)
                GUIUtility.hotControl = 0;
            _hotControl = 0;
        }

        void OnEnable()
        {
            if (_blockerCanvas != null)
                _blockerCanvas.SetActive(true);
        }

        void OnDisable()
        {
            if (_blockerCanvas != null)
                _blockerCanvas.SetActive(false);
        }

        void OnGUI()
        {
            if (Plugin.Instance == null)
                return;
            if (_skin == null)
                _skin = new UiSkin();
            if (_drawWindow == null)
                _drawWindow = DrawWindow;

            // Keep the window the same physical size on high resolutions (laid out for 1080p).
            float scale = Mathf.Clamp(Screen.height / 1080f, 1f, 3f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float screenWidth = Screen.width / scale;
            _screenHeight = Screen.height / scale;
            if (!_placed)
            {
                _rect = new Rect(Mathf.Round((screenWidth - Width) * 0.5f), Mathf.Round(_screenHeight * 0.06f), Width, 0f);
                _placed = true;
            }

            _rect = GUILayout.Window(WindowId, _rect, _drawWindow, GUIContent.none, _skin.Window, GUILayout.Width(Width));
            _rect.x = Mathf.Clamp(_rect.x, 0f, Mathf.Max(0f, screenWidth - _rect.width));
            _rect.y = Mathf.Clamp(_rect.y, 0f, Mathf.Max(0f, _screenHeight - _rect.height));
            GUI.matrix = previousMatrix;

            if (Event.current.type == EventType.Repaint)
                UpdateBlocker(scale);
        }

        void DrawWindow(int id)
        {
            Plugin plugin = Plugin.Instance;
            Lang lang = Lang.Current;
            int hotControlBefore = GUIUtility.hotControl;

            GUILayout.BeginHorizontal();
            GUILayout.Label(lang.Title, _skin.Title);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", _skin.CloseButton))
                SetOpen(false);
            GUILayout.EndHorizontal();

            // The settings scroll when the screen is too low for all of them. The few spare pixels cover the first
            // panel's margin and rounding, so no scrollbar appears when everything fits.
            float viewHeight = Mathf.Min(_contentHeight + 4f, Mathf.Max(200f, _screenHeight - 140f));
            _scroll = GUILayout.BeginScrollView(_scroll, false, false, GUI.skin.horizontalScrollbar,
                GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.Height(viewHeight));
            GUILayout.BeginVertical();
            DrawSettings(plugin, lang);
            GUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint)
                _contentHeight = GUILayoutUtility.GetLastRect().height;
            GUILayout.EndScrollView();

            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(lang.Close, _skin.Button))
                SetOpen(false);
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
            GUILayout.Label(string.Format(lang.WindowHint, plugin.WindowKey.Value), _skin.Hint);

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 40f));

            // A control in this window took or released the mouse during this event.
            if (GUIUtility.hotControl != hotControlBefore)
                _hotControl = GUIUtility.hotControl;
        }

        void DrawSettings(Plugin plugin, Lang lang)
        {
            bool enabled = GUI.enabled;

            // Message time
            GUILayout.BeginVertical(_skin.Panel);
            GUILayout.Label(lang.SectionTime, _skin.SectionTitle);
            ToggleRow(plugin.Timestamps, lang.ShowTime);
            GUI.enabled = enabled && plugin.Timestamps.Value;
            ChoiceRow(lang.TimeFormat, ChatFormat.FormatTime(DateTime.Now, plugin.TimeFormat.Value), step =>
                plugin.TimeFormat.Value = Presets.Step(Presets.TimeFormats, plugin.TimeFormat.Value, step, false));
            ColorRow(lang.TimeColor, plugin.TimeColor, Presets.TimeColors, lang);
            IntRow(plugin.TimeSize, lang.TimeSize, 50, 100, 5, "%");
            ToggleRow(plugin.TimeOnNotifications, lang.TimeOnNotifications);
            GUI.enabled = enabled;
            GUILayout.EndVertical();

            // History
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
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(lang.OpenFolder, _skin.Button))
                plugin.OpenHistoryFolder();
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
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Label(string.Format(lang.HistoryCount, plugin.History.Count), _skin.Hint);
            GUILayout.EndVertical();

            // Chat window
            GUILayout.BeginVertical(_skin.Panel);
            GUILayout.Label(lang.SectionWindow, _skin.SectionTitle);
            IntRow(plugin.Width, lang.Width, Plugin.MinWidth, Plugin.MaxWidth, 5, "%");
            IntRow(plugin.Height, lang.Height, Plugin.MinHeight, Plugin.MaxHeight, 5, "%");
            IntRow(plugin.TextSize, lang.TextSize, Plugin.MinTextSize, Plugin.MaxTextSize, 5, "%");
            ToggleRow(plugin.ResizeHandle, lang.ResizeHandle);
            GUILayout.Label(lang.ResizeHint, _skin.Hint);
            ToggleRow(plugin.RememberPosition, lang.RememberPosition);
            ToggleRow(plugin.CullHiddenLines, lang.CullHiddenLines);
            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(lang.ResetWindow, _skin.Button))
                plugin.ResetWindow();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
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
            ToggleRow(plugin.RecallSent, lang.RecallSent);
            ToggleRow(plugin.RightClickCopy, lang.RightClickCopy);
            GUILayout.EndVertical();
        }

        void ToggleRow(ConfigEntry<bool> entry, string label, float indent = 0f)
        {
            GUILayout.BeginHorizontal(_skin.Row);
            if (indent > 0f)
                GUILayout.Space(indent);
            bool value = GUILayout.Toggle(entry.Value, GUIContent.none, _skin.Check);
            if (GUILayout.Button(label, _skin.ToggleLabel, Shrinkable))
                value = !entry.Value;
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

        /// <summary>
        /// An invisible uGUI panel under the window keeps clicks on the window from also reaching game buttons
        /// behind it (IMGUI does not block uGUI by itself).
        /// </summary>
        void UpdateBlocker(float scale)
        {
            if (_blocker == null)
            {
                if (_blockerCanvas != null)
                    Destroy(_blockerCanvas);
                _blockerCanvas = new GameObject("ChatPlus.InputBlocker") { hideFlags = HideFlags.HideAndDontSave };
                DontDestroyOnLoad(_blockerCanvas);
                Canvas canvas = _blockerCanvas.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = short.MaxValue;
                _blockerCanvas.AddComponent<GraphicRaycaster>();

                var panel = new GameObject("Blocker", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
                panel.transform.SetParent(_blockerCanvas.transform, false);
                Image image = panel.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0f);
                image.raycastTarget = true;
                _blocker = (RectTransform)panel.transform;
                _blocker.anchorMin = new Vector2(0f, 1f);
                _blocker.anchorMax = new Vector2(0f, 1f);
                _blocker.pivot = new Vector2(0f, 1f);
            }
            _blocker.anchoredPosition = new Vector2(_rect.x * scale, -_rect.y * scale);
            _blocker.sizeDelta = new Vector2(_rect.width * scale, _rect.height * scale);
        }

        void OnDestroy()
        {
            if (_skin != null)
                _skin.Destroy();
            if (_blockerCanvas != null)
                Destroy(_blockerCanvas);
        }
    }
}
