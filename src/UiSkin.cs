using System.Collections.Generic;
using UnityEngine;

namespace ChatPlus
{
    /// <summary>Styles of the settings window. The textures are drawn in code, so the mod ships no image files.</summary>
    internal sealed class UiSkin
    {
        internal static readonly Color TextColor = Rgb(0x534334);
        static readonly Color LabelColor = Rgb(0x534334);
        static readonly Color MutedColor = Rgb(0x796B5B);
        static readonly Color AccentColor = Rgb(0xAB503D);
        static readonly Color AccentHover = Rgb(0xC16A52);
        static readonly Color WindowColor = Rgb(0xFFF6E6);
        static readonly Color WindowBorder = Rgb(0xCDB895);
        static readonly Color PanelColor = Rgb(0xFFFAF0);
        static readonly Color PanelBorder = Rgb(0xD7C3A5);
        static readonly Color FieldColor = Rgb(0xFFF3DD);
        static readonly Color ButtonColor = Rgb(0xF4E8D2);
        static readonly Color ButtonHover = Rgb(0xF9DDC8);
        static readonly Color ButtonActive = Rgb(0xF2CCAF);
        static readonly Color ButtonBorder = Rgb(0xD7C3A5);
        static readonly Color CheckColor = Rgb(0xFFFAF0);
        static readonly Color CheckBorder = Rgb(0xB39E81);
        static readonly Color TrackColor = Rgb(0xDED1BA);
        static readonly Color ThumbColor = AccentColor;

        public readonly GUIStyle Window;
        public readonly GUIStyle Panel;
        public readonly GUIStyle Row;
        public readonly GUIStyle Title;
        public readonly GUIStyle Label;
        public readonly GUIStyle ToggleLabel;
        public readonly GUIStyle SectionTitle;
        public readonly GUIStyle Value;
        public readonly GUIStyle Field;
        public readonly GUIStyle EditorInput;
        public readonly GUIStyle EditorHex;
        public readonly GUIStyle ColorButton;
        public readonly GUIStyle Hint;
        public readonly GUIStyle Button;
        public readonly GUIStyle SmallButton;
        public readonly GUIStyle CloseButton;
        public readonly GUIStyle Tab;
        public readonly GUIStyle SelectedTab;
        public readonly GUIStyle Subtitle;
        public readonly GUIStyle Preview;
        public readonly GUIStyle PreviewPaper;
        public readonly GUIStyle Logo;
        public readonly GUIStyle Spacer;
        public readonly GUIStyle Check;
        public readonly GUIStyle Slider;
        public readonly GUIStyle Thumb;
        public readonly GUIStyle Swatch;
        /// <summary>A plain white pixel, tinted when drawn (colour samples).</summary>
        public readonly Texture2D White;

        readonly List<Texture2D> _textures = new List<Texture2D>();

