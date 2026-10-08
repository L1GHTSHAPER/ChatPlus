using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChatPlus.Tests
{
    // Exercise the production ChatLine event methods, not a duplicated gesture policy. The doubles below only
    // provide its Unity boundaries; these tests verify event routing/click eligibility, not rendering or physics.
    internal static class DragRoutingTests
    {
        static ChatLine Create(out ScrollRect scroll)
        {
            Input.Shift = Input.RightShift = false;
            ChatSelection.Enabled = true;
            ChatSelection.CanStart = true;
            ChatSelection.Calls.Clear();
            var parent = new GameObject();
            scroll = parent.AddComponent<ScrollRect>();
            scroll.Velocity = 10f;
            var message = new GameObject { Parent = parent };
            return ChatLine.MakeSelectable(message.AddComponent<TMP_Text>());
        }

        static PointerEventData Press(ChatLine line, int pointerId = -1)
        {
            var pointer = new PointerEventData { pointerId = pointerId, delta = new Vector2(0f, 18f) };
            line.OnPointerDown(pointer);
            line.OnInitializePotentialDrag(pointer);
            return pointer;
        }

        internal static void Run(Action<bool, string> check)
        {
            var line = Create(out var scroll);
            var pointer = Press(line);
            check(scroll.Velocity == 0f, "press stops native scroll inertia through initialization");
            line.OnBeginDrag(pointer);
            line.OnDrag(pointer);
            Input.Shift = true;
            line.OnDrag(pointer);
            line.OnEndDrag(pointer);
            check(scroll.Position == 36f, "ordinary LMB swipe moves chat even with selection enabled");
            check(scroll.Calls.SequenceEqual(new[] { "initialize", "begin", "drag", "drag", "end" }), "native scroll receives its complete drag lifecycle");
            check(!ChatSelection.Calls.Contains("begin") && !ChatSelection.Calls.Contains("drag"), "plain swipe never starts selection, even when Shift is pressed mid-swipe");
            check(!pointer.eligibleForClick, "scroll swipe cannot trigger the message Button on release");
            check(!scroll.Dragging, "native scroll is released at the end of a swipe");

            line = Create(out scroll);
            Input.Shift = true;
            pointer = Press(line);
            Input.Shift = false; // Releasing even before the drag threshold keeps the press's selected mode.
            line.OnBeginDrag(pointer);
            line.OnDrag(pointer);
            line.OnEndDrag(pointer);
            check(ChatSelection.Calls.SequenceEqual(new[] { "begin", "begin-drag", "drag", "end" }), "Shift at press selects text through the whole gesture after Shift is released");
            check(scroll.Calls.SequenceEqual(new[] { "initialize" }) && scroll.Position == 0f, "selection never forwards drag movement to native scrolling");
            check(!pointer.eligibleForClick, "selection drag cannot open a player card");

            line = Create(out scroll);
            Input.RightShift = true;
            pointer = Press(line);
            line.OnBeginDrag(pointer);
            line.OnEndDrag(pointer);
            check(ChatSelection.Calls.Contains("begin-drag"), "right Shift also selects");

            line = Create(out scroll);
            Input.Shift = true;
            pointer = Press(line, pointerId: 0);
            line.OnBeginDrag(pointer);
            line.OnDrag(pointer);
            line.OnEndDrag(pointer);
            check(scroll.Position == 18f && !ChatSelection.Calls.Contains("begin"), "touch swipes scroll, including when a keyboard Shift key is held");

            line = Create(out scroll);
            ChatSelection.Enabled = false;
            Input.Shift = true;
            pointer = Press(line);
            ChatSelection.Enabled = true;
            line.OnBeginDrag(pointer);
            line.OnDrag(pointer);
            line.OnEndDrag(pointer);
            check(scroll.Position == 18f && !ChatSelection.Calls.Contains("begin"), "disabled selection preserves scrolling and does not switch mode if enabled mid-gesture");

            line = Create(out scroll);
            Input.Shift = true;
            pointer = Press(line);
            ChatSelection.CanStart = false; // Anchor cleared or settings disabled between press and drag.
            line.OnBeginDrag(pointer);
            line.OnDrag(pointer);
            line.OnEndDrag(pointer);
            check(scroll.Position == 18f && scroll.Calls.Last() == "end", "unavailable selection falls back to a complete scroll gesture");

            line = Create(out scroll);
            pointer = Press(line);
            check(pointer.eligibleForClick, "ordinary click keeps player-card eligibility");
            line.OnPointerClick(pointer);
            check(scroll.Calls.SequenceEqual(new[] { "initialize" }), "ordinary click does not begin scrolling");

            line = Create(out scroll);
            pointer = Press(line);
            line.OnBeginDrag(pointer);
            typeof(ChatLine).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(line, null);
            line.OnEndDrag(pointer);
            line.OnDrag(pointer);
            check(!scroll.Dragging && scroll.Calls.SequenceEqual(new[] { "initialize", "begin", "end" }), "disabling a line releases scrolling exactly once and ignores stale drag events");

            line = Create(out scroll);
            Input.Shift = true;
            pointer = Press(line);
            line.OnBeginDrag(pointer);
            typeof(ChatLine).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(line, null);
            check(ChatSelection.Calls.TakeLast(2).SequenceEqual(new[] { "end", "clear" }), "disabling a selecting line ends and clears selection");

            line = Create(out scroll);
            pointer = new PointerEventData { button = PointerEventData.InputButton.Right };
            line.OnPointerDown(pointer);
            line.OnBeginDrag(pointer);
            line.OnDrag(pointer);
            line.OnEndDrag(pointer);
            check(ChatSelection.Calls.Count == 0 && scroll.Calls.Count == 0, "right-button input does not clear selection or start a left-button gesture");
        }
    }
}

