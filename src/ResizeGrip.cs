using UnityEngine;
using UnityEngine.EventSystems;

namespace ChatPlus
{
    /// <summary>
    /// The handle at the top-right corner of the chat panel: drag it to resize the panel (its bottom-left corner
    /// stays in place), double-click it for the game's size.
    /// </summary>
    internal sealed class ResizeGrip : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        const float HoverScale = 1.15f;
        const float DoubleClickTime = 0.35f;
        // A press this far (screen pixels) from the previous one is not part of a double-click.
        const float DoubleClickDistance = 12f;

        Vector2 _pressPointer;
        Vector2 _pressScreen;
        Vector2 _pressSize;
        bool _pressValid;
        float _lastPress = -10f;
        bool _dragging;
        bool _hovered;

        // Handling the press here keeps it from going to the panel's background button, which would otherwise
        // receive the click and cancel it as soon as the drag starts.
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            float now = Time.unscaledTime;
            bool doubleClick = now - _lastPress < DoubleClickTime &&
                               (eventData.position - _pressScreen).magnitude < DoubleClickDistance;
            _lastPress = doubleClick ? -10f : now;
            _pressScreen = eventData.position;
            // The resize follows the pointer from where it was pressed, so the drag threshold does not offset it.
            _pressValid = ChatWindow.TryPointer(eventData.position, eventData.pressEventCamera, out _pressPointer);
            if (doubleClick)
                ChatWindow.ResetSize();
            _pressSize = ChatWindow.Size;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !_pressValid)
                return;
            _dragging = true;
            // A drag in between makes the next press a new first click.
            _lastPress = -10f;
            ChatWindow.BeginResize();
            UpdateScale();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || !ChatWindow.TryPointer(eventData.position, eventData.pressEventCamera, out Vector2 pointer))
                return;
            ChatWindow.ResizeTo(_pressSize + (pointer - _pressPointer));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            StopDragging();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            UpdateScale();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            UpdateScale();
        }

        void OnDisable()
        {
            StopDragging();
            _hovered = false;
            UpdateScale();
        }

        void StopDragging()
        {
            if (!_dragging)
                return;
            _dragging = false;
            ChatWindow.EndResize();
            UpdateScale();
        }

        void UpdateScale()
        {
            transform.localScale = Vector3.one * (_hovered || _dragging ? HoverScale : 1f);
        }
    }

    /// <summary>The handle's picture, drawn in code in the colours of the game's chat frame.</summary>
    internal static class GripArt
    {
        const int Size = 64;
        static readonly Color Fill = new Color(0.957f, 0.929f, 0.882f, 1f);
        static readonly Color Ink = new Color(0.416f, 0.302f, 0.322f, 1f);

        public static Texture2D Draw()
        {
            var pixels = new Color[Size * Size];
            // A diagonal double arrow from the bottom-left to the top-right corner (texture rows go bottom-up).
            var start = new Vector2(20f, 20f);
            var end = new Vector2(44f, 44f);
            const float head = 11f;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float box = RoundedRectDistance(p.x - 3f, p.y - 3f, Size - 6f, Size - 6f, 15f);
                    Color color = Color.Lerp(Ink, Fill, Mathf.Clamp01(-box - 3.5f));
                    color.a = Mathf.Clamp01(0.5f - box);

                    float arrow = Mathf.Min(
                        SegmentDistance(p, start, end),
                        Mathf.Min(
                            Mathf.Min(SegmentDistance(p, end, end - new Vector2(head, 0f)), SegmentDistance(p, end, end - new Vector2(0f, head))),
                            Mathf.Min(SegmentDistance(p, start, start + new Vector2(head, 0f)), SegmentDistance(p, start, start + new Vector2(0f, head)))));
                    float ink = Mathf.Clamp01(3.2f - arrow);
                    color = Color.Lerp(color, new Color(Ink.r, Ink.g, Ink.b, color.a), ink);
                    pixels[y * Size + x] = color;
                }
            }
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "ChatPlus.ResizeHandle",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

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
    }
}
