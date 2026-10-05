using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;

namespace ChatPlus
{
    /// <summary>
    /// Client-side chat command /chatplus (alias /chp). Handled commands are never sent to other players.
    /// </summary>
    internal static class ChatCommands
    {
        public const string CommandApiGuid = "com.on-together-mods.commandapi";

        static readonly string[] Names = { "chatplus", "chp" };

        /// <summary>True when the text is one of our commands; <paramref name="args"/> are the words after its name.</summary>
        public static bool TryParse(string text, out string[] args)
        {
            args = null;
            if (string.IsNullOrEmpty(text) || text[0] != '/')
                return false;
            string[] parts = text.Substring(1).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || Array.IndexOf(Names, parts[0].ToLowerInvariant()) < 0)
                return false;
            args = parts.Skip(1).ToArray();
            return true;
        }

        public static void Execute(string[] args)
        {
            Plugin plugin = Plugin.Instance;
            if (plugin == null)
                return;
            Lang lang = Lang.Current;
            if (args.Length == 0)
            {
                plugin.ToggleWindow();
                return;
            }

            switch (args[0].ToLowerInvariant())
            {
                case "ui":
                case "window":
                case "settings":
                    plugin.ToggleWindow();
                    return;
                case "time":
                case "timestamps":
                    Timestamps(plugin, lang, args);
                    return;
                case "size":
                    Size(plugin, lang, args);
                    return;
                case "text":
                case "font":
                    TextSize(plugin, lang, args);
                    return;
                case "lines":
                case "limit":
                case "history":
                    Lines(plugin, lang, args);
                    return;
                case "clear":
                    GameAccess.Notify(string.Format(lang.ChatCleared, ChatLines.Clear()));
                    return;
                case "clearhistory":
                    plugin.History.Clear();
                    GameAccess.Notify(lang.HistoryCleared);
                    return;
                case "folder":
                case "open":
                    plugin.OpenHistoryFolder();
                    return;
                case "reset":
                    plugin.ResetWindow();
                    GameAccess.Notify(lang.WindowReset);
                    return;
                case "keyword":
                case "keywords":
                case "kw":
                    Keyword(plugin, lang, args);
                    return;
                case "status":
                    GameAccess.Notify(plugin.DescribeStatus(lang));
                    return;
                default:
                    GameAccess.Notify(string.Format(lang.Help, plugin.WindowKey.Value));
                    return;
            }
        }

        // /chatplus time [on | off | format HH:mm:ss]
        static void Timestamps(Plugin plugin, Lang lang, string[] args)
        {
            string action = args.Length > 1 ? args[1].ToLowerInvariant() : "toggle";
            switch (action)
            {
                case "toggle":
                case "on":
                case "off":
                    plugin.Timestamps.Value = action == "toggle" ? !plugin.Timestamps.Value : action == "on";
                    GameAccess.Notify(plugin.Timestamps.Value ? lang.TimeOn : lang.TimeOff);
                    return;
                case "format":
                    if (args.Length < 3)
                        break;
                    string format = string.Join(" ", args, 2, args.Length - 2);
                    plugin.TimeFormat.Value = format;
                    plugin.Timestamps.Value = true;
                    GameAccess.Notify(string.Format(lang.TimeFormatSet, format, ChatFormat.FormatTime(DateTime.Now, format)));
                    return;
            }
            GameAccess.Notify(lang.UsageTime);
        }

        // /chatplus size 150 [120] | reset
        static void Size(Plugin plugin, Lang lang, string[] args)
        {
            if (args.Length > 1 && args[1].Equals("reset", StringComparison.OrdinalIgnoreCase))
            {
                ChatWindow.ResetSize();
            }
            else if (args.Length > 1 && TryParsePercent(args[1], out int width))
            {
                int height = plugin.Height.Value;
                if (args.Length > 2 && !TryParsePercent(args[2], out height))
                {
                    GameAccess.Notify(lang.UsageSize);
                    return;
                }
                plugin.Width.Value = width;
                plugin.Height.Value = height;
            }
            else
            {
                GameAccess.Notify(lang.UsageSize);
                return;
            }
            GameAccess.Notify(string.Format(lang.SizeSet, plugin.Width.Value, plugin.Height.Value));
        }

        // /chatplus text 120
        static void TextSize(Plugin plugin, Lang lang, string[] args)
        {
            if (args.Length < 2 || !TryParsePercent(args[1], out int percent))
            {
                GameAccess.Notify(lang.UsageText);
                return;
            }
            plugin.TextSize.Value = percent;
            GameAccess.Notify(string.Format(lang.TextSizeSet, plugin.TextSize.Value));
        }

