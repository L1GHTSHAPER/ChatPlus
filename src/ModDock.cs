using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChatPlus
{
    /// <summary>Independent mods register sibling buttons on one native canvas; the first sibling lays out all.</summary>
    internal sealed class ModDock : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        Func<string> _hint;
        Func<bool> _isOpen, _visible;
        Image _image;
        Image _iconImage;
        Button _control, _native;
        Image _nativeBackground, _nativeIcon;
        Text _tooltip;
        RectTransform _tooltipRect;
        Sprite _background, _icon;
        Texture2D _backgroundTexture, _iconTexture;
        CanvasGroup _group;
        float _nextLayout;
        bool _hover;
        readonly List<DockLayout.Box> _obstacles = new List<DockLayout.Box>();
        readonly Vector3[] _corners = new Vector3[4];

        internal static ModDock Register(string name, string symbol, Action toggle, Func<bool> open, Func<string> hint, Func<bool> visible = null)
        {
            GameObject host = GameObject.Find(UiEnvironment.DockName);
            if (host == null)
            {
                host = new GameObject(UiEnvironment.DockName, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                UnityEngine.Object.DontDestroyOnLoad(host);
                var canvas = host.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32765;
            }
            var button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            button.transform.SetParent(host.transform, false);
            var dock = button.AddComponent<ModDock>();
            dock._hint = hint; dock._isOpen = open; dock._visible = visible ?? (() => true);
            dock._group = button.GetComponent<CanvasGroup>();
            dock._image = button.GetComponent<Image>();
            dock._backgroundTexture = RoundedTexture();
            dock._background = Sprite.Create(dock._backgroundTexture, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(16,16,16,16));
            dock._image.sprite = dock._background;
            dock._image.type = Image.Type.Sliced;
            var control = button.GetComponent<Button>();
            dock._control = control;
            control.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = control.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(1f, .88f, .8f); colors.pressedColor = new Color(.95f, .8f, .68f);
            control.colors = colors;
            control.onClick.AddListener(() => { toggle(); EventSystem.current?.SetSelectedGameObject(null); });
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(button.transform, false);
            var ir = (RectTransform)icon.transform;
            ir.anchorMin = new Vector2(.19f,.19f); ir.anchorMax = new Vector2(.81f,.81f); ir.offsetMin = ir.offsetMax = Vector2.zero;
            dock._iconTexture = IconTexture(symbol);
            dock._icon = Sprite.Create(dock._iconTexture, new Rect(0,0,32,32), new Vector2(.5f,.5f));
            icon.GetComponent<Image>().sprite = dock._icon;
            icon.GetComponent<Image>().color = new Color(.67f,.31f,.24f);
            icon.GetComponent<Image>().raycastTarget = false;
            icon.GetComponent<Image>().preserveAspect = true;
            dock._iconImage = icon.GetComponent<Image>();
            var tip = new GameObject("Tooltip", typeof(RectTransform), typeof(Image));
            tip.transform.SetParent(button.transform, false);
            tip.GetComponent<Image>().sprite = dock._background; tip.GetComponent<Image>().type = Image.Type.Sliced;
            tip.GetComponent<Image>().raycastTarget = false;
            dock._tooltipRect = (RectTransform)tip.transform;
            var label = new GameObject("Text", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(tip.transform, false);
            var lr = (RectTransform)label.transform;
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = new Vector2(10,4); lr.offsetMax = new Vector2(-10,-4);
            dock._tooltip = label.GetComponent<Text>();
            dock._tooltip.font = UiEnvironment.GameFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dock._tooltip.fontSize = 13; dock._tooltip.color = UiSkin.TextColor; dock._tooltip.alignment = TextAnchor.MiddleCenter;
            dock._tooltip.raycastTarget = false; dock._tooltip.supportRichText = false;
            tip.SetActive(false);
            foreach (Transform child in host.transform.Cast<Transform>().OrderBy(t => t.name, StringComparer.Ordinal).ToArray()) child.SetAsLastSibling();
            return dock;
        }

        void Update()
        {
            ApplyNativeStyle();
            float nativeOpacity = NativeSidebar.Opacity;
            bool visible = _visible() && nativeOpacity > .01f;
            _group.alpha = visible ? nativeOpacity : 0f;
            _group.blocksRaycasts = visible;
            _group.interactable = visible;
            if (!visible) _hover = false;
            // The game button has several visual layers; its root Image alone is a translucent hit area.
            // Keep our complete bordered surface opaque instead of copying that hit-area tint/sprite.
            _image.color = _isOpen() ? new Color(1f,.84f,.74f,1f) : Color.white;
            _iconImage.color = new Color(.43f,.34f,.35f,1f);
            if (transform.GetSiblingIndex() == 0 && Time.unscaledTime >= _nextLayout)
            {
                _nextLayout = Time.unscaledTime + .15f;
                LayoutGroup();
            }
            bool show = _hover && _group.alpha > 0f && _group.blocksRaycasts;
            _tooltipRect.gameObject.SetActive(show);
            if (show)
            {
                float scale = UiEnvironment.Scale;
                _tooltip.text = _hint();
                _tooltip.fontSize = Mathf.RoundToInt(13 * scale);
                float width = Mathf.Min(Screen.width - 24f, Mathf.Max(140f * scale, _tooltip.preferredWidth + 24f));
                var rect = (RectTransform)transform;
                float px = rect.anchoredPosition.x;
                bool right = px > Screen.width * .5f;
                _tooltipRect.pivot = new Vector2(right ? 1f : 0f, .5f);
                _tooltipRect.anchorMin = _tooltipRect.anchorMax = new Vector2(.5f,.5f);
                _tooltipRect.sizeDelta = new Vector2(width, 32f * scale);
                _tooltipRect.anchoredPosition = new Vector2((right ? -1f : 1f) * (rect.sizeDelta.x * .5f + 8f), 0f);
            }
        }
        void ApplyNativeStyle()
        {
            Button native = NativeSidebar.Button;
            if (native == null) return;
            Image background = NativeSidebar.Background(native);
            if (background == null) return;
            if (_native != native)
            {
                _native = native;
                _nativeBackground = background;
                _nativeIcon = NativeSidebar.Icon(native);
                _control.transition = Selectable.Transition.ColorTint;
                var colors = _control.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f,.93f,.88f,1f);
                colors.pressedColor = new Color(.96f,.81f,.72f,1f);
                colors.selectedColor = Color.white;
                _control.colors = colors;
                _image.type = Image.Type.Sliced;
            }
        }
        void LayoutGroup()
        {
            _obstacles.Clear();
            foreach (var graphic in GraphicRegistryAll())
            {
                if (!graphic.isActiveAndEnabled || graphic.canvas == null || graphic.canvas.renderMode == RenderMode.WorldSpace || graphic.transform.IsChildOf(transform.parent)) continue;
                bool chat = false;
                for(Transform parent=graphic.transform;parent!=null;parent=parent.parent)
                    if(parent.name.IndexOf("chat",StringComparison.OrdinalIgnoreCase)>=0) { chat=true;break; }
                if(!graphic.raycastTarget && !chat) continue;
                // Invisible window shields must still reserve their rectangle; other fully faded graphics do not.
                bool shield = graphic.transform.root.name == UiEnvironment.WindowName;
                if (!shield && ((chat ? 1f : graphic.color.a) * graphic.canvasRenderer.GetInheritedAlpha() < .03f || graphic.canvasRenderer.cull)) continue;
                var rt = graphic.rectTransform;
                rt.GetWorldCorners(_corners);
                Camera camera = graphic.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : graphic.canvas.worldCamera;
                Vector2 a = RectTransformUtility.WorldToScreenPoint(camera, _corners[0]);
                Vector2 b = RectTransformUtility.WorldToScreenPoint(camera, _corners[2]);
                var rect = Rect.MinMaxRect(Mathf.Min(a.x,b.x), Screen.height-Mathf.Max(a.y,b.y), Mathf.Max(a.x,b.x), Screen.height-Mathf.Min(a.y,b.y));
                // Full-screen dimmers block gameplay and the dock; ordinary full-screen scene decoration is ignored.
                if (rect.width > Screen.width * .95f && rect.height > Screen.height * .95f && !shield) continue;
                rect.xMin -= 6; rect.yMin -= 6; rect.xMax += 6; rect.yMax += 6;
                _obstacles.Add(new DockLayout.Box(rect.x,rect.y,rect.width,rect.height));
            }
            var children = transform.parent.Cast<Transform>().Where(t => t.GetComponent<Button>() != null).ToArray();
            float scale = UiEnvironment.Scale, gap = 6f * scale;
            Vector2 size = NativeSidebar.Size;
            if (size.x < 1f || size.y < 1f) size = new Vector2(36f*scale,36f*scale);
            bool found = DockLayout.Find(Screen.width,Screen.height,size.x,size.y,gap,12f*scale,children.Length,_obstacles,out var groupRect);
            for (int i = 0; i < children.Length; i++)
            {
                var rect = (RectTransform)children[i];
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,1);
                rect.sizeDelta = size;
                rect.anchoredPosition = found ? new Vector2(groupRect.X, -groupRect.Y - i * (size.y + gap)) : new Vector2(-10000,-10000);
            }
        }
        static Graphic[] GraphicRegistryAll() => UnityEngine.Object.FindObjectsByType<Graphic>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        public void OnPointerEnter(PointerEventData data) { _hover = true; }
        public void OnPointerExit(PointerEventData data) { _hover = false; }
        void OnDestroy()
        {
            Destroy(_background); Destroy(_icon); Destroy(_backgroundTexture); Destroy(_iconTexture);
            if (transform.parent != null && transform.parent.childCount == 1) Destroy(transform.parent.gameObject);
        }
        static Texture2D RoundedTexture()
        {
            var pixels = new Color[4096];
            for (int y=0;y<64;y++) for(int x=0;x<64;x++)
            {
                float qx=Mathf.Abs(x+.5f-32)-18, qy=Mathf.Abs(y+.5f-32)-18;
                float d=new Vector2(Mathf.Max(qx,0),Mathf.Max(qy,0)).magnitude+Mathf.Min(Mathf.Max(qx,qy),0)-14;
                var c=Color.Lerp(new Color(.43f,.34f,.35f,1f),new Color(1f,.95f,.86f,1f),Mathf.Clamp01(.5f-d-4f));
                c.a=Mathf.Clamp01(.5f-d); pixels[y*64+x]=c;
            }
            return Texture(pixels,64);
        }
        // Functional pictograms drawn from line segments: bubble, speaker, camera, range, mic and movement.
        static Texture2D IconTexture(string symbol)
        {
            var lines=new List<Vector4>();
            void Line(float x,float y,float a,float b) => lines.Add(new Vector4(x,y,a,b));
            void Box(float x,float y,float w,float h) { Line(x,y,x+w,y);Line(x+w,y,x+w,y+h);Line(x+w,y+h,x,y+h);Line(x,y+h,x,y); }
            if(symbol=="camera") { Box(4,9,24,17);Box(10,5,10,4);Circle(lines,16,17,6); }
            else if(symbol=="sound") { Box(4,13,6,8);Line(10,13,17,7);Line(17,7,17,26);Line(17,26,10,21);Line(22,11,26,15);Line(26,15,26,19);Line(26,19,22,23); }
            else if(symbol=="range") { Circle(lines,16,16,11);Line(16,2,16,9);Line(16,23,16,30);Line(2,16,9,16);Line(23,16,30,16); }
            else if(symbol=="voice") { Box(12,4,8,17);Line(7,16,7,23);Line(7,23,25,23);Line(25,23,25,16);Line(16,23,16,29);Line(10,29,22,29); }
            else if(symbol=="move") { Line(16,29,16,3);Line(16,3,10,9);Line(16,3,22,9);Line(16,29,10,23);Line(16,29,22,23);Line(3,16,29,16);Line(3,16,9,10);Line(3,16,9,22);Line(29,16,23,10);Line(29,16,23,22); }
            else { Box(4,5,24,18);Line(9,23,9,28);Line(9,28,15,23);Line(10,11,22,11);Line(10,17,18,17); }
            var pixels=new Color[1024];
            for(int y=0;y<32;y++) for(int x=0;x<32;x++)
            {
                var p=new Vector2(x+.5f,31.5f-y);float d=100;
                foreach(var l in lines) { var a=new Vector2(l.x,l.y);var b=new Vector2(l.z,l.w);var ab=b-a;d=Mathf.Min(d,(p-a-ab*Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude)).magnitude); }
                pixels[y*32+x]=new Color(1,1,1,Mathf.Clamp01(2.2f-d));
            }
            return Texture(pixels);
        }
        static void Circle(List<Vector4> lines,float x,float y,float radius)
        {
            for(int i=0;i<32;i++) { float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;lines.Add(new Vector4(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius)); }
        }
        static Texture2D Texture(Color[] pixels,int size=32)
        {
            var t=new Texture2D(size,size,TextureFormat.RGBA32,false) { hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp };
            t.SetPixels(pixels);t.Apply(false,true);return t;
        }
    }
}
