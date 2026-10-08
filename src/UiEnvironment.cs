using System;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace ChatPlus
{
    // This source is embedded in each mod. The named scene objects form the cross-assembly contract;
    // there is no required companion DLL and no static state shared across assembly boundaries.
    internal static class UiEnvironment
    {
        internal const string DockName = "LightShaper.ModDock.v1";
        internal const string WindowName = "LightShaper.ModWindows.v1";
        static GameObject _windows;
        static Font _font;
        static float _fontRetry;
        static CanvasScaler _gameScaler;
        static float _scaleRetry;
        internal static Func<bool> LanguageOverride { get; set; }
        internal static bool Russian
        {
            get { if (LanguageOverride != null) return LanguageOverride(); try { return LocalizationSettings.SelectedLocale?.Identifier.Code?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true; } catch { return false; } }
        }
        internal static string L(string en, string ru) => Russian ? ru : en;
        internal static Font GameFont
        {
            get
            {
                if (_font == null && Time.unscaledTime >= _fontRetry)
                {
                    _fontRetry = Time.unscaledTime + 2f;
                    _font = Resources.FindObjectsOfTypeAll<TMP_Text>().Select(t => t.font?.sourceFontFile).FirstOrDefault(f => f != null);
                }
                return _font;
            }
        }
        internal static float Scale
        {
            get
            {
                if(_gameScaler==null && Time.unscaledTime>=_scaleRetry)
                {
                    _scaleRetry=Time.unscaledTime+2;
                    _gameScaler=UnityEngine.Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)
                        .FirstOrDefault(s=>s.GetComponent<Canvas>()?.renderMode!=RenderMode.WorldSpace);
                }
                float factor=1;
                if(_gameScaler!=null)
                {
                    if(_gameScaler.uiScaleMode==CanvasScaler.ScaleMode.ConstantPixelSize) factor=_gameScaler.scaleFactor;
                    else if(_gameScaler.uiScaleMode==CanvasScaler.ScaleMode.ScaleWithScreenSize)
                    {
                        var reference=_gameScaler.referenceResolution;
                        float x=Screen.width/Mathf.Max(1,reference.x), y=Screen.height/Mathf.Max(1,reference.y);
                        float expected=_gameScaler.screenMatchMode==CanvasScaler.ScreenMatchMode.Expand?Mathf.Min(x,y):
                            _gameScaler.screenMatchMode==CanvasScaler.ScreenMatchMode.Shrink?Mathf.Max(x,y):
                            Mathf.Pow(2,Mathf.Lerp(Mathf.Log(x,2),Mathf.Log(y,2),_gameScaler.matchWidthOrHeight));
                        factor=_gameScaler.GetComponent<Canvas>().scaleFactor/Mathf.Max(.01f,expected);
                    }
                }
                return Mathf.Min(Mathf.Clamp(Screen.height/1080f,.65f,3f)*Mathf.Clamp(factor,.5f,2f),Mathf.Max(.3f,Screen.width/360f));
            }
        }
        internal static bool PreviewRendering => AppDomain.CurrentDomain.GetData("LightShaper.PreviewRendering.v1") is bool rendering && rendering;
        internal static bool AnyWindowOpen
        {
            get
            {
                if (_windows == null) _windows = GameObject.Find(WindowName);
                if (_windows == null) return false;
                foreach (Transform child in _windows.transform) if (child.gameObject.activeSelf) return true;
                return false;
            }
        }
        internal static GameObject Windows
        {
            get
            {
                if (_windows == null) _windows = GameObject.Find(WindowName);
                if (_windows == null)
                {
                    _windows = new GameObject(WindowName);
                    UnityEngine.Object.DontDestroyOnLoad(_windows);
                }
                return _windows;
            }
        }
        internal static void InstallInputGuard(Harmony harmony) => harmony.Patch(AccessTools.Method(typeof(InputManager), "Update"),
            postfix: new HarmonyMethod(typeof(UiEnvironment), nameof(BlockGameInput)) { priority = Priority.Last });
        static readonly FieldInfo[] Inputs = typeof(InputManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(f => f.Name.StartsWith("<") && f.Name.EndsWith(">k__BackingField") && (f.FieldType == typeof(bool) || f.FieldType == typeof(float))).ToArray();
        static void BlockGameInput(InputManager __instance)
        {
            if (!AnyWindowOpen) return;
            foreach (var field in Inputs) field.SetValue(__instance, field.FieldType == typeof(bool) ? (object)false : 0f);
        }
    }

    internal sealed class WindowInputShield : IDisposable
    {
        GameObject _host;
        RectTransform _panel;
        internal WindowInputShield(string name)
        {
            _host = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            _host.transform.SetParent(UiEnvironment.Windows.transform, false);
            var canvas = _host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;
            var panel = new GameObject("Window", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(_host.transform, false);
            panel.GetComponent<Image>().color = Color.clear;
            _panel = (RectTransform)panel.transform;
            _panel.anchorMin = _panel.anchorMax = _panel.pivot = new Vector2(0f, 1f);
            _host.SetActive(false);
        }
        internal void Set(bool open) { if (_host != null) _host.SetActive(open); }
        internal void Place(Rect rect, float scale)
        {
            _panel.anchoredPosition = new Vector2(rect.x * scale, -rect.y * scale);
            _panel.sizeDelta = rect.size * scale;
        }
        public void Dispose() { if (_host != null) UnityEngine.Object.Destroy(_host); }
    }
}
