using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChatPlus
{
    /// <summary>
    /// Size and position of the game's chat panel. The game lays the panel out for one fixed size, so on start the
    /// parts that must follow its edges are re-anchored (keeping their current place), the frame image is 9-sliced,
    /// and a resize handle is added. After that the whole panel follows its root's size.
    /// </summary>
    internal static class ChatWindow
    {
        const string FrameName = "Panel_Bg";
        const string InputBackgroundName = "ImageInputFieldBg";
        const string MoveHandleName = "Image_Drag";
        // The frame sprite (1644x1876 pixels) is 9-sliced with these borders (left, bottom, right, top, in sprite
        // pixels): the rounded corners and the bottom part around the input field keep their size.
        static readonly Vector2 FrameSpriteSize = new Vector2(1644f, 1876f);
        static readonly Vector4 FrameBorder = new Vector4(108f, 366f, 114f, 120f);
        // Width of a chat line in the game's MessageText prefab, in case the prefab cannot be read.
        const float DefaultLineWidth = 697.1f;
        // The game keeps the panel this far below the top of the screen while it is dragged.
        const float TopMargin = 25f;
        // The game's drag limit on the right: the panel's pivot stays this far from the screen edge.
        const float GameRightLimit = 120f;
        // The smallest size, as a fraction of the game's size.
        const float MinWidthFactor = 0.7f;
        const float MinHeightFactor = 0.5f;
        const float GripSize = 46f;
        static readonly Vector2 GripOffset = new Vector2(5f, 6f);
        // After the width changed, lines are re-wrapped once it has stayed the same this long (see HoldReflow).
        const float ReflowDelay = 0.3f;

        /// <summary>The clipping of one chat tab's viewport: the game's stencil Mask, or a RectMask2D that culls.</summary>
        sealed class Viewport
        {
            public Mask Mask;
            public Image MaskImage;
            public RectMask2D RectMask;
        }

        static UIManager _ui;
        static RectTransform _root;
        static GameObject _grip;
        static Vector2 _defaultSize;
        // Bottom-left corner of the panel where the game places it, relative to the bottom-left of the screen.
        static Vector2 _defaultCorner;
        static float _rightMargin;
        static bool _resizing;
        static float _reflowAt = -1f;
        static readonly List<VerticalLayoutGroup> Layouts = new List<VerticalLayoutGroup>();
        static readonly List<Viewport> Viewports = new List<Viewport>();
        static Texture2D _gripTexture;
        static Sprite _gripSprite;
        static Sprite _slicedFrame;
        static Sprite _slicedFrameSource;

        internal static bool Ready => _ui != null && _root != null;
        internal static Vector2 Size => _root != null ? _root.sizeDelta : Vector2.zero;

        /// <summary>Called after UIManager.Start, i.e. once per lobby, when the game has placed the panel.</summary>
        internal static void Prepare(UIManager ui)
        {
            GameAccess.SetUI(ui);
            RectTransform root = GameAccess.PanelRoot(ui);
            if (root == null || (ui == _ui && root == _root))
                return;

            // The previous lobby's panel is gone. Until this one is fully set up, nothing resizes it: if a step below
            // fails, the panel keeps the game's size, and the parts re-anchored so far are still in the right place.
            _ui = null;
            _root = null;
            _grip = null;
            _resizing = false;
            ChatSelection.Clear();
            _reflowAt = -1f;
            Layouts.Clear();
            Viewports.Clear();

            Vector2 defaultSize = root.sizeDelta;
            RectTransform corner = GameAccess.CornerBottomLeft(ui);
            if (corner != null)
                Reanchor(corner, Vector2.zero, Vector2.zero);
            GameObject backgroundObject = GameAccess.PanelBackground(ui);
            RectTransform background = backgroundObject != null ? backgroundObject.transform as RectTransform : null;
            if (background != null)
            {
                Reanchor(background, Vector2.zero, Vector2.one);
                RectTransform frame = background.Find(FrameName) as RectTransform;
                if (frame != null)
                {
                    SliceFrame(frame.GetComponent<Image>());
                    BackgroundOpacity.Attach(frame.GetComponent<Image>());
                    Reanchor(frame, Vector2.zero, Vector2.one);
                }
                RectTransform inputBackground = background.Find(InputBackgroundName) as RectTransform;
                if (inputBackground != null)
                {
                    BackgroundOpacity.Attach(inputBackground.GetComponent<Image>());
                    Reanchor(inputBackground, Vector2.zero, new Vector2(1f, 0f));
                }
                RectTransform moveHandle = background.Find(MoveHandleName) as RectTransform;
                if (moveHandle != null)
                    Reanchor(moveHandle, new Vector2(1f, 0f), new Vector2(1f, 0f));
                CreateGrip(ui, background);
            }
            Transform globalScroll = GameAccess.GlobalScroll(ui);
            Transform localScroll = GameAccess.LocalScroll(ui);
            if (globalScroll is RectTransform globalRect)
                Reanchor(globalRect, Vector2.zero, Vector2.one);
            if (localScroll is RectTransform localRect)
                Reanchor(localRect, Vector2.zero, Vector2.one);
            TMP_InputField input = ui.MessageInput;
            if (input != null && input.transform is RectTransform inputRect)
                Reanchor(inputRect, Vector2.zero, new Vector2(1f, 0f));

            TMP_Text prefab = GameAccess.TextPrefab(GameAccess.FindChat());
            float lineWidth = prefab != null && prefab.rectTransform.sizeDelta.x > 1f ? prefab.rectTransform.sizeDelta.x : DefaultLineWidth;
            PrepareContent(GameAccess.Content(ui, false), lineWidth);
            PrepareContent(GameAccess.Content(ui, true), lineWidth);
            PrepareViewport(globalScroll);
            PrepareViewport(localScroll);
            ApplyCulling();

            _defaultSize = defaultSize;
            _defaultCorner = root.anchoredPosition - Vector2.Scale(root.pivot, defaultSize);
            _rightMargin = GameRightLimit - (1f - root.pivot.x) * defaultSize.x;
            _ui = ui;
            _root = root;

            Plugin plugin = Plugin.Instance;
            Vector2? place = null;
            if (plugin.RememberPosition.Value && TryParsePosition(plugin.Position.Value, out Vector2 saved))
                place = saved;
            Apply(place);
        }

        /// <summary>Applies the size settings, keeping the panel's bottom-left corner in place.</summary>
        internal static void Apply()
        {
            Apply(null);
        }

        static void Apply(Vector2? corner)
        {
            if (!Ready)
                return;
            Plugin plugin = Plugin.Instance;
            Vector2 keep = corner ?? Corner;
            Vector2 size = ClampSize(new Vector2(_defaultSize.x * plugin.Width.Value / 100f, _defaultSize.y * plugin.Height.Value / 100f));
            if (Mathf.Abs(size.x - _root.sizeDelta.x) > 0.01f)
                HoldReflow();
            _root.sizeDelta = size;
            SetCorner(keep);
            if (_grip != null && _grip.activeSelf != plugin.ResizeHandle.Value)
                _grip.SetActive(plugin.ResizeHandle.Value);
        }

        /// <summary>Moves the panel to the position in the settings (after an edit of the config file).</summary>
        internal static void ApplyPosition()
        {
            Plugin plugin = Plugin.Instance;
            if (!Ready || !plugin.RememberPosition.Value || !TryParsePosition(plugin.Position.Value, out Vector2 corner))
                return;
            if ((corner - Corner).sqrMagnitude > 0.25f)
                SetCorner(corner);
        }

        internal static void ApplyOpacity()
        {
            if (!Ready) return;
            foreach (BackgroundOpacity opacity in _root.GetComponentsInChildren<BackgroundOpacity>(true))
                opacity.Refresh();
        }

        /// <summary>Called every frame: re-wraps the lines once a width change has settled.</summary>
        internal static void Tick()
        {
            if (_reflowAt < 0f || _resizing || Time.unscaledTime < _reflowAt)
                return;
            _reflowAt = -1f;
            foreach (VerticalLayoutGroup layout in Layouts)
            {
                if (layout != null)
                    layout.childControlWidth = true;
            }
        }

        /// <summary>
        /// While the width keeps changing (resize handle or slider), the lines keep their width instead of being
        /// re-wrapped on every step, which with hundreds of lines would stutter. See Tick.
        /// </summary>
        static void HoldReflow()
        {
            foreach (VerticalLayoutGroup layout in Layouts)
            {
                if (layout != null && layout.childControlWidth)
                    layout.childControlWidth = false;
            }
            _reflowAt = Time.unscaledTime + ReflowDelay;
        }

        /// <summary>After UIManager.BeginDragMessage: the game's limit on the right assumes the default width.</summary>
        internal static void OnBeginMove(UIManager ui)
        {
            if (Ready && ui == _ui)
                UpdateDragLimits(true);
        }

        /// <summary>After UIManager.EndDragMessage: remembers where the panel was put.</summary>
        internal static void OnEndMove(UIManager ui)
        {
            if (Ready && ui == _ui)
                SavePosition();
        }

        internal static void BeginResize()
        {
            if (!Ready)
                return;
            _resizing = true;
            // Keeps the game from fading the panel out while the mouse is outside it.
            GameAccess.SetPanelHeld(_ui, true);
        }

        internal static void ResizeTo(Vector2 size)
        {
            if (!Ready)
                return;
            size = ClampSize(size);
            Plugin plugin = Plugin.Instance;
            // The settings apply the size (see Plugin.OnSettingChanged).
            plugin.Width.Value = Mathf.RoundToInt(size.x / _defaultSize.x * 100f);
            plugin.Height.Value = Mathf.RoundToInt(size.y / _defaultSize.y * 100f);
        }

        internal static void EndResize()
        {
            if (!_resizing)
                return;
            _resizing = false;
            if (_reflowAt >= 0f)
                _reflowAt = Time.unscaledTime;
            if (!Ready)
                return;
            GameAccess.SetPanelHeld(_ui, false);
            SavePosition();
        }

        internal static void ResetSize()
        {
            Plugin plugin = Plugin.Instance;
            plugin.Width.Value = 100;
            plugin.Height.Value = 100;
        }

        internal static void ResetPosition()
        {
            Plugin.Instance.Position.Value = string.Empty;
            if (!Ready)
                return;
            SetCorner(_defaultCorner);
        }

        /// <summary>
        /// Off-screen lines of a long chat are culled with a RectMask2D instead of the game's stencil Mask (whose
        /// lines are all drawn, only hidden). The setting switches between the two.
        /// </summary>
        internal static void ApplyCulling()
        {
            bool cull = Plugin.Instance.CullHiddenLines.Value;
            foreach (Viewport viewport in Viewports)
            {
                if (viewport.Mask == null || viewport.RectMask == null)
                    continue;
                if (cull)
                {
                    viewport.Mask.enabled = false;
                    // The mask's image is drawn only into the stencil; without the mask it would show.
                    if (viewport.MaskImage != null && !viewport.Mask.showMaskGraphic)
                        viewport.MaskImage.enabled = false;
                    viewport.RectMask.enabled = true;
                }
                else
                {
                    viewport.RectMask.enabled = false;
                    if (viewport.MaskImage != null)
                        viewport.MaskImage.enabled = true;
                    viewport.Mask.enabled = true;
                }
            }
        }

        /// <summary>Converts a pointer position into the panel's parent space, in the panel's own units.</summary>
        internal static bool TryPointer(Vector2 screenPosition, Camera camera, out Vector2 point)
        {
            point = Vector2.zero;
            if (!Ready || !(_root.parent is RectTransform parent))
                return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, camera, out point))
                return false;
            Vector3 scale = _root.localScale;
            if (Mathf.Abs(scale.x) > 0.0001f && Mathf.Abs(scale.y) > 0.0001f)
                point = new Vector2(point.x / scale.x, point.y / scale.y);
            return true;
        }

        static Vector2 Corner => _root.anchoredPosition - Vector2.Scale(_root.pivot, _root.sizeDelta);

        static Vector2 Area
        {
            get
            {
                RectTransform parent = _root.parent as RectTransform;
                return parent != null ? parent.rect.size : Vector2.zero;
            }
        }

        static void SetCorner(Vector2 corner)
        {
            Vector2 size = _root.sizeDelta;
            _root.anchoredPosition = ClampCorner(corner, size) + Vector2.Scale(_root.pivot, size);
            UpdateDragLimits(false);
        }

        /// <summary>
        /// Keeps the panel where the game would let you drag it: not further left or down than its default place,
        /// and with its top (tabs) and right side (hide button) on the screen.
        /// </summary>
        static Vector2 ClampCorner(Vector2 corner, Vector2 size)
        {
            Vector2 area = Area;
            if (area.x < 100f || area.y < 100f)
                return corner;
            float maxX = area.x - size.x - _rightMargin;
            float maxY = area.y - TopMargin - size.y - _root.pivot.y * size.y;
            corner.x = Mathf.Max(_defaultCorner.x, Mathf.Min(corner.x, maxX));
            corner.y = Mathf.Max(_defaultCorner.y, Mathf.Min(corner.y, maxY));
            return corner;
        }

        static Vector2 ClampSize(Vector2 size)
        {
            float minWidth = _defaultSize.x * MinWidthFactor;
            float minHeight = _defaultSize.y * MinHeightFactor;
            Vector2 area = Area;
            float maxWidth = area.x > 100f ? area.x - _defaultCorner.x - _rightMargin : float.MaxValue;
            float maxHeight = area.y > 100f ? (area.y - TopMargin - _defaultCorner.y) / (1f + _root.pivot.y) : float.MaxValue;
            size.x = Mathf.Clamp(size.x, minWidth, Mathf.Max(minWidth, maxWidth));
            size.y = Mathf.Clamp(size.y, minHeight, Mathf.Max(minHeight, maxHeight));
            return size;
        }

        /// <summary>The game's drag limits are pivot positions computed for the default size; these fit the current one.</summary>
        static void UpdateDragLimits(bool includeRight)
        {
            Vector2 size = _root.sizeDelta;
            Vector2 pivot = _root.pivot;
            float minX = _defaultCorner.x + pivot.x * size.x;
            float minY = _defaultCorner.y + pivot.y * size.y;
            float? maxX = null;
            Vector2 area = Area;
            if (includeRight && area.x > 100f)
                maxX = Mathf.Max(minX, area.x - (1f - pivot.x) * size.x - _rightMargin);
            GameAccess.SetDragLimits(_ui, minX, minY, maxX);
        }

        static void SavePosition()
        {
            Plugin plugin = Plugin.Instance;
            if (!plugin.RememberPosition.Value)
                return;
            Vector2 corner = Corner;
            plugin.Position.Value = corner.x.ToString("0.#", CultureInfo.InvariantCulture) + ";" +
                                    corner.y.ToString("0.#", CultureInfo.InvariantCulture);
        }

        internal static bool TryParsePosition(string value, out Vector2 corner)
        {
            corner = Vector2.zero;
            if (string.IsNullOrEmpty(value))
                return false;
            string[] parts = value.Split(';');
            if (parts.Length != 2 ||
                !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
                !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                return false;
            corner = new Vector2(x, y);
            return true;
        }

        /// <summary>Changes a RectTransform's anchors without moving or resizing it.</summary>
        static void Reanchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (!(rect.parent is RectTransform parent))
                return;
            Vector2 size = parent.rect.size;
            Vector2 min = Vector2.Scale(rect.anchorMin, size) + rect.offsetMin;
            Vector2 max = Vector2.Scale(rect.anchorMax, size) + rect.offsetMax;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = min - Vector2.Scale(anchorMin, size);
            rect.offsetMax = max - Vector2.Scale(anchorMax, size);
        }

        /// <summary>
        /// Lets the chat lines fill the width of the tab. The padding is chosen so that, at the default size, lines
        /// are exactly as wide as the game makes them.
        /// </summary>
        static void PrepareContent(Transform content, float lineWidth)
        {
            if (content == null)
                return;
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout != null && !layout.childControlWidth && content is RectTransform rect && rect.rect.width > lineWidth * 0.5f)
            {
                RectOffset padding = layout.padding;
                int right = Mathf.RoundToInt(rect.rect.width - padding.left - lineWidth);
                layout.padding = new RectOffset(padding.left, right, padding.top, padding.bottom);
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
                Layouts.Add(layout);
            }
            if (content.GetComponent<LineSizer>() == null)
                content.gameObject.AddComponent<LineSizer>();
        }

        static void PrepareViewport(Transform scroll)
        {
            ScrollRect scrollRect = scroll != null ? scroll.GetComponent<ScrollRect>() : null;
            RectTransform viewport = scrollRect != null ? scrollRect.viewport : null;
            Mask mask = viewport != null ? viewport.GetComponent<Mask>() : null;
            if (mask == null)
                return;
            RectMask2D rectMask = viewport.GetComponent<RectMask2D>();
            if (rectMask == null)
            {
                rectMask = viewport.gameObject.AddComponent<RectMask2D>();
                rectMask.enabled = false;
            }
            Viewports.Add(new Viewport { Mask = mask, MaskImage = viewport.GetComponent<Image>(), RectMask = rectMask });
        }

        /// <summary>
        /// The frame is a single image drawn for the default size; stretching it would stretch its rounded corners and
        /// borders, so it is cut into 9 parts with the same look at the default size.
        /// </summary>
        static void SliceFrame(Image image)
        {
            if (image == null || image.type != Image.Type.Simple)
                return;
            Sprite sprite = image.sprite;
            if (sprite == null || sprite.packed || sprite.border != Vector4.zero)
                return;
            Rect spriteRect = sprite.rect;
            if (Mathf.Abs(spriteRect.width - FrameSpriteSize.x) > 0.5f || Mathf.Abs(spriteRect.height - FrameSpriteSize.y) > 0.5f)
                return;
            float width = image.rectTransform.rect.width;
            float pixelsPerUnit = image.pixelsPerUnit;
            if (width <= 1f || pixelsPerUnit <= 0f)
                return;
            // The sprite is an asset that outlives the lobby, so its sliced copy is made once.
            if (_slicedFrame == null || _slicedFrameSource != sprite)
            {
                _slicedFrame = Sprite.Create(sprite.texture, spriteRect,
                    new Vector2(sprite.pivot.x / spriteRect.width, sprite.pivot.y / spriteRect.height),
                    sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect, FrameBorder);
                _slicedFrame.name = sprite.name + " (ChatPlus 9-slice)";
                _slicedFrameSource = sprite;
            }
            image.sprite = _slicedFrame;
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            // Same scale as the stretched original: the whole sprite width maps to the image's width.
            image.pixelsPerUnitMultiplier = spriteRect.width / (width * pixelsPerUnit);
        }

        static void CreateGrip(UIManager ui, RectTransform background)
        {
            var grip = new GameObject("ChatPlus.ResizeHandle", typeof(RectTransform));
            var rect = (RectTransform)grip.transform;
            rect.SetParent(background, false);
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(GripSize, GripSize);
            rect.anchoredPosition = GripOffset;
            rect.SetAsLastSibling();

            Image image = grip.AddComponent<Image>();
            image.sprite = GripSprite();
            image.raycastTarget = true;
            // The game fades the panel's images in and out; the handle joins them.
            List<Image> images = GameAccess.PanelImages(ui);
            if (images != null)
            {
                if (images.Count > 0 && images[0] != null)
                {
                    Color color = image.color;
                    color.a = images[0].color.a;
                    image.color = color;
                }
                images.Add(image);
            }
            grip.AddComponent<ResizeGrip>();
            grip.SetActive(Plugin.Instance.ResizeHandle.Value);
            _grip = grip;
        }

        static Sprite GripSprite()
        {
            if (_gripSprite != null)
                return _gripSprite;
            _gripTexture = GripArt.Draw();
            _gripSprite = Sprite.Create(_gripTexture, new Rect(0f, 0f, _gripTexture.width, _gripTexture.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            _gripSprite.name = "ChatPlus.ResizeHandle";
            return _gripSprite;
        }

        internal static void Unload()
        {
            if (_grip != null)
                UnityEngine.Object.Destroy(_grip);
            _grip = null;
            if (_gripSprite != null)
                UnityEngine.Object.Destroy(_gripSprite);
            if (_gripTexture != null)
                UnityEngine.Object.Destroy(_gripTexture);
            _gripSprite = null;
            _gripTexture = null;
        }
    }
}
