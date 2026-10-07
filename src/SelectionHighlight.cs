using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChatPlus
{
    // Clipped by the same viewport as the text. Character positions come from TMP after wrapping and sizing.
    internal sealed class SelectionHighlight : MaskableGraphic
    {
        TMP_Text _text;
        int _first;
        int _last;

        internal static SelectionHighlight Create(TMP_Text text)
        {
            var obj = new GameObject("ChatPlus.Selection", typeof(RectTransform));
            var rect = (RectTransform)obj.transform;
            rect.SetParent(text.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = text.rectTransform.pivot;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var graphic = obj.AddComponent<SelectionHighlight>();
            graphic._text = text;
            graphic.raycastTarget = false;
            graphic.color = new Color(0.35f, 0.6f, 1f, 0.32f);
            var layout = obj.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            return graphic;
        }

        internal void Show(int first, int last)
        {
            first = Mathf.Max(0, first);
            last = Mathf.Min(_text != null ? _text.textInfo.characterCount : 0, last);
            _first = first;
            _last = last;
            enabled = last > first;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (_text == null) return;
            TMP_TextInfo info = _text.textInfo;
            for (int i = _first; i < _last && i < info.characterCount;)
            {
                TMP_CharacterInfo start = info.characterInfo[i];
                int end = i;
                while (end + 1 < _last && end + 1 < info.characterCount && info.characterInfo[end + 1].lineNumber == start.lineNumber)
                    end++;
                TMP_CharacterInfo finish = info.characterInfo[end];
                TMP_LineInfo line = info.lineInfo[start.lineNumber];
                float left = start.isVisible ? Mathf.Min(start.origin, start.bottomLeft.x) : start.origin;
                float right = finish.isVisible ? Mathf.Max(finish.xAdvance, finish.topRight.x) : finish.xAdvance;
                float alpha = Mathf.Clamp01(_text.color.a);
                Color tint = color;
                tint.a *= alpha;
                int index = mesh.currentVertCount;
                mesh.AddVert(new Vector3(left, line.descender), tint, Vector2.zero);
                mesh.AddVert(new Vector3(left, line.ascender), tint, Vector2.zero);
                mesh.AddVert(new Vector3(right, line.ascender), tint, Vector2.zero);
                mesh.AddVert(new Vector3(right, line.descender), tint, Vector2.zero);
                mesh.AddTriangle(index, index + 1, index + 2);
                mesh.AddTriangle(index + 2, index + 3, index);
                i = end + 1;
            }
        }
    }
}
