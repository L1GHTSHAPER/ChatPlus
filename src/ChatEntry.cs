using System;

namespace ChatPlus
{
    internal enum ChatKind
    {
        /// <summary>A message from another player.</summary>
        Player,
        /// <summary>A message you sent.</summary>
        Own,
        /// <summary>A line from the game or a mod: someone joined, XP earned, command output...</summary>
        Notification
    }

    /// <summary>One chat line as it is kept in the session history. Plain .NET, no Unity types.</summary>
    internal sealed class ChatEntry
    {
        /// <summary>Local time the line appeared.</summary>
        public DateTime Time;
        public bool IsLocal;
        public ChatKind Kind;
        /// <summary>Steam ID of the sender; empty for notifications.</summary>
        public string SteamId = string.Empty;
        /// <summary>Sender name as the game shows it (may contain rich text tags); empty for notifications.</summary>
        public string Name = string.Empty;
        /// <summary>Message text (may contain rich text tags).</summary>
        public string Message = string.Empty;
        /// <summary>The whole line exactly as the game rendered it, before the mod's additions.</summary>
        public string Line = string.Empty;
    }
}
