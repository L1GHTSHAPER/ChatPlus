using System;
using System.Collections.Generic;
using System.Text;

namespace ChatPlus.Tests
{
    internal static class NicknameSyncTests
    {
        internal static void Run(Action<bool, string> check)
        {
            var owner = new PlayerCustomizationController();
            var player = new PlayerController();
            var panel = new PlayerPanelController();
            NetworkSingleton<PlayerPanelController>.I = panel;
            GameAccess.Chat = new TextChannelManager { MainCustomizationController = owner, MainPlayerController = player };
            GameAccess.LocalPlayerName = "Марк";
            Plugin.Instance.NicknameEnabled.Value = Plugin.Instance.NicknameWorld.Value = true;
            Plugin.Instance.NickStyle = new OutgoingStyle { Enabled = true, ColorMode = MessageColorMode.Solid, Color = "#123456" };
            var profile = new PlayerIDInfo { Name = Encoding.Unicode.GetBytes("Марк"), Location = new byte[] { 3, 4 }, DateOfBirth = new byte[] { 5, 6 }, IsGold = true };
            var nameBefore = profile.Name;
            owner.isOwner = false;
            NicknameSync.FormatOutgoing(owner, ref profile);
            check(ReferenceEquals(profile.Name, nameBefore), "world nickname never changes another player's outgoing profile");
            owner.isOwner = true;
            NicknameSync.FormatOutgoing(owner, ref profile);
            check(Encoding.Unicode.GetString(profile.Name) == "<#123456>Марк</color>", "native join/profile payload carries own styled nickname");
            check(profile.IsGold && profile.Location[0] == 3 && profile.DateOfBirth[0] == 5, "profile formatting preserves other shared fields");
            Plugin.Instance.NicknameWorld.Value = false;
            profile.Name = nameBefore;
            NicknameSync.FormatOutgoing(owner, ref profile);
            check(ReferenceEquals(profile.Name, nameBefore), "chat-only setting leaves native world profile untouched");
            Plugin.Instance.NicknameWorld.Value = true;
            GameAccess.Chat.MainPlayerController = null;
            panel.PlayerControllers.Add(null); panel.IDInfos.Add(profile);
            NicknameSync.Clear();
            UnityEngine.Time.unscaledTime = 0; NicknameSync.Tick();
            check(owner.Sent.Count == 0, "missing local controller cannot select another null player row for synchronization");
            panel.PlayerControllers.Clear(); panel.IDInfos.Clear();
            GameAccess.Chat.MainPlayerController = player;
            NicknameSync.Clear();
            UnityEngine.Time.unscaledTime = 0;
            NicknameSync.Tick();
            check(owner.Sent.Count == 0, "world nickname waits for native lobby registration before sending");
            panel.PlayerControllers.Add(player); panel.IDInfos.Add(profile);
            UnityEngine.Time.unscaledTime = 1; NicknameSync.Tick();
            Plugin.Instance.NickStyle.Color = "#654321";
            UnityEngine.Time.unscaledTime = 1.2f; NicknameSync.Tick();
            check(owner.Sent.Count == 0, "changing nickname color waits for the latest slider value to settle");
            UnityEngine.Time.unscaledTime = 1.6f; NicknameSync.Tick();
            check(owner.Sent.Count == 1 && Encoding.Unicode.GetString(owner.Sent[0].Name) == "<#654321>Марк</color>", "settled nickname sends latest style through the native update RPC");
            check(ReferenceEquals(owner.Sent[0].Location, profile.Location) && ReferenceEquals(owner.Sent[0].DateOfBirth, profile.DateOfBirth),
                "live nickname update copies other already shared profile fields without changing them");
            check(ReferenceEquals(panel.IDInfos[0].Name, nameBefore) && GameAccess.LocalPlayerName == "Марк", "sending does not overwrite source name or local profile before native acknowledgement");
            UnityEngine.Time.unscaledTime = 1.9f; NicknameSync.Tick();
            check(owner.Sent.Count == 1, "nickname update is not spammed while awaiting the server");
            UnityEngine.Time.unscaledTime = 3.7f; NicknameSync.Tick();
            check(owner.Sent.Count == 2, "unacknowledged nickname update retries with a bounded rate");
            panel.IDInfos[0] = owner.Sent[1];
            UnityEngine.Time.unscaledTime = 4; NicknameSync.Tick();
            check(owner.Sent.Count == 2, "native acknowledgement stops nickname retransmission");
            Plugin.Instance.NicknameWorld.Value = false;
            UnityEngine.Time.unscaledTime = 4.3f; NicknameSync.Tick();
            UnityEngine.Time.unscaledTime = 6; NicknameSync.Tick();
            check(owner.Sent.Count == 3 && Encoding.Unicode.GetString(owner.Sent[2].Name) == "Марк", "disabling world styling restores the raw nickname using native synchronization");
            panel.IDInfos[0] = owner.Sent[2];
            UnityEngine.Time.unscaledTime = 6.3f; NicknameSync.Tick();
            profile.Name = Encoding.Unicode.GetBytes("Another mod's name"); panel.IDInfos[0] = profile;
            UnityEngine.Time.unscaledTime = 10; NicknameSync.Tick();
            check(owner.Sent.Count == 3, "after restoration, disabled world styling stops policing other mods' names");
            Plugin.Instance.NicknameWorld.Value = true;
            UnityEngine.Time.unscaledTime = 12; NicknameSync.Tick();
            UnityEngine.Time.unscaledTime = 12.5f; NicknameSync.Tick();
            panel.IDInfos[0] = owner.Sent[3];
            Plugin.Instance.NicknameEnabled.Value = false;
            UnityEngine.Time.unscaledTime = 13; NicknameSync.Tick();
            UnityEngine.Time.unscaledTime = 15; NicknameSync.Tick();
            check(owner.Sent.Count == 5 && Encoding.Unicode.GetString(owner.Sent[4].Name) == "Марк", "disabling nickname styling entirely also restores the world name");
            NicknameSync.RefreshList();
            check(panel.Refreshes == 1, "receiver refreshes an open Tab list after native name update");
            NicknameSync.Clear(); GameAccess.Chat = null;
            Plugin.Instance.NicknameEnabled.Value = false;
        }
    }
}