        public UiSkin()
        {
            // The window texture has a 10px drop shadow around the box; overflow draws it outside the window rect.
            Texture2D window = Box(72, 72, 18f, WindowColor, WindowBorder, 2f, 10);
            Window = new GUIStyle
            {
                border = new RectOffset(30, 30, 30, 30),
                overflow = new RectOffset(10, 10, 10, 10),
                padding = new RectOffset(20, 20, 16, 14)
            };
            Window.normal.background = window;
            Window.onNormal.background = window;

            Panel = new GUIStyle
            {
                border = new RectOffset(9, 9, 9, 9),
                padding = new RectOffset(12, 12, 8, 10),
                margin = new RectOffset(0, 0, 10, 0)
            };
            Panel.normal.background = Box(24, 24, 8f, PanelColor, PanelBorder);

            Row = new GUIStyle { margin = new RectOffset(0, 0, 1, 1) };

            Title = Text(20, TextColor, FontStyle.Bold);
            Title.fixedHeight = 28f;
            Subtitle = Text(12, MutedColor);
            Subtitle.fixedHeight = 20f;

            Label = Text(13, LabelColor);
            Label.wordWrap = true;
            Label.padding = new RectOffset(0, 0, 4, 4);

            // IMGUI uses a state only when it has a background, so hover colours need an (invisible) one.
            Texture2D clear = Make(1, 1, new[] { new Color(0f, 0f, 0f, 0f) });

            ToggleLabel = Text(13, TextColor);
            ToggleLabel.wordWrap = true;
            ToggleLabel.padding = new RectOffset(0, 8, 4, 4);
            ToggleLabel.hover.textColor = AccentColor;
            ToggleLabel.hover.background = clear;

            SectionTitle = Text(14, AccentColor, FontStyle.Bold);
            SectionTitle.fixedHeight = 24f;
            SectionTitle.padding = new RectOffset(8, 0, 0, 0);
            SectionTitle.hover.textColor = AccentHover;
            SectionTitle.hover.background = clear;

            Value = Text(13, TextColor, FontStyle.Normal, TextAnchor.MiddleRight);
            Value.fixedHeight = 24f;

            Field = Text(13, TextColor, FontStyle.Normal, TextAnchor.MiddleCenter);
            Field.fixedHeight = 24f;
            Field.border = new RectOffset(6, 6, 6, 6);
            Field.padding = new RectOffset(8, 8, 0, 0);
            Field.margin = new RectOffset(2, 2, 0, 0);
            Field.normal.background = Box(16, 16, 5f, FieldColor, PanelBorder);
            EditorHex = new GUIStyle(Field) { alignment = TextAnchor.MiddleLeft };
            EditorInput = new GUIStyle(EditorHex) { fixedHeight = 0f, wordWrap = true, padding = new RectOffset(8, 8, 6, 6) };
            ColorButton = new GUIStyle { fixedWidth = 18f, fixedHeight = 18f, margin = new RectOffset(6, 2, 3, 3) };
            ColorButton.normal.background = Make(1, 1, new[] { Color.white });
            ColorButton.hover.background = ColorButton.normal.background;
            ColorButton.active.background = ColorButton.normal.background;

            Hint = Text(11, MutedColor);
            Hint.wordWrap = true;
            Hint.clipping = TextClipping.Overflow;
            Hint.margin = new RectOffset(0, 0, 2, 0);

            Texture2D button = Box(16, 16, 6f, ButtonColor, ButtonBorder);
            Texture2D buttonHover = Box(16, 16, 6f, ButtonHover, ButtonBorder);
            Texture2D buttonActive = Box(16, 16, 6f, ButtonActive, AccentColor);

            Button = Text(13, TextColor, FontStyle.Normal, TextAnchor.MiddleCenter);
            Button.fixedHeight = 28f;
            Button.border = new RectOffset(7, 7, 7, 7);
            Button.padding = new RectOffset(14, 14, 0, 0);
            Button.margin = new RectOffset(0, 8, 0, 0);
            Button.stretchWidth = false;
            SetBackgrounds(Button, button, buttonHover, buttonActive);

            SmallButton = Text(12, TextColor, FontStyle.Normal, TextAnchor.MiddleCenter);
            SmallButton.fixedHeight = 24f;
            SmallButton.border = new RectOffset(7, 7, 7, 7);
            SmallButton.margin = new RectOffset(2, 2, 0, 0);
            SmallButton.stretchWidth = false;
            SetBackgrounds(SmallButton, button, buttonHover, buttonActive);

            Tab = new GUIStyle(Button) { fixedHeight = 34f, stretchWidth = true, fontStyle = FontStyle.Bold,
                padding = new RectOffset(6, 6, 0, 0), margin = new RectOffset(2, 2, 0, 0) };
            SelectedTab = new GUIStyle(Tab);
            SetBackgrounds(SelectedTab, buttonActive, buttonActive, buttonActive);
            SelectedTab.normal.textColor = AccentColor;
            SelectedTab.hover.textColor = AccentColor;
            SelectedTab.active.textColor = AccentColor;
            Logo = Text(21, AccentColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            Logo.fixedHeight = 42f;
            Logo.fixedWidth = 42f;
            Logo.border = new RectOffset(12, 12, 12, 12);
            Logo.margin = new RectOffset(0, 10, 0, 0);
            Logo.normal.background = Box(32, 32, 10f, ButtonHover, ButtonHover);
            Preview = new GUIStyle(Panel) { margin = new RectOffset(0, 0, 0, 0) };
            Preview.normal.background = Box(24, 24, 8f, Rgb(0xE4DDC9), PanelBorder);
            PreviewPaper = new GUIStyle(Preview);
            PreviewPaper.normal.background = Box(24, 24, 8f, PanelColor, PanelBorder);

            CloseButton = Text(18, MutedColor, FontStyle.Normal, TextAnchor.MiddleCenter);
            CloseButton.fixedWidth = 28f;
            CloseButton.fixedHeight = 26f;
            CloseButton.border = new RectOffset(7, 7, 7, 7);
            CloseButton.padding = new RectOffset(0, 0, 0, 2);
            CloseButton.hover.textColor = TextColor;
            CloseButton.hover.background = buttonHover;
            CloseButton.active.background = buttonActive;

            Spacer = new GUIStyle { fixedHeight = 24f, margin = new RectOffset(2, 2, 0, 0) };

            Swatch = new GUIStyle { fixedWidth = 18f, fixedHeight = 18f, margin = new RectOffset(6, 2, 3, 3) };
            White = Make(1, 1, new[] { Color.white });

            Check = new GUIStyle { fixedWidth = 18f, fixedHeight = 18f, margin = new RectOffset(0, 0, 3, 3) };
            Texture2D off = CheckBox(false, false);
            Texture2D offHover = CheckBox(false, true);
            Texture2D on = CheckBox(true, false);
            Texture2D onHover = CheckBox(true, true);
            Check.normal.background = off;
            Check.hover.background = offHover;
            Check.active.background = offHover;
            Check.onNormal.background = on;
            Check.onHover.background = onHover;
            Check.onActive.background = onHover;

            Slider = new GUIStyle
            {
                fixedHeight = 16f,
                border = new RectOffset(4, 4, 0, 0),
                margin = new RectOffset(4, 8, 4, 4)
            };
            Slider.normal.background = SliderTrack();

            Thumb = new GUIStyle { fixedWidth = 14f, fixedHeight = 16f };
            Texture2D thumb = SliderThumb(ThumbColor);
            Texture2D thumbHover = SliderThumb(AccentHover);
            Thumb.normal.background = thumb;
            Thumb.hover.background = thumbHover;
            Thumb.active.background = thumbHover;

            // Prefer the game's font when its source is available; baked TMP assets may have no source Font.
            Font font = GameAccess.TextPrefab(GameAccess.Chat)?.font?.sourceFontFile;
            if (font != null)
                foreach (GUIStyle style in new[] { Title, Subtitle, Label, ToggleLabel, SectionTitle, Value, Field,
                    EditorInput, EditorHex, Hint, Button, SmallButton, Tab, SelectedTab, CloseButton, Logo })
                    style.font = font;
        }

        public void Destroy()
        {
            foreach (Texture2D texture in _textures)
            {
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);
            }
            _textures.Clear();
        }

