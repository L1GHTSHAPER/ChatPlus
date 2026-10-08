using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace ChatPlus
{
    /// <summary>Typed editor for existing entries. Drafts never write to config before successful validation.</summary>
    internal sealed class ConfigMenu : MonoBehaviour
    {
        ConfigFile _config;
        string _title, _symbol;
        ConfigEntryBase[] _entries;
        string[] _tabs;
        int _tab, _pendingTab = -1;
        Rect _rect;
        bool _placed;
        UiSkin _skin;
        GUISkin _guiSkin;
        WindowInputShield _shield;
        ModDock _dock;
        Vector2[] _scroll;
        readonly Dictionary<ConfigEntryBase,string> _drafts = new Dictionary<ConfigEntryBase,string>();
        readonly HashSet<string> _expanded = new HashSet<string>();
        readonly Dictionary<string,bool> _pendingExpand = new Dictionary<string,bool>();
        string _error;
        int _hotControl;
        internal Action ExtraControls { get; set; }
        internal bool IsOpen => enabled;
        internal static ConfigMenu Create(GameObject parent, string title, string symbol, ConfigFile config, Func<bool> visible = null)
        {
            var menu = parent.AddComponent<ConfigMenu>();
            menu.enabled = false;
            menu._title = title; menu._symbol = symbol; menu._config = config;
            menu._entries = config.Select(p => p.Value).ToArray();
            menu._tabs = menu._entries.Select(e => Root(e.Definition.Section)).Distinct().ToArray();
            menu._scroll = new Vector2[menu._tabs.Length];
            menu._shield = new WindowInputShield(title);
            menu._dock = ModDock.Register(title, symbol, menu.Toggle, () => menu.enabled, () => title + " · " + UiEnvironment.L("Settings", "Настройки"), visible);
            return menu;
        }
        internal void Toggle() => SetOpen(!enabled);
        internal void SetOpen(bool open)
        {
            enabled = open;
            _shield?.Set(open);
            if (!open && _hotControl != 0 && GUIUtility.hotControl == _hotControl) GUIUtility.hotControl = 0;
            if (!open) { _hotControl = 0; GUI.FocusControl(null); }
        }
        void Update() { if (Input.GetKeyDown(KeyCode.Escape)) SetOpen(false); }
        void OnDisable() { _shield?.Set(false); }
        void OnGUI()
        {
            if (UiEnvironment.PreviewRendering) return;
            if (_skin == null) { _skin = new UiSkin(); _guiSkin = _skin.CreateGuiSkin(GUI.skin); }
            float scale = UiEnvironment.Scale;
            float width = Mathf.Min(600f, Screen.width / scale - 24f), height = Screen.height / scale;
            if (!_placed) { _rect = new Rect((Screen.width / scale - width)*.5f, height*.06f,width,0); _placed = true; }
            _rect.width = width;
            var matrix = GUI.matrix; var skin = GUI.skin;
            try
            {
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale,scale,1)); GUI.skin = _guiSkin;
                GUI.skin.settings.selectionColor = new Color(.95f,.72f,.58f,.7f);
                _rect = GUILayout.Window((_title.GetHashCode() & 0x3FFFFFFF) + 3000, _rect, DrawWindow, GUIContent.none, _skin.Window, GUILayout.Width(width));
                _rect.x = Mathf.Clamp(_rect.x,12,Mathf.Max(12, Screen.width/scale-width-12));
                _rect.y = Mathf.Clamp(_rect.y,8,Mathf.Max(8,height-_rect.height-8));
                _shield.Place(_rect,scale);
            }
            finally { GUI.skin = skin; GUI.matrix = matrix; }
        }
        void DrawWindow(int id)
        {
            if (Event.current.type == EventType.Layout)
            {
                if (_pendingTab >= 0) { _tab = _pendingTab; _pendingTab = -1; GUI.FocusControl(null); }
                foreach (var change in _pendingExpand) { if (change.Value) _expanded.Add(change.Key); else _expanded.Remove(change.Key); }
                _pendingExpand.Clear();
            }
            int before = GUIUtility.hotControl;
            GUILayout.BeginHorizontal();
            GUILayout.Label(_symbol == "camera" ? "CU" : _symbol == "range" ? "LR" : "PV", _skin.Logo);
            GUILayout.BeginVertical();GUILayout.Label(_title,_skin.Title);GUILayout.Label(UiEnvironment.L("Settings", "Настройки"),_skin.Subtitle);GUILayout.EndVertical();
            GUILayout.FlexibleSpace(); if (GUILayout.Button("×",_skin.CloseButton)) SetOpen(false); GUILayout.EndHorizontal();
            GUILayout.Space(8);
            int columns = _rect.width < 480 ? 2 : 4;
            for(int first=0;first<_tabs.Length;first+=columns)
            {
                GUILayout.BeginHorizontal();
                for(int i=first;i<Mathf.Min(first+columns,_tabs.Length);i++) if(GUILayout.Button(UiLabels.Name(_tabs[i]),i==_tab?_skin.SelectedTab:_skin.Tab)) _pendingTab=i;
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(8);
            float body = Mathf.Min(520, Mathf.Max(80, Screen.height/UiEnvironment.Scale - 220 - 34 * ((_tabs.Length+columns-1)/columns)));
            _scroll[_tab] = GUILayout.BeginScrollView(_scroll[_tab], GUILayout.Height(body));
            foreach(var group in _entries.Where(e => Root(e.Definition.Section)==_tabs[_tab]).GroupBy(e=>e.Definition.Section))
            {
                bool advanced = group.Key.Contains(".") || group.Key == "Quality";
                GUILayout.BeginVertical(_skin.Panel);
                bool show = !advanced || _expanded.Contains(group.Key);
                if(advanced)
                {
                    if(GUILayout.Button((show?"▼ ":"► ")+UiLabels.Name(group.Key),_skin.SectionTitle)) _pendingExpand[group.Key]=!show;
                }
                else GUILayout.Label(UiLabels.Name(group.Key),_skin.SectionTitle);
                if(show) foreach(var entry in group) DrawEntry(entry);
                GUILayout.EndVertical();
            }
            ExtraControls?.Invoke();
            GUILayout.EndScrollView();
            GUILayout.Space(8);
            GUILayout.Label(_error ?? UiEnvironment.L("Changes save automatically. Fields: Apply to save.","Изменения сохраняются сразу. Для полей нажмите «Применить»."),_skin.Hint);
            GUI.DragWindow(new Rect(0,0,_rect.width-48,62));
            if(before != GUIUtility.hotControl) _hotControl=GUIUtility.hotControl;
        }
        void DrawEntry(ConfigEntryBase entry)
        {
            string label = UiLabels.Name(entry.Definition.Key);
            GUILayout.BeginHorizontal(_skin.Row);
            var content = new GUIContent(label, entry.Description.Description);
            if(entry.SettingType==typeof(bool))
            {
                bool value=GUILayout.Toggle((bool)entry.BoxedValue,GUIContent.none,_skin.Check);
                if(GUILayout.Button(content,_skin.ToggleLabel)) value=!(bool)entry.BoxedValue;
                if(value!=(bool)entry.BoxedValue) entry.BoxedValue=value;
            }
            else
            {
                GUILayout.Label(content,_skin.Label,GUILayout.Width(Mathf.Clamp((_rect.width-64)*.4f,105,190)));
                bool numeric=entry.SettingType==typeof(float)||entry.SettingType==typeof(int);
                var range=entry.Description.AcceptableValues;
                var min=range?.GetType().GetProperty("MinValue");var max=range?.GetType().GetProperty("MaxValue");
                if(numeric && min!=null && max!=null)
                {
                    float value=Convert.ToSingle(entry.BoxedValue,CultureInfo.InvariantCulture);
                    float next=GUILayout.HorizontalSlider(value,Convert.ToSingle(min.GetValue(range),CultureInfo.InvariantCulture),Convert.ToSingle(max.GetValue(range),CultureInfo.InvariantCulture),_skin.Slider,_skin.Thumb);
                    GUILayout.Label(value.ToString(entry.SettingType==typeof(int)?"0":"0.##",CultureInfo.InvariantCulture),_skin.Value,GUILayout.Width(54));
                    if(next!=value) entry.BoxedValue=entry.SettingType==typeof(int)?(object)Mathf.RoundToInt(next):(float)Math.Round(next,3);
                }
                else if(entry.SettingType.IsEnum && entry.SettingType != typeof(KeyCode))
                {
                    var values=Enum.GetValues(entry.SettingType);int index=Array.IndexOf(values.Cast<object>().ToArray(),entry.BoxedValue);
                    if(GUILayout.Button("◄",_skin.SmallButton,GUILayout.Width(26))) entry.BoxedValue=values.GetValue((index+values.Length-1)%values.Length);
                    GUILayout.Label(UiLabels.Name(entry.BoxedValue.ToString()),_skin.Field,GUILayout.ExpandWidth(true));
                    if(GUILayout.Button("►",_skin.SmallButton,GUILayout.Width(26))) entry.BoxedValue=values.GetValue((index+1)%values.Length);
                }
                else
                {
                    if(!_drafts.TryGetValue(entry,out string draft)) draft=entry.SettingType==typeof(Color)?"#"+ColorUtility.ToHtmlStringRGBA((Color)entry.BoxedValue):entry.GetSerializedValue();
                    string next=GUILayout.TextField(draft,_skin.EditorHex,GUILayout.MinWidth(40),GUILayout.ExpandWidth(true));
                    if(next!=draft) _drafts[entry]=next;
                    if(GUILayout.Button(UiEnvironment.L("Apply","Применить"),_skin.SmallButton,GUILayout.Width(UiEnvironment.Russian?86:54))) Apply(entry,next);
                }
            }
            GUILayout.EndHorizontal();
            if(entry.SettingType==typeof(Color))
            {
                Color color=(Color)entry.BoxedValue;
                Rect sample=GUILayoutUtility.GetRect(0,24,GUILayout.ExpandWidth(true));
                if(Event.current.type==EventType.Repaint)
                {
                    _skin.PreviewPaper.Draw(sample,false,false,false,false);
                    GUI.DrawTexture(sample,_skin.White,ScaleMode.StretchToFill,true,0,color,0,6);
                    GUI.Label(sample,"#"+ColorUtility.ToHtmlStringRGBA(color),_skin.Value);
                }
            }
        }
        void Apply(ConfigEntryBase entry,string text)
        {
            try
            {
                if(entry.SettingType==typeof(Color))
                {
                    if(!ColorUtility.TryParseHtmlString("#"+text.Trim().TrimStart('#'),out Color color)) throw new FormatException();
                    entry.BoxedValue=color;
                }
                else if(entry.SettingType==typeof(KeyCode)) entry.BoxedValue=Enum.Parse(typeof(KeyCode),text.Trim(),true);
                else if(entry.SettingType==typeof(KeyboardShortcut))
                {
                    foreach(string key in text.Split('+')) if(!Enum.TryParse(key.Trim(),true,out KeyCode parsed) || !Enum.IsDefined(typeof(KeyCode),parsed)) throw new FormatException();
                    entry.BoxedValue=KeyboardShortcut.Deserialize(text.Trim());
                }
                else if(entry.SettingType==typeof(float))
                {
                    if(!float.TryParse(text.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out float value)||float.IsNaN(value)||float.IsInfinity(value)) throw new FormatException();
                    entry.BoxedValue=value;
                }
                else if(entry.SettingType==typeof(int)) entry.BoxedValue=int.Parse(text,NumberStyles.Integer,CultureInfo.InvariantCulture);
                else if(entry.SettingType==typeof(string)) entry.BoxedValue=text;
                else throw new FormatException();
                _drafts.Remove(entry);_error=null;
            }
            catch(Exception) { _error=UiEnvironment.L("Invalid value. The saved setting is unchanged.","Некорректное значение. Сохранённая настройка не изменена."); }
        }
        static string Root(string section) => section.Split('.')[0];
        void OnDestroy()
        {
            _shield?.Dispose(); if(_dock!=null) Destroy(_dock.gameObject); _skin?.Destroy(); if(_guiSkin!=null) Destroy(_guiSkin);
        }
    }
}
