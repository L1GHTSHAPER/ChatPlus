using UnityEngine;

namespace ChatPlus
{
    /// <summary>A short note next to the mouse pointer ("Copied"). Enabled only while it is shown.</summary>
    internal sealed class Toast : MonoBehaviour
    {
        const float Duration = 1.2f;
        const float FadeTime = 0.3f;

        static Toast _instance;

        string _text;
        float _until;
        Vector2 _mouse;
        GUIStyle _style;
        Texture2D _background;

        internal static void Show(string text)
        {
            if (_instance == null)
                return;
            _instance._text = text;
            _instance._until = Time.unscaledTime + Duration;
            _instance._mouse = Input.mousePosition;
            _instance.enabled = true;
        }

        void Awake()
        {
            _instance = this;
            enabled = false;
        }

        void OnGUI()
        {
            float left = _until - Time.unscaledTime;
            if (left <= 0f || string.IsNullOrEmpty(_text))
            {
                enabled = false;
                return;
            }
            if (_style == null)
            {
                _background = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                _background.SetPixel(0, 0, new Color(0.09f, 0.106f, 0.149f, 0.94f));
                _background.Apply();
                _style = new GUIStyle
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(10, 10, 5, 5)
                };
                _style.normal.textColor = new Color(0.9f, 0.925f, 0.97f);
                _style.normal.background = _background;
            }

            // Same scaling as the settings window: laid out for 1080p.
            float scale = Mathf.Clamp(Screen.height / 1080f, 1f, 3f);
            Matrix4x4 previous = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(left / FadeTime));
            var content = new GUIContent(_text);
            Vector2 size = _style.CalcSize(content);
            var position = new Vector2(_mouse.x / scale + 14f, (Screen.height - _mouse.y) / scale - size.y - 6f);
            GUI.Label(new Rect(position, size), content, _style);
            GUI.color = previousColor;
            GUI.matrix = previous;
        }

        void OnDestroy()
        {
            if (_background != null)
                Destroy(_background);
            if (_instance == this)
                _instance = null;
        }
    }
}
