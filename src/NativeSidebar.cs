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
        internal static Image Icon(Button button)
        {
            if (button == null) return null;
            Image background = Background(button);
            foreach (Image image in button.GetComponentsInChildren<Image>(true))
                if (image != background && image.sprite != null) return image;
            return null;
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
                ((RectTransform)button.transform).GetWorldCorners(Corners);
                Camera camera = image.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : image.canvas.worldCamera;
                Vector2 a = RectTransformUtility.WorldToScreenPoint(camera,Corners[0]);
                Vector2 b = RectTransformUtility.WorldToScreenPoint(camera,Corners[2]);
                // Native controls extend past the screen edge; copying the full width makes a stretched pill.
                // Use their visible height as a square size so the functional symbol keeps its proportions.
                float side = Mathf.Abs(b.y-a.y);
                return new Vector2(side,side);
            }
        }
    }
}
