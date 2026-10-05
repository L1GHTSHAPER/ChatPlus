using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace ChatPlus
{
    // Every patch catches its own exceptions: a failure in the mod must never stop a chat message from showing.

    /// <summary>The newest line of a chat tab before the game's method ran, to find the line it adds.</summary>
    internal sealed class LineMark
    {
        public GameObject Newest;

        public static LineMark Take(TextChannelManager chat, bool isLocal)
        {
            List<GameObject> lines = GameAccess.Lines(chat, isLocal);
            return new LineMark { Newest = lines != null && lines.Count > 0 ? lines[lines.Count - 1] : null };
        }
    }

    /// <summary>
    /// The game calls AddMessageUI for every player message it shows (sendByPlayer = true for yours), after its
    /// filters for ignored and muted players and the local chat distance.
    /// </summary>
    [HarmonyPatch(typeof(TextChannelManager), "AddMessageUI")]
    internal static class AddMessagePatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        static void Prefix(TextChannelManager __instance, bool isLocal, out LineMark __state)
        {
            __state = null;
            try
            {
                ChatLines.ApplyLimits();
                __state = LineMark.Take(__instance, isLocal);
            }
            catch (Exception e)
            {
                Plugin.LogError("Could not set the chat line limits: ", e);
            }
        }

        // First, so that a line another mod's postfix adds is not taken for the message's line.
        [HarmonyPostfix]
        [HarmonyPriority(Priority.First)]
        static void Postfix(TextChannelManager __instance, string userName, string text, bool isLocal, int senderIndex, bool sendByPlayer, LineMark __state)
        {
            try
            {
                ChatLines.OnMessageAdded(__instance, userName, text, isLocal, senderIndex, sendByPlayer, __state);
            }
            catch (Exception e)
            {
                Plugin.LogError("Could not process a chat message: ", e);
            }
        }
    }

    /// <summary>Lines of the game and of other mods in the Global tab: someone joined, XP earned, command output.</summary>
    [HarmonyPatch(typeof(TextChannelManager), nameof(TextChannelManager.AddNotification))]
    internal static class AddNotificationPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        static void Prefix(TextChannelManager __instance, out LineMark __state)
        {
            __state = null;
            try
            {
                ChatLines.ApplyLimits();
                __state = LineMark.Take(__instance, false);
            }
            catch (Exception e)
            {
                Plugin.LogError("Could not set the chat line limits: ", e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.First)]
        static void Postfix(TextChannelManager __instance, string text, LineMark __state)
        {
            try
            {
                ChatLines.OnNotificationAdded(__instance, text, __state);
            }
            catch (Exception e)
            {
                Plugin.LogError("Could not process a chat notification: ", e);
            }
        }
    }

    // The game writes its combined hidden counter after AddMessageUI; replace it once that write is complete.
    [HarmonyPatch(typeof(TextChannelManager), "OnChannelMessageReceived")]
    internal static class ReceivedCounterPatch
    {
        [HarmonyPostfix]
        static void Postfix() => HiddenChatNotifications.Refresh();
    }

    [HarmonyPatch(typeof(UIManager), nameof(UIManager.ButtonHide))]
    internal static class HideChatCounterPatch
    {
        [HarmonyPostfix]
        static void Postfix() => HiddenChatNotifications.Refresh();
    }

    /// <summary>The chat of a lobby is set up: put the session's earlier lines back.</summary>
    [HarmonyPatch(typeof(TextChannelManager), "Start")]
    internal static class ChatStartPatch
    {
        [HarmonyPostfix]
        static void Postfix(TextChannelManager __instance)
        {
            try
            {
                ChatLines.OnChatStarted(__instance);
            }
            catch (Exception e)
            {
                Plugin.LogError("Could not restore the chat history: ", e);
            }
        }
    }

    /// <summary>
    /// When the chat panel fades in or out, the game switches both chat tabs off and on (twice) to refresh the font
    /// outline, which rebuilds every line. That only matters when the outline was switched, which in the normal
    /// window mode never happens; with hundreds of lines it is slow, so it is skipped when nothing changed.
    /// </summary>
    [HarmonyPatch(typeof(TextChannelManager), nameof(TextChannelManager.ResetTextMaterials))]
    internal static class ResetMaterialsPatch
    {
        [HarmonyPrefix]
        static bool Prefix(TextChannelManager __instance)
        {
            try
            {
                return OutlineResets.ShouldRun(__instance);
            }
            catch (Exception e)
            {
                Plugin.LogError("Could not check the chat font outline: ", e);
                return true;
            }
        }
    }

    /// <summary>
    /// The game UI is set up. UIManager.Start also records the panel's default place as the drag limit, so the panel
    /// is resized and moved only after it.
    /// </summary>
    [HarmonyPatch(typeof(UIManager), "Start")]
    internal static class UiStartPatch
    {
        [HarmonyPostfix]
        static void Postfix(UIManager __instance)
        {
            try
            {
                ChatWindow.Prepare(__instance);
            }
            catch (Exception e)
            {
                Plugin.LogError("Could not set up the chat window: ", e);
            }
        }
    }

    [HarmonyPatch(typeof(UIManager), nameof(UIManager.BeginDragMessage))]
    internal static class BeginMovePatch
    {
        [HarmonyPostfix]
        static void Postfix(UIManager __instance)
        {
            try
            {
                ChatWindow.OnBeginMove(__instance);
            }
            catch (Exception e)
            {
                Plugin.LogError("Could not update the chat drag limits: ", e);
            }
        }
    }

    [HarmonyPatch(typeof(UIManager), nameof(UIManager.EndDragMessage))]
    internal static class EndMovePatch
    {
        [HarmonyPostfix]
        static void Postfix(UIManager __instance)
        {
            try
            {
                ChatWindow.OnEndMove(__instance);
            }
            catch (Exception e)
            {
                Plugin.LogError("Could not save the chat position: ", e);
            }
        }
    }

    /// <summary>
    /// Enter in the chat: remembers the text for Up/Down recall and runs /chatplus. Runs before the other mods'
    /// command handlers, so their commands are remembered too. For our command the input field is cleared before it
    /// runs, so the game and the other prefixes see an empty message: nothing is sent (even if the command fails), and
    /// the game still releases the input lock.
    /// </summary>
    [HarmonyPatch(typeof(TextChannelManager), nameof(TextChannelManager.OnEnterPressed))]
    internal static class EnterPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        [HarmonyBefore("ontogether.chatsounds", "ontogether.localchatrange", "com.on-together-mods.commandapi", "com.andrewlin.ontogether.alpha")]
        static void Prefix()
        {
            TMP_InputField input;
            string[] args;
            try
            {
                input = GameAccess.MessageInput;
                if (input == null)
                    return;
                string text = input.text;
                Plugin.Instance.Sent.Add(text);
                if (!ChatCommands.TryParse(text, out args))
                    return;
                input.text = string.Empty;
            }
            catch (Exception e)
            {
                Plugin.LogError("Could not read the chat input: ", e);
                return;
            }
            try
            {
                ChatCommands.Execute(args);
            }
            catch (Exception e)
            {
                Plugin.LogError("Chat command failed: ", e);
            }
        }
    }
}