// Narrow native boundaries for the production nickname synchronization code; no Unity/network renderer is simulated.
public struct PlayerIDInfo { public byte[] Name, Location, DateOfBirth; public bool IsGold; }
public class PlayerController { }
public class PlayerCustomizationController
{
    public bool isOwner = true;
    public readonly List<PlayerIDInfo> Sent = new List<PlayerIDInfo>();
    public void UpdatePlayerInfo(PlayerIDInfo info, object rpcInfo) => Sent.Add(info);
}
public class TextChannelManager { public PlayerCustomizationController MainCustomizationController; public PlayerController MainPlayerController; }
public class PlayerPanelController
{
    public readonly List<PlayerController> PlayerControllers = new List<PlayerController>();
    public readonly List<PlayerIDInfo> IDInfos = new List<PlayerIDInfo>();
    public int Refreshes;
    public void UpdateServerPanelOnChange() => Refreshes++;
}
public static class NetworkSingleton<T> { public static T I; }
namespace UnityEngine { public static class Time { public static float unscaledTime; } }
namespace ChatPlus
{
    internal static class GameAccess { internal static TextChannelManager Chat; internal static string LocalPlayerName; }
    internal sealed partial class Plugin
    {
        internal TestSetting NicknameEnabled = new TestSetting(), NicknameWorld = new TestSetting();
        internal OutgoingStyle NickStyle;
        internal OutgoingStyle BuildNicknameStyle() => NickStyle;
        internal static void LogError(string message, Exception error) => throw new Exception(message, error);
    }
}
