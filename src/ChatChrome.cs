using TMPro;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace ChatPlus
{
    /// <summary>Native chat header: gestures and settings, without moving the input or move handle.</summary>
    internal sealed class ChatChrome : MonoBehaviour
    {
        const float HeaderHeight = 86f;
        static readonly Color Ink = new Color(0.325f, 0.263f, 0.204f, 1f);
        static readonly Color Coral = new Color(0.976f, 0.867f, 0.784f, 1f);
        RectTransform _global;
        RectTransform _local;
        RectTransform _footer;
        TMP_Text _hint;
        Image _gearImage;
        Image _frame;
        Image _buttonImage;
        Button _button;
        Texture2D _texture;
        Sprite _sprite;
        Texture2D _gearTexture;
        Sprite _gearSprite;
        Material _fontMaterial;
        string _hintText;
        Lang _lang;
        bool _selection;
        KeyboardShortcut _shortcut;
        bool _textsReady;

        internal static void Attach(RectTransform root, Image frame, Transform global, Transform local)
        {
            if (root == null || frame == null || root.GetComponent<ChatChrome>() != null) return;
            var chrome = root.gameObject.AddComponent<ChatChrome>();
            chrome._frame = frame;
            chrome._global = global as RectTransform;
            chrome._local = local as RectTransform;
            chrome.Create(frame.rectTransform);
        }

        void Create(RectTransform frame)
        {
            var footer = new GameObject("ChatPlus.ChatHeader", typeof(RectTransform));
            _footer = (RectTransform)footer.transform;
            // Use the visible frame's coordinates; its outer parent has a different origin.
            _footer.SetParent(frame, false);
            _footer.anchorMin = new Vector2(0f, 1f);
            _footer.anchorMax = Vector2.one;
            _footer.pivot = new Vector2(0.5f, 1f);
            _footer.offsetMin = new Vector2(48f, -116f);
            _footer.offsetMax = new Vector2(-48f, -48f);

            _hint = Text("Gestures", _footer);
            _hint.rectTransform.offsetMin = Vector2.zero;
            _hint.rectTransform.offsetMax = new Vector2(-88f, 0f);
            _hint.fontSize = 22f;
            _hint.enableAutoSizing = true;
            _hint.fontSizeMin = 18f;
            _hint.fontSizeMax = 22f;
            _hint.alignment = TextAlignmentOptions.MidlineLeft;

            var buttonObject = new GameObject("ChatPlus.SettingsButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)buttonObject.transform;
            rect.SetParent(_footer, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(72f, 68f);
            rect.anchoredPosition = Vector2.zero;
            _buttonImage = buttonObject.GetComponent<Image>();
            _texture = ButtonTexture();
            _sprite = Sprite.Create(_texture, new Rect(0f, 0f, 48f, 48f), new Vector2(.5f, .5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(12f, 12f, 12f, 12f));
            _buttonImage.sprite = _sprite;
            _buttonImage.type = Image.Type.Sliced;
            BackgroundOpacity.Attach(_buttonImage);
            _button = buttonObject.GetComponent<Button>();
            _button.targetGraphic = _buttonImage;
            _button.navigation = new Navigation { mode = Navigation.Mode.None };
            _button.onClick.AddListener(() => Plugin.Instance?.ToggleWindow());
            var gearObject = new GameObject("Gear", typeof(RectTransform), typeof(Image));
            var gearRect = (RectTransform)gearObject.transform;
            gearRect.SetParent(rect, false);
            gearRect.anchorMin = gearRect.anchorMax = gearRect.pivot = new Vector2(.5f, .5f);
            gearRect.sizeDelta = new Vector2(44f, 44f);
            gearRect.anchoredPosition = Vector2.zero;
            _gearTexture = GearTexture();
            _gearSprite = Sprite.Create(_gearTexture, new Rect(0f, 0f, 48f, 48f), new Vector2(.5f, .5f), 100f);
            _gearImage = gearObject.GetComponent<Image>();
            _gearImage.sprite = _gearSprite;
            _gearImage.raycastTarget = false;
            BackgroundOpacity.Attach(_gearImage);
            buttonObject.AddComponent<SettingsButtonHint>();

            // Reserve space above messages; the input and the native move handle keep their original layout.
            ReserveScroll(_global, HeaderHeight);
            ReserveScroll(_local, HeaderHeight);
        }

        static void ReserveScroll(RectTransform scroll, float height)
        {
            if (scroll != null) scroll.offsetMax -= new Vector2(0f, height);
        }

        static TMP_Text Text(string name, RectTransform parent)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.rectTransform.SetParent(parent, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            text.raycastTarget = false;
            text.richText = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        void LateUpdate()
        {
            Plugin plugin = Plugin.Instance;
            if (plugin == null || _footer == null || _frame == null) return;
            TMP_Text prefab = GameAccess.TextPrefab(GameAccess.Chat);
            if (prefab != null && prefab.font != null && _hint.font != prefab.font)
            {
                _hint.font = prefab.font;
                if (_fontMaterial != null) Destroy(_fontMaterial);
                if (prefab.fontSharedMaterial != null)
                {
                    _fontMaterial = new Material(prefab.fontSharedMaterial) { hideFlags = HideFlags.HideAndDontSave };
                    _fontMaterial.DisableKeyword("UNDERLAY_ON");
                    _hint.fontSharedMaterial = _fontMaterial;
                }
            }
            Lang lang = Lang.Current;
            KeyboardShortcut shortcut = plugin.WindowKey.Value;
            if (!_textsReady || lang != _lang || _selection != plugin.SelectText.Value || !shortcut.Equals(_shortcut))
            {
                _lang = lang;
                _selection = plugin.SelectText.Value;
                _shortcut = shortcut;
                _textsReady = true;
                string hint = lang.ChatScrollHint + (_selection ? " · " + lang.ChatSelectHint + "\n" + lang.ChatCopyHint : string.Empty);
                if (_hintText != hint) _hint.text = _hintText = hint;
            }
            float alpha = _frame.gameObject.activeInHierarchy ? _frame.color.a : 0f;
            _hint.color = new Color(0.96f, 0.93f, 0.88f, alpha);
            _gearImage.color = new Color(Ink.r, Ink.g, Ink.b, alpha);
            _buttonImage.color = new Color(Coral.r, Coral.g, Coral.b, alpha);
            _button.interactable = alpha > 0.05f;
            _buttonImage.raycastTarget = alpha > 0.05f;
        }

        static Texture2D ButtonTexture()
        {
            const int size = 48;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + .5f - size / 2f) - 14f, 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + .5f - size / 2f) - 14f, 0f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(10.5f - Mathf.Sqrt(dx * dx + dy * dy)));
                }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { name = "ChatPlus.SettingsButton", hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        static Texture2D GearTexture()
        {
            const int size = 48;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + .5f - size / 2f;
                    float dy = y + .5f - size / 2f;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    float teeth = Mathf.Clamp01((Mathf.Cos(Mathf.Atan2(dy, dx) * 8f) - .1f) * 2f);
                    float alpha = Mathf.Clamp01(16.5f + 3f * teeth - radius) * Mathf.Clamp01(radius - 5.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { name = "ChatPlus.SettingsGear", hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        void OnDestroy()
        {
            ReserveScroll(_global, -HeaderHeight);
            ReserveScroll(_local, -HeaderHeight);
            if (_footer != null) Destroy(_footer.gameObject);
            if (_fontMaterial != null) Destroy(_fontMaterial);
            if (_sprite != null) Destroy(_sprite);
            if (_texture != null) Destroy(_texture);
            if (_gearSprite != null) Destroy(_gearSprite);
            if (_gearTexture != null) Destroy(_gearTexture);
        }
    }

    internal sealed class SettingsButtonHint : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler
    {
        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData pointer)
        {
            if (Plugin.Instance == null) return;
            var shortcut = Plugin.Instance.WindowKey.Value;
            Toast.Show(Lang.Current.SettingsShort + (shortcut.MainKey == KeyCode.None ? string.Empty : " · " + shortcut));
        }
    }
}