        static Color Rgb(int rgb, float alpha = 1f)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
        }

        static GUIStyle Text(int size, Color color, FontStyle fontStyle = FontStyle.Normal, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var style = new GUIStyle
            {
                fontSize = size,
                fontStyle = fontStyle,
                alignment = anchor,
                clipping = TextClipping.Clip,
                wordWrap = false,
                richText = false
            };
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
            return style;
        }

        static void SetBackgrounds(GUIStyle style, Texture2D normal, Texture2D hover, Texture2D active)
        {
            style.normal.background = normal;
            style.hover.background = hover;
            style.active.background = active;
            style.focused.background = normal;
            style.onNormal.background = normal;
            style.onHover.background = hover;
            style.onActive.background = active;
            style.onFocused.background = normal;
        }

        /// <summary>Rounded rectangle with a border and, when shadow &gt; 0, a soft drop shadow in a margin of that many pixels.</summary>
        Texture2D Box(int width, int height, float radius, Color fill, Color border, float borderWidth = 1f, int shadow = 0)
        {
            var pixels = new Color[width * height];
            float boxWidth = width - 2f * shadow;
            float boxHeight = height - 2f * shadow;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float px = x + 0.5f - shadow;
                    float py = y + 0.5f - shadow;
                    float distance = RoundedRectDistance(px, py, boxWidth, boxHeight, radius);
                    Color color = Color.Lerp(border, fill, Mathf.Clamp01(0.5f - distance - borderWidth));
                    color.a *= Mathf.Clamp01(0.5f - distance);
                    if (shadow > 0)
                    {
                        // Texture rows go bottom-up, so sampling 2px higher moves the shadow 2px down on screen.
                        float shadowDistance = Mathf.Max(0f, RoundedRectDistance(px, py + 2f, boxWidth, boxHeight, radius));
                        float falloff = 1f - Mathf.Clamp01(shadowDistance / shadow);
                        color = Over(color, new Color(0f, 0f, 0f, 0.35f * falloff * falloff));
                    }
                    pixels[y * width + x] = color;
                }
            }
            return Make(width, height, pixels);
        }

        Texture2D CheckBox(bool on, bool hover)
        {
            const int size = 18;
            Color fill = on ? (hover ? AccentHover : AccentColor) : CheckColor;
            Color border = on ? fill : (hover ? AccentColor : CheckBorder);
            // Corners of the check mark, written top-down and flipped because texture rows go bottom-up.
            var a = new Vector2(4.6f, size - 9.4f);
            var b = new Vector2(7.6f, size - 12.4f);
            var c = new Vector2(13.4f, size - 5.8f);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float distance = RoundedRectDistance(p.x, p.y, size, size, 4f) + 0.5f;
                    Color color = Color.Lerp(border, fill, Mathf.Clamp01(0.5f - distance - 1.5f));
                    if (on)
                    {
                        float line = Mathf.Min(SegmentDistance(p, a, b), SegmentDistance(p, b, c));
                        color = Color.Lerp(color, Color.white, Mathf.Clamp01(1.6f - line));
                    }
                    color.a *= Mathf.Clamp01(0.5f - distance);
                    pixels[y * size + x] = color;
                }
            }
            return Make(size, size, pixels);
        }

        // 4px high rounded bar in the middle of the slider's 16px height; the sides stretch (border 4).
        Texture2D SliderTrack()
        {
            const int width = 12;
            const int height = 16;
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float distance = RoundedRectDistance(x + 0.5f, y + 0.5f - 6f, width, 4f, 2f);
                    Color color = TrackColor;
                    color.a *= Mathf.Clamp01(0.5f - distance);
                    pixels[y * width + x] = color;
                }
            }
            return Make(width, height, pixels);
        }

        Texture2D SliderThumb(Color fill)
        {
            const int width = 14;
            const int height = 16;
            var center = new Vector2(width * 0.5f, height * 0.5f);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float distance = (new Vector2(x + 0.5f, y + 0.5f) - center).magnitude - 6f;
                    Color color = fill;
                    color.a *= Mathf.Clamp01(0.5f - distance);
                    pixels[y * width + x] = color;
                }
            }
            return Make(width, height, pixels);
        }

        /// <summary>Signed distance from a point to a rounded rectangle spanning (0,0)-(width,height); negative inside.</summary>
        static float RoundedRectDistance(float x, float y, float width, float height, float radius)
        {
            float qx = Mathf.Abs(x - width * 0.5f) - (width * 0.5f - radius);
            float qy = Mathf.Abs(y - height * 0.5f) - (height * 0.5f - radius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
            return outside + inside - radius;
        }

        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + ab * t)).magnitude;
        }

        static Color Over(Color top, Color bottom)
        {
            float alpha = top.a + bottom.a * (1f - top.a);
            if (alpha <= 0f)
                return new Color(0f, 0f, 0f, 0f);
            Color color = (top * top.a + bottom * (bottom.a * (1f - top.a))) / alpha;
            color.a = alpha;
            return color;
        }

        Texture2D Make(int width, int height, Color[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            _textures.Add(texture);
            return texture;
        }
    }
}
