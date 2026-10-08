using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace ChatPlus
{
    public enum UiLanguage
    {
        Auto,
        English,
        Russian
    }

    /// <summary>Texts of the settings window and of the mod's chat messages (English and Russian).</summary>
    internal sealed partial class Lang
    {
        // Settings window
        public string Title;
        public string SectionTime;
        public string SectionHistory;
        public string SectionWindow;
        public string SectionExtras;
        public string SectionNotifications;
        public string ShowGlobalCounter;
        public string ShowLocalCounter;
        public string CountersHint;
        public string GlobalCounterPrefix;
        public string LocalCounterPrefix;
        public string ShowTime;
        public string TimeFormat;
        public string TimeColor;
        public string TimeSize;
        public string TimeOnNotifications;
        public string GlobalLimit;
        public string LocalLimit;
        public string LimitsHint;
        public string RestoreHistory;
        public string RestoreNotifications;
        public string SaveHistory;
        public string KeepAfterRestart;
        public string DailyLogs;
        public string HistoryCount;
        public string OpenFolder;
        public string ClearChat;
        public string ClearHistory;
        public string ConfirmClear;
        public string Width;
        public string Height;
        public string TextSize;
        public string BackgroundOpacity;
        public string BackgroundOpacityHint;
        public string SelectText;
        public string SelectTextHint;
        public string ResizeHandle;
        public string ResizeHint;
        public string RememberPosition;
        public string CullHiddenLines;
        public string ResetWindow;
        public string HighlightMentions;
        public string HighlightColor;
        public string Keywords;
        public string KeywordsHint;
        public string RecallSent;
        public string RightClickCopy;
        public string Close;
        public string WindowHint;
        public string None;
        public string Custom;
        // Parallel to Presets.Colors.
        public string[] ColorNames;

        // Chat
        public string EarlierMessages;
        public string Copied;
        public string DateFormat;
        public string On;
        public string Off;
        public string TimeOn;
        public string TimeOff;
        public string TimeFormatSet;
        public string SizeSet;
        public string TextSizeSet;
        public string LimitsSet;
        public string ChatCleared;
        public string HistoryCleared;
        public string WindowReset;
        public string Status;
        public string KeywordAdded;
        public string KeywordRemoved;
        public string KeywordMissing;
        public string KeywordsList;
        public string KeywordsCleared;
        public string UsageTime;
        public string UsageSize;
        public string UsageText;
        public string UsageLimit;
        public string UsageKeyword;
        public string Help;

        public static readonly Lang English = new Lang
        {
            Title = "Chat Plus",
            SectionTime = "Message time",
            SectionHistory = "History",
            SectionWindow = "Chat window",
            SectionExtras = "Extras",
            SectionNotifications = "Collapsed chat notifications",
            ShowGlobalCounter = "Show the Global message counter",
            ShowLocalCounter = "Show the Local message counter",
            CountersHint = "G: Global · L: Local. Both reset when you expand the chat. Hidden counters keep counting.",
            GlobalCounterPrefix = "G",
            LocalCounterPrefix = "L",
            ShowTime = "Show when each message was sent",
            TimeFormat = "Format",
            TimeColor = "Color",
            TimeSize = "Size",
            TimeOnNotifications = "Also for game notifications",
            GlobalLimit = "Lines in Global",
            LocalLimit = "Lines in Local",
            LimitsHint = "The game itself keeps 50 and 25 lines.",
            RestoreHistory = "Bring earlier messages back after a lobby change",
            RestoreNotifications = "Including game notifications",
            SaveHistory = "Save the history to a file",
            KeepAfterRestart = "Keep the history after a game restart",
            DailyLogs = "Also write readable logs, one file per day",
            HistoryCount = "This session's history: {0} lines",
            OpenFolder = "Open folder",
            ClearChat = "Clear chat",
            ClearHistory = "Clear history",
            ConfirmClear = "Click again to clear",
            Width = "Width",
            Height = "Height",
            TextSize = "Text size",
            BackgroundOpacity = "Background opacity",
            BackgroundOpacityHint = "Applies to backgrounds and ChatPlus buttons. 0%: transparent · 100%: original. Text stays readable.",
            SelectText = "Select chat text with Shift + mouse",
            SelectTextHint = "Drag: scroll. Hold Shift before left-dragging: select, including across messages. Ctrl+C or right-click: copy. Escape: clear.",
            ResizeHandle = "Resize handle on the chat",
            ResizeHint = "Drag the handle at the chat's top-right corner; double-click it for the normal size.",
            RememberPosition = "Remember where the chat was moved",
            CullHiddenLines = "Don't draw lines scrolled out of view (faster)",
            ResetWindow = "Reset size and position",
            HighlightMentions = "Highlight messages that mention you",
            HighlightColor = "Highlight",
            Keywords = "Keywords",
            KeywordsHint = "Your name always counts. More words: /chatplus keyword add <word>",
            RecallSent = "Up / Down in the input field: your previous messages",
            RightClickCopy = "Right-click a message to copy it",
            Close = "Close",
            WindowHint = "{0}: this window · /chatplus help: commands",
            None = "none",
            Custom = "Custom",
            ColorNames = new[] { "Soft", "Gray", "Gold", "Sky", "Mint", "Rose", "White" },

            EarlierMessages = "<align=center><size=80%><color=#F5EDE18C>— earlier messages —</color></size></align>",
            Copied = "Copied",
            DateFormat = "MM/dd",
            On = "on",
            Off = "off",
            TimeOn = "Message time: on",
            TimeOff = "Message time: off",
            TimeFormatSet = "Time format: {0} (looks like {1})",
            SizeSet = "Chat size: {0}% × {1}%",
            TextSizeSet = "Text size: {0}%",
            LimitsSet = "Lines kept: Global {0}, Local {1}",
            ChatCleared = "Chat cleared ({0} lines). The history still has them.",
            HistoryCleared = "History cleared",
            WindowReset = "Chat size and position reset",
            Status = "Time {0} ({1}), lines Global {2} / Local {3}, size {4}% × {5}%, text {6}%, history {7} lines, file {8}",
            KeywordAdded = "Keyword added: {0}",
            KeywordRemoved = "Keyword removed: {0}",
            KeywordMissing = "No such keyword: {0}",
            KeywordsList = "Keywords: {0}",
            KeywordsCleared = "Keywords cleared",
            UsageTime = "Usage: /chatplus time on | off | format <format>, e.g. HH:mm:ss or h:mm tt",
            UsageSize = "Usage: /chatplus size <width %> [height %] | reset",
            UsageText = "Usage: /chatplus text <size %>",
            UsageLimit = "Usage: /chatplus lines <Global> [Local]",
            UsageKeyword = "Usage: /chatplus keyword add | remove <word>, /chatplus keyword list | clear",
            Help =
                "Chat Plus, commands:\n" +
                "/chatplus - settings window ({0})\n" +
                "/chatplus time on | off | format <format>\n" +
                "/chatplus size <width %> [height %] | reset\n" +
                "/chatplus text <size %>\n" +
                "/chatplus lines <Global> [Local] - lines kept in the chat\n" +
                "/chatplus clear - clear the chat, /chatplus clearhistory - forget the history\n" +
                "/chatplus folder - open the history folder\n" +
                "/chatplus keyword add | remove <word>, keyword list | clear\n" +
                "/chatplus status"
        };

        public static readonly Lang Russian = new Lang
        {
            Title = "Улучшенный чат",
            SectionTime = "Время сообщений",
            SectionHistory = "История",
            SectionWindow = "Окно чата",
            SectionExtras = "Дополнительно",
            SectionNotifications = "Уведомления свернутого чата",
            ShowGlobalCounter = "Показывать счетчик глобального чата",
            ShowLocalCounter = "Показывать счетчик локального чата",
            CountersHint = "Г: глобальный · Л: локальный. Оба обнуляются при раскрытии чата. Скрытые счетчики продолжают считать.",
            GlobalCounterPrefix = "Г",
            LocalCounterPrefix = "Л",
            ShowTime = "Показывать время каждого сообщения",
            TimeFormat = "Формат",
            TimeColor = "Цвет",
            TimeSize = "Размер",
            TimeOnNotifications = "И у уведомлений игры",
            GlobalLimit = "Строк в «Глобальном»",
            LocalLimit = "Строк в «Локальном»",
            LimitsHint = "Сама игра хранит 50 и 25 строк.",
            RestoreHistory = "Возвращать прошлые сообщения после смены лобби",
            RestoreNotifications = "Вместе с уведомлениями игры",
            SaveHistory = "Сохранять историю в файл",
            KeepAfterRestart = "Не очищать историю при перезапуске игры",
            DailyLogs = "Ещё и текстовые логи, по файлу на день",
            HistoryCount = "В истории этой сессии: {0} строк",
            OpenFolder = "Открыть папку",
            ClearChat = "Очистить чат",
            ClearHistory = "Очистить историю",
            ConfirmClear = "Нажмите ещё раз",
            Width = "Ширина",
            Height = "Высота",
            TextSize = "Размер текста",
            BackgroundOpacity = "Непрозрачность фона",
            BackgroundOpacityHint = "Фон и кнопки ChatPlus: 0% — прозрачные, 100% — обычные. Прозрачность текста не меняется.",
            SelectText = "Выделять текст чата через Shift + мышь",
            SelectTextHint = "ЛКМ + свайп — прокрутка. Зажмите Shift перед ЛКМ — выделение, в том числе между сообщениями. Ctrl+C или ПКМ — копировать. Esc — снять выделение.",
            ResizeHandle = "Уголок для изменения размера",
            ResizeHint = "Тяните уголок в правом верхнем углу чата; двойной щелчок по нему — обычный размер.",
            RememberPosition = "Запоминать, куда перетащен чат",
            CullHiddenLines = "Не рисовать прокрученные строки (быстрее)",
            ResetWindow = "Сбросить размер и положение",
            HighlightMentions = "Подсвечивать сообщения, где упомянуты вы",
            HighlightColor = "Подсветка",
            Keywords = "Слова",
            KeywordsHint = "Ваше имя учитывается всегда. Ещё слова: /chatplus keyword add <слово>",
            RecallSent = "Вверх / вниз в поле ввода: ваши прошлые сообщения",
            RightClickCopy = "Правый щелчок по сообщению копирует его",
            Close = "Закрыть",
            WindowHint = "{0}: это окно · /chatplus help: команды",
            None = "нет",
            Custom = "Свой",
            ColorNames = new[] { "Мягкий", "Серый", "Золотой", "Голубой", "Мятный", "Розовый", "Белый" },

            EarlierMessages = "<align=center><size=80%><color=#F5EDE18C>— прошлые сообщения —</color></size></align>",
            Copied = "Скопировано",
            DateFormat = "dd.MM",
            On = "вкл",
            Off = "выкл",
            TimeOn = "Время сообщений: вкл",
            TimeOff = "Время сообщений: выкл",
            TimeFormatSet = "Формат времени: {0} (выглядит так: {1})",
            SizeSet = "Размер чата: {0}% × {1}%",
            TextSizeSet = "Размер текста: {0}%",
            LimitsSet = "Хранится строк: «Глобальный» {0}, «Локальный» {1}",
            ChatCleared = "Чат очищен ({0} строк). В истории они остались.",
            HistoryCleared = "История очищена",
            WindowReset = "Размер и положение чата сброшены",
            Status = "Время {0} ({1}), строк «Глобальный» {2} / «Локальный» {3}, размер {4}% × {5}%, текст {6}%, в истории {7} строк, файл {8}",
            KeywordAdded = "Добавлено: {0}",
            KeywordRemoved = "Удалено: {0}",
            KeywordMissing = "Такого слова нет: {0}",
            KeywordsList = "Слова: {0}",
            KeywordsCleared = "Слова удалены",
            UsageTime = "Использование: /chatplus time on | off | format <формат>, например HH:mm:ss или h:mm tt",
            UsageSize = "Использование: /chatplus size <ширина %> [высота %] | reset",
            UsageText = "Использование: /chatplus text <размер %>",
            UsageLimit = "Использование: /chatplus lines <Глобальный> [Локальный]",
            UsageKeyword = "Использование: /chatplus keyword add | remove <слово>, /chatplus keyword list | clear",
            Help =
                "Улучшенный чат, команды:\n" +
                "/chatplus - окно настроек ({0})\n" +
                "/chatplus time on | off | format <формат> - время сообщений\n" +
                "/chatplus size <ширина %> [высота %] | reset - размер окна чата\n" +
                "/chatplus text <размер %> - размер текста\n" +
                "/chatplus lines <Глобальный> [Локальный] - сколько строк хранить в чате\n" +
                "/chatplus clear - очистить чат, /chatplus clearhistory - стереть историю\n" +
                "/chatplus folder - открыть папку с историей\n" +
                "/chatplus keyword add | remove <слово>, keyword list | clear - слова для подсветки\n" +
                "/chatplus status - текущие настройки"
        };

        static Lang _auto = English;
        static float _nextAutoCheck;

        public static Lang Current
        {
            get
            {
                Plugin plugin = Plugin.Instance;
                UiLanguage setting = plugin != null ? plugin.Language.Value : UiLanguage.Auto;
                if (setting == UiLanguage.English)
                    return English;
                if (setting == UiLanguage.Russian)
                    return Russian;
                if (Time.unscaledTime >= _nextAutoCheck)
                {
                    _nextAutoCheck = Time.unscaledTime + 10f;
                    _auto = DetectRussian() ? Russian : English;
                }
                return _auto;
            }
        }

        /// <summary>Re-detects the automatic language on next use (e.g. after the game's language changed).</summary>
        public static void Invalidate()
        {
            _nextAutoCheck = 0f;
        }

        public string ColorName(int index)
        {
            return index >= 0 && index < ColorNames.Length ? ColorNames[index] : Custom;
        }

        static bool DetectRussian()
        {
            try
            {
                string code = GameLocaleCode();
                if (!string.IsNullOrEmpty(code))
                    return code.StartsWith("ru", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                // Unity Localization is not available or not ready: use the system language.
            }
            return Application.systemLanguage == SystemLanguage.Russian;
        }

        // Separate method, so a missing Unity.Localization assembly only fails here.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static string GameLocaleCode()
        {
            Locale locale = LocalizationSettings.SelectedLocale;
            return locale != null ? locale.Identifier.Code : null;
        }
    }
}