namespace UnityEngine
{
    public class GameObject
    {
        public GameObject Parent;
        readonly Dictionary<Type, MonoBehaviour> _components = new Dictionary<Type, MonoBehaviour>();
        public T AddComponent<T>() where T : MonoBehaviour
        {
            var component = (T)Activator.CreateInstance(typeof(T), nonPublic: true);
            component.gameObject = this;
            _components[typeof(T)] = component;
            return component;
        }
        public T GetComponent<T>() where T : class => _components.TryGetValue(typeof(T), out var component) ? component as T : null;
    }
    public class MonoBehaviour
    {
        public GameObject gameObject;
        public T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
        public T GetComponentInParent<T>() where T : class
        {
            for (var obj = gameObject; obj != null; obj = obj.Parent)
            {
                var found = obj.GetComponent<T>();
                if (found != null) return found;
            }
            return null;
        }
    }
    public readonly struct Vector2 { public readonly float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } }
    public enum KeyCode { LeftShift, RightShift }
    public static class Input { public static bool Shift, RightShift; public static bool GetKey(KeyCode key) => key == KeyCode.LeftShift ? Shift : RightShift; }
    public static class GUIUtility { public static string systemCopyBuffer; }
}

namespace UnityEngine.EventSystems
{
    public class PointerEventData
    {
        public enum InputButton { Left, Right, Middle }
        public InputButton button;
        public int pointerId = -1;
        public bool eligibleForClick = true;
        public Vector2 delta;
    }
    public interface IPointerDownHandler { void OnPointerDown(PointerEventData data); }
    public interface IPointerClickHandler { void OnPointerClick(PointerEventData data); }
    public interface IInitializePotentialDragHandler { void OnInitializePotentialDrag(PointerEventData data); }
    public interface IBeginDragHandler { void OnBeginDrag(PointerEventData data); }
    public interface IDragHandler { void OnDrag(PointerEventData data); }
    public interface IEndDragHandler { void OnEndDrag(PointerEventData data); }
}

namespace UnityEngine.UI
{
    public class ScrollRect : MonoBehaviour
    {
        public readonly List<string> Calls = new List<string>();
        public float Velocity, Position;
        public bool Dragging;
        public void OnInitializePotentialDrag(PointerEventData data) { Calls.Add("initialize"); Velocity = 0f; }
        public void OnBeginDrag(PointerEventData data) { Calls.Add("begin"); Dragging = true; }
        public void OnDrag(PointerEventData data) { Calls.Add("drag"); if (Dragging) Position += data.delta.y; }
        public void OnEndDrag(PointerEventData data) { Calls.Add("end"); Dragging = false; }
    }
}

namespace TMPro { public class TMP_Text : MonoBehaviour { public string text; public bool raycastTarget; } }

namespace ChatPlus
{
    internal static class ChatSelection
    {
        internal static bool Enabled = true, CanStart = true;
        internal static readonly List<string> Calls = new List<string>();
        static TMP_Text _anchor;
        internal static void Begin(TMP_Text text, PointerEventData data) { _anchor = text; Calls.Add("begin"); }
        internal static bool BeginDrag(TMP_Text text, PointerEventData data) { if (!Enabled || !CanStart || text != _anchor) return false; Calls.Add("begin-drag"); return true; }
        internal static void Drag(PointerEventData data) => Calls.Add("drag");
        internal static void EndDrag(PointerEventData data) => Calls.Add("end");
        internal static void Clear() { _anchor = null; Calls.Add("clear"); }
        internal static bool Contains(TMP_Text text) => false;
        internal static bool Copy() => false;
    }
    internal sealed class TestSetting { internal bool Value = true; }
    internal sealed class Plugin { internal static Plugin Instance = new Plugin(); internal TestSetting RightClickCopy = new TestSetting(); }
    internal static class ChatLines { internal static LineStyle Style = new LineStyle(); }
    internal static class LineSizer { internal static void Apply(TMP_Text text) { } }
    internal sealed class Lang { internal static Lang Current = new Lang(); internal string Copied = "Copied"; }
    internal static class Toast { internal static void Show(string text) { } }
}
