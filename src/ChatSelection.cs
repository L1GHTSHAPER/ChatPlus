using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChatPlus
{
    internal static class ChatSelection
    {
        static TMP_Text _anchor;
        static TMP_Text _caret;
        static int _anchorIndex;
        static int _caretIndex;
        static Transform _content;
        static Camera _camera;
        static UIManager _heldUi;
        static ScrollRect _scroll;
        static bool _dragging;
        static bool _isLocal;
        static int _startedFrame;
        static readonly Dictionary<TMP_Text, SelectionHighlight> Highlights = new Dictionary<TMP_Text, SelectionHighlight>();

        internal static bool Enabled => Plugin.Instance != null && Plugin.Instance.SelectText.Value;
        internal static bool HasSelection => _anchor != null && _caret != null && (_anchor != _caret || _anchorIndex != _caretIndex);

        internal static void Begin(TMP_Text text, PointerEventData pointer)
        {
            if (!Enabled || text == null || pointer.button != PointerEventData.InputButton.Left) return;
            Clear();
            _anchor = _caret = text;
            _startedFrame = Time.frameCount;
            _content = text.transform.parent;
            _isLocal = _content == GameAccess.Content(GameAccess.UI, true);
            _camera = pointer.pressEventCamera;
            _anchorIndex = _caretIndex = Boundary(text, pointer.position);
            _scroll = text.GetComponentInParent<ScrollRect>();
        }

        internal static void BeginDrag(TMP_Text text, PointerEventData pointer)
        {
            if (!Enabled || text != _anchor || pointer.button != PointerEventData.InputButton.Left) return;
            _dragging = true;
            pointer.eligibleForClick = false;
            if (_scroll != null) _scroll.StopMovement();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            _heldUi = GameAccess.UI;
            GameAccess.SetPanelHeld(_heldUi, true);
            Drag(pointer);
        }

        internal static void Drag(PointerEventData pointer)
        {
            if (_dragging) MoveCaret(pointer.position);
        }

        internal static void EndDrag(PointerEventData pointer)
        {
            if (!_dragging) return;
            MoveCaret(pointer.position);
            ReleaseDrag();
        }

        static void ReleaseDrag()
        {
            _dragging = false;
            GameAccess.SetPanelHeld(_heldUi, false);
            _heldUi = null;
        }

        // Called before the game's event system: outside clicks clear old selection; a new line pointer-down
        // later in the frame starts a new one. Copy is left to text fields whenever one has focus.
        internal static void Tick(bool settingsOpen)
        {
            if (_anchor == null)
            {
                if (_content != null || Highlights.Count > 0) Clear();
                return;
            }
            UIManager ui = GameAccess.UI;
            if (!Enabled || settingsOpen || ui == null || ui.IsMessagePanelHidden ||
                !_anchor.gameObject.activeInHierarchy || _content != GameAccess.Content(ui, _isLocal) ||
                GameAccess.Chat == null || GameAccess.Chat.Islocal != _isLocal || _caret == null || GameAccess.IsAnyTextFieldFocused() ||
                Input.GetKeyDown(KeyCode.Escape) || (Input.GetMouseButtonDown(0) && Time.frameCount != _startedFrame))
            {
                Clear();
                return;
            }
            if (HasSelection && Input.GetKeyDown(KeyCode.C) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
                Copy();
        }

        internal static void LateTick()
        {
            if (_anchor == null || _caret == null) return;
            if (_dragging)
            {
                if (!Input.GetMouseButton(0)) ReleaseDrag();
                else
                {
                    AutoScroll(Input.mousePosition);
                    MoveCaret(Input.mousePosition);
                }
            }
            RefreshHighlights();
        }

        static void AutoScroll(Vector2 point)
        {
            if (_scroll == null || _scroll.viewport == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_scroll.viewport, point, _camera, out Vector2 local)) return;
            Rect rect = _scroll.viewport.rect;
            float direction = local.y > rect.yMax ? 1f : local.y < rect.yMin ? -1f : 0f;
            float travel = _scroll.content != null ? _scroll.content.rect.height - rect.height : 0f;
            if (direction != 0f && travel > 0f)
                _scroll.verticalNormalizedPosition = Mathf.Clamp01(_scroll.verticalNormalizedPosition + direction * Time.unscaledDeltaTime * rect.height / travel);
        }

        static void MoveCaret(Vector2 point)
        {
            if (_content == null) return;
            TMP_Text nearest = null;
            float distance = float.MaxValue;
            for (int i = 0; i < _content.childCount; i++)
            {
                TMP_Text text = _content.GetChild(i).GetComponent<TMP_Text>();
                if (text == null || !text.gameObject.activeInHierarchy) continue;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(text.rectTransform, point, _camera, out Vector2 local)) continue;
                Rect rect = text.rectTransform.rect;
                float dy = Mathf.Max(rect.yMin - local.y, local.y - rect.yMax, 0f);
                if (dy < distance)
                {
                    distance = dy;
                    nearest = text;
                }
            }
            if (nearest == null) return;
            _caret = nearest;
            _caretIndex = Boundary(nearest, point);
        }

        static void PrepareText(TMP_Text text)
        {
            if (text.havePropertiesChanged || text.textInfo.characterCount == 0) text.ForceMeshUpdate();
        }

        static int Boundary(TMP_Text text, Vector2 point)
        {
            PrepareText(text);
            int count = text.textInfo.characterCount;
            if (count == 0) return 0;
            int index = TMP_TextUtilities.FindNearestCharacter(text, point, _camera, false);
            index = Mathf.Clamp(index, 0, count - 1);
            TMP_CharacterInfo character = text.textInfo.characterInfo[index];
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(text.rectTransform, point, _camera, out Vector2 local)) return index;
            // Above / below the whole message selects to its boundary, including wrapped lines.
            TMP_LineInfo first = text.textInfo.lineInfo[0];
            TMP_LineInfo last = text.textInfo.lineInfo[text.textInfo.lineCount - 1];
            if (local.y > first.ascender) return 0;
            if (local.y < last.descender) return count;
            return local.x < (character.origin + character.xAdvance) * 0.5f ? index : index + 1;
        }

        static void Ordered(out TMP_Text first, out int start, out TMP_Text last, out int end)
        {
            bool forward = _anchor.transform.GetSiblingIndex() < _caret.transform.GetSiblingIndex() ||
                (_anchor == _caret && _anchorIndex <= _caretIndex);
            first = forward ? _anchor : _caret;
            start = forward ? _anchorIndex : _caretIndex;
            last = forward ? _caret : _anchor;
            end = forward ? _caretIndex : _anchorIndex;
        }

        static void RefreshHighlights()
        {
            if (!HasSelection || _content == null)
            {
                foreach (SelectionHighlight highlight in Highlights.Values)
                    if (highlight != null) highlight.Show(0, 0);
                return;
            }
            Ordered(out TMP_Text first, out int start, out TMP_Text last, out int end);
            foreach (var pair in Highlights)
                if (pair.Value != null && (pair.Key == null || pair.Key.transform.GetSiblingIndex() < first.transform.GetSiblingIndex() ||
                    pair.Key.transform.GetSiblingIndex() > last.transform.GetSiblingIndex())) pair.Value.Show(0, 0);
            for (int i = first.transform.GetSiblingIndex(); i <= last.transform.GetSiblingIndex(); i++)
            {
                TMP_Text text = _content.GetChild(i).GetComponent<TMP_Text>();
                if (text == null) continue;
                PrepareText(text);
                int from = text == first ? start : 0;
                int to = text == last ? end : text.textInfo.characterCount;
                if (!Highlights.TryGetValue(text, out SelectionHighlight highlight) || highlight == null)
                {
                    highlight = SelectionHighlight.Create(text);
                    Highlights[text] = highlight;
                }
                highlight.Show(from, to);
            }
        }

        internal static bool Contains(TMP_Text text)
        {
            if (!HasSelection || text == null || text.transform.parent != _content) return false;
            Ordered(out TMP_Text first, out _, out TMP_Text last, out _);
            int index = text.transform.GetSiblingIndex();
            return index >= first.transform.GetSiblingIndex() && index <= last.transform.GetSiblingIndex();
        }

        internal static bool Copy()
        {
            if (!HasSelection || _content == null) return false;
            Ordered(out TMP_Text first, out int start, out TMP_Text last, out int end);
            var result = new StringBuilder();
            bool previous = false;
            for (int i = first.transform.GetSiblingIndex(); i <= last.transform.GetSiblingIndex(); i++)
            {
                TMP_Text text = _content.GetChild(i).GetComponent<TMP_Text>();
                if (text == null) continue;
                PrepareText(text);
                var glyphs = new SelectionGlyph[text.textInfo.characterCount];
                for (int g = 0; g < glyphs.Length; g++)
                {
                    TMP_CharacterInfo character = text.textInfo.characterInfo[g];
                    string replacement = character.elementType == TMP_TextElementType.Sprite ? "\uFFFC" :
                        character.character == '\n' ? "\n" : character.character == '\r' ? "\r" :
                        character.stringLength > 1 && character.index >= 0 && character.index < text.text.Length &&
                        text.text[character.index] == '<' ? character.character.ToString() : null;
                    glyphs[g] = new SelectionGlyph(character.index, character.stringLength, replacement);
                }
                if (previous) result.AppendLine();
                result.Append(SelectionText.Slice(text.text, glyphs, text == first ? start : 0, text == last ? end : glyphs.Length));
                previous = true;
            }
            if (result.Length == 0) return false;
            GUIUtility.systemCopyBuffer = result.ToString();
            Toast.Show(Lang.Current.Copied);
            return true;
        }

        internal static void Clear()
        {
            ReleaseDrag();
            foreach (SelectionHighlight highlight in Highlights.Values)
                if (highlight != null) Object.Destroy(highlight.gameObject);
            Highlights.Clear();
            _anchor = _caret = null;
            _content = null;
            _scroll = null;
            _camera = null;
        }
    }
}
