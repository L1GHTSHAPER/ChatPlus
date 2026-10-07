using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChatPlus
{
    /// <summary>Renders with the game's actual TMP font and tag parser, clipped to the settings scroll view.</summary>
    internal sealed class MessagePreview
    {
        GameObject _canvas;
        RectTransform _clip;
        TMP_Text _text;

        internal void Show(Rect bounds, Rect viewport, string message)
        {
            float left = Mathf.Max(bounds.xMin, viewport.xMin);
            float top = Mathf.Max(bounds.yMin, viewport.yMin);
            float right = Mathf.Min(bounds.xMax, viewport.xMax);
            float bottom = Mathf.Min(bounds.yMax, viewport.yMax);
            TMP_Text prefab = GameAccess.TextPrefab(GameAccess.Chat);
            if (right <= left || bottom <= top || prefab == null || prefab.font == null)
            {
                Hide();
                return;
            }
            if (_canvas == null)
            {
                _canvas = new GameObject("ChatPlus.MessagePreview", typeof(Canvas)) { hideFlags = HideFlags.HideAndDontSave };
                Object.DontDestroyOnLoad(_canvas);
                Canvas canvas = _canvas.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = short.MaxValue;
                var clip = new GameObject("Clip", typeof(RectTransform), typeof(RectMask2D));
                clip.transform.SetParent(_canvas.transform, false);
                _clip = clip.GetComponent<RectTransform>();
                _clip.anchorMin = _clip.anchorMax = _clip.pivot = new Vector2(0f, 1f);
                var text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                text.transform.SetParent(_clip, false);
                _text = text.GetComponent<TextMeshProUGUI>();
                _text.rectTransform.anchorMin = _text.rectTransform.anchorMax = _text.rectTransform.pivot = new Vector2(0f, 1f);
                _text.raycastTarget = false;
                _text.richText = true;
                _text.enableAutoSizing = false;
                _text.overflowMode = TextOverflowModes.Truncate;
            }
            _canvas.SetActive(true);
            _clip.anchoredPosition = new Vector2(left, -top);
            _clip.sizeDelta = new Vector2(right - left, bottom - top);
            _text.rectTransform.anchoredPosition = new Vector2(bounds.xMin - left, top - bounds.yMin);
            _text.rectTransform.sizeDelta = bounds.size;
            if (_text.font != prefab.font)
            {
                _text.font = prefab.font;
                _text.fontSharedMaterial = prefab.fontSharedMaterial;
            }
            float scale = Mathf.Clamp(Screen.height / 1080f, 1f, 3f);
            _text.fontSize = 18f * scale;
            _text.color = new Color(0.9f, 0.925f, 0.97f, 1f);
            _text.text = message;
        }

        internal void Hide()
        {
            if (_canvas != null) _canvas.SetActive(false);
        }

        internal void Destroy()
        {
            if (_canvas != null) Object.Destroy(_canvas);
            _canvas = null;
            _text = null;
            _clip = null;
        }
    }
}
