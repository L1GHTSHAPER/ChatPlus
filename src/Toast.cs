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
        UiSkin _skin;

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
            if (UiEnvironment.PreviewRendering) return;
            float left = _until - Time.unscaledTime;
            if (left <= 0f || string.IsNullOrEmpty(_text))
            {
                enabled = false;
                return;
            }
            if (_style == null)
            {
                _skin = new UiSkin();
                _style = new GUIStyle(_skin.Panel)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(10, 10, 5, 5)
                };
                _style.normal.textColor = UiSkin.TextColor;
            }

            // Same scaling as the settings window: laid out for 1080p.
            float scale = UiEnvironment.Scale;
            Matrix4x4 previous = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(left / FadeTime));
            var content = new GUIContent(_text);
            Vector2 size = _style.CalcSize(content);
            var position = new Vector2(_mouse.x / scale + 14f, (Screen.height - _mouse.y) / scale - size.y - 6f);
            position.x = Mathf.Clamp(position.x, 8f, Mathf.Max(8f,Screen.width/scale-size.x-8f));
            position.y = Mathf.Clamp(position.y, 8f, Mathf.Max(8f,Screen.height/scale-size.y-8f));
            GUI.Label(new Rect(position, size), content, _style);
            GUI.color = previousColor;
            GUI.matrix = previous;
        }

        void OnDestroy()
        {
            _skin?.Destroy();
            if (_instance == this)
                _instance = null;
        }
    }
}
