using System;
using System.Text;
using UnityEngine;

namespace ChatPlus
{
    internal static class NicknameSync
    {
        static readonly NicknameSyncState State = new NicknameSyncState();
        static PlayerCustomizationController _owner;
        static bool _styled;
        static float _nextCheck;

        internal static void Clear()
        {
            _owner = null;
            _styled = false;
            _nextCheck = 0f;
            State.Reset();
        }

        internal static void FormatOutgoing(PlayerCustomizationController owner, ref PlayerIDInfo info)
        {
            Plugin plugin = Plugin.Instance;
            if (plugin == null || owner == null || !owner.isOwner ||
                !plugin.NicknameEnabled.Value || !plugin.NicknameWorld.Value) return;
            info.Name = NicknameFormat.Apply(info.Name, plugin.BuildNicknameStyle());
        }

        internal static void Tick()
        {
            float now = Time.unscaledTime;
            if (now < _nextCheck) return;
            _nextCheck = now + .2f;
            Plugin plugin = Plugin.Instance;
            TextChannelManager chat = GameAccess.Chat;
            PlayerCustomizationController owner = chat != null ? chat.MainCustomizationController : null;
            if (plugin == null || owner == null || !owner.isOwner)
            {
                _owner = null;
                _styled = false;
                State.Reset();
                return;
            }
            if (_owner != owner)
            {
                _owner = owner;
                _styled = false;
                State.Reset();
            }
            bool active = plugin.NicknameEnabled.Value && plugin.NicknameWorld.Value;
            if (active) _styled = true;
            else if (!_styled) return;

            if (chat.MainPlayerController == null) return;
            var panel = NetworkSingleton<PlayerPanelController>.I;
            if (panel == null || panel.PlayerControllers == null || panel.IDInfos == null) return;
            int index = panel.PlayerControllers.IndexOf(chat.MainPlayerController);
            if (index < 0 || index >= panel.IDInfos.Count) return; // Wait until the native join RPC registers us.
            string raw = GameAccess.LocalPlayerName;
            if (string.IsNullOrEmpty(raw)) return;
            string desired = active ? NicknameFormat.Compose(raw, plugin.BuildNicknameStyle()) : raw;
            PlayerIDInfo info = panel.IDInfos[index];
            string current = info.Name != null ? Encoding.Unicode.GetString(info.Name) : string.Empty;
            if (!State.ShouldSend(desired, current, now))
            {
                if (!active && desired == current) _styled = false;
                return;
            }
            // Preserve every other field in the already shared profile. Saved PlayerData.Name stays untouched.
            info.Name = Encoding.Unicode.GetBytes(desired);
            owner.UpdatePlayerInfo(info, default);
        }

        internal static void RefreshList()
        {
            try { NetworkSingleton<PlayerPanelController>.I?.UpdateServerPanelOnChange(); }
            catch (Exception error) { Plugin.LogError("Could not refresh nickname in player list: ", error); }
        }
    }
}
