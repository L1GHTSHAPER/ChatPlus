using System;

namespace ChatPlus
{
    /// <summary>Coalesces GUI requests; the caller drains them outside Unity's render loop.</summary>
    internal sealed class PreviewRenderQueue<T>
    {
        T _latest;
        bool _pending;
        internal bool Rendering { get; private set; }

        internal void Request(T request)
        {
            if (Rendering) return;
            _latest = request;
            _pending = true;
        }

        internal void Clear()
        {
            _latest = default;
            _pending = false;
        }

        internal void RenderLatest(Action<T> render)
        {
            if (Rendering || !_pending) return;
            T request = _latest;
            Clear();
            Rendering = true;
            try { render(request); }
            finally { Rendering = false; }
        }
    }
}
