using System;

namespace ChatPlus
{
    /// <summary>Messages received during the current period with the chat collapsed.</summary>
    internal sealed class HiddenMessageCounts
    {
        internal int Global { get; private set; }
        internal int Local { get; private set; }

        internal void ObserveVisibility(bool hidden)
        {
            if (!hidden)
            {
                Global = 0;
                Local = 0;
            }
        }

        internal void Receive(bool isLocal, bool own, bool hidden)
        {
            ObserveVisibility(hidden);
            if (!hidden || own)
                return;
            if (isLocal)
                Local = Math.Min(Local + 1, 99);
            else
                Global = Math.Min(Global + 1, 99);
        }
    }
}
