namespace ChatPlus
{
    internal sealed partial class Lang
    {
        string OutgoingText(string english, string russian) => ReferenceEquals(this, Russian) ? russian : english;
        static readonly string[] EnglishColorModes = { "Original", "Solid color", "Gradient" };
        static readonly string[] RussianColorModes = { "Обычный", "Один цвет", "Градиент" };

        public string SectionOutgoing => OutgoingText("Message editor", "Редактор сообщений");
        public string OutgoingEnable => OutgoingText("Apply this style to messages I send", "Оформлять мои отправляемые сообщения");
        public string OutgoingFont => OutgoingText("Font", "Шрифт");
        public string OutgoingDefaultFont => OutgoingText("Game font", "Шрифт игры");
        public string OutgoingDefaultHint => OutgoingText("Uses the normal font of the recipient's game.", "Использует обычный шрифт игры у получателя.");
        public string OutgoingFontHint => OutgoingText("Liberation Sans changes Latin letters. Russian letters keep the usual font.", "Liberation Sans меняет латиницу. Русские буквы сохраняют обычный шрифт.");
        public string OutgoingColorMode => OutgoingText("Color style", "Окраска текста");
        public string[] OutgoingColorModes => ReferenceEquals(this, Russian) ? RussianColorModes : EnglishColorModes;
        public string OutgoingStartColor => OutgoingText("Start color", "Первый цвет");
        public string OutgoingEndColor => OutgoingText("End color", "Последний цвет");
        public string OutgoingBold => OutgoingText("Bold", "Жирный");
        public string OutgoingItalic => OutgoingText("Italic", "Курсив");
        public string OutgoingDraft => OutgoingText("Message draft", "Черновик сообщения");
        public string OutgoingPreview => OutgoingText("Preview", "Предпросмотр");
        public string OutgoingSample => OutgoingText("Hello! Let's meet by the lake.", "Привет! Hello! Встречаемся у озера.");
        public string OutgoingBudget => OutgoingText("Message with formatting: {0} / {1} characters", "Сообщение с оформлением: {0} / {1} символов");
        public string OutgoingTooLong => OutgoingText("Message with formatting exceeds 250 characters. Shorten it or simplify the style; the draft was kept.", "Сообщение с оформлением длиннее 250 символов. Сократите текст или упростите оформление; черновик сохранён.");
        public string OutgoingFailed => OutgoingText("Message was not sent: formatting failed. Check the ChatPlus log.", "Сообщение не отправлено: ошибка оформления. Проверьте журнал ChatPlus.");
        public string OutgoingInsert => OutgoingText("Insert into chat", "Вставить в чат");
        public string OutgoingReset => OutgoingText("Reset style", "Сбросить оформление");
        public string OutgoingInvalidColor => OutgoingText("Enter six hex digits: #RRGGBB. The last valid color is still in use.", "Введите шесть hex-цифр: #RRGGBB. Пока используется последний корректный цвет.");
        public string OutgoingColorHint => OutgoingText("Enter #RRGGBB, choose a preset, or click the swatch for RGB.", "Введите #RRGGBB, выберите готовый цвет или нажмите образец для RGB.");
        public string OutgoingHint => OutgoingText("Visible to players without mods. Click a color swatch for RGB sliders. Gradients use up to 8 colors to fit the message limit. Commands and manually tagged messages keep their formatting.", "Видно игрокам без модов. Нажмите образец цвета для ползунков RGB. Градиент использует до 8 цветов с учётом лимита длины. Команды и сообщения с вручную введёнными тегами сохраняют своё оформление.");
    }
}
