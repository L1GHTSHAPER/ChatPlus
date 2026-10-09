using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ChatPlus
{
    /// <summary>Reads the real right-hand game buttons. Sprite assets remain owned by the game.</summary>
    internal static class NativeSidebar
    {
        static readonly FieldInfo RightButtons = AccessTools.Field(typeof(OverlayManager), "_rightTopButtons");
        static readonly FieldInfo IdleColor = AccessTools.Field(typeof(UIManager), "_extraButtonColor");
        static readonly FieldInfo HoverColor = AccessTools.Field(typeof(UIManager), "_extraButtonHoverColor");
        static UIManager _ui;
        static OverlayManager _overlay;
        static Button _button;
        static float _retry;
        static readonly Vector3[] Corners = new Vector3[4];

        internal static Button Button
        {
            get
            {
                if ((_overlay == null || _button == null) && Time.unscaledTime >= _retry)
                {
                    _retry = Time.unscaledTime + .5f;
                    _overlay = Object.FindAnyObjectByType<OverlayManager>();
                    if (_overlay != null && RightButtons?.GetValue(_overlay) is List<GameObject> buttons)
                    {
                        foreach (GameObject candidate in buttons)
                        {
                            if (candidate == null) continue;
                            Button button = candidate.GetComponentInChildren<Button>(true);
                            if (button == null || Background(button)?.sprite == null) continue;
                            _button = button;
                            break;
                        }
                    }
                }
                return _button;
            }
        }
        internal static Image Background(Button button) => button == null ? null : button.GetComponent<Image>() ?? button.targetGraphic as Image;
        internal static Image Outline(Button button) => button == null ? null : button.transform.Find("Image_Outline")?.GetComponent<Image>();
        internal static Image Icon(Button button) => button == null ? null : button.transform.Find("Image_Icon")?.GetComponent<Image>();
        internal static Color SurfaceColor(bool highlighted)
        {
            if (_ui == null) _ui = Object.FindAnyObjectByType<UIManager>();
            FieldInfo field = highlighted ? HoverColor : IdleColor;
            if (_ui != null && field?.GetValue(_ui) is Color color) return color;
            return new Color(.9608f,.9294f,.8824f, highlighted ? 1f : .749f);
        }
        internal static Rect ScreenRect(RectTransform rect, Canvas canvas)
        {
            rect.GetWorldCorners(Corners);
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 a = RectTransformUtility.WorldToScreenPoint(camera,Corners[0]);
            Vector2 b = RectTransformUtility.WorldToScreenPoint(camera,Corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x,b.x), Screen.height-Mathf.Max(a.y,b.y),
                Mathf.Max(a.x,b.x), Screen.height-Mathf.Min(a.y,b.y));
        }
        internal static float PixelScale(Image image) =>
            ScreenRect(image.rectTransform,image.canvas).height / Mathf.Max(1f,image.rectTransform.rect.height);
        internal static void CopyImage(Image source, Image target)
        {
            target.sprite = source.sprite;
            target.type = source.type;
            target.material = source.material;
            target.color = source.color;
            target.preserveAspect = source.preserveAspect;
            target.fillCenter = source.fillCenter;
            target.maskable = source.maskable;
            // Our canvas uses screen pixels; retain the native border's screen thickness.
            float referenceRatio = target.canvas.referencePixelsPerUnit / source.canvas.referencePixelsPerUnit;
            target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier * referenceRatio / Mathf.Max(.01f,PixelScale(source));
        }
        internal static void CopyRect(RectTransform source, RectTransform target, float scale)
        {
            target.anchorMin = source.anchorMin; target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.sizeDelta = source.sizeDelta * scale;
            target.anchoredPosition = source.anchoredPosition * scale;
            target.localScale = source.localScale; target.localRotation = source.localRotation;
        }
        internal static bool TryGeometry(out Rect bounds, out float gap, out float preferredTop)
        {
            bounds = default; gap = preferredTop = 0f;
            Button native = Button;
            Image background = Background(native);
            if (background == null || background.canvas == null) return false;
            bounds = ScreenRect((RectTransform)native.transform,background.canvas);
            if (bounds.width < 1f || bounds.height < 1f) return false;
            float pitch = float.MaxValue, lastTop = bounds.y;
            if (_overlay != null && RightButtons?.GetValue(_overlay) is List<GameObject> groups)
            {
                foreach (GameObject group in groups)
                {
                    if (group == null) continue;
                    foreach (Button button in group.GetComponentsInChildren<Button>(true))
                    {
                        Image image = Background(button);
                        if (!button.gameObject.activeInHierarchy || image == null || image.canvas == null || image.sprite != background.sprite) continue;
                        Rect other = ScreenRect((RectTransform)button.transform,image.canvas);
                        if (Mathf.Abs(other.x-bounds.x) > bounds.height*.15f || Mathf.Abs(other.height-bounds.height) > bounds.height*.15f) continue;
                        lastTop = Mathf.Max(lastTop,other.y);
                        float distance = Mathf.Abs(other.y-bounds.y);
                        if (distance > bounds.height) pitch = Mathf.Min(pitch,distance);
                    }
                }
            }
            gap = pitch < float.MaxValue ? pitch-bounds.height : bounds.height*.35f;
            preferredTop = lastTop+bounds.height+gap;
            return true;
        }
        internal static float Opacity
        {
            get
            {
                Button button = Button;
                Image image = Background(button);
                if (_overlay == null || button == null || image == null) return 0f;
                Canvas canvas = image.canvas;
                float alpha = image.canvasRenderer.GetInheritedAlpha();
                if (!SidebarVisibility.Show(_overlay.ResolutionMode == ResolutionMode.Desk,
                    button.gameObject.activeInHierarchy, canvas != null && canvas.isActiveAndEnabled, alpha)) return 0f;
                return alpha;
            }
        }
        internal static Vector2 Size
        {
            get
            {
                Button button = Button;
                Image image = Background(button);
                if (button == null || image == null || image.canvas == null) return Vector2.zero;
                return ScreenRect((RectTransform)button.transform,image.canvas).size;
            }
        }
    }
}
