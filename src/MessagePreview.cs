using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace ChatPlus
{
    /// <summary>Rasterizes the actual TMP font and tags, then draws inside the IMGUI preview rectangle.</summary>
    internal sealed class MessagePreview
    {
        const int PreviewLayer = 31;
        GameObject _host;
        RectTransform _canvasRect;
        Camera _camera;
        TMP_Text _text;
        RenderTexture _target;
        Material _fontMaterial;
        TMP_FontAsset _font;
        Material _sourceMaterial;
        float _fontSize;
        Color _color;
        string _message;
        bool _failed;
        readonly PreviewRenderQueue<PreviewRequest> _requests = new PreviewRenderQueue<PreviewRequest>();
        internal bool Rendering => _requests.Rendering;

        struct PreviewRequest
        {
            internal TMP_Text Prefab;
            internal string Message;
            internal int Width, Height;
            internal float FontSize;
            internal Color Color;
        }

        internal void Draw(Rect bounds, string message, float fontSize, Color color)
        {
            if (Rendering || Event.current.type != EventType.Repaint) return;
            TMP_Text prefab = GameAccess.TextPrefab(GameAccess.Chat);
            if (!_failed && prefab != null && prefab.font != null)
            {
                float scale = Mathf.Abs(GUI.matrix.m00);
                _requests.Request(new PreviewRequest
                {
                    Prefab = prefab, Message = message, Color = color, FontSize = fontSize * scale,
                    Width = Mathf.Clamp(Mathf.CeilToInt(bounds.width * scale), 1, 4096),
                    Height = Mathf.Clamp(Mathf.CeilToInt(bounds.height * scale), 1, 4096)
                });
                if (_target != null && _target.IsCreated())
                {
                    // IMGUI owns position, scale, scrolling and window movement for both background and text.
                    GUI.DrawTexture(bounds, _target, ScaleMode.StretchToFill, true);
                    return;
                }
            }
            // Also usable before a lobby has supplied the game's TMP font.
            var fallback = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(fontSize), wordWrap = true };
            fallback.normal.textColor = color;
            GUI.Label(bounds, TextUtil.StripTags(message), fallback);
        }

        // Called only by SettingsWindow.LateUpdate, before Unity starts rendering cameras.
        internal void RenderPending()
        {
            if (_failed) return;
            object previous = AppDomain.CurrentDomain.GetData("LightShaper.PreviewRendering.v1");
            try
            {
                AppDomain.CurrentDomain.SetData("LightShaper.PreviewRendering.v1", true);
                _requests.RenderLatest(Rasterize);
            }
            catch (Exception error)
            {
                _failed = true;
                Hide();
                Plugin.LogError("Could not render message preview: ", error);
            }
            finally { AppDomain.CurrentDomain.SetData("LightShaper.PreviewRendering.v1", previous); }
        }

        void Rasterize(PreviewRequest request)
        {
            TMP_Text prefab = request.Prefab;
            if (prefab == null || prefab.font == null) { Hide(); return; }
            if (_host == null) Create();
            _host.SetActive(true);
            int width = request.Width, height = request.Height;
            float physicalFontSize = request.FontSize;
            string message = request.Message;
            Color color = request.Color;
            bool dirty = false;
            if (_target == null || !_target.IsCreated() || _target.width != width || _target.height != height)
            {
                ReleaseTarget();
                // URP Render Graph requires a depth buffer on camera output textures.
                _target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                { name = "ChatPlus.PreviewTexture", hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
                _target.Create();
                _camera.targetTexture = _target;
                _camera.orthographicSize = height * .5f;
                _camera.aspect = width / (float)height;
                _canvasRect.sizeDelta = new Vector2(width, height);
                dirty = true;
            }
            if (_font != prefab.font || _sourceMaterial != prefab.fontSharedMaterial)
            {
                _font = prefab.font;
                _sourceMaterial = prefab.fontSharedMaterial;
                _text.font = _font;
                if (_fontMaterial != null) UnityEngine.Object.Destroy(_fontMaterial);
                _fontMaterial = _sourceMaterial != null ? new Material(_sourceMaterial) : null;
                if (_fontMaterial != null)
                {
                    _fontMaterial.hideFlags = HideFlags.HideAndDontSave;
                    _fontMaterial.DisableKeyword("UNDERLAY_ON");
                    _text.fontSharedMaterial = _fontMaterial;
                }
                dirty = true;
            }
            if (_message != message || _fontSize != physicalFontSize || _color != color)
            {
                _text.text = _message = message;
                _text.fontSize = _fontSize = physicalFontSize;
                _text.color = _color = color;
                dirty = true;
            }
            if (!dirty) return;
            _text.ForceMeshUpdate(true);
            Canvas.ForceUpdateCanvases();
            if (GraphicsSettings.currentRenderPipeline == null)
                _camera.Render();
            else
            {
                var renderRequest = new RenderPipeline.StandardRequest { destination = _target };
                if (!RenderPipeline.SupportsRenderRequest(_camera, renderRequest))
                    throw new InvalidOperationException("The active pipeline does not support preview rendering.");
                RenderPipeline.SubmitRenderRequest(_camera, renderRequest);
            }
        }

        void Create()
        {
            _host = new GameObject("ChatPlus.MessagePreview") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(_host);
            // This isolated canvas is away from gameplay and only its dedicated camera sees it.
            _host.transform.position = new Vector3(0f, -10000f, 0f);
            var cameraObject = new GameObject("Camera", typeof(Camera)) { layer = PreviewLayer };
            cameraObject.transform.SetParent(_host.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
            _camera = cameraObject.GetComponent<Camera>();
            _camera.enabled = false;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _camera.useOcclusionCulling = false;
            _camera.orthographic = true;
            _camera.nearClipPlane = .1f;
            _camera.farClipPlane = 20f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;
            _camera.cullingMask = 1 << PreviewLayer;
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)) { layer = PreviewLayer };
            _canvasRect = canvasObject.GetComponent<RectTransform>();
            _canvasRect.SetParent(_host.transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = _camera;
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)) { layer = PreviewLayer };
            _text = textObject.GetComponent<TextMeshProUGUI>();
            _text.rectTransform.SetParent(_canvasRect, false);
            _text.rectTransform.anchorMin = Vector2.zero;
            _text.rectTransform.anchorMax = Vector2.one;
            _text.rectTransform.offsetMin = _text.rectTransform.offsetMax = Vector2.zero;
            _text.raycastTarget = false;
            _text.richText = true;
            _text.enableAutoSizing = false;
            _text.alignment = TextAlignmentOptions.TopLeft;
            _text.overflowMode = TextOverflowModes.Truncate;
        }

        void ReleaseTarget()
        {
            if (_camera != null) _camera.targetTexture = null;
            if (_target == null) return;
            _target.Release();
            UnityEngine.Object.Destroy(_target);
            _target = null;
        }

        internal void Hide()
        {
            _requests.Clear();
            if (_host != null) _host.SetActive(false);
        }

        internal void Destroy()
        {
            Hide();
            ReleaseTarget();
            if (_fontMaterial != null) UnityEngine.Object.Destroy(_fontMaterial);
            if (_host != null) UnityEngine.Object.Destroy(_host);
            _host = null;
            _text = null;
            _camera = null;
        }
    }
}
