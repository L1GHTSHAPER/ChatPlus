using System;

namespace ChatPlus.Tests
{
    internal static class PreviewRenderQueueTests
    {
        internal static void Run(Action<bool, string> check)
        {
            var queue = new PreviewRenderQueue<string>();
            int renders = 0;
            string rendered = null;
            Action<string> render = value => { renders++; rendered = value; };
            queue.RenderLatest(render);
            check(renders == 0, "preview: no render without a GUI request");
            queue.Request("old");
            queue.Request("latest");
            check(renders == 0, "preview: GUI requests never render synchronously");
            queue.RenderLatest(render);
            check(renders == 1 && rendered == "latest", "preview: multiple GUI passes render only latest request");
            queue.RenderLatest(render);
            check(renders == 1, "preview: consumed request is not rendered twice");
            queue.Request("hidden");
            queue.Clear();
            queue.RenderLatest(render);
            check(renders == 1, "preview: closing or changing tabs cancels pending render");
            queue.Request("visible");
            queue.RenderLatest(value =>
            {
                check(queue.Rendering, "preview: rendering guard spans camera callback");
                queue.Request("recursive GUI");
                queue.RenderLatest(render);
                render(value);
            });
            queue.RenderLatest(render);
            check(renders == 2 && rendered == "visible" && !queue.Rendering,
                "preview: nested GUI and camera callbacks cannot reenter or queue another render");
            queue.Request("throw");
            bool threw = false;
            try { queue.RenderLatest(value => throw new InvalidOperationException()); }
            catch (InvalidOperationException) { threw = true; }
            queue.RenderLatest(render);
            check(threw && !queue.Rendering && renders == 2, "preview: exception releases guard and consumes failed request");
            queue.Request("reopened");
            queue.RenderLatest(render);
            check(renders == 3 && rendered == "reopened", "preview: request works again after close or callback exception");
        }
    }
}