        // /chatplus lines 300 [150]
        static void Lines(Plugin plugin, Lang lang, string[] args)
        {
            if (args.Length < 2 || !int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int global))
            {
                GameAccess.Notify(lang.UsageLimit);
                return;
            }
            int local = plugin.LocalLimit.Value;
            if (args.Length > 2 && !int.TryParse(args[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out local))
            {
                GameAccess.Notify(lang.UsageLimit);
                return;
            }
            plugin.GlobalLimit.Value = global;
            plugin.LocalLimit.Value = local;
            GameAccess.Notify(string.Format(lang.LimitsSet, plugin.GlobalLimit.Value, plugin.LocalLimit.Value));
        }

        // /chatplus keyword add mark, markus | remove mark | list | clear
        static void Keyword(Plugin plugin, Lang lang, string[] args)
        {
            string action = args.Length > 1 ? args[1].ToLowerInvariant() : "list";
            List<string> given = TextUtil.ParseKeywords(args.Length > 2 ? string.Join(" ", args, 2, args.Length - 2) : null);
            List<string> words = TextUtil.ParseKeywords(plugin.Keywords.Value);
            switch (action)
            {
                case "add":
                    if (given.Count == 0)
                        break;
                    foreach (string word in given)
                    {
                        if (!words.Exists(w => string.Equals(w, word, StringComparison.OrdinalIgnoreCase)))
                            words.Add(word);
                    }
                    plugin.Keywords.Value = TextUtil.JoinKeywords(words);
                    GameAccess.Notify(string.Format(lang.KeywordAdded, TextUtil.JoinKeywords(given)));
                    return;
                case "remove":
                case "delete":
                    if (given.Count == 0)
                        break;
                    int removed = 0;
                    foreach (string word in given)
                        removed += words.RemoveAll(w => string.Equals(w, word, StringComparison.OrdinalIgnoreCase));
                    if (removed == 0)
                    {
                        GameAccess.Notify(string.Format(lang.KeywordMissing, TextUtil.JoinKeywords(given)));
                        return;
                    }
                    plugin.Keywords.Value = TextUtil.JoinKeywords(words);
                    GameAccess.Notify(string.Format(lang.KeywordRemoved, TextUtil.JoinKeywords(given)));
                    return;
                case "clear":
                    plugin.Keywords.Value = string.Empty;
                    GameAccess.Notify(lang.KeywordsCleared);
                    return;
                case "list":
                    GameAccess.Notify(string.Format(lang.KeywordsList, words.Count > 0 ? TextUtil.JoinKeywords(words) : lang.None));
                    return;
            }
            GameAccess.Notify(lang.UsageKeyword);
        }

        static bool TryParsePercent(string text, out int percent)
        {
            return int.TryParse((text ?? string.Empty).TrimEnd('%'), NumberStyles.Integer, CultureInfo.InvariantCulture, out percent);
        }

        /// <summary>
        /// When CommandAPI is installed, register the command there too so it is listed by its /help and
        /// suggested by CommandTypeahead. Execution still goes through our own prefix, which runs first.
        /// </summary>
        public static void RegisterWithCommandApi()
        {
            if (!Chainloader.PluginInfos.TryGetValue(CommandApiGuid, out BepInEx.PluginInfo info) || info.Instance == null)
                return;
            try
            {
                Assembly assembly = info.Instance.GetType().Assembly;
                Type registry = assembly.GetType("CommandAPI.CommandRegistry");
                Type parameterType = assembly.GetType("CommandAPI.Parameter");
                Type parameterKind = assembly.GetType("CommandAPI.ParameterType");
                if (registry == null || parameterType == null || parameterKind == null)
                    return;
                MethodInfo register = registry.GetMethod("Register", new[]
                {
                    typeof(string), typeof(string), typeof(Action<string[]>), typeof(string), parameterType.MakeArrayType()
                });
                if (register == null)
                    return;

                object stringKind = Enum.Parse(parameterKind, "String");
                Array parameters = Array.CreateInstance(parameterType, 2);
                parameters.SetValue(Activator.CreateInstance(parameterType, "action", stringKind, true, null, null), 0);
                parameters.SetValue(Activator.CreateInstance(parameterType, "value", stringKind, true, null, null), 1);

                var handler = new Action<string[]>(Execute);
                foreach (string name in Names)
                {
                    register.Invoke(null, new object[]
                    {
                        name, Plugin.PluginName, handler,
                        "Chat Plus: settings window, message time, chat size, text size, lines kept, clear, history folder",
                        parameters
                    });
                }
                Plugin.Log.LogInfo("Registered /chatplus with CommandAPI.");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Could not register with CommandAPI (the command still works): " + e.Message);
            }
        }
    }
}
