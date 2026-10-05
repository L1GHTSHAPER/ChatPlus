namespace ChatPlus
{
    /// <summary>
    /// Decides whether the game's TextChannelManager.ResetTextMaterials may run. The game calls it twice (this frame
    /// and the next) whenever the chat panel fades in or out, or the window mode changes, after switching the font
    /// outline (UNDERLAY_ON) of the chat's shared font material. Switching both chat tabs off and on is only needed
    /// when that keyword really changed; otherwise it just rebuilds every line.
    /// </summary>
    internal static class OutlineResets
    {
        static TextChannelManager _chat;
        static bool _outline;
        // Calls still allowed after a change: the game's second call on the next frame.
        static int _extra;

        internal static bool ShouldRun(TextChannelManager chat)
        {
            if (!GameAccess.TryGetFontOutline(out bool outline))
                return true;
            if (!ReferenceEquals(chat, _chat))
            {
                // First reset in this lobby: let it (and its follow-up) run.
                _chat = chat;
                _outline = outline;
                _extra = 1;
                return true;
            }
            if (outline != _outline)
            {
                _outline = outline;
                _extra = 1;
                return true;
            }
            if (_extra > 0)
            {
                _extra--;
                return true;
            }
            return false;
        }
    }
}
