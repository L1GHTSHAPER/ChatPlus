using System.Text;

namespace ChatPlus
{
    internal static class NicknameFormat
    {
        internal static string Compose(string name, OutgoingStyle style) =>
            OutgoingFormat.TryComposeNickname(name, style, out string styled) ? styled : name ?? string.Empty;

        // Never mutate the saved nickname or message payload. Failure preserves the original name bytes.
        internal static byte[] Apply(byte[] nameBytes, OutgoingStyle style)
        {
            if (nameBytes == null || style == null || !style.Enabled) return nameBytes;
            string name = Encoding.Unicode.GetString(nameBytes);
            if (!OutgoingFormat.TryComposeNickname(name, style, out string styled) || styled == name) return nameBytes;
            return Encoding.Unicode.GetBytes(styled);
        }
    }
}
